// Fixture: dynamic key access that static analysis cannot resolve.
// Must produce CL900 notes — never a guessed key and never a false CL001.
using Microsoft.Extensions.Configuration;

internal static class Program
{
    public static void Main(string[] args)
    {
        IConfiguration config = null!;

        var featureName = args.Length > 0 ? args[0] : "default";
        var interpolated = config[$"Features:{featureName}"];   // CL900: dynamic segment

        var key = "App" + ":" + "Name";
        var concatenated = config[key];                         // CL900: local variable

        var resolvable = config["App:Version"];                 // High: literal, still resolved

        Console.WriteLine($"{interpolated}{concatenated}{resolvable}");
    }
}
