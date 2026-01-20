namespace LNotification.Internal;

public abstract class ProviderConfigBase
{
    public bool Enabled { get; set; } = true;
    public string Alias { get; set; } = "default";
}
