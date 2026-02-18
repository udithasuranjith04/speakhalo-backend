namespace SpeakHalo.Shared.Models;

public class Result
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result Success(string? message = null) => new() { IsSuccess = true, Message = message };
    public static Result Failure(string error) => new() { IsSuccess = false, Errors = new List<string> { error } };
    public static Result Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(T data, string? message = null) => new() { IsSuccess = true, Data = data, Message = message };
    public new static Result<T> Failure(string error) => new() { IsSuccess = false, Errors = new List<string> { error } };
    public new static Result<T> Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
