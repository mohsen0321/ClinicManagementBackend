using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinicsystembackend.Models
{

    public class Doctor
    {
        [Key]
        [ForeignKey("User")]
        public int Id { get; set; } 

        [MaxLength(1000)]
        public string? Biography { get; set; } 

        
        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; } = null!;

        
        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

       
        public int AppointmentDurationMinutes { get; set; } = 30;

  
        public User User { get; set; } = null!;

        
        public ICollection<DoctorWorkingHour> WorkingHours { get; set; } = new List<DoctorWorkingHour>();

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}