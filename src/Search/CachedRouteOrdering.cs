namespace CombatSolver;

internal static class CachedRouteOrdering
{
    internal static bool IsBetter(SolverInterimResult candidate, SolverInterimResult current, bool preserveTheft)
    {
        if (!candidate.Won || candidate.Objective.Policy != current.Objective.Policy) return false;
        int quality = SolverInterimResultOrdering.ComparePrimaryQuality(candidate.Won,
            candidate.StrategicHpDeficit, candidate.CombatEndedTurn, current.Won,
            current.StrategicHpDeficit, current.CombatEndedTurn, candidate.GrowthHpCredit,
            current.GrowthHpCredit, candidate.GrowthRewardCount, current.GrowthRewardCount,
            candidate.Objective, current.Objective);
        if (quality != 0) return quality < 0;
        if (preserveTheft && candidate.OutstandingStolenResource != current.OutstandingStolenResource)
            return candidate.OutstandingStolenResource < current.OutstandingStolenResource;
        if (candidate.PotionStrategicCost != current.PotionStrategicCost)
            return candidate.PotionStrategicCost < current.PotionStrategicCost;
        return candidate.ProjectedBattlePotionCount < current.ProjectedBattlePotionCount;
    }
}
