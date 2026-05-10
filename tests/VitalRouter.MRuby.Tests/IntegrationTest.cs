using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MRubyCS;
using MRubyCS.Compiler;
using NUnit.Framework;

namespace VitalRouter.MRuby.Tests;

[TestFixture]
public class IntegrationTest
{
    MRubyState mrb = default!;
    MRubyCompiler compiler = default!;
    Router router = default!;
    TestCommandRecorder recorder = default!;

    [SetUp]
    public void SetUp()
    {
        mrb = MRubyState.Create();
        compiler = MRubyCompiler.Create(mrb);
        router = new Router();
        recorder = new TestCommandRecorder();
        router.Subscribe(recorder);

        mrb.DefineVitalRouter(x =>
        {
            x.AddCommand<TestCommand>("test");
            x.AddCommand<MoveCommand>("move");
        });
    }

    [TearDown]
    public void TearDown()
    {
        compiler.Dispose();
        router.Dispose();
    }

    [Test]
    public async Task ExecuteSingleCommand()
    {
        var irep = compiler.Compile("cmd :test, value: 42");
        await mrb.ExecuteAsync(router, irep);

        Assert.That(recorder.Received, Has.Count.EqualTo(1));
        var cmd = (TestCommand)recorder.Received[0];
        Assert.That(cmd.Value, Is.EqualTo(42));
    }

    [Test]
    public async Task ExecuteMultipleCommands()
    {
        var script = @"
cmd :test, value: 1
cmd :move, id: 'hero', x: 10, y: 20
cmd :test, value: 2
";
        var irep = compiler.Compile(script);
        await mrb.ExecuteAsync(router, irep);

        Assert.That(recorder.Received, Has.Count.EqualTo(3));

        var first = (TestCommand)recorder.Received[0];
        Assert.That(first.Value, Is.EqualTo(1));

        var move = (MoveCommand)recorder.Received[1];
        Assert.That(move.Id, Is.EqualTo("hero"));
        Assert.That(move.X, Is.EqualTo(10));
        Assert.That(move.Y, Is.EqualTo(20));

        var last = (TestCommand)recorder.Received[2];
        Assert.That(last.Value, Is.EqualTo(2));
    }

    [Test]
    public async Task SharedStateReadWrite()
    {
        // Ruby side writes, C# side reads
        var writeScript = @"
state[:greeting] = 'hello'
state[:count] = 99
";
        var irep = compiler.Compile(writeScript);
        await mrb.ExecuteAsync(router, irep);

        var sharedVars = mrb.GetSharedVariables();
        Assert.That(sharedVars.GetOrDefault<string>("greeting"), Is.EqualTo("hello"));
        Assert.That(sharedVars.GetOrDefault<int>("count"), Is.EqualTo(99));

        // C# side writes, Ruby side reads via cmd handler
        sharedVars.Set("from_csharp", 777);

        var readScript = @"
cmd :test, value: state[:from_csharp]
";
        var irep2 = compiler.Compile(readScript);
        await mrb.ExecuteAsync(router, irep2);

        Assert.That(recorder.Received, Has.Count.EqualTo(1));
        var cmd = (TestCommand)recorder.Received[0];
        Assert.That(cmd.Value, Is.EqualTo(777));
    }

    [Test]
    public async Task SharedStateToS()
    {
        var script = @"
state[:greeting] = 'hello'
state[:count] = 99
cmd :test, value: 0, name: state.to_s
";
        var irep = compiler.Compile(script);
        await mrb.ExecuteAsync(router, irep);

        Assert.That(recorder.Received, Has.Count.EqualTo(1));
        var cmd = (TestCommand)recorder.Received[0];
        Assert.That(cmd.Name, Is.EqualTo("{greeting: \"hello\", count: 99}"));
    }

    [Test]
    public async Task SharedStateInspect()
    {
        var script = @"
state[:greeting] = 'hello'
state[:count] = 99
cmd :test, value: 0, name: state.inspect
";
        var irep = compiler.Compile(script);
        await mrb.ExecuteAsync(router, irep);

        Assert.That(recorder.Received, Has.Count.EqualTo(1));
        var cmd = (TestCommand)recorder.Received[0];
        Assert.That(cmd.Name, Is.EqualTo("{greeting: \"hello\", count: 99}"));
    }

    [Test]
    public async Task SharedStateToSEmpty()
    {
        var script = @"
cmd :test, value: 0, name: state.to_s
";
        var irep = compiler.Compile(script);
        await mrb.ExecuteAsync(router, irep);

        Assert.That(recorder.Received, Has.Count.EqualTo(1));
        var cmd = (TestCommand)recorder.Received[0];
        Assert.That(cmd.Name, Is.EqualTo("{}"));
    }

    [Test]
    public async Task PublishContextHasMRubyState()
    {
        MRubyState? capturedState = null;
        MRubySharedVariableTable? capturedVars = null;

        var contextCapture = new ContextCaptureSubscriber(ctx =>
        {
            capturedState = ctx.MRuby();
            capturedVars = ctx.MRubySharedVariables();
        });
        router.Subscribe(contextCapture);

        var irep = compiler.Compile("cmd :test, value: 1");
        await mrb.ExecuteAsync(router, irep);

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedState, Is.SameAs(mrb));
        Assert.That(capturedVars, Is.Not.Null);
    }

    [Test]
    public void CommandNotFound()
    {
        var irep = compiler.Compile("cmd :unknown_command, value: 1");

        var ex = Assert.ThrowsAsync<MRubyRoutingException>(async () =>
        {
            await mrb.ExecuteAsync(router, irep);
        });
        Assert.That(ex!.Message, Does.Contain("unknown_command"));
    }

    [Test]
    public void CancellationStopsScript()
    {
        var cts = new CancellationTokenSource();

        // Subscribe a handler that cancels after receiving the first command
        var cancelOnFirst = new CancelOnFirstSubscriber(cts);
        router.Subscribe(cancelOnFirst);

        var script = @"
cmd :test, value: 1
cmd :test, value: 2
cmd :test, value: 3
";
        var irep = compiler.Compile(script);

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await mrb.ExecuteAsync(router, irep, cts.Token);
        });
    }

    [Test]
    public async Task ConcurrentScriptsOnSameRouter()
    {
        var mrb2 = MRubyState.Create();
        using var compiler2 = MRubyCompiler.Create(mrb2);

        mrb2.DefineVitalRouter(x =>
        {
            x.AddCommand<TestCommand>("test");
            x.AddCommand<MoveCommand>("move");
        });

        var irep1 = compiler.Compile(@"
cmd :test, value: 1
cmd :test, value: 2
cmd :test, value: 3
");
        var irep2 = compiler2.Compile(@"
cmd :move, id: 'npc', x: 100, y: 200
cmd :test, value: 99
");

        await Task.WhenAll(
            mrb.ExecuteAsync(router, irep1).AsTask(),
            mrb2.ExecuteAsync(router, irep2).AsTask()
        );

        var received = recorder.Received;
        Assert.That(received, Has.Count.EqualTo(5));

        var testValues = received.OfType<TestCommand>().Select(c => c.Value).OrderBy(v => v).ToList();
        Assert.That(testValues, Is.EqualTo(new[] { 1, 2, 3, 99 }));

        var moves = received.OfType<MoveCommand>().ToList();
        Assert.That(moves, Has.Count.EqualTo(1));
        Assert.That(moves[0].Id, Is.EqualTo("npc"));
        Assert.That(moves[0].X, Is.EqualTo(100));
        Assert.That(moves[0].Y, Is.EqualTo(200));
    }

    [Test]
    public async Task AsyncSubscriberAwaited()
    {
        // Subscriber that completes asynchronously with a delay.
        // If the fiber resumes before the handler finishes, the order would break.
        var order = new ConcurrentQueue<int>();
        var asyncSubscriber = new AsyncOrderRecorder(order);
        router.Subscribe(asyncSubscriber);

        var irep = compiler.Compile(@"
cmd :test, value: 1
cmd :test, value: 2
cmd :test, value: 3
");
        await mrb.ExecuteAsync(router, irep);

        Assert.That(order.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task AsyncSubscriberModifiesSharedState()
    {
        // Async handler writes to shared state; the next Ruby line reads it.
        // This proves the fiber waits for the async handler to complete.
        var sharedVars = mrb.GetSharedVariables();
        var asyncWriter = new AsyncSharedStateWriter(sharedVars);
        router.Subscribe(asyncWriter);

        var irep = compiler.Compile(@"
cmd :test, value: 0
cmd :test, value: state[:counter]
");
        await mrb.ExecuteAsync(router, irep);

        var received = recorder.Received;
        Assert.That(received, Has.Count.EqualTo(2));
        Assert.That(((TestCommand)received[1]).Value, Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrentAsyncScriptsOnSameRouter()
    {
        // Both scripts use async handlers (Task.Delay).
        // Script A is started first; while its first cmd is being handled asynchronously,
        // Script B starts and runs concurrently on the same Router.
        var order = new ConcurrentQueue<string>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Script A's handler: blocks on first cmd until gate is opened by Script B's handler
        var subscriberA = new GatedSubscriber("A", order, gate);
        var subscriberB = new GateOpenerSubscriber("B", order, gate);

        var mrb2 = MRubyState.Create();
        using var compiler2 = MRubyCompiler.Create(mrb2);

        mrb2.DefineVitalRouter(x =>
        {
            x.AddCommand<TestCommand>("test");
            x.AddCommand<MoveCommand>("move");
        });

        router.Subscribe(subscriberA);
        router.Subscribe(subscriberB);

        var irep1 = compiler.Compile(@"
cmd :test, value: 1
cmd :test, value: 2
");
        var irep2 = compiler2.Compile(@"
cmd :move, id: 'b', x: 0, y: 0
cmd :move, id: 'b', x: 1, y: 1
");

        var task1 = mrb.ExecuteAsync(router, irep1).AsTask();
        var task2 = mrb2.ExecuteAsync(router, irep2).AsTask();

        await Task.WhenAll(task1, task2);

        var events = order.ToArray();
        // B must have started and completed its first cmd while A was waiting on the gate.
        // So "B:start" appears before "A:end" for the first command.
        var bStartIndex = Array.IndexOf(events, "B:start");
        var aEndIndex = Array.IndexOf(events, "A:end");
        Assert.That(bStartIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(aEndIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(bStartIndex, Is.LessThan(aEndIndex),
            $"Expected B to start before A's first cmd finishes. Events: [{string.Join(", ", events)}]");
    }

    /// <summary>Waits for a gate before completing. Records "A:start" / "A:end".</summary>
    class GatedSubscriber(string tag, ConcurrentQueue<string> order, TaskCompletionSource gate) : IAsyncCommandSubscriber
    {
        public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            if (command is TestCommand)
            {
                order.Enqueue($"{tag}:start");
                await gate.Task;
                order.Enqueue($"{tag}:end");
            }
        }
    }

    /// <summary>Opens the gate on first cmd. Records "B:start" / "B:end".</summary>
    class GateOpenerSubscriber(string tag, ConcurrentQueue<string> order, TaskCompletionSource gate) : IAsyncCommandSubscriber
    {
        bool opened;

        public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            if (command is MoveCommand)
            {
                order.Enqueue($"{tag}:start");
                await Task.Delay(10);
                if (!opened)
                {
                    opened = true;
                    gate.TrySetResult();
                }
                order.Enqueue($"{tag}:end");
            }
        }
    }

    class AsyncOrderRecorder(ConcurrentQueue<int> order) : IAsyncCommandSubscriber
    {
        public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            await Task.Delay(50);
            if (command is TestCommand tc)
                order.Enqueue(tc.Value);
        }
    }

    class AsyncSharedStateWriter(MRubySharedVariableTable sharedVars) : IAsyncCommandSubscriber
    {
        int counter;

        public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            await Task.Delay(50);
            sharedVars.Set("counter", Interlocked.Increment(ref counter));
        }
    }

    class ContextCaptureSubscriber(Action<PublishContext> onReceive) : IAsyncCommandSubscriber
    {
        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            onReceive(context);
            return default;
        }
    }

    class CancelOnFirstSubscriber(CancellationTokenSource cts) : IAsyncCommandSubscriber
    {
        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            cts.Cancel();
            return default;
        }
    }
}
