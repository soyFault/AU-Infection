using FauloInfection.Buttons;
using FauloInfection.Infection;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;

namespace FauloInfection.Events;

/// <summary>
/// Reglas globales de jugabilidad del modo Infection.
/// </summary>
public static class InfectionGameEvents
{
    /// <summary>
    /// Los asesinatos vanilla deben respetar las mismas reglas
    /// de equipo y configuración que el resto de Infection.
    /// </summary>
    [RegisterEvent(-10000)]
    public static void BeforeMurderEventHandler(
        BeforeMurderEvent @event)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        // Una infección fallida tiene su propia regla de muerte
        // y no depende de AllowInfectedKills ni FriendlyFire.
        if (InfectionManager.IsFailedInfectionKill(
                @event.Source,
                @event.Target))
        {
            return;
        }
        
        if (!InfectionManager.CanKillTarget(
                @event.Source,
                @event.Target))
        {
            @event.Cancel();
        }
    }
    
    /// <summary>
    /// Hide and Seek puede volver a ocultar los botones personalizados mientras
    /// se reproduce su intro. Refresca el HUD del Seeker inicial únicamente
    /// después de que la intro haya terminado por completo.
    /// </summary>
    [RegisterEvent]
    public static void IntroEndEventHandler(
        IntroEndEvent @event)
    {
        if (!InfectionManager.IsActive ||
            !HudManager.InstanceExists)
        {
            return;
        }

        var localPlayer =
            PlayerControl.LocalPlayer;

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

        var infectButton =
            CustomButtonSingleton<InfectButton>
                .Instance;
        
        infectButton.ResetCooldownAndOrEffect();

        infectButton.SetActive(
            true,
            localPlayer.Data.Role);
    }
}