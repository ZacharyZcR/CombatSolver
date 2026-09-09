namespace CombatSolver;

internal enum ArchetypePreference
{
    Automatic, Strength, Exhaust, Poison, Shiv, Discard, Stars, Forge, Doom, Soul, Summon, FocusOrbs,
}

internal sealed record ArchetypePreferences(
    ArchetypePreference Ironclad = ArchetypePreference.Automatic,
    ArchetypePreference Silent = ArchetypePreference.Automatic,
    ArchetypePreference Regent = ArchetypePreference.Automatic,
    ArchetypePreference Necrobinder = ArchetypePreference.Automatic,
    ArchetypePreference Defect = ArchetypePreference.Automatic)
{
    internal ArchetypePreference For(string character) => ArchetypeCatalog.Normalize(character, character switch
    {
        "IRONCLAD" => Ironclad, "SILENT" => Silent, "REGENT" => Regent,
        "NECROBINDER" => Necrobinder, "DEFECT" => Defect, _ => ArchetypePreference.Automatic,
    });

    internal ArchetypePreferences With(string character, ArchetypePreference value)
    {
        if (ArchetypeCatalog.Normalize(character, value) != value)
            throw new ArgumentOutOfRangeException(nameof(value));
        return character switch
        {
            "IRONCLAD" => this with { Ironclad = value }, "SILENT" => this with { Silent = value },
            "REGENT" => this with { Regent = value }, "NECROBINDER" => this with { Necrobinder = value },
            "DEFECT" => this with { Defect = value }, _ => this,
        };
    }
}

internal static class ArchetypeCatalog
{
    internal static ArchetypePreference Normalize(string character, ArchetypePreference preference) =>
        Supports(character, preference) ? preference : ArchetypePreference.Automatic;

    internal static bool Supports(string character, ArchetypePreference preference) =>
        preference == ArchetypePreference.Automatic || character switch
        {
            "IRONCLAD" => preference is ArchetypePreference.Strength or ArchetypePreference.Exhaust,
            "SILENT" => preference is ArchetypePreference.Poison or ArchetypePreference.Shiv or ArchetypePreference.Discard,
            "REGENT" => preference is ArchetypePreference.Stars or ArchetypePreference.Forge,
            "NECROBINDER" => preference is ArchetypePreference.Doom or ArchetypePreference.Soul or ArchetypePreference.Summon,
            "DEFECT" => preference == ArchetypePreference.FocusOrbs,
            _ => false,
        };

    internal static string Name(ArchetypePreference preference) => preference switch
    {
        ArchetypePreference.Strength => "力量多段配合", ArchetypePreference.Exhaust => "消耗配合",
        ArchetypePreference.Poison => "毒的施加与增幅", ArchetypePreference.Shiv => "小刀配合",
        ArchetypePreference.Discard => "弃牌配合", ArchetypePreference.Stars => "星星供需",
        ArchetypePreference.Forge => "锻造兑现", ArchetypePreference.Doom => "灾厄叠加与结算",
        ArchetypePreference.Soul => "灵魂配合", ArchetypePreference.Summon => "召唤攻击配合",
        ArchetypePreference.FocusOrbs => "集中产球配合", _ => "自动识别（不指定流派）",
    };

    internal static AdviceRole Roles(ArchetypePreference preference) => preference switch
    {
        ArchetypePreference.Strength => AdviceRole.StrengthSource | AdviceRole.StrengthPayoff,
        ArchetypePreference.Exhaust => AdviceRole.ExhaustSource | AdviceRole.ExhaustPayoff | AdviceRole.SelfExhaust,
        ArchetypePreference.Poison => AdviceRole.PoisonSource | AdviceRole.PoisonPayoff,
        ArchetypePreference.Shiv => AdviceRole.ShivSource | AdviceRole.ShivPayoff,
        ArchetypePreference.Discard => AdviceRole.DiscardSource | AdviceRole.DiscardPayoff,
        ArchetypePreference.Forge => AdviceRole.ForgeSource | AdviceRole.Blade,
        ArchetypePreference.Doom => AdviceRole.DoomSource | AdviceRole.DoomPayoff,
        ArchetypePreference.Soul => AdviceRole.SoulSource | AdviceRole.SoulPlayPayoff | AdviceRole.SoulExhaustPayoff,
        ArchetypePreference.Summon => AdviceRole.SummonSource | AdviceRole.OstyAttack,
        ArchetypePreference.FocusOrbs => AdviceRole.FocusSource | AdviceRole.FocusOrbSource,
        _ => AdviceRole.None,
    };

    internal static bool Matches(ArchetypePreference preference, AdviceCard card) => card.Known && !card.Curse
        && !card.Basic && ((Roles(preference) & card.Roles) != 0
            || preference == ArchetypePreference.Stars && (card.Stars > 0 || card.StarCost > 0 || card.StarsX));

    internal static AdviceRole Sources(ArchetypePreference preference) => preference switch
    {
        ArchetypePreference.Strength => AdviceRole.StrengthSource,
        ArchetypePreference.Exhaust => AdviceRole.ExhaustSource | AdviceRole.SelfExhaust,
        ArchetypePreference.Poison => AdviceRole.PoisonSource,
        ArchetypePreference.Shiv => AdviceRole.ShivSource,
        ArchetypePreference.Discard => AdviceRole.DiscardSource,
        ArchetypePreference.Forge => AdviceRole.ForgeSource,
        ArchetypePreference.Doom => AdviceRole.DoomSource,
        ArchetypePreference.Soul => AdviceRole.SoulSource,
        ArchetypePreference.Summon => AdviceRole.SummonSource,
        ArchetypePreference.FocusOrbs => AdviceRole.FocusOrbSource,
        _ => AdviceRole.None,
    };

    internal static double AdviceBonus(AdviceContext context, AdviceCard card)
    {
        if (!Matches(context.Preference, card)) return 0;
        AdviceRole sources = Sources(context.Preference);
        AdviceRole payoffs = Roles(context.Preference) & ~sources;
        bool stars = context.Preference == ArchetypePreference.Stars;
        MechanismBalance current = stars ? DeckMechanismProfile.Stars(context)
            : DeckMechanismProfile.Capture(context, sources, payoffs);
        double supply = stars ? AdviceMechanics.StarSupply(card)
            : (card.Roles & sources) != 0 ? AdviceMechanics.SourceWeight(card) : 0;
        double payoff = stars ? card.StarCost > 0 || card.StarsX ? 1 : 0
            : (card.Roles & payoffs) != 0 ? Math.Clamp(card.PayoffWeight, 0, 4) : 0;
        if (supply + payoff <= 0) return 0;
        int copies = context.GetStatistics().Copies(card.Id);
        double duplicateFactor = 1 + copies * 0.7;
        // Choosing a direction is not evidence that an unsupported payoff already works.
        if (supply == 0 && current.Supply <= 0) return 2 / duplicateFactor;
        var after = new MechanismBalance(current.Supply + supply, current.Payoffs + payoff);
        double total = current.Supply + current.Payoffs + 2;
        double gap = (supply > 0 ? 8 * (current.Payoffs + 1) / total : 0)
            + (payoff > 0 ? 8 * (current.Supply + supply) / total : 0);
        double progress = 24 * (after.Readiness - current.Readiness);
        return Math.Min(24, 8 * Math.Min(1, supply + payoff) + Math.Min(8, gap) + progress) / duplicateFactor;
    }

    // A non-accumulating action tie-break. It never awards repeated plays or changes terminal HP ordering.
    internal static double ActionBonus(ArchetypePreference preference, string? cardId) => cardId is not null
        && (preference == ArchetypePreference.Stars
            ? cardId is "GLOW" or "GATHER_LIGHT" or "SHINING_STRIKE" or "SOLAR_STRIKE" or "BIG_BANG" or "GUIDING_STAR"
            : (Roles(preference) & AdviceMechanics.Roles(cardId)) != 0
                || preference == ArchetypePreference.Shiv && cardId == "SHIV"
                || preference == ArchetypePreference.Soul && cardId == "SOUL") ? 1 : 0;

    internal static int SetupBonus(ArchetypePreference preference, string powerType, int potential)
    {
        bool matches = preference switch
        {
            ArchetypePreference.Strength => powerType is "StrengthPower" or "DemonFormPower",
            ArchetypePreference.Exhaust => powerType is "CorruptionPower" or "DarkEmbracePower" or "FeelNoPainPower",
            ArchetypePreference.Poison => powerType is "EnvenomPower" or "NoxiousFumesPower" or "AccelerantPower",
            ArchetypePreference.Shiv => powerType is "AccuracyPower" or "InfiniteBladesPower",
            ArchetypePreference.Discard => powerType is "AfterimagePower",
            ArchetypePreference.Stars => powerType is "StarNextTurnPower",
            ArchetypePreference.Forge => powerType is "FurnacePower",
            ArchetypePreference.Soul => powerType is "HauntPower" or "DevourLifePower",
            ArchetypePreference.FocusOrbs => powerType is "FocusPower" or "LoopPower",
            _ => false,
        };
        return matches ? Math.Clamp(potential / 5, 0, 8) : 0;
    }
}
