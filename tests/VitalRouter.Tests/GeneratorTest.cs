// using NUnit.Framework;
// using VitalRouter.SourceGenerator;
//
// namespace VitalRouter.Tests;
//
// [TestFixture]
// public class GeneratorTest
// {
//     [Test]
//     public void VitalRouterIncrementalSourceGenerator_Incremental()
//     {
//         var step1 = $$"""
//                       using VitalRouter;
//
//                       struct CommandA : ICommand { }
//
//                       [Routes]
//                       partial class FooPresenter
//                       {
//                           [Route]
//                           void On(CommandA cmd) { }
//                       }
//                       """;
//
//         var step2 = $$"""
//                       using VitalRouter;
//
//                       struct CommandA : ICommand { }
//
//                       [Routes]
//                       partial class FooPresenter
//                       {
//                           [Route]
//                           void On(CommandA cmd)
//                           {
//                               System.Console.WriteLine(cmd);
//                           }
//                       }
//                       """;
//
//         var step3 = $$"""
//                       using VitalRouter;
//                       using System.Threading.Tasks;
//
//                       struct CommandA : ICommand { }
//
//                       [Routes]
//                       partial class FooPresenter
//                       {
//                           [Route]
//                           async ValueTask On(CommandA cmd, CancellationToken cancellationToken)
//                           {
//                               System.Console.WriteLine(cmd);
//                           }
//                       }
//                       """;
//
//         var result = TestHelper.GetIncrementalGeneratorTrackedStepsReasons(
//             "VitalRouter.",
//             new VitalRouterIncrementalSourceGenerator(),
//             step1, step2, step3);
//
//         Assert.That(result, Is.EqualTo(new object()));
//     }
// }