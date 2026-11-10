using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class User
    {
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;   // UserRole enum value

        public bool IsFirstLogin { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(64)]
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
    }
}
