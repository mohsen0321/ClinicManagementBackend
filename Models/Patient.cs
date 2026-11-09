using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinicsystembackend.Models
{
    // جدول بيانات المريض، مرتبط 1:1 بجدول User
    public class Patient
    {
        [Key]
        [ForeignKey("User")]
        public int Id { get; set; } // سيكون نفس الـ Id الخاص بالمستخدم في جدول Users

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; } // Male أو Female

        [MaxLength(200)]
        public string? Address { get; set; }

        // الربط مع جدول User
        public User User { get; set; } = null!;

        // قائمة كل الحجوزات التي قام بها هذا المريض
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}