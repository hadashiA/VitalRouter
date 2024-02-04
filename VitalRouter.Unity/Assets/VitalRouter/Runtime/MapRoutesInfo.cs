using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace VitalRouter.Internal;

public class MapRoutesInfo
{
    static readonly ConcurrentDictionary<Type, MapRoutesInfo> Cache = new();

    public static MapRoutesInfo Analyze(Type type) => Cache.GetOrAdd(type, key => new MapRoutesInfo(key));

    public Type Type { get; }
    public MethodInfo MapToMethod { get; }
    public MethodInfo UnmapRoutesMethod { get; }
    public ParameterInfo[] ParameterInfos { get; }

    public MapRoutesInfo(Type type)
    {
        Type = type;
        MapToMethod = type.GetMethod("MapTo", BindingFlags.Instance | BindingFlags.Public)!;
        UnmapRoutesMethod = type.GetMethod("UnmapRoutes", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfos = MapToMethod.GetParameters();
    }
}

public class InterceptorStackBuilder
{
    public List<Type> Types { get; } = new();

    public InterceptorStackBuilder Add<T>() where T : ICommandInterceptor
    {
        Types.Add(typeof(T));
        return this;
    }
}
