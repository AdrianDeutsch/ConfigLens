using System.Globalization;
using System.Text;
using System.Text.Json;
using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Scanners;

/// <summary>
/// Parses a single JSON configuration file into flat <see cref="ConfigEntry"/>
/// values with 1-based line numbers. Flattening matches the JSON configuration
/// provider of Microsoft.Extensions.Configuration: nested objects become
/// colon-separated paths, array elements get numeric segments, and on duplicate
/// keys the last value wins. Comments and trailing commas are tolerated because
/// the runtime tolerates them in appsettings files as well.
/// </summary>
public static class JsonConfigFileParser
{
    /// <summary>Parses the raw file content into configuration entries.</summary>
    /// <param name="content">Raw UTF-8 file content, optionally with a BOM.</param>
    /// <param name="filePath">Path recorded in the entry locations.</param>
    /// <param name="environment">Environment the file belongs to; empty for the base file.</param>
    /// <exception cref="JsonException">The content is not valid JSON.</exception>
    public static IReadOnlyList<ConfigEntry> Parse(ReadOnlySpan<byte> content, string filePath, string environment)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(environment);

        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
        {
            content = content[3..];
        }

        var newlineOffsets = CollectNewlineOffsets(content);
        var reader = new Utf8JsonReader(content, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var entries = new List<ConfigEntry>();
        var entryIndexByKey = new Dictionary<ConfigKey, int>();
        var path = new List<string>();
        var containers = new List<Container>();
        string? pendingProperty = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    pendingProperty = reader.GetString();
                    break;

                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    EnterContainer(containers, path, ref pendingProperty, reader.TokenType == JsonTokenType.StartArray);
                    break;

                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                    ExitContainer(containers, path);
                    break;

                case JsonTokenType.String:
                    Emit(reader.GetString(), LineOf(newlineOffsets, reader.TokenStartIndex));
                    break;

                case JsonTokenType.Number:
                    Emit(Encoding.UTF8.GetString(reader.ValueSpan), LineOf(newlineOffsets, reader.TokenStartIndex));
                    break;

                case JsonTokenType.True:
                    Emit("true", LineOf(newlineOffsets, reader.TokenStartIndex));
                    break;

                case JsonTokenType.False:
                    Emit("false", LineOf(newlineOffsets, reader.TokenStartIndex));
                    break;

                case JsonTokenType.Null:
                    Emit(null, LineOf(newlineOffsets, reader.TokenStartIndex));
                    break;

                default:
                    break;
            }

            void Emit(string? value, int line)
            {
                if (containers.Count == 0)
                {
                    return; // A bare scalar document has no addressable key.
                }

                var segment = NextSegment(containers, ref pendingProperty);
                path.Add(segment);

                var key = ConfigKey.FromSegments(path);
                var entry = new ConfigEntry(key, value, environment, new SourceLocation(filePath, line));

                // Duplicate keys within one file: the last occurrence wins,
                // matching the behavior of the JSON configuration provider.
                if (entryIndexByKey.TryGetValue(key, out var existingIndex))
                {
                    entries[existingIndex] = entry;
                }
                else
                {
                    entryIndexByKey[key] = entries.Count;
                    entries.Add(entry);
                }

                path.RemoveAt(path.Count - 1);
            }
        }

        return entries;
    }

    /// <summary>Consumes the pending property name or the next array index as path segment.</summary>
    private static string NextSegment(List<Container> containers, ref string? pendingProperty)
    {
        var current = containers[^1];
        if (current.IsArray)
        {
            containers[^1] = current with { NextIndex = current.NextIndex + 1 };
            return current.NextIndex.ToString(CultureInfo.InvariantCulture);
        }

        var segment = pendingProperty!;
        pendingProperty = null;
        return segment;
    }

    private static void EnterContainer(List<Container> containers, List<string> path, ref string? pendingProperty, bool isArray)
    {
        var addedSegment = false;
        if (containers.Count > 0)
        {
            path.Add(NextSegment(containers, ref pendingProperty));
            addedSegment = true;
        }

        containers.Add(new Container(isArray, 0, addedSegment));
    }

    private static void ExitContainer(List<Container> containers, List<string> path)
    {
        var container = containers[^1];
        containers.RemoveAt(containers.Count - 1);
        if (container.AddedSegment)
        {
            path.RemoveAt(path.Count - 1);
        }
    }

    private static List<int> CollectNewlineOffsets(ReadOnlySpan<byte> content)
    {
        var offsets = new List<int>();
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == (byte)'\n')
            {
                offsets.Add(i);
            }
        }

        return offsets;
    }

    /// <summary>Converts a byte offset into a 1-based line number.</summary>
    private static int LineOf(List<int> newlineOffsets, long byteOffset)
    {
        var index = newlineOffsets.BinarySearch((int)byteOffset);
        if (index < 0)
        {
            index = ~index;
        }

        return index + 1;
    }

    /// <summary>Tracks one level of object/array nesting during parsing.</summary>
    private readonly record struct Container(bool IsArray, int NextIndex, bool AddedSegment);
}
