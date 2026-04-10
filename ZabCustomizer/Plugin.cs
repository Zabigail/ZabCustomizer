using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Services;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Loader;

namespace ZabCustomizer;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    public static readonly Vector4 ColorPink = new(0.91f, 0.537f, 0.659f, 1f);
    public static readonly Vector4 ColorPinkHover = new(0.949f, 0.631f, 0.737f, 1f);
    public static readonly Vector4 ColorPinkPressed = new(0.831f, 0.424f, 0.557f, 1f);

    private readonly WindowSystem _windowSystem;
    private readonly DefinitionManager _definitionManager;
    private readonly TextureCompressor _textureCompressor;
    private readonly Config _config;

    // Penumbra IPCs
    private readonly List<IDisposable> _penumbraEventSubscribers = new();
    private readonly GetEnabledState _getEnabledState;
    private readonly GetModDirectory _getModDirectory;
    private readonly ReloadMod _reloadMod;

    private bool _isPenumbraAvailable = false;
    private readonly ConcurrentDictionary<string, CustomizeWindow> _customizeWindows = new();

    public Plugin()
    {
        _getEnabledState = new GetEnabledState(PluginInterface);
        _getModDirectory = new GetModDirectory(PluginInterface);
        _reloadMod = new ReloadMod(PluginInterface);

        _definitionManager = new DefinitionManager(Log);
        _textureCompressor = TextureCompressor.Create(Path.GetDirectoryName(PluginInterface.AssemblyLocation.FullName)!);
        _config = PluginInterface.GetPluginConfig() as Config ?? new Config();

        _windowSystem = new WindowSystem();
        PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        PluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
        CheckPenumbraApi();
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs args)
    {
        CheckPenumbraApi();
    }

    private void CheckPenumbraApi()
    {
        bool available;
        try
        {
            available = _getEnabledState.Invoke();
        }
        catch (IpcNotReadyError)
        {
            available = false;
        }

        if (available != _isPenumbraAvailable)
        {
            _isPenumbraAvailable = available;

            if (available)
            {
                Log.Debug("Penumbra available.");
                AddPenumbraSubscribers();
                _definitionManager.PenumbraModDirectory = _getModDirectory.Invoke();
            }
            else
            {
                Log.Debug("Penumbra unavailabe.");
                _definitionManager.PenumbraModDirectory = null;
                RemovePenumbraSubscribers();
            }
        }
    }

    private void AddPenumbraSubscribers()
    {
        _penumbraEventSubscribers.Add(PostEnabledDraw.Subscriber(PluginInterface, DrawPenumbraUI));
        _penumbraEventSubscribers.Add(ModDirectoryChanged.Subscriber(PluginInterface, PenumbraModDirectoryChanged));
    }

    private void DrawPenumbraUI(string modDirectory)
    {
        if (_definitionManager.PenumbraModDirectory == null)
        {
            return;
        }

        var fullModDirectory = Path.Combine(_definitionManager.PenumbraModDirectory, modDirectory);
        if (_definitionManager.TryGetModCustomizeDefinition(fullModDirectory, out var definition))
        {
            ImGuiHelpers.ScaledDummy(3.0f);
            using (ImRaii.PushColor(ImGuiCol.Button, ColorPink))
            using (ImRaii.PushColor(ImGuiCol.ButtonHovered, ColorPinkHover))
            using (ImRaii.PushColor(ImGuiCol.ButtonActive, ColorPinkPressed))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Carrot, "Customize"))
                {
                    var window = _customizeWindows.GetOrAdd(fullModDirectory, directory =>
                    {
                        var newWindow = new CustomizeWindow(directory, definition, TextureProvider, _definitionManager, _textureCompressor, _config);
                        _windowSystem.AddWindow(newWindow);
                        newWindow.Closed += () =>
                        {
                            _windowSystem.RemoveWindow(newWindow);
                            _customizeWindows.Remove(directory, out _);
                        };
                        newWindow.ModChanged += () => _reloadMod.Invoke(modDirectory);
                        return newWindow;
                    });
                    window.IsOpen = true;
                    window.BringToFront();
                }
            }
            if (ImGui.IsItemHovered())
            {
                using (ImRaii.Tooltip())
                {
                    ImGui.Text("Open the customization window");
                }
            }
        }
    }

    private void PenumbraModDirectoryChanged(string modDirectory, bool isValid)
    {
        if (isValid)
        {
            Log.Debug("Penumbra mod directory changed.");
            _definitionManager.PenumbraModDirectory = modDirectory;
        }
    }

    private void RemovePenumbraSubscribers()
    {
        foreach (var subscriber in _penumbraEventSubscribers)
        {
            subscriber.Dispose();
        }
        _penumbraEventSubscribers.Clear();
    }

    public void Dispose()
    {
        RemovePenumbraSubscribers();

        PluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;

        _textureCompressor.Dispose();
        _definitionManager.Dispose();
    }
}
