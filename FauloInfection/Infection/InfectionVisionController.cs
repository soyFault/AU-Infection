using FauloInfection.Options;
using MiraAPI.GameOptions;
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
    /// Devuelve el tamaño de linterna correspondiente
    /// al equipo actual del jugador.
    ///
    /// Los infectados usan su tamaño propio y los supervivientes
    /// utilizan el suyo, igual que HnS separa Impostor y Crewmate.
    /// </summary>
    public static float GetFlashlightSize(
        PlayerControl player)
    {
        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        return InfectionManager.IsInfected(player)
            ? options.InfectedFlashlightSize.Value
            : options.SurvivorFlashlightSize.Value;
    }

    /// <summary>
    /// Comprueba si un objetivo se encuentra dentro
    /// del cono hacia el que apunta la linterna.
    ///
    /// Cuando UseHnsVision está desactivado no aplicamos
    /// ninguna restricción angular.
    /// </summary>
    public static bool IsTargetInsideFlashlight(
        PlayerControl? source,
        PlayerControl? target)
    {
        if (source == null ||
            target == null)
        {
            return false;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        // La visión radial normal no necesita comprobar
        // hacia dónde está apuntando el jugador.
        if (!options.UseHnsVision.Value)
        {
            return true;
        }

        // FlashlightAngle representa la dirección de la
        // linterna controlada por este cliente.
        //
        // No intentamos aplicar esta comprobación visual
        // sobre jugadores remotos desde este cliente.
        if (source != PlayerControl.LocalPlayer ||
            !source.AmOwner)
        {
            return true;
        }

        // Among Us mantiene la orientación visual de la
        // linterna en FlashlightAngle.
        //
        // El ángulo está expresado en radianes, por lo que
        // podemos obtener directamente el vector de dirección
        // utilizado por el cono.
        var flashlightAngle =
            source.FlashlightAngle;

        var aimDirection =
            new Vector2(
                Mathf.Cos(flashlightAngle),
                Mathf.Sin(flashlightAngle));

        var sourcePosition =
            source.GetTruePosition();

        var targetPosition =
            target.GetTruePosition();

        var targetDirection =
            targetPosition -
            sourcePosition;

        // Si ambos jugadores están prácticamente sobre
        // el mismo punto no existe una dirección angular
        // útil que podamos comprobar.
        if (targetDirection.sqrMagnitude <
            0.0001f)
        {
            return true;
        }

        targetDirection.Normalize();

        var flashlightSize =
            Mathf.Clamp(
                GetFlashlightSize(source),
                0.1f,
                0.5f);

        // FlashlightSize funciona como una escala del ancho
        // angular del cono.
        //
        // 0.10 -> aproximadamente 18 grados por lado.
        // 0.35 -> aproximadamente 63 grados por lado.
        // 0.50 -> aproximadamente 90 grados por lado.
        //
        // De esta forma el cursor solamente determina hacia
        // dónde mira la linterna. El objetivo puede encontrarse
        // en cualquier lugar dentro de toda la zona del cono.
        var halfAngle =
            flashlightSize *
            Mathf.PI;

        // Usamos producto escalar para comprobar el cono.
        //
        // No necesitamos calcular Vector2.Angle ni manejar
        // manualmente el salto entre -PI y PI.
        var minimumDot =
            Mathf.Cos(halfAngle);

        var targetDot =
            Vector2.Dot(
                aimDirection,
                targetDirection);

        return targetDot >=
               minimumDot;
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

        var useHnsVision =
            options.UseHnsVision.Value;

        var isInfected =
            InfectionManager.IsInfected(player);

        var isDead =
            player.Data.IsDead;

        // La forma de la linterna sigue la separación normal
        // de Hide and Seek: superviviente o infectado.
        var flashlightSize =
            GetFlashlightSize(player);

        // HudUpdate se ejecuta continuamente.
        // Solo reaplicamos la iluminación cuando algo relevante cambia.
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
        // donde reproducimos la configuración que utiliza HnS mediante
        // SetupLightingForGameplay.
        player.AdjustLighting();
    }
}