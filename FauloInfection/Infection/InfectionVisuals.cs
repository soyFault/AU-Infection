using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using FauloInfection.Buttons;
using FauloInfection.GameModes;
using MiraAPI.Hud;

namespace FauloInfection.Infection;

/// <summary>
/// Presentación visual local de un jugador convertido al equipo infectado.
/// El estado real de infección sigue perteneciendo a InfectedModifier.
/// </summary>
public static class InfectionVisuals
{
    public static void ApplyInfected(
        PlayerControl? player)
    {
        if (player == null ||
            player.MyPhysics == null)
        {
            return;
        }

        // Si por alguna razón el prefab de intro no está disponible,
        // aplicamos al menos el cuerpo correcto sin intentar animarlo.
        if (!HudManager.InstanceExists ||
            HudManager.Instance.IntroPrefab == null)
        {
            player.MyPhysics.SetBodyType(
                InfectionMode.GetInfectedBodyType());

            player.cosmetics.SetFlipX(
                player.MyPhysics.FlipX);

            return;
        }

        player.StartCoroutine(
            CoTransformIntoSeeker(player));
    }

    public static void RemoveInfected(
        PlayerControl? player)
    {
        if (player == null ||
            player.MyPhysics == null)
        {
            return;
        }

        player.MyPhysics.SetBodyType(
            InfectionMode.GetSurvivorBodyType());

        player.cosmetics.SetBodyCosmeticsVisible(true);

        player.cosmetics.SetFlipX(
            player.MyPhysics.FlipX);
    }

    private static IEnumerator CoTransformIntoSeeker(
        PlayerControl player)
    {
        var wasMoveable =
            player.moveable;

        var wasFacingLeft =
            player.MyPhysics.FlipX;

        // Solo bloqueamos movimiento en el cliente dueño del jugador.
        // Los demás clientes únicamente reproducen la presentación.
        if (player.AmOwner)
        {
            player.NetTransform.Halt();
            player.moveable = false;
        }

        player.MyPhysics.SetBodyType(
            InfectionMode.GetInfectedBodyType());

        // Cambiar el BodyType puede dejar cosméticos mirando
        // hacia la dirección anterior hasta que el jugador se mueva.
        player.MyPhysics.FlipX =
            wasFacingLeft;

        player.cosmetics.SetFlipX(
            wasFacingLeft);

        var introPrefab =
            HudManager.Instance.IntroPrefab;

        if (AprilFoolsMode.ShouldHorseAround())
        {
            yield return player.MyPhysics.CoAnimateCustom(
                introPrefab.HnSSeekerSpawnHorseInGameAnim);
        }
        else if (AprilFoolsMode.ShouldLongAround())
        {
            yield return player.MyPhysics.CoAnimateCustom(
                introPrefab.HnSSeekerSpawnLongInGameAnim);
        }
        else
        {
            // Igual que en la transformación HnS normal:
            // los cosméticos desaparecen durante la animación.
            // CoAnimateCustom vuelve a mostrarlos al terminar.
            player.cosmetics.SetBodyCosmeticsVisible(false);

            yield return player.MyPhysics.CoAnimateCustom(
                introPrefab.HnSSeekerSpawnAnim);
        }

        player.MyPhysics.FlipX =
            wasFacingLeft;

        player.cosmetics.SetFlipX(
            wasFacingLeft);

        if (!player.AmOwner)
        {
            yield break;
        }

        player.moveable =
            wasMoveable;

        // La transformación HnS puede refrescar el HUD después
        // de que InfectedModifier ya haya habilitado los botones.
        if (!HudManager.InstanceExists ||
            player.Data?.Role == null)
        {
            yield break;
        }

        HudManager.Instance.SetHudActive(
            player,
            player.Data.Role,
            true);

        var infectButton =
            CustomButtonSingleton<InfectButton>.Instance;

        infectButton.ResetCooldownAndOrEffect();
        infectButton.SetActive(
            true,
            player.Data.Role);

        var killButton =
            CustomButtonSingleton<InfectedKillButton>.Instance;

        killButton.ResetCooldownAndOrEffect();
        killButton.SetActive(
            true,
            player.Data.Role);
    }
}