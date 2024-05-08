using Domain.Models;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Collections.ObjectModel;

namespace Application;
public class DataService
{
    public ReadOnlyCollection<CaddyContainer> CaddyContainers => _containers.AsReadOnly();
    private readonly List<CaddyContainer> _containers = new();
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<DataService> logger;

    public event Func<Task> Updated;

    public DataService(ILogger<DataService> logger)
    {
        this.logger = logger;
    }

    public async Task AddOrUpdate(CaddyContainer container)
    {
        if (container is null) return;

        try
        {

            await _semaphore.WaitAsync(-1);

            var index = _containers.IndexOf(container);
            if (index < 0)
            {
                _containers.Add(container);
                using (var loga = LogContext.PushProperty("Data", container, true))
                {
                    logger.LogInformation("Added Container {Id}", container.Id[..12]);
                }
            }

            if (index >= 0)
            {
                _containers[index] = container;
                using (var loga = LogContext.PushProperty("Data", container, true))
                {
                    logger.LogInformation("Updated Container {Id}", container.Id[..12]);
                }
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Adding or Updating container info {Id}", container.Id);
        }
        finally
        {
            _semaphore.Release();
        }

        await (Updated?.Invoke() ?? Task.CompletedTask);

    }

    public async Task Delete(string Id)
    {
        try
        {
            await _semaphore.WaitAsync(-1);
            var caddyContainer = _containers.SingleOrDefault(q => q.Id == Id);

            if (caddyContainer is null)
            {
                return;
            }

            using (var loga = LogContext.PushProperty("Data", caddyContainer, true))
            {
                logger.LogInformation("Removing Container {Id}", Id[..12]);
            }

            _containers.Remove(caddyContainer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error trying to remove container from settings");
        }
        finally
        {
            _ = _semaphore.Release();
        }

        await (Updated?.Invoke() ?? Task.CompletedTask);
    }
}
