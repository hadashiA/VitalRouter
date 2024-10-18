#if UNITY_2022_3_OR_NEWER
using System.Threading.Tasks;
using NUnit.Framework;
using VitalRouter.MRuby;

namespace VitalRouter.Tests
{
    [MRubyObject]
    partial class StateCommand : ICommand
    {
        public bool BoolValue { get; set; }
        public int IntValue { get; set; }
        public string StringValue { get; set; } = default!;
    }

    [MRubyCommand("state", typeof(StateCommand))]
    partial class TestCommandPreset : MRubyCommandPreset {}

    [TestFixture]
    public class MRubyScriptTest
    {
        [Test]
        public async Task Subscribers()
        {
            var router = new Router();
            var commandPreset = new TestCommandPreset();
            var ctx = MRubyContext.Create(router, commandPreset);
            ctx.SharedState.Set("b", true);
            ctx.SharedState.Set("i", 123);
            ctx.SharedState.Set("s", "hoge moge");

            var script = ctx.CompileScript(
                "cmd :state,\n" +
                "  intValue: state[:i],\n" +
                "  stringValue: state[:s],\n" +
                "  boolValue: state[:b]\n");

            var stateCommand = default(StateCommand);
            router.Subscribe<StateCommand>((cmd, ctx) =>
            {
                stateCommand = cmd;
                var b = ctx.MRubySharedState()!.GetOrDefault<bool>("b");
                ctx.MRubySharedState()?.Set("b", !b);
            });

            await script.RunAsync();

            script.Dispose();
            script.Dispose();
            script.Dispose();

            Assert.That(stateCommand!.BoolValue, Is.True);
            Assert.That(stateCommand!.IntValue, Is.EqualTo(123));
            Assert.That(stateCommand!.StringValue, Is.EqualTo("hoge moge"));
        }

        [Test]
        public void CatchSynctaxError()
        {
            var router = new Router();
            var commandPreset = new TestCommandPreset();
            var ctx = MRubyContext.Create(router, commandPreset);

            Assert.Throws<MRubyScriptCompileException>(() => ctx.CompileScript("1 1"));
        }

        [Test]
        public void CatchRuntimeError()
        {
            var router = new Router();
            var commandPreset = new TestCommandPreset();
            var ctx = MRubyContext.Create(router, commandPreset);

            var script = ctx.CompileScript( "raise 'hoge fuga'");
            Assert.ThrowsAsync<MRubyScriptException>(async () => await script.RunAsync());
        }

        [Test]
        public void CatchEvaluateError()
        {
            var ctx = MRubyContext.Create();
            Assert.Throws<MRubyScriptException>(() => ctx.Load( "raise 'hoge fuga'"));
        }
    }
}
#endif