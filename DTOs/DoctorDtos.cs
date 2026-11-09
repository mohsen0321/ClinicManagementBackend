using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class WorkingHourDto
    {
        public DayOfWeek DayOfWeek { get; set; } 
        public TimeSpan StartTime { get; set; } 
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class DoctorCreateDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [Required]
        public int SpecialtyId { get; set; }

        [Required]
        public int ClinicId { get; set; }

        public int AppointmentDurationMinutes { get; set; } = 30;

        public List<WorkingHourDto> WorkingHours { get; set; } = new();
    }

    public class DoctorUpdateDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int SpecialtyId { get; set; }
        public int ClinicId { get; set; }
        public int AppointmentDurationMinutes { get; set; } = 30;
        public bool IsActive { get; set; } = true;

        public List<WorkingHourDto> WorkingHours { get; set; } = new();
    }

    public class DoctorListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Biography { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
        public int SpecialtyId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public int ClinicId { get; set; }
        public int AppointmentDurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }
}