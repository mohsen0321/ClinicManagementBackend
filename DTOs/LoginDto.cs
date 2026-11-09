using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    // تسجيل الدخول: المريض أو الطبيب يمكنه إدخال البريد الإلكتروني أو رقم الهاتف
    public class LoginDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string Password { get; set; } = string.Empty;
    }
}