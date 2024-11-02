using BenchmarkDotNet.Attributes;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using R3;
using VitalRouter;
using ZeroMessenger;

namespace VitalRouter.Benchmark;

[MemoryDiagnoser]
public class SubscribeDisposeBenchmark
{
    const int Count = 10000;

    IDisposable[] disposables = default!;

    MessagePipe.ISubscriber<TestMessage> messagePipeSubscriber = default!;
    ZeroMessenger.MessageBroker<TestMessage> zeroMessageBroker = default!;
    R3.Subject<TestMessage> r3Subject = default!;
    System.Reactive.Subjects.Subject<TestMessage> rxNetSubject = default!;
    Prism.Events.PubSubEvent<TestMessage> prismEvent = default!;
    VitalRouter.ICommandSubscribable vitalRouter = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMessagePipe();
        var provider = serviceCollection.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);
    }

    [IterationSetup]
    public void Setup()
    {
        disposables = new IDisposable[Count];
        zeroMessageBroker = new ZeroMessenger.MessageBroker<TestMessage>();
        messagePipeSubscriber = GlobalMessagePipe.GetSubscriber<TestMessage>();
        r3Subject = new();
        rxNetSubject = new();
        prismEvent = new EventAggregator().GetEvent<TestMessage>();
        vitalRouter = new Router();
    }

    [Benchmark(Description = "Subscribe (ZeroMessenger)")]
    public void Benchmark_ZeroMessenger()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = zeroMessageBroker.Subscribe(x => { });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }

    [Benchmark(Description = "Subscribe (MessagePipe)")]
    public void Benchmark_MessagePipe()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = messagePipeSubscriber.Subscribe(x => { });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }

    [Benchmark(Description = "Subscribe (R3 Subject)")]
    public void Benchmark_R3Subject()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = r3Subject.Subscribe(x => { });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }

    [Benchmark(Description = "Subscribe (System.Reactive Subject)")]
    public void Benchmark_RxNetSubject()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = rxNetSubject.Subscribe(x => { });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }

    [Benchmark(Description = "Subscribe (Prism)")]
    public void Benchmark_Prism()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = prismEvent.Subscribe(x => { });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }

    [Benchmark(Description = "Subscribe (VitalRouter)")]
    public void Benchmark_VitalRouter()
    {
        for (int i = 0; i < Count; i++)
        {
            disposables[i] = vitalRouter.Subscribe<TestMessage>((c, context) => {  });
        }
        for (int i = 0; i < Count; i++)
        {
            disposables[i].Dispose();
        }
    }
}