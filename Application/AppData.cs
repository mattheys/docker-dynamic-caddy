using Domain.Models;
using Domain;
using Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Application;
public class AppData : IAppData
{
    private readonly IDbContextFactory<ApplicationData> dbContextFactory;
    private readonly ILogger<AppData> logger;

    public event Func<Task> Updated;

    public AppData(IDbContextFactory<ApplicationData> dbContextFactory, ILogger<AppData> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    public async Task Add(ManualProxy manualProxy)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.ManualProxies.AddAsync(manualProxy);
        await (Updated?.Invoke() ?? Task.CompletedTask);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task Delete(ManualProxy manualProxy)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.ManualProxies.Remove(manualProxy);
        await (Updated?.Invoke() ?? Task.CompletedTask);
        await dbContext.SaveChangesAsync();
    }

    public async Task<ManualProxy[]> Get()
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.ManualProxies.Where(q => true).ToArrayAsync();
    }

    public async Task<ManualProxy[]> Get(string dns)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dns = dns.ToLower();
        return await dbContext.ManualProxies.Where(q => q.DnsName.ToLower() == dns).ToArrayAsync();
    }
}