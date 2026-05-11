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

`WithFilter(...)` returns a derived child router that carries the given filter.
Subscribers registered on the child receive commands published on the parent
with the filter applied — the same way an Rx `Where` chain forwards items
from its source.

```cs
await Router.Default.PublishAsync(new FooCommand());
// The filter runs, then the handler is invoked (if `condition` was true).
```

Publishing directly on the returned child runs the cumulative filter chain
from the root down to that child. Each filter in the tree is invoked exactly
once per publish — there is no duplicate filter execution even when
subscribers exist at multiple depths.

Note: The cumulative chain is snapshotted when `WithFilter` is called.
`AddFilter` invoked on an ancestor after the child was created does not
retroactively apply to the existing child's cumulative chain.

WithFilter, you can add common processing, and you can also ignore commands based on conditions.
For details about Filters, see the [Interceptor](../pipeline/interceptor) section.