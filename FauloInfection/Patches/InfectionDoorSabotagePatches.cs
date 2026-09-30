using System;
using AmongUs.GameOptions;
using FauloInfection.GameModes;
using FauloInfection.Infection;
using HarmonyLib;
using MiraAPI.GameModes;

namespace FauloInfection.Patches;

/// <summary>
/// Adaptadores entre el HUD/sistemas vanilla de sabotaje
/// y las reglas de puertas de Infection.
///
/// La autoridad de permisos permanece en
/// InfectionDoorSabotageController.
/// </summary>
[HarmonyPatch]
internal static class InfectionDoorSabotagePatches
{
    /// <summary>
    /// HideAndSeekMode normalmente transforma cualquier intento
    /// de abrir el mapa de sabotaje en un mapa normal.
    ///
    /// Infection permite el mapa de sabotaje exclusivamente cuando
    /// el jugador local es infectado y la opción de puertas está activa.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(HideAndSeekMode),
        nameof(HideAndSeekMode.ShouldShowSabotageMap))]
    private static bool ShouldShowSabotageMapPrefix(
        HideAndSeekMode __instance,
        ref bool __result)
    {
        if (__instance is not InfectionMode)
        {
            return true;
        }

        __result =
            InfectionDoorSabotageController
                .CanLocalPlayerSabotageDoors();

        return false;
    }

    /// <summary>
    /// HnS normalmente no ofrece sabotajes.
    ///
    /// Durante Infection ignoramos esa restricción global y decidimos
    /// nosotros mismos si el botón debe existir. De esta forma también
    /// funciona para infectados convertidos que conservan un rol
    /// vanilla de tripulante.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(SabotageButton),
        nameof(SabotageButton.Refresh))]
    private static bool SabotageButtonRefreshPrefix(
        SabotageButton __instance)
    {
        if (!InfectionManager.IsActive)
        {
            return true;
        }

        var player =
            PlayerControl.LocalPlayer;

        var visible =
            InfectionDoorSabotageController
                .CanPlayerSabotageDoors(
                    player);

        __instance.ToggleVisible(
            visible);

        if (!visible ||
            player == null ||
            player.inVent ||
            player.petting)
        {
            __instance.SetDisabled();
        }
        else
        {
            __instance.SetEnabled();
        }

        // Infection ya decidió completamente el estado del botón.
        return false;
    }

    /// <summary>
    /// Abre directamente el overlay de sabotaje para un infectado
    /// autorizado.
    ///
    /// No dependemos de GameManager.SabotagesEnabled(), porque HnS
    /// deshabilita sus sabotajes normales y Infection solo quiere
    /// reactivar el cierre de puertas.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(SabotageButton),
        nameof(SabotageButton.DoClick))]
    private static bool SabotageButtonDoClickPrefix(
        SabotageButton __instance)
    {
        if (!InfectionManager.IsActive)
        {
            return true;
        }

        var player =
            PlayerControl.LocalPlayer;

        if (!InfectionDoorSabotageController
                .CanPlayerSabotageDoors(
                    player) ||
            player == null ||
            player.inVent ||
            player.petting ||
            !HudManager.InstanceExists)
        {
            return false;
        }

        HudManager.Instance.ToggleMapVisible(
            new MapOptions
            {
                Mode = MapOptions.Modes.Sabotage,
            });

        return false;
    }

    /// <summary>
    /// El mapa de sabotaje vanilla contiene puertas y sabotajes críticos.
    ///
    /// Infection oculta todos los botones que no correspondan a puertas,
    /// de modo que el overlay presenta únicamente la mecánica permitida.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(MapBehaviour),
        nameof(MapBehaviour.ShowSabotageMap))]
    private static void ShowSabotageMapPostfix(
        MapBehaviour __instance)
    {
        if (!InfectionDoorSabotageController
                .CanLocalPlayerSabotageDoors())
        {
            return;
        }

        var overlay =
            __instance.infectedOverlay;

        if (overlay == null ||
            overlay.allButtons == null)
        {
            return;
        }

        foreach (var button in
                 overlay.allButtons)
        {
            if (button == null)
            {
                continue;
            }

            if (!IsDoorButton(
                    button))
            {
                button.gameObject.SetActive(
                    false);
            }
        }
    }

    /// <summary>
    /// Defensa final para la ruta real de cierre de puertas.
    ///
    /// Aunque otra parte del HUD o un mod intente llamar directamente
    /// RpcCloseDoorsOfType, Infection vuelve a comprobar el permiso.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(ShipStatus),
        nameof(ShipStatus.RpcCloseDoorsOfType))]
    private static bool RpcCloseDoorsOfTypePrefix()
    {
        if (!InfectionManager.IsActive)
        {
            return true;
        }

        return InfectionDoorSabotageController
            .CanLocalPlayerSabotageDoors();
    }

    /// <summary>
    /// Bloquea cualquier RpcUpdateSystem iniciado por un infectado
    /// que no corresponda a un sistema de puertas.
    ///
    /// Los supervivientes siguen pudiendo interactuar con sistemas
    /// normalmente para reparar lo que corresponda. El objetivo de
    /// este filtro es impedir que el equipo infectado inicie otros
    /// sabotajes como luces, reactor, O2, comunicaciones, etc.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(
        typeof(ShipStatus),
        nameof(ShipStatus.RpcUpdateSystem),
        typeof(SystemTypes),
        typeof(byte))]
    private static bool RpcUpdateSystemPrefix(
        [HarmonyArgument(0)]
        SystemTypes systemType)
    {
        if (!InfectionManager.IsActive)
        {
            return true;
        }

        var player =
            PlayerControl.LocalPlayer;

        if (player == null ||
            !InfectionManager.IsInfected(player))
        {
            return true;
        }

        if (!InfectionDoorSabotageController
                .CanPlayerSabotageDoors(
                    player))
        {
            return false;
        }

        return InfectionDoorSabotageController
            .IsDoorSystem(
                systemType);
    }

    /// <summary>
    /// Algunos refreshes del HUD de HnS pueden volver a modificar
    /// la visibilidad de botones vanilla.
    ///
    /// Aplicamos la regla de Infection al final del frame para que
    /// supervivientes e infectados sin permiso nunca conserven visible
    /// el botón de sabotaje.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(HudManager),
        nameof(HudManager.Update))]
    private static void HudManagerUpdatePostfix(
        HudManager __instance)
    {
        if (!InfectionManager.IsActive ||
            !__instance.SabotageButton)
        {
            return;
        }

        var player =
            PlayerControl.LocalPlayer;

        var visible =
            InfectionDoorSabotageController
                .CanPlayerSabotageDoors(
                    player);

        __instance.SabotageButton.ToggleVisible(
            visible);

        if (!visible ||
            player == null ||
            player.inVent ||
            player.petting)
        {
            __instance.SabotageButton.SetDisabled();
        }
        else
        {
            __instance.SabotageButton.SetEnabled();
        }
    }

    /// <summary>
    /// Identifica los botones de cierre de puertas del overlay.
    ///
    /// Vanilla y TOU:Mira utilizan nombres como "Doors" y
    /// "closeDoors", por lo que comprobamos tanto el componente
    /// como su GameObject sin depender de un mapa específico.
    /// </summary>
    private static bool IsDoorButton(
        ButtonBehavior button)
    {
        return ContainsDoor(
                   button.name) ||
               ContainsDoor(
                   button.gameObject.name);
    }

    private static bool ContainsDoor(
        string? value)
    {
        return !string.IsNullOrEmpty(
                   value) &&
               value.Contains(
                   "door",
                   StringComparison.OrdinalIgnoreCase);
    }
}
