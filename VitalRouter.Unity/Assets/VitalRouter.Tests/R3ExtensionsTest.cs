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
        var router = new Router(CommandOrdering.FirstInFirstOut);
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
        var frameProvider = new FakeFrameProvider();
        var subscriber = new TestAsyncSubscriber(frameProvider);
        var router = new Router(CommandOrdering.FirstInFirstOut);

        router.Subscribe(subscriber);

        var task = Observable.Range(0, 3)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndAwaitAsync(router);

        Assert.That(subscriber.Queue.Count, Is.EqualTo(1));
        frameProvider.Advance(1);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(2));
        frameProvider.Advance(1);
        Assert.That(subscriber.Queue.Count, Is.EqualTo(3));

        await task;
    }

    [Test]
    public async Task ForEachPublishAndAwait_Cancel()
    {
        var cts = new CancellationTokenSource();
        var frameProvider = new FakeFrameProvider();
        var subscriber = new TestAsyncSubscriber(frameProvider);
        var router = new Router(CommandOrdering.FirstInFirstOut);

        router.Subscribe(subscriber);

        var task = Observable.Range(0, 3)
            .DelayFrame(1, frameProvider)
            .Select(x => new TestCommand(x))
            .ForEachPublishAndAwaitAsync(router, cts.Token);

        // Assert.That(subscriber.Queue.Count, Is.EqualTo(1));
        // frameProvider.Advance(1);
        // Assert.That(subscriber.Queue.Count, Is.EqualTo(2));
        // frameProvider.Advance(1);

        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

        await task;
    }

    class TestSubscriber : ICommandSubscriber
    {
        public Queue<TestCommand> Queue { get; } = new();

        public void Receive<T>(T command, PublishContext context) where T : ICommand
        {
            Queue.Enqueue((command as TestCommand)!);
        }
    }

    class TestAsyncSubscriber : IAsyncCommandSubscriber, IFrameRunnerWorkItem
    {
        public Queue<object> Queue { get; } = new();
        readonly FrameProvider frameProvider;

        UniTaskCompletionSource? tcs;

        public TestAsyncSubscriber(FrameProvider frameProvider)
        {
            this.frameProvider = frameProvider;
        }

        public UniTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            Queue.Enqueue(command);
            tcs = new UniTaskCompletionSource();
            frameProvider.Register(this);
            return tcs.Task;
        }

        public bool MoveNext(long frameCount)
        {
            tcs?.TrySetResult();
            return false;
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