// Fixture: every supported way to read configuration, all statically resolvable.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UsageApp;

internal static class Program
{
    public static void Main()
    {
        IConfiguration config = null!;
        IServiceCollection services = null!;

        var name = config["App:Name"];                          // indexer, literal -> High
        var timeout = config[AppKeys.Timeout];                  // indexer, const   -> Medium
        var pageSize = config.GetValue<int>("App:PageSize");    // GetValue         -> High
        var smtp = config.GetSection("Smtp");                   // GetSection       -> High
        var host = config.GetSection("Smtp")["Host"];           // composed path    -> High
        services.Configure<UsageSettings>(config.GetSection("Usage")); // options binding

        Console.WriteLine($"{name}{timeout}{pageSize}{smtp.Key}{host}");
    }
}
