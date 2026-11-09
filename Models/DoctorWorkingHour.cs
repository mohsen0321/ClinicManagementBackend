using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{

    public class DoctorWorkingHour
    {
        [Key]
        public int Id { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

       
        public DayOfWeek DayOfWeek { get; set; }

  
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}