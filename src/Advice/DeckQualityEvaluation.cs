namespace CombatSolver;

internal readonly record struct DeckQualityEvaluation(double Output, double Defense, double Consistency, double Startup)
{
    internal double Total => Output + Defense + Consistency + Startup;
    internal static DeckQualityEvaluation Capture(AdviceContext context)
    {
        context = context.Prepare();
        var stats = context.GetStatistics();
        int size = Math.Max(1, context.Deck.Count);
        double seen = Math.Min(size, 10 + Math.Min(4, stats.Draw));
        double access = seen / size;
        double playable = Math.Min(1, Math.Max(0, context.BaseEnergy * 2) / Math.Max(1, stats.Cost * access));
        double defenseNeed = context.Hp * 2 < context.MaxHp ? 1 : 0.7;
        double expectedStars = context.StartingStars + stats.Stars * access;
        double starDemand = context.Deck.Sum(card => Math.Max(0, card.StarCost)) * access;
        double starFit = starDemand > 0 ? Math.Min(1, expectedStars / starDemand) : 1;
        bool summonAvailable = AdviceMechanics.SourceSupply(context, AdviceRole.SummonSource) > 0;
        double usableDamage = 0, usableBlock = 0;
        foreach (var card in context.Deck)
        {
            double fit = card.StarCost > 0 ? starFit : card.StarsX && expectedStars <= 0 ? 0 : 1;
            if (card.Roles.HasFlag(AdviceRole.OstyAttack) && !summonAvailable) fit = 0;
            usableDamage += Math.Max(0, card.Damage) * fit;
            usableBlock += Math.Max(0, card.Block) * fit;
        }
        double startup = 0;
        foreach (var preference in Enum.GetValues<ArchetypePreference>())
        {
            if (preference == ArchetypePreference.Automatic) continue;
            double weight = context.Preference == preference ? 18 : 8;
            startup += weight * DeckStartupModel.Capture(context, preference).Probability;
        }
        return new(usableDamage * access * playable * 0.45,
            usableBlock * access * playable * defenseNeed,
            6 * Math.Min(1, stats.Draw / Math.Max(1, size * 0.15)) - 4 * stats.Expensive / (double)size,
            startup);
    }

    internal static double Addition(AdviceContext context, AdviceCard card, DeckQualityEvaluation baseline)
        => Math.Clamp(2 * (Capture(context with { Deck = context.Deck.Append(card).ToArray() }).Total - baseline.Total), -18, 18);
}
