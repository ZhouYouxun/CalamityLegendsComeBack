# Infernum Mode Boss AI 重写分析

## 范围与结论

本文分析截图中 `Content/BehaviorOverrides/BossAIs` 下的 14 个 Boss：

`AquaticScourge`、`AstrumAureus`、`AstrumDeus`、`CeaselessVoid`、`Cryogen`、`HiveMind`、`OldDuke`、`Perforators`、`PlaguebringerGoliath`、`Polterghast`、`Providence`、`Signus`、`StormWeaver`、`Yharon`。

截图中的 `Common` 是多个 Boss 共用的辅助代码，不是一个 Boss，因此不单独作为战斗分析对象。

这些 Boss 的改写程度都不是简单调参。它们的主 `NPCBehaviorOverride.PreAI` 最终都会返回 `false`，阻止原版 Calamity AI 继续执行，然后由 Infernum 自己维护攻击状态、计时器、阶段转换和攻击选择器。换句话说，这 14 场战斗的主 AI 都属于**完整重写**。不少 Boss 的身体节段、召唤物、分身或专属弹幕也有额外覆盖。

召唤方式主要依据本项目和相邻的 CalamityMod 源码确认。Infernum 没有重写召唤入口时，本文会标为“沿用 Calamity”。阶段百分比均指 Boss 当前生命占最大生命的比例。

本文重点列出实际影响躲避的攻击和敌对实体；纯视觉粒子、装饰性弹幕与音效不逐个展开。源码中存在但当前攻击选择器无法正常选到的攻击，会明确标为“未接入当前循环”。

## 总览

| Boss | 召唤入口 | 主要阶段阈值 | 改写覆盖 |
| --- | --- | --- | --- |
| Aquatic Scourge | 硫磺海使用 `Seafood`；Infernum 禁止自然生成 | 67%、25% | 头、身体、尾完整覆盖 |
| Astrum Aureus | 星辉瘟疫地使用 `Astral Chunk` | 60%、45% | 本体与 Aureus Spawn 覆盖 |
| Astrum Deus | 用 Titan Heart 或 Starcore 右键 Astral Beacon | 60%、33.33% | 头、身体、尾与召唤仪式覆盖 |
| Ceaseless Void | 地牢使用 Rune/Mark of Providence；档案馆内有被锁住的遭遇体 | 66.67%、15% | 本体与 Dark Energy 覆盖 |
| Cryogen | 雪原使用 `Cryo Key` | 90%、70%、55%、40%、25% | 本体完整覆盖 |
| Hive Mind | 腐化之地使用 `Teratoma`，或击杀 Hive Tumor | 72%、56%、39%、20% | 本体、Dark Heart、Hive Blob 覆盖 |
| Old Duke | 硫磺海使用 Infernum 的 `Bloodworm Platter`；也保留原有路线 | 75%、37.5%、20% | 本体、Tooth Ball 与 Trilobite Spike 覆盖 |
| Perforators | 猩红之地使用 `Bloody Worm Food`，或击杀 Perforator Cyst | 70%、50%、25% | Hive 与三种蠕虫头/身体覆盖 |
| Plaguebringer Goliath | 丛林使用 `Abombination` | 75%、30% | 本体完整覆盖，另有自定义无人机/建造者 |
| Polterghast | 地牢使用 `Necroplasmic Beacon` | 65%、35%、致死触发 | 本体、分身与自定义腿覆盖 |
| Providence | 在 Profaned Temple/Garden 用 Profaned Core 右键祭坛 | 70%、4% | 本体、守卫与 Profaned Rocks 覆盖 |
| Signus | 地狱使用 Rune/Mark of Providence；初次有巡逻遭遇 | 70%、30% | 本体完整覆盖 |
| Storm Weaver | 天空使用 Rune/Mark of Providence；击败前可自然遭遇 | 50% | 头、身体、尾完整覆盖 |
| Yharon | 使用 `Yharon Egg` | 一阶段 75%、45%、10%；二阶段 80%、40%、15%、2.5% | 本体完整覆盖 |

---

## Aquatic Scourge

### 召唤、开场与改写程度

Aquatic Scourge 沿用 `Seafood` 召唤，地点必须是硫磺海。Calamity 原本允许它通过自然条件出现，但 Infernum 在全局生成逻辑中禁用了这条自然生成路线，因此这里应当把 `Seafood` 当作稳定的主动召唤方式。

主 AI 位于 `Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeHeadBehaviorOverride.cs`，身体和尾部也分别被覆盖。它不是普通蠕虫 Boss 的追踪 AI：头部状态机决定整场战斗的节奏，身体节段会在特定攻击中主动发射弹幕。

召唤后，它先从玩家下方上升，清理周围硫磺水并制造一个可呼吸、可立足的安全气泡。开场过程中还会落下酸液、气泡和碎石，先让玩家建立对战斗空间和安全气泡的认知。

### 阶段与攻击流程

第一阶段持续到 67% 生命，攻击循环是 `BubbleSpin`、`RadiationPulse`、`WallHitCharges`、`GasBreath`，其中撞墙冲锋会在循环中重复出现。`BubbleSpin` 会围绕玩家旋转、释放气泡，然后引爆或重新引导气泡，并从旋转动作转入冲锋。`RadiationPulse` 是较慢的追逐状态，Boss 周围周期性产生辐射脉冲、弧形酸液和安全气泡。`WallHitCharges` 则要求它真正撞向场地墙面；撞击后它会反弹并短暂失衡，同时喷出碎石和气泡。`GasBreath` 会越过玩家后吐出持续气体，迫使玩家改变原本的移动路线。

生命低于 67% 时，它进入第二阶段转换，转换期间不造成接触伤害。之后攻击池改为 `PerpendicularSpikeBarrage`、`RadiationPulse`、`GasBreath` 和 `WallHitCharges`。最明显的新机制是身体节段会朝运动方向的垂线两侧发射尖刺，把原本只需观察头部的战斗变成整条身体都具有火力。

生命低于 25% 时进入最终阶段。它抬升战斗中的酸水线并在转换时无敌，之后只使用 `GasBreath`、`AcidRain` 和 `SulphurousTyphoon`。`AcidRain` 会让 Boss 反复钻入、上升并吐出从高处落下的酸液；`SulphurousTyphoon` 则让它围绕硫磺龙卷旋转并喷吐气泡。最终阶段不再依赖普通的蠕虫追逐，而是把安全空间压缩、落雨和持续区域伤害组合起来。

### 弹幕与冲撞逻辑

它的弧形酸液 `AcceleratingArcingAcid` 每帧旋转速度方向并将速度乘以约 `1.016`，所以轨迹会弯曲且逐渐加速。`FallingAcid` 受重力影响，主要形成从上方下落的危险区。身体尖刺先追踪，再加速并彼此分离。`SulphuricTornado` 上升时会左右摆动，同时持续喷出落酸。安全气泡不是装饰，而是对抗水体与部分场地压力的核心空间。

这场战斗的冲撞设计很明确：普通冲锋不是单纯穿过玩家，而是与墙面碰撞、反弹、失衡和附带弹幕绑定。Boss 在最终阶段前离开水体会激怒并强化，因此也不适合把它拉出硫磺海逃课。

---

## Astrum Aureus

### 召唤、开场与改写程度

Astrum Aureus 沿用 Calamity 的 `Astral Chunk`，在星辉瘟疫地召唤。主 AI 位于 `Content/BehaviorOverrides/BossAIs/AstrumAureus/AstrumAureusBehaviorOverride.cs`，其召唤出的 Aureus Spawn 也有独立覆盖。

生成后它会先执行约 90 帧的激活状态；如果玩家提前攻击，也可以使它提前苏醒。开场结束后，AI 使用带历史记录的随机攻击选择器，避免连续重复最近两种攻击。每执行四次攻击，它会进入 `Recharge`：接触伤害关闭、防御降低，给玩家一个明确输出窗口。

### 阶段与攻击流程

第一阶段的主要攻击包括行走并射击激光、跃起砸向玩家、导弹齐射、星辉激光爆发和充能。`WalkAndShootLasers` 让 Boss 在地面追逐并发射直线激光；`LeapAtTarget` 先悬停定位，再朝下方重砸，落地时生成彗星、导弹和冲击波；`RocketBarrage` 先作瞄准提示，再释放会追踪的导弹；`AstralLaserBursts` 则用较慢的扩散激光封锁移动。

低于 60% 后进入第二阶段。这里没有很长的转场动画，而是通过发光、速度和参数缩放提高压力。低于 45% 后进入第三阶段，普通的 `AstralLaserBursts` 会从攻击池移除，`AstralDrillLaser` 被显著提高优先级，第一次达到该阶段时尤其容易立刻使用。钻头激光先画出线状预警，再发射橙色和蓝色光束；光束会先向下推进，随后向上弯折，并用额外扩散激光惩罚传送或过度拉远。

### 弹幕与冲撞逻辑

`AstralMissile` 会做弱追踪，并持续以约 `1.021` 的倍率加速；蓝色彗星和普通激光更接近直线弹幕。Aureus Spawn 在本体生命较高时会绕 Boss 旋转，之后才转为追踪并爆炸，因此需要同时观察本体动作与外围小怪。

它不像部分 Boss 那样连续水平冲锋，但跃起砸击本质上是一种带落点预判的纵向冲撞。砸击后的彗星、导弹和冲击波用于封锁玩家从落点侧面绕开的路线。

源码中还存在 `DoAttack_CelestialRain`，但它没有接入当前枚举、状态切换和正常攻击选择器，因此不能把它算作当前战斗中可正常出现的攻击。

---

## Astrum Deus

### 召唤、开场与改写程度

Astrum Deus 通过右键 Astral Beacon 召唤，消耗或使用 Titan Heart，也可以使用可重复利用的 Starcore。Infernum 覆盖了召唤仪式弹幕 `DeusRitualDrama`：仪式约进行 374 帧后，Deus 会在玩家上方约 1900 像素处生成。Beacon 会继续作为战斗场地的横向基准；玩家离它横向超过约 4800 像素时会触发激怒逻辑。

头部主 AI 位于 `Content/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusHeadBehaviorOverride.cs`，身体、尾部和仪式生成器都有覆盖。生成时会构造约 65 个节段，之后完全使用 Infernum 的蠕虫状态机。

### 阶段与攻击流程

开场固定使用 `AstralMeteorShower`。第一阶段的基础攻击还有 `WarpCharge`、`RubbleFromBelow`、`PlasmaAndCrystals` 和 `InfectedStarWeave`。`WarpCharge` 会淡出、传送后冲锋，并在路径上留下火焰；流星雨让 Boss 先上升再下砸，同时以彗星制造背景弹幕；`RubbleFromBelow` 让它钻下去再从下方向上冲出，把碎石向上爆射；`PlasmaAndCrystals` 中 Boss 会突然贴近、追逐，身体节段也会发射星辉水晶；`InfectedStarWeave` 则让它围绕一颗逐渐成长的大型感染恒星运动，随后把恒星投向玩家，并让身体发射激光。

低于 60% 后，攻击池加入 `ConstellationExplosions`、`VortexLemniscate` 和 `AstralSolarSystem`。星座攻击会把星体排成对角线或波形图案，然后整体引爆；双纽线攻击让蠕虫沿类似“∞”的路径运动，同时使用漩涡、等离子与火焰；太阳系攻击会生成绕行的 Deus Spawn，稍后再释放它们追踪玩家。

低于 33.33% 时，它会飞出屏幕上方并脱去外壳，进入最终阶段。正常的五种基础攻击从选择器中移除，改为使用第二阶段攻击和 `DarkGodsOutburst`、`AstralGlobRush`。前者围绕黑洞或暗星旋转，并发射会转动的暗神激光；后者持续贴近、突然校正位置并喷出感染星团。最终阶段不只是数值加速，而是实际替换攻击池和视觉形态。

### 弹幕与冲撞逻辑

星辉水晶进行弱追踪；`PlasmaSpark` 先弱追踪，再以约 `1.015` 的倍率持续加速。两个漩涡会相互靠近，合并时爆炸，因此它们既追人也形成延迟威胁。星辉碎石受重力影响。Deus Spawn 先绕轨道运动，释放后弱追踪并爆炸。大型感染恒星会逐渐加速；黑洞则负责制造旋转的 `DarkGodLaser`。

冲撞方面，`WarpCharge` 通过淡出和传送隐藏起跑位置；`RubbleFromBelow` 是自下而上的冲撞；最终阶段的 `AstralGlobRush` 则以频繁贴近和位置校正制造近身压力。玩家需要同时判断头部冲线和整条身体的弹幕方向。

---

## Ceaseless Void

### 召唤、开场与改写程度

Ceaseless Void 可以在地牢使用 Rune of Kos 的现版本入口 `Mark of Providence` 唤醒。Infernum 还在 Forbidden Archives 中安排了被锁住的自然遭遇体；如果场景里已经存在这种被锁住或串联遭遇的个体，使用召唤物会让现有个体苏醒，而不是再创建一个重复 Boss。

主 AI 位于 `Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidBehaviorOverride.cs`，Dark Energy 也有独立覆盖。Boss 在玩家位于世界地表以上时会激怒并无敌，所以这场战斗明确要求留在地下档案馆/地牢环境中。

### 阶段与攻击流程

开场状态是 `ChainedUp`。此时它没有正常 Boss 条和接触伤害，重点是表现锁链和苏醒。第一阶段的攻击循环围绕固定在场地中的本体展开，包括 `DarkEnergySwirl`、`RedirectingAcceleratingDarkEnergy`、`DiagonalMirrorBolts`、`CircularVortexSpawn`、`SpinningDarkEnergy` 和 `AreaDenialVortexTears`。黑暗能量旋涡会压暗画面，生成可被击杀的 Dark Energy 环，并释放会加速的球体；重定向能量从上方坠落后再加速；镜像弹幕从对角方向成组进入；圆形漩涡先预警，再喷出裂隙弹；区域封锁漩涡则直接划分可走空间。

低于 66.67% 时进入 `ShellCrackTransition`。外壳开裂，并伴随 `DarkEnergyTorrent` 螺旋能量洪流。第二阶段加入 `EnergySuck`：Boss 制造能量环、抛出碎石并把玩家向内拉，玩家靠得过近还会受到接触伤害。这一阶段仍然主要是固定炮台式 Boss，但“向外躲弹幕”的常规选择会被吸力反过来利用。

低于 15% 时进入 `ChainBreakTransition`，锁链真正断裂，Boss 转为移动最终阶段。之后主要使用 `JevilDarkEnergyBursts`、`MirroredCharges` 和 `ConvergingEnergyBarrages`。它会频繁传送并爆发弹幕；镜像冲锋会把本体分置于玩家上方或垂直方向两侧，再按照精确距离加速冲过；汇聚能量则用预警线安排从多方向夹击的弹幕。

### 弹幕与冲撞逻辑

Dark Energy 辅助体绕 Boss 保持固定半径，生成后约 180 帧内无敌，之后才成为可处理的战斗目标。漩涡先显示预警，再根据预设距离计算裂隙弹所需加速度；裂隙弹离开后继续加速。碎石也不是完全匀速，会继续获得速度。

前两阶段几乎没有传统冲锋，压力来自固定中心、吸力和交叉弹幕。真正的冲撞只在锁链断裂后出现，因此 15% 是战斗性质变化最大的阈值：Boss 从场地机关变成会传送、镜像分身并直接冲过玩家的高速目标。

---

## Cryogen

### 召唤、开场与改写程度

Cryogen 沿用 `Cryo Key`，在雪原召唤。主 AI 位于 `Content/BehaviorOverrides/BossAIs/Cryogen/CryogenBehaviorOverride.cs`。它是这批 Boss 中阶段最细的一类：每到一个生命阈值都会打碎一层冰壳、喷出碎片和表现性物体，并重置攻击循环。

离开雪原后会启动激怒计时。持续约 600 帧后，Boss 会进入无敌状态，因此战斗场地必须实际保留雪原判定。

### 阶段与攻击流程

Cryogen 的阈值是 90%、70%、55%、40% 和 25%，合计六个子阶段。第一阶段围绕 `IcicleCircleBurst`、`PredictiveIcicles` 和 `TeleportAndReleaseIceBombs`：它会制造绕行冰刺环，稍后释放并加速；预测冰刺会根据玩家当前位置和速度瞄准；冰炸弹攻击则在移动时放置炸弹，然后传送重新定位。

90% 以下加入 `ShatteringIcePillars`，冰柱从地面升起，同时与冰刺环组合。70% 以下加入 `IcicleTeleportDashes`，Boss 淡出、传送、冲刺，并释放会重新定向的冰刺。55% 以下加入 `HorizontalDash`：它先排到玩家侧面、旋转预警，然后高速横穿，并沿冲锋方向的垂线释放冰刺。

40% 以下加入 `AuroraBulletHell`，战斗开始混合极光灵体和密集弹幕。25% 以下加入 `EternalWinter`，最终阶段把旋转、冲锋、灵体和冰刺组合在同一攻击中。后半场攻击池明显更偏向连续传送冲刺和水平冲刺。

### 弹幕与冲撞逻辑

`AimedIcicleSpike` 会先减速，再按照玩家速度预测结果转向，因此单纯保持固定方向更容易被命中。两类 Aurora Spirit 使用不同运动：一种先沿压扁的下坠正弦轨迹移动，再水平加速；另一种会弯向目标。冰柱从地面上升，冰炸弹会减速、淡出后爆炸。

Cryogen 的冲撞不是从开场就堆速度，而是随冰壳破碎逐层加入。第三、第四层之后，传送冲刺会隐藏出发位置，水平冲刺则用垂直冰刺封锁上下闪避；最终阶段再把两者与弹幕地狱叠加。

---

## Hive Mind

### 召唤、开场与改写程度

Hive Mind 沿用两条 Calamity 入口：在腐化之地使用 `Teratoma`，或击杀自然生成的 Hive Tumor。主 AI 位于 `Content/BehaviorOverrides/BossAIs/HiveMind/HiveMindBehaviorOverride.cs`，Hive Blob 和 Dark Heart 也有覆盖。

它的核心不是传统“到阈值后播放转场并换一套动作”，而是逐步替换攻击，并在最终阶段进行一次带治疗的伪第二条生命。基础状态包括漂移、重置、召唤弧、旋转突进、云层冲刺和 Blob 爆发。

### 阶段与攻击流程

高生命时，`SuspensionStateDrift` 让本体悬浮漂向玩家。此状态下它甚至会受击退影响，被击中后会逃离并重新加速。`NPCSpawnArc` 让它旋转并召唤 Eater of Souls 和 Dark Heart；`SpinLunge` 先传送，再绕行和突进，同时发射 `VileClot`；`CloudDash` 则传送后建立云层和降雨通道。

低于 72% 后，`EaterOfSoulsWall` 替代部分召唤弧攻击，直接创建横向敌怪墙。低于 56% 后，部分旋转突进会被 `UndergroundFlameDash` 替代：它从地下向上喷出 Shade Fire，随后咆哮并冲锋。低于 39% 后，部分云层冲刺被 `CursedRain` 替代，组合云、凝块、降雨和火焰柱。

低于 20% 时，它进入约 90 帧的无敌转换，并把生命从 20% 左右恢复到约 40%。AI 会记住它已经进入最终状态，所以即使生命被抬高，也不会退回早期阶段。之后攻击速度提高，并可能插入 `BlobBurst`，一次性制造波动和大量 Blob 弹幕。

### 弹幕与冲撞逻辑

Dark Heart 使用稳定的悬停 AI，停在玩家上方并降下 Shaderain。Shade Fire 是持续存在的彩色火焰区；Vile Clot 和 Blob 弹幕用于填补冲刺后的空间。

Hive Mind 的主要冲撞是传送后旋转突进、云层冲刺和地下火焰冲锋。它经常先用召唤物或雨幕限制横向空间，再从遮挡较强的位置冲入。20% 阶段的治疗尤其重要：玩家不能把它当作简单的最后 20% 爆发阶段，而要准备应对一段更快、可插入 Blob 爆发的延长战斗。

---

## Old Duke

### 召唤、开场与改写程度

Old Duke 保留 Calamity 原有的 Bloodworm 钓鱼和 Acid Rain 相关路线，同时 Infernum 新增了可重复使用的 `Bloodworm Platter`。在硫磺海使用 Platter 后，Old Duke 会在玩家上方约 800 像素处生成。

主 AI 位于 `Content/BehaviorOverrides/BossAIs/OldDuke/OldDukeBehaviorOverride.cs`。Old Duke Tooth Ball 和 Trilobite Spike 也有额外覆盖。当前覆盖代码把 `outOfOcean` 固定为 `false`，因此这套 AI 本身没有按离开海洋触发的额外激怒行为。

### 阶段与攻击流程

它在 75%、37.5% 和 20% 生命时分别进入约 150 帧的咆哮转换。第一次转换还可能生成成排鲨鱼。整场战斗的核心是成组冲锋：第一阶段会在酸液喷吐、齿球和鲨鱼漩涡之间穿插约五次常规冲锋。

`Charge` 会先调整到合适位置，再高速穿过玩家；玩家距离越远，冲锋速度还会提高。`AcidBelch` 让它悬停并喷出硫磺 Blob。`SharkronSpinSummon` 让 Boss 旋转并制造漩涡，从上方召来鲨鱼。`ToothBallVomit` 发射被覆盖过的齿球 NPC。

低于 75% 后加入更快的常规冲锋和 `GoreAndAcidSpit`，后者同时发射 Old Duke Gore 与追踪酸液。低于 37.5% 后，冲锋组前会加入 `TeleportPause`，让每一轮冲锋的起点更难直接观察。低于 20% 后，攻击循环几乎只剩传送停顿和连续七次冲锋，战斗完全转为高速节奏检查。

### 弹幕与冲撞逻辑

Tooth Ball 会在距离合适时追踪玩家，约 150 帧后减速，约 200 帧后无敌，并在约 250 帧时消失。Trilobite Spike 的覆盖会把部分尖刺替换为追踪 `OldDukeTooth`。`HomingAcid` 和追踪牙齿都使用平滑转向，而不是瞬间锁头。

Old Duke 是这批 Boss 中最典型的“冲来冲去”设计。阶段提高主要不是增加复杂弹幕，而是缩短冲锋之间的喘息、隐藏冲锋起点，并在最终阶段强制连续处理七次冲线。弹幕的作用通常是让玩家不能一直用同一条上下或水平路线规避冲锋。

---

## Perforators

### 召唤、开场与改写程度

Perforators 沿用两条入口：在猩红之地使用 `Bloody Worm Food`，或击杀 Perforator Cyst。Hive 主体位于 `Content/BehaviorOverrides/BossAIs/Perforators/PerforatorHiveBehaviorOverride.cs`，小、中、大三种蠕虫的头部和身体也被覆盖。

这场战斗将 Hive 本体和三条阶段蠕虫绑定在同一状态机里。到达下一个生命阈值附近时，如果当前阶段召唤出的蠕虫仍存活，Hive 可以暂时无敌，防止玩家跳过对应蠕虫阶段。

### 阶段与攻击流程

基础攻击循环由 `DiagonalBloodCharge`、`HorizontalCrimeraSpawnCharge`、`IchorBlasts` 和 `IchorSpinDash` 组成。对角鲜血冲锋会先悬停并预警，抛出下落或飞行的灵液后再冲锋；水平冲锋会在移动时召唤 Crimera；`IchorBlasts` 直接提供远程火力；`IchorSpinDash` 围绕玩家旋转并留下 Ichor Blob 障碍，随后冲入玩家。

低于 70% 时强制执行 `SmallWormBursts` 并召出小型蠕虫，同时部分水平 Crimera 冲锋被 `CrimeraWalls` 替代。小蠕虫会停在玩家下方，向上突进并喷出一圈下落灵液。

低于 50% 时强制执行 `MediumWormBursts`，并把部分 `IchorBlasts` 替换为 `IchorRain`。中型蠕虫持续追逐，并发射 Tooth Ball。低于 25% 时强制执行 `LargeWormBursts`，并把对角鲜血冲锋替换为 `IchorFountainCharge`。大型蠕虫具有更积极的追踪和转弯；最终 Hive 攻击会悬在玩家上方，从口部形成灵液喷泉，同时从两侧制造会加速的 Ichor Bolt 墙。

### 弹幕与冲撞逻辑

`FallingIchor` 和 `FallingIchorBlast` 受重力影响，并可能延后与方块碰撞。`IchorBlast` 的水平速度约以 `1.02` 倍持续增长；`IchorBolt` 约以 `1.022` 倍加速。Tooth Ball 会漂浮并带有不规则抖动。

这场战斗的冲撞来自两个层面：Hive 本体持续执行对角、水平和旋转后冲锋，蠕虫则从下方或侧方切入。各阶段不是简单叠加更多蠕虫，而是同步替换 Hive 自己的攻击，所以 70%、50% 和 25% 都会改变场地封锁方式。

---

## Plaguebringer Goliath

### 召唤、开场与改写程度

Plaguebringer Goliath 沿用 `Abombination`，在丛林使用。主 AI 位于 `Content/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlaguebringerGoliathBehaviorOverride.cs`。小型无人机、爆炸瘟疫冲锋者、炸弹建造者和瘟疫核弹由 Infernum 的自定义 NPC/弹幕配合，而不是只靠本体。

Boss 位于地表以上时会激怒，所以实际场地应当放在地下丛林。Boss Rush 中还有额外强化。它的阶段阈值是 75% 和 30%，但通常不会播放很长的阶段转换；变化主要体现在攻击路由、攻击数量和连续缩放。

### 阶段与攻击流程

`Charge` 是核心动作。Boss 先移动到玩家侧方或斜上方，再高速冲过，并在路径上留下 Plague Cloud。冲锋速度和次数会随着失去生命持续增长，不完全依赖硬阶段。

其他基础攻击包括 `MissileLaunch`、`PlagueVomit`、`CarpetBombing` 和 `ExplodingPlagueChargers`。导弹攻击先悬停瞄准，再发射会重新定向的瘟疫导弹；瘟疫喷吐形成扩散弹幕；地毯轰炸把高速横向移动和导弹/预警组合；爆炸冲锋者攻击会召唤自杀式小怪，它们接近后冲刺并爆炸。

低于 75% 后会使用更强的地毯轰炸路线并提高整体速度。低于 30% 后，选择器加入 `CarpetBombing3`、`DroneSummoning` 和 `BombConstructors`。无人机会围绕固定偏移位置行动，并使 Boss 自身的冲锋节奏变慢一些，随后自爆。炸弹建造攻击会在玩家上方召出两个小型建造者、一个大型建造者和 `PlagueNuke`；建造完成后核弹开始追踪并产生大范围爆炸。如果建造者提前死亡，组装过程可能被打断；建造者死亡时也会爆出火箭。

### 弹幕与冲撞逻辑

重定向瘟疫导弹会先调整方向，再加速追踪；普通瘟疫导弹同样会持续加速。瘟疫云缓慢移动并淡出，主要用于封锁 Boss 刚刚冲过的路线。爆炸冲锋者先升高，再朝玩家猛冲并自爆。

这场战斗是“冲锋作为骨架、机械单位作为干扰”的设计。Boss 会反复从侧面或斜角冲过，而导弹、云和无人机让玩家不能永远朝同一个方向逃。30% 后的核弹建造机制又迫使玩家在躲冲锋之外处理优先目标。

---

## Polterghast

### 召唤、开场与改写程度

Polterghast 沿用 `Necroplasmic Beacon`，在地牢召唤。主 AI 位于 `Content/BehaviorOverrides/BossAIs/Polterghast/PolterghastBehaviorOverride.cs`，分身 `PolterPhantom` 也有覆盖，并使用自定义 `PolterghastLeg`。生成后它创建四条腿，腿不只是装饰，而会参与挥击和场地控制。

阶段阈值是 65%、35%，另有一个由“受到致死伤害”触发的特殊绝望阶段。也就是说，把本体生命打空并不会立刻结束战斗。

### 阶段与攻击流程

第一阶段使用 `EctoplasmUppercutCharges`、`LegSwipes`、`WispCircleCharges` 和 `SpiritPetal`。上勾冲锋先让 Boss 移到玩家下方并隐去，预警后向上穿过玩家，同时向两侧释放灵质；腿部挥击会选择前侧腿进行摆动并释放漩涡；鬼火圆环攻击先安排绕行灵质，再让本体冲锋；灵魂花瓣则生成花形、带随机性的灵魂弹幕。

低于 65% 后加入 `AsgoreRingSoulAttack`、`ArcingSouls` 和 `VortexCharge`。Asgore 环会让灵魂圆环向 Boss 中心收缩，但保留一个随机缺口；弧形灵魂会沿曲线进入；漩涡冲锋让 Boss 加速冲入，同时吐出继续加速的幽灵漩涡。

低于 35% 后加入 `CloneSplit`。Boss 生成三个分身，本体与分身传送到玩家周围，然后同时冲锋。玩家需要从所有影像中读取共同的冲锋时机，而不是只追踪本体。

当本体将受到致死伤害时，正常死亡被拦截。Boss 减速并释放大量灵魂，然后进入 `DesperationAttack`：本体隐形且无敌，黑暗安全圈不断缩小；离开圈会受伤，同时向内螺旋的漩涡和快速 Asgore 圆环持续出现。完成这段绝望攻击后才真正掉落战利品并死亡。

### 弹幕与冲撞逻辑

`CirclingEctoplasm` 绕目标旋转；`EctoplasmShot` 会弱追踪，但运动中逐渐减速；`GhostlyVortex` 约以 `1.045` 倍持续加速。`SpinningSoul` 绕行，`NonReturningSoul` 加速离开，另一类灵魂则会返回或追踪。

Polterghast 的冲锋通常与视觉遮挡和场地机关绑定：从下方隐身上勾、鬼火环后冲锋、漩涡冲锋，以及分身同时冲锋。最终绝望阶段反而不依靠本体撞击，而是把玩家锁在不断缩小的圆中处理密集弹幕。

---

## Providence

### 召唤、开场与改写程度

Infernum 不允许像普通 Calamity 那样直接使用 Profaned Core 召唤 Providence。玩家必须在 Profaned Temple/Garden 找到 `ProvidenceSummoner` 祭坛，并持有 Profaned Core 后右键。祭坛生成的仪式弹幕持续约 375 帧，之后才创建 Boss。

主 AI 位于 `Content/BehaviorOverrides/BossAIs/Providence/ProvidenceBehaviorOverride.cs`。攻击型守卫、治疗型守卫和 Profaned Rocks 也有覆盖。战斗被强烈绑定到祭坛场地：单人时玩家会被限制在竞技场内，并得到无限飞行。夜间召唤会让 Providence 整场保持激怒状态，伤害显著提高。

它不是随机选择几种攻击循环，而是使用 `ProvidenceAttackSection` 维护接近时间线的固定攻击段落。当前代码中 `SyncAttacksWithMusic` 为 `false`，因此攻击不强制跟音乐同步，但顺序仍然具有编排性质。

### 阶段与攻击流程

第一阶段持续到 70% 生命。`FireEnergyCharge` 让 Providence 进入茧形态，场地下方出现熔岩，并制造汇聚火球，随后绕行释放。`CinderAndBombBarrages` 使用圣火余烬和炸弹建立弹幕层；`AcceleratingCrystalFan` 发射扇形加速水晶；攻击型守卫会生成 Commander Spear，随后下砸并爆炸；治疗型守卫绕行并预警，然后射出长距离 Holy Crystal Spike。整个第一阶段会按固定段落重复这些攻击。

进入第二阶段后，战斗会依次经历不同魔法形态。`EnterFireFormBulletHell` 抬升熔岩并混合火球、余烬和岩石；环境炸弹和 `CleansingFireballBombardment` 让火球朝熔岩落下，并在接触场地底部后喷发；`ExplodingSpears` 制造成排爆炸长矛；螺旋炸弹与余烬进一步压缩空间。

之后 `EnterHolyMagicForm` 转入圣魔法段落。`RockMagicRitual` 使用交叉弹幕和绕行岩石，`ErraticMagicBursts` 提供不规则爆发，`DogmaLaserBursts` 释放激光。再之后 `EnterLightForm` 转入光形态，`FinalPhaseRadianceBursts` 同时抬升熔岩并组合炸弹、熔岩 Blob 和激光。完成后，第二阶段时间线继续重复。

生命低于约 4% 时进入死亡演出，而不是继续普通攻击。演出包括结晶、垂死太阳等视觉段落，最后才真正死亡。

### 弹幕与冲撞逻辑

`AcceleratingCrystalShard` 约以 `1.035` 倍加速，`HolyCinder` 约以 `1.026` 倍加速，`HolyCross` 约以 `1.024` 倍加速。基础圣火球会变大并加速。Holy Spear 死亡时会沿直线制造火柱，并在斜向排列中故意留出更宽缺口。Holy Crystal Spike 会向目标方向进行射线检测，延伸到方块或最大距离。Cleansing Fireball 的飞行时间经过计算，使其抵达熔岩后按节奏喷发。

Providence 的主体很少依赖传统连续冲锋。它的威胁来自固定竞技场、熔岩高度、编排式弹幕和守卫协同。玩家要识别当前魔法形态和时间线，而不是只观察 Boss 是否准备撞过来。

---

## Signus

### 召唤、开场与改写程度

Signus 使用 Rune of Kos 的现版本入口 `Mark of Providence`，在地狱召唤。Infernum 还安排了一个初次巡逻遭遇：Signus 可以先在 Profaned Garden 出现，击败前也可以在地狱自然生成。巡逻状态下它不是正式 Boss，不造成伤害、近乎隐形且无敌；会移动、传送，并在玩家接近后离开。对已经存在的巡逻个体使用召唤物，会把它直接唤醒为正式战斗。

主 AI 位于 `Content/BehaviorOverrides/BossAIs/Signus/SignusBehaviorOverride.cs`。阶段阈值是 70% 和 30%，但没有长时间转场，主要通过攻击权重、次数和速度变化提高压力。

### 阶段与攻击流程

`KunaiDashes` 会让 Signus 淡出传送、上升后冲锋，并释放会重新定向的苦无。`ScytheTeleportThrow` 让它传送、突进并发射镰刀扇。`ShadowDash` 先排线并显示预警，随后画面显著变暗，Boss 高速穿过玩家，留下斩击和会爆出苦无的 Cosmic Mine。

`FastHorizontalCharge` 类似高速水平横穿：Signus 先移动到玩家侧面，再直接通过，并发射中间留有缺口的苦无。`CosmicFlameChargeBombs` 则先上升，再冲锋并沿路径放下 Dark Cosmic Bomb。

低于 70% 后，宇宙火焰炸弹攻击加入选择器，Shadow Dash 权重上升。低于 30% 后，现有攻击进一步加速并增加数量。攻击选择器会避免连续重复同一种攻击。

### 弹幕与冲撞逻辑

Cosmic Kunai 初期减速并旋转，随后才瞄准最近玩家，因此它具有延迟锁定性质。Eldritch Scythe 先绕 Signus 运动，再向外发射。Dark Cosmic Bomb 会减速并在延迟后爆炸，Cosmic Mine 则把冲锋路径转化为后续苦无爆发。

Signus 几乎所有核心攻击都带位移或冲锋：苦无冲锋、镰刀传送突进、黑屏 Shadow Dash、水平横穿和落炸弹冲锋。它的设计重点是隐藏或延迟冲锋起点，再用延迟爆炸物阻止玩家沿刚才的躲避路线返回。

---

## Storm Weaver

### 召唤、开场与改写程度

Storm Weaver 使用 Rune of Kos 的现版本入口 `Mark of Providence`，在天空召唤。击败前它也可以自然出现在天空。自然遭遇时先进入 `HuntSkyCreatures`：没有正式 Boss 条和接触伤害，会制造暴风雨与雾，并猎杀天空生物。对现有个体使用召唤物会把它唤醒为正式战斗。

头部主 AI 位于 `Content/BehaviorOverrides/BossAIs/StormWeaver/StormWeaverHeadBehaviorOverride.cs`，身体和尾部也完整覆盖。源码明确移除了原 Calamity 的装甲第一阶段，Infernum 从开始就使用自己的完整蠕虫战斗。唯一硬阈值是 50%。

### 阶段与攻击流程

第一阶段使用固定六步循环，核心包括 `NormalMove`、`AimedLightningBolts`、`IceStorm` 和 `FakeoutCharge`。普通移动是蠕虫追逐；瞄准闪电让身体节段向玩家发射 Homing Weaver Spark；冰风暴让 Boss 围绕玩家运动，并从上方降下带预警的 Frost Wave；假冲锋则通过绕行、尾部闪电和火花制造“即将冲入”的压力。

低于 50% 后，固定循环中的部分攻击被替换。`BerdlyWindGusts` 让 Boss 绕玩家运动，并安排围绕选定中心旋转的风阵通道，同时给予玩家无限飞行。`FogSneakAttackCharges` 会借助浓雾淡出，重排或传送节段，然后从难以观察的位置突然冲锋并释放火花。后半场因此从公开的冰风暴和假动作，转为雾中偷袭与旋转风阵。

### 弹幕与冲撞逻辑

Homing Weaver Spark 先追踪，再以约 `1.026` 倍加速；普通 Weaver Spark 直线飞行并以约 `1.023` 倍加速。Wind Gust 绕选定中心运行，构成移动通道而不是直接锁定玩家。身体和尾部会主动参与发射闪电，因此不能只盯头部。

`SparkBurst` 在源码中有对应状态和执行方法，但正常攻击选择器不会选择它，应视为当前循环未接入的逻辑。Storm Weaver 的冲锋以“假动作”和“雾中突然出现”为主题；50% 后的偷袭冲锋才是最危险的直接撞击。

---

## Yharon

### 召唤、开场与改写程度

Yharon 沿用 `Yharon Egg`，没有额外生物群系限制。主 AI 位于 `Content/BehaviorOverrides/BossAIs/Yharon/YharonBehaviorOverride.cs`。生成后先播放登场效果，再进入第一次冲锋。

它的状态枚举包含普通冲锋、快速冲锋、火球爆发、喷火与流星、火焰龙卷、火焰路径冲锋、巨大火焰龙卷、传送冲锋、二阶段转换、地毯轰炸、凤凰超级冲锋、热浪圆环、火焰漩涡和最终咆哮。攻击选择不是一个简单循环，而是按生命和是否完成二阶段转换划分为八套模式表。

### 一阶段

一阶段高于 75% 时，在普通 `Charge` 和 `FastCharge` 之间穿插火焰龙卷、喷火与流星、火球爆发。普通冲锋会先调整到目标一侧再穿过；快速冲锋更短促，并可在路径上释放火焰。

75% 到 45% 之间，攻击表更偏向快速冲锋、`FireTrailCharge` 和 `MassiveInfernadoSummon`。火焰路径冲锋会留下持续区域；巨大龙卷攻击先让 Yharon 冲锋或调整位置，再创建大范围火焰龙卷。

45% 到 10% 之间，传送冲锋加入高频循环，并继续混合喷火、流星和巨大龙卷。`TeleportingCharge` 隐藏或改变冲锋出发点，因此后段不能只根据 Yharon 当前速度判断下一次冲线。

生命降到 10% 时，Yharon 不会死亡，而是进入约 360 帧的 `EnterSecondPhase` 演出。它在灰烬与飞离动画中无敌并恢复生命，之后进入第二阶段。

### 二阶段

二阶段高于 80% 时，攻击表加入 `CarpetBombing`，并与火焰路径冲锋、快速冲锋、传送冲锋和巨大龙卷组合。地毯轰炸让 Yharon 横向掠过，同时投下火球或流星。

80% 到 40% 之间开始反复使用 `PhoenixSupercharge` 和 `HeatFlashRing`。凤凰超级冲锋让本体进入更强烈的火焰形态并高速冲过；热浪圆环期间 Yharon 暂停接触伤害，移动到玩家上方，在玩家周围生成热焰圆环，并在玩家运动方向布置额外密集区。

40% 到 15% 之间加入 `VorticesOfFlame`。火焰漩涡先预警，之后周期性发射追踪 Vortex Fireball，并从侧面混入流星。15% 到 2.5% 之间，攻击表几乎只剩连续凤凰超级冲锋，是最纯粹的高速冲撞狂暴段。

低于 2.5% 后进入 `FinalDyingRoar`。两个热浪幻影围绕玩家并参与冲锋，随后本体和幻影共同进行覆盖竞技场的地毯轰炸，最终淡化为火花并爆炸，战斗才结束。

### 弹幕与冲撞逻辑

Infernado Spawner 会先追踪玩家，接近到合适距离后才建立龙卷。Lingering Dragon Flame 会逐渐减速，负责把冲锋路径变成持续危险区。Vortex 会周期性发射追踪火球；喷火弹幕附着在 Yharon 口部，并向外释放会重新定向的流星；凤凰花瓣则漂移和下落。

Yharon 是整组中冲锋占比最高的 Boss 之一。阶段提升的核心不是简单把同一次冲锋加速，而是依次加入火焰路径、巨大龙卷、传送起点、凤凰形态、热浪圆环和幻影。玩家必须把每次冲锋留下的持续火焰当作下一次冲锋的场地条件来处理。

---

## 代码入口索引

以下是主 AI 的直接入口，适合继续逐状态阅读：

- `Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeHeadBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/AstrumAureus/AstrumAureusBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusHeadBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Cryogen/CryogenBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/HiveMind/HiveMindBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/OldDuke/OldDukeBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Perforators/PerforatorHiveBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlaguebringerGoliathBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Polterghast/PolterghastBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Providence/ProvidenceBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Signus/SignusBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/StormWeaver/StormWeaverHeadBehaviorOverride.cs`
- `Content/BehaviorOverrides/BossAIs/Yharon/YharonBehaviorOverride.cs`

特殊召唤与遭遇入口：

- `Core/GlobalInstances/GlobalNPCSpawning.cs`
- `Core/ILEditingStuff/MechanicHooks.cs`
- `Content/BehaviorOverrides/BossAIs/AstrumDeus/DeusSpawnerBehaviorOverride.cs`
- `Content/Items/SummonItems/BloodwormPlatter.cs`
- `Content/Tiles/Profaned/ProvidenceSummoner.cs`
- `Content/Projectiles/Generic/ProvidenceSummonerProjectile.cs`
- `Core/GlobalInstances/Systems/CeaselessVoidArchivesSpawnSystem.cs`
