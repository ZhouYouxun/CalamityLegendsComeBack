# Punctus 观察报告：蓄力标枪、手臂动画与环绕岩石逻辑

## 1. 总体判断

Punctus 是 InfernumMode 里非常完整的一把“命中积攒资源 + 右键释放资源”的武器。它表面上是一把从亵渎守卫掉落的标枪/长矛，但实际由三层系统共同完成：

- `Punctus` 物品本体：处理左右键入口、是否允许发射、基础数值。
- `PunctusProjectile` 长矛弹幕：处理手持蓄力、手臂动画、投出、命中、生成岩石。
- `PunctusRock` 岩石弹幕：处理从碎片聚合、围绕玩家旋转、右键瞄准、齐射、强化追踪。
- `PunctusPlayer` 玩家状态：维护最多 6 个岩石槽位和全局旋转计时器。

它最值得注意的地方不是单个弹幕很复杂，而是它把“手持动画”和“资源弹幕”拆得很清楚。长矛只负责制造和触发岩石；岩石自己负责形成、环绕、蓄力和发射；玩家状态只负责让所有岩石共享一个稳定的环绕时间。

另外，用户看到的“手指/手持往后收缩、手臂慢慢动、发光后丢出”并不是独立骨骼动画。Terraria 没有给武器写手指骨骼。这里是用 `SetCompositeArmFront` 改前臂角度，再同步移动长矛弹幕位置，视觉上模拟出“手握长矛向后拉满”的动作。

## 2. 主要源码位置

- `Content/Items/Weapons/Melee/Punctus.cs`
- `Content/Projectiles/Melee/PunctusProjectile.cs`
- `Content/Projectiles/Melee/PunctusRock.cs`
- `Core/GlobalInstances/Players/PunctusPlayer.cs`
- `Core/GlobalInstances/GlobalNPCLoot.cs`

掉落挂在 `ProfanedGuardianCommander` 上，并且要求 `InfernumMode.CanUseCustomAIs` 为 true。也就是说它确实是亵渎守卫相关掉落。

## 3. 物品本体：左右键入口

`Punctus.cs` 里设置：

- `Item.damage = 950`
- `DamageClass.Melee`
- `useAnimation = useTime = 32`
- `Item.channel = true`
- `Item.shoot = PunctusProjectile`
- `Item.shootSpeed = 45f`
- `Item.noUseGraphic = true`
- `Item.noMelee = true`
- `ItemID.Sets.Spears[Item.type] = true`
- `ItemID.Sets.BonusAttackSpeedMultiplier[Item.type] = 0.33f`

`HoldItem` 每帧做两件事：

```cs
Item.channel = true;
player.Calamity().rightClickListener = true;
```

这说明右键不是靠原版物品使用逻辑自然得到的，而是开启 Calamity 的右键监听，让后续弹幕能读 `Owner.Calamity().mouseRight`。

`AltFunctionUse` 返回 true，因此右键会走 alt function。

`Shoot` 里把左键/右键压成一个 `useType`：

```cs
int useType = 0;
if (player.altFunctionUse == 2)
    useType = 1;

Projectile.NewProjectile(..., velocity.SafeNormalize(Vector2.UnitY), type, ..., player.whoAmI, useType);
```

这个 `useType` 进入 `PunctusProjectile.ai[0]`，也就是：

- `0 = NormalThrow`
- `1 = RockThrow`

注意：物品发射时只传单位方向，不乘 `shootSpeed`。真正的 45 速度是在长矛“蓄力完成并释放”时才乘上去。

`CanUseItem` 会扫描已有的 `PunctusProjectile`，如果同玩家已经有一个长矛处于 `Aiming` 状态，就禁止再创建。这样可以防止玩家同时拉出多把正在蓄力的长矛。

## 4. 长矛状态机

`PunctusProjectile` 有两组枚举。

使用模式：

- `NormalThrow`：左键普通投矛。
- `RockThrow`：右键配合已有岩石齐射。

使用状态：

- `Aiming`：手持蓄力。
- `Firing`：投出飞行。
- `Hit`：命中后淡出。

关键常量：

- `PullbackLength = 30`，蓄力/后拉用 30 帧。
- `FadeOutLength = 10`，命中后 10 帧淡出。
- `TintLength = 10`，拉满后的发光提示以 10 帧为基本窗口。
- `MinRocksForHoming = 3`，至少 3 块环绕岩石时，左键投矛获得追踪。
- `MaxCirclingRocks = 6`，最多 6 块岩石。

每帧 AI 根据 `CurrentState` 调用：

- `DoBehavior_Aim`
- `DoBehavior_Fire`
- `DoBehavior_Hit`

如果状态仍是 `Aiming`，`Projectile.timeLeft` 被重置为 240，防止长矛还没丢出去就消失。若是普通投矛且带追踪，则每帧把当前位置塞进 `OldPositions`，后续用来画火焰拖尾。

## 5. 手持/手臂动画到底怎么做

### 5.1 拉弓进度

动画的核心变量是：

```cs
PullbackCompletion => Utilities.Saturate(Timer / PullbackLength)
```

`PullbackLength` 是 30，所以：

- 第 0 帧：`PullbackCompletion = 0`
- 第 15 帧：`PullbackCompletion = 0.5`
- 第 30 帧及以后：`PullbackCompletion = 1`

这是所有“慢慢往后收缩”的时间基础。

### 5.2 瞄准方向平滑

只有本地 owner 会更新瞄准方向：

```cs
float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Main.MouseWorld), true);
Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Main.MouseWorld), aimInterpolant);
```

鼠标离玩家越远，`aimInterpolant` 越接近 1，方向越快贴向鼠标。鼠标太近时插值更小，防止近距离方向乱跳。这里的 `Projectile.velocity` 仍然不是最终飞行速度，而是手持阶段的单位瞄准方向。

### 5.3 前臂角度公式

长矛先算自身旋转：

```cs
Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
```

然后玩家转向：

```cs
Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
```

真正的手臂后拉在这一行：

```cs
float frontArmRotation = Projectile.rotation - PiOver4 - PullbackCompletion * Owner.direction * 0.74f;
if (Owner.direction == 1)
    frontArmRotation += Pi;
```

拆开看：

- `Projectile.rotation - PiOver4` 把贴图旋转还原回瞄准方向。
- `PullbackCompletion * Owner.direction * 0.74f` 是后拉角度。
- `0.74f` 弧度大约是 42.4 度。
- 随着 `PullbackCompletion` 从 0 到 1，玩家前臂最多转动约 42 度。
- `Owner.direction` 让左右朝向下后拉方向相反。
- 右朝向时额外 `+ Pi`，这是适配 Terraria composite arm 朝向的处理。

也就是说，玩家手臂不是突然变成蓄力姿势，而是在 30 帧内从原始瞄准姿势逐渐转到后拉姿势。

### 5.4 长矛位置也跟着手臂后拉

光转手臂还不够，长矛弹幕自身的位置也同步变化：

```cs
Projectile.Center =
    Owner.Center
    + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 16f
    + Projectile.velocity * Projectile.scale * 40f;
```

这行有两个偏移：

- `(frontArmRotation + PiOver2).ToRotationVector2() * 16`：沿手臂垂直/握持方向偏移，让长矛贴在手的位置。
- `Projectile.velocity * 40`：沿瞄准方向往前伸出 40 像素，保证长矛尖端在玩家前方。

因为 `frontArmRotation` 会随 `PullbackCompletion` 变化，所以长矛中心也会跟着手臂慢慢回撤。玩家看到的“手和武器一起往后收缩”就是这个公式和 `SetCompositeArmFront` 共同制造的。

### 5.5 锁住玩家使用动画

手持阶段每帧做：

```cs
Owner.heldProj = Projectile.whoAmI;
Owner.SetDummyItemTime(2);
Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
```

`heldProj` 让游戏知道玩家当前持有这个弹幕。`SetDummyItemTime(2)` 让玩家保持使用物品姿态。`SetCompositeArmFront` 则真正把前臂转到计算出的角度，并且伸展程度是 `Full`。

所以这里没有单独的手指动画，只有前臂 composite arm + 长矛弹幕位置。手指/手持感来自“手臂旋转”和“武器跟随手臂偏移”。

### 5.6 松开按键也要等拉满才发射

发射条件是：

```cs
if (CurrentMode is UseMode.NormalThrow ? !Owner.channel : !Owner.Calamity().mouseRight)
{
    if (PullbackCompletion == 1f)
    {
        ...
        CurrentState = UseState.Firing;
        Projectile.velocity *= Owner.HeldItem.shootSpeed;
        Timer = 0f;
    }
}
```

左键模式看 `!Owner.channel`，右键模式看 `!Owner.Calamity().mouseRight`。但即使玩家提前松开，也必须等 `PullbackCompletion == 1f` 才真正投出。也就是说，提前松手不会立刻失败，而是排队到 30 帧蓄力完成后发射。

发射瞬间会：

- 解除岩石计时暂停。
- `Owner.SetCompositeArmFront(false, ..., 0f)` 还原前臂。
- 播放 `PunctusThrowSound`。
- 播放 `SoundID.DD2_MonkStaffSwing`。
- 如果左键且已有至少 3 块被动岩石，则 `ShouldHome = true`。
- `Projectile.velocity *= Owner.HeldItem.shootSpeed`，把单位方向变成 45 速度。
- 状态切到 `Firing`，`Timer = 0`。

## 6. 长矛拉满后的发光提示

`PreDraw` 负责长矛本体绘制，其中有两层“准备好了”的视觉提示。

### 6.1 背后环形发光

先计算已有岩石：

```cs
int rockTotal = PassiveRocks(Owner);
float glowDistance = 2f;
if ((CurrentMode is NormalThrow && rockTotal >= 3 && CurrentState is not Aiming)
    || (CurrentMode is RockThrow && rockTotal == 6))
    glowDistance = 4f;
```

然后画 12 次长矛贴图副本：

```cs
Vector2 backglowOffset =
    (TwoPi * i / backglowAmount).ToRotationVector2()
    * glowDistance
    * Utilities.Saturate(Timer / 10f);
```

这个背光不是 shader，而是把同一张长矛贴图围绕中心画 12 次，每份偏移一点点，颜色用 `WayfinderSymbol.Colors[1]` 且 alpha 设为 0，制造发光边缘。`Timer / 10` 让背光在前 10 帧内从 0 半径长出来。

### 6.2 拉满后 BasicTintShader 闪光

真正的“拉满发光”条件是：

```cs
bool useTint =
    CurrentState is UseState.Aiming
    && PullbackCompletion >= 1f
    && Timer < PullbackLength + TintLength * 2f;
```

也就是：

- 必须仍在手持蓄力。
- 30 帧拉满。
- 只在第 30 到第 50 帧之间显示。

shader 强度：

```cs
float shiftAmount =
    Utils.GetLerpValue(30, 37.5, Timer, true)
    * Utils.GetLerpValue(50, 42.5, Timer, true);
```

第一个 lerp 负责 30 到 37.5 帧渐入，第二个反向 lerp 负责 42.5 到 50 帧渐出。中间几帧最亮。

然后：

```cs
BasicTintShader.UseSaturation(Lerp(0f, 0.55f, EasingCurves.Circ.InFunction(shiftAmount)));
BasicTintShader.UseOpacity(1f);
BasicTintShader.UseColor(WayfinderSymbol.Colors[0]);
```

这就是“先往后收，拉满后闪一下，再丢出去”的细节来源。发光不是常驻，而是拉满后的短窗口提示。

## 7. 长矛飞行与追踪

`Firing` 状态下：

- 长矛旋转保持 `Projectile.velocity.ToRotation() + PiOver4`。
- 每帧添加光照 `Lighting.AddLight`。
- 随机产生 `SquishyLightParticle`，粒子位置在长矛附近，速度大体朝反方向拖出，像高速运动漏出的能量。

如果 `ShouldHome` 为 false，长矛直线飞行。

如果 `ShouldHome` 为 true：

1. 找 250 像素内最近 NPC，优先 boss。
2. 新速度长度为当前速度 `* 1.032`，限制在 6 到 42。
3. 速度向目标方向 Lerp，强度 0.24。
4. 再用 `RotateTowards` 以 0.1 弧度角速度向目标方向旋转。
5. 最后 normalize 回新速度长度。

所以 Punctus 的左键追踪不是瞬间拐弯，而是加速并较柔和地贴向目标。触发条件是：左键普通投矛，且发射时已有至少 3 块被动岩石。

追踪状态还会通过 `DrawPixelPrimitives` 画拖尾：

- 仅 `NormalThrow + Firing + ShouldHome` 时绘制。
- 用 `PrimitiveTrailCopy`。
- shader 是 `CalamityMod:ImpFlameTrail`。
- 贴图是 `InfernumTextureRegistry.StreakMagma`。
- 宽度从 21 平滑到 5。
- 颜色从亮黄、暗红到近黑过渡。

## 8. 命中后生成岩石

长矛命中 NPC 后立即：

- `CurrentState = Hit`
- `Timer = 0`
- `Projectile.netUpdate = true`

然后计算长矛尖端：

```cs
float spearLength = Projectile.Size.Length();
Vector2 spearTip =
    Projectile.Center
    + Projectile.velocity.SafeNormalize(Vector2.Zero)
    * spearLength
    * 0.5f;
```

岩石生成点不是长矛中心，而是长矛尖端。这样玩家看到的是“扎中目标的位置碎裂出石头”。

随后只由 owner 负责生成岩石，避免多人重复生成：

1. 扫描已有 `PunctusRock`。
2. 只统计 `Circling` 或 `Aiming` 状态的岩石。
3. 如果已有岩石已经完成初始发光，则重置它们的 `Timer` / `timeLeft`，延长存在时间。
4. 如果当前不是右键岩石投掷，且岩石数量小于 6，则生成 1 块新岩石。
5. 如果 `ShouldCreateMoreRocks` 为 true，则最多再生成 3 块，但仍不超过 6 块。
6. 如果没有生成任何岩石，则只播放碎石粒子。

新岩石的 `ai0` 传入：

```cs
Tau * numberOfExistingRocks / MaxCirclingRocks
```

也就是按 6 等分给每块岩石一个初始角度。虽然 `PunctusRock` 实际环绕主要用 `IndexWeAre` 和全局 `RockTimer`，这个传参仍然体现了“均匀分布”的设计意图。

命中反馈还包括：

- `SoundID.DD2_LightningBugZap`
- `SoundID.DD2_ExplosiveTrapExplode`
- 长矛速度清零，像是钉在目标上。
- 屏幕震动：普通 2，强化生成更多岩石时 4。
- 10 个 `GlowyLightParticle`
- 10 个 `MediumMistParticle`
- 1 个 `StrongBloom`
- 8 个 `SparkParticle`

最后 `Hit` 状态下透明度用 10 帧淡出：

```cs
Projectile.Opacity = Utils.GetLerpValue(FadeOutLength, 0f, Timer, true);
```

## 9. 岩石状态机

`PunctusRock` 有三个状态：

- `Circling`：围绕玩家旋转。
- `Aiming`：右键蓄力时，岩石后撤并瞄准。
- `Firing`：松开右键后飞出。

关键常量：

- `CircleLength = 720`，岩石默认存在 720 帧，约 12 秒。
- `CrumbleWarningLength = 180`，最后 3 秒开始抖动提示快消失。
- `MaxCreationParticles = 15`，形成动画最多使用 15 个视觉碎片槽。
- `CreationParticleLifetime = 90`，碎片聚合动画 90 帧。
- `GlowStartTime = 70`
- `GlowEndTime = 110`
- `MinDamageMultiplier = 0.5`
- `MaxDamageMultiplier = 0.8`
- `MinShootSpeed = 30`
- `MaxShootSpeed = 50`
- `BuffedShootSpeed = 40`

有一个小细节：`BuffedDamageMultiplier = 0.8f` 被声明了，但当前发射代码没有直接使用这个常量。满 6 块时伤害倍率来自 `Lerp(0.5, 0.8, 1)`，也就是同样得到 0.8；满 6 只额外把速度设为 40 并开启 `RockIsBuffed`。

## 10. 岩石不是直接刷出来，而是从碎片聚合

`PunctusRock` 第一次 AI 时，弹幕中心还是长矛尖端。它利用这一帧制造“碎片从命中点飞出再聚合”的视觉。

初始化时：

- 生成 `Main.rand.Next(MaxCreationParticles - 5, MaxCreationParticles)` 个 `ProfanusRockParticle`，也就是大约 10 到 14 个碎片。
- 每个碎片位置从长矛尖端开始。
- 初速度沿长矛飞行方向，随机旋转 -0.5 到 0.5 弧度，速度 1 到 8。
- 每个碎片寿命 90 帧。
- 额外生成 15 个 `RockOffsets`，每个是半径 10 内随机偏移。
- 随后把真正岩石弹幕速度清零，避免岩石本体继续沿长矛方向飞。

形成动画在 `UpdateFormingRocks`：

1. 碎片最后 10 帧淡出。
2. 每帧按随机旋转速度旋转。
3. 前一段先向外漂：
   - 基础漂移时间 25 帧。
   - 叠加 `driftDelays`，形成 25、28、31、34、43 等不同漂移时长。
   - 漂移时 `Position += Velocity`，`Velocity *= 0.99`。
4. 漂移结束后，记录 `StartingPosition`。
5. 然后用 `EasingCurves.Sine.InFunction` 快速插值到：

```cs
Projectile.Center + RockOffsets[i]
```

这就是“碎片先炸开，再猛地聚到岩石本体附近”的效果。它不是只画岩石出现，而是先画一群独立的视觉粒子。

绘制这些碎片时还会：

- 每个碎片周围画 12 个小背光。
- 绘制普通碎片贴图。
- 在生命最后 30 帧叠加红热发光。
- 用 `BloomFlare` 快速放大再缩小，盖住真正岩石成形的瞬间。

## 11. 环绕岩石位置

岩石进入 `Circling` 后，每帧位置由：

```cs
Projectile.Center =
    Owner.Center
    - (Owner.GetModPlayer<PunctusPlayer>().RockTimer / 55f
      + Tau * IndexWeAre / 6)
      .ToRotationVector2()
      * 100f;
```

拆开：

- `RockTimer / 55f` 是所有岩石共享的旋转时间。
- `Tau * IndexWeAre / 6` 是第几块岩石的 60 度分布偏移。
- 半径固定 100 像素。
- 前面有一个负号，所以实际环绕方向与正向角度相反。

`IndexWeAre` 来自 `PunctusPlayer.RockSlots` 的第一个空槽。最多 6 个槽，所以最多 6 块岩石。

岩石刚生成时 `Projectile.Opacity = 0`。到 `Timer == CreationParticleLifetime - 10`，也就是第 80 帧，才设为 1。并且如果透明度低于 0.9，就不响应右键进入瞄准。这样可以防止还没形成完的岩石被立刻发射。

## 12. PunctusPlayer 如何统一环绕

`PunctusPlayer` 维护：

- `RockSlots = new int[6]`
- `PauseTimer`
- `CheckToResetTimer`
- `RockTimer`

`ResetEffects` 和 `UpdateDead` 都调用 `ResetRocks`。

如果 `PauseTimer` 为 false：

```cs
RockTimer++;
```

所有岩石都用这个 `RockTimer` 计算环绕位置，所以它们不会各转各的，而是永远保持 6 等分阵型。

当玩家右键蓄力岩石齐射时，`PunctusProjectile` 会把 `PauseTimer = true`。这会冻结所有环绕岩石的全局计时，避免它们在从环绕转向瞄准时继续绕圈导致位置漂移。

岩石进入 `Firing` 后，`PunctusPlayer` 会把对应槽位清成 -1，这样后续命中可以重新占用槽位。

## 13. 右键齐射：岩石如何后撤并发射

右键使用时，物品生成 `PunctusProjectile`，其 `CurrentMode = RockThrow`。长矛自己仍然会进入 `Aiming`，但周围岩石会在自己的 `Circling` 状态里扫描：

```cs
if (profanus.CurrentMode is RockThrow && profanus.CurrentState is Aiming)
```

只要找到这把右键蓄力长矛，岩石就：

- 播放 `VassalJumpSound`
- 记录当前相对玩家的位置 `AimingOffsetPosition = Projectile.Center - Owner.Center`
- 切到 `State.Aiming`
- `Timer = CircleLength - 300f`，也就是 420
- `timeLeft = 300`

进入 `Aiming` 后，岩石位置变成：

```cs
float aimBackStrength = Saturate((Timer - 420) / 20f);
Vector2 aimBackOffset = Projectile.SafeDirectionTo(mouseWorld) * -30f * aimBackStrength;
Projectile.Center = Owner.Center + AimingOffsetPosition + aimBackOffset;
```

这意味着：

- 岩石保留它进入瞄准瞬间的环绕偏移。
- 20 帧内沿“远离鼠标”的方向后撤 30 像素。
- 所有岩石都像被拉弓一样先往后压，再一起发射。

这里有个很细的代码事实：注释说岩石旋转会“逐渐变快”，但当前代码是：

```cs
Projectile.rotation += 0.2f * Utils.GetLerpValue(0f, 40f, Timer, true);
```

因为进入 Aiming 时 `Timer` 被设为 420，所以 `GetLerpValue(0, 40, 420, true)` 立刻就是 1。也就是说当前实际表现是进入瞄准后马上以 0.2 rad/frame 旋转，不是从 0 慢慢加速。真正有 20 帧渐变的是后撤距离 `aimBackStrength`。

松开右键，且 `aimBackStrength >= 1` 后，岩石发射：

1. 统计 `ActiveRocks`。
2. `interlopant = rockAmount / 6`。
3. 速度 `Lerp(30, 50, interlopant)`。
4. 伤害倍率 `Lerp(0.5, 0.8, interlopant)`。
5. 如果满 6 块：
   - 速度改成 40。
   - `RockIsBuffed = true`。
6. 朝鼠标方向发射。
7. `timeLeft = 300`。
8. 播放爆炸音。
9. 生成 20 组沙尘和火烟粒子。
10. 屏幕震动 2。
11. 状态切到 `Firing`。

满 6 块时速度不是 50，而是 40，但会开启 `RockIsBuffed`，后续获得 1200 像素索敌和追踪。

## 14. 岩石飞行、追踪和绘制

普通岩石飞出后：

- 每帧生成 `SandyDustParticle`。
- 每帧 `Projectile.rotation += 0.3f`。
- 只有 `RockIsBuffed` 才追踪。

强化岩石追踪逻辑：

```cs
NPC target = CalamityUtils.ClosestNPCAt(Projectile.Center, 1200f, true, true);
Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 40f, 0.055f);
```

追踪半径很大，Lerp 很低，所以它不是急转弯，而是稳定地向目标方向修正。

强化岩石绘制有专门的 `Gleam` 拖尾：

- 遍历 `Projectile.oldPos`。
- 每两帧位置之间画 4 个插值点，避免高速岩石拖尾断裂。
- 颜色从 `MagicSpiralCrystalShot.ColorSet[0]` 过渡到接近白色。
- alpha 设为 0，做发光式拖尾。

岩石本体每帧还会画：

- 12 份背光，半径 5。
- 主岩石贴图。
- 如果刚生成或强化，则额外叠画 2 次发光贴图。
- 如果还有形成碎片，就最后把碎片和 bloom 画在上面。

## 15. 岩石瞄准线

岩石处于 `Aiming` 时会画一条瞄准线：

- 使用 `InfernumTextureRegistry.Invisible` 作为绘制载体。
- 使用 `CalamityMod:PixelatedSightLine` shader。
- Additive 混合。
- `sampleTexture2` 是 `CertifiedCrustyNoise`。
- `noiseOffset = Main.GameUpdateCount * -0.003f`。
- `laserAngle = (mouseWorld - Projectile.Center).ToRotation() * -1f`。
- `laserWidth = 0.0015 + Pow(opacity, 5) * (Sin(time * 3) * 0.002 + 0.002)`。
- 主色是 `WayfinderSymbol.Colors[1]` 与 `Color.OrangeRed` 的插值。
- 暗色是 `WayfinderSymbol.Colors[2]`。

不过这里也有一个微妙细节：进入 `Aiming` 时岩石 `Timer` 是 420，所以：

```cs
opacity = Utils.GetLerpValue(0, 20, Timer, true)
```

会立即等于 1。也就是说瞄准线不是从 0 慢慢显现，而是一进入瞄准就基本满透明度。后撤动作有 20 帧渐变，但瞄准线没有。

## 16. 这把武器的交互闭环

完整循环可以这样理解：

1. 左键按下，生成长矛，进入 `Aiming`。
2. 30 帧内前臂和长矛一起后拉。
3. 拉满后长矛短暂染色发光。
4. 松开左键，长矛以 45 速度飞出。
5. 命中 NPC 后，长矛尖端生成 1 块岩石，最多存 6 块。
6. 岩石先以碎片形式从命中点散开，再聚合为真正岩石。
7. 岩石进入围绕玩家 100 像素半径旋转的状态。
8. 有 3 块以上岩石时，左键投矛获得追踪。
9. 右键按下，生成一把 `RockThrow` 长矛，同时所有已形成岩石从环绕切到瞄准。
10. 岩石保留环绕阵型，往鼠标反方向后撤 30 像素。
11. 松开右键后，岩石按当前数量决定速度和伤害倍率，一起朝鼠标飞出。
12. 满 6 块时，岩石变成强化状态，获得大范围追踪和额外拖尾。
13. 如果满 6 块右键长矛本体也命中，`ShouldCreateMoreRocks` 会尝试补生成最多 3 块，继续维持资源循环。

## 17. 可复刻重点

如果以后想借鉴 Punctus，最值得保留的是这些结构：

1. 左右键不要写成完全不同武器，统一成一个 `UseMode` 放进 `Projectile.ai[0]`。
2. 手持蓄力用 `Timer / PullbackLength` 做连续进度，而不是硬切帧。
3. 玩家前臂用 `SetCompositeArmFront`，武器弹幕位置用同一个 `frontArmRotation` 公式联动。
4. 发射条件可以允许玩家提前松手，但必须等 `PullbackCompletion == 1` 才真正投出。
5. 拉满反馈用短窗口 tint shader，避免常驻发光导致视觉疲劳。
6. 命中点用 spear tip，而不是 projectile center。
7. 资源物体单独做 projectile，让它们拥有自己的形成、环绕、瞄准、发射状态。
8. 多个资源物体共享一个 player timer，这样环绕阵型稳定。
9. 右键蓄力时暂停共享 timer，避免环绕物在瞄准过渡期间漂移。
10. “满资源强化”不一定只改伤害，也可以改追踪、拖尾、屏幕震动和生成逻辑。

Punctus 的手感不是靠单个大数值，而是靠 30 帧手臂后拉、拉满 tint、命中冻结、碎片聚合、100 像素环绕、右键后撤、满 6 追踪这些小动作叠出来的。每一层都很小，但合起来就有了“扎中后把石头收在身边，再一口气丢出去”的完整仪式感。
