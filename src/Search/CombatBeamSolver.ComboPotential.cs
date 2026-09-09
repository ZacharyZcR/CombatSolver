using CombatSolver.Engine.InCombat.Simulation;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CombatSolver;

internal sealed partial class CombatBeamSolver
{
    private int ComboPotential(CombatPredictionSimulator simulator)
    {
        var combat = (SimulatedCombatState)simulator.State.CombatState;
        var player = simulator.State.GetPlayerCombatState(_player);
        var powers = combat.EffectivePowers();
        bool corruption = combat.GetAmount<CorruptionPower>(_player.Creature) > 0;
        bool noDraw = combat.GetAmount<NoDrawPower>(_player.Creature) > 0;
        int potential = 0;
        foreach (var power in powers)
        {
            if (!ReferenceEquals(power.Owner, _player.Creature) || power.Amount <= 0
                || power.GetType().Assembly != typeof(AccuracyPower).Assembly) continue;
            string powerType = power.GetType().Name;
            if (powerType is not ("AccuracyPower" or "StrengthPower" or "DarkEmbracePower" or "FeelNoPainPower"
                or "FocusPower" or "HauntPower" or "DevourLifePower")) continue;
            if (noDraw && power is DarkEmbracePower) continue;
            foreach (var card in player.Hand.Cards)
            {
                if (card.Preview.GetType().Assembly != typeof(AccuracyPower).Assembly) continue;
                bool exhausts = card.Preview.Keywords.Contains(CardKeyword.Exhaust)
                    || corruption && card.Preview.Type == CardType.Skill;
                if (CombatComboPolicy.Connects(powerType, card.Preview.Id.Entry, exhausts)
                    && combat.CanPlayCard(simulator, card))
                    potential += Math.Clamp(power.Amount, 1, 4);
            }
        }
        return Math.Min(12, potential);
    }
}
