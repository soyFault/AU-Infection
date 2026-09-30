using FauloInfection.GameModes;
using FauloInfection.Infection;
using FauloInfection.Networking;
using FauloInfection.Options;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace FauloInfection.Buttons;

/// <summary>
/// Kill button used by infected players that kept a non-Impostor base role.
/// Botón de matar usado por infectados que conservaron un rol base no-Impostor.
/// </summary>
public sealed class InfectedKillButton :
    CustomActionButton<PlayerControl>
{
    private static LoadableAsset<Sprite>? killSprite;

    public override string Name =>
        "FauloInfection.Button.Kill";

    public override float Cooldown =>
        InfectionManager.InfectedKillCooldownSeconds;

    public override ButtonLocation Location { get; set; } =
        ButtonLocation.BottomRight;

    public override LoadableAsset<Sprite> Sprite =>
        killSprite ??=
            new LoadableAssetWrapper<Sprite>(
                HudManager.Instance.KillButton.graphic.sprite);

    public override PlayerControl? GetTarget()
    {
        var localPlayer =
            PlayerControl.LocalPlayer;

        if (localPlayer == null)
        {
            return null;
        }

        return localPlayer.GetClosestPlayer(
            includeImpostors: true,
            distance: Distance,
            ignoreColliders: false,
            includeGhosts: false,
            predicate: player =>
                InfectionManager.CanKillTarget(
                    localPlayer,
                    player) &&
                InfectionVisionController
                    .IsTargetInsideFlashlight(
                        localPlayer,
                        player));
    }

    public override bool IsTargetValid(
        PlayerControl? target)
    {
        return InfectionManager.CanKillTarget(
                   PlayerControl.LocalPlayer,
                   target) &&
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
        var localPlayer =
            PlayerControl.LocalPlayer;

        if (localPlayer == null ||
            localPlayer.Data?.Role == null)
        {
            return false;
        }

        // The initial Seeker already has the vanilla Kill button.
        // El Seeker inicial ya posee el botón Kill vanilla.
        return InfectionManager.IsActive &&
               InfectionManager.IsInfected(localPlayer) &&
               !localPlayer.Data.Role.IsImpostor &&
               OptionGroupSingleton<InfectionOptions>
                   .Instance
                   .AllowInfectedKills
                   .Value;
    }

    public override bool CanUse()
    {
        var localPlayer =
            PlayerControl.LocalPlayer;

        return localPlayer != null &&
               !localPlayer.Data.IsDead &&
               InfectionManager.IsInfected(localPlayer) &&
               base.CanUse();
    }

    protected override void OnClick()
    {
        var source =
            PlayerControl.LocalPlayer;

        var target =
            Target;

        if (source == null ||
            target == null)
        {
            return;
        }

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            InfectionManager.TryKill(
                source,
                target);
        }
        else if (AmongUsClient.Instance != null)
        {
            Rpc<KillRequestRpc>.Instance.SendTo(
                source,
                AmongUsClient.Instance.HostId,
                target.PlayerId);
        }

        ResetTarget();
    }
}