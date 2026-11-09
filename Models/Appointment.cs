using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

      
        [Required]
        public DateTime AppointmentDate { get; set; }

       
        [Required]
        public TimeSpan TimeSlot { get; set; }

       
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}