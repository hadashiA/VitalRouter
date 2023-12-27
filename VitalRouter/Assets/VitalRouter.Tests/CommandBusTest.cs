using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

class TestCommandSubscriber : IAsyncCommandSubscriber, IImmediateCommandSubscriber
{
    public void Receive<T>(T command) where T : ICommand
    {
    }

    public async UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
    {
    }
}

class TestInterceptor
{
}

[TestFixture]
public class CommandBusTest
{
    public void Hoge()
    {
        var commandBus = new CommandBus();
        // commandBus.Subscribe()
    }
}