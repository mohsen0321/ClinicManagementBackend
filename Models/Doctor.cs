using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinicsystembackend.Models
{
    // جدول الأطباء: مرتبط بحساب المستخدم وبقسم التخصص وبالعيادة
    public class Doctor
    {
        [Key]
        [ForeignKey("User")]
        public int Id { get; set; } // سيكون نفس Id المستخدم في جدول Users

        [MaxLength(1000)]
        public string? Biography { get; set; } // نبذة تعريفية عن الطبيب وخبرته

        // ربط الطبيب بالتخصص (Foreign Key)
        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; } = null!;

        // ربط الطبيب بالعيادة (Foreign Key)
        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        // مدة الكشف بالدقائق (افتراضياً 30 دقيقة)
        public int AppointmentDurationMinutes { get; set; } = 30;

        // الربط مع جدول User
        public User User { get; set; } = null!;

        // مواعيد وساعات عمل الطبيب
        public ICollection<DoctorWorkingHour> WorkingHours { get; set; } = new List<DoctorWorkingHour>();

        // قائمة الحجوزات الخاصة بهذا الطبيب
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}