using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.DTOs.Common;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(to)) return;

        // Dev / Fallback mode if SMTP host or username is not configured
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Username))
        {
            _logger.LogInformation("[DEV EMAIL LOG] To: {To} | Subject: {Subject}\nBody:\n{Body}", to, subject, body);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(to);

            using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", to, subject);
            // Fallback log so dev environment still captures invitation link
            _logger.LogInformation("[DEV EMAIL FALLBACK LOG] To: {To} | Subject: {Subject}\nBody:\n{Body}", to, subject, body);
        }
    }

    public async Task SendWorkspaceInvitationEmailAsync(string toEmail, string workspaceName, string inviterName, string invitationToken)
    {
        var appUrl = string.IsNullOrWhiteSpace(_settings.AppUrl) ? "http://localhost:5173" : _settings.AppUrl.TrimEnd('/');
        var acceptUrl = $"{appUrl}/workspaces";

        var subject = $"[TaskFlow] Lời mời tham gia Không gian làm việc '{workspaceName}'";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333; margin: 0; padding: 20px; }}
        .container {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08); }}
        .header {{ background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 28px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }}
        .content {{ padding: 32px 28px; font-size: 15px; line-height: 1.6; color: #475569; }}
        .highlight {{ font-weight: 600; color: #1e293b; }}
        .btn-container {{ text-align: center; margin: 28px 0; }}
        .btn {{ display: inline-block; background-color: #2563eb; color: #ffffff !important; font-weight: 600; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-size: 15px; transition: background-color 0.2s; }}
        .token-box {{ background-color: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 12px 16px; margin: 20px 0; font-family: monospace; font-size: 14px; word-break: break-all; color: #334155; text-align: center; }}
        .footer {{ background-color: #f8fafc; padding: 20px 28px; font-size: 13px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>TaskFlow</h1>
        </div>
        <div class=""content"">
            <p>Xin chào,</p>
            <p><span class=""highlight"">{WebUtility.HtmlEncode(inviterName)}</span> đã mời bạn gia nhập Không gian làm việc <span class=""highlight"">'{WebUtility.HtmlEncode(workspaceName)}'</span> trên hệ thống TaskFlow.</p>
            
            <div class=""btn-container"">
                <a href=""{acceptUrl}"" class=""btn"" target=""_blank"">Mở TaskFlow để Chấp nhận Lời mời</a>
            </div>

            <p style=""font-size: 13px; color: #64748b;"">Mã Token xác nhận lời mời của bạn:</p>
            <div class=""token-box"">{invitationToken}</div>

            <p style=""font-size: 13px; color: #64748b;"">Lời mời này có hiệu lực trong <strong>7 ngày</strong>. Bạn có thể xem và chấp nhận lời mời trực tiếp tại màn hình Danh sách Không gian làm việc sau khi đăng nhập.</p>
        </div>
        <div class=""footer"">
            &copy; {DateTime.UtcNow.Year} TaskFlow - Project & Task Management System. All rights reserved.
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }
}
