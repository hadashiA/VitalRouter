using UnityEngine;

namespace VitalRouter;

public class SubscriptionHandle : MonoBehaviour
{
    public Subscription Subscription { get; set; }

    void OnDestroy()
    {
        Subscription.Dispose();
    }
}