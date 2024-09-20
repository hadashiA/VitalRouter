using System;
using Cysharp.Threading.Tasks;
using Sandbox;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using VitalRouter;

// public struct TextCommand : ICommand
// {
//     public string Body;
// }
//
// public class SampleMruby : MonoBehaviour
// {
//     [SerializeField]
//     TextMeshProUGUI label;
//
//     async UniTaskVoid Start()
//     {
//         var router = Router.Default;
//         router.Subscribe<TextCommand>(async (cmd, ctx) =>
//         {
//             label.text += $"From mruby: {cmd.GetType().Name}: {cmd.Body}\n";
//             await UniTask.Delay(TimeSpan.FromSeconds(1));
//         });
//
//         var context = MRubyContext.Create(router, new MyCommands());
//
//         const string ruby = "3.times { |i| cmd :text, body: \"ほげほげ #{i}\" }\n" +
//                             "\n";
//         var script = context.CompileScript(ruby);
//
//         await script.RunAsync();
//     }
// }

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

        using var subscription = p.MapTo(Router.Default, new AInterceptor());
        // var subscription = p.MapTo(Router.Default);
        Profiler.BeginSample("Test 1");
        await Router.Default.PublishAsync(typeof(CharacterExitCommand), new CharacterExitCommand());
        Profiler.EndSample();

        Profiler.BeginSample("Test 2");
        await Router.Default.PublishAsync(new CharacterExitCommand());
        Profiler.EndSample();

        Profiler.BeginSample("Test 3");
        await Router.Default.PublishAsync(new CharacterExitCommand());
        Profiler.EndSample();
    }
}
