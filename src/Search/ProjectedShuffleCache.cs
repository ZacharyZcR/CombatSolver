using System.Runtime.InteropServices;
using CombatSolver.Engine.Common;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;

namespace CombatSolver;

// Worker-owned; only RNG values and index swaps are retained, never card references.
internal sealed class ProjectedShuffleCache
{
    private const int Capacity = 64;
    private const int MaximumCards = 256;
    private readonly Dictionary<(PredictionRngState State, int Count), int[]> _swaps = [];
    private (PredictionRngState State, int Count) _lastKey;
    private int[]? _lastSwaps;
    public long Hits { get; private set; }
    public long Misses { get; private set; }

    public void Shuffle<T>(List<T> items, Rng source)
    {
        if (items.Count < 2) return;
        if (items.Count > MaximumCards)
        {
            items.UnstableShuffle(source.Clone());
            return;
        }
        var key = (source.CaptureState(), items.Count);
        int[]? swaps = key == _lastKey ? _lastSwaps : null;
        if (swaps is null && !_swaps.TryGetValue(key, out swaps))
        {
            Misses++;
            swaps = CreateSwaps(items.Count, source);
            if (_swaps.Count == Capacity) _swaps.Clear();
            _swaps.Add(key, swaps);
        }
        else Hits++;
        _lastKey = key;
        _lastSwaps = swaps;
        Span<T> values = CollectionsMarshal.AsSpan(items);
        for (int i = 0; i < swaps.Length; i++)
        {
            int j = swaps[i];
            if (i != j) (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static int[] CreateSwaps(int count, Rng source)
    {
        List<int> permutation = Enumerable.Range(0, count).ToList();
        permutation.UnstableShuffle(source.Clone());
        int[] swaps = new int[count];
        Span<int> values = stackalloc int[count];
        Span<int> positions = stackalloc int[count];
        for (int i = 0; i < count; i++) values[i] = positions[i] = i;
        for (int i = 0; i < count; i++)
        {
            int j = positions[permutation[i]];
            swaps[i] = j;
            positions[values[i]] = j;
            positions[values[j]] = i;
            (values[i], values[j]) = (values[j], values[i]);
        }
        return swaps;
    }

    public void Clear()
    {
        _swaps.Clear();
        _lastSwaps = null;
    }
}
