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

        public async Task SendAppointmentConfirmationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, int queueNumber)
        {
            var subject = "Appointment Confirmed - QuickClinique";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #007bff;'>Appointment Confirmed!</h2>
                    <h3>Hello {patientName},</h3>
                    <p>Your appointment has been confirmed. Here are the details:</p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Date:</strong> {appointmentDate}</p>
                        <p><strong>Time:</strong> {appointmentTime}</p>
                        <p><strong>Queue Number:</strong> #{queueNumber}</p>
                    </div>
                    <p>Please arrive on time for your appointment. You will be notified when it's your turn.</p>
                    <p>If you need to reschedule or cancel, please contact us as soon as possible.</p>
                    <br>
                    <p>Best regards,<br>QuickClinique Team</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendQueuePositionUpdateEmail(string toEmail, string patientName, int newPosition, int queueNumber)
        {
            var subject = "Queue Update - You've Moved Up! - QuickClinique";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>Queue Position Update</h2>
                    <h3>Hello {patientName},</h3>
                    <p>Good news! Your position in the queue has moved up.</p>
                    <div style='background-color: #d4edda; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                        <p style='margin: 0;'><strong>Your New Position:</strong> #{newPosition} in line</p>
                        <p style='margin: 10px 0 0 0;'><strong>Queue Number:</strong> #{queueNumber}</p>
                    </div>
                    <p>Please be ready for your appointment. You will be notified when it's your turn to be seen.</p>
                    <p>If you're not at the clinic yet, please make your way to the clinic as soon as possible.</p>
                    <br>
                    <p>Best regards,<br>QuickClinique Team</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAppointmentCompletedEmail(string toEmail, string patientName, string appointmentDate)
        {
            var subject = "Appointment Completed - QuickClinique";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #007bff;'>Appointment Completed</h2>
                    <h3>Hello {patientName},</h3>
                    <p>Your appointment on <strong>{appointmentDate}</strong> has been completed.</p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <p>Thank you for visiting QuickClinique. We hope you had a positive experience.</p>
                        <p>If you have any questions or concerns about your visit, please don't hesitate to contact us.</p>
                    </div>
                    <p>We look forward to serving you again in the future.</p>
                    <br>
                    <p>Best regards,<br>QuickClinique Team</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Read from environment variables first (for production), then fall back to configuration
                var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _configuration["EmailSettings:FromEmail"];
                var fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? _configuration["EmailSettings:FromName"];
                var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? _configuration["EmailSettings:SmtpPort"];
                var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? _configuration["EmailSettings:SmtpPassword"];

                // Validate configuration
                if (string.IsNullOrEmpty(fromEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] FromEmail is not configured");
                    return;
                }
                if (string.IsNullOrEmpty(smtpServer))
                {
                    Console.WriteLine("[EMAIL ERROR] SmtpServer is not configured");
                    return;
                }
                if (string.IsNullOrEmpty(smtpPassword))
                {
                    Console.WriteLine("[EMAIL ERROR] SmtpPassword is not configured");
                    return;
                }
                if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out int smtpPort))
                {
                    Console.WriteLine("[EMAIL ERROR] SmtpPort is invalid");
                    return;
                }

                Console.WriteLine($"[EMAIL] Attempting to send email to: {toEmail}");
                Console.WriteLine($"[EMAIL] Using SMTP server: {smtpServer}:{smtpPort}");
                Console.WriteLine($"[EMAIL] From: {fromEmail} ({fromName})");

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
                    EnableSsl = true,
                    Timeout = 30000 // 30 seconds timeout
                };

                await smtpClient.SendMailAsync(message);
                Console.WriteLine($"[EMAIL SUCCESS] Email sent successfully to {toEmail}");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"[EMAIL ERROR] SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"[EMAIL ERROR] Status Code: {smtpEx.StatusCode}");
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine($"[EMAIL ERROR] Inner Exception: {smtpEx.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] Failed to send email: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[EMAIL ERROR] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EMAIL ERROR] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}