using System.ComponentModel.DataAnnotations;

namespace Domain.Models;
public class ManualProxy
{
    [Key]
    public int Id { get; set; }
    public string DnsName { get; set; }
    public string TargetHostName { get; set; }
    public ushort TargetPort { get; set; }
}
