using System.Threading.Tasks;

namespace VitalRouter.MRuby
{
    public static class PublishContextExtensions
    {
        public static MRubyContext? MRuby(this PublishContext publishContext)
        {
            if (publishContext.Extensions.TryGetValue(MRubyContextInterceptor.MRubyContextKey, out var obj) &&
                obj is MRubyContext mrubyContext)
            {
                return mrubyContext;
            }
            return null;
        }

        public static MRubySharedState? MRubySharedState(this PublishContext publishContext)
        {
            return publishContext.MRuby()?.SharedState;
        }
    }

    public class MRubyContextInterceptor : ICommandInterceptor
    {
        public const string MRubyContextKey = "VitalRouter.MRubyContext";
        internal readonly MRubyContext MrubyContext;

        public MRubyContextInterceptor(MRubyContext mrubyContext)
        {
            MrubyContext = mrubyContext;
        }

        public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
        {
            if (MrubyContext is { IsInvalid: false, IsClosed: false })
            {
                context.Extensions[MRubyContextKey] = MrubyContext;
            }
            return next(command, context);
        }
    }
}