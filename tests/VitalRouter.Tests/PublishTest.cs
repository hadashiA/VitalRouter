using System.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

[TestFixture]
public class PublishTest
{
    readonly Router router = new();

    [Test]
    public async Task PublishNonGeneric()
    {
        ICommand cmd = new CommandA(111);

        CommandA? routed = null;
        router.Subscribe<CommandA>((x, ctx) =>
        {
            routed = x;
        });

        await router.PublishAsync(cmd.GetType(), cmd);

        Assert.That(routed, Is.EqualTo(cmd));
    }
}