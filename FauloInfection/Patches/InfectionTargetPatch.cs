using FauloInfection.Infection;
using FauloInfection.Options;
using HarmonyLib;
using MiraAPI.GameOptions;

namespace FauloInfection.Patches;

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
internal static class InfectionTargetPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        ImpostorRole __instance,
        NetworkedPlayerInfo target,
        ref bool __result)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        var source = __instance.Player;
        var targetPlayer = target?.Object;

        if (source == null ||
            targetPlayer == null ||
            !InfectionManager.IsInfected(source))
        {
            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>.Instance;

        // Si los asesinatos están desactivados,
        // ningún objetivo debe considerarse válido.
        if (!options.AllowInfectedKills.Value)
        {
            __result = false;
            return;
        }

        // El juego vanilla ve a los infectados convertidos como Tripulantes.
        // Aquí añadimos nuestra propia regla de equipo para que el Seeker
        // tampoco pueda seleccionarlos cuando Friendly Fire está apagado.
        if (!options.FriendlyFire.Value &&
            InfectionManager.IsInfected(targetPlayer))
        {
            __result = false;
        }
    }
}