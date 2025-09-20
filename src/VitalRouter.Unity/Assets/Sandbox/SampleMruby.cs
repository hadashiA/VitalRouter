using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MRubyCS;
using MRubyCS.Compiler;
using MRubyCS.Serializer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValueTaskSupplement;
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

    [Routes]
    public partial class SampleMruby : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI label = default!;

        [SerializeField]
        Button button = default!;

        int counter;

        async UniTaskVoid Start()
        {
            var mrb = MRubyState.Create();
            using var compiler = MRubyCompiler.Create(mrb);

            mrb.DefineVitalRouter(x =>
            {
                x.AddCommand<TextCommand>("text");
            });

            mrb.DefineMethod(mrb.ObjectClass, mrb.Intern("log"), (_, self) =>
            {
                var inspect = mrb.Stringify(mrb.GetArgumentAt(0));
                UnityEngine.Debug.Log(inspect.ToString());
                return MRubyValue.Nil;
            });

            compiler.LoadSourceCode(
                "class CharacterContext\n" +
                "  def initialize(id)\n" +
                "    @id = id\n" +
                "  end\n" +
                "  \n" +
                "  def text(body)\n" +
                "    log body\n" +
                "    cmd :text, id: @id, body:\n" +
                "  end\n" +
                "end\n" +
                "\n" +
                "def with(id, &block)\n" +
                "  CharacterContext.new(id).instance_eval(&block)\n" +
                "end\n");

            var irep = compiler.Compile(
                "loop do\n" +
                "  c = state[:counter].to_i\n" +
                "  with(:Bob) do\n" +
                $"    text \"Hello #{{c}}\"\n" +
                "  end\n" +
                "end\n" +
                "log 'owari'\n" +
                "\n");

            MapTo(Router.Default);

            await mrb.ExecuteAsync(Router.Default, irep, destroyCancellationToken);
            UnityEngine.Debug.Log("End Script");
        }

        public async UniTask On(TextCommand cmd, PublishContext ctx)
        {
            UnityEngine.Debug.Log($"On TextCommand {cmd.Id} {cmd.Body}");
            ctx.MRubySharedVariables()!.Set("counter", ++counter);

            label.text = cmd.Body;

            await button.OnClickAsync(ctx.CancellationToken);
        }
    }
}