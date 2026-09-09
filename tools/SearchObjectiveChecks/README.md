# Search objective contracts

Run `dotnet run --project tools/SearchObjectiveChecks/SearchObjectiveChecks.csproj -c Release`.

Links production objective and ordering code; only the runtime result DTO is stubbed. Covers the 30 HP default, reward constraints, custom reserves, balanced loss-before-growth ordering, and no-gain fallback without reward-only HP gating. Does not run game simulation, beam search, potion eligibility, or UI.

新增缓存正式质量比较、跨进程原子更新、取消与同分不覆盖，以及短组合保留条件。文件检查使用临时目录并自动清理。
