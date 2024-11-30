---
sidebar_label: Simple Event handler pattern
title: Simple Event handler pattern
---

[Declarative Routing](./declarative-routing-pattern) You can also register a lambda expression simply and receive commands without following the pattern.

```cs
Router.Default.Subscribe<FooCommand>((cmd, context) =>
{
    // sync handler  
});
```

Here, the first argument `cmd` passed to the lambda expression is the command, and the second argument `context` is the PublishContext.
For more information about the PublishContext, please refer to [this](../pipeline/publish-context.md).

You can register an async handler using `SubscribeAwait`.

```cs
Router.Default.SubscribeAwait<FooCommand>(async (cmd, context) =>
{
    await DoSomeThingAsync();
}, CommandOrdering.Parallel);
```

The `CommandOrdering` argument of SubscribeAwait can specify the behavior when it is executed in parallel during await.
For details, see the [Sequential Control](../pipeline/sequential-control.mdx) section.

To issue a command, do the following.

```cs
await Router.Default.PublishAsync(new FooCommand());
```

The above handler is called by PublishAsync. You can wait for all handlers to complete using await.

## Another router instance

It is possible to create multiple `Router` instances.

```cs
var router = new Router();

// Router has some interfaces.
ISubscribable subscribable = router;
IPublisher publisher = router;

subscribable.Subscribe(cmd =>
{
})

await publisher.PublishAsync(new FooCommand());
```

The cost of instantiating a router is small.
For this reason, it can also be used as a simple alternative to events.

## Filter

Filters are useful if you want to insert common processing before and after commands are delivered to the handler.

```cs
Router.Default
    .WithFilter(async (cmd, context, next) =>
    {
        if (condition) await next(cmd, context);
    })
   .Subscribe((cmd, context) => { /* ... */ });
```

WithFilter, you can add common processing, and you can also ignore commands based on conditions.
For details about Filters, see the [Interceptor](../pipeline/interceptor) section.