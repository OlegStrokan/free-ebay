namespace Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public List<string> Errors { get; }

    protected Result(bool isSuccess, T? value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, []);

    public static Result<T> Failure(string error) => new(false, default, [error]);

    public static Result<T> Failure(List<string> errors) => new(false, default, errors);
}

public class Result : Result<bool>
{
    private Result(bool ok, List<string> errors)
        : base(ok, ok, errors)
    {
    }

    public static Result Success() => new(true, []);

    public static new Result Failure(string error) => new(false, [error]);

    public static new Result Failure(List<string> errors) => new(false, errors);
}