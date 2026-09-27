using MiraAPI.Utilities.Assets;

namespace FauloInfection.Assets;

public static class InfectionAssets
{
    // Botón de Infectar
    public static LoadableResourceAsset InfectButton { get; } =
        new("FauloInfection.Resources.Buttons.Infect.png");
}