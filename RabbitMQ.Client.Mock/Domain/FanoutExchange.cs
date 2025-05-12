using System.Runtime.CompilerServices;

namespace RabbitMQ.Client.Mock.Domain;

internal class FanoutExchange : Exchange
{
    public FanoutExchange(string name, int connectionNumber)
        : base(name, ExchangeType.Fanout, connectionNumber)
    {        
    }

    private string _bindingKey = Guid.NewGuid().ToString();

    public override async ValueTask BindQueueAsync(string bindingKey, RabbitQueue queue, IDictionary<string, object?>? arguments = null)
    {
        // We ignore the bindingkey provided and introduce our own, exchange unique, bindingkey.
        // doing so, we essentially create the simple Queues list that we need.
        await base.BindQueueAsync(_bindingKey, queue, arguments);
    }

    public override async ValueTask UnbindQueueAsync(string bindingKey, RabbitQueue queue)
    {
        // We ignore the bindingkey provided and introduce our own, exchange unique, bindingkey.
        // doing so, we essentially create the simple Queues list that we need.
        await base.UnbindQueueAsync(_bindingKey, queue);
    }
}
