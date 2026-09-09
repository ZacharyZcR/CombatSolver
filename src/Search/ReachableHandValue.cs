namespace CombatSolver;

internal static class ReachableHandValue
{
    internal static int Calculate(
        ReadOnlySpan<(int Energy, int Stars, int Value)> cards,
        int energyCapacity,
        int starCapacity)
    {
        int totalEnergy = 0;
        int totalStars = 0;
        long totalValue = 0;
        foreach (var card in cards)
        {
            totalEnergy += card.Energy;
            totalStars += card.Stars;
            totalValue += card.Value;
        }
        energyCapacity = Math.Min(Math.Max(0, energyCapacity), totalEnergy);
        starCapacity = Math.Min(Math.Max(0, starCapacity), totalStars);
        if (energyCapacity == totalEnergy && starCapacity == totalStars
            && energyCapacity >= 0 && starCapacity >= 0 && totalValue <= int.MaxValue)
            return (int)totalValue;

        bool energyUnconstrained = energyCapacity == totalEnergy && totalValue <= int.MaxValue;
        bool starsUnconstrained = starCapacity == totalStars && totalValue <= int.MaxValue;
        if (energyUnconstrained) energyCapacity = 0;
        if (starsUnconstrained) starCapacity = 0;

        int width = checked(starCapacity + 1);
        int length = checked((energyCapacity + 1) * width);
        Span<int> best = length <= 256 ? stackalloc int[length] : new int[length];
        best.Clear();
        foreach (var card in cards)
        {
            int energyCost = energyUnconstrained ? 0 : card.Energy;
            int starCost = starsUnconstrained ? 0 : card.Stars;
            for (int energy = energyCapacity; energy >= energyCost; energy--)
            for (int stars = starCapacity; stars >= starCost; stars--)
            {
                int index = energy * width + stars;
                best[index] = Math.Max(best[index],
                    best[(energy - energyCost) * width + stars - starCost] + card.Value);
            }
        }
        return best[^1];
    }
}
