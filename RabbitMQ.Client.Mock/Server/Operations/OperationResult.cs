namespace RabbitMQ.Client.Mock.Server.Operations;

internal class OperationResult
{
    private OperationResult() { }

    public static OperationResult Success(string? message = null)
    {
        return new OperationResult
        {
            Status = OperationResultStatus.Success,
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

    public static OperationResult Failure(string message)
    {
        return new OperationResult
        {
            Status = OperationResultStatus.Failure,
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

    public static OperationResult Failure(string message, Exception exception)
    {
        return new OperationResult
        {
            Status = OperationResultStatus.Failure,
            Message = message,
            Exception = exception
        };
    }

    public bool IsSuccess => Status == OperationResultStatus.Success;

    public bool IsFailure => Status == OperationResultStatus.Failure;

    public bool IsTimeout => Status == OperationResultStatus.Timeout;

    public OperationResultStatus Status { get; private set; }

    public string? Message { get; private set; }

    public Exception? Exception { get; private set; }
}
