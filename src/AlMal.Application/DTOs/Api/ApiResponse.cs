namespace AlMal.Application.DTOs.Api;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public PaginationInfo? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Ok(T data, PaginationInfo pagination) => new() { Success = true, Data = data, Pagination = pagination };
    public static ApiResponse<T> Fail(string code, string message) => new() { Success = false, Error = new ApiError { Code = code, Message = message } };
}

public class ApiError
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
