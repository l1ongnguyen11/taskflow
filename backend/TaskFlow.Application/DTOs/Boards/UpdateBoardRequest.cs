namespace TaskFlow.Application.DTOs.Boards;

public class UpdateBoardRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
