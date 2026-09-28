using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI;
using MiraAPI.Translation;
using MiraAPI.PluginLoading;
using Reactor;
using FauloInfection.Compatibility;
using FauloInfection.Patches;

namespace FauloInfection;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
// TOU:Mira sigue siendo completamente opcional.
// Si está instalado, BepInEx carga FauloInfection después de TOU
// para que podamos instalar la capa de compatibilidad de forma segura.
[BepInDependency(
    "auavengers.tou.mira",
    BepInDependency.DependencyFlags.SoftDependency)]
public sealed class InfectionPlugin : BasePlugin, IMiraPlugin
{
    public const string Id = "com.fault.fauloinfection";
    public const string Name = "FauloInfection";
    public const string Version = "0.1.0";

    public Harmony Harmony { get; } = new(Id);

    public string OptionsTitleText => Name;
    
    public string GetAbbreviatedModName() =>
        $"<b><color={InfectionCreditsColorPatch.CreditsColor}>FI</color></b>";

    public string CustomOptionMenuNameTwo => Name;

    public ConfigFile GetConfigFile() => Config;
    
    public InfectionPlugin()
    {
        MiraLocaleManager.Register(Id, "FauloInfection");
    }

    public override void Load()
    {
        Log.LogInfo($"{Name} v{Version} cargado.");

        Harmony.PatchAll();
        
        TownOfUsMiraCompatibility.Initialize(Harmony);
        
        VersionDisplay.Register();
    }
}