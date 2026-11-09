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
    public class SpecialtiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SpecialtiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب جميع التخصصات (متاح للجميع: مريض وزائر وأدمن)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // استعلام LINQ: تحويل كل Specialty إلى SpecialtyDto وحساب عدد أطبائها
            var specialties = await _context.Specialties
                .Select(s => new SpecialtyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DoctorsCount = s.Doctors.Count(d => d.User.IsActive)
                })
                .ToListAsync();

            return Ok(specialties);
        }

        // 2. جلب تخصص محدد بواسطة الـ Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var specialty = await _context.Specialties
                .Include(s => s.Doctors)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (specialty == null)
                return NotFound(new { message = "التخصص غير موجود" });

            return Ok(new SpecialtyDto
            {
                Id = specialty.Id,
                Name = specialty.Name,
                Description = specialty.Description,
                DoctorsCount = specialty.Doctors.Count(d => d.User.IsActive)
            });
        }

        // 3. إضافة تخصص جديد (خاص بالأدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SpecialtyCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool exists = await _context.Specialties.AnyAsync(s => s.Name.ToLower() == dto.Name.Trim().ToLower());
            if (exists)
                return Conflict(new { message = "هذا التخصص مضاف بالفعل" });

            var specialty = new Specialty
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim()
            };

            _context.Specialties.Add(specialty);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, specialty);
        }

        // 4. تعديل تخصص (خاص بالأدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SpecialtyCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null)
                return NotFound(new { message = "التخصص غير موجود" });

            bool duplicate = await _context.Specialties.AnyAsync(s => s.Name.ToLower() == dto.Name.Trim().ToLower() && s.Id != id);
            if (duplicate)
                return Conflict(new { message = "يوجد تخصص آخر يحمل نفس الاسم" });

            specialty.Name = dto.Name.Trim();
            specialty.Description = dto.Description?.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تعديل التخصص بنجاح" });
        }

        // 5. حذف تخصص (خاص بالأدمن فقط مع حماية الارتباط)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var specialty = await _context.Specialties
                .Include(s => s.Doctors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (specialty == null)
                return NotFound(new { message = "التخصص غير موجود" });

            // قاعدة مهمة: منع الحذف إذا كان هناك أطباء ينتمون لهذا التخصص
            if (specialty.Doctors.Any())
            {
                return BadRequest(new { message = "لا يمكن حذف هذا التخصص لأنه مرتبط بأطباء حاليين. يرجى نقل الأطباء إلى تخصص آخر أولاً." });
            }

            _context.Specialties.Remove(specialty);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف التخصص بنجاح" });
        }
    }
}