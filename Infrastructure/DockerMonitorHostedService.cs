using Docker.DotNet;
using Docker.DotNet.Models;
using Domain;
using Domain.Models;
using Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicDockerCaddy;
public class DockerMonitorHostedService :IHostedService
{
    private readonly ILogger<DockerMonitorHostedService> logger;
    private readonly IEnumerable<IDockerMonitor> dockerMonitors;
    private readonly CancellationTokenSource shutdownTokenSource = new CancellationTokenSource();

    public static event Func<CaddyContainer, Task>? ContainerStarted;
    public static event Func<string, Task>? ContainerStopped;

    public DockerMonitorHostedService(ILogger<DockerMonitorHostedService> logger, IEnumerable<IDockerMonitor> dockerMonitors)
    {
        this.logger = logger;
        this.dockerMonitors = dockerMonitors;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        foreach (var monitor in dockerMonitors)
        {
            monitor.ContainerStarted += Monitor_ContainerStarted;
            monitor.ContainerStopped += Monitor_ContainerStopped;
            await monitor.Start(shutdownTokenSource.Token);
        }

    }

    private async Task Monitor_ContainerStopped(string arg)
    {
        await (ContainerStopped?.Invoke(arg) ?? Task.CompletedTask);
    }
    private async Task Monitor_ContainerStarted(CaddyContainer arg)
    {
        await (ContainerStarted?.Invoke(arg) ?? Task.CompletedTask);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if(cancellationToken.IsCancellationRequested) return;

        shutdownTokenSource.Cancel();
    }

  

}
