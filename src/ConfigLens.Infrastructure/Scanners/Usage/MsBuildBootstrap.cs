using Microsoft.Build.Locator;

namespace ConfigLens.Infrastructure.Scanners.Usage;

/// <summary>
/// Registers the MSBuild instance of the installed .NET SDK exactly once.
/// Must run before any Microsoft.Build type is loaded, which is why the
/// scanner calls it as its very first step.
/// </summary>
internal static class MsBuildBootstrap
{
    private static readonly Lock SyncRoot = new();

    /// <summary>Registers the default MSBuild instance if not registered yet.</summary>
    public static void EnsureRegistered()
    {
        lock (SyncRoot)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }
}
