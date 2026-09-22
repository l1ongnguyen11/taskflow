namespace TaskFlow.Application.DTOs.Boards;

public class UpdateBoardColumnRequest
{
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Color { get; set; }
    public int? WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }
}
