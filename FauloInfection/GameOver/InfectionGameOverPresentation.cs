using FauloInfection.GameModes;
using MiraAPI.Translation;
using MiraAPI.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FauloInfection.GameOver;

/// <summary>
/// Aplica la presentación visual específica de Infection
/// sobre la pantalla final creada por Among Us.
/// </summary>
internal static class InfectionGameOverPresentation
{
    public static void ApplyInfectedVictory(
        EndGameManager endGameManager)
    {
        Apply(
            endGameManager,
            MiraLocaleManager.Get(
                "FauloInfection.GameOver.Infected.Win",
                "Infected win!"),
            InfectionMode.InfectionColor,
            InfectionMode.GetInfectedBodyType(),
            hideBodyCosmetics: true);
    }

    public static void ApplySurvivorVictory(
        EndGameManager endGameManager)
    {
        Apply(
            endGameManager,
            MiraLocaleManager.Get(
                "FauloInfection.GameOver.Survivors.Win",
                "Survivors win!"),
            Palette.CrewmateBlue,
            InfectionMode.GetSurvivorBodyType(),
            hideBodyCosmetics: false);
    }

    private static void Apply(
        EndGameManager endGameManager,
        string winText,
        Color color,
        PlayerBodyTypes bodyType,
        bool hideBodyCosmetics)
    {
        endGameManager.WinText.text = winText;
        endGameManager.WinText.color = color;

        endGameManager.BackgroundBar.material.SetColor(
            ShaderID.Color,
            color);

        var winners =
            EndGameResult.CachedWinners.ToArray();

        foreach (var player in
                 Object.FindObjectsOfType<PoolablePlayer>())
        {
            // EndGameManager también contiene PoolablePlayer usados por
            // elementos de navegación y progresión. Solo modificamos los
            // modelos de los ganadores colocados directamente en la escena final.
            if (player == null ||
                player.transform.parent != endGameManager.transform)
            {
                continue;
            }

            var winner = winners.FirstOrDefault(
                data =>
                    data.PlayerName ==
                    player.cosmetics.nameText.text);

            // Primero cambiamos el tipo de cuerpo.
            player.SetBodyType(bodyType);
            player.SetFlipX(false);

            if (hideBodyCosmetics)
            {
                // El cuerpo Seeker utiliza anclajes distintos.
                // Hide and Seek normalmente oculta estos cosméticos
                // para evitar colocarlos sobre puntos incorrectos.
                player.SetBodyCosmeticsVisible(false);
            }

            // Among Us creó inicialmente este modelo como un Crewmate
            // normal y ya había colocado sus cosméticos. Reaplicamos
            // el atuendo después de cambiar el cuerpo para reconstruir
            // correctamente los anclajes del nuevo modelo.
            if (winner != null)
            {
                player.UpdateFromPlayerOutfit(
                    winner.Outfit,
                    PlayerMaterial.MaskType.None,
                    false,
                    true);

                player.SetFlipX(false);

                if (hideBodyCosmetics)
                {
                    player.SetBodyCosmeticsVisible(false);
                }
            }
        }
    }
}