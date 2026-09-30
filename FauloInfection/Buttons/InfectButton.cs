using FauloInfection.Assets;
using FauloInfection.GameModes;
using FauloInfection.Infection;
using FauloInfection.Networking;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace FauloInfection.Buttons;

public sealed class InfectButton : CustomActionButton<PlayerControl>
{
    public override string Name =>
        "FauloInfection.Button.Infect";
    
    public override float Cooldown =>
        InfectionManager.InfectCooldownSeconds;

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

        // Find the closest valid crewmate within the role's normal ability range.
        // Busca al tripulante válido más cercano dentro del alcance normal de la habilidad.
        //
        // Cuando usamos la visión HnS, el objetivo también debe estar
        // dentro del cono hacia el que está apuntando la linterna.
        return localPlayer.GetClosestPlayer(
            includeImpostors: true,
            distance: Distance,
            ignoreColliders: false,
            includeGhosts: false,
            predicate: player =>
                !InfectionManager.IsInfected(player) &&
                InfectionVisionController
                    .IsTargetInsideFlashlight(
                        localPlayer,
                        player));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        return target != null &&
               target.Data != null &&
               !target.Data.Disconnected &&
               !target.Data.IsDead &&
               !target.inVent &&
               !InfectionManager.IsInfected(target) &&
               InfectionVisionController
                   .IsTargetInsideFlashlight(
                       PlayerControl.LocalPlayer,
                       target);
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
        var source = PlayerControl.LocalPlayer;
        var target = Target;

        if (source == null ||
            target == null)
        {
            return;
        }

        // The host validates infections directly.
        // El host valida las infecciones directamente.
        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            InfectionManager.TryInfect(
                source,
                target);
        }
        // Non-host clients send only the target ID to the host.
        // Los clientes que no son host envían únicamente el ID del objetivo al host.
        else if (AmongUsClient.Instance != null)
        {
            Rpc<InfectRequestRpc>.Instance.SendTo(
                source,
                AmongUsClient.Instance.HostId,
                target.PlayerId);
        }

        // Clear the selected target after the action is attempted.
        // Limpia el objetivo seleccionado después de intentar la acción.
        ResetTarget();
    }
}