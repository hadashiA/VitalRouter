using System;
using System.Collections.Generic;

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

public class CompositeSubscription : IDisposable
{
    readonly CommandBus commandBus;
    readonly List<Subscription> subscriptions = new();

    public void Add(in Subscription x)
    {
        lock (subscriptions)
        {
            subscriptions.Add(x);
        }
    }

    public void Dispose()
    {
        lock (subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
            subscriptions.Clear();
        }
    }
}
