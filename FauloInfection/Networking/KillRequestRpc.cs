using FauloInfection.Infection;
using Hazel;
using InnerNet;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace FauloInfection.Networking;

/// <summary>
/// Client-to-host request for an infected player's kill attempt.
/// Solicitud del cliente al host para un intento de asesinato de un infectado.
/// </summary>
[RegisterCustomRpc((uint)InfectionRpc.KillRequest)]
public sealed class KillRequestRpc(
    InfectionPlugin plugin,
    uint id)
    : PlayerCustomRpc<InfectionPlugin, byte>(plugin, id)
{
    public override RpcLocalHandling LocalHandling =>
        RpcLocalHandling.Before;

    public override void Write(
        MessageWriter writer,
        byte targetPlayerId)
    {
        writer.Write(targetPlayerId);
    }

    public override byte Read(
        MessageReader reader)
    {
        return reader.ReadByte();
    }

    public override void Handle(
        PlayerControl sender,
        byte targetPlayerId)
    {
        // Only the host may validate and execute the kill.
        // Solo el host puede validar y ejecutar el asesinato.
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            AmongUsClient.Instance.GameState !=
            InnerNetClient.GameStates.Started)
        {
            return;
        }

        // Reject invalid, disconnected or dead senders.
        // Rechaza emisores inválidos, desconectados o muertos.
        if (sender == null ||
            sender.Data == null ||
            sender.Data.Disconnected ||
            sender.Data.IsDead)
        {
            return;
        }

        var target =
            GameData.Instance?
                .GetPlayerById(targetPlayerId)?
                .Object;

        if (target == null)
        {
            return;
        }

        // The host performs the real gameplay validation.
        // El host realiza la validación real de jugabilidad.
        InfectionManager.TryKill(
            sender,
            target);
    }
}