using FauloInfection.Infection;
using MiraAPI.GameEnd;

namespace FauloInfection.GameOver;

/// <summary>
/// Representa una victoria del equipo superviviente.
/// </summary>
public sealed class SurvivorVictoryGameOver : CustomGameOver
{
    public override bool VerifyCondition(
        PlayerControl playerControl,
        NetworkedPlayerInfo[] winners)
    {
        return InfectionManager.IsActive &&
               winners.Length > 0 &&
               winners.All(winner =>
                   winner != null &&
                   winner.Object != null &&
                   !InfectionManager.IsInfected(
                       winner.Object));
    }
    
    public override void AfterEndGameSetup(
        EndGameManager endGameManager)
    {
        InfectionGameOverPresentation
            .ApplySurvivorVictory(endGameManager);
    }
}