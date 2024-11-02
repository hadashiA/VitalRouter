using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VitalRouter.Tests;

static class TestHelper
{
    static readonly object SyncRoot = new();
    static Compilation? baseCompilation;

    static Compilation GetCompilation()
    {
        lock (SyncRoot)
        {
            if (baseCompilation == null)
            {
                // running .NET Core system assemblies dir path
                var baseAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                var systemAssemblies = Directory.GetFiles(baseAssemblyPath)
                    .Where(x =>
                    {
                        var fileName = Path.GetFileName(x);
                        if (fileName.EndsWith("Native.dll")) return false;
                        return fileName.StartsWith("System") ||
                               fileName is "mscorlib.dll" or "netstandard.dll";
                    });

                var references = systemAssemblies
                    .Append(typeof(Router).Assembly.Location) // + VitalRouter.dll
                    .Select(x => MetadataReference.CreateFromFile(x))
                    .ToArray();

                baseCompilation = CSharpCompilation.Create("TestProject",
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
            return baseCompilation;
        }
    }

    public static IReadOnlyList<Diagnostic> RunGenerator(
        string source,
        IIncrementalGenerator generator,
        string[]? preprocessorSymbols = null,
        AnalyzerConfigOptionsProvider? options = null)
    {
        if (preprocessorSymbols == null)
        {
            preprocessorSymbols = ["NET8_0_OR_GREATER"];
        }

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp10, preprocessorSymbols: preprocessorSymbols);

        var driver = CSharpGeneratorDriver.Create(generator).WithUpdatedParseOptions(parseOptions);
        if (options != null)
        {
            driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(options);
        }

        var compilation = GetCompilation().AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return newCompilation.GetDiagnostics();
    }

    public static (string Key, string Reasons)[][] GetIncrementalGeneratorTrackedStepsReasons(
        string keyPrefixFilter,
        IIncrementalGenerator generator,
        params string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp10);
        var driver = CSharpGeneratorDriver.Create(
                [generator.AsSourceGenerator()],
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true))
            .WithUpdatedParseOptions(parseOptions);

        var generatorResults = sources
            .Select(source =>
            {
                var compilation = GetCompilation().AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));
                driver = driver.RunGenerators(compilation);
                return driver.GetRunResult().Results[0];
            })
            .ToArray();

        var reasons = generatorResults
            .Select(x => x.TrackedSteps
                .Where(x => x.Key.StartsWith(keyPrefixFilter) || x.Key == "SourceOutput")
                .Select(x =>
                {
                    if (x.Key == "SourceOutput")
                    {
                        var values = x.Value.Where(x => x.Inputs[0].Source.Name?.StartsWith(keyPrefixFilter) ?? false);
                        return (
                            x.Key,
                            Reasons: string.Join(", ", values.SelectMany(x => x.Outputs).Select(x => x.Reason).ToArray())
                        );
                    }

                    return (
                        Key: x.Key[keyPrefixFilter.Length..],
                        Reasons: string.Join(", ", x.Value.SelectMany(x => x.Outputs).Select(x => x.Reason).ToArray())
                    );
                })
                .OrderBy(x => x.Key)
                .ToArray())
            .ToArray();

        return reasons;
    }
}