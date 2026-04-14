namespace IntelliImport.Domain.ValueObjects;

/// <summary>
/// Railway-oriented Result pattern. Used by all service and handler responses.
/// Suitable for extraction as a standalone NuGet package.
/// </summary>
public sealed class Result<T>
{
    private Result(T? value, bool isSuccess, string? error, string? errorCode)
    {
        Value     = value;
        IsSuccess = isSuccess;
        Error     = error;
        ErrorCode = errorCode;
    }

    public T?     Value     { get; }
    public bool   IsSuccess { get; }
    public bool   IsFailure => !IsSuccess;
    public string? Error    { get; }
    public string? ErrorCode { get; }

    public static Result<T> Success(T value)
        => new(value, true, null, null);

    public static Result<T> Failure(string error, string? errorCode = null)
        => new(default, false, error, errorCode);

    /// <summary>Map value if success, propagate failure.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess
            ? Result<TOut>.Success(mapper(Value!))
            : Result<TOut>.Failure(Error!, ErrorCode);

    /// <summary>Bind to another Result-returning function (monadic chaining).</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value!) : Result<TOut>.Failure(Error!, ErrorCode);

    public override string ToString()
        => IsSuccess ? $"Success({Value})" : $"Failure({ErrorCode}: {Error})";
}

/// <summary>Non-generic Result for commands that return no value.</summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error     = error;
        ErrorCode = errorCode;
    }

    public bool    IsSuccess { get; }
    public bool    IsFailure => !IsSuccess;
    public string? Error     { get; }
    public string? ErrorCode { get; }

    public static readonly Result Ok      = new(true,  null, null);
    public static Result Failure(string error, string? errorCode = null)
        => new(false, error, errorCode);
}
