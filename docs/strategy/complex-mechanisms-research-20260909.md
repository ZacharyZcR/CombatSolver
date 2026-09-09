# 复杂机制与评分边界调查（0.111.0）

日期：2026-09-09。延续[角色体系调查](archetype-advice-research-20260909.md)。本轮为静态研究，没有修改算法、启动游戏或验证推荐胜率。

## 证据与范围

事实来自本机游戏 `data_sts2_windows_x86_64/sts2.dll`，使用 ilspycmd 9.1.0.7988 定向读取。新增读取 13 个原生类型：ForgeCmd、OstyCmd、OrbCmd、OrbQueue、DarkOrb、PlasmaOrb、EndOfDays、SoulStorm、SpiritOfAshPower、DieForYouPower、CreatureCmd、CalculatedDamageVar、OrbModel；另复查前轮 Soul、Unleash、SovereignBlade、DoomPower。提取文件仅保存在忽略目录 `.local/archetype-research/`，不分发原生反编译源码。不是全卡池审计。

建议算法对照对象是 PR #73 的 `0333116`（`AdviceMechanics`），战斗实现对照当前本地分支。建议评分未识别机制不代表战斗引擎不支持机制；静态差异也不能直接定性为模拟 Bug。

下文使用原生类型标识，避免未核对官方本地化时自行翻译卡名。

## 1. Forge：生成、强化与兑现是三个环节

原生 `ForgeCmd.Forge` 先查非 IsDupe 且未消耗的 SovereignBlade；没有才生成一张加入手牌。随后强化所有非 IsDupe 的 Blade，**包括消耗堆中的 Blade**，最后执行 AfterForge。不能理解为每次 Forge 都生成新牌，也不能理解为只强化手里的一张。

SovereignBlade 有独立伤害、重复次数和其他战斗状态；OnPlay 使用伤害与重复次数执行攻击。追加伤害只有通过后续攻击才能兑现，不能直接记为本次造成伤害，更不能凭“数值增加”归入永久成长。

评分建议：识别 Forge 来源及费用、Blade 的可用性、支持重复攻击的效果；对来源收益做饱和处理。战斗内再根据能量、手牌与攻击窗口判断是否值得先强化。仅用 Forge 数量乘固定分数会高估来不及兑现的强化。

未来最小验证：无 Blade 时生成；仅有已消耗 Blade 时再生成且旧 Blade 也强化；IsDupe 排除；本回合无法支付攻击费用；重复攻击下强化的收益。还需沿完整 Hook 链核对 AfterForge，不能只比较 `PersistentPowerSupport.Forge` 一个函数就断言漏触发。

## 2. Soul：同一张牌有多个不同的收益时点

`Soul.OnPlay` 抽 2 张（升级 3 张），费用 0，关键词 Exhaust；`CreateInHand` 通过生成卡命令入手。生成数量不是已经完成的抽牌数量，仍受手牌容纳、打出机会、NoDraw 等条件影响。

`HauntPower.AfterCardPlayed` 对本人打出的 Soul 触发伤害；`SoulStorm` 的计算倍率则读取**当前消耗堆中 Soul 的数量**。后者不是“累计打出 Soul 次数”，移出消耗堆会改变条件。`AfterCardPlayedMirrors.HandleDevourLifePower` 还展示 Soul 打出后的召唤关联，但本轮未独立读取该 Power 原生实现，暂作为后续核对项。

建议拆成 Soul 生成、Soul 打出收益、消耗堆 Soul 收益三类。生成牌可以补抽牌能力，但不能把尚未生成的 Token 当成起始牌组卡数，再额外重复计算抽牌收益。SoulStorm 没有来源时也不能当完全无效牌，它有自己的基础伤害。

另一个容易误判的入口：`SpiritOfAshPower.BeforeCardPlayed` 在打出 Ethereal 牌前提供格挡，不能把它归类为“回合末 Ethereal 消耗后才获得格挡”。

未来最小验证：生成但未打出；NoDraw 下打出；Soul 进入与离开消耗堆；同一牌同时服务抽牌、打出触发和消耗触发，分数不重复计算同一收益。

## 3. Osty：血量兼有承伤和输出价值

`OstyCmd.Summon` 先经过 ModifySummonAmount。存活时调用 GainMaxHp；`CreatureCmd.GainMaxHp` 增加最大生命后按实际增加量治疗。缺失或死亡时走创建/复活分支，设定最大生命并治疗；复活还有 AfterOstyRevived，最终记录召唤历史并触发 AfterSummon。

`DieForYouPower.ModifyUnblockedDamageTarget` 只在目标为主人、Osty 存活且伤害属于 PoweredAttack 时转移未格挡伤害。因此不能把 Osty HP 当作对所有失血都有效的主人 HP。具体溢出伤害仍需沿伤害结算链核对，本轮不作结论。

`Unleash` 的动态伤害倍率读取存活 Osty 的**当前** HP，缺失时 OnPlay 不攻击。模型同时定义基础项与额外伤害项，所以不能仅凭倍率把它说成“基础伤害乘血量”。

建议评分识别召唤供给与 OstyAttack 依赖；战斗评分分别保留主人生命、Osty 当前生命、最大生命与是否存活，避免把承伤价值和输出价值简单相加而双计。Osty 变大属于战斗资源，不应自动变成玩家永久最大生命收益。

未来最小验证：受伤但存活时召唤；死亡后召唤；先召唤再攻击与相反顺序；PoweredAttack 与其他失血；主人已有格挡时的承伤链。全套状态相等前不扩大缓存复用范围。

## 4. Doom：阈值、时点与死亡 Hook

`DoomPower.IsOwnerDoomed` 判断 CurrentHp <= Amount。敌方在 BeforeSideTurnEnd 检查，玩家方在 AfterSideTurnEnd 检查，并要求拥有者属于 participants、存活且战斗未结束。达到阈值不等于立即取消敌方本轮行动。

`EndOfDays.OnPlay` 对敌人施加 Doom 后主动调用 DoomKill，属于即时检查入口。DoomKill 批量处理目标，然后统一执行 AfterDiedToDoom；原生注释特别提到逐个调用会漏掉复活相关 Fatal 触发。因此必须保留死亡与复活语义，不能把阈值满足直接写成终局胜利。

可用于启发式的缺口是 `max(0, 当前生命 - Doom)`，但它只表示距离阈值的差，不是预期伤害。伤害可降低缺口，继续叠 Doom 也可降低缺口；是否能免受下一次攻击取决于实际结算入口。

奖励评分可以识别 Doom 来源、放大与主动结算入口；战斗内按每个敌人分别估计缺口和剩余威胁。禁止把全场 Doom 总量当作等量即时伤害。

未来最小验证：阈值前后各 1 点；普通 Doom 与 EndOfDays；同一时点多个目标死亡；复活敌人；目标回血后重新检查阈值。

## 5. 充能球：有序状态，不能压成数量

`OrbCmd.Channel` 满槽时先 EvokeNext，再尝试入队，成功后调用 AfterOrbChanneled。EvokeNext/Last 分别取队首/队尾；普通激发先从队列移除，执行球效果，再调用 AfterOrbEvoked。`dequeue=false` 则可激发而不移除。

`OrbQueue.RemoveCapacity` 从尾部直接移除超额球，**没有激发调用**。增加槽位上限为 10；更多槽位也会延后满槽自动激发，不能一律理解为更快获得爆发收益。

`DarkOrb` 初始积蓄 6，被动把经过 ModifyOrbValue 的基础 6 加到积蓄中；激发直接读取积蓄并选择当前 HP 最低的可命中敌人。已有积蓄与后续增长必须分开，不应将现有积蓄再次统一乘 Focus。

`PlasmaOrb` 的被动/激发值直接为 1/2 能量，没有走该球的 ModifyOrbValue；被动在回合开始触发。Dark 被动在回合结束触发。两类球不能共用“每回合末统一产出”的估值。

建议评分区分产球、Focus、扩槽、激发入口，避免给以 Plasma 为主的牌组无条件套 Focus 协同。具体战斗至少保留球类型顺序、容量、各 Dark 积蓄、触发修正与目标状态。相同球数量而顺序不同的状态不能共用结果缓存。

未来最小验证：相同球集合交换顺序后满槽产球；移除槽位不激发；保留球的激发；不同积蓄 Dark；改变敌人血量后 Dark 目标变化；Plasma 回合开始收益与当前回合能量需求。

## 实现顺序与验收

| 批次 | 内容 | 可以证明什么 |
| --- | --- | --- |
| 下一批建议评分 | Soul 来源/两种收益入口、召唤/OstyAttack、Forge 来源/兑现、Plasma 与 Focus 区分 | 纯数据用例下评分有依据、方向正确、无重复加分 |
| 后续战斗评估 | Doom 阈值与结算入口、Osty 当前 HP 的条件价值 | 有限场景搜索能保留真正有效的路线 |
| 后续顺序评估 | 球队列、Dark 积蓄、跨回合兑现 | 原生差分与固定预算短搜中保持语义并改善决策 |

建议评分使用有限的已审查元数据，并显示依赖缺口；未知机制不套用肯定的协同理由。奖励界面缺少未来敌人、手牌和抽牌顺序，不应把假想单一局面模拟出的数值包装成准确收益。

缓存只复用输入完整的派生计算。牌组概况可按牌组/升级/相关遗物变化失效；战斗价值缓存必须包含相应分支状态、政策和评估边界。不要为了命中率合并有序球、消耗堆 Soul、Osty 生命或 Doom 目标不同的节点。

重复可行操作不等于可无限获益：至少需检查能量/星能、可再次取得的卡、抽牌限制、消耗与生成来源、敌方行动和退出条件。Forge、SoulStorm 或 Dark 的战斗内增长不应被计入永久成长目标，更不能因此取消已有收益达标停止机制。

本轮仅完成原生方法与现有建议规则的静态对照、文档链接检查；上述未来用例均未执行。没有新的性能、胜率或实际安装结论。
