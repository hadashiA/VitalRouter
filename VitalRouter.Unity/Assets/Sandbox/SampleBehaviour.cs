using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using VitalRouter;

public struct DataCommand : ICommand
{
    public int Damage { get; set; }
}


public class SampleBehaviour : MonoBehaviour
{
    readonly ICommandInterceptor[] interceptors =
    {
        new AInterceptor(),
        new BInterceptor()
    };

    public async UniTaskVoid Start()
    {
        var p = new SamplePresenter();

        var subscription = p.MapTo(Router.Default, new AInterceptor());
        // var subscription = p.MapTo(Router.Default);
        Profiler.BeginSample("Test 1");
        await Router.Default.PublishAsync(new CharacterExitCommand());
        Profiler.EndSample();

        Profiler.BeginSample("Test 2");
        await Router.Default.PublishAsync(new CharacterExitCommand());
        Profiler.EndSample();
        subscription.Dispose();

        Profiler.BeginSample("Test 3");
        await Router.Default.PublishAsync(new CharacterExitCommand());
         Profiler.EndSample();
        subscription.Dispose();

        Profiler.BeginSample("Rent 1");
        var context = InvokeContext<CharacterExitCommand>.Rent(interceptors);
        Profiler.EndSample();

        Profiler.BeginSample("!!!! Inv 1");
        await context.InvokeRecursiveAsync(new CharacterExitCommand());
        Profiler.EndSample();

        Profiler.BeginSample("Return 1");
        context.Return();
        Profiler.EndSample();

        Profiler.BeginSample("Rent 2");
        context = InvokeContext<CharacterExitCommand>.Rent(interceptors);
        Profiler.EndSample();

        Profiler.BeginSample("!!!! Inv 2");
        await context.InvokeRecursiveAsync(new CharacterExitCommand());
        Profiler.EndSample();

        Profiler.BeginSample("Return 2");
        context.Return();
        Profiler.EndSample();
    }
}
