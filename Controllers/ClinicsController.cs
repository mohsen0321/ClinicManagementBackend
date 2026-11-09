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
    public class ClinicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClinicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clinics = await _context.Clinics
                .Select(c => new ClinicDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Address = c.Address,
                    PhoneNumber = c.PhoneNumber,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    DoctorsCount = c.Doctors.Count(d => d.User.IsActive)
                })
                .ToListAsync();

            return Ok(clinics);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var clinic = await _context.Clinics
                .Include(c => c.Doctors)
                    .ThenInclude(d => d.User)
                .Include(c => c.Doctors)
                    .ThenInclude(d => d.Specialty)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clinic == null)
                return NotFound(new { message = "العيادة غير موجودة" });

            var result = new
            {
                clinic.Id,
                clinic.Name,
                clinic.Description,
                clinic.Address,
                clinic.PhoneNumber,
                clinic.ImageUrl,
                clinic.IsActive,
                Doctors = clinic.Doctors
                    .Where(d => d.User.IsActive)
                    .Select(d => new
                    {
                        d.Id,
                        FullName = $"{d.User.FirstName} {d.User.LastName}",
                        Specialty = d.Specialty.Name,
                        d.User.PhoneNumber,
                        d.User.ProfilePictureUrl
                    })
            };

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClinicCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clinic = new Clinic
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Address = dto.Address.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
            };

            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, clinic);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClinicCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
                return NotFound(new { message = "العيادة غير موجودة" });

            clinic.Name = dto.Name.Trim();
            clinic.Description = dto.Description?.Trim();
            clinic.Address = dto.Address.Trim();
            clinic.PhoneNumber = dto.PhoneNumber.Trim();
            clinic.ImageUrl = dto.ImageUrl;
            clinic.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تعديل بيانات العيادة بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
                return NotFound(new { message = "العيادة غير موجودة" });

            clinic.IsActive = !clinic.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = clinic.IsActive ? "تم تفعيل العيادة" : "تم تعطيل العيادة", isActive = clinic.IsActive });
        }
    }
}