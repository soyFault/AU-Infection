namespace FauloInfection.Networking;

internal enum InfectionRpc : uint
{
    InfectRequest = 1,
    KillRequest = 2,
    PlayTransform = 3,
    TaskComplete = 4,
}