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

    private static bool? _lastUseFlashlight;

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

        _lastUseFlashlight =
            null;

        _lastFlashlightSize =
            -1f;
    }

    /// <summary>
    /// Actualiza la linterna del jugador local cuando cambia
    /// su equipo o la configuración de visión de Infection.
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

        var useFlashlight =
            options.UseHnsVision.Value;

        // La forma de la linterna sigue la separación normal
        // de Hide and Seek: superviviente o infectado.
        var flashlightSize =
            InfectionManager.IsInfected(player)
                ? OptionGroupSingleton<HnsImpostorOptions>
                    .Instance
                    .ImpostorFlashlightSize
                    .Value
                : OptionGroupSingleton<HnsCrewmateOptions>
                    .Instance
                    .CrewmateFlashlightSize
                    .Value;

        // HudUpdate se ejecuta continuamente.
        // Solo reaplicamos la iluminación cuando algo relevante cambia.
        if (_lastPlayerId ==
            player.PlayerId &&
            _lastUseFlashlight ==
            useFlashlight &&
            Mathf.Approximately(
                _lastFlashlightSize,
                flashlightSize))
        {
            return;
        }

        _lastPlayerId =
            player.PlayerId;

        _lastUseFlashlight =
            useFlashlight;

        _lastFlashlightSize =
            flashlightSize;

        player.lightSource.useFlashlight =
            useFlashlight;

        player.lightSource.flashlightSize =
            flashlightSize;

        player.AdjustLighting();
    }
}