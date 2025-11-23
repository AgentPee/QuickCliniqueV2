using SendGrid;
using SendGrid.Helpers.Mail;

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

        public async Task SendAppointmentCancellationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, string? reason = null)
        {
            var subject = "Appointment Cancelled - QuickClinique";
            var reasonSection = !string.IsNullOrWhiteSpace(reason) 
                ? $@"
                    <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #ffc107;'>
                        <p style='margin: 0;'><strong>Reason:</strong> {reason}</p>
                    </div>"
                : "";
            
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #dc3545;'>Appointment Cancelled</h2>
                    <h3>Hello {patientName},</h3>
                    <p>We regret to inform you that your appointment has been cancelled.</p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Date:</strong> {appointmentDate}</p>
                        <p><strong>Time:</strong> {appointmentTime}</p>
                    </div>
                    {reasonSection}
                    <p>If you need to reschedule your appointment, please log in to your account and book a new appointment, or contact us for assistance.</p>
                    <p>We apologize for any inconvenience this may cause.</p>
                    <br>
                    <p>Best regards,<br>QuickClinique Team</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendEmailAsync called for: {toEmail}");
                Console.WriteLine($"[EMAIL] Subject: {subject}");
                
                // Read SendGrid API key from environment variable or configuration
                // Support both SENDGRID_API_KEY (new) and SMTP_PASSWORD (legacy) for backward compatibility
                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") 
                    ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                    ?? _configuration["EmailSettings:SmtpPassword"];

                // Read sender information
                var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _configuration["EmailSettings:FromEmail"];
                var fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? _configuration["EmailSettings:FromName"];
                var replyToEmail = Environment.GetEnvironmentVariable("EMAIL_REPLY_TO") ?? fromEmail;

                Console.WriteLine($"[EMAIL DEBUG] API Key found: {!string.IsNullOrEmpty(apiKey)}");
                Console.WriteLine($"[EMAIL DEBUG] FromEmail found: {!string.IsNullOrEmpty(fromEmail)}");
                Console.WriteLine($"[EMAIL DEBUG] FromEmail value: {fromEmail ?? "NULL"}");
                Console.WriteLine($"[EMAIL DEBUG] FromName value: {fromName ?? "NULL"}");

                // Validate configuration
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("[EMAIL ERROR] SendGrid API key is not configured");
                    Console.WriteLine("[EMAIL ERROR] For Railway: Set SENDGRID_API_KEY or SMTP_PASSWORD environment variable");
                    Console.WriteLine("[EMAIL ERROR] For Local: Set SENDGRID_API_KEY env var or use appsettings.Development.json");
                    Console.WriteLine("[EMAIL ERROR] Current environment: " + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"));
                    Console.WriteLine("[EMAIL ERROR] Checking configuration keys:");
                    Console.WriteLine($"[EMAIL ERROR]   SENDGRID_API_KEY: {Environment.GetEnvironmentVariable("SENDGRID_API_KEY") != null}");
                    Console.WriteLine($"[EMAIL ERROR]   SMTP_PASSWORD: {Environment.GetEnvironmentVariable("SMTP_PASSWORD") != null}");
                    Console.WriteLine($"[EMAIL ERROR]   EmailSettings:SmtpPassword: {_configuration["EmailSettings:SmtpPassword"] != null}");
                    Console.WriteLine($"[EMAIL ERROR]   EmailSettings:SmtpPassword value length: {(_configuration["EmailSettings:SmtpPassword"]?.Length ?? 0)}");
                    return; // Return instead of throwing to avoid breaking the application
                }

                if (string.IsNullOrEmpty(fromEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] FromEmail is not configured");
                    Console.WriteLine("[EMAIL ERROR] Check environment variable EMAIL_FROM or appsettings.json");
                    return; // Return instead of throwing to avoid breaking the application
                }

                Console.WriteLine($"[EMAIL] Attempting to send email to: {toEmail}");
                Console.WriteLine($"[EMAIL] Using SendGrid Web API");
                Console.WriteLine($"[EMAIL] From: {fromEmail} ({fromName})");
                Console.WriteLine($"[EMAIL] API Key length: {(apiKey?.Length ?? 0)} characters");

                // Create SendGrid client
                var client = new SendGridClient(apiKey);

                // Convert HTML to plain text for better deliverability
                var plainTextBody = ConvertHtmlToPlainText(body);

                // Create email message with both HTML and plain text
                var msg = new SendGridMessage
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = body,
                    PlainTextContent = plainTextBody
                };
                msg.AddTo(new EmailAddress(toEmail));

                // Set Reply-To header
                msg.SetReplyTo(new EmailAddress(replyToEmail, fromName));

                // Add important headers to improve deliverability and reduce spam
                msg.AddHeader("X-Entity-Ref-ID", Guid.NewGuid().ToString());
                msg.SetClickTracking(false, false); // Disable click tracking to avoid spam filters
                msg.SetOpenTracking(false); // Disable open tracking
                
                // Set email category for better tracking in SendGrid
                msg.AddCategory("transactional");

                // Add List-Unsubscribe header (best practice even for transactional emails)
                var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") 
                    ?? _configuration["BaseUrl"] 
                    ?? "https://your-app-name.up.railway.app";
                msg.AddHeader("List-Unsubscribe", $"<{baseUrl}/unsubscribe?email={Uri.EscapeDataString(toEmail)}>");
                msg.AddHeader("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

                // Send email
                var startTime = DateTime.Now;
                Console.WriteLine($"[EMAIL] Sending via SendGrid API...");
                
                var response = await client.SendEmailAsync(msg);
                
                var elapsed = (DateTime.Now - startTime).TotalSeconds;

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[EMAIL SUCCESS] Email sent successfully to {toEmail} in {elapsed:F2} seconds");
                    Console.WriteLine($"[EMAIL SUCCESS] Status Code: {response.StatusCode}");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"[EMAIL ERROR] SendGrid API Error: Status Code {response.StatusCode}");
                    Console.WriteLine($"[EMAIL ERROR] Response: {responseBody}");
                    throw new Exception($"SendGrid API returned status code {response.StatusCode}: {responseBody}");
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
                    Console.WriteLine($"[EMAIL ERROR] Inner Exception Type: {ex.InnerException.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Converts HTML content to plain text for email deliverability
        /// </summary>
        private string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags and decode HTML entities
            var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
            plainText = System.Net.WebUtility.HtmlDecode(plainText);
            
            // Clean up whitespace
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");
            plainText = plainText.Trim();
            
            // Replace common HTML entities
            plainText = plainText.Replace("&nbsp;", " ");
            plainText = plainText.Replace("&amp;", "&");
            plainText = plainText.Replace("&lt;", "<");
            plainText = plainText.Replace("&gt;", ">");
            plainText = plainText.Replace("&quot;", "\"");
            
            return plainText;
        }
    }
}
