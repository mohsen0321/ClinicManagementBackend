using System.ComponentModel.DataAnnotations;

namespace clinicsystembackend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

     
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Patient";

       
        public string? ProfilePictureUrl { get; set; }

       
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
        public bool IsActive { get; set; } = true;

     
        public Patient? Patient { get; set; }

       
        public Doctor? Doctor { get; set; }
   
        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
    }
}