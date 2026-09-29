using FauloInfection.Options;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using UnityEngine;

namespace FauloInfection.Infection;

/// <summary>
/// Asigna de forma autoritativa las tareas iniciales de Infection.
/// Solo el host decide qué tareas recibe cada jugador.
/// </summary>
public static class InfectionTaskManager
{
    public static void AssignInitialTasks()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            ShipStatus.Instance == null)
        {
            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>.Instance;

        var ship =
            ShipStatus.Instance;

        // Preparamos los índices reales de las tareas
        // disponibles en el mapa actual.
        ship.numScans = 0;
        ship.AssignTaskIndexes();

        var commonPool =
            ship.CommonTasks
                .ToArray()
                .Where(task => task != null)
                .ToList();

        var shortPool =
            ship.ShortTasks
                .ToArray()
                .Where(task => task != null)
                .ToList();

        var longPool =
            ship.LongTasks
                .ToArray()
                .Where(task => task != null)
                .ToList();

        var commonCount =
            options.EnableTasks.Value
                ? Mathf.RoundToInt(
                    options.CommonTasks.Value)
                : 0;

        var shortCount =
            options.EnableTasks.Value
                ? Mathf.RoundToInt(
                    options.ShortTasks.Value)
                : 0;

        var longCount =
            options.EnableTasks.Value
                ? Mathf.RoundToInt(
                    options.LongTasks.Value)
                : 0;

        // Las tareas comunes son compartidas por todos
        // los supervivientes, como en Among Us normal.
        var sharedCommon =
            PickTasks(
                commonPool,
                commonCount,
                []);

        foreach (var player in
                 PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null ||
                player.Data == null ||
                player.Data.Disconnected)
            {
                continue;
            }

            // Los infectados nunca reciben tareas.
            // Si las tareas están desactivadas,
            // ningún jugador recibe una lista.
            if (!options.EnableTasks.Value ||
                InfectionManager.IsInfected(player))
            {
                player.Data.RpcSetTasks(
                    Array.Empty<byte>());

                continue;
            }

            var usedTypes =
                new HashSet<TaskTypes>();

            var taskIds =
                new List<byte>();

            foreach (var task in sharedCommon)
            {
                if (usedTypes.Add(task.TaskType))
                {
                    taskIds.Add(
                        (byte)task.Index);
                }
            }

            foreach (var task in
                     PickTasks(
                         longPool,
                         longCount,
                         usedTypes))
            {
                taskIds.Add(
                    (byte)task.Index);
            }

            foreach (var task in
                     PickTasks(
                         shortPool,
                         shortCount,
                         usedTypes))
            {
                taskIds.Add(
                    (byte)task.Index);
            }

            player.Data.RpcSetTasks(
                taskIds.ToArray());

            Logger<InfectionPlugin>.Info(
                $"Asignadas {taskIds.Count} tareas de Infection " +
                $"al jugador {player.PlayerId} " +
                $"({commonCount}/{shortCount}/{longCount}).");
        }
    }

    /// <summary>
    /// Escoge tareas aleatorias sin repetir tipos
    /// dentro de la lista del mismo jugador.
    /// </summary>
    private static List<NormalPlayerTask> PickTasks(
        IEnumerable<NormalPlayerTask> source,
        int count,
        HashSet<TaskTypes> usedTypes)
    {
        var available =
            source
                .Where(task => task != null)
                .ToList();

        var picked =
            new List<NormalPlayerTask>();

        while (picked.Count < count &&
               available.Count > 0)
        {
            var index =
                HashRandom.FastNext(
                    available.Count);

            var task =
                available[index];

            available.RemoveAt(index);

            if (!usedTypes.Add(task.TaskType))
            {
                continue;
            }

            picked.Add(task);
        }

        return picked;
    }
}