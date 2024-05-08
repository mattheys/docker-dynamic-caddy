namespace Domain.Models;

public record ProxyAddress()
{
    public int Index { get; set; }
    public string DNSName { get; set; }
    public string TargetHostName { get; set; }
    public ushort TargetPort { get; set; }

}
