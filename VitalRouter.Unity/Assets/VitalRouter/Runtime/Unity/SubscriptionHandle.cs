using UnityEngine;

namespace VitalRouter.Unity;

public class SubscriptionHandle : MonoBehaviour
{
    public Subscription Subscription { get; set; }

    void OnDestroy()
    {
        Subscription.Dispose();
    }
}