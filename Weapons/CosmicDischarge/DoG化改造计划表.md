# CosmicDischarge 神明吞噬者化 · 完全改造计划表
## 【V3.0 · 参数矫正版 + 塔纳托斯脊椎形态重建】

> 目标：彻底抹除所有冰霜/寒冰痕迹，用CalamityMod全粒子库重建每一个特效。
> 每个攻击形态拥有完全不同的视觉指纹。没有一个攻击用相同的粒子组合。
> V3修正：参数对齐源码实测值；第三形态完全重写对标《塔纳托斯的脊椎》。
> 参考源：《神明吞噬者及其所有周边的特效完整分析.md》（V2.0）

---

# 序章：参数标准（源码校准表）

> 所有参数以实际Calamity源码为基准。"感觉合理"的大数字在这里大多数是错的。

| 粒子类 | 常见错误用法 | 正确参考范围 | 来源 |
|-------|-----------|------------|------|
| SparkParticle scale | 任意 | 0.8f~1.1f | DimensionTearingDisk实测 |
| SparkParticle velocity | 任意 | 3f~6f | DimensionTearingDisk实测 |
| SparkParticle lifetime | 任意 | 14~21帧 | DimensionTearingDisk实测 |
| GlowSparkParticle squash | (0.25,1.8) 极细长 | (0.45f,1.5f) | 类默认值(0.5,1.6) |
| GlowSparkParticle velocity | 8~10f | 3~5f | 对齐SparkParticle |
| SquishyLightParticle scale | 任意 | **0.15f** | DoGTeleportRift源码 |
| ImpactParticle scale | 0.8~1.0f 过大 | 0.3f~0.5f | 星形尺寸单位不同 |
| GenericSparkle scale | 0.3~0.9f | 0.2f~0.4f | 上限收敛 |
| BoltParticle stretch | (0.1,4.0) 极端 | (0.3f,1.5f) | 可见但不过分 |
| StaticGlowLine 目标距离 | 100~300f | 50f~90f | 屏内可见范围 |
| FlareShine scale.Y | 2.5f 刺穿感 | 1.2f | 可见但不尖 |
| FlatGlow originalScale.Y | 3f 巨大 | 1.5f | 适中 |
| TechyHolosquare scale | 0.5~1.0f | 0.25f~0.55f | 适中 |
| StaticPulseRing finalScale | 0.8f+ | 0.3f~0.45f | 不要太大 |
| DetailedExplosion finalScale | 1.5~2.5f | 0.8f~1.3f | 对齐FlameExplosion量级 |
| RoundedStarParticle scale | 0.4~0.8f | 0.25f~0.5f | 对齐星形系列 |

---

# 第零章：全局色彩系统重构

## 0.1 颜色常量替换表

```csharp
// CosmicDischargeCommon.cs 中的常量全部替换：
static readonly Color FrostCoreColor  = new(150, 255, 255);  // 删除
static readonly Color FrostGlowColor  = new(110, 175, 255);  // 删除
static readonly Color FrostDarkColor  = new(58,  84,  150);  // 删除
static readonly Color FrostWhiteColor = new(225, 250, 255);  // 删除

// 新增DoG颜色体系：
public static Color DoGColor =>
    Color.Lerp(Color.Fuchsia, Color.Cyan,
        MathHelper.SmoothStep(0, 1,
            (MathF.Sin(Main.GlobalTimeWrappedHourly * 2) + 1) * 0.5f));

public static readonly Color DoGCyan    = Color.Cyan;
public static readonly Color DoGFuchsia = Color.Fuchsia;
public static readonly Color DoGPurple  = new(136, 26, 186);
public static readonly Color DoGSkyBlue = Color.SkyBlue;
public static readonly Color DoGWhite   = Color.White;

// 三色Spark公式（标准配方）：
public static Color ThreeColorSpark =>
    Color.Lerp(
        Color.Lerp(Color.Fuchsia, Color.AliceBlue, Main.rand.NextFloat(0.5f)),
        Color.Cyan, Main.rand.NextFloat());
```

## 0.2 Debuff替换

```csharp
// 删除 ApplyColdDebuffs
// 新增:
target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

## 0.3 武器轨迹替换（参数已校准）

```csharp
// 外层（IPixelatedPrimitiveRenderer层）:
widthFunction = _ => 58f;   // 参考DoGFire源码72px，武器略小
colorFunction = _ => DoGCyan;  // 常态

// 内层:
widthFunction = _ => 22f;   // 参考DoGFire 24px被动
colorFunction = _ => DoGSkyBlue;  // 常态

// 每帧附加（低频）:
if (Main.rand.NextBool(4))  // 25%概率，不要每帧都触发
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        tipPosition + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextVector2Circular(1.5f, 1.5f),
        DoGColor, 0.35f, 12, emitsLight: true));
```

---

# 第一章：形态0——鞭形DoG（WhipMode）

> 视觉指纹：SemiCircularSmear弧线模糊 + ElectricSpark电弧积蓄；
> 命中：GlowSparkParticle + FlameExplosion（参数已校准，不尖锐）

## 1.0 Combo-0：WhipOver（46帧，从上往下挥）

### 帧0-20（前摇）
```csharp
// 每帧：ElectricSpark在鞭尖（scale=0.4f，速度小）
GeneralParticleHandler.SpawnParticle(new ElectricSpark(
    whipTip + Main.rand.NextVector2Circular(10f, 10f),
    Main.rand.NextVector2Circular(2f, 2f),          // 速度2f，不要3f以上
    color:            DoGColor,
    bloom:            DoGFuchsia,
    scale:            0.4f,                          // 校准: 0.4f
    lifeTime:         10,
    maxJumpRotation:  MathHelper.PiOver4,
    jumpTime:         6f,
    rotationSpeed:    1.2f));

// 每2帧：NanoParticle在鞭柄（低频，氛围）
if (Time % 2 == 0)
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        handlePos + Main.rand.NextVector2Circular(6f, 6f),
        Main.rand.NextVector2Circular(1f, 1f),       // 速度1f，微粒不快
        DoGCyan, 0.35f, 14, emitsLight: true));      // scale=0.35f
```

### 帧10-46（挥鞭）
```csharp
// 每帧：SemiCircularSmearVFX（color乘以0.6f，不要太亮）
GeneralParticleHandler.SpawnParticle(new SemiCircularSmearVFX(
    whipCenter, Vector2.Zero,
    color:    DoGColor * 0.6f,
    rotation: whipAngle,
    squish:   new Vector2(1f, 1.2f),                 // 轻微拉伸，不要1.5以上
    scale:    whipLength / 80f,
    lifeTime: 3));

// 每3帧：HeavySmokeParticle（50%概率）
if (Time % 3 == 0 && Main.rand.NextBool(2))
    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
        whipTip + Main.rand.NextVector2Circular(4f, 4f),
        Main.rand.NextVector2Circular(1.5f, 1.5f) - Vector2.UnitY * 1f,
        DoGFuchsia * 0.5f, scale: 0.7f, lifeTime: 10, opacity: 0.45f));
```

### 命中时
```csharp
// GlowSparkParticle×5（squash对齐类默认值，不做极端尖刺）
for (int i = 0; i < 5; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
        target.Center + Main.rand.NextVector2Circular(6f, 6f),
        Main.rand.NextVector2Circular(4f, 4f),       // 速度4f（DimensionTearingDisk参考5f）
        gravity:      true,
        lifetime:     Main.rand.Next(12, 18),
        scale:        Main.rand.NextFloat(0.7f, 1.0f), // 参考SparkParticle量级
        color:        Main.rand.NextBool() ? DoGCyan : DoGFuchsia,
        squash:       new Vector2(0.45f, 1.5f),      // 校准: 接近类默认(0.5,1.6)
        quickShrink:  false));                        // 不快速变形

// FlameExplosion×2
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    target.Center, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.2f, 0.85f),               // squish温和
    rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.08f, finalScale: 0.5f,          // 校准: finalScale 0.5f
    lifeTime: 12, opacity: 0.75f));
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    target.Center, Vector2.Zero, DoGFuchsia * 0.8f,
    squish: new Vector2(0.85f, 1.2f),
    rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.06f, finalScale: 0.42f,         // 比第一个略小
    lifeTime: 10, opacity: 0.65f));

// StaticPulseRing×1
GeneralParticleHandler.SpawnParticle(new StaticPulseRing(
    target.Center, Vector2.Zero, DoGColor, Vector2.One, 0f,
    originalScale: 0.03f, finalScale: 0.35f,         // 校准: finalScale 0.35f
    lifeTime: 12));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 1.1 Combo-1：WhipUnder（46帧，从下往上挥，Cyan主）

### 帧0-46
```csharp
// 每帧：CircularSmearVFX（Cyan，颜色×0.55f）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    whipCenter, Vector2.Zero,
    color:    DoGCyan * 0.55f,
    rotation: whipAngle + MathHelper.Pi,
    scale:    whipLength / 70f,
    lifeTime: 3));

// 每3帧：BoltParticle（stretch校准，不极端）
if (Time % 3 == 0)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        whipTip, velocity: whipSwingDir * 3f,        // 速度3f
        color:     DoGCyan, glowColor: DoGWhite,
        scale:     0.55f,                             // 校准
        lifetime:  10,
        rotation:  whipSwingDir.ToRotation(),
        stretch:   new Vector2(0.3f, 1.5f),           // 校准: 不是(0.1,4)
        affectedByGravity: false, glowCenter: true, glowFade: true, fadeIn: false));
```

### 命中时
```csharp
// BoltParticle×4（全方向散射，stretch校准）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 5f),
        color:     DoGCyan, glowColor: DoGWhite,
        scale:     0.5f, lifetime: 10,
        rotation:  0f,
        stretch:   new Vector2(0.3f, 1.5f),           // 校准
        affectedByGravity: true, glowCenter: true, glowFade: true, fadeIn: false));

// ImpactParticle×2（scale校准，不要超0.5f）
GeneralParticleHandler.SpawnParticle(new ImpactParticle(
    target.Center, 0.10f, 16, 0.4f, DoGCyan));       // 校准: scale=0.4f
GeneralParticleHandler.SpawnParticle(new ImpactParticle(
    target.Center, 0.07f, 14, 0.32f, DoGWhite));      // 校准: scale=0.32f

// GlowOrbParticle×4
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
        target.Center + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextVector2Circular(4f, 4f), gravity: true,
        lifetime: 12, scale: 0.12f,                  // scale=0.12f (SquishyLight参考0.15f)
        color: DoGCyan, glowCenter: true));

// DirectionalPulseRing
GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
    target.Center, Vector2.Zero, DoGCyan,
    squish:        new Vector2(1.4f, 0.65f),
    rotation:      whipAngle,
    originalScale: 0.03f, finalScale: 0.38f,          // 校准
    lifeTime: 14));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 1.2 Combo-2：WhipThrust（52帧，向前刺，线形爆发）

### 帧0-25（前摇）
```csharp
// 每帧：LineVFX（厚度0.025f，略细）
GeneralParticleHandler.SpawnParticle(new LineVFX(
    player.Center, (whipTip - player.Center),
    thickness: 0.025f, color: DoGColor * 0.35f));

// 每2帧：NanoParticle沿鞭身
if (Time % 2 == 0)
    for (int i = 0; i < 2; i++)                      // 2个而非3个
        GeneralParticleHandler.SpawnParticle(new NanoParticle(
            Vector2.Lerp(player.Center, whipTip, (i + 1) / 3f)
                + Main.rand.NextVector2Circular(5f, 5f),
            Main.rand.NextVector2Circular(1.5f, 1.5f),
            DoGColor, 0.35f, 10, emitsLight: true));
```

### 帧25-52（刺出）
```csharp
// StaticGlowLine从玩家到鞭尖（短距离，不跨越大量屏幕）
GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
    player.Center, whipTip, Vector2.Zero,
    lifetime: 3, xScale: 0.06f, xShrink: 1.0f,       // 校准: xScale=0.06f
    color: DoGColor, glow: true));

// 每3帧：CritSpark（速度2~4f）
if (Time % 3 == 0)
    GeneralParticleHandler.SpawnParticle(new CritSpark(
        whipTip + Main.rand.NextVector2Circular(6f, 6f),
        thrustDir * Main.rand.NextFloat(2f, 4f),       // 校准: 不超4f
        color: DoGColor, bloom: DoGWhite,
        scale: 0.4f, lifeTime: 14,                     // 校准: scale=0.4f
        rotationSpeed: 1f, bloomScale: 1.0f, hueShift: 0.004f));
```

### 命中时
```csharp
// StaticGlowLine×6（距离校准 50~90f）
for (int i = 0; i < 6; i++) {
    Vector2 dir = Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / 6f);
    GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
        target.Center, target.Center + dir * Main.rand.NextFloat(50f, 90f),  // 校准
        Vector2.Zero, 15, xScale: 0.06f, xShrink: 0.88f,
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia, glow: true));
}

// FlatGlow（scale校准）
GeneralParticleHandler.SpawnParticle(new FlatGlow(
    target.Center, Vector2.Zero, DoGColor,
    rotation: thrustDir.ToRotation(),
    originalScale: new Vector2(0.06f, 1.5f),           // 校准: Y=1.5f不是3f
    finalScale:    new Vector2(1.5f, 0.06f),
    lifeTime: 10));

// BloomLineVFX（capped=true）
GeneralParticleHandler.SpawnParticle(new BloomLineVFX(
    player.Center, (target.Center - player.Center) * 1.1f,
    thickness: 0.05f, color: DoGColor,
    lifetime: 7, capped: true));

// GlowSquareParticle×5（scale校准，小碎片）
for (int i = 0; i < 5; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
        target.Center + Main.rand.NextVector2Circular(10f, 10f),
        Main.rand.NextVector2Circular(4f, 4f), gravity: true,
        lifetime: 12, scale: Main.rand.NextFloat(0.04f, 0.08f),  // 校准: 小碎片
        color: ThreeColorSpark, rotation: Main.rand.NextFloat(0.05f)));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

# 第二章：形态1——剑形DoG（SwordMode）

## 2.0 Combo-0：SwordSwingOne（36帧，Cyan主，右挥）

### 帧0-36
```csharp
// 每帧：CircularSmearVFX（color×0.5f）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    bladeTip, Vector2.Zero,
    color: DoGCyan * 0.5f,
    rotation: swingAngle,
    scale: bladeLength / 60f, lifeTime: 4));

// 每2帧：GenericSparkle×1（scale校准 0.25~0.35f）
if (Time % 2 == 0)
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        bladeMidpoint + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(1.5f, 1.5f),     // 速度1.5f
        color: DoGCyan, bloom: DoGWhite,
        scale: Main.rand.NextFloat(0.22f, 0.35f),      // 校准: 0.22~0.35f
        lifeTime: 12, rotationSpeed: 1.2f, bloomScale: 0.8f));

// 帧15（挥砍到位）：FlareShine×3（scale校准，不是(0.2,2.5)）
if (Time == 15) {
    for (int i = 0; i < 3; i++)
        GeneralParticleHandler.SpawnParticle(new FlareShine(
            bladeTip, Main.rand.NextVector2Circular(1.5f, 1.5f),
            DoGWhite, DoGCyan, MathHelper.PiOver2 * i,
            scale:      new Vector2(0.12f, 1.2f),      // 校准: Y=1.2f
            finalScale: new Vector2(0.03f, 0.25f),
            lifeTime: 10, spawnDelay: i));
}
```

### 命中时
```csharp
// ImpactParticle×2（scale校准）
GeneralParticleHandler.SpawnParticle(new ImpactParticle(target.Center, 0.10f, 16, 0.4f, DoGCyan));
GeneralParticleHandler.SpawnParticle(new ImpactParticle(target.Center, 0.07f, 14, 0.3f, DoGFuchsia));

// GlowSparkParticle×5（参数全校准）
for (int i = 0; i < 5; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
        target.Center + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(4f, 4f), gravity: true,
        lifetime: Main.rand.Next(12, 18),
        scale: Main.rand.NextFloat(0.7f, 1.0f),
        color: DoGCyan, squash: new Vector2(0.45f, 1.5f),  // 校准
        quickShrink: false));

// StaticPulseRing（scale校准）
GeneralParticleHandler.SpawnParticle(new StaticPulseRing(
    target.Center, Vector2.Zero, DoGCyan, Vector2.One, 0f,
    originalScale: 0.03f, finalScale: 0.32f,  // 校准: 0.32f
    lifeTime: 12));

GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    target.Center, Vector2.Zero, DoGCyan,
    squish: Vector2.One, rotation: 0f,
    originalScale: 0.08f, finalScale: 0.5f, lifeTime: 13, opacity: 0.7f));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 2.1 Combo-1：SwordSwingTwo（36帧，Fuchsia主，反挥）

### 帧0-36
```csharp
// 每帧：CircularSmearVFX（Fuchsia）
GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
    bladeTip, Vector2.Zero, DoGFuchsia * 0.5f,
    rotation: swingAngle + MathHelper.Pi,
    scale: bladeLength / 60f, lifeTime: 4));

// 每3帧：FancyStars（scale 0.2~0.4f）
if (Main.rand.NextBool(3))
    GeneralParticleHandler.SpawnParticle(new FancyStars(
        bladeTip + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextFloat(MathHelper.TwoPi),
        scale: Main.rand.NextFloat(0.2f, 0.38f),      // 校准: 0.2~0.38f
        velocity: Main.rand.NextVector2Circular(2f, 2f),
        rotationSpeed: Main.rand.NextFloat(0.04f, 0.08f),
        lifeTime: 14,
        color: Main.rand.NextBool() ? DoGFuchsia : DoGColor));
```

### 命中时
```csharp
// SlashThrough（贯穿斩击线）
GeneralParticleHandler.SpawnParticle(new SlashThrough(
    DoGFuchsia, target.Center, swingDir.ToRotation(), 14, target));

// GlowSparkParticle×6（参数全校准，不尖锐）
for (int i = 0; i < 6; i++)
    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
        target.Center + Main.rand.NextVector2Circular(8f, 8f),
        Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 1f,
        gravity: true, lifetime: Main.rand.Next(12, 18),
        scale: Main.rand.NextFloat(0.7f, 1.0f),
        color: ThreeColorSpark,
        squash: new Vector2(0.45f, 1.5f),             // 校准: 不是(0.22,1.9)
        quickShrink: false));

// CritSpark×4（hueShift，速度校准2~3f）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new CritSpark(
        target.Center + Main.rand.NextVector2Circular(10f, 10f),
        Main.rand.NextVector2Circular(2f, 3f),         // 校准: 2~3f
        DoGFuchsia, DoGWhite,
        scale: 0.4f, lifeTime: 18,                     // 校准: scale=0.4f
        rotationSpeed: 1.2f, bloomScale: 1.0f, hueShift: 0.005f));

// DirectionalPulseRing
GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
    target.Center, Vector2.Zero, DoGFuchsia,
    squish: new Vector2(1.5f, 0.65f), rotation: swingDir.ToRotation(),
    originalScale: 0.03f, finalScale: 0.4f, lifeTime: 13));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 2.2 Combo-2：SwordFinisher（72帧，双色终结斩）

### 帧0-36（蓄力）
```csharp
// 每5帧：ConstellationRingVFX（offset=25f，scale=0.6f）
if (Time % 5 == 0)
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Vector2.Zero, starAmount: 5, spinSpeed: 0.03f,
        offset: 25f, color: DoGColor, scale: 0.6f, lifeTime: 18));

// 每4帧：ChargeUpLineVFX×3（距离100f，不是120f）
if (Time % 4 == 0)
    for (int i = 0; i < 3; i++) {
        float dir = i * MathHelper.TwoPi / 3f + Time * 0.04f;
        GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(
            player.Center + dir.ToRotationVector2() * 100f,
            dir + MathHelper.Pi, 0.03f, DoGColor, 18, telegraph: true));
    }

// 每2帧：NanoParticle汇聚
GeneralParticleHandler.SpawnParticle(new NanoParticle(
    player.Center + Main.rand.NextVector2Circular(80f, 80f),
    向player.Center * 3f, DoGColor, 0.35f, 18, emitsLight: true));
```

### 帧36（爆发瞬间）
```csharp
CustomPulse(DoGWhite * 0.8f, 0.25f, 1.8f, "CalamityMod/Particles/ShineExplosion1")
CustomPulse(DoGCyan  * 0.7f, 0.4f,  2.2f, "CalamityMod/Particles/PlasmaExplosion")
CustomPulse(DoGFuchsia*0.6f, 0.55f, 2.6f, "CalamityMod/Particles/ShineExplosion2")
PlayerCenteredPulseRing(player, Zero, DoGCyan,    One, 0f, 0.03f, 0.45f, 15)
PlayerCenteredPulseRing(player, Zero, DoGFuchsia, One, 0f, 0.05f, 0.65f, 18)
StrongBloom(player.Center, DoGWhite, scale=1.2f)   // 校准: scale=1.2f
CalamityUtils.AddScreenshakeAt(player.Center, 3.5f)
```

### 命中时
```csharp
// DetailedExplosion三连（finalScale校准 ≤1.0f）
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.2f, 0.85f), rotation: 0f,
    originalScale: 0.2f, finalScale: 1.0f,            // 校准: finalScale=1.0f
    lifeTime: 20));
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGWhite,
    squish: Vector2.One, rotation: MathHelper.PiOver4,
    originalScale: 0.28f, finalScale: 0.85f, lifeTime: 18));
GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
    target.Center, Vector2.Zero, DoGFuchsia * 0.85f,
    squish: new Vector2(0.85f, 1.2f), rotation: MathHelper.Pi / 3f,
    originalScale: 0.25f, finalScale: 0.9f, lifeTime: 17));

// DoGDistortionMetaball（数量校准）
for (int i = 0; i < 8; i++)                          // 8个square（不是12个）
    DoGDistortionMetaball.SpawnSquare(
        target.Center + Main.rand.NextVector2Circular(40f, 40f),
        Main.rand.NextVector2Circular(3f, 3f),
        Main.rand.NextFloat(25f, 55f));               // size校准
for (int i = 0; i < 4; i++)
    DoGDistortionMetaball.SpawnCircle(
        target.Center + Main.rand.NextVector2Circular(25f, 25f),
        Vector2.Zero, Main.rand.NextFloat(15f, 35f));

// DoGRiftCrack×5（不是8条）
for (int i = 0; i < 5; i++)
    Projectile.NewProjectile(...DoGRiftCrack,
        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f), ...);

// SlashThrough
GeneralParticleHandler.SpawnParticle(new SlashThrough(
    DoGColor, target.Center, swingDir.ToRotation(), 16, target));

// GenericSparkle×8（scale校准 0.2~0.38f）
for (int i = 0; i < 8; i++)
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        target.Center + Main.rand.NextVector2Circular(15f, 15f),
        Main.rand.NextVector2Circular(4f, 4f),        // 速度4f
        color: ThreeColorSpark, bloom: DoGWhite,
        scale: Main.rand.NextFloat(0.2f, 0.38f),      // 校准
        lifeTime: Main.rand.Next(18, 28),
        rotationSpeed: 1.5f, bloomScale: 1.2f));

// ImpactParticle×3（scale校准 0.3~0.5f）
for (int i = 0; i < 3; i++)
    GeneralParticleHandler.SpawnParticle(new ImpactParticle(
        target.Center + Main.rand.NextVector2Circular(4f, 4f),
        angularVelocity: 0.08f + i * 0.06f,
        lifetime: 16 - i * 2,
        scale: 0.32f + i * 0.1f,                      // 校准: 0.32/0.42/0.52f
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia));

// StaticGlowLine×8（距离校准 50~90f）
for (int i = 0; i < 8; i++) {
    Vector2 dir = Main.rand.NextVector2Unit();
    GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
        target.Center, target.Center + dir * Main.rand.NextFloat(50f, 90f),  // 校准
        dir * 0.3f, 15, xScale: 0.07f, xShrink: 0.88f,
        color: i % 2 == 0 ? DoGCyan : DoGFuchsia, glow: true));
}

StrongBloom(target.Center, DoGWhite, scale=1.5f)      // 校准
CalamityUtils.AddScreenshakeAt(target.Center, 7f)
SoundEngine.PlaySound(SoundID.Item122, target.Center)
target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
```

---

# 第三章：形态2——塔纳托斯脊椎DoG化（ChainMode）

> **核心参考**：SpineOfThanatosProjectile.cs
> SpineOfThanatos是一把通道型链鞭：2条Bezier曲线链（左右各一）+ 1条直射链。
> 链节由Body1/Body2/Tail分段纹理绘制（交替），带glowmask。
> 在直射链拉回时（FlyBackTime=40帧）触发 CreateBadassPrismExplosion：
>   → ThanatosBoom（圆形dust爆炸）
>   → PrismRay ×12（扇形彩虹光线，用ExoPalette着色，30帧存活）
>
> **CosmicDischarge链刃形态 = DoG化的塔纳托斯**：
> - 链节外观：SpineOfThanatosBody1/Body2/Tail纹理 + DoG色Glowmask叠加
> - Combo-0 = 单条左弧（SwingDirection=+1）
> - Combo-1 = 双条同时左右弧（同时生成2个弹幕）
> - Combo-2 = 直射链 → 拉回时爆发扇形DoG激光（12条PrismRay风格）

## 3.0 Combo-0：ChainArc单弧（64帧，Cyan主）

### 绘制链节（PreDraw）
```csharp
// 参考SpineOfThanatosProjectile.cs PreDraw：
// 每个链节i交替使用Body1/Body2，首节用Tail，末节用主纹理
for (int i = 0; i < whipPoints.Count - 1; i++) {
    string segTex = i == 0 ? "SpineOfThanatosTail"
                   : $"SpineOfThanatosBody{i % 2 + 1}";  // 交替Body1/Body2
    Texture2D tex     = segTex.Request();
    Texture2D glowTex = (segTex + "Glowmask").Request();

    float rot = (whipPoints[i+1] - whipPoints[i]).ToRotation() + PiOver2;

    // 基础纹理（原始光照颜色）
    Main.EntitySpriteDraw(tex,     whipPoints[i] - Main.screenPosition, null,
        Lighting.GetColor(whipPoints[i].ToTileCoordinates()), rot, origin, scale, None, 0);

    // DoG glowmask叠加（Cyan/Fuchsia交替，加法混合）
    Color glowColor = (i % 4 < 2) ? DoGCyan * 0.7f : DoGFuchsia * 0.6f;
    Main.EntitySpriteDraw(glowTex, whipPoints[i] - Main.screenPosition, null,
        glowColor, rot, origin, scale, None, 0);
}
```

### 每帧特效（AI中）
```csharp
// 每4帧沿链条：ElectricSpark×1（scale=0.35f，小）
if (Time % 4 == 0) {
    int randSeg = Main.rand.Next(whipPoints.Count);
    GeneralParticleHandler.SpawnParticle(new ElectricSpark(
        whipPoints[randSeg] + Main.rand.NextVector2Circular(5f, 5f),
        Main.rand.NextVector2Circular(1.5f, 1.5f),    // 速度1.5f，紧贴链条
        color:           DoGCyan, bloom: DoGFuchsia,
        scale:           0.35f,                        // 校准: 0.35f
        lifeTime:        8,
        maxJumpRotation: MathHelper.PiOver4,
        jumpTime:        5f));
}

// 每帧：链刃尖NanoParticle×1
GeneralParticleHandler.SpawnParticle(new NanoParticle(
    bladeTip + Main.rand.NextVector2Circular(5f, 5f),
    Main.rand.NextVector2Circular(1f, 1f),
    DoGCyan, 0.32f, 10, emitsLight: true));
```

### 命中时
```csharp
// ImpactParticle×2（六叉旋转星，scale校准）
GeneralParticleHandler.SpawnParticle(new ImpactParticle(bladeTip, 0.10f, 15, 0.38f, DoGCyan));
GeneralParticleHandler.SpawnParticle(new ImpactParticle(bladeTip, 0.07f, 13, 0.28f, DoGWhite));

// BoltParticle×3（stretch校准）
for (int i = 0; i < 3; i++)
    GeneralParticleHandler.SpawnParticle(new BoltParticle(
        target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 5f),
        color: DoGCyan, glowColor: DoGWhite,
        scale: 0.45f, lifetime: 10,
        rotation: Main.rand.NextFloat(MathHelper.TwoPi),
        stretch: new Vector2(0.3f, 1.4f),             // 校准
        affectedByGravity: true, glowCenter: true, glowFade: true, fadeIn: false));

// GlowOrbParticle×3
for (int i = 0; i < 3; i++)
    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
        target.Center, Main.rand.NextVector2Circular(4f, 4f), gravity: false,
        lifetime: 12, scale: 0.12f, color: DoGCyan, glowCenter: true));

// StaticPulseRing×1
GeneralParticleHandler.SpawnParticle(new StaticPulseRing(
    target.Center, Vector2.Zero, DoGCyan, Vector2.One, 0f,
    originalScale: 0.03f, finalScale: 0.32f, lifeTime: 12));

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 3.1 Combo-1：ChainArc双弧（64帧，同时左右，Fuchsia+Cyan对称）

> 参考 SpineOfThanatosItem.Shoot()：同时生成 SwingDirection=+1 和 -1 两条弹幕。

### 机制
```csharp
// Item.Shoot中同时生成2个ChainModeHoldout弹幕:
Projectile.NewProjectile(source, pos, vel, type, damage, kb, owner, 0f, +1f);  // 右弧
Projectile.NewProjectile(source, pos, vel, type, damage, kb, owner, 0f, -1f);  // 左弧
// SwingDirection通过AI[1]传递，控制弯曲方向
```

### 左弧绘制（SwingDirection=-1，Fuchsia主）
```csharp
// glowmask用Fuchsia/Purple交替（区分于Combo-0的Cyan/Fuchsia）
Color glowColor = (i % 4 < 2) ? DoGFuchsia * 0.7f : DoGPurple * 0.5f;
```

### 每帧特效（每条链分别执行）
```csharp
// 每4帧：链上GlowSquareParticle×1（Fuchsia主）
if (Time % 4 == 0) {
    int randSeg = Main.rand.Next(whipPoints.Count);
    GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
        whipPoints[randSeg] + Main.rand.NextVector2Circular(4f, 4f),
        Main.rand.NextVector2Circular(1.5f, 1.5f), gravity: false,
        lifetime: 8, scale: Main.rand.NextFloat(0.04f, 0.07f),  // 校准: 小碎片
        color: DoGFuchsia,
        rotation: Main.rand.NextFloat(0.04f, 0.1f)));
}

// 链刃尖：FancyStars×1（低频，scale 0.2~0.35f）
if (Main.rand.NextBool(5))
    GeneralParticleHandler.SpawnParticle(new FancyStars(
        bladeTip + Main.rand.NextVector2Circular(6f, 6f),
        Main.rand.NextFloat(MathHelper.TwoPi),
        scale: Main.rand.NextFloat(0.2f, 0.35f),      // 校准
        velocity: Main.rand.NextVector2Circular(2f, 2f),
        rotationSpeed: Main.rand.NextFloat(0.04f, 0.08f),
        lifeTime: 12, color: DoGFuchsia));
```

### 命中时（每条链独立触发）
```csharp
// RoundedStarParticle×4（spiral=false，deceleration减速飞出）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(
        target.Center + Main.rand.NextVector2Circular(15f, 15f),
        Main.rand.NextVector2Circular(3f, 3f),
        color: i % 2 == 0 ? DoGFuchsia : DoGColor,
        scale: Main.rand.NextFloat(0.25f, 0.45f),     // 校准: 0.25~0.45f
        lifetime: 20,
        rotationSpeed: 0.04f, deceleration: 0.93f,
        useSpiralAI: false, spiralTarget: Vector2.Zero, ownerIndex: player.whoAmI));

// TechyHolosquareParticle×4（scale校准 0.25~0.5f）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(
        target.Center + Main.rand.NextVector2Circular(12f, 12f),
        Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f, 5f),  // 速度校准
        scale: Main.rand.NextFloat(0.25f, 0.5f),      // 校准
        color: ThreeColorSpark, lifetime: 14, opacity: 0.8f));

// ImpactParticle×1（Fuchsia）
GeneralParticleHandler.SpawnParticle(new ImpactParticle(
    target.Center, 0.12f, 15, 0.42f, DoGFuchsia));   // 校准: scale=0.42f

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
```

---

## 3.2 Combo-2：ChainFinisher — 直射链 + 扇形PrismRay爆发（80帧）

> **核心参考**：SpineOfThanatosProjectile.CreateBadassPrismExplosion()
> 原版：ThanatosBoom + 12条PrismRay（ExoPalette彩虹，fan spread ±0.57rad，终点距420f）
> DoG化版：DoG火焰爆炸 + 12条FriendlyLaserWallBeam风格DoG彩虹射线扇形

### 帧0-50（直射链飞出阶段，参考SwingDirection=0）
```csharp
// 链条沿直线飞出（不弯曲，MaximumBendFactor=0）
// 每帧：链上NanoParticle（更密集，因为直射速度更快）
if (Time % 3 == 0) {
    int randSeg = Main.rand.Next(whipPoints.Count);
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        whipPoints[randSeg] + Main.rand.NextVector2Circular(4f, 4f),
        Main.rand.NextVector2Circular(1.5f, 1.5f),
        DoGColor, 0.35f, 10, emitsLight: true));
}

// 链刃尖积蓄感：每4帧ConstellationRingVFX（scale=0.5f，offset=20f）
if (Time % 4 == 0 && Time > 15)
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        bladeTip, Vector2.Zero, starAmount: 5, spinSpeed: 0.05f,
        offset: 20f, color: DoGColor, scale: 0.5f, lifeTime: 12));
```

### 帧50（FlyBackTime触发，链开始拉回，同时触发爆发）

> 参考 SpineOfThanatos: `if (SwingDirection == 0f && Projectile.timeLeft == FlyBackTime) CreateBadassPrismExplosion()`

```csharp
// ★★★ ThanatosBoom DoG化版 ★★★
// 原版是RainbowMk2 dust，DoG化成DoGDistortionMetaball + FlameExplosion

// FlameExplosion×2（中型，参考ThanatosBoom的爆炸半径54px）
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    bladeTip, Vector2.Zero, DoGCyan,
    squish: new Vector2(1.1f, 0.9f), rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.12f, finalScale: 0.65f, lifeTime: 15, opacity: 0.75f));
GeneralParticleHandler.SpawnParticle(new FlameExplosion(
    bladeTip, Vector2.Zero, DoGFuchsia * 0.8f,
    squish: new Vector2(0.9f, 1.1f), rotation: Main.rand.NextFloat(MathHelper.TwoPi),
    originalScale: 0.1f, finalScale: 0.55f, lifeTime: 13, opacity: 0.65f));

// DoGDistortionMetaball×6（中等数量，不要像大招那么多）
for (int i = 0; i < 6; i++)
    DoGDistortionMetaball.SpawnSquare(
        bladeTip + Main.rand.NextVector2Circular(30f, 30f),
        Main.rand.NextVector2Circular(2.5f, 2.5f),
        Main.rand.NextFloat(20f, 45f));

// CustomPulse×2
CustomPulse(DoGWhite*0.75f, 0.2f, 1.6f, "CalamityMod/Particles/ShineExplosion1")
CustomPulse(DoGColor*0.65f, 0.3f, 1.9f, "CalamityMod/Particles/PlasmaExplosion")

// ★★★ PrismRay DoG化版 ★★★
// 原版：12条ExoPalette彩虹射线，spread ±0.57rad，终点420f
// DoG化：12条FriendlyLaserWallBeam，Fuchsia/Cyan颜色，扇形向前散开
NPC potentialTarget = bladeTip.ClosestNPCAt(700f);  // 参考原版700f检测范围
int rayCount = 12;                                    // 参考原版12条
for (int i = 0; i < rayCount; i++) {
    // 原版: rayRotation = Projectile.rotation + Lerp(-0.57f, 0.57f, i/(float)rayCount)
    float rayRot = Projectile.rotation + MathHelper.Lerp(-0.57f, 0.57f, i / (float)rayCount);
    Vector2 rayEnd;

    if (potentialTarget != null &&
        Projectile.rotation.ToRotationVector2().AngleBetween(
            (potentialTarget.Center - bladeTip).SafeNormalize(Vector2.Zero))
        < MathHelper.Pi * 0.27f)
    {
        // 有目标且对准：全部射向目标（集束，高伤害）
        rayEnd = potentialTarget.Center;
    }
    else {
        // 无目标：扇形散开，参考原版420f
        rayEnd = bladeTip + rayRot.ToRotationVector2() * 420f;
    }

    // DoG化：用FriendlyLaserWallBeam代替PrismRay（从端点向bladeTip方向射入）
    int ray = Projectile.NewProjectile(source,
        rayEnd, (bladeTip - rayEnd).SafeNormalize(Vector2.Zero),
        typeof(FriendlyLaserWallBeam),
        (int)(damage * 0.8f), 0f, owner,
        ai0: MathHelper.Lerp(-1f, 1f, i / (float)rayCount));  // 偏转模拟PrismRay扇形
    if (Main.projectile.IndexInRange(ray))
        Main.projectile[ray].scale *= 0.35f;                   // 校准: 细版激光

    // 每条射线起点放1个GenericSparkle（DoG色）
    Color rayColor = i % 2 == 0 ? DoGCyan : DoGFuchsia;
    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
        rayEnd, Vector2.Zero,
        color: rayColor, bloom: DoGWhite,
        scale: 0.25f, lifeTime: 15,                            // 校准: scale=0.25f
        rotationSpeed: 1.5f, bloomScale: 1.0f));
}

// GalaxyMetaball×4（宇宙背景，Thanatos大爆炸的空间感）
for (int i = 0; i < 4; i++)
    GalaxyMetaball.Particles.Add(new CosmicParticle(
        bladeTip + Main.rand.NextVector2Circular(40f, 40f),
        Main.rand.NextVector2Circular(3f, 3f), size: Main.rand.NextFloat(20f, 40f)));

// StarsmokeMetaball×3
for (int i = 0; i < 3; i++)
    StarsmokeMetaball.SpawnParticle(
        bladeTip + Main.rand.NextVector2Circular(20f, 20f),
        Main.rand.NextVector2Circular(2f, 2f),
        size: Main.rand.NextFloat(12f, 28f), lifetime: 25,
        squash: new Vector2(0.85f, 1.15f),
        shrinkSpeed: 0.12f, velocitySquash: 0.4f);

// 音效（参考原版：SoundID.DD2_DarkMageHealImpact）
SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Pitch = 0.15f }, bladeTip);
CalamityUtils.AddScreenshakeAt(bladeTip, 6f)
StrongBloom(bladeTip, DoGWhite, scale=1.4f)

target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
```

---

# 第四章：QuickDraw（48帧，任何形态中途右键）

> DoGTeleportRift风格闪现+爆炸。参数同样遵循校准原则。

## 4.1 帧0-20（蓄力）

```csharp
// 每帧：NanoParticle×4汇聚（scale=0.4f，速度4f）
for (int i = 0; i < 4; i++)
    GeneralParticleHandler.SpawnParticle(new NanoParticle(
        player.Center + Main.rand.NextVector2Circular(50f, 50f),
        向player.Center * 4f, DoGColor, 0.4f, 14, bigSize: true, emitsLight: true));

// 每4帧：ChargeUpLineVFX×5（距离70f）
if (Time % 4 == 0)
    for (int i = 0; i < 5; i++) {
        float dir = i * MathHelper.TwoPi / 5f;
        GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(
            player.Center + dir.ToRotationVector2() * 70f,  // 校准: 70f
            dir + MathHelper.Pi, 0.03f, DoGColor, 16, telegraph: true));
    }

// 每帧：SquishyLightParticle×3（scale=0.15f，参考DoGTeleportRift源码）
for (int i = 0; i < 3; i++)
    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
        velocity: Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 8f),
        color: DoGColor, squishRatio: 0.5f, lifetime: 10,
        scale: 0.15f,                                  // ★ 源码校准: 0.15f ★
        hueShift: 0.003f));

// 帧0：ConstellationRingVFX×2（双圈）
if (Time == 0) {
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Zero, 8, 0.03f, 50f, DoGCyan, 0.7f, 22));
    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
        player.Center, Zero, 6, -0.025f, 38f, DoGFuchsia, 0.65f, 20));
}

// 帧0：RoundedStarParticle×8螺旋汇聚（scale校准）
if (Time == 0) {
    for (int i = 0; i < 8; i++)
        GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(
            player.Center + Main.rand.NextVector2Circular(65f, 65f),
            Zero, DoGColor, Main.rand.NextFloat(0.25f, 0.45f),  // 校准
            20, 0.04f, 1f, useSpiralAI: true,
            spiralTarget: player.Center, ownerIndex: player.whoAmI));
}
```

## 4.2 帧20（爆炸，参数全校准）

```csharp
// DetailedExplosion三连（finalScale ≤1.0f）
DetailedExplosion(mousePos, Zero, DoGCyan,    (1.2f,0.85f), 0,    0.2f, 1.0f, 22)
DetailedExplosion(mousePos, Zero, DoGWhite,   (1f,1f),      Pi/4, 0.28f,0.85f,20)
DetailedExplosion(mousePos, Zero, DoGFuchsia, (0.85f,1.2f), Pi/2, 0.25f,0.9f, 18)

// FlameExplosion×3
FlameExplosion(mousePos, Zero, DoGCyan,    (1.15f,0.9f), rand*Pi, 0.1f, 0.55f, 14, 0.72f)
FlameExplosion(mousePos, Zero, DoGFuchsia, (0.9f,1.15f), rand*Pi, 0.08f,0.48f, 12, 0.65f)
FlameExplosion(mousePos, Zero, DoGWhite,   (1f,1f),      rand*Pi, 0.06f,0.38f, 10, 0.6f)

// DoGDistortionMetaball（参考DoGTeleportRift爆炸，数量适中）
for (int i = 0; i < 18; i++)  // 18个square（DoGTeleportRift用35，我们一半）
    DoGDistortionMetaball.SpawnSquare(mousePos + rand.NextVector2Circular(60f,60f),
        rand.NextVector2Circular(4f,4f), rand.NextFloat(20f,50f));
for (int i = 0; i < 8; i++)
    DoGDistortionMetaball.SpawnCircle(mousePos + rand.NextVector2Circular(40f,40f),
        Zero, rand.NextFloat(12f,35f));

// DoGRiftCrack×10（参考DoGTeleportRift的25，我们40%）
for (int i = 0; i < 10; i++)
    NewProjectile(DoGRiftCrack, rand.NextVector2Unit() * rand.NextFloat(3f,9f))

// GenericSparkle×10（scale校准）
for (int i = 0; i < 10; i++)
    GenericSparkle(mousePos + rand.NextVector2Circular(12f,12f),
        rand.NextVector2Circular(3.5f,3.5f),          // 速度3.5f
        ThreeColorSpark, DoGWhite,
        Main.rand.NextFloat(0.2f, 0.35f),             // 校准: 0.2~0.35f
        rand.Next(16, 28), 1.5f, 1.0f)

// ImpactParticle×3（scale校准）
ImpactParticle(mousePos, 0.12f, 18, 0.45f, DoGCyan)
ImpactParticle(mousePos, 0.09f, 16, 0.35f, DoGFuchsia)
ImpactParticle(mousePos, 0.06f, 14, 0.28f, DoGWhite)

// FlareShine×6（scale校准）
for (int i = 0; i < 6; i++)
    FlareShine(mousePos + rand.NextVector2Circular(15f,15f),
        rand.NextVector2Circular(1.5f,1.5f),
        DoGWhite, DoGCyan,
        angle: i * MathHelper.Pi / 3f,
        scale: new Vector2(0.1f, 1.2f),               // 校准: Y=1.2f
        finalScale: new Vector2(0.02f, 0.2f),
        lifeTime: 12, spawnDelay: i / 2)

// FancyStars×10（scale校准）
for (int i = 0; i < 10; i++)
    FancyStars(mousePos + rand.NextVector2Circular(25f,25f),
        rand.NextFloat(MathHelper.TwoPi),
        Main.rand.NextFloat(0.2f, 0.4f),              // 校准
        rand.NextVector2Circular(5f,5f), rand.NextFloat(0.04f, 0.09f),
        18, ThreeColorSpark)

// CustomPulse×3
CustomPulse(DoGCyan*0.85f,    0.2f, 2.2f, "ShineExplosion1")
CustomPulse(DoGWhite*0.8f,    0.32f,2.6f, "PlasmaExplosion")
CustomPulse(DoGFuchsia*0.75f, 0.45f,3.0f, "ShineExplosion2")

// StaticGlowLine×10（距离校准 50~90f）
for (int i = 0; i < 10; i++) {
    Vector2 dir = rand.NextVector2Unit();
    StaticGlowLine(mousePos, mousePos + dir * rand.NextFloat(50f, 90f),  // 校准
        dir * 0.3f, 16, 0.06f, 0.87f,
        i%2==0 ? DoGCyan : DoGFuchsia, true)
}

// GlowSquareParticle×6（scale校准）
for (int i = 0; i < 6; i++)
    GlowSquareParticle(mousePos + rand.NextVector2Circular(15f,15f),
        rand.NextVector2Circular(4f,4f), true, 14,
        rand.NextFloat(0.04f, 0.08f),                  // 校准: 小碎片
        ThreeColorSpark, true, rand.NextFloat(0.06f,0.12f))

// SquishyLightParticle×10（scale校准=0.15f）
for (int i = 0; i < 10; i++)
    SquishyLightParticle(rand.NextVector2Unit() * rand.NextFloat(6f, 14f),
        ThreeColorSpark, 0.5f, 12, 0.15f, hueShift: 0.004f)  // ★ 0.15f ★

// FlatGlow×3（scale校准）
for (int i = 0; i < 3; i++)
    FlatGlow(mousePos, Zero, DoGColor, i*MathHelper.Pi/1.5f,
        new Vector2(0.06f, 1.4f), new Vector2(1.4f, 0.06f), 12)  // 校准

// GalaxyMetaball×5
for (int i = 0; i < 5; i++)
    GalaxyMetaball.Particles.Add(new CosmicParticle(
        mousePos + rand.NextVector2Circular(45f,45f),
        rand.NextVector2Circular(4f,4f), rand.NextFloat(18f,40f)))

// 全屏激光（向最近敌人，细版）
if (nearestEnemy != null) {
    int ray = NewProjectile(FriendlyLaserWallBeam, mousePos + dir*2016f, -dir,
        damage*0.7f, ai0: 0f)
    Main.projectile[ray].scale *= 0.35f;               // 细版
}

StrongBloom(mousePos, DoGWhite, scale=1.5f)            // 校准
CalamityUtils.AddScreenshakeAt(mousePos, 10f)
SoundEngine.PlaySound("DoGLaserWallBigAttack", mousePos)
```

---

# 第五章：右键模式切换（Galaxia风格Portal，18帧）

## CosmicDischargeSwitchPortal弹幕

```csharp
// 位置: player.Top，生命周期18帧，0伤害
// 3层旋转Portal（参考StreamGougePortal）

// PreDraw（每帧）:
Color[] layerColors = { Color.Black, DoGCyan, DoGFuchsia };
float[] rotSpeeds   = { 0.015f, 0.04f, 0.07f };  // 参考StreamGouge不同速度
// 绘制3次同一纹理，不同旋转速度

// AI每帧特效:
// 帧0-8（开启阶段）
if (Time < 8) {
    // 每帧：NanoParticle×2汇聚
    for (int i = 0; i < 2; i++)
        NanoParticle(Projectile.Center + rand.NextVector2Circular(35f,35f),
            向Projectile.Center * 3f, DoGColor, 0.32f, 12)

    // 每2帧：TechyHolosquareParticle×1散出
    if (Time % 2 == 0)
        TechyHoloysquareParticle(Projectile.Center + rand.NextVector2Circular(10f,10f),
            rand.NextVector2Unit() * 3f, 0.3f, DoGColor, 12, 0.75f)
}

// 帧4：ConstellationRingVFX（scale=0.5f）
if (Time == 4)
    ConstellationRingVFX(Projectile.Center, Zero, 6, 0.04f, 28f, DoGColor, 0.5f, 16)

// 帧8（模式切换发生）:
if (Time == 8) {
    ToggleAttackMode();  // ★ 实际切换 ★
    CustomPulse(DoGWhite, 0.2f, 1.5f, "ShineExplosion2")
    CustomPulse(DoGColor, 0.28f,1.8f, "PlasmaExplosion")
    TechyHolosquareParticle ×6 散射
    StrongBloom(Projectile.Center, DoGWhite, scale=1.0f)
    CalamityUtils.AddScreenshakeAt(Projectile.Center, 3f)
    SoundEngine.PlaySound("DemonSwordKillMode" or SoundID.Item122 with Pitch=0.5f)
}

// 帧8-18（关闭阶段）
// 每帧：ElectricSpark×1消散
ElectricSpark(Projectile.Center + rand.NextVector2Circular(15f,15f),
    rand.NextVector2Circular(1.5f,1.5f), DoGColor, DoGFuchsia, 0.3f, 8)
```

---

# 第六章：大招重设计（参数同步校准）

## 6.1 蓄力（60帧，参数校准）

```csharp
// 每3帧：ChargeUpLineVFX×8（距离校准130f，不是150f）
if (Time % 3 == 0)
    for (int i = 0; i < 8; i++) {
        float dir = i * MathHelper.TwoPi / 8f + Time * 0.015f;
        ChargeUpLineVFX(player.Center + dir.ToRotVec2() * 130f,
            dir + MathHelper.Pi, 0.03f, DoGColor, 18, telegraph: true)
    }

// RoundedStarParticle×10螺旋（scale校准 0.3~0.5f）
if (Time == 0)
    for (int i = 0; i < 10; i++)
        RoundedStarParticle(player.Center + rand.NextVector2Circular(80f,80f),
            Zero, ThreeColorSpark, rand.NextFloat(0.3f, 0.5f), 62,  // 校准
            0.04f, 1f, useSpiralAI: true, player.Center, player.whoAmI)

// 每10帧：PlayerCenteredPulseRing（finalScale校准）
if (Time % 10 == 0) {
    PlayerCenteredPulseRing(player, Zero, DoGCyan,    One, 0f, 0.03f, 0.45f, 25)  // 校准
    PlayerCenteredPulseRing(player, Zero, DoGFuchsia, One, 0f, 0.05f, 0.65f, 28)
}

// 每2帧：NanoParticle×4
for (int i = 0; i < 4; i++)
    NanoParticle(player.Center + rand.NextVector2Circular(100f,100f),
        向player.Center * 3.5f, DoGColor, rand.NextFloat(0.3f,0.6f), 18, emitsLight: true)

// screenshake渐增（0→4f）
CalamityUtils.AddScreenshakeAt(player.Center, Time / 60f * 4f)
```

## 6.2 爆发（帧60，参数全校准）

```csharp
// FriendlyLaserWallBeam：每个目标3条（主+2偏转）
// scale *= 0.35f  校准细版

// DetailedExplosion三连（finalScale ≤1.0f）
DetailedExplosion(player.Center, Zero, DoGCyan,    (1.3f,0.8f), 0,    0.3f, 1.0f, 26)
DetailedExplosion(player.Center, Zero, DoGWhite,   (1f,1f),     Pi/5, 0.45f,0.85f,24)
DetailedExplosion(player.Center, Zero, DoGFuchsia, (0.8f,1.3f), Pi/3, 0.4f, 0.9f, 22)

// DoGDistortionMetaball（DoGTeleportRift的60%数量）
Square ×20,  Circle ×10

// DoGRiftCrack×15（DoGTeleportRift的60%）
// GenericSparkle×12 (scale 0.2~0.35f)
// ImpactParticle×4 (scale 0.3~0.5f)
// FlareShine×8 (scale.Y=1.2f)
// FancyStars×12 (scale 0.2~0.4f)
// StaticGlowLine×12 (距离50~90f)
// GlowSquareParticle×8 (scale 0.04~0.08f)
// TechyHolosquare×10 (scale 0.25~0.5f)
// ElectricSpark×6 (scale 0.35f)
// BoltParticle×8 (stretch (0.3,1.5))
// SquishyLightParticle×12 (scale 0.15f ★)
// NanoParticle×30 (scale 0.3~0.6f)

// CustomPulse×3（scale上限3.0f以内）
CustomPulse(DoGCyan*0.85f,    0.25f, 2.5f, "ShineExplosion1")
CustomPulse(DoGWhite*0.8f,    0.4f,  2.8f, "PlasmaExplosion")
CustomPulse(DoGFuchsia*0.75f, 0.55f, 3.2f, "ShineExplosion2")

// PlayerCenteredPulseRing×3（大型扩散，finalScale校准）
PlayerCenteredPulseRing(player, Zero, ThreeColorSpark, One, 0f, 0.04f, 0.8f, 28)
PlayerCenteredPulseRing(player, Zero, ThreeColorSpark, One, 0f, 0.06f, 1.1f, 32)
PlayerCenteredPulseRing(player, Zero, ThreeColorSpark, One, 0f, 0.08f, 1.4f, 36)

// GalaxyMetaball×10、StarsmokeMetaball×5
// ConstellationRingVFX×3（scale 0.8~1.2f）
// FlatGlow×4（scale校准）

// StrongBloom scale=2.5f（大招特例，允许更大）
StrongBloom(player.Center, DoGWhite, scale=2.5f)
CalamityUtils.AddScreenshakeAt(player.Center, 15f)
```

---

# 第七章：被动+Glowmask

```csharp
// 武器轨迹（0.3章已校准）

// 被动CritSpark（低频，每5帧1个，scale=0.32f）
if (Main.rand.NextBool(5))
    CritSpark(bladeMidpoint + rand.NextVector2Circular(7f,7f),
        rand.NextVector2Circular(1f,1f), DoGColor, DoGWhite,
        0.32f, 14, 1f, 0.8f, hueShift: 0.003f)  // 校准: scale=0.32f

// PostDraw双层glowmask（flickerOpacity=0.7+0.3*sin）
Draw(weaponGlowCyan,    center, DoGCyan    * flickerOpacity, ...)
Draw(weaponGlowFuchsia, center, DoGFuchsia * flickerOpacity * 0.65f, ...)
```

---

# 第八章：新增弹幕清单

| 弹幕 | 类型 | 说明 |
|-----|------|------|
| CosmicDischargeSwitchPortal | 视觉 | 右键切换3层旋转门（18帧） |
| CosmicDischargeDoGFire | IPixelatedPrimitive | 双层ImpFlameTrail武器轨迹 |

---

# 第九章：实施路线图

| 优先级 | 任务 | 依赖 |
|------|-----|------|
| P0 | 色彩常量替换 + GodSlayerInferno debuff | 无 |
| P1 | DoGFire双层轨迹 + PostDraw glowmask | P0 |
| P2 | SwordFinisher (2.2)：最强普攻特效 | P1 |
| P2 | SwordSwing (2.0/2.1)：CircularSmear + GenericSparkle | P1 |
| P3 | WhipMode (1.0/1.1/1.2)：参数校准版 | P1 |
| P4 | **ChainMode重写 (3.0/3.1/3.2)：塔纳托斯脊椎化** | P1 |
| P4 | 链节纹理：SpineOfThanatosBody1/2/Tail + DoG glowmask | P4前置 |
| P5 | QuickDraw (帧20全爆发) | P2 |
| P5 | 右键SwitchPortal弹幕 | P0 |
| P6 | 大招蓄力+爆发+持续场 | 全部前置 |
