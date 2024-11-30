---
sidebar_label: Error handling
title: Error handling 
---

Exceptions thrown by the Route method can be caught by try/catch around `await PublishAsync(..)`.

```cs
[Route]
void On(FooCommand cmd)
{
    throw new InvalidOperationException("OOPS!!!");
} 
```

```cs
try
{
    await Router.PublishAsync(new FooCommand());
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message); // => "OOPS!!"
}
```

Another powerful way to insert a common error handler is to use an Interceptor.

```cs
class ExceptionHandling : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(  
        T command,  
        CancellationToken cancellation,  
        Func<T, CancellationToken, ValueTask> next)  
        where T : ICommand  
    {  
        try
        {
            await next(command, cancellation);
        }
        catch (Exception ex)
        {
            // Error tracking you like			
            UnityEngine.Debug.Log($"oops! {ex.Message}");			
        }
    }		
}
```

```cs
// Add error handler globally
Router.Default.AddFilter(new ErrorHandling());

// Add error handler to specific handler
[Routes]
[Filter(typeof(ErrorHandling))]
partial class FooPresenter
{
    // ...
}
```

Interceptors can be set globally for the router, or at the level of each handler.
For details, please refer to the [Interceptor](../pipeline/interceptor) section.
