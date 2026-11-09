using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class ClinicCreateUpdateDto
    {
        [Required(ErrorMessage = "اسم العيادة مطلوب"), MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "عنوان العيادة مطلوب"), MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم هاتف العيادة مطلوب"), Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }


    public class ClinicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int DoctorsCount { get; set; }
    }
}