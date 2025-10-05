using System;

namespace VitalRouter
{
public class PreserveAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class RoutesAttribute : Attribute
{
    public CommandOrdering Ordering { get; }

    public RoutesAttribute(CommandOrdering ordering = CommandOrdering.Parallel)
    {
        Ordering = ordering;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RouteAttribute : Attribute
{
    public CommandOrdering Ordering { get; }

    public RouteAttribute(CommandOrdering ordering = CommandOrdering.Parallel)
    {
        Ordering = ordering;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class FilterAttribute : Attribute
{
    public Type InterceptorType { get; }

    public FilterAttribute(Type interceptorType)
    {
        InterceptorType = interceptorType;
    }
}
#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class FilterAttribute<T> : Attribute where T : ICommandInterceptor
{
}
#endif
}