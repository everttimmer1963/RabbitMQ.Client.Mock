namespace RabbitMQ.Client.Mock.Server.Operations;

internal class OperationResult
{
    public static object NullValue = new object();

    public bool IsSuccess => Status == OperationResultStatus.Success;

    public bool IsFailure => Status == OperationResultStatus.Failure;

    public bool IsTimeout => Status == OperationResultStatus.Timeout;

    public bool IsNullValue => (this == NullValue);

    public OperationResultStatus Status { get; protected set; }

    public string? Message { get; protected set; }

    public Exception? Exception { get; protected set; }

    public static OperationResult<TResult> Success<TResult>(string? message = null, TResult? result = null) where TResult : class
    {
        return new OperationResult<TResult>(result)
        {
            Status = OperationResultStatus.Success,
            Message = message
        };
    }

    public static OperationResult<TResult> TimedOut<TResult>(string? message = null) where TResult : class
    {
        return new OperationResult<TResult>
        {
            Status = OperationResultStatus.Timeout,
            Message = message
        };
    }

    public static OperationResult<TResult> Failure<TResult>(string message) where TResult : class
    {
        return new OperationResult<TResult>
        {
            Status = OperationResultStatus.Failure,
            Message = message
        };
    }

    public static OperationResult<TResult> Failure<TResult>(Exception exception) where TResult : class
    {
        return new OperationResult<TResult>
        {
            Status = OperationResultStatus.Failure,
            Message = exception.Message,
            Exception = exception
        };
    }

    public static OperationResult<TResult> Failure<TResult>(string message, Exception exception) where TResult : class
    {
        return new OperationResult<TResult>
        {
            Status = OperationResultStatus.Failure,
            Message = message,
            Exception = exception
        };
    }
}

internal class OperationResult<TResult> : OperationResult where TResult : class
{
    internal OperationResult(TResult? result = null)
    {
        Result = result;
    }

    public TResult? Result { get; internal set; }
}
