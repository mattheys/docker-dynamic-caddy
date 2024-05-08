using Application;
using Domain;
using Domain.Models;
using DynamicDockerCaddy;
using Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Serilog;

internal class Program
{
    public static CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

    public static async Task Main(string[] args)
    {
        try
        {

            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

            if (!File.Exists("config/appsettings.json"))
            {
                Directory.CreateDirectory("config");
                File.WriteAllText("config/appsettings.json", exampleJson);
            }

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(cfg =>
            {
                cfg.ReloadOnChange = true;
                cfg.Path = "config/appsettings.json";
                cfg.Optional = false;
            });

            builder.Configuration.AddEnvironmentVariables("DDC:");

            var settings = builder.Configuration.GetSection("App");
            builder.Services.Configure<Settings>(settings);

            builder.Services.AddSerilog(config =>
            {
                config.ReadFrom.Configuration(builder.Configuration);
            });

            builder.Services.AddSingleton<IAppData,AppData>();
            builder.Services.AddSingleton<DataService>();
            builder.Services.AddHostedService<LogicService>();
            builder.Services.AddHostedService<DnsServerHostedService>();
            builder.Services.AddHostedService<DockerMonitorHostedService>();

            foreach (var item in settings.Get<Settings>().DockerHostSettings)
            {
                builder.Services.AddSingleton<IDockerMonitor>(sp=>
                {
                    var logger = sp.GetRequiredService<ILogger<DockerMonitor>>();
                    return new DockerMonitor(item, logger);
                });
            }

            builder.Services.AddDbContextFactory<ApplicationData>(opt =>
            {
                opt.UseSqlite("Data Source=data/database.db");
            });

            builder.WebHost.UseUrls(settings.GetValue<string>("Url") ?? "http://0.0.0.0:5000/");

            StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddMudServices();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationData>();
                dbContext.Database.Migrate();
            }

            app.MapGet("/ask", async Task<Results<Ok, BadRequest>> (string domain, 
                DataService dataService,
                IAppData appData,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("OnDemandTLS");

                if (dataService.CaddyContainers.SelectMany(s => s.ProxyAddresses).Any(q => q.DNSName.Equals(domain, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogInformation("Got successful On-Demand-TLS ask for {DnsName}", domain);
                    return TypedResults.Ok();
                }

                var dbResult = await appData.Get(domain);

                if (dbResult.Any())
                {
                    logger.LogInformation("Got successful On-Demand-TLS ask for {DnsName}", domain);
                    return TypedResults.Ok();
                }

                logger.LogDebug("Got failed On-Demand-TLS ask for {DnsName}", domain);
                return TypedResults.BadRequest();
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            await app.RunAsync(Program.CancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal(ex, "Fatal error in application.");
        }

    }

    static string exampleJson = @"{
  ""App"": {
    ""Url"": ""http://0.0.0.0:5000/"",
    ""DockerHostSettings"": [
      {
        ""FriendlyName"": ""Local Docker"",
        ""Endpoint"": ""unix:///var/run/docker.sock""       //Linux Socket
        //""Endpoint"": ""npipe://./pipe/docker_engine""    //Windows Socket
        //""Endpoint"": ""tcp://192.168.1.1:2375""          //TCP Socket        
      }
    ]
  },
  ""Serilog"": {
    ""MinimumLevel"": {
      ""Default"": ""Information"",
      ""Override"": {
        ""Microsoft"": ""Warning"",
        ""Microsoft.Hosting.Lifetime"": ""Warning""
      }
    },
    ""WriteTo"": [
      {
        ""Name"": ""Console""
      },
      {
        ""Name"": ""File"",
        ""Args"": {
          ""path"": ""logs\\log.txt"",
          ""rollingInterval"": ""Day""
        }
      }
    ],
    ""Enrich"": [ ""FromLogContext"", ""WithThreadId"" ]
  }
}";

}