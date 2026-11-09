using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    // لاستقبال بيانات إضافة أو تعديل تخصص
    public class SpecialtyCreateUpdateDto
    {
        [Required(ErrorMessage = "اسم التخصص مطلوب"), MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    // لعرض التخصص مع عدد الأطباء فيه
    public class SpecialtyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DoctorsCount { get; set; }
    }
}