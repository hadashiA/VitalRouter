---
sidebar_label: Fan-out
title: Fan-out
---

If you want to group the awaiting subscribers, you can use `FanOutInterceptor`

For example, in the following example, commands are delivered to groupA and groupA in parallel, but in sequential within each group.

```cs
var fanOut = new FanOutInterceptor();
var groupA = new Router(CommandOrdering.Sequential)
var groupB = new Router(CommandOrdering.Sequential);

fanOut.Add(groupA);
fanOut.Add(groupB);

Router.Default.Filter(fanOut);

// Map routes per group

presenter1.MapTo(groupA);
presenter2.MapTo(groupA);

presente3.MapTo(groupB);
presente4.MapTo(groupB);
```

With Microsoft.Extensions.DependencyInjection

```cs
builder.AddVitalRouter(routing =>
{
    routing
        .FanOut(groupA =>
        {
            groupA.Ordering = CommandOrdering.Sequential;
            groupA.Map<Presenter1>();
            groupA.Map<Presenter2>();
        })    
        .FanOut(groupB =>
        {
            groupB.Ordering = CommandOrdering.Sequential;
            groupB.Map<Presenter3>();
            groupB.Map<Presenter4>();
        })                
});
```

With VContainer (Unity)

```cs
public class SampleLifetimeScope : LifetimeScope
{
    public override void Configure(IContainerBuilder builder)
    {                
        builder.RegisterVitalRouter(routing =>
        {
            routing
                .FanOut(groupA =>
                {
                    groupA.Ordering = CommandOrdering.Sequential;
                    groupA.Map<Presenter1>();
                    groupA.Map<Presenter2>();
                })    
                .FanOut(groupB =>
                {
                    groupB.Ordering = CommandOrdering.Sequential;
                    groupB.Map<Presenter3>();
                    groupB.Map<Presenter4>();
                })                
        });
    }
}
```
