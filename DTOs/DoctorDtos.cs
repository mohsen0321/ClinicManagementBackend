using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    // ساعات عمل الطبيب
    public class WorkingHourDto
    {
        public DayOfWeek DayOfWeek { get; set; } // 0=Sunday, 1=Monday ... 6=Saturday
        public TimeSpan StartTime { get; set; } // مثال 09:00:00
        public TimeSpan EndTime { get; set; }   // مثال 17:00:00
        public bool IsAvailable { get; set; } = true;
    }

    // لإضافة طبيب جديد بواسطة الأدمن
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

        // قائمة ساعات العمل الأسبوعية
        public List<WorkingHourDto> WorkingHours { get; set; } = new();
    }

    // لتعديل بيانات الطبيب
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

    // لعرض الطبيب في القوائم والبحث
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