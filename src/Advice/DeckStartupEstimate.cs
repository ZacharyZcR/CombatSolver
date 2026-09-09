namespace CombatSolver;

internal readonly record struct DeckStartupEstimate(double Probability, double EnergyFit);

internal static class DeckStartupModel
{
    // Without-replacement draw probability, not a claim about known shuffle order.
    internal static double Miss(int cards, int successes, int draws)
    {
        draws = Math.Clamp(draws, 0, cards);
        double result = 1;
        for (int i = 0; i < draws; i++) result *= Math.Max(0, cards - successes - i) / (double)(cards - i);
        return result;
    }

    internal static double Together(int cards, int sources, int payoffs, int overlap, int draws)
        => Math.Clamp(1 - Miss(cards, sources, draws) - Miss(cards, payoffs, draws)
            + Miss(cards, sources + payoffs - overlap, draws), 0, 1);

    internal static DeckStartupEstimate Capture(AdviceContext context, ArchetypePreference preference)
    {
        AdviceRole sourceMask = ArchetypeCatalog.Sources(preference);
        AdviceRole payoffMask = ArchetypeCatalog.Roles(preference) & ~sourceMask;
        int sources = 0, payoffs = 0, overlap = 0;
        double sourceCost = 0, payoffCost = 0, timing = 0, starCost = 0;
        foreach (var card in context.Deck)
        {
            if (!card.Known || card.Curse) continue;
            bool source = preference == ArchetypePreference.Stars ? card.Stars > 0
                : (card.Roles & sourceMask) != 0;
            source &= card.Availability > 0 && card.SourceAmount > 0;
            bool payoff = preference == ArchetypePreference.Stars ? card.StarCost > 0 || card.StarsX
                : (card.Roles & payoffMask) != 0;
            if (source) { sources++; sourceCost += Math.Max(0, card.Cost); timing += Math.Clamp(card.Availability, 0, 1); }
            if (payoff) { payoffs++; payoffCost += card.EnergyX ? 2 : Math.Max(0, card.Cost); starCost += card.StarCost; }
            if (source && payoff) overlap++;
        }
        int draws = Math.Min(context.Deck.Count, 10 + (int)Math.Min(4, context.GetStatistics().Draw));
        bool startingSource = preference switch
        {
            ArchetypePreference.Stars => context.StartingStars > 0,
            ArchetypePreference.Summon => context.SummonSupply > 0,
            ArchetypePreference.FocusOrbs => context.InitialFocusOrbs > 0,
            _ => false,
        };
        double meanSourceCost = startingSource ? 0 : sourceCost / Math.Max(1, sources);
        double meanPayoffCost = payoffCost / Math.Max(1, payoffs);
        bool generatedPayoff = preference == ArchetypePreference.Forge;
        if (generatedPayoff) meanPayoffCost = 2; // Forge creates its own Blade.
        double sharedCost = overlap / (double)Math.Max(1, Math.Min(sources, payoffs)) * Math.Min(meanSourceCost, meanPayoffCost);
        double energyFit = Math.Min(1, Math.Max(0, context.BaseEnergy) / Math.Max(1, meanSourceCost + meanPayoffCost - sharedCost));
        double probability = startingSource ? 1 - Miss(context.Deck.Count, payoffs, draws)
            : generatedPayoff ? 1 - Miss(context.Deck.Count, sources, draws)
            : Together(context.Deck.Count, sources, payoffs, overlap, draws);
        double availability = startingSource ? 1 : timing / Math.Max(1, sources);
        if (preference == ArchetypePreference.Stars && payoffs > 0)
            availability *= Math.Min(1, (context.StartingStars + context.GetStatistics().Stars * draws / Math.Max(1, context.Deck.Count))
                / Math.Max(1, starCost / payoffs));
        return new(probability * availability * energyFit, energyFit);
    }
}
