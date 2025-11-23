using System.Net;
using System.Net.Mail;

namespace QuickClinique.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmail(string toEmail, string name, string verificationLink)
        {
            var subject = "Verify Your Email - QuickClinique";
            var body = $@"
                <h3>Hello {name},</h3>
                <p>Welcome to QuickClinique! Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
                <p>Or copy this link to your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>QuickClinique Team</p>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendVerificationCodeEmail(string toEmail, string name, string verificationCode)
        {
            var subject = "Your Email Verification Code - QuickClinique";
            var body = $@"
                <h3>Hello {name},</h3>
                <p>Welcome to QuickClinique! Please use the verification code below to verify your email address:</p>
                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                    <h2 style='color: #4ECDC4; font-size: 32px; letter-spacing: 5px; margin: 0;'>{verificationCode}</h2>
                </div>
                <p>Enter this code on the verification page to complete your registration.</p>
                <p>This code will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>QuickClinique Team</p>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmail(string toEmail, string name, string resetLink)
        {
            var subject = "Reset Your Password - QuickClinique";
            var body = $@"
                <h3>Hello {name},</h3>
                <p>You requested to reset your password. Click the link below to reset it:</p>
                <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>Or copy this link to your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour.</p>
                <br>
                <p>If you didn't request this, please ignore this email.</p>
                <p>Best regards,<br>QuickClinique Team</p>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // For development - just log the email
                Console.WriteLine($"=== EMAIL TO: {toEmail} ===");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {body}");
                Console.WriteLine("=== END EMAIL ===");

                // In production, uncomment and configure the code below with your SMTP settings
                /*
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                // In production, you might want to log this properly
            }
        }
    }
}