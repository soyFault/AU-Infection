using FauloInfection.Infection;
using FauloInfection.Options;
using HarmonyLib;
using MiraAPI.GameOptions;

namespace FauloInfection.Patches;

/// <summary>
/// Hace que Infection pueda utilizar correctamente
/// el sistema de linterna de Hide and Seek.
/// </summary>
[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.IsFlashlightEnabled))]
internal static class InfectionFlashlightEnabledPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PlayerControl __instance,
        ref bool __result)
    {
        if (!InfectionManager.IsActive ||
            __instance == null ||
            __instance != PlayerControl.LocalPlayer)
        {
            return true;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        // Infection controla su propia condición de linterna.
        //
        // No dejamos que el método vanilla compruebe si el GameMode
        // es HideNSeek, porque Infection es un modo custom de Mira.
        __result =
            options.UseHnsVision.Value &&
            __instance.Data != null &&
            !__instance.Data.IsDead &&
            LobbyBehaviour.Instance == null;

        return false;
    }
}

/// <summary>
/// Configura físicamente el LightSource del jugador local.
///
/// Esto reproduce la ruta utilizada por Hide and Seek:
///
/// SetFlashlightInputMethod()
/// -> SetupLightingForGameplay(...)
/// </summary>
[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.AdjustLighting))]
internal static class InfectionAdjustLightingPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PlayerControl __instance)
    {
        if (!InfectionManager.IsActive ||
            __instance == null ||
            __instance != PlayerControl.LocalPlayer)
        {
            return true;
        }

        if (__instance.Data == null ||
            __instance.lightSource == null ||
            __instance.TargetFlashlight == null)
        {
            return true;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        var useFlashlight =
            options.UseHnsVision.Value &&
            !__instance.Data.IsDead &&
            LobbyBehaviour.Instance == null;

        // HnS permite tamaños diferentes para Crewmate e Impostor.
        //
        // Infection conserva esa separación, pero utiliza sus propias
        // opciones de supervivientes e infectados para no depender
        // de las opciones internas del modo HnS de MiraAPI.
        var flashlightSize =
            useFlashlight
                ? InfectionVisionController
                    .GetFlashlightSize(__instance)
                : 0f;

        __instance.SetFlashlightInputMethod();

        __instance.lightSource.SetupLightingForGameplay(
            useFlashlight,
            flashlightSize,
            __instance.TargetFlashlight.transform);

        // Ya hicimos toda la configuración que necesitábamos.
        // Evitamos que AdjustLighting vanilla la sobrescriba.
        return false;
    }
}

/// <summary>
/// Aplica los radios de visión propios de Infection
/// según el equipo real de cada jugador.
/// </summary>
[HarmonyPatch(
    typeof(ShipStatus),
    nameof(ShipStatus.CalculateLightRadius))]
internal static class InfectionLightRadiusPatch
{
    [HarmonyPostfix]
    private static void CalculateLightRadiusPostfix(
        ShipStatus __instance,
        NetworkedPlayerInfo player,
        ref float __result)
    {
        if (!InfectionManager.IsActive ||
            player == null)
        {
            return;
        }

        // Conservamos la visión completa de los muertos.
        if (player.IsDead)
        {
            __result =
                __instance.MaxLightRadius;

            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;
        
        // Cuando usamos la visión de Hide and Seek,
        // la linterna es el sistema de visión autoritativo.
        //
        // Los multiplicadores propios de Infection solo
        // se aplican cuando esa linterna está desactivada.
        if (options.UseHnsVision.Value)
        {
            return;
        }

        float vision;

        // El Impostor base es el Seeker inicial.
        // Debe usar su ajuste independiente.
        if (player.Role?.IsImpostor == true)
        {
            vision =
                options
                    .InitialInfectedVision
                    .Value;
        }
        // Un infectado convertido conserva normalmente
        // su rol base, así que lo identificamos por el estado de Infection.
        else if (player.Object != null &&
                 InfectionManager.IsInfected(
                     player.Object))
        {
            vision =
                options
                    .InfectedVision
                    .Value;
        }
        else
        {
            vision =
                options
                    .CrewmateVision
                    .Value;
        }

        __result =
            __instance.MaxLightRadius *
            vision;
    }
}