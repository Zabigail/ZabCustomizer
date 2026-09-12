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
    public record class PenumbraModOptionGroup(string Id, string Name, string DisplayName)
    {
        public bool IsDestinationTarget(CustomizeDestination destination)
        {
            if (destination.GroupId != null)
            {
                return destination.GroupId == Id;
            }
            else if (destination.GroupJsonFilename != null)
            {
                string groupName = Path.GetFileNameWithoutExtension(destination.GroupJsonFilename).Substring("group_000_".Length);
                return groupName.Equals(Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // destination has neither group ID nor group JSON filename, which is invalid
                return false;
            }
        }
    }

    public static async Task AddGroupOptionAsync(string modDirectory, PenumbraModOptionGroup group, string optionDisplayName, Dictionary<string, string> fileReplacements)
    {
        // Read meta json
        var jsonPath = Path.Combine(modDirectory, "meta.json");
        JsonNode? modJsonNode = null;
        try
        {
            using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                modJsonNode = await JsonNode.ParseAsync(stream).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Exception reading mod '{modDirectory}'.");
        }

        if (modJsonNode != null && modJsonNode.GetValueKind() == JsonValueKind.Object && modJsonNode["Groups"] is JsonArray groupArray)
        {
            // Add to the correct group
            foreach (var groupNode in groupArray)
            {
                if (groupNode != null && groupNode.GetValueKind() == JsonValueKind.Object
                    && groupNode["Id"] is JsonValue idValue && idValue.GetValueKind() == JsonValueKind.String && idValue.GetValue<string>() == group.Id
                    && groupNode["Options"] is JsonArray optionsArray)
                {
                    optionsArray.Add(new
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = optionDisplayName,
                        Description = $"Added with Zab's Customizer on {DateTime.Now.ToShortDateString()}",
                        Files = fileReplacements,
                    });
                    break;
                }
            }

            // Write meta json
            try
            {
                using (var stream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write))
                using (var writer = new Utf8JsonWriter(stream))
                {
                    modJsonNode.WriteTo(writer);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Exception writing mod '{modDirectory}'.");
            }
        }
        else
        {
            Plugin.Log.Error("Could not read mod.");
        }
    }

    public static async Task<IReadOnlyList<PenumbraModOptionGroup>> GetGroupsAsync(string modDirectory)
    {
        var jsonPath = Path.Combine(modDirectory, "meta.json");
        List<PenumbraModOptionGroup> groups = new();
        try
        {
            using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var modJsonNode = await JsonNode.ParseAsync(stream).ConfigureAwait(false);
                if (modJsonNode != null && modJsonNode.GetValueKind() == JsonValueKind.Object && modJsonNode["Groups"] is JsonArray groupArray)
                {
                    foreach (var groupNode in groupArray)
                    {
                        if (groupNode != null && groupNode.GetValueKind() == JsonValueKind.Object && groupNode["Id"] is JsonValue idValue && idValue.GetValueKind() == JsonValueKind.String && groupNode["Name"] is JsonValue nameValue && nameValue.GetValueKind() == JsonValueKind.String)
                        {
                            string id = idValue.GetValue<string>();
                            string name = nameValue.GetValue<string>();
                            string displayName = name;

                            if (groupNode["DisplayName"] is JsonValue displayNameValue && displayNameValue.GetValueKind() == JsonValueKind.String)
                            {
                                displayName = displayNameValue.GetValue<string>();
                            }

                            groups.Add(new(id, name, displayName));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Exception getting groups of mod '" + modDirectory + "'.");
        }
        return groups;
    }
}
