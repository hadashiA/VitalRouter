using BenchmarkDotNet.Attributes;
using MediatR;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using R3;
using VitalRouter;
using ZeroMessenger;

using VitalRouter.Benchmark;

[MemoryDiagnoser]
[InvocationCount(10000)]
public class PublishBenchmark
{
    const int SubscribeCount = 8;

    MessagePipe.IPublisher<TestMessage> messagePipePublisher = default!;
    MessagePipe.ISubscriber<TestMessage> messagePipeSubscriber = default!;

    ZeroMessenger.MessageBroker<TestMessage> zeroMessengerBroker = default!;

    R3.Subject<TestMessage> r3Subject = default!;
    System.Reactive.Subjects.Subject<TestMessage> rxNetSubject = default!;

    PubSubEvent<TestMessage> prismEvent = default!;
    VitalRouter.Router vitalRouter = default!;

    MediatR.IMediator mediatRMediator = default!;
    PubSub.Hub pubsubHub = default!;

    TestMessage message = new();

    [IterationSetup]
    public void Setup()
    {
        static void Method(int i)
        {

        }

        zeroMessengerBroker = new ZeroMessenger.MessageBroker<TestMessage>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMessagePipe();
        serviceCollection.AddMediatR(x => x.RegisterServicesFromAssembly(GetType().Assembly));
        var provider = serviceCollection.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        messagePipePublisher = GlobalMessagePipe.GetPublisher<TestMessage>();
        messagePipeSubscriber = GlobalMessagePipe.GetSubscriber<TestMessage>();

        r3Subject = new();
        rxNetSubject = new();
        prismEvent = new Prism.Events.EventAggregator().GetEvent<TestMessage>();
        vitalRouter = new VitalRouter.Router();
        mediatRMediator = provider.GetRequiredService<IMediator>();
        pubsubHub = new();

        for (int i = 0; i < SubscribeCount; i++)
        {
            zeroMessengerBroker.Subscribe(i, (x, state) => Method(state));
            messagePipeSubscriber.Subscribe(x => Method(i));
            rxNetSubject.Subscribe(x => Method(i));
            r3Subject.Subscribe(i, (x, state) => Method(state));
            prismEvent.Subscribe(x => Method(i));
            vitalRouter.Subscribe<TestMessage>((c, context) => Method(i));
            pubsubHub.Subscribe<TestMessage>(x => Method(i));
        }
    }

    [Benchmark(Description = "Publish (ZeroMessenger)")]
    public void Benchmark_ZeroMessenger()
    {
        zeroMessengerBroker.Publish(message);
    }

    [Benchmark(Description = "Publish (MessagePipe)")]
    public void Benchmark_MessagePipe()
    {
        messagePipePublisher.Publish(message);
    }

    [Benchmark(Description = "Publish (System.Reactive Subject)")]
    public void Benchmark_RxNetSubject()
    {
        rxNetSubject.OnNext(message);
    }

    [Benchmark(Description = "Publish (R3 Subject)")]
    public void Benchmark_R3Subject()
    {
        r3Subject.OnNext(message);
    }

    [Benchmark(Description = "Publish (Prism)")]
    public void Benchmark_Prism()
    {
        prismEvent.Publish(message);
    }

    [Benchmark(Description = "Publish (VitalRouter)")]
    public void Benchmark_VitalRouter()
    {
        // sync subscribers only
        vitalRouter.PublishAsync(message);
    }

    [Benchmark(Description = "Publish (MediatR)")]
    public void Benchmark_MediatR()
    {
        mediatRMediator.Publish(message).Wait();
    }

    [Benchmark(Description = "Publish (PubSub)")]
    public void Benchmark_PubSub()
    {
        pubsubHub.Publish(message);
    }
}