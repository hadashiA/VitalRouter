using NUnit.Framework.Constraints;
using VitalRouter.Internal;

namespace VitalRouter;

public class RoutingBuilder
{
    readonly ExpandBuffer<ICommandSubscriber> subscribers = new(16);
    readonly ExpandBuffer<IAsyncCommandSubscriber> asyncSubscribers = new(16);
    readonly ExpandBuffer<IAsyncCommandInterceptor> interceptors = new(4);
}