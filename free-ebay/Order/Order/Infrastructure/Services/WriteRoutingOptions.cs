namespace Infrastructure.Services;

public sealed class WriteRoutingOptions
{
    public bool Enabled { get; set; }
    public string CurrentRegion { get; set; } = string.Empty;
    public List<string> Regions { get; set; } = new();
}
