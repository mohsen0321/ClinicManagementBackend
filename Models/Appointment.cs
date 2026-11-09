using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{
    // جدول الحجوزات يربط بين المريض والطبيب في تاريخ وساعة محددة
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // المريض صاحب الحجز
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        // الطبيب المراد الكشف عنده
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        // تاريخ اليوم المحجوز (مثال: 2026-11-10)
        [Required]
        public DateTime AppointmentDate { get; set; }

        // وقت الحجز (مثال: 10:30 صباحاً)
        [Required]
        public TimeSpan TimeSlot { get; set; }

        // حالة الحجز: "Pending", "Accepted", "Rejected", "Cancelled", "Completed"
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // سبب الرفض في حال رفض الأدمن للحجز
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // ملاحظات المريض الاختيارية عند الحجز (سبب الزيارة أو الأعراض)
        [MaxLength(500)]
        public string? Notes { get; set; }

        // تاريخ تقديم الحجز
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}