using System.Collections.Generic;
using UnityEngine;

namespace VitalRouter.Unity;

public class SubscriptionHandle : MonoBehaviour
{
    public List<Subscription> Subscriptions { get; } = new();

    void OnDestroy()
    {
        foreach (var subscription in Subscriptions)
        {
            subscription.Dispose();
        }
    }
}
