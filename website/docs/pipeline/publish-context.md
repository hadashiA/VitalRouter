---
sidebar_label: PublishContext
title: PublishContext
---

A handler that can receive published commands can receive `PublishContext`.
```cs
[Routes]
partial class FooHandler
{
    [Route]
    async ValueTask On(FooCommand cmd, PublishContext ctx)
    {
        // ...
    }
}
```

```cs
subscribable.Subscribe((FooCommand cmd, PublishContext ctx) => { /* ... */ });
subscribable.SubscribeAwait(async (FooCommand cmd, PublishContext ctx) => { /* ... */ });
```

`PublishContext` allows you to hold valid data from the time a command is published until all Interceptors/Subscribers have finished processing.

The `PublishContext.CancellationToken` property is a CancellationToken that is canceled when PublishAsync is canceled.

```cs
    [Route]
    async ValueTask On(FooCommand cmd, PublishContext ctx)
    {
        if (ctx.CancellationToken.IsCancelRequested) return;
    }
```

You can also set arbitrary data to the PublishContext and refer to it later in the pipeline.

```cs
class MyFilter : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(  
        T command,  
        PublishContext context,  
        PublishContinuation next)  
        where T : ICommand  
    {
        context.Extensiont.TryAdd("CustomData", 123456789);
        await next(command, context);
    }		
}
```

```cs
[Routes]
partial class FooHandler
{
    [Route]
    async ValueTask On(FooCommand cmd, PublishContext context)
    {
        context.Extensions["CustomData"] // => 123456789
    }
}
```