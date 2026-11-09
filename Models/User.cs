using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{
    // جدول المستخدمين العام لجميع من يدخل النظام (مرضى أو أطباء/مسؤولين)
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        // كلمة المرور مشفرة بـ BCrypt ولن نخزن كلمة المرور الحقيقية أبداً
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // دور المستخدم: "Admin" أو "Patient"
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Patient";

        // رابط الصورة الشخصية (اختياري)
        public string? ProfilePictureUrl { get; set; }

        // تاريخ إنشاء الحساب
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // هل الحساب مفعّل؟ (يستخدمه الأدمن لتعطيل الحسابات عند الحاجة)
        public bool IsActive { get; set; } = true;

        // علاقات الربط:
        // إذا كان المستخدم مريضاً، يرتبط بهذا الكائن
        public Patient? Patient { get; set; }

        // إذا كان المستخدم طبيباً، يرتبط بهذا الكائن
        public Doctor? Doctor { get; set; }
        // أضف هذين السطرين داخل كلاس User في Models/User.cs:
        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
    }
}