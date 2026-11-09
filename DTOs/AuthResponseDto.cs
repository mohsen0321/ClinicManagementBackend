namespace clinicsystembackend.DTOs
{
    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Token { get; set; } = string.Empty; 
        public DateTime ExpiresAt { get; set; }
    }
}