namespace TaskFlow.Application.DTOs.Boards;

public class CreateBoardColumnRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int? WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }
}
