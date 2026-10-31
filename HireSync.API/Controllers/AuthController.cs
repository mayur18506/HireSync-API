using HireSync.API.Data;
using HireSync.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace HireSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email pehle se registered hai!" });

            // Password hashing using BCrypt (NuGet: BCrypt.Net-Next)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                AuthProvider = "Local"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Registration successful!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email ya password!" });

            string token = GenerateJwtToken(user);
            return Ok(new { token, user = new { user.Id, user.FullName, user.Email } });
        }

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    AuthProvider = dto.Provider,
                    PasswordHash = null
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            string token = GenerateJwtToken(user);
            return Ok(new { token, user = new { user.Id, user.FullName, user.Email } });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return BadRequest(new { message = "Ye email registered nahi hai!" });

            string token = Random.Shared.Next(100000, 999999).ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(15);
            await _db.SaveChangesAsync();

            try
            {
                await SendOtpEmailAsync(user.Email, user.FullName, token);
                return Ok(new { message = "📩 6-digit Reset OTP aapke email inbox par bhej diya gaya hai!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Email send nahi ho paya! App Password check karein: {ex.Message}" });
            }
        }

        private async Task SendOtpEmailAsync(string toEmail, string fullName, string otp)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var appPassword = _config["EmailSettings:AppPassword"]?.Replace(" ", "");

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword))
                throw new Exception("appsettings.json mein SenderEmail ya AppPassword set nahi hai.");

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, appPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "HireSync AI Security"),
                Subject = "HireSync AI - Password Reset OTP",
                IsBodyHtml = true,
                Body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 480px; margin: auto; padding: 24px; background: #0f172a; color: #f8fafc; border-radius: 12px;'>
                <h2 style='color: #818cf8; margin-top: 0;'>HireSync AI</h2>
                <p>Hi <strong>{fullName}</strong>,</p>
                <p>Use the 6-digit OTP below to reset your HireSync AI password. Valid for <strong>15 minutes</strong>:</p>
                <div style='background: #1e293b; padding: 16px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #34d399; border-radius: 8px; margin: 20px 0;'>
                    {otp}
                </div>
                <p style='font-size: 12px; color: #94a3b8;'>If you did not request a password reset, please ignore this email.</p>
            </div>"
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.ResetToken == dto.Token);
            if (user == null || user.ResetTokenExpiry < DateTime.Now)
                return BadRequest(new { message = "Invalid ya expired reset token!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password successfully reset ho gaya!" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "SuperSecretKeyForHireSyncApp2026SecureToken!";
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}