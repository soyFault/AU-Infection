using FauloInfection.Infection;
using HarmonyLib;
using MiraAPI.HnsReimplemented;

namespace FauloInfection.Patches;

/// <summary>
/// Evita que el cálculo vanilla de HnS de MiraAPI
/// sobrescriba el medidor adaptado a Infection.
/// </summary>
[HarmonyPatch]
internal static class InfectionHnsHudPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(HnsDangerMeter),
        nameof(HnsDangerMeter.FixedUpdate))]
    private static bool HnsDangerMeterFixedUpdatePrefix()
    {
        return !InfectionManager.IsActive;
    }
}