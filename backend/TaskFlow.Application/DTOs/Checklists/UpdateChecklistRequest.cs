namespace TaskFlow.Application.DTOs.Checklists;

public class UpdateChecklistRequest
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
}
