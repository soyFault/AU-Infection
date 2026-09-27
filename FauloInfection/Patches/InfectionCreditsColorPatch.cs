using System.Text.RegularExpressions;
using HarmonyLib;

namespace FauloInfection.Patches;

[HarmonyPatch(typeof(Reactor.Utilities.ReactorCredits), "GetText")]
public static class InfectionCreditsColorPatch
{
    // Aquí se cambia el color del Menú Principal // Main Menu color patch
    public const string CreditsColor = "#45D66B";

    private const string CreditsLabel =
        InfectionPlugin.Name + " " + InfectionPlugin.Version;

    private static void Postfix(ref string? __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            return;
        }

        var coloredLabel =
            $"<color={CreditsColor}><noparse>{CreditsLabel}</noparse></color>";

        var updated = Regex.Replace(
            __result,
            $@"<color=#[0-9A-Fa-f]{{3,8}}><noparse>{Regex.Escape(CreditsLabel)}</noparse></color>",
            coloredLabel);

        if (updated == __result)
        {
            updated = __result.Replace(
                $"<noparse>{CreditsLabel}</noparse>",
                coloredLabel);
        }

        if (updated == __result)
        {
            updated = __result.Replace(
                CreditsLabel,
                coloredLabel);
        }

        __result = updated;
    }
}