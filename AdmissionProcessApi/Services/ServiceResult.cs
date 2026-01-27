namespace AdmissionProcessApi.Services;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? ErrorCode { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage, int? errorCode = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

public class ServiceResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? ErrorCode { get; private set; }

    private ServiceResult() { }

    public static ServiceResult Success()
    {
        return new ServiceResult { IsSuccess = true };
    }

    public static ServiceResult Failure(string errorMessage, int? errorCode = null)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}
