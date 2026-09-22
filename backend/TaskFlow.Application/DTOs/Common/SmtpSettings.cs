namespace TaskFlow.Application.DTOs.Common;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "TaskFlow System";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AppUrl { get; set; } = "http://localhost:5173";
}
