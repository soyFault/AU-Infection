using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI.PluginLoading;
using Reactor;

namespace FauloInfection;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
public sealed class InfectionPlugin : BasePlugin, IMiraPlugin
{
    public const string Id = "com.fault.fauloinfection";
    public const string Name = "FauloInfection";
    public const string Version = "0.1.0";

    public Harmony Harmony { get; } = new(Id);

    public string OptionsTitleText => Name;

    public string CustomOptionMenuNameTwo => Name;

    public ConfigFile GetConfigFile() => Config;

    public override void Load()
    {
        Log.LogInfo($"{Name} v{Version} cargado.");

        Harmony.PatchAll();
    }
}
