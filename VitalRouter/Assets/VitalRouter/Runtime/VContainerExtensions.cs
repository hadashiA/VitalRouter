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
    public MethodInfo MapRoutesMethod { get; }
    public ParameterInfo[] ParameterInfos { get; }

    public MapRoutesInfo(Type type)
    {
        MapRoutesMethod = type.GetMethod("MapRoutes", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfos = MapRoutesMethod.GetParameters();
    }
}

public partial class RoutingBuilder
{
    public IReadOnlyList<Type> RoutingTypes => routingTypes;
    public IReadOnlyList<Type> GlobalInterceptorTypes => globalInterceptorTypes;

    readonly IContainerBuilder containerBuilder;
    readonly List<Type> routingTypes = new();
    readonly List<Type> globalInterceptorTypes = new();

    public RoutingBuilder(IContainerBuilder containerBuilder)
    {
        this.containerBuilder = containerBuilder;
    }

    public void Use<T>() where T : ICommandInterceptor
    {
        globalInterceptorTypes.Add(typeof(T));
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
        routingTypes.Add(typeof(T));
    }

    public void Map<T>(T instance) where T : class
    {
        containerBuilder.RegisterInstance(instance);
        routingTypes.Add(instance.GetType());
    }

    public void MapComponentInHierarchy<T>() where T : UnityEngine.Component
    {
        containerBuilder.RegisterComponentInHierarchy<T>();
        routingTypes.Add(typeof(T));
    }

    public void MapComponentInNewPrefab<T>(T prefab) where T : UnityEngine.Component
    {
        containerBuilder.RegisterComponentInNewPrefab(prefab, Lifetime.Singleton);
        routingTypes.Add(typeof(T));
    }
}

public static class VContainerExtensions
{
    static readonly ConcurrentDictionary<Type, MapRoutesInfo> MapRoutesInfoCache = new();

    public static void RegisterVitalRouter(this IContainerBuilder builder, Action<RoutingBuilder> configure)
    {
        var routing = new RoutingBuilder(builder);
        configure(routing);

        foreach (var interceptorType in routing.GlobalInterceptorTypes)
        {
            builder.Register(interceptorType, Lifetime.Singleton);
        }

        builder.Register(container =>
            {
                var commandBus = new CommandBus();
                foreach (var interceptorType in routing.GlobalInterceptorTypes)
                {
                    commandBus.Use((ICommandInterceptor)container.Resolve(interceptorType));
                }
                return commandBus;
            }, Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();

        builder.RegisterBuildCallback(container =>
        {
            var commandBus = container.Resolve<CommandBus>();
            for (var i = 0; i < routing.RoutingTypes.Count; i++)
            {
                var type = routing.RoutingTypes[i];
                var instance = container.Resolve(type);

                // TODO: more optimize
                var mapRoutesInfo = MapRoutesInfoCache.GetOrAdd(type, key => new MapRoutesInfo(key));
                var parameters = CappedArrayPool<object>.Shared8Limit.Rent(mapRoutesInfo.ParameterInfos.Length);
                try
                {
                    parameters[0] = commandBus;
                    for (var paramIndex = 1; paramIndex < parameters.Length; paramIndex++)
                    {
                        parameters[paramIndex] = container.CreateInstance(mapRoutesInfo.ParameterInfos[paramIndex].ParameterType);
                    }
                    mapRoutesInfo.MapRoutesMethod.Invoke(instance, parameters);
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
