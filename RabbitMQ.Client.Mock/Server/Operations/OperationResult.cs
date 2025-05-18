namespace RabbitMQ.Client.Mock.Server.Operations;

internal class OperationResult(object? result = null)
{
    public bool IsSuccess => Status == OperationResultStatus.Success;

    public bool IsWarning => Status == OperationResultStatus.Warning;

    public bool IsFailure => Status == OperationResultStatus.Failure;

    public bool IsTimeout => Status == OperationResultStatus.Timeout;

    public OperationResultStatus Status { get; protected set; }

    public string? Message { get; protected set; }

    public Exception? Exception { get; protected set; }

    public TResult? GetResult<TResult>() where TResult : class
    {
        return result switch
        {
            null => default,
            TResult typedResult => typedResult,
            _ => throw new InvalidOperationException($"Result is not of type {typeof(TResult).Name}.")
        };
    }

    public static OperationResult Success(string? message = null, object? result = null)
    {
        return new OperationResult(result)
        {
            Status = OperationResultStatus.Success,
            Message = message
        };
    }

    public static OperationResult Warning(string? message = null, object? result = null)
    {
        return new OperationResult(result)
        {
            Status = OperationResultStatus.Warning,
            Message = message
        };
    }

    public static OperationResult TimedOut(string? message = null)
    {
        return new OperationResult
        {
            Status = OperationResultStatus.Timeout,
            Message = message
        };
    }
    public static OperationResult Failure(Exception exception)
    {
        return new OperationResult
        {
            Status = OperationResultStatus.Failure,
            Message = exception.Message,
            Exception = exception
        };
    }
}