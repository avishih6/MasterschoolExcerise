namespace AdmissionProcessBL.Services;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? HttpStatusCode { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage, int httpStatusCode = 400)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = httpStatusCode
        };
    }

    public static ServiceResult<T> Conflict(string errorMessage, T? existingData = default)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = 409,
            Data = existingData
        };
    }
}

public class ServiceResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? HttpStatusCode { get; private set; }

    private ServiceResult() { }

    public static ServiceResult Success()
    {
        return new ServiceResult { IsSuccess = true };
    }

    public static ServiceResult Failure(string errorMessage, int httpStatusCode = 400)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            HttpStatusCode = httpStatusCode
        };
    }
}
