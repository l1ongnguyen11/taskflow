using System.Collections.Generic;

namespace TaskFlow.Application.Common;

public class PaginationMetadata
{
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class PaginatedResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();

    public static PaginatedResponse<T> SuccessResponse(List<T> data, int totalCount, int limit, int offset, string message = "")
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Limit = limit,
                Offset = offset
            }
        };
    }

    public static PaginatedResponse<T> FailResponse(string message)
    {
        return new PaginatedResponse<T>
        {
            Success = false,
            Message = message,
            Data = new List<T>()
        };
    }
}
