using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using clinicsystembackend.Data;
using clinicsystembackend.DTOs;

namespace clinicsystembackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. إحصائيات لوحة تحكم المسؤول (Admin Dashboard Overview)
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var stats = new AdminDashboardStatsDto
            {
                TotalPatients = await _context.Patients.CountAsync(),
                TotalDoctors = await _context.Doctors.CountAsync(d => d.User.IsActive),
                TotalClinics = await _context.Clinics.CountAsync(c => c.IsActive),
                TotalSpecialties = await _context.Specialties.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending"),
                AcceptedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Accepted"),
                RejectedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Rejected"),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Completed")
            };

            // أحدث 5 حجوزات مسجلة
            stats.RecentAppointments = await _context.Appointments
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new AppointmentDetailDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    PatientPhone = a.Patient.User.PhoneNumber,
                    DoctorId = a.DoctorId,
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    SpecialtyName = a.Doctor.Specialty.Name,
                    ClinicName = a.Doctor.Clinic.Name,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status
                })
                .ToListAsync();

            return Ok(stats);
        }

        // 2. إحصائيات لوحة تحكم المريض (Patient Dashboard Overview)
        [Authorize(Roles = "Patient")]
        [HttpGet("patient-stats")]
        public async Task<IActionResult> GetPatientStats()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out int patientId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(patientId);
            if (user == null)
                return NotFound();

            var myAppointmentsQuery = _context.Appointments.Where(a => a.PatientId == patientId);

            var stats = new PatientDashboardStatsDto
            {
                PatientName = $"{user.FirstName} {user.LastName}",
                ProfilePictureUrl = user.ProfilePictureUrl,
                TotalAppointments = await myAppointmentsQuery.CountAsync(),
                PendingAppointments = await myAppointmentsQuery.CountAsync(a => a.Status == "Pending"),
                AcceptedAppointments = await myAppointmentsQuery.CountAsync(a => a.Status == "Accepted"),
                RejectedAppointments = await myAppointmentsQuery.CountAsync(a => a.Status == "Rejected")
            };

            var today = DateTime.UtcNow.Date;

            // أقرب موعد قادم للمريض
            stats.UpcomingAppointment = await myAppointmentsQuery
                .Where(a => a.AppointmentDate.Date >= today && (a.Status == "Accepted" || a.Status == "Pending"))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .Select(a => new AppointmentDetailDto
                {
                    Id = a.Id,
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    SpecialtyName = a.Doctor.Specialty.Name,
                    ClinicName = a.Doctor.Clinic.Name,
                    ClinicAddress = a.Doctor.Clinic.Address,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status
                })
                .FirstOrDefaultAsync();

            // آخر 5 حجوزات قام بها
            stats.RecentAppointments = await myAppointmentsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new AppointmentDetailDto
                {
                    Id = a.Id,
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    SpecialtyName = a.Doctor.Specialty.Name,
                    ClinicName = a.Doctor.Clinic.Name,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status
                })
                .ToListAsync();

            return Ok(stats);
        }
    }
}