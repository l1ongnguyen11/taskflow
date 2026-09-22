namespace TaskFlow.Application.DTOs.Boards;

public class CreateBoardRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
