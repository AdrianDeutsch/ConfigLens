// Fixture: reads keys that do not exist. "App:Timeuot" is a typo of the
// existing "App:Timeout" (CL001 + CL007); "Database:Host" is simply missing
// (CL001 only). The typo also leaves "App:Timeout" unread (CL003).
using Microsoft.Extensions.Configuration;

internal static class Program
{
    public static void Main()
    {
        IConfiguration config = null!;

        var name = config["App:Name"];
        var timeout = config["App:Timeuot"];
        var host = config["Database:Host"];

        Console.WriteLine($"{name}{timeout}{host}");
    }
}
