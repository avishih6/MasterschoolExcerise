namespace AdmissionProcessBL;

public class LogicResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? HttpStatusCode { get; private set; }

    private LogicResult() { }

    public static LogicResult<T> Success(T data)
    {
        return new LogicResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static LogicResult<T> Failure(string errorMessage, int httpStatusCode = 400)
    {
        return new LogicResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = httpStatusCode
        };
    }

    public static LogicResult<T> Conflict(string errorMessage, T? existingData = default)
    {
        return new LogicResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = 409,
            Data = existingData
        };
    }
}

public class LogicResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? HttpStatusCode { get; private set; }

    private LogicResult() { }

    public static LogicResult Success()
    {
        return new LogicResult { IsSuccess = true };
    }

    public static LogicResult Failure(string errorMessage, int httpStatusCode = 400)
    {
        return new LogicResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = httpStatusCode
        };
    }
}
