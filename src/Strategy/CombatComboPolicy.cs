namespace CombatSolver;

// Reviewed short chains only. No reward is accumulated for playing cards repeatedly.
internal static class CombatComboPolicy
{
    internal static bool Connects(string power, string card, bool exhausts) => power switch
    {
        "AccuracyPower" => card is "BLADE_DANCE" or "CLOAK_AND_DAGGER" or "FAN_OF_KNIVES" or "SHIV",
        "StrengthPower" => card is "TWIN_STRIKE" or "SWORD_BOOMERANG" or "RIP_AND_TEAR" or "SHIV",
        "DarkEmbracePower" or "FeelNoPainPower" => exhausts,
        "FocusPower" => card is "ZAP" or "BALL_LIGHTNING" or "COOLHEADED" or "GLACIER" or "COLD_SNAP" or "DARKNESS",
        "HauntPower" or "DevourLifePower" => card is "SOUL" or "GRAVE_WARDEN" or "REAVE" or "SEVERANCE",
        _ => false,
    };

    internal static bool Preserve(int progress, int hp, int parentHp, int loss, int parentLoss, int slots)
        => slots >= 3 && progress > 0 && hp >= parentHp && loss <= parentLoss;
}
