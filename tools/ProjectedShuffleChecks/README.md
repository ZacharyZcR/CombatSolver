# Projected shuffle cache checks

Run `dotnet run --project tools/ProjectedShuffleChecks/ProjectedShuffleChecks.csproj -c Release` with game 0.111.0 paths in `local.props`.

Links the production cache and RNG extensions, and invokes the actual game's generic `UnstableShuffle` without starting the game. Checks 2,700 permutations across seeds, advanced states, 0–257 items, equal-sort distinct identities, cache hits, eviction and clearing. Source RNG must remain unchanged. Microbenchmarks isolate repeated cache hits; they do not measure real search hit rates, miss-heavy workloads or whole-game speedup.
