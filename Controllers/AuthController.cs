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
        private readonly IConfiguration _configuration; 
        public AuthController(ApplicationDbContext context, JwtService jwtService, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration; 
        }

     
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (emailExists)
                return Conflict(new { message = "البريد الإلكتروني مسجل بالفعل لمستخدم آخر" });

            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (phoneExists)
                return Conflict(new { message = "رقم الهاتف مسجل بالفعل لمستخدم آخر" });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = passwordHash,
                Role = "Patient", 
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patient = new Patient
            {
                Id = user.Id
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var searchInput = dto.EmailOrPhone.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == searchInput || u.PhoneNumber == searchInput);

            if (user == null)
                return Unauthorized(new { message = "البريد الإلكتروني / رقم الهاتف أو كلمة المرور غير صحيحة" });

            if (!user.IsActive)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "تم تعطيل هذا الحساب، يرجى مراجعة إدارة العيادة" });

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "البريد الإلكتروني / رقم الهاتف أو كلمة المرور غير صحيحة" });

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

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
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

            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != userId);
            if (emailExists)
                return Conflict(new { message = "هذا البريد الإلكتروني مسجل لمستخدم آخر بالفعل" });

            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber.Trim() && u.Id != userId);
            if (phoneExists)
                return Conflict(new { message = "رقم الهاتف مسجل لمستخدم آخر بالفعل" });

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Email = dto.Email.Trim().ToLower();
            user.PhoneNumber = dto.PhoneNumber.Trim();

            user.ProfilePictureUrl = dto.ProfilePictureUrl;

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

            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
                return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (user == null)
            {
                return Ok(new { message = "إذا كان هذا البريد مسجلاً لدينا، فقد تم إرسال رمز التحقق إليه." });
            }

            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            user.PasswordResetOtp = otpCode;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

      
            await _emailService.SendOtpEmailAsync(user.Email, otpCode, $"{user.FirstName} {user.LastName}");

            return Ok(new { message = "تم إرسال رمز التحقق (OTP) إلى بريدك الإلكتروني بنجاح." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (user == null)
                return BadRequest(new { message = "البيانات المدخلة غير صحيحة" });

    
            if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtp != dto.Otp.Trim())
                return BadRequest(new { message = "رمز التحقق (OTP) غير صحيح" });

        
            if (!user.PasswordResetOtpExpiry.HasValue || user.PasswordResetOtpExpiry.Value < DateTime.UtcNow)
                return BadRequest(new { message = "انتهت صلاحية رمز التحقق، يرجى طلب رمز جديد" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح! يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة." });
        }
       
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            GoogleJsonWebSignature.Payload payload;
            try
            {
            
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                
                    Audience = new[] { _configuration["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "توكن Google غير صالح أو منتهي الصلاحية: " + ex.Message });
            }
            var email = payload.Email.ToLower().Trim();
     
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user != null)
            {
            
                if (!user.IsActive)
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "تم تعطيل هذا الحساب من قِبل الإدارة" });
                if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.ProfilePictureUrl = payload.Picture;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
            
                user = new User
                {
                    FirstName = !string.IsNullOrWhiteSpace(payload.GivenName) ? payload.GivenName : "مريض",
                    LastName = !string.IsNullOrWhiteSpace(payload.FamilyName) ? payload.FamilyName : "جديد",
                    Email = email,
                    PhoneNumber = "0000000000",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), 
                    Role = "Patient",
                    ProfilePictureUrl = payload.Picture,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
             
                var patient = new Patient { Id = user.Id };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
         
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