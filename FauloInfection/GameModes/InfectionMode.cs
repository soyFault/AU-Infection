using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using FauloInfection.Buttons;
using FauloInfection.GameModes;
using MiraAPI.Hud;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Presentación visual local de un jugador convertido al equipo infectado.
/// El estado real de infección sigue perteneciendo a InfectedModifier.
/// </summary>
public static class InfectionVisuals
{
    /// <summary>
    /// Aplica el outfit especial utilizado por el Wrangler
    /// en Hide and Seek Horse Mode.
    ///
    /// El Wrangler no es un PlayerBodyTypes independiente:
    /// utiliza cuerpo Normal junto con PlayerOutfitType.HorseWrangler.
    /// </summary>
    public static bool ApplyHorseWranglerOutfit(
        PlayerControl? player)
    {
        if (player == null ||
            player.Data == null ||
            player.MyPhysics == null ||
            GameManagerCreator.Instance == null ||
            GameManagerCreator.Instance.HideAndSeekManagerPrefab == null)
        {
            return false;
        }

        var hideAndSeekManager =
            GameManagerCreator
                .Instance
                .HideAndSeekManagerPrefab;

        var currentOutfit =
            player.CurrentOutfit;

        if (currentOutfit == null)
        {
            return false;
        }

        // Hide and Seek conserva el color y el nombre del jugador,
        // pero cambia a su outfit especial de Horse Wrangler.
        hideAndSeekManager
            .horseWranglerOutfit
            .ColorId =
            currentOutfit.ColorId;

        hideAndSeekManager
            .horseWranglerOutfit
            .PlayerName =
            currentOutfit.PlayerName;

        player.SetOutfit(
            hideAndSeekManager.horseWranglerOutfit,
            PlayerOutfitType.HorseWrangler);

        // El outfit HorseWrangler se presenta sobre cuerpo Normal.
        player.MyPhysics.SetBodyType(
            PlayerBodyTypes.Normal);

        ResetCosmeticsScale(
            player);

        player.cosmetics.SetBodyCosmeticsVisible(
            true);

        player.cosmetics.SetFlipX(
            player.MyPhysics.FlipX);

        return true;
    }

    public static void ApplyInfected(
        PlayerControl? player,
        bool blockMovement = true)
    {
        if (player == null ||
            player.MyPhysics == null)
        {
            return;
        }

        // Si por alguna razón el prefab de intro no está disponible,
        // aplicamos al menos la presentación final correcta.
        if (!HudManager.InstanceExists ||
            HudManager.Instance.IntroPrefab == null)
        {
            if (InfectionMode.ShouldUseHorseModel())
            {
                ApplyHorseWranglerOutfit(
                    player);
            }
            else
            {
                player.MyPhysics.SetBodyType(
                    InfectionMode.GetInfectedBodyType());

                ResetCosmeticsScale(
                    player);

                player.cosmetics.SetFlipX(
                    player.MyPhysics.FlipX);
            }

            return;
        }

        player.StartCoroutine(
            CoTransformIntoSeeker(
                player,
                blockMovement));
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

        ResetCosmeticsScale(
            player);

        player.cosmetics.SetBodyCosmeticsVisible(
            true);

        player.cosmetics.SetFlipX(
            player.MyPhysics.FlipX);
    }

    private static IEnumerator CoTransformIntoSeeker(
        PlayerControl player,
        bool blockMovement)
    {
        var wasMoveable =
            player.moveable;

        var wasFacingLeft =
            player.MyPhysics.FlipX;

        // Solo bloqueamos movimiento al jugador dueño durante
        // una conversión que ocurre dentro de la partida.
        //
        // Los clientes remotos únicamente reproducen la presentación.
        if (blockMovement &&
            player.AmOwner)
        {
            player.NetTransform.Halt();
            player.moveable = false;
        }

        var introPrefab =
            HudManager.Instance.IntroPrefab;

        if (InfectionMode.ShouldUseHorseModel())
        {
            // Hasta este punto un infectado recién convertido continúa
            // usando BodyType.Horse porque todavía conserva su outfit Default.
            //
            // Aplicar HorseWrangler cambia de forma atómica la presentación
            // real que necesita la animación de transformación.
            if (!ApplyHorseWranglerOutfit(
                    player))
            {
                player.MyPhysics.SetBodyType(
                    PlayerBodyTypes.Normal);

                ResetCosmeticsScale(
                    player);
            }

            player.MyPhysics.FlipX =
                wasFacingLeft;

            player.cosmetics.SetFlipX(
                wasFacingLeft);

            var animation =
                introPrefab
                    .HnSSeekerSpawnHorseInGameAnim;

            // AnimateCustom puede ser reemplazado por el refresh normal
            // de movimiento/idle en jugadores remotos.
            //
            // Ejecutamos directamente la animación en PlayerAnimations y
            // marcamos DoingCustomAnimation para que no sea sustituida.
            player.MyPhysics.DoingCustomAnimation =
                true;

            yield return player
                .MyPhysics
                .Animations
                .CoPlayCustomAnimation(
                    animation);

            player.cosmetics.AnimateSkinIdle();

            player.MyPhysics
                .Animations
                .PlayIdleAnimation();

            player.cosmetics.SetBodyCosmeticsVisible(
                true);

            player.MyPhysics.DoingCustomAnimation =
                false;

            // Al pasar de Horse a Normal, CosmeticsLayer puede conservar
            // la escala del caballo. Restauramos explícitamente la escala
            // del body actual para evitar el "mini-tripulante".
            ResetCosmeticsScale(
                player);
        }
        else
        {
            player.MyPhysics.SetBodyType(
                InfectionMode.GetInfectedBodyType());

            ResetCosmeticsScale(
                player);

            // Cambiar el BodyType puede dejar cosméticos mirando
            // hacia la dirección anterior hasta que el jugador se mueva.
            player.MyPhysics.FlipX =
                wasFacingLeft;

            player.cosmetics.SetFlipX(
                wasFacingLeft);

            if (AprilFoolsMode.ShouldLongAround())
            {
                yield return player.MyPhysics.CoAnimateCustom(
                    introPrefab.HnSSeekerSpawnLongInGameAnim);
            }
            else
            {
                // Igual que en la transformación HnS normal:
                // los cosméticos desaparecen durante la animación.
                // CoAnimateCustom vuelve a mostrarlos al terminar.
                player.cosmetics.SetBodyCosmeticsVisible(
                    false);

                yield return player.MyPhysics.CoAnimateCustom(
                    introPrefab.HnSSeekerSpawnAnim);
            }

            ResetCosmeticsScale(
                player);
        }

        player.MyPhysics.FlipX =
            wasFacingLeft;

        player.cosmetics.SetFlipX(
            wasFacingLeft);

        if (blockMovement &&
            player.AmOwner)
        {
            player.moveable =
                wasMoveable;
        }

        if (!player.AmOwner)
        {
            yield break;
        }

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

    /// <summary>
    /// Restaura la escala visual correspondiente al BodyType actual.
    ///
    /// Cambiar de Horse a Normal puede dejar CosmeticsLayer utilizando
    /// la escala del modelo anterior hasta el siguiente refresh vanilla.
    /// </summary>
    private static void ResetCosmeticsScale(
        PlayerControl player)
    {
        player.cosmetics.SetScale(
            player.MyPhysics.Animations.DefaultPlayerScale,
            player.defaultCosmeticsScale);
    }
}
