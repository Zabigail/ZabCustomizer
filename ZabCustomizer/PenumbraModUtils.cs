using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ZabCustomizer;

/// <summary>
/// Provides static helpers for manipulating Penumbra mods on disk.
/// </summary>
public static class PenumbraModUtils
{
    public static async Task AddGroupOptionAsync(string groupJsonPath, string optionDisplayName, Dictionary<string, string> fileReplacements)
    {
        JsonNode? groupJson;
        using (var stream = new FileStream(groupJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            groupJson = await JsonNode.ParseAsync(stream);
        }

        if (groupJson != null && groupJson["Options"] is JsonArray array)
        {
            array.Add(new
            {
                Name = optionDisplayName,
                Description = $"Added with Zab's Customizer on {DateTime.Now.ToShortDateString()}",
                Files = fileReplacements,
            });

            using (var stream = new FileStream(groupJsonPath, FileMode.Create, FileAccess.Write))
            using (var writer = new Utf8JsonWriter(stream))
            {
                groupJson.WriteTo(writer);
            }
        }
    }
}
