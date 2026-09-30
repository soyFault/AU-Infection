using FauloInfection.Infection;
using FauloInfection.Options;
using HarmonyLib;
using MiraAPI.GameOptions;

namespace FauloInfection.Patches;

/// <summary>
/// Permite que Infection active las variantes visuales de April Fools
/// sin modificar globalmente el comportamiento de otros modos de juego.
/// </summary>
[HarmonyPatch]
internal static class InfectionAprilFoolsPatches
{
    /// <summary>
    /// Mientras Infection está activo, UseHorseModel hace que todos
    /// los sistemas vanilla/MiraAPI reconozcan realmente Horse Mode.
    ///
    /// Fuera de Infection se conserva completamente el resultado vanilla.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(AprilFoolsMode),
        nameof(AprilFoolsMode.ShouldHorseAround))]
    private static void ShouldHorseAroundPostfix(
        ref bool __result)
    {
        if (__result ||
            !InfectionManager.IsActive)
        {
            return;
        }

        __result =
            OptionGroupSingleton<InfectionOptions>
                .Instance
                .UseHorseModel
                .Value;
    }
}