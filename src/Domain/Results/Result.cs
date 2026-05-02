namespace IntelliImport.Domain.Results;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; protected set; }
    public string? Code { get; protected set; }

    protected Result() { }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error, string code) => 
        new() { IsSuccess = false, Error = error, Code = code };
}

public class Result<T> : Result
{
    public T? Value { get; protected set; }

    public static Result<T> Success(T value) => 
        new() { IsSuccess = true, Value = value };
    
    public static Result<T> Failure(string error, string code) => 
        new() { IsSuccess = false, Error = error, Code = code, Value = default };
}