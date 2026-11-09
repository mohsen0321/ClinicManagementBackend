using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.DTOs
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google Token مطلوب")]
        public string IdToken { get; set; } = string.Empty;
    }
}