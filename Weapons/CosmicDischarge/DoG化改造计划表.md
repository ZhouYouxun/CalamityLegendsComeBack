# CosmicDischarge 神明吞噬者化 · 完全改造计划表
## 【V2.0 · 粒子库全覆盖版 · 远不止Spark和Trail】

> 目标：彻底抹除所有冰霜/寒冰痕迹，用CalamityMod全粒子库重建每一个特效。
> 每个攻击形态拥有完全不同的视觉指纹。没有一个攻击用相同的粒子组合。
> 参考源：《神明吞噬者及其所有周边的特效完整分析.md》（V2.0）

---

# 第零章：全局色彩系统重构

## 0.1 颜色常量替换表

```csharp
// CosmicDischargeCommon.cs 中的常量全部替换：

// 旧常量 → 新常量
static readonly Color FrostCoreColor  = new(150, 255, 255);  // 删除
static readonly Color FrostGlowColor  = new(110, 175, 255);  // 删除
static readonly Color FrostDarkColor  = new(58,  84,  150);  // 删除
static readonly Color FrostWhiteColor = new(225, 250, 255);  // 删除

// 新增DoG颜色体系：
public static Color DoGColor =>
    Color.Lerp(Color.Fuchsia, Color.Cyan,
        MathHelper.SmoothStep(0, 1,
            (MathF.Sin(Main.GlobalTimeWrappedHourly * 2) + 1) * 0.5f));

public static readonly Color DoGCyan    = Color.Cyan;         // RGB(0,255,255)
public static readonly Color DoGFuchsia = Color.Fuchsia;      // RGB(255,0,255)
public static readonly Color DoGPurple  = new(136, 26, 186);  // 扭曲紫
public static readonly Color DoGSkyBlue = Color.SkyBlue;      // 被动火焰内层
public static readonly Color DoGWhite   = Color.White;         // 核心白光
public static readonly Color DoGAlice   = Color.AliceBlue;     // 三色Spark第三色

// 三色Spark公式（CosmicDischarge标准配方）：
public static Color ThreeColorSpark =>
    Color.Lerp(
        Color.Lerp(Color.Fuchsia, Color.AliceBlue, Main.rand.NextFloat(0.5f)),
        Color.Cyan, Main.rand.NextFloat());
```

## 0.2 Debuff系统替换

```csharp
// 删除 ApplyColdDebuffs (Nightwither + Frozen + Frostburn2 + Chilled)
// 新增 ApplyDoGDebuffs:
target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
// GodSlayerInferno: 已被CosmicShiv、DimensionTearingDisk、MawOfInfinity使用，确认可用
```

## 0.3 武器轨迹替换

```csharp
// 删除: BuildCurvedBlade (冰剑弧线) 的蓝白渐变色
// 新增: DoG双层火焰轨迹（参考DoGFire.cs）

// 外层（在IPixelatedPrimitiveRenderer层绘制）:
GameShaders.Misc["CalamityMod:ImpFlameTrail"].UseImage0(
    "CalamityMod/ExtraTextures/Trails/ScarletDevilStreak");
widthFunction = _ => 72f;
colorFunction = _ => mode == KillMode ? Color.Purple : DoGCyan;

// 内层:
GameShaders.Misc["CalamityMod:ImpFlameTrail"].UseImage0(
    "CalamityMod/ExtraTextures/Trails/SylvestaffStreak");
widthFunction = _ => 28f;
colorFunction = _ => mode == KillMode ? DoGFuchsia : DoGSkyBlue;

// 每帧附加（在AI的PerformEffects中）:
if (Main.rand.NextBool(2))
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        tipPosition + Main.rand.NextVector2Circular(10f, 10f),
        Main.rand.NextVector2Circular(2f, 2f), DoGColor, 0.4f, 15, emitsLight: true));
```

---

# 第一章：形态0——鞭形DoG（WhipMode）

> **视觉指纹**：鞭体 = SemiCircularSmearVFX弧线模糊 + HeavySmoke尾迹；
> 命中 = GlowSparkParticle散射 + FlameExplosion爆发；
> 蓄力关键帧 = ElectricSpark电弧

## 1.0 Combo-0：WhipOver（46帧，从上往下挥）

### 帧0-20（前摇蓄力阶段）
```csharp
// 每帧在鞭尖位置：ElectricSpark（电弧积蓄感）
GeneralParticleHandler.SpawnParticle(new ElectricSpark(
    whipTip + Main.rand.NextVector2Circular(15f, 15f),
    Main.rand.NextVector2Circular(3f, 3f),
    color:         DoGColor,
    bloom:         DoGFuchsia,
    scale:         0.6f,
    lifeTime:      12,
    maxJumpRotation: MathHelper.PiOver4,
    jumpTime:      6f,
    rotationSpeed: 1.5f));

// 每帧1个NanoParticle在鞭柄
GeneralParticleHandler.SpawnParticle(new NanoParticle(
    handlePos + Main.rand.NextVector2Circular(8f, 8f),
    Main.rand.NextVector2Circular(1.5f, 1.5f),
    DoGCyan, 0.5f, 18, emitsLight: true));
```

### 帧10-46（挥鞭弧线阶段）
```csharp
// 每帧：SemiCircularSmearVFX跟踪鞭体弧度
GeneralParticleHandler.SpawnParticle(new SemiCircularSmearVFX(
    whipCenter,
    velocity: Vector2.Zero,
    color:    DoGColor * 0.7f,
    rotation: whipAngle,
    squish:   new Vector2(1f, 1.3f),
    scale:    whipLength / 80f,
    lifeTime: 3));

// 每帧：HeavySmokeParticle在鞭尖（50%概率，制造烟尾）
if (Main.rand.NextBool(2))
    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
        whipTip + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 1.5f,
        DoGFuchsia * 0.6f, scale: 0.8f, lifeTime: 12, opacity: 0.5f));
```

### 命中时（OnHitNPC）
```csharp
// GlowSparkParticle散射（细长竖条向四面飞溅）
for (int i = 0; i < 10; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
        target.Center + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextVector2Circular(8f, 8f),
        gravity: true,
        lifetime: 14,
        scale: Main.rand.NextFloat(0.6f, 1.0f),
        color: Main.rand.NextBool() ? DoGCyan : DoGFuchsia,
        squash: new Vector2(0.25f, 1.8f),    // 极细长
        quickShrink: true));

// FlameExplosion爆发（×2，一Cyan一Fuchsia，方向错位）
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    target.Center, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.4f, 0.7f), rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.1f, finalScale: 0.7f, lifeTime: 14, opacity: 0.8f));
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    target.Center, Vector2.Zero, DoGFuchsia * 0.8f,
    squish: new Vector2(0.7f, 1.3f), rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.08f, finalScale: 0.55f, lifeTime: 12, opacity: 0.7f));

// StaticPulseRing×1
GeneralParticleHandler.SpawnParticle(new StaticPulseRing(
    target.Center, Vector2.Zero, DoGColor, Vector2.One, 0f,
    originalScale: 0.04f, finalScale: 0.45f, lifeTime: 14));

// GodSlayerInferno debuff
target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 1.1 Combo-1：WhipUnder（46帧，从下往上挥）

> **视觉区别**：改用Cyan为主色，SemiCircularSmearVFX + BoltParticle闪电爆发

### 帧0-46
```csharp
// 每帧：CircularSmearVFX（比SemiCircular更圆，Cyan主色）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    whipCenter, Vector2.Zero,
    color: DoGCyan * 0.6f,
    rotation: whipAngle + MathHelper.Pi,
    scale: whipLength / 70f,
    lifeTime: 3));

// 每3帧：BoltParticle（向鞭摆动方向飞出）
if (Time % 3 == 0)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        whipTip, velocity: whipSwingDir * 4f,
        color: DoGCyan, glowColor: DoGWhite,
        scale: 0.7f, lifetime: 10,
        rotation: whipSwingDir.ToRotation(),
        stretch: new Vector2(0.12f, 3.5f),
        affectedByGravity: false, glowCenter: true, glowFade: true, fadeIn: false));
```

### 命中时
```csharp
// BoltParticle爆发（×6，全方向散射）
for (int i = 0; i < 6; i++)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        target.Center, velocity: Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f),
        color: DoGCyan, glowColor: DoGWhite, scale: 0.6f, lifetime: 12,
        rotation: 0f, stretch: new Vector2(0.1f, 4f),
        affectedByGravity: true, glowCenter: true, glowFade: true, fadeIn: false));

// ImpactParticle（三角星旋转印记，Cyan）
for (int i = 0; i < 3; i++)
    GeneralParticleHandler.SpawnParticle(new ImpactParticle(
        target.Center, angularVelocity: 0.08f + i * 0.06f,
        lifetime: 15 - i * 2, scale: 0.6f + i * 0.2f, color: DoGCyan));

// GlowOrbParticle×6（球形散射）
for (int i = 0; i < 6; i++)
    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
        target.Center + Main.rand.NextVector2Circular(10f, 10f),
        Main.rand.NextVector2Circular(6f, 6f), gravity: true,
        lifetime: 12, scale: 0.12f, color: DoGCyan, glowCenter: true));

// DirectionalPulseRing（椭圆形，向鞭摆方向拉伸）
GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
    target.Center, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.5f, 0.6f),  // 横向拉伸
    rotation: whipAngle,
    originalScale: 0.04f, finalScale: 0.5f, lifeTime: 15));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 1.2 Combo-2：WhipThrust（52帧，向前刺）

> **视觉区别**：直线刺击，LineVFX + StaticGlowLine + CritSpark

### 帧0-25（前摇）
```csharp
// 每帧：LineVFX（从玩家到鞭尖的光线，每帧刷新）
GeneralParticleHandler.SpawnParticle(new LineVFX(
    player.Center, (whipTip - player.Center),
    thickness: 0.03f, color: DoGColor * 0.4f, telegraph: false));

// 每2帧：NanoParticle沿鞭身飘散
if (Time % 2 == 0)
    for (int i = 0; i < 3; i++)
        GeneralParticleHandler.SpawnParticle(new NanoParticle(
            Vector2.Lerp(player.Center, whipTip, i / 3f) + Main.rand.NextVector2Circular(6f, 6f),
            Main.rand.NextVector2Circular(2f, 2f), DoGColor, 0.4f, 12, emitsLight: true));
```

### 帧25-52（刺出阶段）
```csharp
// StaticGlowLine（从玩家到鞭尖的持续光束）
GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
    player.Center, whipTip, velocity: Vector2.Zero,
    lifetime: 3,       // 每帧刷新（3帧存活）
    xScale: 0.08f, xShrink: 1.0f,  // 不收缩
    color: DoGColor, glow: true));

// CritSpark（沿刺击方向散出，带色相偏移）
if (Time % 3 == 0)
    GeneralParticleHandler.SpawnParticle(new CritSpark(
        whipTip + Main.rand.NextVector2Circular(8f, 8f),
        thrustDir * Main.rand.NextFloat(2f, 6f),
        color: DoGColor, bloom: DoGWhite,
        scale: 0.5f, lifeTime: 15,
        rotationSpeed: 1.2f, bloomScale: 1.5f, hueShift: 0.004f));
```

### 命中时
```csharp
// 全部StaticGlowLine×8条从中心射出
for (int i = 0; i < 8; i++) {
    Vector2 dir = Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / 8f);
    GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
        target.Center, target.Center + dir * Main.rand.NextFloat(80f, 180f),
        Vector2.Zero, 18, xScale: 0.08f, xShrink: 0.88f,
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia, glow: true));
}

// FlatGlow（横向扁平光晕，刺击效果）
GeneralParticleHandler.SpawnParticle(new FlatGlow(
    target.Center, Vector2.Zero, DoGColor,
    rotation: thrustDir.ToRotation(),
    originalScale: new Vector2(0.1f, 3f), finalScale: new Vector2(3f, 0.1f),
    lifeTime: 12));

// BloomLineVFX（从玩家穿透目标的短暂激光，capped=true）
GeneralParticleHandler.SpawnParticle(new BloomLineVFX(
    player.Center, (target.Center - player.Center) * 1.3f,
    thickness: 0.06f, color: DoGColor,
    lifetime: 8, capped: true));

// GlowSquareParticle×8（方形碎片四散）
for (int i = 0; i < 8; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
        target.Center + Main.rand.NextVector2Circular(12f, 12f),
        Main.rand.NextVector2Circular(7f, 7f), gravity: true,
        lifetime: 14, scale: Main.rand.NextFloat(0.05f, 0.12f),
        color: ThreeColorSpark, rotation: Main.rand.NextFloat(0.05f)));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

# 第二章：形态1——剑形DoG（SwordMode）

> **视觉指纹**：剑身 = CircularSmearVFX弧形残像 + 双层ImpFlameTrail轨迹；
> 命中 = SlashThrough斩击线 + DetailedExplosion爆炸核心；
> 终结 = DoGDistortionMetaball + DoGRiftCrack全面爆发

## 2.0 Combo-0：SwordSwingOne（36帧，Cyan主色右挥）

### 帧0-36
```csharp
// 每帧：CircularSmearVFX（在刃尖弧线轨迹）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    bladeTip, Vector2.Zero,
    color: DoGCyan * 0.5f,
    rotation: swingAngle,
    scale: bladeLength / 60f,
    lifeTime: 4));

// 每帧：GenericSparkle沿刃身（×2，Cyan+White）
if (Time % 2 == 0) {
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        bladeMidpoint + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(2f, 2f),
        color: DoGCyan, bloom: DoGWhite,
        scale: 0.3f, lifeTime: 12, rotationSpeed: 1.5f, bloomScale: 0.8f));
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        bladeTip + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextVector2Circular(3f, 3f),
        color: DoGWhite, bloom: DoGCyan,
        scale: 0.4f, lifeTime: 10, rotationSpeed: 2f, bloomScale: 1.2f));
}

// 帧15时（挥砍到位）：FlareShine闪烁
if (Time == 15) {
    for (int i = 0; i < 4; i++)
        GeneralParticleHandler.SpawnParticle(new FlareShine(
            bladeTip, Main.rand.NextVector2Circular(2f, 2f),
            DoGWhite, DoGCyan, MathHelper.PiOver2 * i,
            scale: new Vector2(0.2f, 2.5f), finalScale: new Vector2(0.05f, 0.5f),
            lifeTime: 12, spawnDelay: i));
}
```

### 命中时
```csharp
// 标准中型DoG爆炸（配方A变体）
ImpactParticle(target.Center, 0.12f, 18, 0.8f, DoGCyan) ×2
ImpactParticle(target.Center, 0.09f, 15, 0.6f, DoGFuchsia) ×1
GlowSparkParticle (散射×8, Cyan, squash=(0.3f,1.6f))
StaticPulseRing (Cyan, scale=0.05f→0.4f, t=12)
FlameExplosion (Cyan, rot=0, scale=0.1f→0.6f, t=14)
target.AddBuff(GodSlayerInferno, 180)
```

---

## 2.1 Combo-1：SwordSwingTwo（36帧，Fuchsia主色反挥）

> **视觉区别**：SlashThrough穿透斩击线，GlowSparkParticle散射

### 帧0-36
```csharp
// 每帧：CircularSmearVFX（Fuchsia，反方向弧度）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    bladeTip, Vector2.Zero,
    color: DoGFuchsia * 0.5f,
    rotation: swingAngle + MathHelper.Pi,
    scale: bladeLength / 60f, lifeTime: 4));

// 每帧：FancyStars（在刃尖随机散出，Fuchsia主色）
if (Main.rand.NextBool(3))
    GeneralParticleHandler.SpawnParticle(new FancyStars(
        bladeTip + Main.rand.NextVector2Circular(10f, 10f),
        rotation: Main.rand.NextFloat(MathHelper.TwoPi),
        scale: Main.rand.NextFloat(0.3f, 0.6f),
        velocity: Main.rand.NextVector2Circular(3f, 3f),
        rotationSpeed: Main.rand.NextFloat(0.05f, 0.1f),
        lifeTime: 15,
        color: Main.rand.NextBool() ? DoGFuchsia : DoGColor));
```

### 命中时
```csharp
// SlashThrough斩击线（贯穿NPC）
GeneralParticleHandler.SpawnParticle(new SlashThrough(
    DoGFuchsia, target.Center, swingDir.ToRotation(), 15, target));

// GlowSparkParticle×12（Fuchsia为主，细长竖条）
for (int i = 0; i < 12; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
        target.Center + Main.rand.NextVector2Circular(10f, 10f),
        Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 2f,
        gravity: true, lifetime: Main.rand.Next(12, 20),
        scale: Main.rand.NextFloat(0.7f, 1.1f),
        color: ThreeColorSpark,
        squash: new Vector2(0.22f, 1.9f), quickShrink: true));

// CritSpark×6（色相偏移，Fuchsia→Cyan循环）
for (int i = 0; i < 6; i++)
    GeneralParticleHandler.SpawnParticle(new CritSpark(
        target.Center + Main.rand.NextVector2Circular(15f, 15f),
        Main.rand.NextVector2Circular(5f, 5f),
        DoGFuchsia, DoGWhite, 0.5f, 20, 1.5f, 1.2f, hueShift: 0.006f));

// DirectionalPulseRing（向挥砍方向拉伸）
GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
    target.Center, Vector2.Zero, DoGFuchsia,
    squish: new Vector2(1.8f, 0.5f), rotation: swingDir.ToRotation(),
    originalScale: 0.04f, finalScale: 0.5f, lifeTime: 14));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 2.2 Combo-2：SwordFinisher（72帧，双色终结斩）

> **视觉等级**：★★★★★ 最壮观的普攻特效
> 使用DoGDistortionMetaball + DoGRiftCrack + DetailedExplosion三连爆

### 帧0-36（蓄力/蓄力提速）
```csharp
// 每帧：ConstellationRingVFX围绕剑体旋转（星环蓄力感）
if (Time % 5 == 0)
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Vector2.Zero, starAmount: 6, spinSpeed: 0.04f,
        offset: 30f, color: DoGColor, scale: 0.7f, lifeTime: 20));

// 每帧：ChargeUpLineVFX从玩家四周向中心汇聚
if (Time % 4 == 0)
    for (int i = 0; i < 4; i++) {
        float dir = i * MathHelper.PiOver2 + Time * 0.05f;
        GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(
            player.Center + dir.ToRotationVector2() * 120f,
            dir + MathHelper.Pi, 0.035f, DoGColor, 20,
            telegraph: true));
    }

// 每帧：NanoParticle能量微粒汇聚
GeneralParticleHandler.SpawnParticle(new NanoParticle(
    player.Center + Main.rand.NextVector2Circular(100f, 100f),
    (player.Center - (player.Center + Main.rand.NextVector2Circular(100f, 100f))).SafeNormalize(Vector2.Zero) * 3f,
    DoGColor, 0.5f, 20, emitsLight: true));
```

### 帧36（爆发瞬间—无命中也触发）
```csharp
// 自身爆发效果（释放能量）
CustomPulse(DoGWhite * 0.8f, 0.3f, 2.5f, "CalamityMod/Particles/ShineExplosion1")
CustomPulse(DoGCyan  * 0.7f, 0.5f, 3.0f, "CalamityMod/Particles/PlasmaExplosion")
CustomPulse(DoGFuchsia*0.6f, 0.7f, 3.5f, "CalamityMod/Particles/ShineExplosion2")
PlayerCenteredPulseRing(player, Zero, DoGCyan,    One, 0f, 0.04f, 0.6f, 16)
PlayerCenteredPulseRing(player, Zero, DoGFuchsia, One, 0f, 0.06f, 0.9f, 20)
StrongBloom(player.Center, DoGWhite, scale=1.5f)
CalamityUtils.AddScreenshakeAt(player.Center, 4f)
```

### 命中时
```csharp
// ★ 核心: DetailedExplosion三连（不同色彩时序）★
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.3f, 0.75f), rotation: 0f,
    originalScale: 0.25f, finalScale: 1.8f, lifeTime: 22));
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGWhite,
    squish: new Vector2(0.9f, 0.9f), rotation: MathHelper.PiOver4,
    originalScale: 0.35f, finalScale: 1.5f, lifeTime: 20));
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGFuchsia * 0.9f,
    squish: new Vector2(0.7f, 1.3f), rotation: MathHelper.Pi / 3f,
    originalScale: 0.3f, finalScale: 1.2f, lifeTime: 18));

// DoGDistortionMetaball（现实扭曲）
for (int i = 0; i < 12; i++)
    DoGDistortionMetaball.SpawnSquare(
        target.Center + Main.rand.NextVector2Circular(50f, 50f),
        Main.rand.NextVector2Circular(4f, 4f),
        Main.rand.NextFloat(30f, 70f));
for (int i = 0; i < 6; i++)
    DoGDistortionMetaball.SpawnCircle(
        target.Center + Main.rand.NextVector2Circular(30f, 30f),
        Vector2.Zero, Main.rand.NextFloat(20f, 45f));

// DoGRiftCrack×8条裂缝从命中点射出
for (int i = 0; i < 8; i++)
    Projectile.NewProjectile(
        npcSource, target.Center,
        Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 10f),
        ModContent.ProjectileType<DoGRiftCrack>(),
        0, 0f, player.whoAmI);

// SlashThrough（贯穿斩击线）
GeneralParticleHandler.SpawnParticle(new SlashThrough(
    DoGColor, target.Center, swingDir.ToRotation(), 18, target));

// GenericSparkle×15（宇宙感星形）
for (int i = 0; i < 15; i++)
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        target.Center + Main.rand.NextVector2Circular(20f, 20f),
        Main.rand.NextVector2Circular(7f, 7f),
        color: ThreeColorSpark, bloom: DoGWhite,
        scale: Main.rand.NextFloat(0.3f, 0.8f), lifeTime: Main.rand.Next(20, 35),
        rotationSpeed: 2f, bloomScale: 1.5f));

// ImpactParticle×4（旋转六叉星）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new ImpactParticle(
        target.Center + Main.rand.NextVector2Circular(5f, 5f),
        angularVelocity: 0.1f + i * 0.07f,
        lifetime: 18 - i * 2,
        scale: 0.8f + i * 0.15f,
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia));

// StaticGlowLine×12（从中心射出，向外消散）
for (int i = 0; i < 12; i++) {
    Vector2 dir = Main.rand.NextVector2Unit();
    GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
        target.Center, target.Center + dir * Main.rand.NextFloat(100f, 220f),
        dir * 0.5f, 18, xScale: 0.09f, xShrink: 0.87f,
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia, glow: true));
}

// 三种CustomPulse波
CustomPulse(DoGCyan*0.8f,    0.2f, 2.0f, "CalamityMod/Particles/ShineExplosion1")
CustomPulse(DoGWhite*0.9f,   0.3f, 2.5f, "CalamityMod/Particles/PlasmaExplosion")
CustomPulse(DoGFuchsia*0.7f, 0.45f,3.0f, "CalamityMod/Particles/ShineExplosion2")

// 强光晕 + 震屏
StrongBloom(target.Center, DoGWhite, scale=2f)
CalamityUtils.AddScreenshakeAt(target.Center, 10f)
SoundEngine.PlaySound(SoundID.Item122, target.Center)

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
```

---

# 第三章：形态2——链刃DoG（ChainKnifeMode）

> **视觉指纹**：链条 = ElectricSpark + NanoParticle电流；
> 链刃旋转 = TechyHolosquareParticle碎片；
> 命中 = GalaxyMetaball + BoltParticle + RoundedStarParticle

## 3.0 Combo-0：ChainKnifeArc（64帧，链刃弧形旋转-正向）

### 每帧（链条AI期间）
```csharp
// 每帧沿链条等距放NanoParticle（电流传导感）
int segments = chain.Length;
for (int i = 0; i < segments; i += 3) {
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        chain[i] + Main.rand.NextVector2Circular(4f, 4f),
        Main.rand.NextVector2Circular(1.5f, 1.5f),
        color: i % 6 < 3 ? DoGCyan : DoGFuchsia,
        scale: 0.35f, lifeTime: 10, emitsLight: true));
}

// 每3帧：ElectricSpark沿链条跳跃
if (Time % 3 == 0)
    GeneralParticleHandler.SpawnParticle(new ElectricSpark(
        chain[Main.rand.Next(segments)] + Main.rand.NextVector2Circular(6f, 6f),
        Main.rand.NextVector2Circular(3f, 3f),
        color: DoGCyan, bloom: DoGFuchsia,
        scale: 0.55f, lifeTime: 10,
        maxJumpRotation: MathHelper.PiOver4, jumpTime: 5f));

// 链刃尖：TechyHolosquareParticle（科技感碎片）
if (Main.rand.NextBool(4))
    GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(
        knifePos + Main.rand.NextVector2Circular(12f, 12f),
        Main.rand.NextVector2Circular(4f, 4f),
        scale: Main.rand.NextFloat(0.4f, 0.8f),
        color: DoGColor, lifetime: 14, opacity: 0.8f));
```

### 命中时
```csharp
// GalaxyMetaball（宇宙感背景元球）
for (int i = 0; i < 6; i++)
    GalaxyMetaball.Particles.Add(new CosmicParticle(
        target.Center + Main.rand.NextVector2Circular(30f, 30f),
        Main.rand.NextVector2Circular(4f, 4f), size: Main.rand.NextFloat(20f, 45f)));

// BoltParticle×8（闪电爆发）
for (int i = 0; i < 8; i++)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 9f),
        color: DoGCyan, glowColor: DoGWhite,
        scale: 0.7f, lifetime: 12,
        rotation: Main.rand.NextFloat(MathHelper.TwoPi),
        stretch: new Vector2(0.12f, 3.8f),
        affectedByGravity: true, glowCenter: true, glowFade: true, fadeIn: false));

// GlowOrbParticle×5（圆形散射）
for (int i = 0; i < 5; i++)
    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
        target.Center, Main.rand.NextVector2Circular(7f, 7f), gravity: false,
        lifetime: 14, scale: 0.15f, color: DoGCyan, glowCenter: true));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 3.1 Combo-1：ChainKnifeArc（64帧，链刃弧形旋转-反向）

> **视觉区别**：改用Fuchsia主色，RoundedStarParticle螺旋，GlowSquareParticle

### 每帧
```csharp
// 改用Fuchsia + FancyStars
if (Main.rand.NextBool(3))
    GeneralParticleHandler.SpawnParticle(new FancyStars(
        chain[Main.rand.Next(chain.Length)] + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextFloat(MathHelper.TwoPi),
        scale: Main.rand.NextFloat(0.2f, 0.45f),
        velocity: Main.rand.NextVector2Circular(2f, 2f),
        rotationSpeed: Main.rand.NextFloat(0.03f, 0.08f),
        lifeTime: 14, color: DoGFuchsia));

// GlowSquareParticle沿链旋转
if (Time % 4 == 0)
    GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
        chain[Main.rand.Next(chain.Length)] + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(2f, 2f), gravity: false,
        lifetime: 10, scale: Main.rand.NextFloat(0.04f, 0.09f),
        color: DoGFuchsia, rotation: Main.rand.NextFloat(0.05f, 0.12f)));
```

### 命中时
```csharp
// RoundedStarParticle螺旋收缩（×8围绕玩家旋转但在命中点爆出）
for (int i = 0; i < 8; i++)
    GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(
        target.Center + Main.rand.NextVector2Circular(50f, 50f),
        Main.rand.NextVector2Circular(3f, 3f),
        color: i % 2 == 0 ? DoGFuchsia : DoGColor,
        scale: Main.rand.NextFloat(0.4f, 0.8f),
        lifetime: 25, rotationSpeed: 0.05f, deceleration: 0.95f,
        useSpiralAI: false, spiralTarget: target.Center, ownerIndex: player.whoAmI));

// TechyHolosquareParticle×10（Fuchsia + 色差）
for (int i = 0; i < 10; i++)
    GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(
        target.Center + Main.rand.NextVector2Circular(20f, 20f),
        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f),
        scale: Main.rand.NextFloat(0.5f, 1.0f),
        color: ThreeColorSpark, lifetime: 16, opacity: 0.85f));

// ImpactParticle（Fuchsia色，旋转六叉）
GeneralParticleHandler.SpawnParticle(new ImpactParticle(
    target.Center, 0.15f, 18, 0.9f, DoGFuchsia));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 3.2 Combo-2：ChainKnifeFinisher（80帧，链刃最强终结）

> **视觉等级**：★★★★★（与SwordFinisher并列）
> StarsmokeMetaball + FriendlyLaserWallBeam + 完整DoGTeleportRift风格爆发

### 帧0-50（蓄力/旋转加速）
```csharp
// 每帧：StarsmokeMetaball积累（星烟效果）
StarsmokeMetaball.SpawnParticle(
    knifePos + Main.rand.NextVector2Circular(15f, 15f),
    knifeVelocity * 0.3f,
    size: Main.rand.NextFloat(15f, 35f),
    lifetime: 30,
    squash: new Vector2(0.8f, 1.2f),
    shrinkSpeed: 0.15f, velocitySquash: 0.5f);

// 每2帧：ConstellationRingVFX围绕链刃
if (Time % 10 == 0)
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        knifePos, Vector2.Zero, starAmount: 8, spinSpeed: 0.06f,
        offset: 20f, color: DoGColor, scale: 0.6f, lifeTime: 15));

// 每帧沿链条：ChargeUpLineVFX向链刃汇聚
if (Time % 6 == 0 && Time > 20)
    GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(
        knifePos + Main.rand.NextVector2Circular(100f, 100f),
        (knifePos - (knifePos + Main.rand.NextVector2Circular(100f, 100f))).ToRotation(),
        0.04f, DoGColor, 20, telegraph: true));
```

### 帧50（链刃"拉回"冲击波）
```csharp
// 全屏FriendlyLaserWallBeam（向最近敌人射出）
if (target != null) {
    Vector2 dir = knifePos.DirectionTo(target.Center);
    int laser = Projectile.NewProjectile(source,
        knifePos + dir * 2016f, -dir,
        typeof(FriendlyLaserWallBeam), (int)(baseDmg * 0.6f), 0f, owner, -1.5f);
    Main.projectile[laser].scale *= 0.4f;
}

// 爆炸特效
DetailedExplosion(knifePos, Zero, DoGCyan,    (1.2f,0.8f), 0, 0.3f, 2.0f, 24)
DetailedExplosion(knifePos, Zero, DoGFuchsia, (0.8f,1.2f), Pi/3, 0.4f, 1.6f, 22)
GalaxyMetaball ×8个CosmicParticle (大size=40~80f)
DoGDistortionMetaball.SpawnSquare ×10
DoGDistortionMetaball.SpawnCircle ×5
DoGRiftCrack ×6 (短裂缝)
GenericSparkle ×15
ImpactParticle ×4
StaticGlowLine ×10 (from knifePos outward)
CustomPulse(White, 0.4f, 3.5f, "PlasmaExplosion")
StrongBloom(knifePos, White, scale=2.5f)
CalamityUtils.AddScreenshakeAt(knifePos, 12f)

target.AddBuff(GodSlayerInferno, 400)
```

---

# 第四章：QuickDraw（48帧，任何形态中途右键）

> **设计目标**：DoGTeleportRift风格的闪现+爆炸，使用最全面的粒子组合
> **所有之前未在普攻出现的粒子类在这里集中使用**

## 4.1 帧0-20（蓄力/闪现前摇）

```csharp
// 大量NanoParticle在玩家周围（能量密集汇聚）
for (int i = 0; i < 8; i++)
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        player.Center + Main.rand.NextVector2Circular(60f, 60f),
        (player.Center - (player.Center + Main.rand.NextVector2Circular(60f, 60f))).SafeNormalize(Zero) * 5f,
        DoGColor, 0.6f, 15, bigSize: true, emitsLight: true));

// ChargeUpLineVFX×6从玩家向外发散（实际是蓄力感，反向从内向外）
for (int i = 0; i < 6; i++) {
    float dir = i * MathHelper.TwoPi / 6f;
    GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(
        player.Center + dir.ToRotationVector2() * 80f,
        dir + MathHelper.Pi, 0.04f, DoGColor, 18,
        telegraph: true, fullFadeInPoint: 0.3f));
}

// SquishyLightParticle高速汇聚（每帧）
for (int i = 0; i < 5; i++)
    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
        velocity: (player.Center - Main.rand.NextVector2Circular(100f, 100f) - player.Center).SafeNormalize(Zero) * 12f,
        color: DoGColor, squishRatio: 0.5f, lifetime: 10, scale: 0.18f, hueShift: 0.003f));

// ConstellationRingVFX×2（内外双圈，螺旋旋转）
if (Time == 0) {
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Zero, 10, 0.04f, 60f, DoGCyan, 0.9f, 25));
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Zero, 8, -0.035f, 45f, DoGFuchsia, 0.8f, 22));
}

// RoundedStarParticle×10围绕玩家螺旋汇聚
if (Time == 0) {
    for (int i = 0; i < 10; i++)
        GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(
            player.Center + Main.rand.NextVector2Circular(80f, 80f),
            Zero, DoGColor, 0.5f, 22, 0.05f, 1f,
            useSpiralAI: true, spiralTarget: player.Center, ownerIndex: player.whoAmI));
}
```

## 4.2 帧20（爆炸瞬间，也是闪现到目标位置）

```csharp
// ★ 全粒子集中爆发 ★

// 爆炸核心（DetailedExplosion三连）
DetailedExplosion(mousePos, Zero, DoGCyan,    (1.4f,0.7f), 0,    0.3f, 2.5f, 25)
DetailedExplosion(mousePos, Zero, DoGWhite,   (1f,1f),     Pi/4, 0.5f, 2.0f, 23)
DetailedExplosion(mousePos, Zero, DoGFuchsia, (0.7f,1.3f), Pi/2, 0.4f, 1.8f, 21)

// FlameExplosion×4（不同方向）
for (int i = 0; i < 4; i++)
    FlameExplosion(mousePos + Main.rand.NextVector2Circular(20f, 20f),
        Zero, i % 2 == 0 ? DoGCyan : DoGFuchsia,
        squish: new Vector2(0.8f + Main.rand.NextFloat(0.6f), 0.8f + Main.rand.NextFloat(0.4f)),
        rotation: Main.rand.NextFloat(MathHelper.TwoPi),
        originalScale: 0.15f, finalScale: 1.0f, lifeTime: 16, opacity: 0.75f)

// DoGDistortionMetaball大量（DoGTeleportRift爆炸规格）
for (int i = 0; i < 20; i++)
    DoGDistortionMetaball.SpawnSquare(mousePos + Main.rand.NextVector2Circular(60f, 60f),
        Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(25f, 65f));
for (int i = 0; i < 10; i++)
    DoGDistortionMetaball.SpawnCircle(mousePos + Main.rand.NextVector2Circular(40f, 40f),
        Zero, Main.rand.NextFloat(15f, 40f));

// DoGRiftCrack×12条
for (int i = 0; i < 12; i++)
    Projectile.NewProjectile(source, mousePos,
        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f),
        typeof(DoGRiftCrack), 0, 0f, owner);

// GenericSparkle×20（星形爆发）
for (int i = 0; i < 20; i++)
    GenericSparkle(mousePos + Main.rand.NextVector2Circular(15f, 15f),
        Main.rand.NextVector2Circular(10f, 10f),
        ThreeColorSpark, DoGWhite, Main.rand.NextFloat(0.3f, 0.9f),
        Main.rand.Next(18, 35), 2f, 1.5f)

// ImpactParticle×5（旋转六叉）
for (int i = 0; i < 5; i++)
    ImpactParticle(mousePos, 0.07f + i * 0.08f, 20 - i * 2, 0.6f + i * 0.2f,
        i % 2 == 0 ? DoGCyan : DoGFuchsia)

// FlareShine×8（十字爆闪）
for (int i = 0; i < 8; i++)
    FlareShine(mousePos + Main.rand.NextVector2Circular(20f, 20f),
        Main.rand.NextVector2Circular(2f, 2f),
        DoGWhite, DoGCyan,
        angle: i * MathHelper.PiOver2 * 0.5f,
        scale: new Vector2(0.15f, 2.5f), finalScale: new Vector2(0.02f, 0.4f),
        lifeTime: 15, spawnDelay: i / 2)

// FancyStars×15（星光飞散）
for (int i = 0; i < 15; i++)
    FancyStars(mousePos + Main.rand.NextVector2Circular(30f, 30f),
        Main.rand.NextFloat(MathHelper.TwoPi),
        Main.rand.NextFloat(0.3f, 0.7f),
        Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextFloat(0.04f, 0.1f), 20,
        ThreeColorSpark)

// CustomPulse×3种
CustomPulse(DoGCyan*0.9f,   0.2f, 3.0f, "ShineExplosion1")
CustomPulse(DoGWhite*0.85f, 0.4f, 3.5f, "PlasmaExplosion")
CustomPulse(DoGFuchsia*0.8f,0.6f, 4.0f, "ShineExplosion2")

// StaticGlowLine×16（爆炸射线）
for (int i = 0; i < 16; i++) {
    Vector2 dir = Main.rand.NextVector2Unit();
    StaticGlowLine(mousePos, mousePos + dir * Main.rand.NextFloat(120f, 300f),
        dir * 0.5f, 20, 0.08f, 0.87f, i % 2 == 0 ? DoGCyan : DoGFuchsia, true)
}

// GlowSquareParticle×10（方形碎片）
for (int i = 0; i < 10; i++)
    GlowSquareParticle(mousePos + Main.rand.NextVector2Circular(20f, 20f),
        Main.rand.NextVector2Circular(8f, 8f), true, 16,
        Main.rand.NextFloat(0.06f, 0.13f), ThreeColorSpark, true,
        Main.rand.NextFloat(0.08f, 0.15f))

// SquishyLightParticle×15（高速散射）
for (int i = 0; i < 15; i++)
    SquishyLightParticle(Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f, 20f),
        ThreeColorSpark, 0.5f, 12, 0.2f, hueShift: 0.005f)

// FlatGlow×4（横向切光）
for (int i = 0; i < 4; i++)
    FlatGlow(mousePos, Zero, DoGColor,
        rotation: i * MathHelper.PiOver2,
        originalScale: new Vector2(0.1f, 2.5f),
        finalScale: new Vector2(2.5f, 0.1f), lifeTime: 14)

// GalaxyMetaball（宇宙背景）
for (int i = 0; i < 8; i++)
    GalaxyMetaball.Particles.Add(new CosmicParticle(
        mousePos + Main.rand.NextVector2Circular(60f, 60f),
        Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(25f, 60f)))

// 全屏激光（向最近敌人）
if (nearestEnemy != null)
    FriendlyLaserWallBeam(mousePos, dirToEnemy, damage*0.7f, scale=0.5f)

// 强力震屏
StrongBloom(mousePos, DoGWhite, scale=3f)
CalamityUtils.AddScreenshakeAt(mousePos, 14f)
SoundEngine.PlaySound("DoGLaserWallBigAttack", mousePos)
```

---

# 第五章：右键模式切换（Galaxia风格，18帧Portal动画）

> **参考**：FourSeasonsGalaxia.cs 右键生成 GalaxiaHoldout 在 player.Top
> **参考**：StreamGougePortal.cs 3层旋转传送门

## 5.1 触发机制

```csharp
// NewLegendCosmicDischarge.cs 右键：
// 1. 检查是否已有SwitchPortal弹幕（防止重复生成）
// 2. 生成 CosmicDischargeSwitchPortal 在 player.Top (0伤害视觉弹幕)
// 3. SwitchPortal负责18帧动画 + 动画完成后回调 ToggleAttackMode()
```

## 5.2 新增弹幕 CosmicDischargeSwitchPortal.cs

```csharp
// 位置: player.Top (跟随玩家，0伤害)
// 生命周期: 18帧

// 帧0-8（传送门开启）:
// 每帧：3层旋转Portal（Stream Gouge参考）
// Layer 0: Color.Black,   rotation += 0.02f
// Layer 1: DoGCyan,       rotation += 0.05f
// Layer 2: DoGFuchsia,    rotation += 0.09f
// 同时：TechyHolosquareParticle×2散出
// 同时：NanoParticle×3汇聚

// 帧4 (渐显)：
// ConstellationRingVFX(player.Top, Zero, 6, 0.04f, 30f, DoGColor, 0.5f, 10)
// GlowOrbParticle×5（围绕Portal旋转）

// 帧8（Portal全开，模式切换发生）:
// ★ 在这一帧调用 ToggleAttackMode() ★
// CustomPulse(DoGWhite, 0.2f, 2f, "ShineExplosion2")
// CustomPulse(DoGColor, 0.3f, 2.5f, "PlasmaExplosion")
// TechyHolosquareParticle×10散射
// StrongBloom(player.Top, White, scale=1.5f)
// CalamityUtils.AddScreenshakeAt(player.Top, 4f)
// SoundEngine.PlaySound("DemonSwordKillMode" or similar)

// 帧8-18（传送门关闭）:
// 渐渐缩小，FlatGlow余辉
// ElectricSpark×2/帧（传送门消散电弧）
```

## 5.3 视觉时序
```
帧0:  传送门开始扩大（Black→Cyan→Fuchsia三层出现）
帧4:  ConstellationRingVFX + NanoParticle最密集
帧8:  ★模式切换★ + 爆闪 + CustomPulse
帧12: 传送门开始消失
帧18: 传送门完全消失，GlowOrb余波消散
```

---

# 第六章：大招重设计（CosmicDischargeUltimateField完全重建）

> **旧大招**：冰圈旋转场 → 全部删除
> **新大招**：DoGTeleportRift规格的蓄力+全屏爆炸+激光墙复合技

## 6.1 大招触发条件（保持不变，仅替换视觉）

## 6.2 蓄力阶段（60帧）

### 帧0-60（每帧）
```csharp
// ① ChargeUpLineVFX×10从四面八方向玩家汇聚
if (Time % 3 == 0)
    for (int i = 0; i < 10; i++) {
        float dir = i * MathHelper.TwoPi / 10f + Time * 0.02f; // 缓慢旋转
        ChargeUpLineVFX(
            player.Center + dir.ToRotVec2() * (150f + MathF.Sin(Time * 0.1f) * 30f),
            dir + MathHelper.Pi, 0.04f, DoGColor, 20, telegraph: true)
    }

// ② RoundedStarParticle×12围绕玩家螺旋
if (Time == 0)
    for (int i = 0; i < 12; i++)
        RoundedStarParticle(
            player.Center + Main.rand.NextVector2Circular(100f, 100f),
            Zero, ThreeColorSpark, Main.rand.NextFloat(0.4f, 0.8f), 65,
            rotationSpeed: 0.04f + i * 0.005f, deceleration: 1f,
            useSpiralAI: true, spiralTarget: player.Center, ownerIndex: player.whoAmI)

// ③ PlayerCenteredPulseRing每10帧一个
if (Time % 10 == 0) {
    PlayerCenteredPulseRing(player, Zero, DoGCyan,    One, 0f, 0.05f, 0.8f, 30)
    PlayerCenteredPulseRing(player, Zero, DoGFuchsia, One, 0f, 0.07f, 1.1f, 35)
}

// ④ ConstellationRingVFX内外双圈
if (Time % 15 == 0) {
    ConstellationRingVFX(player.Center, Zero, 12, 0.03f, 100f, DoGCyan, 1.2f, 40)
    ConstellationRingVFX(player.Center, Zero, 8, -0.025f, 70f, DoGFuchsia, 1.0f, 35)
}

// ⑤ NanoParticle全场大量（能量粒子海）
for (int i = 0; i < 6; i++)
    NanoParticle(player.Center + Main.rand.NextVector2Circular(120f, 120f),
        向player.Center的方向 * 4f, DoGColor, Main.rand.NextFloat(0.3f, 0.7f), 20, emitsLight: true)

// ⑥ 缓慢增加的screenshake（0→5f）
CalamityUtils.AddScreenshakeAt(player.Center, Time / 60f * 5f)
```

## 6.3 爆发阶段（帧60，一次性）

```csharp
// === 阶段一：全屏激光墙（向所有可见敌人） ===
foreach (NPC target in activaNPCs in range) {
    Vector2 dir = player.Center.DirectionTo(target.Center);
    // 主激光（从极远处射入）
    int laser1 = NewProjectile(FriendlyLaserWallBeam, player.Center + dir * 2016f,
        -dir, damage*1.0f, ai0: 0f)
    Main.projectile[laser1].scale *= 0.5f;
    // 偏转副激光×2
    NewProjectile(FriendlyLaserWallBeam, player.Center + dir.RotatedBy(0.15f) * 2016f,
        -dir.RotatedBy(0.15f), damage*0.6f, ai0: 1.5f)
}
SoundEngine.PlaySound("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack", player.Center)

// === 阶段二：DoGTeleportRift级别爆炸 ===
// DetailedExplosion三连（大型）
DetailedExplosion(player.Center, Zero, DoGCyan,    (1.5f,0.7f), 0,    0.4f, 3.0f, 28)
DetailedExplosion(player.Center, Zero, DoGWhite,   (1f,1f),     Pi/5, 0.6f, 2.5f, 26)
DetailedExplosion(player.Center, Zero, DoGFuchsia, (0.7f,1.4f), Pi/3, 0.5f, 2.8f, 24)

// DoGDistortionMetaball大爆发
for (int i = 0; i < 35; i++)
    DoGDistortionMetaball.SpawnSquare(...)
for (int i = 0; i < 18; i++)
    DoGDistortionMetaball.SpawnCircle(...)

// DoGRiftCrack×25条（全方向）
for (int i = 0; i < 25; i++)
    NewProjectile(DoGRiftCrack, player.Center, rand.NextVector2Unit()*rand.NextFloat(4f,12f), ...)

// GenericSparkle×25（最密集）
SparkParticle ×30（三色混合）
SquishyLightParticle ×25（高速散射）
GlowSparkParticle ×15（细长）
ImpactParticle ×6（六叉旋转）
FlareShine ×12（十字闪烁，各方向）
FancyStars ×20（星光飞散）
StaticGlowLine ×20（放射状）
GlowSquareParticle ×12（方形碎片）
GlowOrbParticle ×10（圆球散射）
TechyHolosquareParticle ×15（科技碎片）
ElectricSpark ×10（电弧）
BoltParticle ×12（闪电）
NanoParticle ×50（能量微尘暴风雪）

// CustomPulse三种同时
CustomPulse(DoGCyan*0.9f,    0.3f, 4.0f, "ShineExplosion1")
CustomPulse(DoGWhite*0.85f,  0.5f, 4.5f, "PlasmaExplosion")
CustomPulse(DoGFuchsia*0.8f, 0.7f, 5.0f, "ShineExplosion2")

// PlayerCenteredPulseRing×3（大型扩散）
for (int i = 0; i < 3; i++)
    PlayerCenteredPulseRing(player, Zero, ThreeColorSpark, One, 0f,
        0.05f + i*0.03f, 1.5f + i*0.5f, 30 + i*5)

// GalaxyMetaball×15
for (int i = 0; i < 15; i++)
    GalaxyMetaball.Particles.Add(new CosmicParticle(..., size=40~90f))

// StarsmokeMetaball×8（星烟）
for (int i = 0; i < 8; i++)
    StarsmokeMetaball.SpawnParticle(player.Center + rand.NextVector2Circular(80f,80f),
        rand.NextVector2Circular(5f,5f), size=40f, lifetime=40, ...)

// FlatGlow×6（方位光）
for (int i = 0; i < 6; i++)
    FlatGlow(player.Center, Zero, DoGColor, i*Pi/3,
        (0.1f,3.5f), (3.5f,0.1f), 18)

// ConstellationRingVFX×3（大型星环爆发）
ConstellationRingVFX(player.Center, Zero, 16, 0.05f, 150f, DoGColor, 1.5f, 35)
ConstellationRingVFX(player.Center, Zero, 12, -0.04f, 110f, DoGCyan, 1.2f, 30)
ConstellationRingVFX(player.Center, Zero, 10, 0.03f,  80f, DoGFuchsia, 1.0f, 25)

// 强力震屏
StrongBloom(player.Center, DoGWhite, scale=4f)
CalamityUtils.AddScreenshakeAt(player.Center, 18f)
```

## 6.4 大招持续场地效果（帧60-300）

```csharp
// 保留从UltimateField.cs的"减速敌人"逻辑，替换视觉：
// 旧: 42节点冰圈 + Frost dust
// 新:

// 每帧：DoGFire风格的双层轨迹围绕场地边界旋转
// （用IPixelatedPrimitiveRenderer绘制42节点DoG火焰圈）
// 外层: ScarletDevilStreak, width=50f, Cyan
// 内层: SylvestaffStreak, width=20f, Fuchsia

// 每3帧：NanoParticle在场地内随机（大量，小scale）
for (int i = 0; i < 4; i++)
    NanoParticle(fieldCenter + rand.NextVector2Circular(fieldRadius, fieldRadius),
        rand.NextVector2Circular(2f,2f), DoGColor, 0.4f, 20, emitsLight: true)

// 每10帧：StaticPulseRing从场地中心扩散
StaticPulseRing(fieldCenter, Zero, DoGColor, One, 0f, 0.03f, fieldRadius/50f, 20)

// 每15帧：ConstellationRingVFX
ConstellationRingVFX(fieldCenter, Zero, 8, 0.02f, fieldRadius*0.8f, DoGColor, 0.8f, 20)

// 每5帧：GenericBloom在场地内随机（柔和光晕）
GenericBloom(fieldCenter + rand.NextVector2Circular(fieldRadius,fieldRadius),
    Zero, DoGColor*0.4f, scale=3f, lifeTime=15)
```

---

# 第七章：被动系统重设计

> **旧被动**: DrawCurvedBladeGlow（冰蓝色多层曲线）
> **新被动**: DoGFire风格双层轨迹 + NanoParticle电流 + 色相循环发光

## 7.1 武器自发光（每帧常态）

```csharp
// 刃身：双层ImpFlameTrail（详见0.3章）
// 每帧：CritSpark沿刃身（低频，制造"活的"感觉）
if (Main.rand.NextBool(5))
    CritSpark(bladeMidpoint + rand.NextVector2Circular(8f,8f),
        rand.NextVector2Circular(1f,1f), DoGColor, DoGWhite, 0.3f, 15,
        rotationSpeed: 1f, bloomScale: 0.8f, hueShift: 0.003f)

// 每帧：NanoParticle沿刃（大量低scale）
if (rand.NextBool(3))
    NanoParticle(bladeMidpoint + rand.NextVector2Circular(15f,15f),
        rand.NextVector2Circular(1.5f,1.5f), DoGColor, 0.35f, 12, emitsLight: true)
```

## 7.2 Glowmask系统（PostDraw）

```csharp
// 双层glowmask（参考DevourerofGodsHead.cs）
// 外层: DoGCyan色
Draw(weaponGlowCyan, center, DoGCyan * flickerOpacity, ...)
// 内层: DoGFuchsia色
Draw(weaponGlowFuchsia, center, DoGFuchsia * flickerOpacity * 0.7f, ...)
// flickerOpacity = 0.7f + 0.3f * sin(Time * 0.15f)  → 缓慢脉动
```

---

# 第八章：新增弹幕清单

| 弹幕名称 | 类型 | 说明 |
|---------|------|------|
| CosmicDischargeSwitchPortal | 视觉弹幕 | 右键切换时的3层旋转传送门(18帧) |
| CosmicDischargeDoGFire | IPixelatedPrimitive | 武器本体DoGFire风格轨迹 |
| CosmicDischargeRiftProjectile | 战斗弹幕 | 参考DoGRiftCrack，蓄力型子弹 |

---

# 第九章：粒子使用全覆盖检查表

| 粒子类 | 使用场合 | 颜色 |
|-------|---------|------|
| SparkParticle | QuickDraw, 所有命中 | 三色混合 |
| GlowOrbParticle | 1.1命中, QuickDraw | Cyan |
| GlowSparkParticle | 1.0命中, 2.1命中 | Fuchsia/Cyan |
| GlowSquareParticle | 1.2命中, QuickDraw | 三色 |
| NanoParticle | 全程被动, 蓄力场 | DoGColor |
| GenericBloom | 大招持续场 | DoGColor |
| GenericSparkle | 2.0帧, 大招 | Fuchsia/Cyan |
| CritSpark | 1.2挥出, 被动 | DoGColor + hueShift |
| FlareShine | 2.0帧15, QuickDraw | White+Cyan |
| FancyStars | 3.1每帧, QuickDraw | Fuchsia |
| ImpactParticle | 2.0/2.1/2.2命中 | Cyan/Fuchsia |
| RoundedStarParticle | 3.1命中, 大招蓄力 | 三色, UseSpiralAI |
| ConstellationRingVFX | 2.2蓄力, 右键Portal, 大招 | DoGColor |
| BoltParticle | 1.1命中, 3.0命中 | Cyan+White |
| ElectricSpark | 1.0每帧, 3.0每帧 | Cyan+Fuchsia |
| StaticGlowLine | 1.2命中, 2.2命中 | Cyan/Fuchsia |
| LineVFX | 1.2前摇 | DoGColor |
| BloomLineVFX | 1.2命中 | DoGColor, capped=true |
| StaticPulseRing | 1.0命中, 3.0命中, 大招 | Cyan/Fuchsia |
| PlayerCenteredPulseRing | 2.2爆发, 大招爆发 | Cyan/Fuchsia |
| DirectionalPulseRing | 1.1命中, 2.1命中 | Cyan/Fuchsia |
| FlameExplosion | 1.0命中, QuickDraw | Cyan/Fuchsia |
| DetailedExplosion | 2.2命中, QuickDraw, 大招 | Cyan+White+Fuchsia三连 |
| HeavySmokeParticle | 1.0挥出, 武器轨迹 | Fuchsia |
| CircularSmearVFX | 2.0/2.1每帧 | Cyan/Fuchsia |
| SemiCircularSmearVFX | 1.0/1.1每帧 | DoGColor |
| SlashThrough | 2.1命中, 2.2命中 | Fuchsia/DoGColor |
| ChargeUpLineVFX | 2.2蓄力, 右键Portal, 大招 | DoGColor, telegraph=true |
| SquishyLightParticle | 蓄力汇聚, QuickDraw, 大招 | Cyan/Fuchsia |
| CustomPulse | 2.2爆发, QuickDraw, 大招 | 三种纹理同时 |
| TechyHolosquareParticle | 1.2命中, 3.1命中, Portal切换 | 三色+色差 |
| FlatGlow | 1.2命中, QuickDraw, 大招 | DoGColor |
| DoGDistortionMetaball | 2.2命中, QuickDraw, 大招 | 自动DoGPurple边缘 |
| GalaxyMetaball | 3.0命中, 3.2终结, QuickDraw, 大招 | BeforeNPCs层 |
| StarsmokeMetaball | 3.2蓄力, 大招 | Magenta→Coral循环 |
| DoGRiftCrack | 2.2命中, QuickDraw, 大招 | Fuchsia |

**所有粒子类均已分配到具体攻击。无一遗漏。**

---

# 第十章：实施路线图

## P0（立即）— 颜色体系
- 删除所有FrostXxxColor常量
- 添加DoGColor动态属性和静态常量
- 替换ApplyColdDebuffs → ApplyDoGDebuffs(GodSlayerInferno)

## P1（第一阶段）— 轨迹重建
- 实现CosmicDischargeDoGFire（双层ImpFlameTrail）
- 替换DrawCurvedBladeGlow
- 实现PostDraw双层glowmask

## P2（第二阶段）— 普攻特效（从最复杂开始）
- SwordFinisher(2.2)：DetailedExplosion + DoGDistortionMetaball + DoGRiftCrack
- SwordSwingOne/Two(2.0/2.1)：CircularSmearVFX + GenericSparkle + CritSpark
- WhipOver/Under/Thrust(1.0/1.1/1.2)：SemiCircularSmear + BoltParticle + StaticGlowLine

## P3（第三阶段）— 链刃特效
- ChainKnifeArc×2(3.0/3.1)：NanoParticle链条 + ElectricSpark + TechyHolosquare
- ChainKnifeFinisher(3.2)：StarsmokeMetaball + FriendlyLaserWallBeam

## P4（第四阶段）— 特殊技能
- QuickDraw(帧20爆发)：全粒子库集中爆发
- 右键Portal(CosmicDischargeSwitchPortal)：3层旋转门 + 模式切换回调

## P5（最终）— 大招重建
- 删除旧UltimateField
- 实现新大招蓄力（60帧ChargeUpLine + RoundedStar螺旋）
- 实现大招爆发（FriendlyLaserWallBeam + 完整粒子爆炸）
- 实现大招持续场地（DoGFire圈 + NanoParticle海）
