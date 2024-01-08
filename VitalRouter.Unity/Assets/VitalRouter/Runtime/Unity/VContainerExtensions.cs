#if VITALROUTER_VCONTAINER_INTEGRATION
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using VitalRouter.Internal;

namespace VitalRouter.VContainer;

class MapRoutesInfo
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

class RoutingDisposable : IDisposable
{
    readonly IObjectResolver container;
    readonly IReadOnlyList<MapRoutesInfo> routes;

    public RoutingDisposable(IObjectResolver container, IReadOnlyList<MapRoutesInfo> routes)
    {
        this.container = container;
        this.routes = routes;
    }

    public void Dispose()
    {
        for (var i = 0; i < routes.Count; i++)
        {
            var instance = container.Resolve(routes[i].Type);
            routes[i].UnmapRoutesMethod.Invoke(instance, null);
        }
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

public class RoutingBuilder
{
    public InterceptorStackBuilder Filters { get; } = new();
    public bool OverrideRouter { get; set; }

    internal IReadOnlyList<MapRoutesInfo> MapRoutesInfos => mapRoutesInfos;

    readonly IContainerBuilder containerBuilder;
    readonly List<MapRoutesInfo> mapRoutesInfos = new();

    public RoutingBuilder(IContainerBuilder containerBuilder)
    {
        this.containerBuilder = containerBuilder;
    }

    public void Map<T>()
    {
        if (typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)))
        {
            containerBuilder.RegisterComponentOnNewGameObject(typeof(T), Lifetime.Singleton);
        }
        else
        {
            containerBuilder.Register<T>(Lifetime.Singleton);
        }
        mapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
    }

    public void Map<T>(T instance) where T : class
    {
        containerBuilder.RegisterInstance(instance);
        mapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
    }

    public void MapComponentInHierarchy<T>() where T : UnityEngine.Component
    {
        containerBuilder.RegisterComponentInHierarchy<T>();
        mapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
    }

    public void MapComponentInNewPrefab<T>(T prefab) where T : UnityEngine.Component
    {
        containerBuilder.RegisterComponentInNewPrefab(prefab, Lifetime.Singleton);
        mapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
    }
}

public static class VContainerExtensions
{
    public static void RegisterVitalRouter(this IContainerBuilder builder, Action<RoutingBuilder> configure)
    {
        var routing = new RoutingBuilder(builder);
        configure(routing);

        builder.RegisterVitalRouterInterceptors(routing);
        builder.RegisterVitalRouterDisposable(routing);

        if (!builder.Exists(typeof(Router)) || routing.OverrideRouter)
        {
            builder.Register<Router>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }

        builder.RegisterBuildCallback(container =>
        {
            var router = container.Resolve<Router>();
            InvokeMapRoutes(router, routing, container);
        });
    }

    public static void RegisterVitalRouter(this IContainerBuilder builder, Router routerInstance, Action<RoutingBuilder> configure)
    {
        var routing = new RoutingBuilder(builder);
        configure(routing);

        builder.RegisterInstance(routerInstance)
            .AsImplementedInterfaces()
            .AsSelf();

        builder.RegisterVitalRouterInterceptors(routing);
        builder.RegisterVitalRouterDisposable(routing);

        builder.RegisterBuildCallback(container =>
        {
            InvokeMapRoutes(routerInstance, routing, container);
        });
    }

    static void RegisterVitalRouterInterceptors(this IContainerBuilder builder, RoutingBuilder routing)
    {
        foreach (var interceptorType in routing.Filters.Types)
        {
            builder.Register(interceptorType, Lifetime.Singleton);
        }

        for (var i = 0; i < routing.MapRoutesInfos.Count; i++)
        {
            var info = routing.MapRoutesInfos[i];
            for (var paramIndex = 1; paramIndex < info.ParameterInfos.Length; paramIndex++)
            {
                var interceptorType = info.ParameterInfos[paramIndex].ParameterType;
                if (!builder.Exists(interceptorType))
                {
                    builder.Register(interceptorType, Lifetime.Singleton);
                }
            }
        }
    }

    static void RegisterVitalRouterDisposable(this IContainerBuilder builder, RoutingBuilder routing)
    {
        builder.Register(container => new RoutingDisposable(container, routing.MapRoutesInfos), Lifetime.Singleton);
    }

    static void InvokeMapRoutes(Router router, RoutingBuilder routing, IObjectResolver container)
    {
        foreach (var interceptorType in routing.Filters.Types)
        {
            router.Filter((ICommandInterceptor)container.Resolve(interceptorType));
        }

        for (var i = 0; i < routing.MapRoutesInfos.Count; i++)
        {
            var info = routing.MapRoutesInfos[i];
            var instance = container.Resolve(info.Type);

            // TODO: more optimize
            var parameters = CappedArrayPool<object>.Shared8Limit.Rent(info.ParameterInfos.Length);
            try
            {
                parameters[0] = router;
                for (var paramIndex = 1; paramIndex < parameters.Length; paramIndex++)
                {
                    parameters[paramIndex] = container.Resolve(info.ParameterInfos[paramIndex].ParameterType);
                }
                info.MapToMethod.Invoke(instance, parameters);
            }
            finally
            {
                CappedArrayPool<object>.Shared8Limit.Return(parameters);
            }
        }

    }
}
#endif
