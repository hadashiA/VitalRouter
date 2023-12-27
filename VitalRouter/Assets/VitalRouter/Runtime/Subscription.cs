using System;

namespace VitalRouter;

public struct Subscription : IDisposable
{
    readonly CommandBus commandBus;
    readonly ICommandSubscriber? subscriber;

    public Subscription(CommandBus commandBus, ICommandSubscriber subscriber)
    {
        this.commandBus = commandBus;
        this.subscriber = subscriber;
    }

    public void Dispose()
    {
        commandBus.Unsubscribe(subscriber);
    }
}
