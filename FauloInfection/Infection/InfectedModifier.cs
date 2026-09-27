using MiraAPI.Modifiers;
using MiraAPI.Translation;

namespace FauloInfection.Infection;

/// <summary>
/// Marca sincronizada para los jugadores que han sido convertidos
/// al equipo infectado después de comenzar la partida.
/// -
/// This is for marking the converted players to the infected team
/// after the game already started.
/// </summary>

public sealed class InfectedModifier : BaseModifier
{
    public override string ModifierName =>
        MiraLocaleManager.Get(
            "FauloInfection.Modifier.Infected.Name",
            "Infected");

    public override bool HideOnUi => false;

    public override bool ShowInFreeplay => true;
}