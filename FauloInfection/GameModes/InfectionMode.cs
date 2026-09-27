using FauloInfection.Infection;
using MiraAPI.GameModes;
using UnityEngine;

namespace FauloInfection.GameModes;

// Hereda-Copia las propiedades de Escondidas // We inherit the HideAndSeekMode behaviors
public sealed class InfectionMode : HideAndSeekMode
{
    // Este color es el que aparece en el selector de partida y el mismo que aparece en el Menu Principal .
    // This color is the one in the game screen. Same as Main Menu
    public static Color InfectionColor { get; } =
        new Color32(69, 214, 107, 255);

    // Hace el modo visible porque Mira API por defecto los oculta en builds no dev.
    // (Makes the gamemode selectable bc Mira API hides it by default in non-dev builds)
    public override bool HideMode => false;

    public override string Name =>
        "FauloInfection.GameMode.Infection.Name";

    public override string Description =>
        "FauloInfection.GameMode.Infection.Description";

    public override Color Color => InfectionColor;
    
    // Inicializa el estado de Infection solo después de asignar todos los roles,
    // para poder identificar de forma fiable al Seeker inicial mediante su rol base.
    
    // Initialize Infection state only after all player roles have been assigned,
    // so the initial Seeker can be reliably identified from their base role.

    public override void PostAssignRoles(
        LogicRoleSelectionNormal instance)
    {
        base.PostAssignRoles(instance);

        InfectionManager.InitializeRound();
    }
}