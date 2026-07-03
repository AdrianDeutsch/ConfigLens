// Fixture: "Features:Legacy" exists in configuration but is never read (CL003).
// "Logging:*" is framework-consumed and must not be flagged.
using Microsoft.Extensions.Configuration;

internal static class Program
{
    public static void Main()
    {
        IConfiguration config = null!;

        var name = config["App:Name"];

        Console.WriteLine(name);
    }
}
