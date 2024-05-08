using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;
public class ApplicationData : DbContext
{
    private readonly ILoggerFactory loggerFactory;
    public DbSet<ManualProxy> ManualProxies { get; set; }

    public ApplicationData(DbContextOptions options, ILoggerFactory loggerFactory) : base(options)
    {
        this.loggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _ = optionsBuilder.UseLoggerFactory(loggerFactory);
    }
}
