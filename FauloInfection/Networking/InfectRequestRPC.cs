using FauloInfection.Infection;
using Hazel;
using InnerNet;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace FauloInfection.Networking;

/// <summary>
/// Client-to-host request used when an infected player tries to infect a target.
/// Solicitud del cliente al host usada cuando un jugador infectado intenta infectar a un objetivo.
/// </summary>
[RegisterCustomRpc((uint)InfectionRpc.InfectRequest)]
public sealed class InfectRequestRpc(
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

    public override byte Read(MessageReader reader)
    {
        return reader.ReadByte();
    }

    public override void Handle(
        PlayerControl sender,
        byte targetPlayerId)
    {
        // Only the host is allowed to process infection requests.
        // Solo el host puede procesar solicitudes de infección.
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            AmongUsClient.Instance.GameState !=
            InnerNetClient.GameStates.Started)
        {
            return;
        }

        // Reject requests from invalid, disconnected or dead senders.
        // Rechaza solicitudes de jugadores inválidos, desconectados o muertos.
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

        // The host performs the real validation.
        // El host realiza la validación real.
        InfectionManager.TryInfect(
            sender,
            target);
    }
}