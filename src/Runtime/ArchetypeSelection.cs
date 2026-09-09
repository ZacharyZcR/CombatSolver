using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace CombatSolver;

internal static class ArchetypeSelection
{
    internal static Player? CurrentPlayer()
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        return Entry.Enabled && run?.Players.Count == 1 ? LocalContext.GetMe(run) : null;
    }

    internal static string Character => CurrentPlayer()?.Character.Id.Entry ?? string.Empty;
    internal static ArchetypePreference Current => SolverSettings.Current.Archetypes.For(Character);

    internal static void Select(ArchetypePreference preference) => SolverController.SetArchetypePreference(Character, preference);
}
