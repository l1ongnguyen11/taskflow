using System.Threading.Tasks;

namespace TaskFlow.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendWorkspaceInvitationEmailAsync(string toEmail, string workspaceName, string inviterName, string invitationToken);
}
