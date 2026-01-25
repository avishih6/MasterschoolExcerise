namespace AdmissionProcessDAL.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string userId, string verificationToken);
}
