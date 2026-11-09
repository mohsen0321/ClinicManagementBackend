using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    // طلب حجز موعد جديد
    public class BookAppointmentDto
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; } 

        [Required]
        public TimeSpan TimeSlot { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

  
    public class RescheduleAppointmentDto
    {
        [Required]
        public DateTime NewDate { get; set; }

        [Required]
        public TimeSpan NewTimeSlot { get; set; }
    }

  
    public class RejectAppointmentDto
    {
        [Required(ErrorMessage = "يرجى توضيح سبب الرفض"), MaxLength(500)]
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class AppointmentDetailDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;

        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorPicture { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;

        public DateTime AppointmentDate { get; set; }
        public TimeSpan TimeSlot { get; set; }
        public string Status { get; set; } = string.Empty; 
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}