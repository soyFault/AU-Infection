using FauloInfection.GameModes;
using FauloInfection.Infection;
using MiraAPI.Translation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FauloInfection.UI;

/// <summary>
/// Handles Infection-specific HUD notifications.
/// Maneja las notificaciones del HUD específicas de Infection.
/// </summary>
public static class InfectionHud
{
    /// <summary>
    /// Shows a Hide & Seek-style popup when a player becomes infected.
    /// Muestra un popup estilo Hide & Seek cuando un jugador es infectado.
    /// </summary>
    public static void ShowInfected(PlayerControl? player)
    {
        if (player == null ||
            player.Data == null ||
            !HudManager.InstanceExists)
        {
            return;
        }

        // Reuse the vanilla Hide & Seek death popup so Infection
        // keeps the same visual language without replacing the death HUD.
        // Reutiliza el popup de muerte de Hide & Seek para que Infection
        // conserve el mismo estilo visual sin reemplazar el HUD de muerte.
        var popupPrefab =
            GameManagerCreator.Instance
                .HideAndSeekManagerPrefab
                .DeathPopupPrefab;

        if (popupPrefab == null)
        {
            return;
        }

        var popup =
            Object.Instantiate(
                popupPrefab,
                HudManager.Instance.transform.parent);

        // The index only affects the popup's render depth.
        // El índice solo afecta la profundidad de renderizado del popup.
        popup.Show(
            player,
            InfectionManager.GetConnectedInfected().Count);

        if (popup.text != null &&
            popup.text.transform.TryGetComponent<TextTranslatorTMP>(
                out var translator))
        {
            // Replace the normal "died" message with our infection message.
            // Reemplaza el mensaje normal de "murió" por nuestro mensaje de infección.
            translator.defaultStr =
                MiraLocaleManager.Get(
                    "FauloInfection.Hud.PlayerInfected",
                    "Was infected");

            translator.TargetText =
                StringNames.None;

            translator.ResetText();
        }

        // Use Infection's color to distinguish conversion from death.
        // Usa el color de Infection para diferenciar una conversión de una muerte.
        if (popup.text != null)
        {
            popup.text.color =
                InfectionMode.InfectionColor;
        }
    }
}