using System.Threading.Tasks;
using MRubyCS;

namespace VitalRouter.MRuby;

public static class PublishContextExtensions
{
    public static MRubyState? MRuby(this PublishContext publishContext)
    {
        if (publishContext.Extensions.TryGetValue(MRubyStateInterceptor.MRubyStateKey, out var obj) &&
            obj is MRubyState mrb)
        {
            return mrb;
        }
        return null;
    }

    public static MRubySharedVariableTable? MRubySharedVariables(this PublishContext publishContext)
    {
        return publishContext.MRuby()?.GetSharedVariables();
    }
}

public sealed class MRubyStateInterceptor : ICommandInterceptor
{
    public const string MRubyStateKey = "VitalRouter.MRubyState";
    internal readonly MRubyState? MRubyState;

    public MRubyStateInterceptor(MRubyState mrb)
    {
        MRubyState = mrb;
    }

    public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
        where T : ICommand
    {
        context.Extensions[MRubyStateKey] = MRubyState;
        return next(command, context);
    }
}