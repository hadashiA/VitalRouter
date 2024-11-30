---
sidebar_label: Microsoft.Extensions
title: Microsoft.Extensions.DependencyInjection
---

[Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-usage) is a standard DI implementation developed in the dotnet/runtime repository, and is also an abstract layer for DI. It is very common in .NET environments, including server-side.

By installing the [VitalRouter.Extensions.DependencyInjection](https://www.nuget.org/packages/VitalRouter.Extensions.DependencyInjection) package, you can use the following features that use VitalRouter from Microsoft.Extensions.DependencyInjection.

```cs
using VitalRouter;

// Example of using Generic Host
var builder = Host.CreateApplicationBuilder();

builder.Services.AddVitalRouter(routing =>
{
    // Map all `[Routes]` targets in the specified assembly.
    routing.MapAll(GetType().Assembly);
    
    // Map specific class.
    routing.Map<FooPresenter>();
});
```

### Resolving

The instances mapped here are released with to dispose of the DI container.

In this case, publisher is also injectable.

## Resolving

If `AddVitalRouter` is set, `Router`, its interface, `ICommandPublisher` and `ICommandSubscriber` can be retrieved from the DI container.

```cs
class HogeController
{
    readonly ICommandPublisher publisher;
    
    // Resolve `ICommandPublisher`
    public HogeController(ICommandPublisher publisher)
    {
        this.publisher = publisher;
    }

    public void DoSomething()
    {
        publisher.PublishAsync(new FooCommand { X = 1, Y = 2 }).Forget();
    }
}
```

```cs
public class HogePresenter
{
    // Resolve `ICommandSubscribable`
    public FooController(ICommandSubscribable subscribable)
    {
        subscribable.Subscribe((cmd, ctx) => { /* ... */ });
    }
}
```
### Ordering

By setting the `Ordering` property, you can set the order control for the Routers you register here.

```cs
builder.Services.AddVitalRouter(routing =>
{
    // ...
    routing.Ordering = CommandOrdering.Sequential;
    // ...
});			
```

For more information about `CommandOrdering`, please refer to the [Sequential control](../pipeline/sequential-control) section.

### Filters

You can use the `Filters` property to add `Interceptors` that apply to this scope.

```cs
builder.Services.AddVitalRouter(routing =>
{
    // ...
    routing.Filters.Add<Filter1>();
    // ...
});			
```

It is convenient to set up the Filter via DI, as this automates the instantiation of the Interceptor and dependency resolution.

For more information about Interceptors, please refer to the [Interceptor](../pipeline/interceptor) section.
