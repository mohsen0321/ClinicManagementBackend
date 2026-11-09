using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using clinicsystembackend.Data;
using clinicsystembackend.DTOs;

namespace clinicsystembackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // متاح للأدمن فقط
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض جميع المرضى مع إمكانية البحث بالاسم أو الهاتف أو الإيميل
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.Patients
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.User.FirstName.ToLower().Contains(s) ||
                    p.User.LastName.ToLower().Contains(s) ||
                    p.User.Email.ToLower().Contains(s) ||
                    p.User.PhoneNumber.Contains(s));
            }

            var patients = await query
                .OrderByDescending(p => p.User.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    FullName = $"{p.User.FirstName} {p.User.LastName}",
                    p.User.Email,
                    p.User.PhoneNumber,
                    p.User.ProfilePictureUrl,
                    p.Gender,
                    p.DateOfBirth,
                    p.Address,
                    p.User.CreatedAt,
                    p.User.IsActive,
                    TotalAppointments = p.Appointments.Count()
                })
                .ToListAsync();

            return Ok(patients);
        }

        // 2. عرض تفاصيل مريض محدد مع سجل جميع حجوزاته السابقة
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor).ThenInclude(d => d.Specialty)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor).ThenInclude(d => d.Clinic)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound(new { message = "المريض غير موجود" });

            var result = new
            {
                patient.Id,
                FullName = $"{patient.User.FirstName} {patient.User.LastName}",
                patient.User.Email,
                patient.User.PhoneNumber,
                patient.User.ProfilePictureUrl,
                patient.Gender,
                patient.DateOfBirth,
                patient.Address,
                patient.User.CreatedAt,
                patient.User.IsActive,
                Appointments = patient.Appointments
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new AppointmentDetailDto
                    {
                        Id = a.Id,
                        DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                        SpecialtyName = a.Doctor.Specialty.Name,
                        ClinicName = a.Doctor.Clinic.Name,
                        AppointmentDate = a.AppointmentDate,
                        TimeSlot = a.TimeSlot,
                        Status = a.Status,
                        RejectionReason = a.RejectionReason,
                        CreatedAt = a.CreatedAt
                    }).ToList()
            };

            return Ok(result);
        }

        // 3. تعطيل أو تفعيل حساب المريض
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound(new { message = "المريض غير موجود" });

            patient.User.IsActive = !patient.User.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = patient.User.IsActive ? "تم تفعيل حساب المريض" : "تم تعطيل حساب المريض",
                isActive = patient.User.IsActive
            });
        }
    }
}