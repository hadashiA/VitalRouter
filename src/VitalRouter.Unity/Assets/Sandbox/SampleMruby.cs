using System;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using TMPro;
using UnityEngine;
using VitalRouter;
using VitalRouter.MRuby;

[MessagePackObject(keyAsPropertyName: true)]
public struct TextCommand : ICommand
{
    public string Body;
}

[MessagePackObject(keyAsPropertyName: true)]
public struct PositionCommand : ICommand
{
    public int X;
}



[MRubyCommand("text", typeof(TextCommand))]
[MRubyCommand("pos", typeof(PositionCommand))]
public partial class MyCommands : MRubyCommandPreset {}

public class SampleMruby : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI label = default!;

    async UniTaskVoid Start()
    {
        var router = Router.Default;
        router.Subscribe<TextCommand>(async (cmd, ctx) =>
        {
            Debug.Log($"{cmd.GetType().Name}: {cmd.Body}");
            label.text += $"From mruby: {cmd.GetType().Name}: {cmd.Body}\n";
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        });
        router.Subscribe<PositionCommand>((cmd, ctx) =>
        {
            label.text += $"From mruby: {cmd.GetType().Name}: {cmd.X}\n";
        });

        var context = MRubyContext.Create(router, new MyCommands());

        context.SharedState.SetString("a", "hoge mogeo");
        context.SharedState.SetInt("i", 12345);
        context.SharedState.SetBool("b", true);

        const string ruby = "3.times { |i| cmd :text, body: \"ほげほげ #{i}\" }\n" +
                            "\n";
        var script = context.CompileScript(ruby);

        while (true)
        {
            label.text = "";
            await script.RunAsync();
            await UniTask.Delay(TimeSpan.FromSeconds(5));
        }
    }
}