namespace UsageApp;

/// <summary>Options class bound to the "Usage" section.</summary>
public sealed class UsageSettings
{
    public int RetryCount { get; set; }

    public string? Endpoint { get; set; }
}
