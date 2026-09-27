using Reactor.Utilities;

namespace FauloInfection.Patches;

public static class VersionDisplay
{
    public static void Register()
    {
        ReactorCredits.Register<InfectionPlugin>(ReactorCredits.AlwaysShow);
    }
}