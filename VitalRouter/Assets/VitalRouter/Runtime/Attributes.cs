using System;

namespace VitalRouter;

public class PreserveAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class RoutesAttribute : PreserveAttribute
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RoutesBeforeAttribute : Attribute
{
    public Type InterceptorType { get; }

    public RoutesBeforeAttribute(Type interceptorType)
    {
        InterceptorType = interceptorType;
    }
}
