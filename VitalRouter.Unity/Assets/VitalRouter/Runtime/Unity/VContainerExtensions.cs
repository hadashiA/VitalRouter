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
    public MethodInfo MapRoutesMethod { get; }
    public MethodInfo UnmapRoutesMethod { get; }
    public ParameterInfo[] ParameterInfos { get; }

    public MapRoutesInfo(Type type)
    {
        Type = type;
        MapRoutesMethod = type.GetMethod("MapTo", BindingFlags.Instance | BindingFlags.Public)!;
        UnmapRoutesMethod = type.GetMethod("UnmapRoutes", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfos = MapRoutesMethod.GetParameters();
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

    internal bool CommandBusOverriden { get; set; }
    internal IReadOnlyList<MapRoutesInfo> MapRoutesInfos => mapRoutesInfos;

    readonly IContainerBuilder containerBuilder;
    readonly List<MapRoutesInfo> mapRoutesInfos = new();

    public RoutingBuilder(IContainerBuilder containerBuilder)
    {
        this.containerBuilder = containerBuilder;
    }

    public void OverrideCommandBus()
    {
        CommandBusOverriden = true;
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
        var router = new RoutingBuilder(builder);
        configure(router);

        foreach (var interceptorType in router.Filters.Types)
        {
            builder.Register(interceptorType, Lifetime.Singleton);
        }

        for (var i = 0; i < router.MapRoutesInfos.Count; i++)
        {
            var info = router.MapRoutesInfos[i];
            for (var paramIndex = 1; paramIndex < info.ParameterInfos.Length; paramIndex++)
            {
                var interceptorType = info.ParameterInfos[paramIndex].ParameterType;
                if (!builder.Exists(interceptorType))
                {
                    builder.Register(interceptorType, Lifetime.Singleton);
                }
            }
        }

        if (!builder.Exists(typeof(Router)) || router.CommandBusOverriden)
        {
            builder.Register<Router>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }

        builder.Register(container =>
        {
            return new RoutingDisposable(container, router.MapRoutesInfos);
        }, Lifetime.Singleton);

        builder.RegisterBuildCallback(container =>
        {
            var commandBus = container.Resolve<Router>();
            foreach (var interceptorType in router.Filters.Types)
            {
                commandBus.Filter((ICommandInterceptor)container.Resolve(interceptorType));
            }

            for (var i = 0; i < router.MapRoutesInfos.Count; i++)
            {
                var info = router.MapRoutesInfos[i];
                var instance = container.Resolve(info.Type);

                // TODO: more optimize
                var parameters = CappedArrayPool<object>.Shared8Limit.Rent(info.ParameterInfos.Length);
                try
                {
                    parameters[0] = commandBus;
                    for (var paramIndex = 1; paramIndex < parameters.Length; paramIndex++)
                    {
                        parameters[paramIndex] = container.Resolve(info.ParameterInfos[paramIndex].ParameterType);
                    }
                    info.MapRoutesMethod.Invoke(instance, parameters);
                }
                finally
                {
                    CappedArrayPool<object>.Shared8Limit.Return(parameters);
                }
            }
        });
    }
}
#endif
