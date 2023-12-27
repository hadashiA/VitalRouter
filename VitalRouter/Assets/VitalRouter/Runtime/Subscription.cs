using System;

namespace VitalRouter;

public struct Subscription : IDisposable
{
    readonly CommandBus commandBus;
    readonly ICommandListener? listener;
    readonly IAsyncCommandListener? asyncListener;

    public Subscription(CommandBus commandBus, ICommandListener listener)
    {
        this.commandBus = commandBus;
        this.listener = listener;
        this.asyncListener = null;
    }

    public Subscription(CommandBus commandBus, IAsyncCommandListener asyncListener)
    {
        this.commandBus = commandBus;
        this.asyncListener = asyncListener;
        this.listener = null;
    }

    public void Dispose()
    {
        if (listener != null)
            commandBus.Unsubscribe(listener);
        if (asyncListener != null)
            commandBus.Unsubscribe(asyncListener);
    }
}
