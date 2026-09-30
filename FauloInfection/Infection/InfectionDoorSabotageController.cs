using FauloInfection.Options;
using MiraAPI.GameOptions;

namespace FauloInfection.Infection;

/// <summary>
/// Centraliza las reglas de sabotaje de puertas de Infection.
///
/// Infection solo permite sabotear puertas a miembros del equipo
/// infectado y únicamente cuando la opción del host está activada.
/// </summary>
public static class InfectionDoorSabotageController
{
    // TOU:Mira utiliza estos IDs para sus sistemas de puertas
    // alternativos. Los aceptamos sin tomar una dependencia directa
    // de TOU para conservar la compatibilidad opcional.
    private const byte TouSkeldDoorsSystemId = 151;
    private const byte TouManualDoorsSystemId = 152;

    /// <summary>
    /// Devuelve si el jugador local puede sabotear puertas ahora mismo.
    /// </summary>
    public static bool CanLocalPlayerSabotageDoors()
    {
        return CanPlayerSabotageDoors(
            PlayerControl.LocalPlayer);
    }

    /// <summary>
    /// Comprueba el permiso real de Infection para sabotear puertas.
    ///
    /// Esto no depende del rol vanilla. Por tanto, funciona tanto
    /// para el Seeker inicial como para infectados convertidos.
    /// </summary>
    public static bool CanPlayerSabotageDoors(
        PlayerControl? player)
    {
        if (!InfectionManager.IsActive ||
            player == null ||
            player.Data == null ||
            player.Data.Role == null ||
            player.Data.IsDead ||
            player.Data.Disconnected)
        {
            return false;
        }

        return InfectionManager.IsInfected(player) &&
               OptionGroupSingleton<InfectionOptions>
                   .Instance
                   .InfectedCanSabotageDoors
                   .Value;
    }

    /// <summary>
    /// Reconoce los sistemas de puertas vanilla y los dos sistemas
    /// de puertas opcionales utilizados actualmente por TOU:Mira.
    ///
    /// Se usa únicamente para impedir que un infectado envíe
    /// RpcUpdateSystem para sabotajes que no sean puertas.
    /// </summary>
    public static bool IsDoorSystem(
        SystemTypes systemType)
    {
        var systemId =
            (byte)systemType;

        return systemType == SystemTypes.Doors ||
               systemId == TouSkeldDoorsSystemId ||
               systemId == TouManualDoorsSystemId;
    }
}
