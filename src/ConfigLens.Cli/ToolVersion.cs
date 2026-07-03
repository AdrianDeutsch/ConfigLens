using System.Reflection;

namespace ConfigLens.Cli;

/// <summary>Resolves the tool version MinVer stamped into the assembly.</summary>
internal static class ToolVersion
{
    /// <summary>Informational version without build metadata, e.g. <c>0.1.0-alpha.5</c>.</summary>
    public static string Current { get; } =
        typeof(ToolVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}
