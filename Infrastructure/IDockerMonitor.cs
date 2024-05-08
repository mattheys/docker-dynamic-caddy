using Domain.Models;

namespace Infrastructure;
public interface IDockerMonitor
{
    event Func<CaddyContainer, Task>? ContainerStarted;
    event Func<string, Task>? ContainerStopped;

    Task Start(CancellationToken cancellationToken);
}