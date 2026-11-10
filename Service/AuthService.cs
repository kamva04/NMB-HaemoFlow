using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.Domain;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NMB_HLabSys.Services
{
	public class AuthService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly EmailService _email;

		private const string SESSION_USERID = "UserId";
		private const string SESSION_ROLE = "Role";
		private const string SESSION_EMAIL = "Email";
		private const string SESSION_FIRSTLOGIN = "IsFirstLogin";

		public AuthService(
			ApplicationDbContext context,
			IHttpContextAccessor httpContextAccessor,
			EmailService email)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_email = email;
		}

		// HASH PASSWORD
		public string HashPassword(string plainText)
			=> BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);

		public bool VerifyPassword(string plainText, string hash)
			=> BCrypt.Net.BCrypt.Verify(plainText, hash);

		// LOGIN
		public (bool success, string? errorMessage) Login(string email, string password)
		{
			var user = _context.Users
				.FirstOrDefault(u => u.Email == email);

			if (user == null)
				return (false, "Invalid email or password.");

			if (!VerifyPassword(password, user.PasswordHash))
				return (false, "Invalid email or password.");

			if (user.Role.Equals("Technician", StringComparison.OrdinalIgnoreCase) &&
                !_context.Technicians.Any(t => t.UserId == user.UserId && !t.IsDeleted))
                return (false, "This technician account is inactive. Please contact your laboratory manager.");
			if (user.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) &&
				!_context.Doctors.Any(d => d.UserId == user.UserId && !d.IsDeleted))
				return (false, "This doctor account is inactive. Please contact your laboratory manager.");
            var session = _httpContextAccessor.HttpContext!.Session;

			session.SetInt32(SESSION_USERID, user.UserId);
			session.SetString(SESSION_ROLE, user.Role);
			session.SetString(SESSION_EMAIL, user.Email);
			session.SetString(SESSION_FIRSTLOGIN, user.IsFirstLogin.ToString());

			return (true, null);
		}

		// LOGOUT
		public void Logout()
		{
			_httpContextAccessor.HttpContext!.Session.Clear();
		}

		// SESSION HELPERS
		public int? GetCurrentUserId()
		{
			return _httpContextAccessor.HttpContext?.Session.GetInt32(SESSION_USERID);
		}

		public string? GetCurrentRole()
		{
			return _httpContextAccessor.HttpContext?.Session.GetString(SESSION_ROLE);
		}

		public string? GetCurrentEmail()
		{
			return _httpContextAccessor.HttpContext?.Session.GetString(SESSION_EMAIL);
		}

		public bool IsFirstLogin()
		{
			return _httpContextAccessor.HttpContext?.Session.GetString(SESSION_FIRSTLOGIN) == "True";
		}

		public bool IsLoggedIn()
		{
			var userId = GetCurrentUserId();
            if (!userId.HasValue) return false;
            if (IsInRole("Technician") && !_context.Technicians.Any(t => t.UserId == userId.Value && !t.IsDeleted))
            {
                Logout();
                return false;
            }
			if (IsInRole("Doctor") && !_context.Doctors.Any(d => d.UserId == userId.Value && !d.IsDeleted))
			{
				Logout();
				return false;
			}
            return true;
		}

		public bool IsInRole(string role)
		{
			return GetCurrentRole()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
		}


		//------------------------------------------------------------------------------------------------------------//
		//Added 


		public string? CreatePasswordResetToken(string email)
		{
			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null)
				return null;

			var tokenBytes = RandomNumberGenerator.GetBytes(32);
			var token = Convert.ToBase64String(tokenBytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');

			user.PasswordResetTokenHash = Convert.ToHexString(
				SHA256.HashData(Encoding.UTF8.GetBytes(token)));
			user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
			_context.SaveChanges();

			return token;
		}

		public void SendPasswordResetEmail(string email, string resetLink)
		{
			_email.Send(
				email,
				"Password Reset",
				$@"
        <p>Click the link below to reset your password:</p>

        <p><a href='{resetLink}'>Reset Password</a></p>

        <p>This link expires in one hour and can only be used once.</p>

        <p>If you did not request a password reset, please ignore this email.</p>
    "); 
		}

		//added ed here

		//------------------------------------------------------------------------------------------------------------//

		// Added Code For Reset Passward 
		public (bool ok, string? err) ResetPassword(string email, string token, string newPassword)
		{
			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null ||
				string.IsNullOrEmpty(user.PasswordResetTokenHash) ||
				!user.PasswordResetTokenExpiresAt.HasValue ||
				user.PasswordResetTokenExpiresAt.Value <= DateTime.UtcNow)
				return (false, "This password reset link is invalid or has expired. Request a new one.");

			byte[] providedHash;
			try
			{
				providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
			}
			catch (ArgumentException)
			{
				return (false, "This password reset link is invalid or has expired. Request a new one.");
			}

			byte[] storedHash;
			try
			{
				storedHash = Convert.FromHexString(user.PasswordResetTokenHash);
			}
			catch (FormatException)
			{
				return (false, "This password reset link is invalid or has expired. Request a new one.");
			}

			if (storedHash.Length != providedHash.Length ||
				!CryptographicOperations.FixedTimeEquals(storedHash, providedHash))
				return (false, "This password reset link is invalid or has expired. Request a new one.");

			user.PasswordHash = HashPassword(newPassword);
			user.IsFirstLogin = false;
			user.PasswordResetTokenHash = null;
			user.PasswordResetTokenExpiresAt = null;

			_context.SaveChanges();

			return (true, null);
		}// Added code for Reset Password End here 


//--------------------------------------------------------------------------------------------------------------------//

		// CHANGE PASSWORD
		public (bool ok, string? err) ChangePassword(int userId, string currentPassword, string newPassword)
		{
			var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

			if (user == null)
				return (false, "User not found.");

			if (!VerifyPassword(currentPassword, user.PasswordHash))
				return (false, "Current password is incorrect.");

			user.PasswordHash = HashPassword(newPassword);
			user.IsFirstLogin = false;

			_context.SaveChanges();

			_httpContextAccessor.HttpContext!.Session.SetString(SESSION_FIRSTLOGIN, "False");

			return (true, null);
		}
//------------------------------------------------------------------------------------------------------------------//
		// CHANGE EMAIL
		public (bool ok, string? err) ChangeEmail(int userId, string currentPassword, string newEmail)
		{
			var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

			if (user == null)
				return (false, "User not found.");

			if (!VerifyPassword(currentPassword, user.PasswordHash))
				return (false, "Current password is incorrect.");

			bool emailExists = _context.Users
				.Any(u => u.Email == newEmail && u.UserId != userId);

			if (emailExists)
				return (false, "That email is already in use.");

			user.Email = newEmail;

			_context.SaveChanges();

			_httpContextAccessor.HttpContext!.Session.SetString(SESSION_EMAIL, newEmail);

			return (true, null);
		}

		// GENERATE TEMPORARY PASSWORD
		public string GenerateTemporaryPassword()
		{
			const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string lower = "abcdefghijklmnopqrstuvwxyz";
			const string digits = "0123456789";
			const string special = "!@#$%^&*";
			const string all = upper + lower + digits + special;

			var bytes = RandomNumberGenerator.GetBytes(12);

			var chars = new char[12];

			chars[0] = upper[bytes[0] % upper.Length];
			chars[1] = digits[bytes[1] % digits.Length];
			chars[2] = special[bytes[2] % special.Length];

			for (int i = 3; i < chars.Length; i++)
			{
				chars[i] = all[bytes[i] % all.Length];
			}

			for (int i = chars.Length - 1; i > 0; i--)
			{
				int j = RandomNumberGenerator.GetInt32(i + 1);
				(chars[i], chars[j]) = (chars[j], chars[i]);
			}

			return new string(chars);
		}
	}
}