namespace InvoiceReminder.Application.Abstractions;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    protected Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value)
    {
        return new(true, value, null);
    }

    public static Result<T> Failure(string error)
    {
        return new(false, default, error);
    }
}
