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
        // El intro del Seeker administra su propio hide time.
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
            // Ahora ShouldHorseAround() está realmente activo durante
            // Infection, así que primero dejamos que Hide and Seek
            // prepare sus cosméticos especiales de Horse Wrangler.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSpecialCosmetics(
                    player);
            }

            // Fallback por seguridad.
            //
            // Si el GameManager activo no llegó a aplicar el outfit
            // especial, lo hacemos manualmente.
            if (player.CurrentOutfitType !=
                PlayerOutfitType.HorseWrangler)
            {
                ApplyHorseWranglerOutfit(
                    player);
            }

            // Horse Wrangler utiliza cuerpo Normal.
            player.MyPhysics.SetBodyType(
                PlayerBodyTypes.Normal);

            // Cambiar de Horse a Normal puede dejar la escala
            // del caballo aplicada a CosmeticsLayer.
            ResetCosmeticsScale(
                player);

            player.MyPhysics.FlipX =
                wasFacingLeft;

            player.cosmetics.SetFlipX(
                wasFacingLeft);

            var animation =
                introPrefab
                    .HnSSeekerSpawnHorseInGameAnim;

            // Esta transformación concreta de Horse Mode utiliza
            // PlayerControl.AnimateCustom en el HnS real.
            //
            // CoPlayCustomAnimation reproducía correctamente el audio
            // y la duración, pero no la presentación visual completa
            // del Horse Wrangler.
            player.AnimateCustom(
                animation);

            // AnimateCustom inicia su propia coroutine.
            // Nosotros esperamos únicamente para devolver movimiento
            // y refrescar el HUD cuando la transformación haya terminado.
            yield return new WaitForSeconds(
                animation.length);

            // Nos aseguramos de no conservar escala de Horse
            // después de terminar la transformación.
            ResetCosmeticsScale(
                player);

            player.cosmetics.SetBodyCosmeticsVisible(
                true);

            player.MyPhysics.FlipX =
                wasFacingLeft;

            player.cosmetics.SetFlipX(
                wasFacingLeft);
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