namespace CombatSolver;

// Owned by one UI evaluation batch. No live cards or process-wide cache.
internal sealed class AdviceDeckStatistics
{
    internal IReadOnlyList<AdviceCard> Deck { get; }
    private readonly Dictionary<string, int> _copies = new(StringComparer.Ordinal);
    private readonly Dictionary<AdviceRole, (int Count, double Supply, double Payoff)> _roles = [];
    private readonly int[] _tags = new int[10];
    internal int Attacks { get; }
    internal int DamageCards { get; }
    internal int Blocks { get; }
    internal int Expensive { get; }
    internal int CheapAttacks { get; }
    internal double Damage { get; }
    internal double Block { get; }
    internal double Cost { get; }
    internal double Draw { get; }
    internal double Stars { get; }
    internal int StarUsers { get; }
    internal double IndirectDraw { get; }
    internal int Reviewed { get; }

    internal AdviceDeckStatistics(IReadOnlyList<AdviceCard> deck)
    {
        Deck = deck;
        foreach (var card in deck)
        {
            _copies[card.Id] = Copies(card.Id) + 1;
            if (card.Attack) Attacks++;
            if (card.Attack || card.Damage > 0) DamageCards++;
            if (card.Block > 0) Blocks++;
            if (!card.EnergyX && card.Cost >= 2) Expensive++;
            if (card.Attack && !card.EnergyX && card.Cost <= 1) CheapAttacks++;
            Damage += Math.Max(0, card.Damage);
            Block += Math.Max(0, card.Block);
            Cost += card.EnergyX ? 2 : Math.Max(0, card.Cost);
            Draw += AdviceMechanics.DrawSupply(card);
            IndirectDraw += AdviceMechanics.IndirectDrawSupply(card);
            Stars += AdviceMechanics.StarSupply(card);
            if (card.StarCost > 0 || card.StarsX) StarUsers++;
            if (card.Coverage == AdviceCoverage.Partial) Reviewed++;
            for (int bit = 0; bit < _tags.Length; bit++)
                if (((int)card.Tags & (1 << bit)) != 0) _tags[bit]++;
            var group = _roles.GetValueOrDefault(card.Roles);
            _roles[card.Roles] = (group.Count + 1, group.Supply + AdviceMechanics.SourceWeight(card),
                group.Payoff + Math.Clamp(card.PayoffWeight, 0, 4));
        }
    }

    internal int Copies(string id) => _copies.GetValueOrDefault(id);
    internal int Tagged(AdviceTag tag) => _tags[System.Numerics.BitOperations.TrailingZeroCount((uint)tag)];
    internal double Supply(AdviceRole mask)
    {
        double sum = 0;
        foreach (var (roles, group) in _roles) if ((roles & mask) != 0) sum += group.Supply;
        return sum;
    }
    internal double Payoffs(AdviceRole mask)
    {
        double sum = 0;
        foreach (var (roles, group) in _roles) if ((roles & mask) != 0) sum += group.Payoff;
        return sum;
    }
}
