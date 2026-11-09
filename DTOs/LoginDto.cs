using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string Password { get; set; } = string.Empty;
    }
}