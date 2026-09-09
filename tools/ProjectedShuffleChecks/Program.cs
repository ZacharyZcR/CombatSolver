using CombatSolver;
using CombatSolver.Engine.Common;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Extensions;
using System.Diagnostics;

ProjectedShuffleCache cache = new();
int checks = 0;
foreach (int count in new[]{0,1,2,5,10,30,100,256,257})
for (ulong seed=0;seed<100;seed++)
{
    Rng rng = new(seed); rng.Advance((int)(seed%7));
    var state=rng.CaptureState();
    for(int repeat=0;repeat<3;repeat++)
    {
        var expected=Enumerable.Range(0,count).Select(i=>new Item(i,i%3)).ToList();
        var actual=expected.ToList();
        expected.Sort();actual.Sort();
        expected.UnstableShuffle(rng.Clone());cache.Shuffle(actual,rng);
        if(!expected.SequenceEqual(actual)||rng.CaptureState()!=state)throw new Exception("Permutation or RNG mismatch");
        checks++;
    }
}
cache.Clear();
var sharedRng=new Rng(42UL);var input=Enumerable.Range(0,30).ToList();
cache.Shuffle(input,sharedRng);
for(int i=0;i<10000;i++){input.UnstableShuffle(sharedRng.Clone());cache.Shuffle(input,sharedRng);}
foreach(bool previous in new[]{true,false})
{
    long bytes=GC.GetAllocatedBytesForCurrentThread();long start=Stopwatch.GetTimestamp();
    for(int i=0;i<200000;i++)
        if(previous)input.UnstableShuffle(sharedRng.Clone());else cache.Shuffle(input,sharedRng);
    double ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
    Console.WriteLine($"previous={previous} calls=200000 count=30 bytes={allocated} ms={ms:F2}");
}
foreach (int count in new[]{10,30,100})
{
    var values=Enumerable.Range(0,count).Select(i=>new Item(i,i%3)).ToList();
    var timings=new[]{new List<double>(),new List<double>()};
    cache.Shuffle(values,sharedRng);
    for(int warm=0;warm<20000;warm++){values.UnstableShuffle(sharedRng.Clone());cache.Shuffle(values,sharedRng);}
    for(int run=0;run<5;run++)
    foreach(int mode in (run%2==0?new[]{0,1}:new[]{1,0}))
    {
        long start=Stopwatch.GetTimestamp();
        for(int i=0;i<100000;i++)
            if(mode==0)values.UnstableShuffle(sharedRng.Clone());else cache.Shuffle(values,sharedRng);
        timings[mode].Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
    timings[0].Sort();timings[1].Sort();
    Console.WriteLine($"reference-items count={count} 100000 calls median/5 previous={timings[0][2]:F2}ms cached={timings[1][2]:F2}ms");
}
// Clear must discard the fast path as well as the dictionary.
long misses=cache.Misses;cache.Clear();cache.Shuffle(input,sharedRng);
if(cache.Misses!=misses+1)throw new Exception("Clear retained cached permutation");
Console.WriteLine($"PROJECTED_SHUFFLE_CHECKS_OK checks={checks} hits={cache.Hits} misses={cache.Misses}");
record Item(int Id,int Value):IComparable<Item>{public int CompareTo(Item? other)=>Value.CompareTo(other!.Value);}
