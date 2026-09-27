using System.Collections.Generic;
using System.Linq;
using FauloInfection.GameModes;
using MiraAPI.GameModes;
using MiraAPI.Modifiers;
using Reactor.Utilities;

namespace FauloInfection.Infection;

/// <summary>
/// Determina qué jugadores pertenecen al equipo infectado
/// y cuáles siguen siendo supervivientes. 
/// This is probably an unoptimized mess, I am still learning
/// </summary>

public static class InfectionManager
{
    /// <summary>
    /// Indica si Infección es actualmente el modo de juego activo.
    /// Infection is the active gamemode
    /// </summary>
    public static bool IsActive =>
        CustomGameModeManager.IsActiveGameMode<InfectionMode>();

    /// <summary>
    /// Determina si un jugador pertenece actualmente al equipo infectado.
    /// My brain is to fried to translate the comments, I'll do it later
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
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        
        // Only the host validates the initial Infection team composition. 
        // Solo el host valida la composición inicial de los equipos de Infection.
        Logger<InfectionPlugin>.Info(
            $"Infection state ready: " +
            $"{GetInfected().Count} infected, " +
            $"{RemainingSurvivors} survivors.");
    }

    private static IEnumerable<PlayerControl> GetLivingPlayers()
    {
        return PlayerControl.AllPlayerControls
            .ToArray()
            .Where(player =>
                player != null &&
                player.Data != null &&
                player.Data.Role != null &&
                !player.Data.Disconnected &&
                !player.Data.IsDead);
    }

    private static IEnumerable<PlayerControl> GetConnectedPlayers()
    {
        return PlayerControl.AllPlayerControls
            .ToArray()
            .Where(player =>
                player != null &&
                player.Data != null &&
                player.Data.Role != null &&
                !player.Data.Disconnected);
    }
}