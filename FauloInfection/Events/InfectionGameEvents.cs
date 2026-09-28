using FauloInfection.Infection;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;

namespace FauloInfection.Events;

public static class InfectionGameEvents
{

    [RegisterEvent(-10000)]
    public static void BeforeMurderEventHandler(
        BeforeMurderEvent @event)
    {
        if (!InfectionManager.IsActive)
        {
            return;
        }

        // Los asesinatos vanilla deben obedecer
        // las mismas reglas de equipos de Infection.
        if (!InfectionManager.CanKillTarget(
                @event.Source,
                @event.Target))
        {
            @event.Cancel();
        }
    }
}