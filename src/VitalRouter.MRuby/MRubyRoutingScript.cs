using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MRubyCS;
using MRubyCS.Internals;

namespace VitalRouter.MRuby
{
    public class MRubyRoutingScript : IDisposable
    {
        public static bool TryFindScript(RFiber fiber, out MRubyRoutingScript script)
        {
            return scriptTable.TryGetValue(fiber, out script!);
        }

        static readonly ConcurrentDictionary<RFiber, MRubyRoutingScript> scriptTable = new();

        public Router Router { get; }
        public CancellationToken CancellationToken { get; private set; }

        TaskCompletionSource<bool>? completionSource;
        readonly RFiber fiber;

        public MRubyRoutingScript(RFiber fiber, Router router)
        {
            this.fiber = fiber;
            Router = router;
        }

        public ValueTask RunAsync(CancellationToken cancellation = default)
        {
            if (fiber.State != FiberState.Created && fiber.State != FiberState.Terminated)
            {
                throw new MRubyRoutingException($"Script already running. {fiber.State}");
            }

            scriptTable.TryAdd(fiber, this);
            fiber.Resume(Array.Empty<MRubyValue>());

            if (fiber.State == FiberState.Terminated)
            {
                return new ValueTask();
            }

            cancellation.ThrowIfCancellationRequested();

            completionSource = new TaskCompletionSource<bool>();
            CancellationToken = cancellation;

            return new ValueTask(completionSource.Task);
        }

        public void Resume()
        {
            try
            {
                fiber.Resume(Array.Empty<MRubyValue>());
                if (fiber.State == FiberState.Terminated)
                {
                    completionSource!.TrySetResult(true);
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }

            if (CancellationToken.IsCancellationRequested)
            {
                completionSource!.TrySetCanceled(CancellationToken);
            }
        }

        internal void SetException(Exception exception)
        {
            completionSource!.TrySetException(exception);
        }

        public void Dispose()
        {
            scriptTable.TryRemove(fiber, out _);
        }
    }
}
