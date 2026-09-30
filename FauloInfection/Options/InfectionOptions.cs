using FauloInfection.GameModes;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;

namespace FauloInfection.Options;

/// <summary>
/// Gameplay options specific to the Infection game mode.
/// Opciones de jugabilidad específicas del modo Infección.
/// </summary>
public sealed class InfectionOptions : AbstractOptionGroup<InfectionMode>
{
    public override string GroupName =>
        "FauloInfection.Options.Group";

    public override uint GroupPriority => 0;

    /// <summary>
    /// Determines whether infected players are allowed to kill.
    /// Determina si los jugadores infectados pueden matar.
    /// </summary>
    public ModdedToggleOption AllowInfectedKills { get; set; } =
        new(
            "FauloInfection.Options.AllowInfectedKills",
            false);

    /// <summary>
    /// Determines whether infected players may kill other infected players.
    /// Determina si los jugadores infectados pueden matar a otros infectados.
    /// </summary>
    public ModdedToggleOption FriendlyFire { get; set; } =
        new(
            "FauloInfection.Options.FriendlyFire",
            false)
        {
            // Friendly Fire only matters when killing is enabled.
            // Fuego Amigo solo tiene sentido cuando los asesinatos están habilitados.
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .AllowInfectedKills
                    .Value,
        };

    public ModdedNumberOption InfectionChance { get; set; } =
        new(
            "FauloInfection.Options.InfectionChance",
            100f,
            0f,
            100f,
            5f,
            MiraNumberSuffixes.Percent);

    public ModdedToggleOption UseHnsVision { get; set; } =
        new(
            "FauloInfection.Options.UseHnsVision",
            true);

    /// <summary>
    /// Determines the width of the survivor flashlight cone
    /// when Infection uses Hide and Seek vision.
    ///
    /// Determina el ancho del cono de linterna de los supervivientes
    /// cuando Infection usa la visión de Hide and Seek.
    /// </summary>
    public ModdedNumberOption SurvivorFlashlightSize { get; set; } =
        new(
            "FauloInfection.Options.SurvivorFlashlightSize",
            0.35f,
            0.1f,
            0.5f,
            0.05f,
            MiraNumberSuffixes.Multiplier,
            "0.00")
        {
            // Flashlight size only matters while the HnS flashlight
            // vision system is active.
            //
            // El tamaño de la linterna solo tiene sentido mientras
            // el sistema de visión HnS esté activo.
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .UseHnsVision
                    .Value,
        };

    /// <summary>
    /// Determines the width of the infected flashlight cone
    /// when Infection uses Hide and Seek vision.
    ///
    /// Determina el ancho del cono de linterna de los infectados
    /// cuando Infection usa la visión de Hide and Seek.
    /// </summary>
    public ModdedNumberOption InfectedFlashlightSize { get; set; } =
        new(
            "FauloInfection.Options.InfectedFlashlightSize",
            0.35f,
            0.1f,
            0.5f,
            0.05f,
            MiraNumberSuffixes.Multiplier,
            "0.00")
        {
            // Flashlight size only matters while the HnS flashlight
            // vision system is active.
            //
            // El tamaño de la linterna solo tiene sentido mientras
            // el sistema de visión HnS esté activo.
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .UseHnsVision
                    .Value,
        };

    public ModdedToggleOption NotifyInfections { get; set; } =
        new(
            "FauloInfection.Options.NotifyInfections",
            true);

    public ModdedNumberOption CrewmateVision { get; set; } =
        new(
            "FauloInfection.Options.CrewmateVision",
            0.6f,
            0.25f,
            5f,
            0.05f,
            MiraNumberSuffixes.Multiplier,
            "0.00")

        {
            Visible = () =>
                !OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .UseHnsVision
                    .Value,
        };

    public ModdedNumberOption InfectedVision { get; set; } =
        new(
            "FauloInfection.Options.InfectedVision",
            0.6f,
            0.25f,
            5f,
            0.05f,
            MiraNumberSuffixes.Multiplier,
            "0.00")
        {
            Visible = () =>
                !OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .UseHnsVision
                    .Value,
        };

    public ModdedNumberOption InitialInfectedVision { get; set; } =
        new(
            "FauloInfection.Options.InitialInfectedVision",
            0.6f,
            0.25f,
            5f,
            0.05f,
            MiraNumberSuffixes.Multiplier,
            "0.00")
        {
            Visible = () =>
                !OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .UseHnsVision
                    .Value,
        };

    public ModdedToggleOption UseHorseModel { get; set; } =
        new(
            "FauloInfection.Options.UseHorseModel",
            false);

    /// <summary>
    /// Determines whether survivors may use vents.
    /// Determina si los supervivientes pueden usar ductos.
    /// </summary>
    public ModdedToggleOption CrewmatesCanVent { get; set; } =
        new(
            "FauloInfection.Options.CrewmatesCanVent",
            true);

    /// <summary>
    /// Determines whether the initial Seeker and converted infected
    /// players may use vents.
    ///
    /// Determina si el Seeker inicial y los infectados convertidos
    /// pueden usar ductos.
    /// </summary>
    public ModdedToggleOption InfectedCanVent { get; set; } =
        new(
            "FauloInfection.Options.InfectedCanVent",
            false);

    public ModdedNumberOption VentUses { get; set; } =
        new(
            "FauloInfection.Options.VentUses",
            1f,
            0f,
            15f,
            1f,
            MiraNumberSuffixes.None,
            "0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .CrewmatesCanVent
                    .Value ||
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .InfectedCanVent
                    .Value,
        };

    public ModdedNumberOption VentDuration { get; set; } =
        new(
            "FauloInfection.Options.VentDuration",
            3f,
            1f,
            30f,
            1f,
            MiraNumberSuffixes.Seconds,
            "0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .CrewmatesCanVent
                    .Value ||
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .InfectedCanVent
                    .Value,
        };

    public ModdedNumberOption VentCooldown { get; set; } =
        new(
            "FauloInfection.Options.VentCooldown",
            10f,
            0f,
            60f,
            2.5f,
            MiraNumberSuffixes.Seconds,
            "0.0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .CrewmatesCanVent
                    .Value ||
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .InfectedCanVent
                    .Value,
        };

    public ModdedNumberOption AdrenalineActivationTime { get; set; } =
        new(
            "FauloInfection.Options.AdrenalineActivationTime",
            60f,
            0f,
            180f,
            5f,
            MiraNumberSuffixes.Seconds,
            "0");

    public ModdedToggleOption EnableTasks { get; set; } =
        new(
            "FauloInfection.Options.EnableTasks",
            true);

    public ModdedNumberOption CommonTasks { get; set; } =
        new(
            "FauloInfection.Options.CommonTasks",
            1f,
            0f,
            4f,
            1f,
            MiraNumberSuffixes.None,
            "0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .EnableTasks
                    .Value,
        };

    public ModdedNumberOption ShortTasks { get; set; } =
        new(
            "FauloInfection.Options.ShortTasks",
            2f,
            0f,
            8f,
            1f,
            MiraNumberSuffixes.None,
            "0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .EnableTasks
                    .Value,
        };

    public ModdedNumberOption LongTasks { get; set; } =
        new(
            "FauloInfection.Options.LongTasks",
            1f,
            0f,
            4f,
            1f,
            MiraNumberSuffixes.None,
            "0")
        {
            Visible = () =>
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .EnableTasks
                    .Value,
        };

    public ModdedToggleOption DisableProximityIndicators { get; set; } =
        new(
            "FauloInfection.Options.DisableProximityIndicators",
            false);

    public ModdedToggleOption InfectedCanSabotageDoors { get; set; } =
        new(
            "FauloInfection.Options.InfectedCanSabotageDoors",
            false);
}
