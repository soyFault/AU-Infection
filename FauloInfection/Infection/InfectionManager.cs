using FauloInfection.GameModes;
using FauloInfection.Options;
using MiraAPI.GameOptions;
using MiraAPI.GameModes;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Determina qué jugadores pertenecen al equipo infectado
/// y cuáles siguen siendo supervivientes.
/// This is probably an unoptimized mess, I am still learning
/// </summary>
public static class InfectionManager
{
    /// <summary>
    /// Stores when each infected player is allowed to infect again.
    /// Guarda cuándo puede volver a infectar cada jugador infectado.
    /// </summary>
    private static readonly Dictionary<byte, float> NextInfectAt = [];

    /// <summary>
    /// Stores when each infected player is allowed to kill again.
    /// Guarda cuándo puede volver a matar cada jugador infectado.
    /// </summary>
    private static readonly Dictionary<byte, float> NextKillAt = [];

    /// <summary>
    /// Shared infection cooldown used by the button and host validation.
    /// Cooldown compartido usado por el botón y la validación del host.
    /// </summary>
    public const float InfectCooldownSeconds = 5f;

    /// <summary>
    /// Shared kill cooldown used by infected players.
    /// Cooldown compartido de asesinato usado por los infectados.
    /// </summary>
    public const float InfectedKillCooldownSeconds = 1f;

    /// <summary>
    /// Indica si Infección es actualmente el modo de juego activo.
    /// Infection is the active gamemode
    /// </summary>
    public static bool IsActive =>
        CustomGameModeManager.IsActiveGameMode<InfectionMode>();

    /// <summary>
    /// Determina si un jugador pertenece actualmente al equipo infectado.
    /// My brain is too fried to translate the comments, I'll do it later
    /// </summary>
    public static bool IsInfected(PlayerControl? player)
    {
        if (player == null ||
            player.Data == null ||
            player.Data.Role == null)
        {
            return false;
        }

        // El Seeker inicial ya pertenece al equipo infectado
        // debido a su rol base.
        if (player.Data.Role.IsImpostor)
        {
            return true;
        }

        // Los jugadores convertidos conservan su rol base,
        // pero reciben InfectedModifier.
        var modifierComponent =
            player.GetComponent<ModifierComponent>();

        return modifierComponent != null &&
               modifierComponent.HasModifier<InfectedModifier>(true);
    }

    /// <summary>
    /// Devuelve los infectados vivos y conectados.
    /// </summary>
    public static IReadOnlyList<PlayerControl> GetInfected()
    {
        return GetLivingPlayers()
            .Where(IsInfected)
            .ToArray();
    }

    /// <summary>
    /// Devuelve los supervivientes vivos y conectados.
    /// </summary>
    public static IReadOnlyList<PlayerControl> GetSurvivors()
    {
        return GetLivingPlayers()
            .Where(player => !IsInfected(player))
            .ToArray();
    }

    /// <summary>
    /// Devuelve todos los infectados conectados,
    /// incluso si están muertos.
    /// </summary>
    public static IReadOnlyList<PlayerControl> GetConnectedInfected()
    {
        return GetConnectedPlayers()
            .Where(IsInfected)
            .ToArray();
    }

    /// <summary>
    /// Devuelve todos los supervivientes conectados,
    /// incluso si están muertos.
    /// </summary>
    public static IReadOnlyList<PlayerControl> GetConnectedSurvivors()
    {
        return GetConnectedPlayers()
            .Where(player => !IsInfected(player))
            .ToArray();
    }

    /// <summary>
    /// Número de supervivientes vivos que quedan.
    /// </summary>
    public static int RemainingSurvivors =>
        GetSurvivors().Count;

    /// <summary>
    /// Indica si ya no queda ningún superviviente vivo.
    /// </summary>
    public static bool EveryoneInfected =>
        RemainingSurvivors == 0;

    /// <summary>
    /// Inicializa el estado de Infection después
    /// de que Among Us haya asignado los roles.
    /// </summary>
    public static void InitializeRound()
    {
        // Clear cooldown data left over from a previous round.
        // Limpia los cooldowns que hayan quedado de una ronda anterior.
        NextInfectAt.Clear();
        NextKillAt.Clear();

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        // Give every initially infected player the normal infection cooldown
        // before they are allowed to infect someone.
        // Da a cada infectado inicial el cooldown normal antes de permitirle
        // infectar a otro jugador.
        var initialInfectReadyAt =
            Time.time + InfectCooldownSeconds;

        var initialKillReadyAt =
            Time.time + InfectedKillCooldownSeconds;

        foreach (var infected in GetInfected())
        {
            NextInfectAt[infected.PlayerId] =
                initialInfectReadyAt;

            NextKillAt[infected.PlayerId] =
                initialKillReadyAt;
        }

        // Only the host validates the initial Infection team composition.
        // Solo el host valida la composición inicial de los equipos de Infection.
        Logger<InfectionPlugin>.Info(
            $"Infection state ready: " +
            $"{GetInfected().Count} infected, " +
            $"{RemainingSurvivors} survivors.");
    }

    /// <summary>
    /// Validates an infection attempt on the host before changing team state.
    /// Valida un intento de infección en el host antes de modificar el estado de los equipos.
    /// </summary>
    /// <returns>
    /// True if the host accepted and applied the infection.
    /// </returns>
    public static bool TryInfect(
        PlayerControl? source,
        PlayerControl? target)
    {
        // The host is the only authority allowed to approve infections.
        // El host es la única autoridad que puede aprobar infecciones.
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            !IsActive ||
            !IsValidLivingPlayer(source) ||
            !IsValidLivingPlayer(target) ||
            source == target ||
            !IsInfected(source) ||
            IsInfected(target) ||
            source!.inVent ||
            target!.inVent)
        {
            return false;
        }

        // Reject the request if this infected player is still on cooldown.
        // Rechaza la solicitud si este jugador infectado todavía está en cooldown.
        if (NextInfectAt.TryGetValue(
                source.PlayerId,
                out var readyAt) &&
            Time.time < readyAt)
        {
            return false;
        }

        var distance =
            source.Data.Role.GetAbilityDistance();

        // Recalculate the target on the host to verify range and line of sight.
        // Recalcula el objetivo en el host para verificar alcance y línea de visión.
        var validatedTarget =
            source.GetClosestPlayer(
                includeImpostors: true,
                distance: distance,
                ignoreColliders: false,
                includeGhosts: false,
                predicate: player =>
                    player.PlayerId == target.PlayerId &&
                    !IsInfected(player));

        if (validatedTarget == null ||
            validatedTarget.PlayerId != target.PlayerId)
        {
            return false;
        }

        if (!Infect(target))
        {
            return false;
        }

        // Both the source and newly infected player receive a fresh cooldown.
        // Tanto el infectado original como el recién infectado reciben un nuevo cooldown.
        var nextReadyAt =
            Time.time + InfectCooldownSeconds;

        NextInfectAt[source.PlayerId] =
            nextReadyAt;

        NextInfectAt[target.PlayerId] =
            nextReadyAt;

        Logger<InfectionPlugin>.Info(
            $"Infection spread: " +
            $"{source.PlayerId} -> {target.PlayerId}. " +
            $"{GetInfected().Count} infected, " +
            $"{RemainingSurvivors} survivors.");

        return true;
    }

    /// <summary>
    /// Applies the synchronized infection marker to a player.
    /// Aplica el marcador sincronizado de infección a un jugador.
    /// </summary>
    public static bool Infect(PlayerControl? target)
    {
        if (!IsActive ||
            !IsValidLivingPlayer(target) ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            IsInfected(target))
        {
            return false;
        }

        // MiraAPI synchronizes this modifier to every client.
        // MiraAPI sincroniza este modificador con todos los clientes.
        target!.RpcAddModifier<InfectedModifier>();

        // EN: A newly infected player must wait before their first kill.
        // ES: Un jugador recién infectado debe esperar antes de su primer asesinato.
        NextKillAt[target!.PlayerId] =
            Time.time + InfectedKillCooldownSeconds;

        return true;
    }

    /// <summary>
    /// EN: Determines whether an infected player is allowed to kill this target.
    /// ES: Determina si un jugador infectado puede matar a este objetivo.
    /// </summary>
    public static bool CanKillTarget(
        PlayerControl? source,
        PlayerControl? target)
    {
        if (!IsActive ||
            !IsValidLivingPlayer(source) ||
            !IsValidLivingPlayer(target) ||
            source == target ||
            !IsInfected(source) ||
            source!.inVent ||
            target!.inVent)
        {
            return false;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>.Instance;

        // EN: Killing can be disabled completely for infected players.
        // ES: Los asesinatos pueden desactivarse completamente para los infectados.
        if (!options.AllowInfectedKills.Value)
        {
            return false;
        }

        // EN: Without Friendly Fire, infected players cannot kill teammates.
        // ES: Sin Fuego Amigo, los infectados no pueden matar a sus compañeros.
        if (!options.FriendlyFire.Value &&
            IsInfected(target))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// EN: Host-authoritative kill attempt used by converted infected players.
    /// ES: Intento de asesinato autoritativo del host usado por infectados convertidos.
    /// </summary>
    public static bool TryKill(
        PlayerControl? source,
        PlayerControl? target)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            !CanKillTarget(source, target))
        {
            return false;
        }

        if (NextKillAt.TryGetValue(
                source!.PlayerId,
                out var readyAt) &&
            Time.time < readyAt)
        {
            return false;
        }

        var distance =
            source!.Data!.Role!.GetAbilityDistance();
        
        // Recalculate the target on the host before accepting the kill.
        // Recalcula el objetivo en el host antes de aceptar el asesinato.
        var validatedTarget =
            source.GetClosestPlayer(
                includeImpostors: true,
                distance: distance,
                ignoreColliders: false,
                includeGhosts: false,
                predicate: player =>
                    player.PlayerId == target!.PlayerId &&
                    CanKillTarget(source, player));

        if (validatedTarget == null ||
            validatedTarget.PlayerId != target!.PlayerId)
        {
            return false;
        }

        NextKillAt[source.PlayerId] =
            Time.time + InfectedKillCooldownSeconds;

        // EN: Use the normal Among Us murder RPC so death animations,
        // bodies and the Hide & Seek death notification remain intact.
        // ES: Usa el RPC normal de asesinato de Among Us para conservar
        // animaciones, cadáveres y la notificación de muerte de Hide & Seek.
        source.RpcMurderPlayer(target, true);

        Logger<InfectionPlugin>.Info(
            $"Infection kill: " +
            $"{source.PlayerId} -> {target.PlayerId}.");

        return true;
    }

    /// <summary>
    /// Removes converted-player infection markers and clears temporary cooldown state.
    /// Elimina los marcadores de jugadores convertidos y limpia el estado temporal de cooldowns.
    /// </summary>
    public static int Reset()
    {
        NextInfectAt.Clear();
        NextKillAt.Clear();

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return 0;
        }

        var removed = 0;

        foreach (var player in
                 PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null)
            {
                continue;
            }

            var modifierComponent =
                player.GetComponent<ModifierComponent>();

            if (modifierComponent == null ||
                !modifierComponent.HasModifier<InfectedModifier>())
            {
                continue;
            }

            player.RpcRemoveModifier<InfectedModifier>();
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// Checks whether a player exists, is connected, has a role and is alive.
    /// Comprueba que un jugador exista, esté conectado, tenga rol y siga vivo.
    /// </summary>
    private static bool IsValidLivingPlayer(
        PlayerControl? player)
    {
        return IsValidConnectedPlayer(player) &&
               !player!.Data.IsDead;
    }

    /// <summary>
    /// Checks whether a player exists, has valid game data and is connected.
    /// Comprueba que un jugador exista, tenga datos válidos y esté conectado.
    /// </summary>
    private static bool IsValidConnectedPlayer(
        PlayerControl? player)
    {
        return player != null &&
               player.Data != null &&
               player.Data.Role != null &&
               !player.Data.Disconnected;
    }

    private static IEnumerable<PlayerControl> GetLivingPlayers()
    {
        return PlayerControl.AllPlayerControls
            .ToArray()
            .Where(IsValidLivingPlayer)
            .Select(player => player!);
    }

    private static IEnumerable<PlayerControl> GetConnectedPlayers()
    {
        return PlayerControl.AllPlayerControls
            .ToArray()
            .Where(IsValidConnectedPlayer)
            .Select(player => player!);
    }
}