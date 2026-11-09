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
    public class DoctorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. البحث والتصفية وعرض الأطباء (متاح للجميع)
        // أمثلة:
        // /api/doctors
        // /api/doctors?specialtyId=1
        // /api/doctors?clinicId=1
        // /api/doctors?search=محمد
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? specialtyId,
            [FromQuery] int? clinicId)
        {
            // بناء استعلام LINQ ديناميكي
            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Clinic)
                .AsQueryable();

            // تصفية حسب اسم الطبيب
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(d =>
                    d.User.FirstName.ToLower().Contains(searchLower) ||
                    d.User.LastName.ToLower().Contains(searchLower));
            }

            // تصفية حسب التخصص
            if (specialtyId.HasValue && specialtyId.Value > 0)
            {
                query = query.Where(d => d.SpecialtyId == specialtyId.Value);
            }

            // تصفية حسب العيادة
            if (clinicId.HasValue && clinicId.Value > 0)
            {
                query = query.Where(d => d.ClinicId == clinicId.Value);
            }

            var doctors = await query
                .Select(d => new DoctorListDto
                {
                    Id = d.Id,
                    FullName = $"{d.User.FirstName} {d.User.LastName}",
                    Email = d.User.Email,
                    PhoneNumber = d.User.PhoneNumber,
                    ProfilePictureUrl = d.User.ProfilePictureUrl,
                    Biography = d.Biography,
                    SpecialtyName = d.Specialty.Name,
                    SpecialtyId = d.SpecialtyId,
                    ClinicName = d.Clinic.Name,
                    ClinicId = d.ClinicId,
                    AppointmentDurationMinutes = d.AppointmentDurationMinutes,
                    IsActive = d.User.IsActive
                })
                .ToListAsync();

            return Ok(doctors);
        }

        // 2. جلب تفاصيل طبيب مع ساعات عمله الكاملة
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Clinic)
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound(new { message = "الطبيب غير موجود" });

            var result = new
            {
                doctor.Id,
                doctor.User.FirstName,
                doctor.User.LastName,
                FullName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                doctor.User.Email,
                doctor.User.PhoneNumber,
                doctor.User.ProfilePictureUrl,
                doctor.Biography,
                doctor.SpecialtyId,
                SpecialtyName = doctor.Specialty.Name,
                doctor.ClinicId,
                ClinicName = doctor.Clinic.Name,
                doctor.AppointmentDurationMinutes,
                doctor.User.IsActive,
                WorkingHours = doctor.WorkingHours.Select(w => new WorkingHourDto
                {
                    DayOfWeek = w.DayOfWeek,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    IsAvailable = w.IsAvailable
                }).OrderBy(w => w.DayOfWeek).ToList()
            };

            return Ok(result);
        }

        // 3. إضافة طبيب جديد مع حسابه ومواعيده (أدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DoctorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // التحقق من عدم تكرار البريد ورقم الهاتف
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower()))
                return Conflict(new { message = "البريد الإلكتروني مسجل بالفعل" });

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber.Trim()))
                return Conflict(new { message = "رقم الهاتف مسجل بالفعل" });

            // التحقق من وجود التخصص والعيادة
            if (!await _context.Specialties.AnyAsync(s => s.Id == dto.SpecialtyId))
                return BadRequest(new { message = "التخصص المحدد غير موجود" });

            if (!await _context.Clinics.AnyAsync(c => c.Id == dto.ClinicId))
                return BadRequest(new { message = "العيادة المحددة غير موجودة" });

            // 1. إنشاء حساب المستخدم كـ Admin (طبيب مسؤول)
            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Admin",
                ProfilePictureUrl = dto.ProfilePictureUrl,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. إنشاء سجل الطبيب
            var doctor = new Doctor
            {
                Id = user.Id,
                Biography = dto.Biography?.Trim(),
                SpecialtyId = dto.SpecialtyId,
                ClinicId = dto.ClinicId,
                AppointmentDurationMinutes = dto.AppointmentDurationMinutes > 0 ? dto.AppointmentDurationMinutes : 30
            };

            // 3. إضافة ساعات العمل إن وجدت
            if (dto.WorkingHours != null && dto.WorkingHours.Any())
            {
                foreach (var wh in dto.WorkingHours)
                {
                    doctor.WorkingHours.Add(new DoctorWorkingHour
                    {
                        DayOfWeek = wh.DayOfWeek,
                        StartTime = wh.StartTime,
                        EndTime = wh.EndTime,
                        IsAvailable = wh.IsAvailable
                    });
                }
            }

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { message = "تم إنشاء حساب الطبيب بنجاح", doctorId = doctor.Id });
        }

        // 4. تعديل بيانات الطبيب وساعات عمله (أدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DoctorUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound(new { message = "الطبيب غير موجود" });

            // فحص تكرار الإيميل ورقم الهاتف لمستخدم آخر
            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower() && u.Id != doctor.Id);
            if (emailExists)
                return Conflict(new { message = "البريد الإلكتروني مسجل لمستخدم آخر" });

            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber.Trim() && u.Id != doctor.Id);
            if (phoneExists)
                return Conflict(new { message = "رقم الهاتف مسجل لمستخدم آخر" });

            // تحديث بيانات المستخدم
            doctor.User.FirstName = dto.FirstName.Trim();
            doctor.User.LastName = dto.LastName.Trim();
            doctor.User.Email = dto.Email.Trim().ToLower();
            doctor.User.PhoneNumber = dto.PhoneNumber.Trim();
            doctor.User.ProfilePictureUrl = dto.ProfilePictureUrl;
            doctor.User.IsActive = dto.IsActive;

            // تحديث بيانات الطبيب
            doctor.Biography = dto.Biography?.Trim();
            doctor.SpecialtyId = dto.SpecialtyId;
            doctor.ClinicId = dto.ClinicId;
            doctor.AppointmentDurationMinutes = dto.AppointmentDurationMinutes;

            // تحديث ساعات العمل: نحذف القديمة ونضيف الجديدة
            _context.DoctorWorkingHours.RemoveRange(doctor.WorkingHours);
            if (dto.WorkingHours != null)
            {
                foreach (var wh in dto.WorkingHours)
                {
                    _context.DoctorWorkingHours.Add(new DoctorWorkingHour
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = wh.DayOfWeek,
                        StartTime = wh.StartTime,
                        EndTime = wh.EndTime,
                        IsAvailable = wh.IsAvailable
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تحديث بيانات الطبيب ومواعيده بنجاح" });
        }

        // 5. تفعيل أو تعطيل حساب الطبيب (أدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound(new { message = "الطبيب غير موجود" });

            doctor.User.IsActive = !doctor.User.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = doctor.User.IsActive ? "تم تفعيل حساب الطبيب" : "تم تعطيل حساب الطبيب",
                isActive = doctor.User.IsActive
            });
        }
    }
}