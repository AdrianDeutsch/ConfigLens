// Fixture: a healthy app — every key is read, nothing missing, no secrets.
// Must produce zero findings and a score of 100.
using Microsoft.Extensions.Configuration;

internal static class Program
{
    public static void Main()
    {
        IConfiguration config = null!;

        var name = config["App:Name"];
        var pageSize = config.GetValue<int>("App:PageSize");

        Console.WriteLine($"{name}: {pageSize}");
    }
}
