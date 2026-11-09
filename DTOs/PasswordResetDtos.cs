using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز التحقق OTP مطلوب"), StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يتكون من 6 أرقام")]
        public string Otp { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة"), MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب"), Compare("NewPassword", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}