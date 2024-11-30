---
sidebar_label: VContainer
title: VContainer integration
---

[VContainer](https://github.com/hadashiA/VContainer) is a popular DI library for Unity.

If VContainer is installed in your project, the `VITALROUTER_VCONTAINER_INTEGRATION` compiler switch will be automatically enabled, and the following functions will be available.  

## `RegisterVitalRouter`

The `RegisterVitalRouter` method is added to the IContainerBuilder of VContainer.

```cs
using VContainer;
using VitalRouter;
using VitalRouter.VContainer;

// VContaner's configuration
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterVitalRouter(routing =>
        {
            routing.Map<FooPresenter>(); // < Register routing plain class

            // Or, use MonoBehaviour instance with DI
            routing.MapComponent(instance);
            // Or, use MonoBehaviour in the scene
            routing.MapComponentInHierarchy<MyRoutesComponent>();
            // Or, use MonoBehaviour from prefab
            routing.MapComponentInNewPrefab(prefab);
        });			
    }
}
```

As in this example, the type with `[Routes]` is registered in the Action passed as an argument. The type specified here is registered in the DI container at the same time, and when the DI scope is created, MapTo is automatically called for the Router linked to the DI container, and when the scope is destroyed, Unmap is automatically called.

By default, when RegisterVitalRouter is called, `Router` is newly registered in the DI container.
If you want to use an existing Router instance (or Router.Default), register it as follows.


```cs
builder.RegisterInstance(Router.Default);

builder.RegisterVitalRouter(routing =>
{
    // ...
});			
```

### Ordering

By setting the `Ordering` property, you can set the order control for the Routers you register here.

```cs
builder.RegisterVitalRouter(routing =>
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
builder.RegisterVitalRouter(routing =>
{
    // ...
    routing.Filters.Add<Filter1>();
    // ...
});			
```

It is convenient to set up the Filter via DI, as this automates the instantiation of the Interceptor and dependency resolution.

For more information about Interceptors, please refer to the [Interceptor](../pipeline/interceptor) section.

## Resolving

If `RegisterVitalRouter` is set, the Router and its interface, `ICommandPublisher` and `ICommandSubscriber` can be retrieved from the DI container.

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

## Child scope

VContainer can create child scopes at any time during execution.

`RegisterVitalRouter` inherits the Router defined in the parent.
For example,

```cs
public class ParentLifetimeScope : LifetimeScope  
{  
    protected override void Configure(IContainerBuilder builder)  
    {    
        builder.RegisterVitalRouter(routing =>  
        {  
            routing.Map<PresenterA>();  
        });
        
        builder.Register<ParentPublisher>(Lifetime.Singleton);
    }  
}
```

```cs
public class ChildLifetimeScope : LifetimeScope  
{  
    protected override void Configure(IContainerBuilder builder)  
    {    
        builder.RegisterVitalRouter(routing =>  
        {  
            routing.Map<PresenterB>();  
        });  
        
        builder.Register<MyChildPublisher>(Lifetime.Singleton);
    }  
}
```

- When an instance in the parent scope publishes used `ICommandPublisher`, PresenterA and PresenterB receive it.
- When an instance in the child scope publishes `ICommandPublisher`, also PresenterA and PresenterB receives.

If you want to create a dedicated Router for a child scope, do the following.

```diff
builder.RegisterVitalRouter(routing =>  
{
+    routing.Isolated = true;
    routing.Map<PresenterB>();  
});  
```

