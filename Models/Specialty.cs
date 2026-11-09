using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace clinicsystembackend.Models
{
    // جدول التخصصات الطبية مثل (طب وجراحة العيون، طب الأطفال، الأسنان...)
    public class Specialty
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // قائمة الأطباء التابعين لهذا التخصص
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}