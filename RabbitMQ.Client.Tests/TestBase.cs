namespace RabbitMQ.Client.Tests;
public class TestBase
{ 
    protected enum QueueType
    { 
        Classic,
        Quorum
    }

    protected readonly IConnectionFactory factory;

    public TestBase()
    {
        this.factory = new ConnectionFactory();
        ConfigureFactory(this.factory);
    }

    protected async ValueTask<QueueDeclareOk> QueueDeclareAndBindAsync(
                            IChannel channel,
                            string queue = "",
                            string exchange = "",
                            string bindingKey = "",
                            bool durable = false,
                            bool exclusive = true,
                            bool autoDelete = true,
                            IDictionary<string, object?>? arguments = null,
                            bool passive = false,
                            bool nowait = false,
                            CancellationToken cancellationToken = default)
    {
        // first check requirements
        ArgumentNullException.ThrowIfNull(channel);
        if (string.IsNullOrEmpty(queue) && !IsClassicQueue(arguments))
        {
            throw new ArgumentException("Queue name cannot be null or empty for non-classic queues.", nameof(queue));
        }

        // declare the queue
        var result = await channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, passive, nowait, cancellationToken);

        // if an exchange has been specified, bind the queue to it.
        if (!string.IsNullOrWhiteSpace(exchange))
        {
            await channel.QueueBindAsync(result.QueueName, exchange, bindingKey, arguments, nowait, cancellationToken);
        }

        return result;
    }

    private bool IsClassicQueue(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || !arguments.ContainsKey("x-queue-type"))
        {
            return true;
        }

        var type = (string?)arguments["x-queue-type"];
        if(type is null)
        {
            return true;
        }

        return string.Equals(type.ToLowerInvariant(), "classic", StringComparison.OrdinalIgnoreCase);
    }

    protected void ConfigureFactory(IConnectionFactory factory)
    {
        // Modify the factory configuration as needed for your tests
    }
}
