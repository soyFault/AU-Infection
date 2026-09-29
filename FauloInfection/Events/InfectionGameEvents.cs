using FauloInfection.Buttons;
using FauloInfection.Infection;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;

namespace FauloInfection.Events;

/// <summary>
/// Gameplay rules that are global to the Infection game mode.
/// </summary>
public static class InfectionGameEvents
{
    /// <summary>
    /// Infection replaces murder with conversion, so no murder path is allowed to
    /// complete while the mode is active. This also protects against the vanilla
    /// kill keybind or another mod attempting to trigger a normal murder.
    /// </summary>
    [RegisterEvent(-10000)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        @event.Cancel();
    }
    
    /// <summary>
    /// Hide and Seek puede volver a ocultar los botones personalizados mientras
    /// se reproduce su intro. Refresca el HUD del Seeker inicial únicamente
    /// después de que la intro haya terminado por completo.
    /// </summary>
    [RegisterEvent]
    public static void IntroEndEventHandler(IntroEndEvent @event)
    {
        if (!InfectionManager.IsActive ||
            !HudManager.InstanceExists)
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null ||
            localPlayer.Data?.Role == null ||
            !InfectionManager.IsInfected(localPlayer))
        {
            return;
        }

        HudManager.Instance.SetHudActive(
            localPlayer,
            localPlayer.Data.Role,
            true);

        CustomButtonSingleton<InfectButton>
            .Instance
            .SetActive(true, localPlayer.Data.Role);
    }
}