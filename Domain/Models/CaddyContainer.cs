namespace Domain.Models;

public record CaddyContainer(string Id, string EndPoint, string? FriendlyName = null)
{
    public string Name => FriendlyName ?? EndPoint;
    public List<ProxyAddress> ProxyAddresses { get; set; } = new List<ProxyAddress>();
}
