using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب"), MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأخير مطلوب"), MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب"), Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة"), MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 خانات")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب"), Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}