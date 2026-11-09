using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{
    // جدول لتخزين مواعيد العمل الأسبوعية لكل طبيب
    public class DoctorWorkingHour
    {
        [Key]
        public int Id { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        // يوم العمل: من 0 إلى 6 (0 = الأحد، 1 = الإثنين، ... 6 = السبت)
        public DayOfWeek DayOfWeek { get; set; }

        // وقت بداية العمل (مثلاً 09:00:00)
        [Required]
        public TimeSpan StartTime { get; set; }

        // وقت نهاية العمل (مثلاً 17:00:00)
        [Required]
        public TimeSpan EndTime { get; set; }

        // هل هذا اليوم متاح أم عطلة للطبيب؟
        public bool IsAvailable { get; set; } = true;
    }
}