---
sidebar_label: Interceptor
title: Interceptor
---

Interceptors can intercede additional processing before or after the any published command has been passed and consumed to subscribers.

![Diagram](../assets/diagram_interceptors.svg)

### Use interceptor 

There are three levels to enable interceptor.

1. Apply globally to the `Router` (`IPublisher`, `ISubscribable`)
2. Apply it to all methods in the `[Routes]` class.
3. Apply only to specific handler.
 - Apply the `[Route]` method 
 - Apply  `.Subscribe(...)`, `.SubscribeAwait(...)`

```cs
// 1. Apply globally to the router.
Router.Default.AddFilter(new YourInterceptor());

var router = new Router();
router.AddFilter(new YourInterceptor());

IPublisher pubilsher = router;
publisher.AddFilter(new YourInterceptor());

ISubscribable subscribable = router;
subscribable.AddFilter(new YourInterceptor());
```


```cs
// 1. Apply to the router with VContaienr.
builder.RegisterVitalRouter(routing => 
{
    routing.Filters.Add<Logging>();
    routing.Filters.Add<ErrorHandling>();
});
```

```cs
// 1. Apply to the router with Microsoft.Extensions.DependencyInjection.
builder.AddVitalRouter(routing => 
{
    routing.Filters.Add<Logging>();
    routing.Filters.Add<ErrorHandling>();
});
```

```cs
[Routes]
[Filter(typeof(Logging))] // 2. Apply to the type
public partial class FooPresenter
{
    [Filter(typeof(ExtraInterceptor))] // 3. Apply to the method
    public void On(CommandA cmd)
    {
        // ...
    }
}
```

All of these are executed in the order in which they are registered, from top to bottom.

If you take the way of 2 or 3, the Interceptor instance is resolved as follows.

- If you are using DI, the DI container will resolve this automatically.
- if you are not using DI, you will need to pass the instance in the `MapTo` call.
    - ```cs
	  MapTo(Router.Default, new Logging(), new ErrorHandling());
	  ```
    - ```cs
	  // auto-generated
	  public Subscription MapTo(ICommandSubscribable subscribable, Logging interceptor1, ErrorHandling interceptor2) { /* ... */ }
	  ```


### Create custom interceptor

Arbitrary interceptors can be created by implementing `ICommandInterceptor`.

Example 1:  Some kind of processing is interspersed before and after the command is consumed.

```cs
class Logging : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(  
        T command,  
        PublishContext context,  
        PublishCOntinuation<T> next)
        where T : ICommand  
    {  
        UnityEngine.Debug.Log($"Start {typeof(T)}");	
        // Execute subsequent routes.	
        await next(command, context);		
        UnityEngine.Debug.Log($"End   {typeof(T)}");
    }		
}
```

Example 2:  try/catch all subscribers exceptions.

```cs
class ExceptionHandling : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(  
        T command,  
        PublishContext context,  
        PublishContinuation<T> next)  
        where T : ICommand  
    {  
        try
        {
            await next(command, context);
        }
        catch (Exception ex)
        {
            // Error tracking you like			
            UnityEngine.Debug.Log($"oops! {ex.Message}");			
        }
    }		
}
```

Example 3:  Filtering command.

```cs
class MyFilter : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(  
        T command,  
        PublishContext context,
        PublishContinuation<T> next)  
        where T : ICommand  
    {  
        if (command is FooCommand { X: > 100 } cmd) 
        {
            // Deny. Skip the rest of the subscribers.
            return;
        }
        // Allow.
        await next(command, context);
    }		
}
```
