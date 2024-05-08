#nullable disable warnings

using Domain.Models;

namespace Domain.Models;
public class Settings
{
    public DockerHostSettings[] DockerHostSettings { get; set; }
}

public class DockerHostSettings
{
    public string? FriendlyName { get; set; }
    public string Endpoint { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseTls { get; set; } = false;
}