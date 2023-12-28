using System;

namespace VitalRouter;

public struct Subscription : IDisposable
{
    readonly CommandBus commandBus;
    readonly ICommandSubscriber? subscriber;
    readonly IAsyncCommandSubscriber? asyncSubscriber;

    public Subscription(CommandBus commandBus, ICommandSubscriber subscriber)
    {
        this.commandBus = commandBus;
        this.subscriber = subscriber;
    }

    public Subscription(CommandBus commandBus, IAsyncCommandSubscriber subscriber)
    {
        this.commandBus = commandBus;
        this.asyncSubscriber = subscriber;
    }

    public void Dispose()
    {
        if (subscriber != null)
            commandBus.Unsubscribe(subscriber);
        if (asyncSubscriber != null)
            commandBus.Unsubscribe(asyncSubscriber);
    }
}