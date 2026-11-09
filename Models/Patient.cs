using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinicsystembackend.Models
{
  
    public class Patient
    {
        [Key]
        [ForeignKey("User")]
        public int Id { get; set; } 

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

  
        public User User { get; set; } = null!;

       
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}