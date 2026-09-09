namespace CombatSolver;

// One free slot has diminishing inventory value: discount only one ordinary potion.
internal readonly record struct PotionOpportunityPolicy(int Hp, int MaxHp, bool CanRefill, bool Boss)
{
    internal int FirstOrdinaryCost(int ordinaryCost, bool full)
    {
        int cost = full ? Math.Max(1, ordinaryCost / 3) : ordinaryCost;
        if (MaxHp <= 0) return cost;
        if (Hp <= MaxHp * 0.3) cost -= Math.Max(1, ordinaryCost / 3);
        if (CanRefill) cost -= Math.Max(1, ordinaryCost / 4);
        if (Boss) cost--;
        else if (!full && !CanRefill && Hp >= MaxHp * 0.7) cost += Math.Max(1, ordinaryCost / 4);
        return Math.Max(1, cost);
    }
}

internal static class PotionInventoryValue
{
    internal static int PaidCapacity(int hpBudget, int regularCost, int firstCost)
    {
        if (regularCost <= 0 || firstCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(regularCost), "Paid potion costs must be positive.");
        // Boss policies use a prohibitive sentinel when HP cannot retain value.
        if (regularCost >= int.MaxValue / 4 || firstCost >= int.MaxValue / 4 || hpBudget < firstCost)
            return 0;
        return 1 + (hpBudget - firstCost) / regularCost;
    }

    internal static int RequiredCost(int cost, bool full, bool ordinaryPotionUsed, int ordinaryCost,
        PotionOpportunityPolicy opportunity = default)
        => ordinaryPotionUsed
            ? Math.Max(0, cost - ordinaryCost + opportunity.FirstOrdinaryCost(ordinaryCost, full))
            : cost;
}
