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
            try
            {
                Console.WriteLine($"[EMAIL] SendVerificationEmail called for: {toEmail}, Name: {name}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendVerificationEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "User";
                }

                if (string.IsNullOrWhiteSpace(verificationLink))
                {
                    Console.WriteLine("[EMAIL ERROR] SendVerificationEmail: verificationLink is null or empty");
                    return;
                }

                var subject = "Verify Your Email - QuickClinique";
                var body = GetEmailTemplate(
                    title: "Verify Your Email",
                    greeting: $"Hello {System.Net.WebUtility.HtmlEncode(name)},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>Welcome to QuickClinique! Please verify your email address by clicking the button below:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{System.Net.WebUtility.HtmlEncode(verificationLink)}' style='display: inline-block; background: linear-gradient(135deg, #06B6D4 0%, #0891B2 100%); color: #FFFFFF; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 12px rgba(6, 182, 212, 0.3);'>Verify Email</a>
                        </div>
                        <p style='margin: 20px 0; color: #718096; font-size: 14px;'>Or copy this link to your browser:</p>
                        <p style='margin: 0 0 20px 0; color: #06B6D4; word-break: break-all; font-size: 14px;'>{System.Net.WebUtility.HtmlEncode(verificationLink)}</p>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>This link will expire in 24 hours.</p>",
                    primaryColor: "#06B6D4"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendVerificationEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendPasswordResetEmail(string toEmail, string name, string resetLink)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendPasswordResetEmail called for: {toEmail}, Name: {name}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendPasswordResetEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "User";
                }

                if (string.IsNullOrWhiteSpace(resetLink))
                {
                    Console.WriteLine("[EMAIL ERROR] SendPasswordResetEmail: resetLink is null or empty");
                    return;
                }

                var subject = "Reset Your Password - QuickClinique";
                var body = GetEmailTemplate(
                    title: "Reset Your Password",
                    greeting: $"Hello {System.Net.WebUtility.HtmlEncode(name)},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>You requested to reset your password. Click the button below to reset it:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{System.Net.WebUtility.HtmlEncode(resetLink)}' style='display: inline-block; background: linear-gradient(135deg, #06B6D4 0%, #0891B2 100%); color: #FFFFFF; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 12px rgba(6, 182, 212, 0.3);'>Reset Password</a>
                        </div>
                        <p style='margin: 20px 0; color: #718096; font-size: 14px;'>Or copy this link to your browser:</p>
                        <p style='margin: 0 0 20px 0; color: #06B6D4; word-break: break-all; font-size: 14px;'>{System.Net.WebUtility.HtmlEncode(resetLink)}</p>
                        <p style='margin: 0 0 20px 0; color: #718096; font-size: 14px;'>This link will expire in 1 hour.</p>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>If you didn't request this, please ignore this email.</p>",
                    primaryColor: "#06B6D4"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendPasswordResetEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendAppointmentConfirmationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, int queueNumber)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendAppointmentConfirmationEmail called for: {toEmail}, Patient: {patientName}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendAppointmentConfirmationEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(patientName))
                {
                    patientName = "Valued Patient";
                }

                var subject = "Appointment Confirmed - QuickClinique";
                var body = GetEmailTemplate(
                    title: "Appointment Confirmed!",
                    greeting: $"Hello {patientName},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>Your appointment has been confirmed. Here are the details:</p>
                        <div style='background-color: #F0F4F8; padding: 24px; border-radius: 12px; margin: 24px 0; border-left: 4px solid #06B6D4;'>
                            <p style='margin: 0 0 12px 0; color: #2D3748;'><strong style='color: #0891B2;'>Date:</strong> {System.Net.WebUtility.HtmlEncode(appointmentDate ?? "N/A")}</p>
                            <p style='margin: 0 0 12px 0; color: #2D3748;'><strong style='color: #0891B2;'>Time:</strong> {System.Net.WebUtility.HtmlEncode(appointmentTime ?? "N/A")}</p>
                            <p style='margin: 0; color: #2D3748;'><strong style='color: #0891B2;'>Queue Number:</strong> <span style='background: linear-gradient(135deg, #06B6D4 0%, #0891B2 100%); color: #FFFFFF; padding: 4px 12px; border-radius: 6px; font-weight: 600;'>#{queueNumber}</span></p>
                        </div>
                        <p style='margin: 20px 0; color: #2D3748; line-height: 1.6;'>Please arrive on time for your appointment. You will be notified when it's your turn.</p>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>If you need to reschedule or cancel, please contact us as soon as possible.</p>",
                    primaryColor: "#06B6D4"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendAppointmentConfirmationEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to allow caller to handle
            }
        }

        public async Task SendQueuePositionUpdateEmail(string toEmail, string patientName, int newPosition, int queueNumber)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendQueuePositionUpdateEmail called for: {toEmail}, Patient: {patientName}, Position: {newPosition}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendQueuePositionUpdateEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(patientName))
                {
                    patientName = "Valued Patient";
                }

                var subject = "Queue Update - You've Moved Up! - QuickClinique";
                var body = GetEmailTemplate(
                    title: "Queue Position Update",
                    greeting: $"Hello {patientName},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>Good news! Your position in the queue has moved up.</p>
                        <div style='background-color: #D1FAE5; padding: 24px; border-radius: 12px; margin: 24px 0; border-left: 4px solid #10B981;'>
                            <p style='margin: 0 0 12px 0; color: #2D3748;'><strong style='color: #059669;'>Your New Position:</strong> <span style='background: linear-gradient(135deg, #10B981 0%, #059669 100%); color: #FFFFFF; padding: 4px 12px; border-radius: 6px; font-weight: 600;'>#{newPosition} in line</span></p>
                            <p style='margin: 0; color: #2D3748;'><strong style='color: #059669;'>Queue Number:</strong> <span style='background: linear-gradient(135deg, #06B6D4 0%, #0891B2 100%); color: #FFFFFF; padding: 4px 12px; border-radius: 6px; font-weight: 600;'>#{queueNumber}</span></p>
                        </div>
                        <p style='margin: 20px 0; color: #2D3748; line-height: 1.6;'>Please be ready for your appointment. You will be notified when it's your turn to be seen.</p>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>If you're not at the clinic yet, please make your way to the clinic as soon as possible.</p>",
                    primaryColor: "#10B981"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendQueuePositionUpdateEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendAppointmentCompletedEmail(string toEmail, string patientName, string appointmentDate)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendAppointmentCompletedEmail called for: {toEmail}, Patient: {patientName}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendAppointmentCompletedEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(patientName))
                {
                    patientName = "Valued Patient";
                }

                var subject = "Appointment Completed - QuickClinique";
                var body = GetEmailTemplate(
                    title: "Appointment Completed",
                    greeting: $"Hello {patientName},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>Your appointment on <strong style='color: #0891B2;'>{System.Net.WebUtility.HtmlEncode(appointmentDate ?? "N/A")}</strong> has been completed.</p>
                        <div style='background-color: #F0F4F8; padding: 24px; border-radius: 12px; margin: 24px 0; border-left: 4px solid #06B6D4;'>
                            <p style='margin: 0 0 12px 0; color: #2D3748; line-height: 1.6;'>Thank you for visiting QuickClinique. We hope you had a positive experience.</p>
                            <p style='margin: 0; color: #2D3748; line-height: 1.6;'>If you have any questions or concerns about your visit, please don't hesitate to contact us.</p>
                        </div>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>We look forward to serving you again in the future.</p>",
                    primaryColor: "#06B6D4"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendAppointmentCompletedEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendAppointmentCancellationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, string? reason = null)
        {
            try
            {
                Console.WriteLine($"[EMAIL] SendAppointmentCancellationEmail called for: {toEmail}, Patient: {patientName}");
                
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine("[EMAIL ERROR] SendAppointmentCancellationEmail: toEmail is null or empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(patientName))
                {
                    patientName = "Valued Patient";
                }

                var subject = "Appointment Cancelled - QuickClinique";
                var reasonSection = !string.IsNullOrWhiteSpace(reason) 
                    ? $@"
                        <div style='background-color: #FEF3C7; padding: 20px; border-radius: 12px; margin: 20px 0; border-left: 4px solid #F59E0B;'>
                            <p style='margin: 0; color: #2D3748;'><strong style='color: #D97706;'>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</p>
                        </div>"
                    : "";
                
                var body = GetEmailTemplate(
                    title: "Appointment Cancelled",
                    greeting: $"Hello {patientName},",
                    content: $@"
                        <p style='margin: 0 0 20px 0; color: #2D3748; line-height: 1.6;'>We regret to inform you that your appointment has been cancelled.</p>
                        <div style='background-color: #F0F4F8; padding: 24px; border-radius: 12px; margin: 24px 0; border-left: 4px solid #DC2626;'>
                            <p style='margin: 0 0 12px 0; color: #2D3748;'><strong style='color: #0891B2;'>Date:</strong> {System.Net.WebUtility.HtmlEncode(appointmentDate ?? "N/A")}</p>
                            <p style='margin: 0; color: #2D3748;'><strong style='color: #0891B2;'>Time:</strong> {System.Net.WebUtility.HtmlEncode(appointmentTime ?? "N/A")}</p>
                        </div>
                        {reasonSection}
                        <p style='margin: 20px 0; color: #2D3748; line-height: 1.6;'>If you need to reschedule your appointment, please log in to your account and book a new appointment, or contact us for assistance.</p>
                        <p style='margin: 0; color: #718096; font-size: 14px;'>We apologize for any inconvenience this may cause.</p>",
                    primaryColor: "#DC2626"
                );

                await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SendAppointmentCancellationEmail exception: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
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
        /// Creates a minimal email template matching the app's teal theme
        /// </summary>
        private string GetEmailTemplate(string title, string greeting, string content, string primaryColor = "#06B6D4")
        {
            // HTML encode title and greeting to prevent XSS and formatting issues
            var encodedTitle = System.Net.WebUtility.HtmlEncode(title ?? "QuickClinique");
            var encodedGreeting = System.Net.WebUtility.HtmlEncode(greeting ?? "Hello,");
            
            // Ensure primaryColor is safe (only allow hex colors)
            if (string.IsNullOrWhiteSpace(primaryColor) || !System.Text.RegularExpressions.Regex.IsMatch(primaryColor, @"^#[0-9A-Fa-f]{6}$"))
            {
                primaryColor = "#06B6D4";
            }

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{encodedTitle}</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; background-color: #F0F4F8;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse; background-color: #F0F4F8; padding: 40px 20px;'>
        <tr>
            <td align='center'>
                <table role='presentation' style='max-width: 600px; width: 100%; border-collapse: collapse; background-color: #FFFFFF; border-radius: 12px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08); overflow: hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, {primaryColor} 0%, #0891B2 100%); padding: 32px 40px; text-align: center;'>
                            <h1 style='margin: 0; color: #FFFFFF; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;'>QuickClinique</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='margin: 0 0 24px 0; color: {primaryColor}; font-size: 24px; font-weight: 600;'>{encodedTitle}</h2>
                            <p style='margin: 0 0 24px 0; color: #2D3748; font-size: 16px; line-height: 1.6;'>{encodedGreeting}</p>
                            {content}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #F0F4F8; padding: 32px 40px; text-align: center; border-top: 1px solid #E2E8F0;'>
                            <p style='margin: 0 0 8px 0; color: #718096; font-size: 14px;'>Best regards,</p>
                            <p style='margin: 0; color: #2D3748; font-size: 14px; font-weight: 600;'>QuickClinique Team</p>
                            <p style='margin: 24px 0 0 0; color: #718096; font-size: 12px; line-height: 1.5;'>This is an automated message. Please do not reply to this email.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
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
