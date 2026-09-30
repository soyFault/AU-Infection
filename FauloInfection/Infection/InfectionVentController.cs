using AmongUs.GameOptions;
using FauloInfection.Options;
using MiraAPI.GameOptions;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Controla localmente los límites de uso de ductos de Infection.
///
/// El permiso depende del equipo real de Infection, no del rol vanilla:
/// supervivientes e infectados tienen opciones independientes.
/// </summary>
public static class InfectionVentController
{
    private static bool _roundActive;
    private static bool _initialized;
    private static byte _trackedPlayerId = byte.MaxValue;

    private static int _usesLeft;
    private static bool _wasInVent;
    private static bool _exitQueued;

    private static float _enteredAt = -1f;
    private static float _nextVentAt;

    /// <summary>
    /// Devuelve si el equipo actual del jugador tiene permiso para usar ductos.
    /// No aplica todavía usos ni cooldown.
    /// </summary>
    public static bool HasTeamPermission(
        PlayerControl? player)
    {
        if (!InfectionManager.IsActive ||
            player == null ||
            player.Data == null ||
            player.Data.IsDead ||
            player.Data.Disconnected)
        {
            return false;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        return InfectionManager.IsInfected(player)
            ? options.InfectedCanVent.Value
            : options.CrewmatesCanVent.Value;
    }

    /// <summary>
    /// Decide si un jugador puede iniciar un nuevo uso de ducto.
    ///
    /// Si el jugador ya está dentro devolvemos true para no interferir
    /// con la salida o el movimiento entre ductos conectados.
    /// </summary>
    public static bool CanUseVent(
        PlayerControl? player)
    {
        if (!HasTeamPermission(player))
        {
            return false;
        }

        if (player!.inVent)
        {
            return true;
        }

        // Los límites son estado local: cada cliente controla los usos
        // y cooldown de su propio jugador.
        if (!player.AmOwner)
        {
            return true;
        }

        EnsureRoundInitialized(
            player);

        return _usesLeft > 0 &&
               Time.time >= _nextVentAt;
    }

    /// <summary>
    /// Se ejecuta desde HudManager.Update y mantiene usos, cooldown
    /// y expulsión por tiempo máximo sincronizados con el jugador local.
    /// </summary>
    public static void Update(
        HudManager hud)
    {
        var gameStarted =
            InfectionManager.IsActive &&
            GameManager.Instance != null &&
            GameManager.Instance.GameHasStarted;

        if (!gameStarted)
        {
            _roundActive = false;
            _initialized = false;
            return;
        }

        var player =
            PlayerControl.LocalPlayer;

        if (player == null ||
            player.Data == null)
        {
            return;
        }

        if (!_roundActive)
        {
            _roundActive = true;
            ResetRound(
                player);
        }
        else
        {
            EnsureRoundInitialized(
                player);
        }

        var isInVent =
            player.inVent;

        // Una entrada real consume un uso.
        // Cambiar entre ductos mientras ya estás dentro no consume otro.
        if (isInVent &&
            !_wasInVent)
        {
            _usesLeft =
                Mathf.Max(
                    0,
                    _usesLeft - 1);

            _enteredAt =
                Time.time;

            _exitQueued = false;
        }

        // El cooldown empieza al salir, tanto si fue una salida manual
        // como si VentDuration expulsó al jugador.
        if (!isInVent &&
            _wasInVent)
        {
            _enteredAt = -1f;
            _exitQueued = false;

            _nextVentAt =
                Time.time +
                GetCooldown();
        }

        _wasInVent =
            isInVent;

        if (isInVent &&
            _enteredAt >= 0f)
        {
            var duration =
                GetDuration();

            var elapsed =
                Time.time -
                _enteredAt;

            if (elapsed >= duration &&
                !_exitQueued)
            {
                ForceExit(
                    player);
            }
        }

        UpdateHud(
            hud,
            player);
    }

    /// <summary>
    /// Reinicia el estado local al empezar una ronda nueva.
    /// </summary>
    private static void ResetRound(
        PlayerControl player)
    {
        _trackedPlayerId =
            player.PlayerId;

        _usesLeft =
            GetMaxUses();

        _wasInVent =
            player.inVent;

        _exitQueued = false;
        _enteredAt = -1f;
        _nextVentAt = 0f;
        _initialized = true;
    }

    private static void EnsureRoundInitialized(
        PlayerControl player)
    {
        if (_initialized &&
            _trackedPlayerId ==
            player.PlayerId)
        {
            return;
        }

        ResetRound(
            player);
    }

    private static int GetMaxUses()
    {
        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .VentUses
                    .Value));
    }

    private static float GetDuration()
    {
        return Mathf.Max(
            0.1f,
            OptionGroupSingleton<InfectionOptions>
                .Instance
                .VentDuration
                .Value);
    }

    private static float GetCooldown()
    {
        return Mathf.Max(
            0f,
            OptionGroupSingleton<InfectionOptions>
                .Instance
                .VentCooldown
                .Value);
    }

    /// <summary>
    /// Expulsa al jugador local cuando supera VentDuration.
    /// </summary>
    private static void ForceExit(
        PlayerControl player)
    {
        var vent =
            Vent.currentVent;

        if (vent == null ||
            player.MyPhysics == null)
        {
            return;
        }

        _exitQueued = true;

        vent.SetButtons(
            false);

        player.MyPhysics.RpcExitVent(
            vent.Id);
    }

    /// <summary>
    /// Mantiene visibles los usos/cooldown de la habilidad Engineer
    /// y el cooldown del botón de vent del Seeker inicial.
    ///
    /// El permiso real sigue perteneciendo a CanUseVent.
    /// </summary>
    private static void UpdateHud(
        HudManager hud,
        PlayerControl player)
    {
        if (player.Data?.Role == null)
        {
            return;
        }

        var hasTeamPermission =
            HasTeamPermission(
                player);

        // Un jugador que ya agotó todos sus usos tampoco necesita
        // conservar visible un botón que ya no podrá volver a usar.
        //
        // Durante el cooldown sí mantenemos visible el botón para que
        // el jugador entienda que la habilidad sigue existiendo.
        var shouldShowVentButton =
            hasTeamPermission &&
            (_usesLeft > 0 ||
             player.inVent);

        var cooldown =
            GetCooldown();

        var cooldownRemaining =
            Mathf.Max(
                0f,
                _nextVentAt -
                Time.time);

        if (player.Data.Role.Role ==
            RoleTypes.Engineer &&
            hud.AbilityButton)
        {
            if (hud.AbilityButton.gameObject.activeSelf !=
                shouldShowVentButton)
            {
                hud.AbilityButton.gameObject.SetActive(
                    shouldShowVentButton);
            }

            if (!shouldShowVentButton)
            {
                return;
            }

            hud.AbilityButton.SetUsesRemaining(
                _usesLeft);

            if (player.inVent &&
                _enteredAt >= 0f)
            {
                var duration =
                    GetDuration();

                var remaining =
                    Mathf.Max(
                        0f,
                        duration -
                        (Time.time - _enteredAt));

                hud.AbilityButton.SetFillUp(
                    remaining,
                    duration);
            }
            else if (cooldown > 0f &&
                     hud.AbilityButton.cooldownTimerText)
            {
                // Engineer sí posee un cooldownTimerText válido en el HUD
                // normal de HnS. Aun así comprobamos la referencia antes
                // de llamar SetCoolDown para no repetir el NRE observado
                // durante la inicialización del HUD.
                hud.AbilityButton.SetCoolDown(
                    cooldownRemaining,
                    cooldown);
            }

            return;
        }

        // El Seeker inicial usa ImpostorVentButton.
        //
        // En el HUD HnS ese ActionButton puede existir sin todos los
        // subcomponentes que ActionButton.SetCoolDown espera. El log de
        // prueba mostró un NullReferenceException exactamente dentro de
        // SetCoolDown cuando lo llamábamos desde aquí, así que para este
        // botón solo controlamos visibilidad. El cooldown real continúa
        // siendo aplicado por CanUseVent.
        if (hud.ImpostorVentButton)
        {
            if (hud.ImpostorVentButton.gameObject.activeSelf !=
                shouldShowVentButton)
            {
                hud.ImpostorVentButton.gameObject.SetActive(
                    shouldShowVentButton);
            }
        }
    }
}
