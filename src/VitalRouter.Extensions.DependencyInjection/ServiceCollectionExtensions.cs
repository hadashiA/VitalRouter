using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VitalRouter.Internal;

namespace VitalRouter;

public class VitalRouterOptions
{
    public InterceptorStackBuilder Filters { get; } = new();
    public CommandOrdering Ordering { get; set; }

    internal readonly List<MapRoutesInfo> MapRoutesInfos = [];
    internal readonly List<VitalRouterOptions> Subsequents = [];

    public VitalRouterOptions MapAll(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(x => x.GetCustomAttribute<RoutesAttribute>() != null);
        foreach (var type in types)
        {
            MapRoutesInfos.Add(MapRoutesInfo.Analyze(type));
        }
        return this;
    }

    public VitalRouterOptions Map<T>()
    {
        MapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
        return this;
    }

    public VitalRouterOptions Map<T>(T instance) where T : class
    {
        MapRoutesInfos.Add(MapRoutesInfo.Analyze(typeof(T)));
        return this;
    }

    public VitalRouterOptions Sequential()
    {
        Filters.Add<SequentialOrdering>();
        return this;
    }

    public VitalRouterOptions FanOut(Action<VitalRouterOptions> configure)
    {
        var subsequent = new VitalRouterOptions();
        configure(subsequent);
        Subsequents.Add(subsequent);
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVitalRouter(this IServiceCollection services)
    {
        return services.AddVitalRouter(static _ => { });
    }

    public static IServiceCollection AddVitalRouter(this IServiceCollection services, Action<VitalRouterOptions> configure)
    {
        var options = new VitalRouterOptions();
        configure(options);

        var router = new Router();
        var routers = new List<(Router, VitalRouterOptions)>();
        services.AddVitalRouterRecursive(router, options, routers);

        services.AddSingleton(serviceProvider =>
        {
            foreach (var (r, o) in routers)
            {
                foreach (var interceptorType in o.Filters.Types)
                {
                    r.Filter((ICommandInterceptor)serviceProvider.GetRequiredService(interceptorType));
                }

                foreach (var info in o.MapRoutesInfos)
                {
                    var instance = serviceProvider.GetRequiredService(info.Type);

                    var parameters = new object[info.ParameterInfos.Length];
                    parameters[0] = r;
                    for (var paramIndex = 1; paramIndex < parameters.Length; paramIndex++)
                    {
                        parameters[paramIndex] = serviceProvider.GetRequiredService(info.ParameterInfos[paramIndex].ParameterType);
                    }
                    info.MapToMethod.Invoke(instance, parameters);
                }
            }

            return router;
        });

        services.AddSingleton<ICommandPublisher>(container => container.GetRequiredService<Router>());
        services.AddSingleton<ICommandSubscribable>(container => container.GetRequiredService<Router>());

        return services;
    }

    static IServiceCollection AddVitalRouterInterceptors(this IServiceCollection services, VitalRouterOptions options)
    {
        switch (options.Ordering)
        {
            case CommandOrdering.Sequential:
                options.Filters.Add<SequentialOrdering>();
                break;
        }

        foreach (var interceptorType in options.Filters.Types)
        {
            services.TryAddSingleton(interceptorType);
        }

        foreach (var info in options.MapRoutesInfos)
        {
            for (var paramIndex = 1; paramIndex < info.ParameterInfos.Length; paramIndex++)
            {
                var interceptorType = info.ParameterInfos[paramIndex].ParameterType;
                services.TryAddSingleton(interceptorType);
            }
        }

        return services;
    }

    static void AddVitalRouterRecursive(
        this IServiceCollection services,
        Router routerInstance,
        VitalRouterOptions options,
        ICollection<(Router, VitalRouterOptions)> routers)
    {
        services.AddVitalRouterInterceptors(options);

        foreach (var info in options.MapRoutesInfos)
        {
            services.TryAddSingleton(info.Type);
        }

        if (options.Subsequents.Count > 0)
        {
            var fanOut = new FanOutInterceptor();
            foreach (var subsequentOptions in options.Subsequents)
            {
                var subsequentRouter = new Router();
                services.AddVitalRouterRecursive(subsequentRouter, subsequentOptions, routers);
                fanOut.Add(subsequentRouter);
            }
            routerInstance.Filter(fanOut);
        }

        routers.Add((routerInstance, options));
    }
}