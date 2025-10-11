namespace QuickClinique.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string toEmail, string name, string verificationLink);
        Task SendPasswordResetEmail(string toEmail, string name, string resetLink);
    }
}