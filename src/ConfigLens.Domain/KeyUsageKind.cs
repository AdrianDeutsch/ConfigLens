namespace ConfigLens.Domain;

/// <summary>
/// How a configuration key is read in code.
/// </summary>
public enum KeyUsageKind
{
    /// <summary>Indexer access: <c>configuration["Key"]</c>.</summary>
    IndexerAccess = 0,

    /// <summary>Typed read: <c>configuration.GetValue&lt;T&gt;("Key")</c>.</summary>
    GetValue = 1,

    /// <summary>Section access: <c>configuration.GetSection("Key")</c>.</summary>
    GetSection = 2,

    /// <summary>Options binding: <c>services.Configure&lt;T&gt;(section)</c>, <c>section.Bind(...)</c>, <c>section.Get&lt;T&gt;()</c>.</summary>
    OptionsBinding = 3,
}
