using System.Diagnostics;

namespace RabbitMQ.Client.Tests;
public class TestBase
{ 
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

        // declare the queue
        var result = await channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, passive, nowait, cancellationToken);

        // if an exchange has been specified, bind the queue to it.
        if (!string.IsNullOrWhiteSpace(exchange))
        {
            await channel.QueueBindAsync(result.QueueName, exchange, bindingKey, arguments, nowait, cancellationToken);
        }

        return result;
    }

    protected void ConfigureFactory(IConnectionFactory factory)
    {
        // Modify the factory configuration as needed for your tests
    }

    protected async ValueTask WaitUntilAsync(Func<bool> condition, int timeout = 10000, int interval = 100)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeout)
        {
            await Task.Delay(interval);
        }
    }

    protected ValueTask<string> CreateUniqueQueueName()
    {
        var queueName = $"RQ-UnitTest-{Guid.NewGuid().ToString("D")}";
        return ValueTask.FromResult(queueName);
    }
}
