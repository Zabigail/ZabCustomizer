using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ZabCustomizer;

/// <summary>
/// Identifies a Penumbra modpack option group to add an entry to, and a game path the entry should map to.
/// </summary>
/// <param name="GroupJsonFilename">The filename of the .json file that defines the group to add to, relative to the mod directory.</param>
/// <param name="GamePath">The absolute game path to map the entry to.</param>
public record class CustomizeDestination(string GroupJsonFilename, string GamePath);

/// <summary>
/// One customizable slot the player can add a texture to.
/// </summary>
/// <param name="DisplayName">The player-facing name of the slot.</param>
/// <param name="OutputDirectory">The directory within the mod directory to place the new file.</param>
/// <param name="Destinations">The groups and game paths to add the new file to.</param>
/// <param name="AspectRecommendationWidth">The numberator (width) of the recommended aspect ratio.</param>
/// <param name="AspectRecommendationHeight">The denominator (height) of the recommended aspect ratio.</param>
public record class CustomizeSlot(string DisplayName, string OutputDirectory, List<CustomizeDestination> Destinations, int AspectRecommendationWidth = 1, int AspectRecommendationHeight = 1, string Notes = "");

/// <summary>
/// Specifies the customization options that are available for a Penumbra mod.
/// </summary>
/// <param name="Slots">The various options that can be customized.</param>
public record class CustomizeDefinition(List<CustomizeSlot> Slots, string Notes = "")
{
    /// <summary>
    /// The expected filename of customize definitions within Penumbra mods.
    /// </summary>
    public const string Filename = "customize.json";

    /// <summary>
    /// Reads a customize definition from the given UTF-8 JSON stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The customize definition that was read, if any.</returns>
    public static CustomizeDefinition? FromStream(Stream stream)
    {
        return JsonSerializer.Deserialize<CustomizeDefinition>(stream);
    }
}
