using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Boards;

public class ReorderColumnsRequest
{
    public List<ColumnReorderItem> Columns { get; set; } = new();
}

public class ColumnReorderItem
{
    public Guid ColumnId { get; set; }
    public int Position { get; set; }
}
