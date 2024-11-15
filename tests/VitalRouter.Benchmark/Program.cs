using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using VitalRouter;
using VitalRouter.Benchmark;

var router = new Router();
router.Subscribe<TestMessage>((x, ctx) => { });
router.Subscribe<TestMessage>((x, ctx) => { });
router.Subscribe<TestMessage>((x, ctx) => { });
router.Subscribe<TestMessage>((x, ctx) => { });

for (var i = 0; i < 100; i++)
{
    _ = router.PublishAsync(new TestMessage());
}

MeasureProfiler.StartCollectingData();
for (var i = 0; i < 100; i++)
{
    _ = router.PublishAsync(new TestMessage());
}
MeasureProfiler.SaveData();

BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()!).Run(args);

class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.ShortRun
            .WithWarmupCount(5)
            .WithIterationCount(50));
    }
}