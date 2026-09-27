using FauloInfection.Assets;
using FauloInfection.GameModes;
using FauloInfection.Infection;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace FauloInfection.Buttons;

public sealed class InfectButton : CustomActionButton<PlayerControl>
{
    public override string Name =>
        "FauloInfection.Button.Infect";

    public override float Cooldown => 5f;

    public override ButtonLocation Location { get; set; } =
        ButtonLocation.BottomRight;

    public override LoadableAsset<Sprite> Sprite =>
        InfectionAssets.InfectButton;

    public override PlayerControl? GetTarget()
    {
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null)
        {
            return null;
        }

        // EN: Find the closest valid crewmate within the role's normal ability range.
        // ES: Busca al tripulante válido más cercano dentro del alcance normal de la habilidad.
        return localPlayer.GetClosestPlayer(
            includeImpostors: true,
            distance: Distance,
            ignoreColliders: false,
            includeGhosts: false,
            predicate: player =>
                !InfectionManager.IsInfected(player));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        return target != null &&
               target.Data != null &&
               !target.Data.Disconnected &&
               !target.Data.IsDead &&
               !target.inVent &&
               !InfectionManager.IsInfected(target);
    }

    public override void SetOutline(bool active)
    {
        Target?.cosmetics.SetOutline(
            active,
            new Il2CppSystem.Nullable<Color>(
                InfectionMode.InfectionColor));
    }

    public override bool Enabled(RoleBehaviour? role)
    {
        var localPlayer = PlayerControl.LocalPlayer;

        return InfectionManager.IsActive &&
               localPlayer != null &&
               localPlayer.Data != null &&
               !localPlayer.Data.Disconnected &&
               !localPlayer.Data.IsDead &&
               InfectionManager.IsInfected(localPlayer);
    }

    public override bool CanUse()
    {
        var localPlayer = PlayerControl.LocalPlayer;

        return localPlayer != null &&
               localPlayer.Data != null &&
               !localPlayer.Data.IsDead &&
               InfectionManager.IsInfected(localPlayer) &&
               base.CanUse();
    }

    protected override void OnClick()
    {
        // Nothing here yet, just testing the button
    }
}