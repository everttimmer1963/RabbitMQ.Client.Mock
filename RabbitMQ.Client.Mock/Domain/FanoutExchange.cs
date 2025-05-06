using System.Runtime.CompilerServices;

namespace RabbitMQ.Client.Mock.Domain;

internal class FanoutExchange : Exchange
{
    public FanoutExchange(string name)
        : base(name, ExchangeType.Fanout)
    {        
    }

    private string _bindingKey = Guid.NewGuid().ToString();

    public override async ValueTask BindQueueAsync(string bindingKey, RabbitQueue queue)
    {
        // We ignore the bindingkey provided and introduce our own, exchange unique, bindingkey.
        // doing so, we essentially create the simple Queues list that we need.
        await base.BindQueueAsync(_bindingKey, queue);
    }

    public override async ValueTask UnbindQueueAsync(string bindingKey, RabbitQueue queue)
    {
        // We ignore the bindingkey provided and introduce our own, exchange unique, bindingkey.
        // doing so, we essentially create the simple Queues list that we need.
        await base.UnbindQueueAsync(_bindingKey, queue);
    }
}
