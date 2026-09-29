using FauloInfection.Options;
using MiraAPI.GameOptions;
using MiraAPI.HnsReimplemented.Options;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Mantiene la presentación local de la visión de Infection
/// sincronizada con las opciones del modo.
/// </summary>
public static class InfectionVisionController
{
    private static byte _lastPlayerId =
        byte.MaxValue;

    private static bool? _lastUseHnsVision;

    private static bool? _lastWasInfected;

    private static bool? _lastWasDead;

    private static float _lastFlashlightSize =
        -1f;

    /// <summary>
    /// Limpia el estado almacenado al comenzar una nueva ronda.
    ///
    /// Aunque el jugador y las opciones coincidan con la ronda anterior,
    /// el LightSource de la nueva ronda debe volver a configurarse.
    /// </summary>
    public static void ResetRound()
    {
        _lastPlayerId =
            byte.MaxValue;

        _lastUseHnsVision =
            null;

        _lastWasInfected =
            null;

        _lastWasDead =
            null;

        _lastFlashlightSize =
            -1f;
    }

    /// <summary>
    /// Actualiza la iluminación del jugador local cuando cambia
    /// su equipo, estado o configuración de visión.
    ///
    /// La configuración real de la linterna se realiza desde
    /// InfectionVisionPatches al interceptar PlayerControl.AdjustLighting.
    /// </summary>
    public static void UpdateLocalLighting()
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        var player =
            PlayerControl.LocalPlayer;

        if (player == null ||
            player.Data == null ||
            player.lightSource == null)
        {
            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        var useHnsVision =
            options.UseHnsVision.Value;

        var isInfected =
            InfectionManager.IsInfected(player);

        var isDead =
            player.Data.IsDead;

        var flashlightSize =
            isInfected
                ? OptionGroupSingleton<HnsImpostorOptions>
                    .Instance
                    .ImpostorFlashlightSize
                    .Value
                : OptionGroupSingleton<HnsCrewmateOptions>
                    .Instance
                    .CrewmateFlashlightSize
                    .Value;

        // HudUpdate se ejecuta continuamente.
        // Solo reconstruimos la iluminación cuando algo relevante cambia.
        if (_lastPlayerId ==
            player.PlayerId &&
            _lastUseHnsVision ==
            useHnsVision &&
            _lastWasInfected ==
            isInfected &&
            _lastWasDead ==
            isDead &&
            Mathf.Approximately(
                _lastFlashlightSize,
                flashlightSize))
        {
            return;
        }

        _lastPlayerId =
            player.PlayerId;

        _lastUseHnsVision =
            useHnsVision;

        _lastWasInfected =
            isInfected;

        _lastWasDead =
            isDead;

        _lastFlashlightSize =
            flashlightSize;

        // No modificamos directamente useFlashlight.
        //
        // AdjustLighting será interceptado por InfectionVisionPatches,
        // donde reproducimos la configuración que utiliza HnS.
        player.AdjustLighting();
    }
}