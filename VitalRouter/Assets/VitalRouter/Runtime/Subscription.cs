using System;

namespace VitalRouter;

public struct Subscription : IDisposable
{
    readonly ICommandSubscribable commandBus;
    readonly ICommandSubscriber? subscriber;
    readonly IAsyncCommandSubscriber? asyncSubscriber;

    public Subscription(ICommandSubscribable commandBus, ICommandSubscriber subscriber)
    {
        this.commandBus = commandBus;
        this.subscriber = subscriber;
    }

    public Subscription(ICommandSubscribable commandBus, IAsyncCommandSubscriber subscriber)
    {
        this.commandBus = commandBus;
        this.asyncSubscriber = subscriber;
    }

    public Subscription(ICommandSubscribable commandBus, ICommandSubscriber subscriber, IAsyncCommandSubscriber asyncSubscriber)
    {
        this.commandBus = commandBus;
        this.subscriber = subscriber;
        this.asyncSubscriber = asyncSubscriber;
    }

    public void Dispose()
    {
        if (subscriber != null)
            commandBus.Unsubscribe(subscriber);
        if (asyncSubscriber != null)
            commandBus.Unsubscribe(asyncSubscriber);
    }
}
