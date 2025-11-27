namespace QuickClinique.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string toEmail, string name, string verificationLink);
        Task SendPasswordResetEmail(string toEmail, string name, string resetLink);
        Task SendAccountActivationEmail(string toEmail, string name, string loginUrl);
        Task SendAppointmentConfirmationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, int queueNumber);
        Task SendQueuePositionUpdateEmail(string toEmail, string patientName, int newPosition, int queueNumber);
        Task SendAppointmentCompletedEmail(string toEmail, string patientName, string appointmentDate);
        Task SendAppointmentCancellationEmail(string toEmail, string patientName, string appointmentDate, string appointmentTime, string? reason = null);
    }
}