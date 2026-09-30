using AmongUs.GameOptions;
using FauloInfection.Infection;
using HarmonyLib;
using UnityEngine;

namespace FauloInfection.Patches;

/// <summary>
/// Adaptadores entre los ductos vanilla y las reglas de Infection.
/// La lógica y el estado permanecen en InfectionVentController.
/// </summary>
[HarmonyPatch]
internal static class InfectionVentPatches
{
    /// <summary>
    /// Infection decide el acceso al ducto por equipo real,
    /// usos restantes y cooldown, independientemente del rol vanilla.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(Vent),
        nameof(Vent.CanUse))]
    private static void VentCanUsePostfix(
        Vent __instance,
        ref float __result,
        [HarmonyArgument(0)]
        NetworkedPlayerInfo pc,
        [HarmonyArgument(1)]
        ref bool canUse,
        [HarmonyArgument(2)]
        ref bool couldUse)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        var player =
            pc?.Object;

        if (player == null ||
            player.Data == null)
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

        // Nunca bloqueamos la salida/movimiento entre ductos
        // de un jugador que ya se encuentra dentro.
        if (player.inVent)
        {
            canUse = true;
            couldUse = true;
            __result = 0f;
            return;
        }

        if (!InfectionVentController.CanUseVent(
                player))
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

        // Si Infection concede el permiso, recalculamos CanUse
        // con las mismas comprobaciones físicas que utiliza MiraAPI.
        // Esto permite ventear incluso si el rol base no podría hacerlo.
        couldUse = true;

        var center =
            player.Collider.bounds.center;

        var position =
            __instance.transform.position;

        var distance =
            Vector2.Distance(
                center,
                position);

        canUse =
            distance <=
            __instance.UsableDistance &&
            !PhysicsHelpers.AnythingBetween(
                player.Collider,
                center,
                position,
                Constants.ShipOnlyMask,
                false);

        __result =
            distance;
    }

    /// <summary>
    /// El controlador necesita ejecutarse después de los updates vanilla
    /// para que su cooldown y contador sean la última presentación del frame.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(HudManager),
        nameof(HudManager.Update))]
    private static void HudManagerUpdatePostfix(
        HudManager __instance)
    {
        InfectionVentController.Update(
            __instance);
    }

    /// <summary>
    /// Engineer tiene su propio cooldown y temporizador de expulsión.
    ///
    /// Infection usa sus opciones VentCooldown/VentDuration como única
    /// fuente de verdad, así que neutralizamos esos dos temporizadores
    /// vanilla y dejamos que InfectionVentController los gestione.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(EngineerRole),
        nameof(EngineerRole.FixedUpdate))]
    private static void EngineerFixedUpdatePostfix(
        EngineerRole __instance)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        var player =
            PlayerControl.LocalPlayer;

        if (player == null ||
            player.Data?.Role == null ||
            player.Data.Role.Role !=
            RoleTypes.Engineer)
        {
            return;
        }

        __instance.cooldownSecondsRemaining =
            0f;

        if (player.inVent)
        {
            __instance.inVentTimeRemaining =
                float.MaxValue;
        }
    }
}
