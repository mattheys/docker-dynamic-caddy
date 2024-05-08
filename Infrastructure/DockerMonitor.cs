using Docker.DotNet.Models;
using Docker.DotNet;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Infrastructure;
public class DockerMonitor : IDockerMonitor
{
    private readonly DockerHostSettings dockerHostSettings;
    private readonly ILogger<DockerMonitor> logger;
    private DockerClient client;
    private readonly List<string> AddActions = new List<string>() { "start" };
    private readonly List<string> DeleteActions = new List<string>() { "stop", "die" };

    public event Func<CaddyContainer, Task>? ContainerStarted;
    public event Func<string, Task>? ContainerStopped;

    public DockerMonitor(DockerHostSettings dockerHostSettings, ILogger<DockerMonitor> logger)
    {
        this.dockerHostSettings = dockerHostSettings;
        this.logger = logger;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting Docker Monitor for {Endpoint}", dockerHostSettings.Endpoint);

            await StartDockerClient(cancellationToken);

            logger.LogInformation("Started Docker Monitor for {Endpoint}", dockerHostSettings.Endpoint);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting Docker Monitor for {Endpoint}", dockerHostSettings.Endpoint);
        }
    }

    private async Task StartDockerClient(CancellationToken cancellationToken)
    {
        Credentials credentials = null;

        if (dockerHostSettings.Username is not null && dockerHostSettings.Password is not null)
        {
            credentials = new Docker.DotNet.BasicAuth.BasicAuthCredentials(dockerHostSettings.Username, dockerHostSettings.Password, dockerHostSettings.UseTls);
        }

        client = new DockerClientConfiguration(
            endpoint: new Uri(dockerHostSettings.Endpoint),
            credentials: credentials
            ).CreateClient();

        var progress = new Progress<Message>();
        progress.ProgressChanged += Progress_ProgressChanged;

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>()
                {
                    {
                        "status",
                        new Dictionary<string, bool>()
                        {
                            { "running", true }
                        }
                    },
                }
            }
        );

        foreach (var item in containers)
        {
            var container = await GetContainer(item);
            if (container is null) { continue; }
            await (ContainerStarted?.Invoke(container) ?? Task.CompletedTask);
        }

        client.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, cancellationToken);

    }

    private async void Progress_ProgressChanged(object? sender, Message e)
    {
        if(e.ID is null) return; 
        if (!e.Type.Equals("container", StringComparison.OrdinalIgnoreCase)) return;

        logger.LogTrace("Docker Event: Id {Id}, From {From}, Action {Action}", e.ID[..12], e.From, e.Action);

        if (AddActions.Contains(e.Action))
        {
            var container = await GetContainer(e.ID);

            if (container == null) { return; }

            await (ContainerStarted?.Invoke(container) ?? Task.CompletedTask);
        }

        if (DeleteActions.Contains(e.Action))
        {
            await (ContainerStopped?.Invoke(e.ID) ?? Task.CompletedTask);
        }
    }

    private async Task<CaddyContainer?> GetContainer(string Id)
    {
        var container = await GetContainerListItem(Id);

        if (container is null) { return null; }

        return await GetContainer(container);
    }

    private async Task<CaddyContainer?> GetContainer(ContainerListResponse container)
    {
        await Task.CompletedTask;

        try
        {
            if (container is null) return null;

            var caddyContainer = new CaddyContainer(container.ID, dockerHostSettings.Endpoint, dockerHostSettings.FriendlyName);

            foreach (var label in container.Labels.Where(q => q.Key.StartsWith("caddy.dynamic.docker")))
            {
                var index = 0;

                string labelKey = label.Key;

                if (label.Key.Contains(':'))
                {
                    index = int.Parse(label.Key.Split(":")[1]);
                    labelKey = label.Key.Split(":")[0];
                }

                var proxyAddress = caddyContainer.ProxyAddresses.SingleOrDefault(q => q.Index == index);

                if (proxyAddress is null)
                {
                    proxyAddress = new ProxyAddress() { Index = index };
                    caddyContainer.ProxyAddresses.Add(proxyAddress);
                }

                if (labelKey.EndsWith("target"))
                {
                    proxyAddress.TargetHostName = label.Value.Split(":")[0];
                    proxyAddress.TargetPort = ushort.Parse(label.Value.Split(':')[1]);
                }

                if (labelKey.EndsWith("dns"))
                {
                    proxyAddress.DNSName = label.Value;
                }

            }
            if (caddyContainer.ProxyAddresses.Count > 0)
            {
                return caddyContainer;
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error trying to add container to list");
        }

        return null;

    }

    private async Task<ContainerListResponse?> GetContainerListItem(string Id)
    {
        if (Id is null) { return null; }

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(new ContainersListParameters()
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>() { { "id", new Dictionary<string, bool>() { { Id, true } } } }
        });

        if (containers.Count == 1)
        {
            return containers[0];
        }

        return null;
    }

}
