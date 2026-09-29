using FauloInfection.Buttons;
using FauloInfection.UI;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Translation;

namespace FauloInfection.Infection;

public sealed class InfectedModifier : BaseModifier
{
    private bool refreshHudNextFixedUpdate;

    public override string ModifierName =>
        MiraLocaleManager.Get(
            "FauloInfection.Modifier.Infected.Name",
            "Infected");

    public override bool HideOnUi => true;

    public override bool ShowInFreeplay => false;

    public override void OnActivate()
    {
        base.OnActivate();
        
        // Un jugador convertido deja inmediatamente de ser
        // un superviviente y no debe conservar sus tareas.
        Player.ClearTasks();

        // El host sincroniza la lista vacía para que todos los clientes
        // compartan el mismo estado de tareas del jugador convertido.
        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            Player.Data != null)
        {
            Player.Data.RpcSetTasks(
                Array.Empty<byte>());
        }

        // Every client that receives the synchronized modifier
        // shows the infection notification.
        // Cada cliente que recibe el modificador sincronizado
        // muestra la notificación de infección.
        if (InfectionManager.IsActive &&
            HudManager.InstanceExists)
        {
            InfectionHud.ShowInfected(Player);
        }

        if (!Player.AmOwner ||
            Player.Data?.Role == null ||
            !HudManager.InstanceExists)
        {
            return;
        }


        // MiraAPI llama OnActivate antes de actualizar ActiveModifiers.
        // Esperamos al FixedUpdate del modifier, cuando el estado de infección
        // ya puede ser consultado correctamente por los botones.
        refreshHudNextFixedUpdate = true;
    }
    
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!refreshHudNextFixedUpdate ||
            !Player.AmOwner ||
            Player.Data?.Role == null ||
            !HudManager.InstanceExists)
        {
            return;
        }

        refreshHudNextFixedUpdate = false;

        // Reproduce el mismo tipo de refresco de HUD que hacía
        // aparecer los botones al abrir y cerrar el mapa.
        HudManager.Instance.SetHudActive(
            Player,
            Player.Data.Role,
            true);

        var infectButton =
            CustomButtonSingleton<InfectButton>.Instance;

        infectButton.ResetCooldownAndOrEffect();
        infectButton.SetActive(
            true,
            Player.Data.Role);

        var killButton =
            CustomButtonSingleton<InfectedKillButton>.Instance;

        killButton.ResetCooldownAndOrEffect();
        killButton.SetActive(
            true,
            Player.Data.Role);
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        
        refreshHudNextFixedUpdate = false;

        if (!Player.AmOwner ||
            Player.Data?.Role == null ||
            !HudManager.InstanceExists)
        {
            return;
        }

        // Hide the Infection and Kil action again if the infection marker is removed.
        // Oculta nuevamente la acción de Infectar y Matar si se elimina el marcador de infección.
        CustomButtonSingleton<InfectButton>
            .Instance
            .SetActive(
                false,
                Player.Data.Role);

        CustomButtonSingleton<InfectedKillButton>
            .Instance
            .SetActive(
                false,
                Player.Data.Role);
    }
}