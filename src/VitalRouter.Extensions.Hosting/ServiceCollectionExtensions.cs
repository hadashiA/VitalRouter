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
        services.TryAddSingleton<Router>();
        services.AddVitalRouterRecursive(router, options);
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

    static void AddVitalRouterHostedService(this IServiceCollection services, Router router, VitalRouterOptions options)
    {
        services.AddHostedService(container => new VitalRouterHostedService(container, router, options));
    }

    static void AddVitalRouterRecursive(this IServiceCollection services, Router routerInstance, VitalRouterOptions options)
    {
        services.AddVitalRouterInterceptors(options);

        if (options.Subsequents.Count > 0)
        {
            var fanOut = new FanOutInterceptor();
            foreach (var subsequentOptions in options.Subsequents)
            {
                var subsequentRouter = new Router();
                services.AddVitalRouterRecursive(subsequentRouter, subsequentOptions);
                fanOut.Add(subsequentRouter);
            }
            routerInstance.Filter(fanOut);
        }

        services.AddVitalRouterHostedService(routerInstance, options);
    }
}