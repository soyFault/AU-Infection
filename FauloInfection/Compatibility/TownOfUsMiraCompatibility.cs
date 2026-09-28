using System;
using System.Collections;
using System.Linq;
using FauloInfection.Infection;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using Reactor.Utilities;
using UnityEngine;

namespace FauloInfection.Compatibility;

public static class TownOfUsMiraCompatibility
{
    private const string TownOfUsAssemblyName =
        "TownOfUsMira";

    private static bool initialized;
    private static bool townOfUsDetected;

    private static bool infectionIntroActive;
    private static IntroCutscene? activeIntro;

    private static PlayerControl? deferredSeeker;
    private static AnimationClip? deferredSeekerAnimation;

    public static void Initialize(
        Harmony harmony)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        var townOfUsAssembly =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly =>
                    assembly.GetName().Name ==
                    TownOfUsAssemblyName);

        if (townOfUsAssembly == null)
        {
            Logger<InfectionPlugin>.Info(
                "TOU:Mira no detectado. " +
                "La compatibilidad con TOU queda inactiva.");

            return;
        }

        townOfUsDetected = true;

        var animateCustom =
            AccessTools.Method(
                typeof(PlayerControl),
                "AnimateCustom",
                new[]
                {
                    typeof(AnimationClip)
                });

        if (animateCustom == null)
        {
            Logger<InfectionPlugin>.Info(
                "TOU:Mira detectado, pero no se encontró " +
                "PlayerControl.AnimateCustom(AnimationClip).");

            return;
        }

        var animateCustomPrefix =
            AccessTools.Method(
                typeof(TownOfUsMiraCompatibility),
                nameof(AnimateCustomPrefix));

        if (animateCustomPrefix == null)
        {
            Logger<InfectionPlugin>.Info(
                "No se pudo localizar el guard de compatibilidad " +
                "para PlayerControl.AnimateCustom.");

            return;
        }

        harmony.Patch(
            animateCustom,
            prefix: new HarmonyMethod(
                animateCustomPrefix));

        Logger<InfectionPlugin>.Info(
            "Compatibilidad con TOU:Mira instalada: " +
            "transformación HnS del Seeker diferida.");
    }

    [RegisterEvent]
    public static void IntroBeginEventHandler(
        IntroBeginEvent @event)
    {
        if (!townOfUsDetected ||
            !InfectionManager.IsActive)
        {
            return;
        }

        infectionIntroActive = true;
        activeIntro = @event.IntroCutscene;

        deferredSeeker = null;
        deferredSeekerAnimation = null;
    }

    [RegisterEvent]
    public static void IntroEndEventHandler(
        IntroEndEvent @event)
    {
        if (!townOfUsDetected)
        {
            return;
        }

        infectionIntroActive = false;
        activeIntro = null;

        if (!InfectionManager.IsActive ||
            deferredSeeker == null ||
            deferredSeekerAnimation == null)
        {
            deferredSeeker = null;
            deferredSeekerAnimation = null;
            return;
        }

        var seeker = deferredSeeker;
        var animation = deferredSeekerAnimation;

        deferredSeeker = null;
        deferredSeekerAnimation = null;

        Coroutines.Start(
            CoReplaySeekerTransform(
                seeker,
                animation));
    }

    private static bool AnimateCustomPrefix(
        PlayerControl __instance,
        AnimationClip __0)
    {
        if (!townOfUsDetected ||
            !infectionIntroActive ||
            !InfectionManager.IsActive ||
            activeIntro == null)
        {
            return true;
        }

        var localPlayer =
            PlayerControl.LocalPlayer;

        if (localPlayer == null ||
            localPlayer.Data?.Role == null ||
            localPlayer.Data.Role.IsImpostor)
        {
            return true;
        }

        if (__instance == null ||
            __instance.Data?.Role?.IsImpostor != true)
        {
            return true;
        }

        var isSeekerIntroAnimation =
            __0 == activeIntro.HnSSeekerSpawnAnim ||
            __0 == activeIntro.HnSSeekerSpawnHorseInGameAnim ||
            __0 == activeIntro.HnSSeekerSpawnLongInGameAnim;

        if (!isSeekerIntroAnimation)
        {
            return true;
        }

        deferredSeeker = __instance;
        deferredSeekerAnimation = __0;

        Logger<InfectionPlugin>.Info(
            $"TOU compat: transformación mundial del Seeker diferida. " +
            $"Player ID: {__instance.PlayerId}, " +
            $"clip: {__0.name}.");

        return false;
    }

    private static IEnumerator CoReplaySeekerTransform(
        PlayerControl seeker,
        AnimationClip animation)
    {
        yield return null;

        if (!InfectionManager.IsActive ||
            seeker == null ||
            seeker.Data == null ||
            seeker.Data.Disconnected)
        {
            yield break;
        }

        Logger<InfectionPlugin>.Info(
            $"TOU compat: reproduciendo transformación mundial " +
            $"del Seeker después de la intro. " +
            $"Player ID: {seeker.PlayerId}.");
        
        // La reproducción original de MiraAPI ocultaba los cosméticos
        // al iniciar la transformación. Como ahora diferimos la animación,
        // debemos ocultarlos otra vez en el momento real de reproducción.
        //
        // AnimateCustom los vuelve a mostrar automáticamente al terminar.
        seeker.cosmetics.SetBodyCosmeticsVisible(false);

        seeker.AnimateCustom(animation);
    }
}