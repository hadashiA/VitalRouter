using Microsoft.Extensions.Hosting;

namespace VitalRouter.Extensions.Hosting.Tests;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddVitalRouter();
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}