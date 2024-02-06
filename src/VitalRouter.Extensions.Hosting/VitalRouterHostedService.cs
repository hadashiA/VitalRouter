using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VitalRouter;

public class VitalRouterHostedService(
    IServiceProvider serviceProvider,
    IReadOnlyCollection<(Router, VitalRouterOptions)> routers)
    : IHostedService, IDisposable
{
    bool stopped;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (router, options) in routers)
        {
            foreach (var interceptorType in options.Filters.Types)
            {
                router.Filter((ICommandInterceptor)serviceProvider.GetRequiredService(interceptorType));
            }

            foreach (var info in options.MapRoutesInfos)
            {
                var instance = serviceProvider.GetRequiredService(info.Type);

                var parameters = new object[info.ParameterInfos.Length];
                parameters[0] = router;
                for (var paramIndex = 1; paramIndex < parameters.Length; paramIndex++)
                {
                    parameters[paramIndex] = serviceProvider.GetRequiredService(info.ParameterInfos[paramIndex].ParameterType);
                }
                info.MapToMethod.Invoke(instance, parameters);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        UnmapRoutes();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        UnmapRoutes();
    }

    void UnmapRoutes()
    {
        if (stopped) return;

        foreach (var (router, options) in routers)
        {
            foreach (var routesInfo in options.MapRoutesInfos)
            {
                var instance = serviceProvider.GetService(routesInfo.Type);
                if (instance != null)
                {
                    routesInfo.UnmapRoutesMethod.Invoke(instance, null);
                }
            }
        }
        stopped = true;
    }
}
