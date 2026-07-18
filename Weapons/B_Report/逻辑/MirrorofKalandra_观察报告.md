# Mirror of Kalandra（卡兰德之镜）逻辑观察报告

> 文件位置：`Items/Weapons/Summon/MirrorofKalandra.cs`  
> 召唤物目录：`Projectiles/Summon/MirrorofKalandraMinions/`

---

## 一、武器整体结构概述

Mirror of Kalandra 是一把**捐赠者道具**，归类为召唤武器（`DamageClass.Summon`），但它在 Calamity 召唤武器群体中有一个非常显著的特点：**它不是反复召唤同一种小怪，而是每次使用召唤出一个完全不同的小怪，共 5 种，召满之后武器失效**。

5 个召唤物：

| 召唤顺序 | 名称 | 类型 |
|---|---|---|
| 1 | AtzirisDisfavor | 旋转斧（冲撞型） |
| 2 | Starforge | 旋转体（冲撞型 + 爆炸副技） |
| 3 | Paradoxica | 弯刀（传送刺穿型） |
| 4 | WindRipper | 弓（固定炮台型） |
| 5 | HopeShredder | 弓（固定炮台型 + 分裂箭） |

---

## 二、召唤逻辑的特殊性

### 2.1 分阶段解锁式召唤

`Shoot()` 的逻辑并不是简单地"再召唤一个同样的"，而是根据玩家当前已拥有的小怪类型，决定下一个要生成什么：

```
第 1 次使用：未拥有 AtzirisDisfavor → 生成 AtzirisDisfavor
第 2 次使用：已拥有 AtzirisDisfavor → 检查剩余，优先生成 Starforge
第 3 次使用：已拥有 Starforge → 检查剩余，优先生成 Paradoxica
第 4 次使用：已拥有 Paradoxica → 检查剩余，优先生成 WindRipper
第 5 次使用：已拥有 WindRipper → 生成 HopeShredder
```

代码里的实现用的是一串**顺序 if 覆盖**（不是 else-if），最后一个成立的条件会覆盖之前的选择：

```csharp
type = ModContent.ProjectileType<HopeShredder>(); // 默认
if (WindRipper 未拥有) type = WindRipper;
if (Paradoxica 未拥有) type = Paradoxica;  // 覆盖
if (Starforge  未拥有) type = Starforge;   // 再覆盖
```

所以实际生成顺序是 Starforge → Paradoxica → WindRipper → HopeShredder（优先级从高到低）。

### 2.2 硬性上限：HopeShredder 控制武器失效

```csharp
public override bool CanUseItem(Player player) =>
    player.ownedProjectileCounts[HopeShredder] != 1;
```

一旦 HopeShredder 被召唤出来（count == 1），武器就完全不可使用。这个上限不是靠插槽数判断的，而是直接检测特定类型的存在数量，是一种**主动锁定**机制。

### 2.3 共享 Buff，各自独立续命

所有 5 个召唤物都调用同一个 Buff（`KalandraMirrorBuff`），并在自己的 `CheckMinionExistence()` 里独立处理自身的 `timeLeft = 2` 续命逻辑。每个小怪每帧都会 `Owner.AddBuff(KalandraMirrorBuff, 3600)` 刷新 buff——只要有任何一个还活着，整个 buff 就不会断。这是 Calamity 多小怪召唤武器的标准共享 buff 模式，本身并无特别之处。

---

## 三、三个"近战型"召唤物的逻辑对比

这三个小怪都是武器本体直接造成伤害（`penetrate = -1`，通过碰撞打怪），但三者的移动和攻击逻辑完全不同。

---

### 3.1 AtzirisDisfavor（斧头）— 纯冲撞型

**核心逻辑**：旋转朝向目标，用自身旋转角方向驱动速度。

```csharp
Projectile.velocity = Projectile.rotation.ToRotationVector2()
    * (MinRamSpeed + (12f / (distanceToTarget * 0.01f)));
Projectile.rotation = Projectile.rotation.AngleTowards(
    Projectile.AngleTo(Target.Center), 0.001f * distanceToTarget);
```

- **速度公式** `MinRamSpeed + 12f / (dist * 0.01f)` 意味着：目标越近，速度越快，防止近距离绕圈；目标越远，速度约等于 MinRamSpeed（30）。
- `AngleTowards` 的转向速率是 `0.001f * distanceToTarget`：**距离越远转得越快**，越近越慢，给了它一种"远距甩尾，近距贴身"的观感。
- 碰撞冷却（iframes）= 20 帧，适中。
- 无任何副技，就是纯粹的飞斧乱撞。
- 闲置时以玩家为中心做正弦摆动（`sin(Oscillation) / OscillationRange`），8 个振幅单位让晃动幅度较小。

**在 Calamity 中的同类参照**：这种"旋转角驱动速度 + AngleTowards 转向"模式在许多冲撞型小怪里都有出现，是最基础的战士型 AI 框架。

---

### 3.2 Starforge（紫色旋转体）— 冲撞 + 延迟爆炸型

**核心逻辑**：冲撞逻辑与 AtzirisDisfavor 完全相同（最小速度 40，最大速度 60，更快）。差异在于附加了**蓄力爆炸副技**：

```csharp
// 在 AI 中每帧累积计时器：
TimerToBoom++;

// 在 OnHitNPC 里触发爆炸：
if (TimerToBoom >= Purple_BlastFireRate /* 240帧 */ && Main.myPlayer == Projectile.owner)
{
    Projectile.NewProjectile(..., StarforgeBlast, damage * 2f, ...);
    TimerToBoom = 0f;
}
```

- 计时器只在**有目标时**（`Target is not null` 的分支里）累积，闲置时不增加。
- 每 240 帧（4 秒战斗时间）蓄满，下一次命中触发爆炸。
- `StarforgeBlast` 是一个**不可见的、半径 300px 的 AOE 弹幕**，存在 10 帧，击中所有范围内敌人，伤害 = 本体伤害 × 2。
- iframes = 28，比 AtzirisDisfavor 更长，这合理——它需要时间蓄积计时器，所以单次打击的伤害期望被设计成更低，用爆炸来补偿。
- 绘制时自旋方向与 AtzirisDisfavor 相反（`DrawSpin -=`），视觉上形成区分。

**关键副技结构**：`TimerToBoom` 放在 `ai[0]`，仅在 `OnHitNPC` 触发效果，计时和伤害逻辑完全分离。这是一种常见的"积累触发"副技模式。

---

### 3.3 Paradoxica（弯刀）— 传送 + 循环刺穿型

这个小怪的逻辑与前两者**完全不同**，是三者里最复杂的。

#### 首次接触目标：瞬间传送

```csharp
if (!hasTeleported)
{
    // 散布光尘，然后直接设置位置到目标中心
    Projectile.Center = Target.Center;
    hasTeleported = true;
}
```

第一次发现目标时，不是飞过去，而是**直接传送到目标身上**，并播放粒子效果。这个 `hasTeleported` 状态通过 `SendExtraAI / ReceiveExtraAI` 同步给多人（是本武器 5 个小怪里唯一需要额外网络同步的）。

#### 传送后的循环刺穿攻击（66帧循环）

攻击周期分为三个阶段（代码注释来自 "Virid Vanguard @ DoBehaviour_RegularPierceSlashes()"，即直接移植了 Virid Vanguard 的攻击逻辑，周期延长为 1.5 倍）：

```
循环周期 = 66 帧（MaxUpdates=2，实际体感约 33 帧 = 0.55秒）

Phase 1 (0% ~ 40%):  快速移动到起始位置（目标附近随机偏移80px内），并向上上升200px
Phase 2 (40% ~ 54%): 快速 Lerp 穿刺向目标（冲刺）
Phase 3 (54% ~ 100%): 穿过目标，到达目标"身后"的延伸点
```

- `MaxUpdates = 2`：每帧执行两次 AI，使弯刀的移动速度看起来是普通弹幕的两倍，但 iframes 设为 40 时，实际有效 iframes 只有 20（因为 MaxUpdates 会让 localNPCHitCooldown 消耗更快）。
- 起始位置每周期随机化（`ChargeStartingPosition = Target.Center + NextVector2Circular(80, 80)`），使每次刺穿角度不固定。
- 在无目标时，`hasTeleported = false` 重置，`AITimer = 0`，`extraUpdates = 0`——完全归零，等待下次目标。

**与前两者的本质区别**：AtzirisDisfavor 和 Starforge 是**持续追踪**（每帧按速度向目标方向运动），Paradoxica 是**基于时间插值的分段动画**（用 `GetLerpValue` + `Vector2.Lerp` 在固定时间内完成一次完整的刺穿动作），本质是把攻击过程参数化为一个 0→1 的进度值，然后在该进度值上插值位置。

---

## 四、两把"弓型"召唤物的逻辑对比

弓和近战型的最大区别：**弓本体不造成伤害（`CanDamage() => false`）**，伤害完全由它们发射的子弹承担（`MinionShot[Type] = true`）。它们也**不追踪目标**，始终固定在玩家附近特定位置。

---

### 4.1 WindRipper（弓·风系）— 预测瞄准，直射

**位置逻辑**（始终执行，无论有无目标）：

```csharp
Projectile.Center = Vector2.Lerp(Projectile.Center,
    Owner.Center + (-PiOver2 - PiOver4 * 1.5f).ToRotationVector2()
    * (IdleDistanceFromPlayer + IdleDistanceFromPlayer * (sin(Oscillation) / OscillationRange)),
    0.4f);
```

固定角度 `-PiOver2 - PiOver4 * 1.5` ≈ 247.5°，即玩家**左上方偏左**位置（大约是10点钟方向）。0.4 的 Lerp 系数意味着响应速度较快，基本实时跟随玩家。

**射击逻辑**：
- 7 帧动画，`frameCounter % BowChargeTime(5)` 每 5 帧推进一帧
- 第 5 帧触发射击：生成 `WindRipperArrow`
- 瞄准方式：`CalculatePredictiveAimToTargetMaxUpdates`，预测瞄准（根据目标速度和箭矢速度提前量）
- `WindRipperArrow`：直线飞行，`MaxUpdates = 10`（等效飞行速度很高），命中/消亡无特殊效果

---

### 4.2 HopeShredder（弓·邪恶系）— 预测瞄准，分裂箭

**位置逻辑**：

```csharp
Owner.Center + (-PiOver2 + PiOver4 * 1.5f).ToRotationVector2() * ...
```

固定角度 `-PiOver2 + PiOver4 * 1.5` ≈ 22.5°，即玩家**右上方偏右**（大约是2点钟方向）。与 WindRipper 关于玩家头顶轴线**对称分布**，两把弓形成一左一右的对称阵型。

**差异一：射速更慢**  
`BowChargeTime = 8`（WindRipper 是 5），7 帧动画总计 56 帧（≈0.93秒）完成一个射击周期，WindRipper 是 35 帧（≈0.58秒）。

**差异二：箭矢分裂**  
`HopeShredderArrow` 在 `OnKill` 时（命中目标或消亡）生成 3 枚 `HopeShredderArrowSplit`：

```csharp
for (int i = -SplitSpreadAngle; i < SplitSpreadAngle * 2; i += SplitSpreadAngle)
    // i = -8, 0, +8（度）
```

每枚分裂箭伤害 = 主箭 × 0.33，iframes = 30，飞行方向在主箭方向基础上分别旋转 -8°、0°、+8°。从设计意图来看这把弓单次发射伤害预期比 WindRipper 低，靠分裂箭对密集群体补偿输出。

**差异三：视觉残影**  
HopeShredder 的 PreDraw 在主体前后各 5px 额外绘制两个半透明深蓝影像，WindRipper 无此效果。

---

## 五、与 Calamity 中同类型小怪的横向比较

### 5.1 冲撞型（AtzirisDisfavor / Starforge）

"旋转角 + AngleTowards 驱动速度"的冲撞型 AI 在 Calamity 召唤武器中是最常见的战士框架。

#### DazzlingStabber（`Projectiles/Summon/DazzlingStabber.cs`）

有三种变体（Crystal、Stone、Fire），核心冲撞方式与 AtzirisDisfavor 相似：

```csharp
Projectile.velocity = Projectile.velocity.ToRotation()
    .AngleTowards(angleToTargetCoords, angularTurnSpeed)
    .ToRotationVector2() * Projectile.velocity.Length();
```

注意区别：DazzlingStabber 保持的是**速度的量（Length）**不变，只旋转方向；AtzirisDisfavor 是每帧根据距离**重新计算速度大小**。前者惯性感更强，后者更直接。DazzlingStabber 还有多段命中机制（每冷却周期最多 6 次）和护甲穿透，攻击节奏上更复杂。

#### EnchantedKnifeSummon（`Projectiles/Summon/EnchantedKnifeSummon.cs`）

混合型：进入战斗时会传送到攻击位置（与 Paradoxica 有相似之处），然后绕目标做弧形摆动，对齐后发起一次冲刺。这是一个"传送 + 弧形蓄力 + 冲刺"的三段模式，比纯冲撞复杂，但最终命中阶段的速度驱动方式和 Kalandra 的战士型类似。

**与 AtzirisDisfavor / Starforge 的共同点**：`AngleTowards` 转向 + `ToRotationVector2()` 速度 + `penetrate = -1` 无限穿透。  
**区别**：Kalandra 的冲撞型没有"冷却状态机"，它是**持续性的**，始终朝目标冲；DazzlingStabber 是周期性的，每轮攻击后有重置。

与这些相比，AtzirisDisfavor / Starforge 的**距离自适应速度**（`12f / (dist * 0.01f)` 项）是一个针对"绕圈"问题的直接工程解法：靠近时加速强行突破，不需要额外的状态机切换逻辑。实现更简单，但在极端情况下会出现"过冲—回头—过冲"的抖动。

---

### 5.2 循环刺穿型（Paradoxica）

Paradoxica 的代码里有明确注释：

```csharp
// Code from Virid Vanguard @ DoBehaviour_RegularPierceSlashes().
// 1.5x as long as Virid Vanguard.
```

#### ViridVanguardBlade（`Projectiles/Summon/ViridVanguardBlade.cs`）—— Paradoxica 的逻辑来源

`RegularPierceSlashes` 行为的核心代码（位于 `ViridVanguardBlade.cs` 约 375 行）：

```csharp
if (AITimer % attackCycleTime == 1f)
    ChargeStartingPosition = Projectile.Center + Main.rand.NextVector2Circular(80f, 80f);

float pierceCompletion = Utils.GetLerpValue(upwardRiseTimeRatio, upwardRiseTimeRatio + pierceTimeRatio, attackCompletion, true);
float throughTargetCompletion = Utils.GetLerpValue(upwardRiseTimeRatio + pierceTimeRatio, 1f, attackCompletion, true);
Projectile.Center = Vector2.Lerp(startingPosition, Target.Center, pierceCompletion);
if (throughTargetCompletion > 0f)
    Projectile.Center = Vector2.Lerp(Target.Center, endingPosition, throughTargetCompletion);
```

Paradoxica 的攻击循环**逐行**来自此处，周期由 44 帧扩大为 66 帧（1.5 倍）。  
区别在于 Paradoxica **加了初始传送**：Virid Vanguard 是从附近位置飞过来的，而 Paradoxica 会在第一次检测到目标时直接 `Projectile.Center = Target.Center`，省去飞行时间，对远距离目标响应更快。同时 Paradoxica 没有 Virid Vanguard 的多状态机（Virid Vanguard 还有水平斩、垂直斩等多种状态），只有一个循环刺穿模式。

#### CalamarisLamentMinion（`Projectiles/Summon/CalamarisLamentMinion.cs`）—— "贴附型"刺穿变体

另一种思路：不是"穿过目标再回头"，而是"追上目标后贴在上面持续伤害"：

```csharp
if (!Projectile.getRect().Intersects(Target.getRect()))
    // 追赶逻辑（高惯性）
else
    Projectile.velocity *= 0.2f; // 贴附，剧烈减速
```

贴附时伤害乘 1.5x。这和 Paradoxica 的"穿透后飞出"是两种截然不同的哲学：Paradoxica 每 0.55 秒做一次干净的刺穿，Calamaris 持续叠伤。

---

### 5.3 固定炮台型（WindRipper / HopeShredder）

#### CosmilampMinion（`Projectiles/Summon/CosmilampMinion.cs`）—— 最接近的同类

`CanDamage() => false`，悬停在玩家身边，靠 `NewProjectile` 打输出——结构与两把弓最为相近。关键差异：

- Cosmilamp 多个副本**错相位射击**（`HoverOffsetInterpolant` 控制每个副本的触发偏移），形成波浪式弹幕；WindRipper/HopeShredder 是两个独立的炮台，各自独立按帧计数，没有相互协调的设计。
- Cosmilamp 用 `timeLeft % BeamShootRate` 决定射击时机；WindRipper/HopeShredder 用 7 帧动画的第 5 帧触发，把**动画和射击绑定**在一起，有更清晰的视觉反馈。

#### RustyDrone（`Projectiles/Summon/RustyDrone.cs`）—— 炮台型的另一种写法

同样 `CanDamage() => false`，12 帧动画，使用 `timeLeft % ReleaseRate` 触发射击，悬停时有正弦波动：

```csharp
Projectile.velocity = -Vector2.UnitY * Math.Sin(TwoPi * timeLeft / 96f) * 3f;
```

与 WindRipper/HopeShredder 的区别：RustyDrone 是**无目标的全方位扩散**（发射 `RustyBeaconPulse` 向四周扩散），而两把弓是有**预测瞄准**的定向射击（`CalculatePredictiveAimToTargetMaxUpdates`）。预测瞄准是 WindRipper/HopeShredder 相较于 Calamity 大多数炮台型小怪的明显优势，对高速移动目标命中率显著更高。

---

## 六、整体设计总结

### 这件武器的逻辑有哪些是"不同的"

1. **异构多小怪（最显著）**：5 个召唤物没有任何一个的 AI 是相同的，涵盖了冲撞、爆炸、传送刺穿、直射弓、分裂弓五种不同逻辑体系。这在 Calamity 所有召唤武器里极为罕见——几乎所有其他多小怪召唤武器都是同一 AI 的多个实例。

2. **分阶段解锁式召唤**：每次使用释放不同小怪，全部召出后武器锁死。大多数召唤武器是"每次使用 +1 个同类型小怪直到插槽满"，这里的机制完全反转了：不是插槽限制你，而是**武器主动在合适的时机停止接受输入**。

3. **炮台型小怪固定方位编排**：WindRipper 和 HopeShredder 的悬停位置是精心设计的对称点（左上 vs 右上），构成了一个有意图的视觉阵型，而不是随意悬停在玩家周围某处。

4. **Paradoxica 的多人同步**：其他 4 个小怪都不需要 `SendExtraAI/ReceiveExtraAI`，只有 Paradoxica 因为用了基于本地布尔值（`hasTeleported`）和本地向量（`ChargeStartingPosition`）的状态，需要显式同步。这是分段 Lerp 攻击方式的代价——状态更复杂，同步需求也更高。

### 哪些是和其他召唤武器一样的

- **Buff 续命机制**（`timeLeft = 2` + 每帧 AddBuff）：标准 Calamity 小怪存活模式，无区别。
- **`MinionHoming` 获取目标**：全 5 个小怪都用同一个工具方法，与 Calamity 其他召唤物一致。
- **冲撞型 AI 框架**（AtzirisDisfavor / Starforge）：是 Calamity 里最常见的近战小怪骨架，只是参数和副技不同。
- **炮台射击型 AI 框架**（WindRipper / HopeShredder）：同样是常见模板，WindRipper 几乎可以看作最标准的炮台型写法。
- **`originalDamage` 传递**：通过 `minion.originalDamage = Item.damage` 保证缩放一致，是标准做法。
