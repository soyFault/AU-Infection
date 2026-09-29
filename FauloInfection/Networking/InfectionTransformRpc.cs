using FauloInfection.Infection;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace FauloInfection.Networking;

/// <summary>
/// RPC de presentación para la transformación de un jugador
/// que acaba de ser convertido.
///
/// No modifica el estado de infección.
/// InfectedModifier sigue siendo la fuente de verdad.
/// </summary>
[RegisterCustomRpc((uint)InfectionRpc.PlayTransform)]
public sealed class InfectionTransformRpc(
    InfectionPlugin plugin,
    uint id)
    : PlayerCustomRpc<InfectionPlugin, byte>(
        plugin,
        id)
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
        if (!InfectionManager.IsActive)
        {
            return;
        }

        var target =
            GameData.Instance?
                .GetPlayerById(targetPlayerId)?
                .Object;

        if (target == null ||
            target.Data == null ||
            target.Data.Disconnected ||
            target.Data.IsDead)
        {
            return;
        }

        InfectionVisuals.ApplyInfected(
            target);
        
        // La transformación y su notificación son presentación local.
        // NotifyInfection decide internamente si la opción está activada.
        InfectionHudController.NotifyInfection(
            target);
    }
}