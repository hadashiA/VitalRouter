using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VitalRouter;
using VitalRouter.MRuby;

namespace Sandbox
{
    public enum CharacterType
    {
        A,
        B,
        C
    }

    [MRubyObject]
    public partial struct CharacterSpeakCommand : ICommand
    {
        public string Id;
        public string Text;
    }

    [MRubyObject]
    public partial struct CharacterMoveCommand : ICommand
    {
        public string Id;
        public Vector3 To;
    }

    [MRubyObject]
    public readonly partial struct CharacterEnterCommand : ICommand
    {
        public readonly CharacterType[] Types;

        public CharacterEnterCommand(CharacterType[] types)
        {
            Types = types;
        }
    }

    [MRubyObject]
    public readonly partial struct CharacterExitCommand : ICommand
    {
    }

    [MRubyCommand("speak", typeof(CharacterSpeakCommand))]
    [MRubyCommand("move", typeof(CharacterMoveCommand))]
    public partial class MyCommands : MRubyCommandPreset {}

    public class SampleMruby : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI label = default!;

        async UniTaskVoid Start()
        {
            var router = Router.Default;
            router.Subscribe<CharacterSpeakCommand>(async (cmd, ctx) =>
            {
                Debug.Log($"{cmd.GetType().Name}: {cmd.Text}");
                label.text += $"From mruby: {cmd.GetType().Name}: {cmd.Text}\n";
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            });
            router.Subscribe<CharacterMoveCommand>((cmd, ctx) =>
            {
                UnityEngine.Debug.Log($"From mruby: {cmd.GetType().Name}: {cmd.To}\n");
            });

            var context = MRubyContext.Create(router, new MyCommands());

            context.SharedState.Set("a", "hoge mogeo");
            context.SharedState.Set("i", 12345);
            context.SharedState.Set("b", true);

            const string ruby = "wait 2.secs\n" +
                                "log \"@@@@@\"\n" +
                                "cmd :move, to: [1, 2, 3]\n" +
                                "3.times { |i| cmd :speak, id: 'Bob', text: %Q{Hello ほげ ほげ #{i}} }\n" +
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
}