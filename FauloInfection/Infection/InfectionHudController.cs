using System;
using FauloInfection.Options;
using MiraAPI.GameOptions;
using MiraAPI.HnsReimplemented;
using MiraAPI.HnsReimplemented.Options;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FauloInfection.Infection;

/// <summary>
/// Mantiene los elementos visuales de Hide and Seek
/// sincronizados con el estado real de Infection.
/// </summary>
public static class InfectionHudController
{

    // El tracker representa estados discretos que cambian muy poco,
    // así que no necesita recalcularse cada frame.
    private const float TrackerUpdateInterval = 0.25f;

    // El medidor sí depende del movimiento, pero 10 Hz es suficiente
    // para que visualmente responda de forma continua.
    private const float DangerUpdateInterval = 0.10f;

    private static float _nextTrackerUpdateAt;
    private static float _nextDangerUpdateAt;
    private static ObjectPoolBehavior? _pingPool;
    private static float _nextPingAt;
    private static float _hidePingsAt;
    
    private static int _infectionPopupCount;

    /// <summary>
    /// Obtiene el HnsMusicHandler que HideAndSeekMode
    /// añade al HudManager.
    ///
    /// No utilizamos HnsMusicHandler.Instance porque distintas
    /// builds de MiraAPI no exponen necesariamente ese campo
    /// estático de forma compatible.
    /// </summary>
    private static HnsMusicHandler? GetMusicHandler(
        HudManager? hud = null)
    {
        if (hud != null)
        {
            return hud.gameObject
                .GetComponent<HnsMusicHandler>();
        }

        if (!HudManager.InstanceExists)
        {
            return null;
        }

        return HudManager.Instance
            .gameObject
            .GetComponent<HnsMusicHandler>();
    }

    /// <summary>
    /// Limpia el estado visual temporal de Infection
    /// al comenzar una nueva ronda.
    /// </summary>
    public static void ResetRound()
    {
        _nextTrackerUpdateAt = 0f;
        _nextDangerUpdateAt = 0f;
        _nextPingAt = 0f;
        _hidePingsAt = 0f;
        _infectionPopupCount = 0;

        if (_pingPool != null)
        {
            Object.Destroy(
                _pingPool.gameObject);

            _pingPool = null;
        }

        // El componente puede existir ya porque InfectionMode
        // llama base.Initialize() antes de ResetRound().
        //
        // Si las pistas todavía no han sido inicializadas,
        // ResetMusic simplemente no modifica nada.
        var music =
            GetMusicHandler();

        if (music != null)
        {
            music.ResetMusic();
        }
    }

    /// <summary>
    /// Muestra una notificación visual cuando un superviviente
    /// se convierte en infectado.
    ///
    /// Esta opción no afecta las notificaciones de muertes reales.
    /// </summary>
    public static void NotifyInfection(
        PlayerControl player)
    {
        if (!OptionGroupSingleton<InfectionOptions>
                .Instance
                .NotifyInfections
                .Value ||
            !HudManager.InstanceExists)
        {
            return;
        }

        // Reutilizamos la presentación de HnS para mostrar
        // visualmente que un jugador dejó de ser superviviente.
        // Esto no marca al jugador como muerto.
        HudManager.Instance.NotifyOfDeath();

        var popup =
            GameManagerCreator
                .Instance
                .HideAndSeekManagerPrefab
                .DeathPopupPrefab;

        _infectionPopupCount++;

        var item =
            Object.Instantiate(
                popup,
                HudManager.Instance.transform.parent);

        item.Show(
            player,
            _infectionPopupCount);
    }


    public static void Update(
        HudManager hud)
    {
        var now = Time.time;
        
        UpdateTaskPanelVisibility(hud);

        if (now >= _nextTrackerUpdateAt)
        {
            _nextTrackerUpdateAt =
                now + TrackerUpdateInterval;

            UpdateSurvivorTracker(hud);
        }

        if (now >= _nextDangerUpdateAt)
        {
            _nextDangerUpdateAt =
                now + DangerUpdateInterval;

            UpdateDangerMeter(hud);
        }
        
        UpdateAdrenalinePings();
    }
    
    /// <summary>
    /// Oculta completamente el panel de tareas cuando
    /// el jugador no tiene tareas reales que mostrar.
    /// </summary>
    private static void UpdateTaskPanelVisibility(
        HudManager hud)
    {
        var localPlayer =
            PlayerControl.LocalPlayer;

        if (!hud.TaskPanel ||
            localPlayer == null ||
            localPlayer.Data == null)
        {
            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;
        
        var hasPendingTasks = false;

        var tasks =
            localPlayer.Data.Tasks;

        if (tasks != null)
        {
            for (var i = 0;
                 i < tasks.Count;
                 i++)
            {
                var task =
                    tasks[i];

                if (task != null &&
                    !task.Complete)
                {
                    hasPendingTasks = true;
                    break;
                }
            }
        }

        var showTaskPanel =
            options.EnableTasks.Value &&
            !InfectionManager.IsInfected(localPlayer) &&
            hasPendingTasks;

        if (hud.TaskPanel.gameObject.activeSelf !=
            showTaskPanel)
        {
            hud.TaskPanel.gameObject.SetActive(
                showTaskPanel);
        }
    }

    /// <summary>
    /// Usa el contador visual de HnS para representar
    /// supervivientes que ya fueron convertidos o murieron.
    /// </summary>
    private static void UpdateSurvivorTracker(
        HudManager hud)
    {
        var tracker =
            hud.CrewmatesKilled;

        if (!tracker)
        {
            return;
        }

        tracker.gameObject.SetActive(true);
        
        // Recorremos directamente la colección de jugadores.
        var removedSurvivorCount = 0;

        var players =
            PlayerControl.AllPlayerControls;

        for (var i = 0;
             i < players.Count;
             i++)
        {
            var player =
                players[i];

            if (player == null ||
                player.Data == null ||
                player.Data.Role == null)
            {
                continue;
            }

            // El Seeker inicial nunca ocupa uno de los
            // iconos de Crewmate del tracker.
            if (player.Data.Role.IsImpostor)
            {
                continue;
            }

            // Un jugador debe consumir como máximo un icono.
            // Si fue convertido y después murió, sigue contando una vez.
            if (player.Data.IsDead ||
                InfectionManager.IsInfected(player))
            {
                removedSurvivorCount++;
            }
        }

        if (removedSurvivorCount <= 0)
        {
            return;
        }

        var spriteCount =
            tracker.crewmateSprites.Count;

        var animationCount =
            tracker.slashAnimations.Count;

        if (spriteCount == 0 ||
            animationCount == 0)
        {
            return;
        }

        var amountToMark =
            Math.Min(
                removedSurvivorCount,
                spriteCount);

        for (var i = 0;
             i < amountToMark;
             i++)
        {
            var sprite =
                tracker.crewmateSprites[i];

            if (!sprite ||
                sprite.IsKilled)
            {
                continue;
            }

            sprite.SetKilled(
                tracker.slashAnimations[
                    i % animationCount]);
        }
    }

    /// <summary>
    /// Calcula el peligro usando cualquier infectado vivo,
    /// incluido el Seeker inicial y los infectados convertidos.
    ///
    /// El mismo nivel calculado para el DangerMeter se envía
    /// también al sistema musical de Hide and Seek.
    /// </summary>
    private static void UpdateDangerMeter(
        HudManager hud)
    {
        var meter =
            hud.DangerMeter;

        var localPlayer =
            PlayerControl.LocalPlayer;

        if (!meter ||
            localPlayer == null ||
            localPlayer.Data == null)
        {
            return;
        }

        var options =
            OptionGroupSingleton<InfectionOptions>
                .Instance;

        var music =
            GetMusicHandler(hud);

        // Si las ayudas de proximidad están desactivadas,
        // ocultamos el medidor y mantenemos únicamente
        // la música normal de superviviente.
        if (options
            .DisableProximityIndicators
            .Value)
        {
            meter.SetDangerValue(
                0f,
                0f);

            meter.gameObject.SetActive(
                false);

            if (music != null)
            {
                music.SetMusicValues(
                    0f,
                    0f);
            }

            return;
        }

        // Un infectado no necesita saber qué tan cerca
        // está otro miembro de su propio equipo.
        if (localPlayer.Data.Disconnected ||
            localPlayer.Data.IsDead ||
            InfectionManager.IsInfected(localPlayer))
        {
            meter.SetDangerValue(
                0f,
                0f);

            meter.gameObject.SetActive(
                false);

            return;
        }

        meter.gameObject.SetActive(
            true);

        var baseSpeed =
            OptionGroupSingleton<HnsCrewmateOptions>
                .Instance
                .PlayerSpeed
                .Value;

        var scaryDistance =
            55f * baseSpeed;

        var veryScaryDistance =
            15f * baseSpeed;

        if (scaryDistance <
            veryScaryDistance)
        {
            (
                scaryDistance,
                veryScaryDistance
            ) =
            (
                veryScaryDistance,
                scaryDistance
            );
        }
        
        // Buscamos directamente al infectado vivo más cercano.
        var closestDistanceSquared =
            float.MaxValue;

        var foundInfected = false;

        var players =
            PlayerControl.AllPlayerControls;

        for (var i = 0;
             i < players.Count;
             i++)
        {
            var player =
                players[i];

            if (player == null ||
                player == localPlayer ||
                player.Data == null ||
                player.Data.Disconnected ||
                player.Data.IsDead ||
                !InfectionManager.IsInfected(player))
            {
                continue;
            }

            foundInfected = true;

            var distanceSquared =
                (player.transform.position -
                 localPlayer.transform.position)
                .sqrMagnitude;

            if (distanceSquared <
                closestDistanceSquared)
            {
                closestDistanceSquared =
                    distanceSquared;
            }
        }

        if (!foundInfected)
        {
            meter.SetDangerValue(
                0f,
                0f);

            if (music != null)
            {
                music.SetMusicValues(
                    0f,
                    0f);
            }

            return;
        }

        // Conservamos aquí la misma escala usada por
        // la implementación HnS de MiraAPI.
        var dangerLevel1 =
            Mathf.Clamp01(
                (scaryDistance -
                 closestDistanceSquared) /
                (scaryDistance -
                 veryScaryDistance));

        var dangerLevel2 =
            Mathf.Clamp01(
                (veryScaryDistance -
                 closestDistanceSquared) /
                veryScaryDistance);

        meter.SetDangerValue(
            dangerLevel1,
            dangerLevel2);

        // HnsMusicHandler conserva el control de sus propios
        // AudioSource y realiza el crossfade en FixedUpdate.
        //
        // Infection únicamente le proporciona los mismos
        // niveles de peligro que acabamos de mostrar en el HUD.
        if (music != null)
        {
            music.SetMusicValues(
                dangerLevel1,
                dangerLevel2);
        }
    }
    
    /// <summary>
    /// Durante Adrenalina, muestra periódicamente a los infectados
    /// la dirección de cada superviviente vivo.
    /// </summary>
    private static void UpdateAdrenalinePings()
    {
        var localPlayer =
            PlayerControl.LocalPlayer;

        var helper =
            HideAndSeekHudHelper.Instance;

        if (localPlayer == null ||
            localPlayer.Data == null ||
            helper == null ||
            !helper.IsFinalCountdown ||
            !InfectionManager.IsInfected(localPlayer))
        {
            HideActivePings();
            return;
        }

        if (_pingPool == null)
        {
            _pingPool =
                Object.Instantiate(
                    GameManagerCreator
                        .Instance
                        .HideAndSeekManagerPrefab
                        .PingPool,
                    localPlayer.transform);
        }

        if (_hidePingsAt > 0f &&
            Time.time >= _hidePingsAt)
        {
            HideActivePings();
            _hidePingsAt = 0f;
        }

        if (Time.time < _nextPingAt)
        {
            return;
        }

        HideActivePings();

        var survivors =
            InfectionManager.GetSurvivors();

        for (var i = 0;
             i < survivors.Count;
             i++)
        {
            var survivor =
                survivors[i];

            if (survivor == null ||
                survivor.Data == null ||
                survivor.Data.IsDead ||
                survivor.Data.Disconnected)
            {
                continue;
            }

            var ping =
                _pingPool.Get<PingBehaviour>();

            ping.target =
                survivor.GetTruePosition();

            ping.AmSeeker = true;
            ping.UpdatePosition();
            ping.gameObject.SetActive(true);
            ping.SetImageEnabled(true);
        }

        // Los pings permanecen visibles solo unos segundos,
        // igual que en Hide and Seek.
        _hidePingsAt =
            Time.time + 2f;

        _nextPingAt =
            Time.time +
            OptionGroupSingleton<HnsFinalHideOptions>
                .Instance
                .PingInterval
                .Value;
    }

    /// <summary>
    /// Oculta todos los pings activos creados durante Adrenalina.
    /// </summary>
    private static void HideActivePings()
    {
        if (_pingPool == null)
        {
            return;
        }

        foreach (var pooled in
                 _pingPool.activeChildren)
        {
            var ping =
                pooled.TryCast<PingBehaviour>();

            if (ping == null)
            {
                continue;
            }

            ping.target =
                Vector3.zero;

            ping.SetImageEnabled(false);
            ping.gameObject.SetActive(false);
        }
    }
}