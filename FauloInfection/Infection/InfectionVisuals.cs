using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using FauloInfection.Buttons;
using FauloInfection.GameModes;
using MiraAPI.Hud;
using PowerTools;
using Reactor.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

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

    /// <summary>
    /// Configura la jerarquía HorseWrangle igual que el intro real de HnS.
    ///
    /// HorseWrangleVisualPlayer ya vive dentro de HorseWrangleVisualSuit,
    /// así que nunca debe clonarse como un segundo objeto independiente.
    /// </summary>
    public static bool ConfigureHorseWrangleVisual(
        PoolablePlayer? suitVisual,
        PoolablePlayer? wranglerVisual,
        PlayerControl? player,
        bool facingLeft)
    {
        if (suitVisual == null ||
            wranglerVisual == null ||
            player == null ||
            player.Data == null ||
            player.MyPhysics == null)
        {
            return false;
        }

        var previousOutfit =
            player.CurrentOutfit;

        var previousOutfitType =
            player.CurrentOutfitType;

        if (previousOutfit == null ||
            !ApplyHorseWranglerOutfit(
                player))
        {
            return false;
        }

        // Configuramos el visual todavía inactivo.
        // Si el Animator comienza antes de que el PoolablePlayer interior
        // tenga los datos correctos, la secuencia puede quedarse en su
        // primer frame o perder los AnimationEvents.
        suitVisual.gameObject.SetActive(
            false);

        suitVisual.SetBodyType(
            PlayerBodyTypes.Seeker);

        wranglerVisual.SetBodyType(
            PlayerBodyTypes.Normal);

        wranglerVisual.UpdateFromPlayerData(
            player.Data,
            player.CurrentOutfitType,
            PlayerMaterial.MaskType.None,
            false,
            null,
            false);

        wranglerVisual.SetFlipX(
            facingLeft);

        wranglerVisual.ToggleName(
            false);

        suitVisual.SetBodyCosmeticsVisible(
            false);

        suitVisual.UpdateFromPlayerData(
            player.Data,
            player.CurrentOutfitType,
            PlayerMaterial.MaskType.None,
            false,
            null,
            false);

        suitVisual.SetFlipX(
            facingLeft);

        suitVisual.ToggleName(
            false);

        // El outfit HorseWrangler solo se toma prestado para poblar
        // los PoolablePlayer de la presentación. El PlayerControl real
        // debe seguir siendo Horse hasta que termine la transformación.
        player.SetOutfit(
            previousOutfit,
            previousOutfitType);

        player.MyPhysics.SetBodyType(
            PlayerBodyTypes.Horse);

        ResetCosmeticsScale(
            player);

        player.cosmetics.SetBodyCosmeticsVisible(
            true);

        player.cosmetics.SetFlipX(
            facingLeft);

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
            // El PlayerControl real permanece Horse mientras preparamos
            // la presentación. En cuanto el visual HorseWrangle está listo,
            // ocultamos únicamente sus cosméticos reales para que el jugador
            // no vea un caballo con sombrero debajo de la animación.
            player.MyPhysics.SetBodyType(
                PlayerBodyTypes.Horse);

            ResetCosmeticsScale(
                player);

            player.MyPhysics.FlipX =
                wasFacingLeft;

            player.cosmetics.SetFlipX(
                wasFacingLeft);

            var originalSuit =
                introPrefab.HorseWrangleVisualSuit;

            var suitVisual =
                Object.Instantiate(
                    originalSuit,
                    player.transform);

            // El prefab del intro está en espacio de UI. Solo adaptamos
            // el root completo al espacio del mapa; los hijos conservan
            // exactamente sus transforms porque el clip usa esas rutas.
            suitVisual.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    -0.5f);

            suitVisual.transform.localRotation =
                Quaternion.identity;

            suitVisual.transform.localScale =
                Vector3.one;

            SetLayerRecursively(
                suitVisual.gameObject,
                player.gameObject.layer);

            var wranglerTransform =
                suitVisual.transform.Find(
                    "PoolablePlayer");

            var wranglerVisual =
                wranglerTransform != null
                    ? wranglerTransform
                        .GetComponent<PoolablePlayer>()
                    : null;

            if (!ConfigureHorseWrangleVisual(
                    suitVisual,
                    wranglerVisual,
                    player,
                    wasFacingLeft))
            {
                Logger<InfectionPlugin>.Warning(
                    $"HorseWrangle visual could not be configured for player {player.PlayerId}. " +
                    "Applying the final Wrangler outfit without the transition.");

                Object.Destroy(
                    suitVisual.gameObject);

                ApplyHorseWranglerOutfit(
                    player);
            }
            else
            {
                var animation =
                    introPrefab.HnSSeekerSpawnHorseAnim;

                var animator =
                    suitVisual.GetComponent<Animator>();

                if (animator == null ||
                    animation == null)
                {
                    Logger<InfectionPlugin>.Warning(
                        $"HorseWrangle Animator or clip was missing for player {player.PlayerId}. " +
                        "Applying the final Wrangler outfit without the transition.");

                    Object.Destroy(
                        suitVisual.gameObject);

                    ApplyHorseWranglerOutfit(
                        player);
                }
                else
                {
                    Logger<InfectionPlugin>.Info(
                        $"HorseWrangle transition started for player {player.PlayerId}. " +
                        $"Clip={animation.name}, Length={animation.length:0.00}s.");

                    // La animación completa pertenece al AnimatorController
                    // HorseWrangleIntro del root HorseWranglePoolablePlayer.
                    //
                    // En los intentos anteriores llamábamos SpriteAnim.Play()
                    // sobre una copia ya activa. Eso esperaba la duración del
                    // clip, pero la presentación visual y sus AnimationEvents
                    // (incluido el yeehaw) no llegaban a ejecutarse.
                    //
                    // Esta vez configuramos el clon inactivo, lo activamos
                    // cuando ya está listo, reiniciamos su AnimatorController
                    // y reproducimos explícitamente su estado real desde t=0.
                    suitVisual.gameObject.SetActive(
                        true);

                    animator.enabled =
                        true;

                    animator.Rebind();

                    animator.Update(
                        0f);

                    animator.Play(
                        "HnSSeekerSpawnHorse",
                        0,
                        0f);

                    animator.Update(
                        0f);

                    // El visual del intro ya está activo encima del jugador.
                    // Ocultamos CosmeticsLayer del PlayerControl real
                    // para evitar que se vea el caballo normal con sombrero.
                    player.cosmetics.gameObject.SetActive(
                        false);

                    yield return new WaitForSeconds(
                        animation.length);

                    player.cosmetics.gameObject.SetActive(
                        true);

                    Object.Destroy(
                        suitVisual.gameObject);

                    ApplyHorseWranglerOutfit(
                        player);

                    Logger<InfectionPlugin>.Info(
                        $"HorseWrangle transition finished for player {player.PlayerId}.");
                }
            }
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

        // Una transformación Horse puede ocultar temporalmente
        // el CosmeticsLayer completo. Siempre lo restauramos antes
        // de devolver el control al jugador.
        if (!player.cosmetics.gameObject.activeSelf)
        {
            player.cosmetics.gameObject.SetActive(
                true);
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
    /// Mueve la jerarquía visual del intro a la misma layer
    /// que utiliza el PlayerControl dentro del mapa.
    /// </summary>
    private static void SetLayerRecursively(
        GameObject gameObject,
        int layer)
    {
        gameObject.layer =
            layer;

        var transform =
            gameObject.transform;

        for (var i = 0;
             i < transform.childCount;
             i++)
        {
            SetLayerRecursively(
                transform.GetChild(i).gameObject,
                layer);
        }
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
