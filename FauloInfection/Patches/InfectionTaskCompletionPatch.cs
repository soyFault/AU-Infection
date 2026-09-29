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
        
        // Data.Tasks nos proporciona el TypeId sincronizado,
        // pero la propiedad Length de la plantilla puede ser None.
        // Por eso determinamos la categoría comprobando en qué
        // colección de tareas del mapa se encuentra ese TypeId.
        var taskLength =
            ResolveTaskLength(
                ShipStatus.Instance,
                taskInfo.TypeId);

        if (taskLength ==
            NormalPlayerTask.TaskLength.None)
        {
            Logger<InfectionPlugin>.Warning(
                $"No se pudo determinar la categoría " +
                $"de la tarea tipo {taskInfo.TypeId} " +
                $"del jugador {__instance.PlayerId}, " +
                $"tarea {idx}.");

            return;
        }

        Logger<InfectionPlugin>.Info(
            $"Tarea local detectada: " +
            $"jugador {__instance.PlayerId}, " +
            $"tarea {idx}, " +
            $"tipo {taskInfo.TypeId}, " +
            $"categoría {taskLength}.");

        Rpc<InfectionTaskCompleteRpc>
            .Instance
            .Send(
                __instance,
                new InfectionTaskCompleteData(
                    idx,
                    (byte)taskLength));
    }
    
    /// <summary>
    /// Determina si un TypeId pertenece al grupo
    /// Common, Short o Long del mapa actual.
    /// </summary>
    private static NormalPlayerTask.TaskLength
        ResolveTaskLength(
            ShipStatus ship,
            byte typeId)
    {
        if (ContainsTaskType(
                ship.CommonTasks,
                typeId))
        {
            return NormalPlayerTask
                .TaskLength
                .Common;
        }

        if (ContainsTaskType(
                ship.ShortTasks,
                typeId))
        {
            return NormalPlayerTask
                .TaskLength
                .Short;
        }

        if (ContainsTaskType(
                ship.LongTasks,
                typeId))
        {
            return NormalPlayerTask
                .TaskLength
                .Long;
        }

        return NormalPlayerTask
            .TaskLength
            .None;
    }

    /// <summary>
    /// Comprueba si una colección de tareas contiene
    /// una plantilla con el TypeId indicado.
    /// </summary>
    private static bool ContainsTaskType(
        Il2CppInterop.Runtime.InteropTypes.Arrays
            .Il2CppReferenceArray<NormalPlayerTask> tasks,
        byte typeId)
    {
        if (tasks == null)
        {
            return false;
        }

        for (var i = 0;
             i < tasks.Length;
             i++)
        {
            var task = tasks[i];

            if (task != null &&
                (byte)task.TaskType == typeId)
            {
                return true;
            }
        }

        return false;
    }
}