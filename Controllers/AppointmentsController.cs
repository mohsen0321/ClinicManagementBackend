using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using clinicsystembackend.Data;
using clinicsystembackend.DTOs;
using clinicsystembackend.Models;

namespace clinicsystembackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out int id) ? id : 0;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        [HttpGet("available-slots")]
        public async Task<IActionResult> GetAvailableSlots([FromQuery] int doctorId, [FromQuery] DateTime date)
        {
            var requestedDate = date.Date;
            if (requestedDate < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "لا يمكن البحث عن مواعيد في تاريخ سابق" });
            }

            var doctor = await _context.Doctors
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                return NotFound(new { message = "الطبيب غير موجود" });

            var dayOfWeek = requestedDate.DayOfWeek;

            var workingHour = doctor.WorkingHours
                .FirstOrDefault(w => w.DayOfWeek == dayOfWeek && w.IsAvailable);

            if (workingHour == null)
            {
                return Ok(new List<string>());
            }

            var duration = TimeSpan.FromMinutes(doctor.AppointmentDurationMinutes > 0 ? doctor.AppointmentDurationMinutes : 30);
            var allSlots = new List<TimeSpan>();
            var currentSlot = workingHour.StartTime;

            while (currentSlot + duration <= workingHour.EndTime)
            {
                allSlots.Add(currentSlot);
                currentSlot = currentSlot.Add(duration);
            }

            var bookedSlots = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                            a.AppointmentDate.Date == requestedDate &&
                            a.Status != "Rejected" &&
                            a.Status != "Cancelled")
                .Select(a => a.TimeSlot)
                .ToListAsync();

            var availableSlots = allSlots
                .Where(slot => !bookedSlots.Contains(slot))
                .Select(slot => slot.ToString(@"hh\:mm"))
                .ToList();

            return Ok(availableSlots);
        }

        [Authorize(Roles = "Patient")]
        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var patientId = GetCurrentUserId();
            var bookingDate = dto.AppointmentDate.Date;

            if (bookingDate < DateTime.UtcNow.Date)
                return BadRequest(new { message = "لا يمكن الحجز في تاريخ سابق" });

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == dto.DoctorId);

            if (doctor == null || !doctor.User.IsActive)
                return BadRequest(new { message = "الطبيب المحدد غير متاح حالياً" });

            var dayOfWeek = bookingDate.DayOfWeek;
            var workingHour = doctor.WorkingHours
                .FirstOrDefault(w => w.DayOfWeek == dayOfWeek && w.IsAvailable);

            if (workingHour == null || dto.TimeSlot < workingHour.StartTime || dto.TimeSlot >= workingHour.EndTime)
                return BadRequest(new { message = "هذا الوقت يقع خارج ساعات عمل الطبيب" });

            bool isAlreadyBooked = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == dto.DoctorId &&
                a.AppointmentDate.Date == bookingDate &&
                a.TimeSlot == dto.TimeSlot &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            if (isAlreadyBooked)
                return Conflict(new { message = "عذراً، هذا الموعد تم حجزه للتو من قِبل مريض آخر. يرجى اختيار موعد آخر." });

            bool patientHasConflict = await _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.AppointmentDate.Date == bookingDate &&
                a.TimeSlot == dto.TimeSlot &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            if (patientHasConflict)
                return Conflict(new { message = "لديك حجز آخر مسجل بالفعل في نفس هذا التوقيت" });

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = dto.DoctorId,
                AppointmentDate = bookingDate,
                TimeSlot = dto.TimeSlot,
                Status = "Pending",
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { message = "تم تقديم طلب الحجز بنجاح وهو قيد مراجعة العيادة", appointmentId = appointment.Id });
        }

        [Authorize(Roles = "Patient")]
        [HttpGet("my-appointments")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var patientId = GetCurrentUserId();

            var appointments = await _context.Appointments
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.TimeSlot)
                .Select(a => new AppointmentDetailDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    PatientPhone = a.Patient.User.PhoneNumber,
                    PatientEmail = a.Patient.User.Email,
                    DoctorId = a.DoctorId,
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    DoctorPicture = a.Doctor.User.ProfilePictureUrl,
                    SpecialtyName = a.Doctor.Specialty.Name,
                    ClinicName = a.Doctor.Clinic.Name,
                    ClinicAddress = a.Doctor.Clinic.Address,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status,
                    RejectionReason = a.RejectionReason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var a = await _context.Appointments
                .Include(x => x.Patient).ThenInclude(p => p.User)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Doctor).ThenInclude(d => d.Specialty)
                .Include(x => x.Doctor).ThenInclude(d => d.Clinic)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null)
                return NotFound(new { message = "الحجز غير موجود" });

            if (!isAdmin && a.PatientId != currentUserId)
                return Forbid();

            var dto = new AppointmentDetailDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                PatientPhone = a.Patient.User.PhoneNumber,
                PatientEmail = a.Patient.User.Email,
                DoctorId = a.DoctorId,
                DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                DoctorPicture = a.Doctor.User.ProfilePictureUrl,
                SpecialtyName = a.Doctor.Specialty.Name,
                ClinicName = a.Doctor.Clinic.Name,
                ClinicAddress = a.Doctor.Clinic.Address,
                AppointmentDate = a.AppointmentDate,
                TimeSlot = a.TimeSlot,
                Status = a.Status,
                RejectionReason = a.RejectionReason,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            };

            return Ok(dto);
        }

        [Authorize]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            if (!isAdmin && appointment.PatientId != currentUserId)
                return Forbid();

            if (appointment.Status == "Completed")
                return BadRequest(new { message = "لا يمكن إلغاء حجز تمت زيارته بالفعل" });

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إلغاء الحجز بنجاح" });
        }

        [Authorize]
        [HttpPut("{id}/reschedule")]
        public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            if (!isAdmin && appointment.PatientId != currentUserId)
                return Forbid();

            var newDate = dto.NewDate.Date;
            if (newDate < DateTime.UtcNow.Date)
                return BadRequest(new { message = "لا يمكن تعديل الحجز لتاريخ سابق" });

            bool isSlotTaken = await _context.Appointments.AnyAsync(a =>
                a.Id != id &&
                a.DoctorId == appointment.DoctorId &&
                a.AppointmentDate.Date == newDate &&
                a.TimeSlot == dto.NewTimeSlot &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            if (isSlotTaken)
                return Conflict(new { message = "الموعد الجديد غير متاح، يرجى اختيار موعد آخر" });

            appointment.AppointmentDate = newDate;
            appointment.TimeSlot = dto.NewTimeSlot;
            appointment.Status = "Pending"; 
            appointment.RejectionReason = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تعديل الموعد بنجاح وهو بانتظار التأكيد" });
        }

        
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-all")]
        public async Task<IActionResult> GetAllForAdmin(
            [FromQuery] string? status,
            [FromQuery] int? doctorId,
            [FromQuery] int? clinicId,
            [FromQuery] DateTime? date,
            [FromQuery] string? search)
        {
            var query = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Specialty)
                .Include(a => a.Doctor).ThenInclude(d => d.Clinic)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status.ToLower() == status.Trim().ToLower());

            if (doctorId.HasValue && doctorId.Value > 0)
                query = query.Where(a => a.DoctorId == doctorId.Value);

            if (clinicId.HasValue && clinicId.Value > 0)
                query = query.Where(a => a.Doctor.ClinicId == clinicId.Value);

            if (date.HasValue)
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Patient.User.FirstName.ToLower().Contains(s) ||
                    a.Patient.User.LastName.ToLower().Contains(s) ||
                    a.Patient.User.PhoneNumber.Contains(s) ||
                    a.Doctor.User.FirstName.ToLower().Contains(s) ||
                    a.Doctor.User.LastName.ToLower().Contains(s));
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.TimeSlot)
                .Select(a => new AppointmentDetailDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    PatientPhone = a.Patient.User.PhoneNumber,
                    PatientEmail = a.Patient.User.Email,
                    DoctorId = a.DoctorId,
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    DoctorPicture = a.Doctor.User.ProfilePictureUrl,
                    SpecialtyName = a.Doctor.Specialty.Name,
                    ClinicName = a.Doctor.Clinic.Name,
                    ClinicAddress = a.Doctor.Clinic.Address,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status,
                    RejectionReason = a.RejectionReason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> AcceptAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            appointment.Status = "Accepted";
            appointment.RejectionReason = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم قبول الحجز وتأكيده بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectAppointment(int id, [FromBody] RejectAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            appointment.Status = "Rejected";
            appointment.RejectionReason = dto.RejectionReason.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم رفض الحجز وتسجيل سبب الرفض" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            appointment.Status = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new { message = "تمت الزيارة واكتمال الحجز بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الحجز غير موجود" });

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف سجل الحجز بنجاح" });
        }
    }
}