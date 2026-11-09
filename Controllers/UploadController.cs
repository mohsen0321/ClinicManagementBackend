using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clinicsystembackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // يتطلب تسجيل الدخول
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "يرجى اختيار صورة صالحة للرفع" });

            // أقصى حجم مسموح به: 2 ميجابايت (2 * 1024 * 1024 بايت)
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "حجم الصورة كبير جداً، أقصى حجم مسموح به هو 2 ميجابايت" });

            // التحقق من امتداد الملف
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "نوع الملف غير مدعوم. الأنواع المسموحة فقط: JPG, JPEG, PNG, WEBP" });

            // تحديد مسار مجلد uploads داخل wwwroot
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // توليد اسم فريد وآمن للملف لمنع التكرار
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // حفظ الملف فعلياً
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // إرجاع الرابط النسبي الذي يمكن للمتصفح عرضه مباشرة
            var fileUrl = $"/uploads/{uniqueFileName}";
            return Ok(new { url = fileUrl, message = "تم رفع الصورة بنجاح" });
        }
    }
}