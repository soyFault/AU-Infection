using FauloInfection.Options;
using MiraAPI.GameOptions;
using MiraAPI.HnsReimplemented;
using Reactor.Utilities;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Gestiona los efectos que produce completar tareas
/// sobre el temporizador de Infection.
/// </summary>
public static class InfectionTaskEvents
{
    /// <summary>
    /// Guarda cada tarea ya procesada durante esta ronda.
    ///
    /// PlayerId forma parte de la clave porque dos jugadores
    /// pueden tener tareas con el mismo ID.
    /// </summary>
    private static readonly HashSet<(byte PlayerId, uint TaskId)>
        CompletedTasks = [];

    /// <summary>
    /// Limpia el historial de tareas procesadas
    /// al comenzar una nueva ronda.
    /// </summary>
    public static void ResetRound()
    {
        CompletedTasks.Clear();
    }

    /// <summary>
    /// Aplica una única deducción de tiempo por tarea completada.
    /// </summary>
    public static void HandleCompletedTask(
        PlayerControl player,
        uint taskId,
        NormalPlayerTask.TaskLength taskLength)
    {
        if (!InfectionManager.IsActive ||
            player == null ||
            player.Data == null ||
            player.Data.Disconnected ||
            !OptionGroupSingleton<InfectionOptions>
                .Instance
                .EnableTasks
                .Value ||
            InfectionManager.IsInfected(player) ||
            HideAndSeekHudHelper.Instance == null ||
            HideAndSeekHudHelper.Instance.IsFinalCountdown)
        {
            return;
        }

        // Una tarea solo puede modificar el temporizador
        // una vez durante la ronda.
        if (!CompletedTasks.Add(
                (player.PlayerId, taskId)))
        {
            return;
        }

        var playerCount =
            GameData.Instance?.PlayerCount ?? 0;

        var deduction =
            taskLength switch
            {
                NormalPlayerTask.TaskLength.Long =>
                    Mathf.Max(
                        0f,
                        20f - playerCount),

                NormalPlayerTask.TaskLength.None or
                NormalPlayerTask.TaskLength.Common or
                NormalPlayerTask.TaskLength.Short =>
                    Mathf.Max(
                        0f,
                        10f - (playerCount / 2f)),

                _ => 0f,
            };

        if (deduction <= 0f)
        {
            return;
        }

        var before =
            HideAndSeekHudHelper
                .Instance
                .GetTotalTimeRemaining();

        HideAndSeekHudHelper
            .Instance
            .OnTaskComplete(deduction);

        var after =
            HideAndSeekHudHelper
                .Instance
                .GetTotalTimeRemaining();

        Logger<InfectionPlugin>.Info(
            $"Tarea de Infection completada: " +
            $"jugador {player.PlayerId}, " +
            $"tarea {taskId}, " +
            $"tipo {taskLength}, " +
            $"-{deduction:0.0}s " +
            $"({before:0.0}s -> {after:0.0}s).");
    }
}