using Cysharp.Threading.Tasks;

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
        readonly MRubyContext mrubyContext;

        public MRubyContextInterceptor(MRubyContext mrubyContext)
        {
            this.mrubyContext = mrubyContext;
        }

        public UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
        {
            context.Extensions[MRubyContextKey] = mrubyContext;
            return next(command, context);
        }
    }
}