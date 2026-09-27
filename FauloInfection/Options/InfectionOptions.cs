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
}