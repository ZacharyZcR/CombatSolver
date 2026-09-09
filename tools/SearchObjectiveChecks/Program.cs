using CombatSolver;
if (args is ["--cache-write", var targetPath, var scoreText])
{
    int candidateScore = int.Parse(scoreText);
    BetterRouteFile.Store(targetPath, old => candidateScore < int.Parse(old), () => scoreText, CancellationToken.None);
    return;
}
int checks = 0;
void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
SearchObjectiveOutcome Outcome(SearchObjective mode, int hp, int growth, int loss = 0) => new(
    SearchObjectivePolicy.Default with { Mode = mode }, new(0, 0, growth), 0, 0, 0, 0, 0, 0, loss, hp);
Check(SearchObjectivePolicy.Default.MinimumEndingHp == 30, "Default reserve must be 30 HP");
foreach (var mode in new[] { SearchObjective.PermanentGrowth, SearchObjective.NetResources })
{
    var safe = Outcome(mode, 30, 1) with { GoldGain = 1 };
    var risky = Outcome(mode, 29, 100) with { GoldGain = 1000 };
    Check(safe.MeetsLimits && !risky.MeetsLimits && safe.CompareTo(risky) < 0,
        "Rewards must not override the HP reserve");
    Check(!(Outcome(mode, 50, 100, 11) with { GoldGain = 1 }).MeetsLimits, "Loss cap still applies");
    Check((risky with { Policy = risky.Policy with { MinimumEndingHp = 20 } }).MeetsLimits,
        "Explicit custom reserves remain supported");
}
for (int gain = 1; gain <= 100; gain++)
{
    var safe = Outcome(SearchObjective.Balanced, 50, 0);
    var risky = Outcome(SearchObjective.Balanced, 49, gain);
    Check(SolverInterimResultOrdering.ComparePrimaryQuality(true, 0, 2, true, 1, 2,
        candidateGrowthRewardCount: 0, currentGrowthRewardCount: gain,
        candidateObjective: safe, currentObjective: risky) < 0,
        "Balanced must prefer lower strategic loss over more growth");
    Check(SolverInterimResultOrdering.ComparePrimaryQuality(true, 0, 2, true, 0, 2,
        candidateGrowthRewardCount: gain, currentGrowthRewardCount: 0,
        candidateObjective: safe, currentObjective: safe) < 0,
        "Balanced must prefer free growth on an HP tie");
}
Check(Outcome(SearchObjective.Balanced, 1, 0).MeetsLimits,
    "Reward-only reserve must not gate balanced mode");
foreach (var mode in new[] { SearchObjective.PermanentGrowth, SearchObjective.NetResources })
{
    var noGain = Outcome(mode, 20, 0, 15);
    var unsafeGain = Outcome(mode, 19, 3, 16) with { GoldGain = 10 };
    var safeGain = Outcome(mode, 30, 3, 0) with { GoldGain = 10 };
    Check(!noGain.HasObjectiveGain && noGain.MeetsLimits,
        "No-gain routes must not be blocked by reward-only limits");
    Check(noGain.CompareTo(unsafeGain) < 0, "No gain must beat unsafe farming");
    Check(safeGain.CompareTo(noGain) < 0, "Safe positive gain remains preferred");
}
var zero = Outcome(SearchObjective.NetResources, 20, 0);
var spent = zero with { GoldGain = -10, EndingHp = 25 };
Check(zero.CompareTo(spent) == 0 && spent.MeetsLimits,
    "Nonpositive net returns fall back to HP and existing resource policy");
var unrelatedGold = Outcome(SearchObjective.PermanentGrowth, 20, 0) with { GoldGain = 100 };
Check(!unrelatedGold.HasObjectiveGain, "Gold must not count as permanent growth");
foreach(var mode in new[]{SearchObjective.PermanentGrowth,SearchObjective.NetResources})
{
    var policy=SearchObjectivePolicy.Default with { Mode=mode, GrowthTarget=0, NetResourceTarget=0 };
    var reached=Outcome(mode,30,3) with { Policy=policy, GoldGain=25 };
    Check(reached.CanStopSearch(true), "Legacy zero uses finite defaults");
    Check(!reached.CanStopSearch(false), "Incomplete routes must not stop search");
    Check(!(reached with { EndingHp=29 }).CanStopSearch(true), "Unsafe farming must not stop search");
    Check(!(reached with { Growth=default, GoldGain=0 }).CanStopSearch(true), "No gain is not target completion");
    var extra=reached with { Growth=new(0,0,100), GoldGain=1000 };
    Check(extra.TargetValue==reached.TargetValue, "Rewards above target must not improve objective priority");
}
Check(!Outcome(SearchObjective.Balanced,50,100).CanStopSearch(true), "Balanced retains its stopping rules");
var cached = new SolverInterimResult(true, 0, 4, 4, 9, 1, 0, 100, 2)
    { Objective = Outcome(SearchObjective.Balanced, 46, 0) };
Check(CachedRouteOrdering.IsBetter(cached with { StrategicHpDeficit = 3 }, cached, false), "Strictly better health replaces cache");
Check(!CachedRouteOrdering.IsBetter(cached with { StrategicHpDeficit = 5, Score = 10000 }, cached, false), "Heuristic score cannot overwrite better HP");
Check(!CachedRouteOrdering.IsBetter(cached with { Score = 10000 }, cached, false), "Equivalent outcomes do not churn cache");
Check(!CachedRouteOrdering.IsBetter(cached with { Won = false, StrategicHpDeficit = 0 }, cached, false), "Incomplete plans cannot replace complete plans");
Check(CachedRouteOrdering.IsBetter(cached with { PotionStrategicCost = 3 }, cached, false), "Potion value is part of cache quality");
Check(!CachedRouteOrdering.IsBetter(cached with { OutstandingStolenResource = 1, PotionStrategicCost = 0 }, cached, true), "Theft policy survives replacement");
Check(!CachedRouteOrdering.IsBetter(cached with { Objective = Outcome(SearchObjective.PermanentGrowth, 50, 3) }, cached, false), "Different objectives never share replacement ordering");
var growthCached = cached with { Objective = Outcome(SearchObjective.PermanentGrowth, 40, 1) };
Check(!CachedRouteOrdering.IsBetter(growthCached with { Objective = Outcome(SearchObjective.PermanentGrowth, 29, 100) }, growthCached, false), "Unsafe growth cannot overwrite safe cache");
foreach (var (power, card, exhaust) in new[] { ("AccuracyPower", "BLADE_DANCE", false),
    ("AccuracyPower", "SHIV", true), ("StrengthPower", "TWIN_STRIKE", false),
    ("DarkEmbracePower", "SHIV", true), ("FocusPower", "GLACIER", false), ("HauntPower", "SOUL", true) })
    Check(CombatComboPolicy.Connects(power, card, exhaust), "Reviewed setup connects to its follow-up");
Check(!CombatComboPolicy.Connects("AccuracyPower", "STRIKE", false), "Unrelated cards are not combo continuations");
Check(CombatComboPolicy.Preserve(2, 50, 50, 0, 0, 3), "Safe ready combo can receive a slot");
Check(!CombatComboPolicy.Preserve(2, 49, 50, 1, 0, 3)
    && !CombatComboPolicy.Preserve(0, 50, 50, 0, 0, 3)
    && !CombatComboPolicy.Preserve(2, 50, 50, 0, 0, 2), "No extra HP loss, no empty combo, no defense starvation");
string cacheDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "combat-solver-cache-check-" + Guid.NewGuid().ToString("N"));
string cachePath = System.IO.Path.Combine(cacheDirectory, "route.json");
try
{
    Check(BetterRouteFile.Store(cachePath, _ => false, () => "5", CancellationToken.None), "Initial cache write");
    Check(!BetterRouteFile.Store(cachePath, old => int.Parse(old) > 6, () => throw new Exception("Must not serialize worse route"), CancellationToken.None), "Inferior cache write is skipped");
    Check(BetterRouteFile.Store(cachePath, old => int.Parse(old) > 3, () => "3", CancellationToken.None), "Superior route replaces file");
    Check(File.ReadAllText(cachePath) == "3", "Fresh file read observes replacement");
    using var cancel = new CancellationTokenSource();
    try { BetterRouteFile.Store(cachePath, _ => true, () => { cancel.Cancel(); return "1"; }, cancel.Token); Check(false, "Cancellation expected"); }
    catch (OperationCanceledException) { Check(File.ReadAllText(cachePath) == "3", "Cancellation preserves old file"); }
    Parallel.For(1, 9, candidate => BetterRouteFile.Store(cachePath, old => candidate < int.Parse(old), () => candidate.ToString(), CancellationToken.None));
    Check(File.ReadAllText(cachePath) == "1", "Concurrent writers retain the best route");
    var children = new[] { "0", "-1" }.Select(score =>
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        start.ArgumentList.Add("--cache-write"); start.ArgumentList.Add(cachePath); start.ArgumentList.Add(score);
        return System.Diagnostics.Process.Start(start)!;
    }).ToArray();
    foreach (var child in children)
    {
        using (child) Check(child.WaitForExit(5000) && child.ExitCode == 0, "Independent process finishes cache update");
    }
    Check(File.ReadAllText(cachePath) == "-1", "Independent processes retain the best route");
    Check(Directory.GetFiles(cacheDirectory, "*.tmp").Length == 0, "Temporary files cleaned");
}
finally { if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true); }
Console.WriteLine($"SEARCH_OBJECTIVE_CHECKS_OK checks={checks}");
