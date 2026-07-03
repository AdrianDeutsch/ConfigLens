// Fixture: options bindings with type mismatches (CL005) and a binding to a
// section that does not exist (CL006).
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OptionsApp;

internal static class Program
{
    public static void Main()
    {
        IConfiguration config = null!;
        IServiceCollection services = null!;

        services.Configure<ServerOptions>(config.GetSection("Server"));
        services.Configure<AuditOptions>(config.GetSection("Audit"));

        Console.WriteLine("OptionsApp fixture");
    }
}
