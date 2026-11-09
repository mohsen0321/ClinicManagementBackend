using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب"), MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأخير مطلوب"), MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب"), Phone]
        public string PhoneNumber { get; set; } = string.Empty;


        public string? ProfilePictureUrl { get; set; }

 
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة"), MinLength(6, ErrorMessage = "كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب"), Compare("NewPassword", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}