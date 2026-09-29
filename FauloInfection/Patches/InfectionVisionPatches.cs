using FauloInfection.Infection;
using FauloInfection.Options;
using HarmonyLib;
using MiraAPI.GameOptions;

namespace FauloInfection.Patches;

/// <summary>
/// Aplica los radios de visión propios de Infection
/// según el equipo real de cada jugador.
/// </summary>
[HarmonyPatch(
    typeof(ShipStatus),
    nameof(ShipStatus.CalculateLightRadius))]
internal static class InfectionVisionPatches
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