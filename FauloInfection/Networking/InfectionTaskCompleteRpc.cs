using FauloInfection.Infection;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace FauloInfection.Networking;

/// <summary>
/// Datos necesarios para reproducir en todos los clientes
/// el efecto de una tarea completada.
/// </summary>
public readonly record struct InfectionTaskCompleteData(
    uint TaskId,
    byte TaskLength);

/// <summary>
/// Sincroniza una tarea completada para que todas las instancias
/// apliquen exactamente la misma deducción al temporizador.
/// </summary>
[RegisterCustomRpc((uint)InfectionRpc.TaskComplete)]
public sealed class InfectionTaskCompleteRpc(
    InfectionPlugin plugin,
    uint id)
    : PlayerCustomRpc<
        InfectionPlugin,
        InfectionTaskCompleteData>(
        plugin,
        id)
{
    public override RpcLocalHandling LocalHandling =>
        RpcLocalHandling.Before;

    public override void Write(
        MessageWriter writer,
        InfectionTaskCompleteData data)
    {
        writer.Write(data.TaskId);
        writer.Write(data.TaskLength);
    }

    public override InfectionTaskCompleteData Read(
        MessageReader reader)
    {
        return new InfectionTaskCompleteData(
            reader.ReadUInt32(),
            reader.ReadByte());
    }

    public override void Handle(
        PlayerControl sender,
        InfectionTaskCompleteData data)
    {
        if (sender == null ||
            sender.Data == null ||
            sender.Data.Disconnected)
        {
            return;
        }

        InfectionTaskEvents.HandleCompletedTask(
            sender,
            data.TaskId,
            (NormalPlayerTask.TaskLength)
            data.TaskLength);
    }
}