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

    protected ValueTask<IList<string>> CreateTestMessagesAsync(int count)
    { 
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add($"Hello world! This is test message nr {i}.");
        }
        return ValueTask.FromResult<IList<string>>(list);
    }

    protected void ConfigureFactory(IConnectionFactory factory)
    {
        if (factory is ConnectionFactory connectionFactory)
        {
            connectionFactory.HostName = "localhost";
            connectionFactory.Port = 5672;
            connectionFactory.UserName = "mocktest";
            connectionFactory.Password = "mocktest";
            connectionFactory.Ssl = new SslOption
            {
                Enabled = false,
                ServerName = "localhost"
            };
        }
    }

    protected async ValueTask WaitUntilAsync(Func<bool> condition, int timeout = 10000, int interval = 100)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeout)
        {
            await Task.Delay(interval);
        }
    }

    protected ValueTask<string> CreateUniqueExchangeNameAsync()
    {
        var exchangeName = $"RQ-UnitTest-Exchange-{Guid.NewGuid().ToString("D")}";
        return ValueTask.FromResult(exchangeName);
    }

    protected ValueTask<string> CreateUniqueQueueNameAsync()
    {
        var queueName = $"RQ-UnitTest-Queue-{Guid.NewGuid().ToString("D")}";
        return ValueTask.FromResult(queueName);
    }
}
