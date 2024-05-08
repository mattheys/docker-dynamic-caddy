using DynamicDockerCaddy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application;
public class LogicService : IHostedService
{
    private readonly ILogger<LogicService> logger;
    private readonly DataService dataService;


    public LogicService(ILogger<LogicService> logger, DataService dataService)
    {
        DockerMonitorHostedService.ContainerStarted += DockerMonitorHostedService_ContainerStarted;
        DockerMonitorHostedService.ContainerStopped += DockerMonitorHostedService_ContainerStopped;
        this.logger = logger;
        this.dataService = dataService;
    }

    private async Task DockerMonitorHostedService_ContainerStopped(string Id)
    {
        await dataService.Delete(Id);
    }
    private async Task DockerMonitorHostedService_ContainerStarted(Domain.Models.CaddyContainer container)
    {
        await dataService.AddOrUpdate(container);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
