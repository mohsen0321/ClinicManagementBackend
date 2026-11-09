using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace clinicsystembackend.Models
{
    // جدول العيادات والمراكز
    public class Clinic
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // قائمة الأطباء المتاحين في هذه العيادة
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}