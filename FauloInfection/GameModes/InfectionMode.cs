using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using FauloInfection.GameOver;
using FauloInfection.Infection;
using FauloInfection.Options;
using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.GameModes;
using MiraAPI.HnsReimplemented;
using MiraAPI.HnsReimplemented.Options;
using UnityEngine;

namespace FauloInfection.GameModes;

public sealed class InfectionMode : HideAndSeekMode
{
    public static Color InfectionColor { get; } =
        new Color32(69, 214, 107, 255);

    // Hace el modo visible porque Mira API por defecto los oculta en builds no dev.
    // (Makes the gamemode selectable bc Mira API hides it by default in non-dev builds)
    public override bool HideMode => false;

    public override string Name =>
        "FauloInfection.GameMode.Infection.Name";

    public override string Description =>
        "FauloInfection.GameMode.Infection.Description";

    public override Color Color => InfectionColor;

    /// <summary>
    /// Determina si Infection debe usar la presentación
    /// Horse/Wrangler de Hide and Seek.
    ///
    /// La opción propia del modo permite usarla aunque
    /// April Fools no esté activo.
    /// </summary>
    public static bool ShouldUseHorseModel()
    {
        return AprilFoolsMode.ShouldHorseAround();
    }
    
    /// <summary>
    /// Devuelve el tipo de cuerpo usado por cualquier miembro
    /// del equipo infectado.
    /// </summary>
    public static PlayerBodyTypes GetInfectedBodyType()
    {
        // El Wrangler de HnS utiliza el cuerpo Normal.
        // Su apariencia especial proviene de las animaciones
        // HorseWrangle/HnSSeekerSpawnHorse.
        if (ShouldUseHorseModel())
        {
            return PlayerBodyTypes.Normal;
        }

        if (AprilFoolsMode.ShouldLongAround())
        {
            return PlayerBodyTypes.LongSeeker;
        }

        return PlayerBodyTypes.Seeker;
    }

    /// <summary>
    /// Devuelve el tipo de cuerpo normal de los supervivientes.
    /// </summary>
    public static PlayerBodyTypes GetSurvivorBodyType()
    {
        if (ShouldUseHorseModel())
        {
            return PlayerBodyTypes.Horse;
        }

        if (AprilFoolsMode.ShouldLongAround())
        {
            return PlayerBodyTypes.Long;
        }

        return PlayerBodyTypes.Normal;
    }

    /// <summary>
    /// HideAndSeekMode solo reconoce como Seeker al Impostor base.
    /// Infection también debe tratar como Seeker a los jugadores
    /// convertidos mediante InfectedModifier.
    /// </summary>
    public override PlayerBodyTypes GetBodyType(
        PlayerControl player)
    {
        return InfectionManager.IsInfected(player)
            ? GetInfectedBodyType()
            : GetSurvivorBodyType();
    }

    
    
    /// <summary>
    /// Solo los supervivientes pueden utilizar consolas de tareas,
    /// y únicamente cuando las tareas están activadas.
    /// </summary>
    public override bool CanUseTasks(
        Console console)
    {
        var localPlayer =
            PlayerControl.LocalPlayer;

        return OptionGroupSingleton<InfectionOptions>
                   .Instance
                   .EnableTasks
                   .Value &&
               localPlayer != null &&
               !InfectionManager.IsInfected(localPlayer);
    }
    
    /// <summary>
    /// Limpia las tareas locales que Hide and Seek puede volver a crear
    /// durante la inicialización del HUD.
    ///
    /// El Seeker inicial nunca debe tener tareas de superviviente.
    /// Si las tareas están desactivadas, ningún jugador debe conservarlas.
    /// </summary>
    public override void Initialize()
    {
        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        // Infection usa su propia opción de Adrenalina como duración
        // de la fase final de Hide and Seek.
        //
        // Debe configurarse antes de base.Initialize(), porque la
        // infraestructura de HnS prepara sus temporizadores durante
        // esa inicialización.
        OptionGroupSingleton<HnsFinalHideOptions>
            .Instance
            .FinalHideTime
            .SetValue(
                options.AdrenalineActivationTime.Value,
                false);

        base.Initialize();
        
        // Limpia contadores, pings y notificaciones visuales
        // que pudieran quedar de la ronda anterior.
        InfectionHudController.ResetRound();
        
        // La nueva ronda crea una nueva fuente de iluminación,
        // así que debe volver a aplicarse la configuración de Infection.
        InfectionVisionController.ResetRound();
        
        // Cada ronda puede reutilizar los mismos IDs de tareas,
        // así que eliminamos el historial de la ronda anterior.
        InfectionTaskEvents.ResetRound();

        var localPlayer =
            PlayerControl.LocalPlayer;

        if (localPlayer == null ||
            localPlayer.Data == null)
        {
            return;
        }
        
        // Options ya fue obtenido antes de inicializar HnS.

        if (InfectionManager.IsInfected(localPlayer) ||
            !options.EnableTasks.Value)
        {
            localPlayer.ClearTasks();
        }
    }
    
    /// <summary>
    /// Copia las cantidades de tareas configuradas en Infection
    /// a las opciones vanilla antes de que HnS prepare la ronda.
    /// </summary>
    public override void AssignRoles(
        out bool runOriginal,
        LogicRoleSelectionNormal instance)
    {
        var infectionOptions =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        var vanillaOptions =
            GameOptionsManager
                .Instance
                .currentNormalGameOptions;

        if (infectionOptions.EnableTasks.Value)
        {
            vanillaOptions.NumCommonTasks =
                Mathf.RoundToInt(
                    infectionOptions.CommonTasks.Value);

            vanillaOptions.NumShortTasks =
                Mathf.RoundToInt(
                    infectionOptions.ShortTasks.Value);

            vanillaOptions.NumLongTasks =
                Mathf.RoundToInt(
                    infectionOptions.LongTasks.Value);
        }
        else
        {
            vanillaOptions.NumCommonTasks = 0;
            vanillaOptions.NumShortTasks = 0;
            vanillaOptions.NumLongTasks = 0;
        }

        base.AssignRoles(
            out runOriginal,
            instance);
    }

    // Inicializa el estado de Infection solo después de asignar todos los roles,
    // para poder identificar de forma fiable al Seeker inicial mediante su rol base.
    //
    // Initialize Infection state only after all player roles have been assigned,
    // so the initial Seeker can be reliably identified from their base role.
    public override void PostAssignRoles(
        LogicRoleSelectionNormal instance)
    {
        base.PostAssignRoles(instance);

        InfectionManager.InitializeRound();
        InfectionTaskManager.AssignInitialTasks();
    }
    
    /// <summary>
    /// Reemplaza las condiciones de victoria de Hide and Seek
    /// por condiciones basadas en el equipo real de Infection.
    ///
    /// Solo el host puede terminar la partida.
    /// </summary>
    public override void CheckGameEnd(
        out bool runOriginal,
        LogicGameFlowNormal instance)
    {
        // No queremos que HideAndSeekMode ejecute después
        // sus condiciones basadas en Impostor/Crewmate.
        runOriginal = false;

        if (AmongUsClient.Instance == null ||
            AmongUsClient.Instance.IsGameOver ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var infectedCount =
            InfectionManager.GetInfected().Count;

        var survivorCount =
            InfectionManager.RemainingSurvivors;

        // Ya no queda ningún superviviente vivo.
        // Los infectados ganan.
        if (survivorCount == 0 &&
            infectedCount > 0)
        {
            CustomGameOver.Trigger<InfectedVictoryGameOver>(
                InfectionManager
                    .GetConnectedInfected()
                    .Select(player => player.Data)
                    .Where(data => data != null));

            return;
        }

        // Ya no queda ningún infectado vivo.
        // Los supervivientes ganan.
        if (infectedCount == 0 &&
            survivorCount > 0)
        {
            CustomGameOver.Trigger<SurvivorVictoryGameOver>(
                InfectionManager
                    .GetConnectedSurvivors()
                    .Select(player => player.Data)
                    .Where(data => data != null));

            return;
        }

        // Si termina el tiempo de Hide and Seek y todavía
        // queda al menos un superviviente, sobreviven y ganan.
        if (survivorCount > 0 &&
            HideAndSeekHudHelper.Instance != null &&
            HideAndSeekHudHelper.Instance.AllTimersExpired())
        {
            CustomGameOver.Trigger<SurvivorVictoryGameOver>(
                InfectionManager
                    .GetConnectedSurvivors()
                    .Select(player => player.Data)
                    .Where(data => data != null));
        }
    }

    /// <summary>
    /// Decide qué jugadores aparecen como ganadores.
    ///
    /// Aquí usamos el estado de Infection y no el rol base,
    /// porque un infectado convertido puede seguir teniendo
    /// Engineer o Crewmate como rol.
    ///
    /// Los muertos conectados siguen siendo parte de su equipo.
    /// Los desconectados no aparecen como ganadores.
    /// </summary>
    public override List<NetworkedPlayerInfo>? CalculateWinners()
    {
        var infectedWon =
            InfectionManager.RemainingSurvivors == 0;

        var winningTeam =
            infectedWon
                ? InfectionManager.GetConnectedInfected()
                : InfectionManager.GetConnectedSurvivors();

        var winners =
            new List<NetworkedPlayerInfo>();

        foreach (var player in winningTeam)
        {
            if (player.Data != null)
            {
                winners.Add(player.Data);
            }
        }

        return winners;
    }

    public override void HudUpdate(
        HudManager instance)
    {
        base.HudUpdate(instance);
        
        // Infection no permite reportar cuerpos.
        // Mantenemos el botón oculto aunque otro refresh del HUD
        // intente volver a mostrarlo.
        if (instance.ReportButton)
        {
            instance.ReportButton.SetDisabled();
            instance.ReportButton.ToggleVisible(false);
        }

        var localPlayer =
            PlayerControl.LocalPlayer;

        if (localPlayer != null &&
            localPlayer.Data?.Role != null &&
            localPlayer.Data.Role.IsImpostor)
        {
            // El Seeker inicial usa el botón Kill vanilla.
            // Si los asesinatos están desactivados, mantenemos ese botón oculto
            // incluso si Among Us refresca el HUD e intenta mostrarlo otra vez.
            if (!OptionGroupSingleton<InfectionOptions>
                    .Instance
                    .AllowInfectedKills
                    .Value)
            {
                instance.KillButton.ToggleVisible(false);
            }
        }
        
        // Mantiene sincronizados los elementos de Hide and Seek adaptados
        // a Infection: peligro, contador de conversiones y demás HUD propio.
        InfectionHudController.Update(instance);
        
        // La linterna es presentación local y debe adaptarse si
        // el jugador cambia de superviviente a infectado.
        InfectionVisionController.UpdateLocalLighting();
    }
}
