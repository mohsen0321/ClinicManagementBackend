using clinicsystembackend.Data;
using clinicsystembackend.DTOs;
using clinicsystembackend.Models;
using clinicsystembackend.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
namespace clinicsystembackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration; // <-- 1. أضف هذا المتغير
        public AuthController(ApplicationDbContext context, JwtService jwtService, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration; // <-- حفظه هنا
        }

        // 1. تسجيل مريض جديد
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // أ. التحقق من عدم تكرار البريد الإلكتروني باستخدام LINQ
            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (emailExists)
                return Conflict(new { message = "البريد الإلكتروني مسجل بالفعل لمستخدم آخر" });

            // ب. التحقق من عدم تكرار رقم الهاتف
            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (phoneExists)
                return Conflict(new { message = "رقم الهاتف مسجل بالفعل لمستخدم آخر" });

            // ج. تشفير كلمة المرور باستخدام BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // د. إنشاء كائن المستخدم (ملاحظة أمنية: دائماً الرتبة هي Patient عند التسجيل الذاتي)
            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = passwordHash,
                Role = "Patient", // محمي من التلاعب
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // هـ. إنشاء سجل للمريض مرتبط بهذا المستخدم
            var patient = new Patient
            {
                Id = user.Id
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // و. توليد التوكن مباشرة ليسجل دخوله تلقائياً
            var (token, expiresAt) = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token,
                ExpiresAt = expiresAt
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }

        // 2. تسجيل الدخول (بالإيميل أو برقم الهاتف)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var searchInput = dto.EmailOrPhone.Trim().ToLower();

            // البحث عن المستخدم سواء أدخل بريده الإلكتروني أو رقم هاتفه
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == searchInput || u.PhoneNumber == searchInput);

            if (user == null)
                return Unauthorized(new { message = "البريد الإلكتروني / رقم الهاتف أو كلمة المرور غير صحيحة" });

            // فحص هل الحساب مفعل أم معطل بواسطة الإدارة
            if (!user.IsActive)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "تم تعطيل هذا الحساب، يرجى مراجعة إدارة العيادة" });

            // التحقق من صحة كلمة المرور عبر BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "البريد الإلكتروني / رقم الهاتف أو كلمة المرور غير صحيحة" });

            // توليد التوكن
            var (token, expiresAt) = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }

        // 3. جلب بيانات الملف الشخصي للمستخدم الحالي
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            // استخراج معرف المستخدم من الـ Claims الموجودة في التوكن
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.ProfilePictureUrl,
                DateOfBirth = user.Patient?.DateOfBirth,
                Gender = user.Patient?.Gender,
                Address = user.Patient?.Address
            });
        }

        // 4. تعديل الملف الشخصي
        // 4. تعديل الملف الشخصي
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            // التأكد أن الإيميل الجديد غير مستخدم من مستخدم آخر
            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != userId);
            if (emailExists)
                return Conflict(new { message = "هذا البريد الإلكتروني مسجل لمستخدم آخر بالفعل" });

            // التأكد أن رقم الهاتف غير مسجل لمستخدم آخر
            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber.Trim() && u.Id != userId);
            if (phoneExists)
                return Conflict(new { message = "رقم الهاتف مسجل لمستخدم آخر بالفعل" });

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Email = dto.Email.Trim().ToLower();
            user.PhoneNumber = dto.PhoneNumber.Trim();

            // حفظ رابط الصورة الجديد (أو null إذا حذف الصورة)
            user.ProfilePictureUrl = dto.ProfilePictureUrl;

            // إذا كان المستخدم مريضاً، نعدل بياناته الإضافية
            if (user.Role == "Patient")
            {
                if (user.Patient == null)
                {
                    user.Patient = new Patient { Id = user.Id };
                    _context.Patients.Add(user.Patient);
                }
                user.Patient.DateOfBirth = dto.DateOfBirth;
                user.Patient.Gender = dto.Gender;
                user.Patient.Address = dto.Address;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تحديث البيانات بنجاح", profilePictureUrl = user.ProfilePictureUrl });
        }

        // 5. تغيير كلمة المرور
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            // التأكد من صحة كلمة المرور القديمة
            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
                return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة" });

            // تشفير وتعيين كلمة المرور الجديدة
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
        }
        // 6. طلب كود OTP لنسيان كلمة المرور
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (user == null)
            {
                // لأسباب أمنية، نرجع نفس رسالة النجاح حتى لا يعرف المخترق هل الإيميل موجود أم لا
                return Ok(new { message = "إذا كان هذا البريد مسجلاً لدينا، فقد تم إرسال رمز التحقق إليه." });
            }

            // توليد كود عشوائي من 6 أرقام
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // حفظ الكود مع وقت صلاحية 15 دقيقة
            user.PasswordResetOtp = otpCode;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            // إرسال الإيميل
            await _emailService.SendOtpEmailAsync(user.Email, otpCode, $"{user.FirstName} {user.LastName}");

            return Ok(new { message = "تم إرسال رمز التحقق (OTP) إلى بريدك الإلكتروني بنجاح." });
        }

        // 7. التحقق من كود OTP وتعيين كلمة المرور الجديدة
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (user == null)
                return BadRequest(new { message = "البيانات المدخلة غير صحيحة" });

            // فحص صحة الرمز
            if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtp != dto.Otp.Trim())
                return BadRequest(new { message = "رمز التحقق (OTP) غير صحيح" });

            // فحص انتهاء الوقت (15 دقيقة)
            if (!user.PasswordResetOtpExpiry.HasValue || user.PasswordResetOtpExpiry.Value < DateTime.UtcNow)
                return BadRequest(new { message = "انتهت صلاحية رمز التحقق، يرجى طلب رمز جديد" });

            // تشفير وتعيين كلمة المرور الجديدة
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // تصفير الرمز لمنع استخدامه مرة أخرى
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح! يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة." });
        }
        // 8. تسجيل الدخول أو إنشاء حساب مريض جديد باستخدام Google
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            GoogleJsonWebSignature.Payload payload;
            try
            {
                // 1. التحقق من صحة التوكن القادم من Google
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    // التحقق من أن التوكن صادر لتطبيقنا
                    Audience = new[] { _configuration["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "توكن Google غير صالح أو منتهي الصلاحية: " + ex.Message });
            }
            var email = payload.Email.ToLower().Trim();
            // 2. فحص هل المستخدم مسجل لدينا مسبقاً؟
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user != null)
            {
                // المستخدم موجود بالفعل
                if (!user.IsActive)
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "تم تعطيل هذا الحساب من قِبل الإدارة" });
                // إذا لم يكن لديه صورة ولديه صورة في حساب Google، نحدث صورته
                if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.ProfilePictureUrl = payload.Picture;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // 3. مستخدم جديد لأول مرة: ننشئ له حساب مريض تلقائياً وبأمان تام
                user = new User
                {
                    FirstName = !string.IsNullOrWhiteSpace(payload.GivenName) ? payload.GivenName : "مريض",
                    LastName = !string.IsNullOrWhiteSpace(payload.FamilyName) ? payload.FamilyName : "جديد",
                    Email = email,
                    PhoneNumber = "0000000000", // قيمة افتراضية يمكنه تعديلها لاحقاً من بروفايله
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // كلمة مرور عشوائية مشفرة
                    Role = "Patient", // تلقائياً مريض
                    ProfilePictureUrl = payload.Picture,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                // إنشاء سجل المريض
                var patient = new Patient { Id = user.Id };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
            // 4. توليد JWT Token الخاص بنظامنا
            var (token, expiresAt) = _jwtService.GenerateToken(user);
            var response = new AuthResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token,
                ExpiresAt = expiresAt
            };
            return Ok(response);
        }

    }
}