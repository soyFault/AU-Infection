using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using FauloInfection.Buttons;
using FauloInfection.GameModes;
using MiraAPI.Hud;
using PowerTools;
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

    public static void ApplyInfected(
        PlayerControl? player,
        bool blockMovement = true,
        bool waitForInitialHideWindow = false)
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
                blockMovement,
                waitForInitialHideWindow));
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
        bool blockMovement,
        bool waitForInitialHideWindow)
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
            // HnSSeekerSpawnHorseInGameAnim no reproduce de forma fiable
            // la transformación visual dentro del modo custom de MiraAPI.
            //
            // Para esta ruta usamos exactamente la presentación del intro:
            //
            // HorseWrangleVisualSuit
            //     +
            // HorseWrangleVisualPlayer
            //     +
            // HnSSeekerSpawnHorseAnim
            //
            // El jugador real queda oculto debajo y solo vuelve a mostrarse
            // cuando termina la animación.
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

            player.cosmetics.SetBodyCosmeticsVisible(
                false);

            var originalSuit =
                introPrefab.HorseWrangleVisualSuit;

            var originalWrangler =
                introPrefab.HorseWrangleVisualPlayer;

            var suitVisual =
                Object.Instantiate(
                    originalSuit,
                    player.transform);

            var wranglerVisual =
                Object.Instantiate(
                    originalWrangler,
                    player.transform);

            // Los visuales del IntroCutscene están creados para una cámara
            // distinta. Al convertirlos en hijos del PlayerControl usamos
            // coordenadas locales de gameplay y la misma layer del jugador.
            suitVisual.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    -0.25f);

            suitVisual.transform.localRotation =
                Quaternion.identity;

            suitVisual.transform.localScale =
                Vector3.one;

            wranglerVisual.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    -0.24f);

            wranglerVisual.transform.localRotation =
                Quaternion.identity;

            wranglerVisual.transform.localScale =
                Vector3.one;

            SetLayerRecursively(
                suitVisual.gameObject,
                player.gameObject.layer);

            SetLayerRecursively(
                wranglerVisual.gameObject,
                player.gameObject.layer);

            suitVisual.gameObject.SetActive(
                true);

            wranglerVisual.gameObject.SetActive(
                true);

            suitVisual.SetBodyType(
                PlayerBodyTypes.Seeker);

            wranglerVisual.SetBodyType(
                PlayerBodyTypes.Normal);

            // El Wrangler interior utiliza exactamente el mismo outfit
            // final que acabamos de preparar en el PlayerControl real.
            wranglerVisual.UpdateFromPlayerData(
                player.Data,
                player.CurrentOutfitType,
                PlayerMaterial.MaskType.None,
                false,
                null,
                false);

            wranglerVisual.SetFlipX(
                wasFacingLeft);

            wranglerVisual.ToggleName(
                false);

            // Igual que el intro original: el traje usa los datos del
            // jugador, pero oculta sus cosméticos mientras se reproduce
            // HnSSeekerSpawnHorseAnim.
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
                wasFacingLeft);

            suitVisual.ToggleName(
                false);

            var component =
                suitVisual.GetComponent<SpriteAnim>();

            var animation =
                introPrefab.HnSSeekerSpawnHorseAnim;

            component.Play(
                animation,
                1f);

            // La parte visible de "quitarse el traje" comienza alrededor
            // del segundo 5 del mismo clip que utiliza el tutorial.
            component.SetTime(
                5f);

            if (waitForInitialHideWindow)
            {
                // El Seeker inicial debe coincidir con la cuenta de HnS:
                // mantiene el traje durante los primeros cinco segundos
                // y ejecuta la transformación durante los últimos cinco.
                component.Pause();

                yield return new WaitForSeconds(
                    5f);

                component.Resume();
            }

            var remainingAnimationTime =
                Mathf.Max(
                    0.1f,
                    animation.length - 5f);

            yield return new WaitForSeconds(
                remainingAnimationTime);

            Object.Destroy(
                suitVisual.gameObject);

            Object.Destroy(
                wranglerVisual.gameObject);

            ResetCosmeticsScale(
                player);

            player.cosmetics.SetBodyCosmeticsVisible(
                true);
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
    /// Hace que los visuales copiados desde IntroCutscene utilicen
    /// la misma layer de render que el PlayerControl del mapa.
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
