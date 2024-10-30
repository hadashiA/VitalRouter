using MediatR;

namespace VitalRouter.Benchmark;

public class TestMessage : PubSubEvent<TestMessage>, INotification, ICommand;