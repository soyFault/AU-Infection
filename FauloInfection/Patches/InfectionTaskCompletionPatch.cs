using FauloInfection.Infection;
using FauloInfection.Networking;
using HarmonyLib;
using Reactor.Networking.Rpc;
using Reactor.Utilities;

namespace FauloInfection.Patches;

/// <summary>
/// Detecta cuando el jugador local completa una tarea
/// y sincroniza su efecto sobre el temporizador de Infection.
/// </summary>
[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.RpcCompleteTask))]
internal static class InfectionTaskCompletionPatch
{
    [HarmonyPrefix]
    private static void RpcCompleteTaskPrefix(
        PlayerControl __instance,
        uint idx)
    {
        if (!InfectionManager.IsActive ||
            __instance == null ||
            !__instance.AmOwner ||
            __instance.Data == null ||
            __instance.Data.Tasks == null ||
            InfectionManager.IsInfected(__instance) ||
            ShipStatus.Instance == null)
        {
            return;
        }

        // No utilizamos myTasks como fuente de verdad.
        // La reconstrucción histórica encontró diferencias
        // entre esa lista local y Data.Tasks sincronizado.
        NetworkedPlayerInfo.TaskInfo? taskInfo =
            null;

        foreach (var candidate in
                 __instance.Data.Tasks)
        {
            if (candidate != null &&
                candidate.Id == idx)
            {
                taskInfo = candidate;
                break;
            }
        }

        if (taskInfo == null)
        {
            var knownIds =
                string.Join(
                    ",",
                    __instance.Data.Tasks
                        .ToArray()
                        .Where(candidate =>
                            candidate != null)
                        .Select(candidate =>
                            candidate.Id));

            Logger<InfectionPlugin>.Warning(
                $"No se pudo resolver la tarea sincronizada " +
                $"{idx} del jugador " +
                $"{__instance.PlayerId}. " +
                $"IDs conocidos: [{knownIds}].");

            return;
        }

        // Data.Tasks guarda el TypeId sincronizado.
        // Con él recuperamos la plantilla real del mapa,
        // que contiene la longitud de la tarea.
        var taskTemplate =
            ShipStatus.Instance.GetTaskById(
                taskInfo.TypeId);

        if (taskTemplate == null)
        {
            Logger<InfectionPlugin>.Warning(
                $"No se pudo resolver la plantilla " +
                $"de tarea tipo {taskInfo.TypeId} " +
                $"del jugador {__instance.PlayerId}, " +
                $"tarea {idx}.");

            return;
        }

        Logger<InfectionPlugin>.Info(
            $"Tarea local detectada: " +
            $"jugador {__instance.PlayerId}, " +
            $"tarea {idx}, " +
            $"tipo {taskInfo.TypeId}, " +
            $"longitud {taskTemplate.Length}.");

        Rpc<InfectionTaskCompleteRpc>
            .Instance
            .Send(
                __instance,
                new InfectionTaskCompleteData(
                    idx,
                    (byte)taskTemplate.Length));
    }
}