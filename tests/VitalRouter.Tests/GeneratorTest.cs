using System;
using System.Linq;
using NUnit.Framework;
using VitalRouter.SourceGenerator;

namespace VitalRouter.Tests;

[TestFixture]
public class GeneratorTest
{
    const string Step1 = """
        using VitalRouter;

        struct CommandA : ICommand { }

        [Routes]
        partial class FooPresenter
        {
            [Route]
            void On(CommandA cmd) { }
        }
        """;

    // Same [Route] signature as Step1, only the method body changed.
    const string Step2 = """
        using VitalRouter;

        struct CommandA : ICommand { }

        [Routes]
        partial class FooPresenter
        {
            [Route]
            void On(CommandA cmd)
            {
                System.Console.WriteLine(cmd);
            }
        }
        """;

    // The [Route] signature changed (sync void -> async ValueTask + CancellationToken).
    const string Step3 = """
        using VitalRouter;
        using System.Threading;
        using System.Threading.Tasks;

        struct CommandA : ICommand { }

        [Routes]
        partial class FooPresenter
        {
            [Route]
            async ValueTask On(CommandA cmd, CancellationToken cancellationToken)
            {
                System.Console.WriteLine(cmd);
            }
        }
        """;

    [Test]
    public void Incremental_BodyChange_IsCached_SignatureChange_Regenerates()
    {
        var results = TestHelper.GetIncrementalGeneratorTrackedStepsReasons(
            "VitalRouter.",
            new VitalRouterIncrementalSourceGenerator(),
            Step1, Step2, Step3);

        // Step 2 only changed a method body -> the model is value-equal, so neither
        // the transform nor the source output re-runs (Cached). The key guarantee is
        // that nothing regenerates.
        var step2 = results[1].ToDictionary(x => x.Key, x => x.Reasons);
        Assert.That(step2["RoutesProvider"], Does.Contain("Cached"));
        Assert.That(step2["SourceOutput"], Does.Contain("Cached"));
        Assert.That(step2["SourceOutput"], Does.Not.Contain("Modified"));

        // Step 3 changed the [Route] signature -> the model differs and the source
        // output must be regenerated (Modified or New), i.e. it must NOT be cached.
        var step3 = results[2].ToDictionary(x => x.Key, x => x.Reasons);
        Assert.That(step3["RoutesProvider"], Does.Contain("Modified").Or.Contain("New"));
        Assert.That(step3["SourceOutput"], Does.Contain("Modified").Or.Contain("New"));
        Assert.That(step3["SourceOutput"], Does.Not.Contain("Cached"));
    }
}
