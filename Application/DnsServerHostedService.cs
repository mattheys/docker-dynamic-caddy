using ARSoft.Tools.Net.Dns;
using ARSoft.Tools.Net;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Domain;
using Domain.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application;
public class DnsServerHostedService : IHostedService
{
    private readonly DataService dataService;
    private readonly ILogger<DnsServerHostedService> logger;
    private readonly IAppData appData;
    private DnsServer dnsServer;

    public DnsServerHostedService(DataService dataService, ILogger<DnsServerHostedService> logger, IAppData appData)
    {
        this.dataService = dataService;
        this.logger = logger;
        this.appData = appData;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        logger.LogInformation("Starting DNS Server");

        dnsServer = new DnsServer();

        dnsServer.QueryReceived += DnsServer_QueryReceived;

        dnsServer.Start();

        logger.LogInformation("DNS Server Started");

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        logger.LogInformation("Stopping DNS Server");

        dnsServer.Stop();

        logger.LogInformation("DNS Server Stopped");
    }

    private async Task DnsServer_QueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage query) return;
        DnsMessage response = query.CreateResponseInstance();

        try
        {

            List<DnsRecordBase> records = new List<DnsRecordBase>();

            foreach (var question in query.Questions)
            {
                List<DnsRecordBase>? answerRecrods = null;

                switch (question.RecordType)
                {
                    case RecordType.Srv:
                        logger.LogInformation("Got a Dns Request type {RecordType} for {DnsName}", question.RecordType, question.Name);
                        answerRecrods = await GetSrvRecord(question);
                        break;
                    default:
                        logger.LogDebug("Got a Dns Request type {RecordType} for {DnsName}", question.RecordType, question.Name);
                        break;
                }

                if (answerRecrods?.Count > 0)
                {
                    records.AddRange(answerRecrods);
                }

            }

            if (records.Count == 0)
            {
                response.ReturnCode = ReturnCode.NxDomain;
                e.Response = response;
                using (var loga = LogContext.PushProperty("query", query, true))
                {
                    logger.LogDebug("No records for query");
                }
                return;
            }

            response.ReturnCode = ReturnCode.NoError;
            response.AnswerRecords = records;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing DNS request.");
        }

        e.Response = response;
    }

    private async Task<List<DnsRecordBase>> GetSrvRecord(DnsQuestion question)
    {
        var name = question.Name.ToString();
        var dns = name.Replace("srv-", "");
        if (dns.EndsWith(".")) { dns = dns.TrimEnd('.'); }

        var proxyContainers = dataService.CaddyContainers.SelectMany(q => q.ProxyAddresses).Where(q => q.DNSName.Equals(dns, StringComparison.OrdinalIgnoreCase));

        List<DnsRecordBase> returnList = new List<DnsRecordBase>();

        foreach (var item in proxyContainers)
        {

            var srv = new SrvRecord(question.Name, 60, 10, 10, item.TargetPort, DomainName.Parse(item.TargetHostName));

            using (var loga = LogContext.PushProperty("AnswerRecord", srv, true))
            {
                logger.LogInformation("Found Srv record for {DnsName}, {HostName}:{Port}", name, item.TargetHostName, item.TargetPort);
            }

            returnList.Add(srv);
        }

        var manualProxies = await appData.Get(dns);

        foreach (var item in manualProxies)
        {
            var srv = new SrvRecord(question.Name, 60, 10, 10, item.TargetPort, DomainName.Parse(item.TargetHostName));

            using (var loga = LogContext.PushProperty("AnswerRecord", srv, true))
            {
                logger.LogInformation("Found Srv record for {DnsName}, {HostName}:{Port}", name, item.TargetHostName, item.TargetPort);
            }

            returnList.Add(srv);
        }

        if (returnList.Count == 0)
        {
            logger.LogInformation("Found no records Srv records for {DnsName}", name);
            return Enumerable.Empty<DnsRecordBase>().ToList();
        }

        return returnList;

    }
}
