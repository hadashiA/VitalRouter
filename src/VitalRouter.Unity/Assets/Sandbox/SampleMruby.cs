using System;
using Cysharp.Threading.Tasks;
using MRubyCS;
using MRubyCS.Compiler;
using MRubyCS.Serializer;
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
            var state = MRubyState.Create();

            state.AddVitalRouter(x =>
            {
                x.AddCommand<TextCommand>("text");
            });

            var compiler = MRubyCompiler.Create(state);

            compiler.LoadSourceCode("def hoge(x) = x * 100");
            var result1 = compiler.LoadSourceCode("hoge(7)");
            UnityEngine.Debug.Log(result1);

            compiler.LoadSourceCode(
                "class CharacterContext\n" +
                "  def initialize(id)\n" +
                "    @id = id\n" +
                "  end\n" +
                "  \n" +
                "  def text(body)\n" +
                "    log(body)\n" +
                "    cmd :text, id: @id, body:\n" +
                "  end\n" +
                "end\n" +
                "\n" +
                "def with(id, &block)\n" +
                "  CharacterContext.new(id).instance_eval(&block)\n" +
                "end\n");

            var irep = compiler.Compile(
                "3.times do |x|\n" +
                "  log x\n" +
                "  c = state[:counter].to_i\n" +
                "  with(:Bob) do\n" +
                $"    text \"Hello #{{c}}\"\n" +
                "  end\n" +
                "end\n" +
                "log 'owari'\n" +
                "\n");

            MapTo(Router.Default);

            await state.ExecuteAsync(Router.Default, irep, destroyCancellationToken);
            UnityEngine.Debug.Log("OK");
        }

        public async UniTask On(TextCommand cmd, PublishContext ctx)
        {
            ctx.MRubySharedVariables()!.Set("counter", ++counter);

            label.text = cmd.Body;

            await button.OnClickAsync();
        }
    }
}

