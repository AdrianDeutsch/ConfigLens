namespace OptionsApp;

/// <summary>Bound to the "Server" section; every value in config mismatches its type.</summary>
public sealed class ServerOptions
{
    public int Port { get; set; }

    public bool EnableTls { get; set; }

    public LogMode Mode { get; set; }

    public string? Banner { get; set; }
}

public enum LogMode
{
    Quiet,
    Verbose,
}
