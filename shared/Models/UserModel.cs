namespace Shared.Models
{
    public class UserModel
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // For frontend login
        public string Password { get; set; } // Plain password input

        // For backend storage
        public string PasswordHash { get; set; } // Hashed password stored in DB

        // Additional Properties Required by Backend
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}