using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VitalRouter.Runtime
{
    public static class Initializer
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Register()
        {
            Router.AsyncLockFactory = () => new UniTaskAsyncLock();

            Router.YieldAction = async cancellationToken =>
            {
// #if VITALROUTER_UNITASK_INTEGRATION
                await UniTask.Yield(cancellationToken);
// #elif UNITY_2023_2_OR_NEWER
                await Awaitable.NextFrameAsync(cancellationToken);
//#endif
            };
        }
    }
}