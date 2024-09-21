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

    static class CharacterActor
    {
        public static async UniTask MoveAsync(string id, Vector3 position)
        {
        }
    }

    static class MessageBalloon
    {
        public static async UniTask PresentAndWaitAsync(string id, string message)
        {
        }
    }

    // ReSharper disable all UnassignedField.Global
    // ReSharper disable all NotAccessedField.Global
    // ReSharper disable all UnusedMember.Global

    [MRubyObject]
    public partial struct CharacterMoveCommand : ICommand
    {
        public string Id;
        public Vector3 To;
    }

    [MRubyObject]
    public partial struct CharacterSpeakCommand : ICommand
    {
        public string Id;
        public string Text;
    }

    [MRubyCommand("speak", typeof(CharacterSpeakCommand))]
    [MRubyCommand("move", typeof(CharacterMoveCommand))]
    public partial class MyCommands : MRubyCommandPreset {}


    [Routes]
    class MRubyPresenter
    {
        // mruby's `cmd :move` calls this async handler.
        public async UniTask On(CharacterMoveCommand cmd)
        {
            await CharacterActor.MoveAsync(cmd.Id, cmd.To);
        }

        // mruby's `cmd :speak` calls this async handler.
        public async UniTask On(CharacterSpeakCommand cmd)
        {
            await MessageBalloon.PresentAndWaitAsync(cmd.Id, cmd.Text);
        }
    }


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


            // You can set any shared state
            context.SharedState.Set("flag_1", true);


            context.SharedState.Set("a", "hoge mogeo");
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