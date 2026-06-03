using System;
using System.IO;
using ChibiRuby;
using NUnit.Framework;

namespace VitalRouter.MRuby.Tests;

[TestFixture]
public class RbsGeneratorTest
{
    MRubyState mrb = default!;
    string path = default!;

    [SetUp]
    public void SetUp()
    {
        mrb = MRubyState.Create();
        path = Path.Combine(Path.GetTempPath(), $"vitalrouter_{Guid.NewGuid():N}.rbs");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(path)) File.Delete(path);
    }

    // Exports the RBS to a temp file via the public API and returns its contents.
    string ExportRbs(Action<VitalRouterDefinition> configure)
    {
        mrb.DefineVitalRouter(x =>
        {
            configure(x);
            x.ExportRbsTo(path);
        });
        return File.ReadAllText(path);
    }

    [Test]
    public void ExportRbs_BasicCommands()
    {
        var rbs = ExportRbs(x =>
        {
            x.AddCommand<TestCommand>("test");
            x.AddCommand<MoveCommand>("move");
        });

        Assert.That(rbs, Does.Contain("class Object"));
        Assert.That(rbs, Does.Contain("def cmd: (:test, ?value: Integer) -> void"));
        Assert.That(rbs, Does.Contain("| (:move, ?id: String, ?x: Integer, ?y: Integer) -> void"));
    }

    [Test]
    public void ExportRbs_NoCommands()
    {
        var rbs = ExportRbs(_ => { });

        Assert.That(rbs, Does.Contain("def cmd: (Symbol, **untyped) -> void"));
    }

    [Test]
    public void ExportRbs_TypeMapping()
    {
        var rbs = ExportRbs(x => x.AddCommand<RichCommand>("rich"));

        Assert.That(rbs, Does.Contain("?int_value: Integer"));
        Assert.That(rbs, Does.Contain("?float_value: Float"));
        Assert.That(rbs, Does.Contain("?bool_flag: bool"));
        Assert.That(rbs, Does.Contain("?name: String"));
        Assert.That(rbs, Does.Contain("?facing: Symbol"));               // enum -> Symbol
        Assert.That(rbs, Does.Contain("?numbers: Array[Integer]"));       // array
        Assert.That(rbs, Does.Contain("?tags: Array[String]"));           // List<T>
        Assert.That(rbs, Does.Contain("?scores: Hash[String, Integer]")); // Dictionary<K,V>
        Assert.That(rbs, Does.Contain("?nullable: Integer?"));            // Nullable<T>
        Assert.That(rbs, Does.Contain("?aliased_key: Integer"));          // [MRubyMember]
        Assert.That(rbs, Does.Not.Contain("ignored"));                    // [MRubyIgnore]
    }

    [Test]
    public void ExportRbsTo_CreatesMissingDirectory()
    {
        var nested = Path.Combine(Path.GetTempPath(), $"vitalrouter_{Guid.NewGuid():N}", "sig", "vitalrouter.rbs");
        try
        {
            mrb.DefineVitalRouter(x => x.AddCommand<TestCommand>("test").ExportRbsTo(nested));

            Assert.That(File.Exists(nested), Is.True);
            Assert.That(File.ReadAllText(nested), Does.Contain("def cmd: (:test, ?value: Integer) -> void"));
        }
        finally
        {
            if (File.Exists(nested)) File.Delete(nested);
        }
    }
}
