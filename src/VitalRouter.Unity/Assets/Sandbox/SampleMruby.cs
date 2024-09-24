using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    // ReSharper disable all Unity.IncorrectMethodSignature

    [MRubyObject]
    public partial struct TextCommand : ICommand
    {
        public string Body;
    }

    [MRubyCommand("text", typeof(TextCommand))]
    public partial class MyCommands : MRubyCommandPreset {}

    [Routes]
    public partial class SampleMruby : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI label = default!;

        [SerializeField]
        Button button = default!;

        int counter;

        async UniTask Start()
        {
            using var context = MRubyContext.Create(Router.Default, new MyCommands());
            using var script = context.CompileScript(
                "3.times { |i| cmd :text, body: \"Hello #{i}\" }\n");

            MapTo(Router.Default);

            await script.RunAsync();
        }

        public async UniTask On(TextCommand cmd, PublishContext ctx)
        {
            ctx.MRubySharedState()!.Set("counter", ++counter);

            label.text = cmd.Body;

            await button.OnClickAsync();
        }
    }
}

