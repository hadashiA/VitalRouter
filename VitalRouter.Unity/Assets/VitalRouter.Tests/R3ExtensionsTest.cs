#if VITALROUTER_R3_INTEGRATION
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using VitalRouter.R3;

namespace VitalRouter.Tests;

[TestFixture]
public class R3ExtensionsTest
{
    [Test]
    public void SubscribeToPublish()
    {
        var subscriber = new TestSubscriber();

        var router = new Router();
        router.Subscribe(subscriber);

        Observable.Range(0, 3)
            .Select(x => new TestCommand(x))
            .SubscribeToPublish(router);

        Assert.That(subscriber.Queue.Dequeue().Value, Is.EqualTo(0));
        Assert.That(subscriber.Queue.Dequeue().Value, Is.EqualTo(1));
        Assert.That(subscriber.Queue.Dequeue().Value, Is.EqualTo(2));
    }

    [Test]
    public async Task AsObservable()
    {
        var result = new Queue<TestCommand>();

        var router = new Router();
        router.AsObservable<TestCommand>()
            .Subscribe(result, (cmd, r) => r.Enqueue(cmd));

        await router.PublishAsync(new TestCommand(100));
        await router.PublishAsync(new TestCommand(200));
        await router.PublishAsync(new TestCommand(300));

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Dequeue().Value, Is.EqualTo(100));
        Assert.That(result.Dequeue().Value, Is.EqualTo(200));
        Assert.That(result.Dequeue().Value, Is.EqualTo(300));
    }

    [Test]
    public async Task ForEachPublishAndForget()
    {
        var subscriber = new TestSubscriber();
        var router = new Router();

        router.Subscribe(subscriber);

        var result = new Queue<TestCommand>();

        await Observable.Range(0, 5)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndForgetAsync(router);

        Assert.That(subscriber.Queue.Count, Is.EqualTo(5));
    }

    [Test]
    public async Task ForEachPublishAndForget_Cancel()
    {
        var fakeTimeProvider = new FakeFrameProvider();
        var cts = new CancellationTokenSource();

        var subscriber = new TestSubscriber();
        var router = new Router();
        router.Subscribe(subscriber);

        var task = Observable.Range(0, 10)
            .DelayFrame(1, fakeTimeProvider)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndForgetAsync(router, cts.Token);

        Assert.That(subscriber.Queue.Count, Is.EqualTo(0));

        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Test]
    public async Task ForEachPublishAndAwait()
    {
        var cts = new CancellationTokenSource();
        var frameProvider = new FakeFrameProvider();
        var subscriber = new TestAsyncSubscriber(frameProvider);
        var router = new Router(CommandOrdering.FirstInFirstOut);

        router.Subscribe(subscriber);

        var task = Observable.Range(0, 3)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndForgetAsync(router, cts.Token);

        Assert.That(subscriber.Queue.Count, Is.EqualTo(1));
        frameProvider.Advance(1);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(2));
        frameProvider.Advance(1);

        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ForEachPublishAndAwait_Cancel()
    {

        var frameProvider = new FakeFrameProvider();
        var subscriber = new TestAsyncSubscriber(frameProvider);
        var router = new Router(CommandOrdering.FirstInFirstOut);

        router.Subscribe(subscriber);

        var task = Observable.Range(0, 3)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndForgetAsync(router);

        Assert.That(subscriber.Queue.Count, Is.EqualTo(1));
        frameProvider.Advance(1);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(2));
        frameProvider.Advance(1);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(3));
        frameProvider.Advance(1);

        await task;
    }

    class TestSubscriber : ICommandSubscriber
    {
        public Queue<TestCommand> Queue { get; } = new();

        public void Receive<T>(T command) where T : ICommand
        {
            Queue.Enqueue((command as TestCommand)!);
        }
    }

    class TestAsyncSubscriber : IAsyncCommandSubscriber
    {
        public Queue<object> Queue { get; } = new();
        readonly FrameProvider frameProvider;

        public TestAsyncSubscriber(FrameProvider frameProvider)
        {
            this.frameProvider = frameProvider;
        }

        public UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
        {
            Queue.Enqueue(command);
            var tcs = new UniTaskCompletionSource();
            frameProvider.Register(new OneShot(tcs));
            return tcs.Task;
        }

        class OneShot : IFrameRunnerWorkItem
        {
            readonly UniTaskCompletionSource source;

            public OneShot(UniTaskCompletionSource source)
            {
                this.source = source;
            }

            public bool MoveNext(long frameCount)
            {
                source.TrySetResult();
                return false;
            }
        }
    }

    class TestCommand : ICommand
    {
        public int Value { get; set; }

        public TestCommand(int value)
        {
            Value = value;
        }
    }
}
#endif