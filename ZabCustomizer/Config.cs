using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZabCustomizer;

[Serializable]
public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string LastBrowseDirectory { get; set; } = ".";

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
