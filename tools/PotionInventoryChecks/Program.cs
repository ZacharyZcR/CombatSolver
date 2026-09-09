using CombatSolver;
int checks = 0;
void Check(int actual, int expected) { if (actual != expected) throw new Exception($"{actual} != {expected}"); checks++; }
Check(PotionInventoryValue.RequiredCost(9, true, true, 9), 3);
Check(PotionInventoryValue.RequiredCost(18, true, true, 9), 12);
Check(PotionInventoryValue.RequiredCost(18, true, false, 9), 18);
Check(PotionInventoryValue.RequiredCost(9, false, true, 9), 9);
Check(PotionInventoryValue.RequiredCost(0, true, false, 9), 0);
for (int regular = 1; regular <= 20; regular++)
for (int first = 1; first <= regular; first++)
for (int hp = 0; hp <= 100; hp++)
{
    int expected = 0;
    while (first + expected * regular <= hp) expected++;
    Check(PotionInventoryValue.PaidCapacity(hp, regular, first), expected);
}
Check(PotionInventoryValue.PaidCapacity(100, int.MaxValue, int.MaxValue), 0);
Check(PotionInventoryValue.PaidCapacity(-1, 9, 3), 0);
Check(PotionInventoryValue.PaidCapacity(3, 9, 3), 1);
Check(PotionInventoryValue.PaidCapacity(11, 9, 3), 1);
Check(PotionInventoryValue.PaidCapacity(12, 9, 3), 2);
var healthy = new PotionOpportunityPolicy(70, 80, false, false);
var danger = new PotionOpportunityPolicy(20, 80, false, false);
var refill = new PotionOpportunityPolicy(70, 80, true, false);
Check(PotionInventoryValue.RequiredCost(9, false, true, 9, healthy), 11);
Check(PotionInventoryValue.RequiredCost(9, false, true, 9, danger), 6);
Check(PotionInventoryValue.RequiredCost(9, true, true, 9, refill), 1);
Check(PotionInventoryValue.RequiredCost(18, true, false, 9, refill), 18);
Check(PotionInventoryValue.RequiredCost(18, true, true, 9, refill), 10);
Check(PotionInventoryValue.RequiredCost(9, false, true, 9, healthy with { Boss = true }), 8);
for (int hp = 1; hp <= 80; hp++)
{
    int first = new PotionOpportunityPolicy(hp, 80, true, false).FirstOrdinaryCost(9, true);
    Check(first >= 1 && first <= 9 ? 1 : 0, 1);
    Check(PotionInventoryValue.PaidCapacity(0, 9, first), 0);
}
Console.WriteLine($"POTION_INVENTORY_CHECKS_OK checks={checks}");
