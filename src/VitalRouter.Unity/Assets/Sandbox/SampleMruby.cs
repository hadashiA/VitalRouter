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
        public string Id;
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

            var fvalue = context.EvaluateUnsafe("0.12345").RawValue;
            UnityEngine.Debug.Log($"!!!!! float {fvalue.TT} IsObject={fvalue.IsObject} F={fvalue.FloatValue:F5}");

            context.Load("def hoge(x) = x * 100");
            var h = context.Evaluate<int>("hoge(7)");

            context.Load("class CharacterContext\n" +
                         "  def initialize(id)\n" +
                         "    @id = id\n" +
                         "  end\n" +
                         "  \n" +
                         "  def text(body)\n" +
                         "log(body)\n" +
                         "    cmd :text, id: @id, body:\n" +
                         "  end\n" +
                         "end\n" +
                         "\n" +
                         "def with(id, &block)\n" +
                         "  CharacterContext.new(id).instance_eval(&block)\n" +
                         "end\n");

            using var script = context.CompileScript(
                "loop do\n" +
                "  log(state[:counter].to_s)\n" +
                "  c = state[:counter].to_i\n" +
                "  with(:Bob) do\n" +
                $"    text \"Hello #{{c}} calculated: {h}\"\n" +
                "  end\n" +
                "end\n");

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

