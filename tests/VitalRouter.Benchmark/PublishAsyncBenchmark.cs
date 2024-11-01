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
public class PublishAsyncBenchmark
{
    const int SubscribeCount = 8;

    MessagePipe.IAsyncPublisher<TestMessage> messagePipePublisher = default!;
    MessagePipe.IAsyncSubscriber<TestMessage> messagePipeSubscriber = default!;

    ZeroMessenger.MessageBroker<TestMessage> zeroMessengerBroker = default!;

    PubSubEvent<TestMessage> prismEvent = default!;
    VitalRouter.Router vitalRouter = default!;

    MediatR.IMediator mediatRMediator = default!;
    PubSub.Hub pubsubHub = default!;

    TestMessage message = new();

    [IterationSetup]
    public void Setup()
    {
        static ValueTask Method(int i)
        {
            return default;
        }

        zeroMessengerBroker = new ZeroMessenger.MessageBroker<TestMessage>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMessagePipe();
        serviceCollection.AddMediatR(x => x.RegisterServicesFromAssembly(GetType().Assembly));
        var provider = serviceCollection.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        messagePipePublisher = GlobalMessagePipe.GetAsyncPublisher<TestMessage>();
        messagePipeSubscriber = GlobalMessagePipe.GetAsyncSubscriber<TestMessage>();

        prismEvent = new Prism.Events.EventAggregator().GetEvent<TestMessage>();
        vitalRouter = new VitalRouter.Router();
        mediatRMediator = provider.GetRequiredService<IMediator>();
        pubsubHub = new();

        for (int i = 0; i < SubscribeCount; i++)
        {
            zeroMessengerBroker.Subscribe(i, (x, state) => Method(state));
            messagePipeSubscriber.Subscribe((x, c) => Method(i));
            prismEvent.Subscribe(x => Method(i));
            vitalRouter.SubscribeAwait<TestMessage>((c, context) => Method(i));
            pubsubHub.Subscribe<TestMessage>(x => Method(i));
        }
    }

    [Benchmark(Description = "Publish (ZeroMessenger)")]
    public async Task Benchmark_ZeroMessenger()
    {
        await zeroMessengerBroker.PublishAsync(message);
    }

    [Benchmark(Description = "Publish (MessagePipe)")]
    public async Task Benchmark_MessagePipe()
    {
        await messagePipePublisher.PublishAsync(message);
    }

    [Benchmark(Description = "Publish (VitalRouter)")]
    public async Task Benchmark_VitalRouter()
    {
        await vitalRouter.PublishAsync(message);
    }

    [Benchmark(Description = "Publish (MediatR)")]
    public async Task Benchmark_MediatR()
    {
        await mediatRMediator.Publish(message);
    }

    [Benchmark(Description = "Publish (PubSub)")]
    public async Task Benchmark_PubSub()
    {
        await pubsubHub.PublishAsync(message);
    }
}