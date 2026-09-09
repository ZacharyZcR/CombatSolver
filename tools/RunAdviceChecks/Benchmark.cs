using CombatSolver;
using System.Diagnostics;

internal static class AdviceBenchmark
{
    internal static void Run()
    {
        var deck=Enumerable.Range(0,32).Select(i=>new AdviceCard(i%4==0?"BLADE_DANCE":"CARD_"+i, i%2==0?8:0,i%2==1?6:0,0,1,i%2==0,false,false,true,AdviceTag.None,Roles:i%4==0?AdviceRole.ShivSource:AdviceRole.None)).ToArray();
        var context=new AdviceContext(deck,new HashSet<string>(),250,50,80,0,1);
        AdviceOffer[] offers=[new("remove",AdviceKind.Removal,"remove",75),new("card",AdviceKind.Card,"card",50,deck[0]),new("skip",AdviceKind.Skip,"skip")];
        for(int i=0;i<3;i++) RunAdvice.Rank(context,offers,true);
        long before=GC.GetAllocatedBytesForCurrentThread();var sw=Stopwatch.StartNew();
        for(int i=0;i<20;i++) RunAdvice.Rank(context,offers,true);
        sw.Stop();Console.WriteLine($"SHOP_EVALUATION rounds=20 deck=32 elapsed_ms={sw.Elapsed.TotalMilliseconds:F2} allocated={GC.GetAllocatedBytesForCurrentThread()-before}");
    }
}
