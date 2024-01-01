using System;

namespace VitalRouter;

public interface IRoutable
{
    Type SubscriberType { get; }
    Type AsyncSubscriberType { get; }
    void MapRoutes(ICommandSubscribable subscribable);
}