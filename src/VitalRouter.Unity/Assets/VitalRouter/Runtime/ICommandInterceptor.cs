using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace VitalRouter
{
public delegate UniTask PublishContinuation<in T>(T cmd, PublishContext ctx) where T : ICommand;

public interface ICommandInterceptor
{
    UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
        where T : ICommand;
}

public abstract class TypedCommandInterceptro<T> : ICommandInterceptor
    where T : ICommand
{
    public UniTask InvokeAsync<TReceive>(TReceive command, PublishContext context, PublishContinuation<TReceive> next)
        where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            var nextCasted = Unsafe.As<PublishContinuation<TReceive>, PublishContinuation<T>>(ref next);
            var commandCasted = Unsafe.As<TReceive, T>(ref command);
            return InvokeAsync(commandCasted, context, nextCasted);
        }
        return next(command, context);
    }

    public abstract UniTask InvokeAsync(
        T command,
        PublishContext context,
        PublishContinuation<T> next);
}
}