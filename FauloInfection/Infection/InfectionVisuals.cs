using System.Collections;
using System.Collections.Generic;
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
            player.MyPhysics == null ||
            GameManagerCreator.Instance == null ||
            GameManagerCreator.Instance.HideAndSeekManagerPrefab == null)
        {
            return false;
        }

        var currentOutfit =
            player.CurrentOutfit;

        if (currentOutfit == null)
        {
            return false;
        }

        var hideAndSeekManager =
            GameManagerCreator
                .Instance
                .HideAndSeekManagerPrefab;

        // Preparamos una copia lógica del outfit HorseWrangler para los
        // PoolablePlayer del visual, pero NO se lo aplicamos todavía al
        // PlayerControl real.
        //
        // Los fixes anteriores llamaban temporalmente SetOutfit(...HorseWrangler)
        // sobre el jugador real y luego intentaban restaurarlo. Aunque el body
        // volvía a Horse, el sistema de cosméticos podía dejar vivo el sombrero
        // del Wrangler durante la animación, que es el objeto flotante observado.
        var wranglerOutfit =
            hideAndSeekManager
                .horseWranglerOutfit;

        wranglerOutfit.ColorId =
            currentOutfit.ColorId;

        wranglerOutfit.PlayerName =
            currentOutfit.PlayerName;

        // Configuramos el visual todavía inactivo.
        // Si comienza antes de que el PoolablePlayer interior tenga los datos
        // correctos, la secuencia puede quedarse en un frame incorrecto.
        suitVisual.gameObject.SetActive(
            false);

        suitVisual.SetBodyType(
            PlayerBodyTypes.Seeker);

        wranglerVisual.SetBodyType(
            PlayerBodyTypes.Normal);

        // Alimentamos directamente el PoolablePlayer con el outfit especial.
        // No necesitamos cambiar CurrentOutfitType del jugador real para ello.
        wranglerVisual.UpdateFromPlayerOutfit(
            wranglerOutfit,
            PlayerMaterial.MaskType.None,
            false,
            false);

        wranglerVisual.SetFlipX(
            facingLeft);

        wranglerVisual.ToggleName(
            false);

        // HnSSeekerSpawnHorse anima este hijo desde escala 0.
        // UpdateFromPlayerOutfit/SetBodyType pueden dejarlo con la escala
        // normal antes de que SpriteAnim aplique el primer frame.
        wranglerVisual.transform.localPosition =
            new Vector3(
                -0.31f,
                -0.18f,
                0.1f);

        wranglerVisual.transform.localScale =
            new Vector3(
                0f,
                0f,
                1f);

        suitVisual.SetBodyCosmeticsVisible(
            false);

        suitVisual.UpdateFromPlayerOutfit(
            wranglerOutfit,
            PlayerMaterial.MaskType.None,
            false,
            false);

        suitVisual.SetFlipX(
            facingLeft);

        suitVisual.ToggleName(
            false);

        // El visual exterior HorseWrangle no debe mostrar cosméticos normales
        // encima del traje. Solo deben verse las piezas dedicadas de la
        // animación: Horse Parent, SeekerHand, Zipper, BackgroundSuit, etc.
        SetChildActive(
            suitVisual.transform,
            "HatSlot",
            false);

        SetChildActive(
            suitVisual.transform,
            "Skin",
            false);

        SetChildActive(
            suitVisual.transform,
            "Visor",
            false);

        SetChildActive(
            suitVisual.transform,
            "PetSlot",
            false);

        SetChildActive(
            suitVisual.transform,
            "NameText_TMP",
            false);

        SetChildActive(
            suitVisual.transform,
            "ColorBlindText",
            false);

        // El PlayerControl real nunca cambió de outfit aquí. Solo aseguramos
        // que siga usando el body Horse mientras el clon reproduce el clip.
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

            // HorseWranglePoolablePlayer está construido para la cámara
            // del IntroCutscene y resulta ~1.5x demasiado grande en gameplay.
            // 0.67343 es la escala usada por la rama Horse del propio prefab
            // y compensa ese tamaño al colocarlo sobre un PlayerControl.
            const float horseWrangleGameplayScale =
                0.67343f;

            suitVisual.transform.localScale =
                new Vector3(
                    horseWrangleGameplayScale,
                    horseWrangleGameplayScale,
                    1f);

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

                var component =
                    suitVisual.GetComponent<SpriteAnim>();

                if (component == null ||
                    animation == null)
                {
                    Logger<InfectionPlugin>.Warning(
                        $"HorseWrangle SpriteAnim or clip was missing for player {player.PlayerId}. " +
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

                    // Capturamos solo los renderers ACTIVOS del PlayerControl real
                    // antes de mostrar la copia HorseWrangle. El clon ya existe como
                    // hijo, pero sigue inactivo, así que no debe entrar en esta lista.
                    //
                    // El bug anterior usaba GetComponentsInChildren(true) y filtraba
                    // únicamente renderer.enabled. Eso también capturaba los renderers
                    // del clon inactivo y los deshabilitaba antes de reproducir el clip,
                    // por eso toda la transformación quedaba invisible.
                    var originalRenderers =
                        CaptureVisibleRenderers(
                            player.gameObject);

                    SetRenderersEnabled(
                        originalRenderers,
                        false);

                    suitVisual.gameObject.SetActive(
                        true);

                    // Esta es exactamente la API usada por el IntroCutscene
                    // real de Hide and Seek. SpriteAnim sustituye el clip del
                    // Animator compartido y también procesa sus AnimationEvents,
                    // incluido el yeehaw.
                    component.Play(
                        animation,
                        1f);

                    // Fuerza inmediatamente el frame inicial del clip.
                    // En ese frame el PoolablePlayer interior está a escala 0,
                    // por lo que no debe aparecer un tripulante gigante encima
                    // del caballo antes de la revelación.
                    component.SetTime(
                        0f);

                    yield return new WaitForSeconds(
                        animation.length);

                    SetRenderersEnabled(
                        originalRenderers,
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
    /// Activa o desactiva un hijo directo del visual HorseWrangle.
    /// </summary>
    private static void SetChildActive(
        Transform root,
        string childName,
        bool active)
    {
        var child =
            root.Find(
                childName);

        if (child != null)
        {
            child.gameObject.SetActive(
                active);
        }
    }

    /// <summary>
    /// Captura únicamente los Renderer visibles que ya pertenecen
    /// al PlayerControl real. Incluye SpriteRenderer/MeshRenderer/etc.
    ///
    /// Esto es importante porque algunos cosméticos, como ciertos sombreros,
    /// no necesariamente usan el mismo tipo de renderer que el body.
    ///
    /// Debe llamarse antes de activar el visual HorseWrangle para no incluir
    /// los renderers del clon.
    /// </summary>
    private static List<Renderer> CaptureVisibleRenderers(
        GameObject root)
    {
        var result =
            new List<Renderer>();

        var renderers =
            root.GetComponentsInChildren<Renderer>(
                true);

        foreach (var renderer in renderers)
        {
            if (renderer != null &&
                renderer.enabled &&
                renderer.gameObject.activeInHierarchy)
            {
                result.Add(
                    renderer);
            }
        }

        return result;
    }

    /// <summary>
    /// Activa o desactiva un conjunto previamente capturado de renderers.
    /// </summary>
    private static void SetRenderersEnabled(
        IEnumerable<Renderer> renderers,
        bool enabled)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled =
                    enabled;
            }
        }
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
