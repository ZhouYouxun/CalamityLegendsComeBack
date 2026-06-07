# 《NewLegendBrinyBaron》非大招特效研究报告

> 研究对象：`CalamityLegendsComeBack.Weapons.BrinyBaron` 系列文件中的普通攻击、右键手里剑、短冲刺、被动快斩与水柱派生特效。  
> 研究目的：在不分析大招本体的前提下，提炼这把武器已经形成的视觉语言，让后续重做大招时能够继承现有武器的“水蓝、冰白、潮汐、切割、龙卷、短促爆闪”的特效体系。  
> 核心原则：**本文只要提到视觉特效，就一定写出它的名字；如果特效是 `CustomSpark`，则额外写出它使用的贴图路径。**

---

## 0. 摘要：这把武器的特效不是“普通水属性”，而是“海爵式高速水刃系统”

`NewLegendBrinyBaron` 这把武器目前已经形成了一套相当清晰的非大招视觉语言。它并不是单纯依靠 `DustID.Water` 撒水花，也不是把所有东西都做成蓝色光球，而是通过多个层级共同构成“海爵式高速水刃系统”：

第一层是 **方向性水流**，代表特效主要是 `DustID.Water`、`DustID.Frost`、`GlowOrbParticle`、`LineParticle`。它们大多沿弹幕的反方向、侧方向、刀尖后方释放，形成“水被刀锋和弹幕速度扯开”的感觉。

第二层是 **冰白冲击反馈**，代表特效主要是 `ImpactParticle`、`DirectionalPulseRing`、`WaterFoamParticle`、`GlowSparkParticle`、`CritSpark`。这些特效不负责持续铺满屏幕，而负责在命中、冲刺启动、粘附切割、短促爆发时给玩家一个清楚的“打中了”“启动了”“切开了”的瞬间信号。

第三层是 **additive 光刃绘制**，代表贴图包括 `CalamityMod/Particles/GlowBlade`、`CalamityMod/Particles/ThinEndedLine`、`CalamityMod/Particles/BloomCircle`、`CalamityMod/ExtraTextures/BloomCirclePinpoint`、`CalamityMod/ExtraTextures/SimpleStar`、`CalamityMod/Particles/CircularSmearSmokey`、`CalamityMod/Particles/SemiCircularSmearSwipe`。这一层让武器从“水”上升为“高速水刃”，尤其短冲刺 `BrinyBaron_SkillDashTornado_BladeDash` 和右键手里剑 `BrinyBaron_RightClick_Shuriken` 的绘制都明显依赖 additive 光效。

第四层是 **符文水魔法核心**，代表贴图包括 `CalamityLegendsComeBack/Texture/KsTexture/light_03`、`CalamityLegendsComeBack/Texture/KsTexture/circle_03`、`CalamityLegendsComeBack/Texture/KsTexture/circle_04`、`CalamityLegendsComeBack/Texture/KsTexture/magic_03`、`CalamityLegendsComeBack/Texture/KsTexture/magic_04`、`CalamityLegendsComeBack/Texture/KsTexture/star_04`、`CalamityLegendsComeBack/Texture/KsTexture/twirl_02`、`CalamityLegendsComeBack/Texture/KsTexture/window_04`。这些贴图让武器不是纯自然水流，而是有“海爵、潮汐、魔法阵、旋涡核心”的高级感。

第五层是 **成长阶段差异**。`BBSwing_Wave` 通过 `SpawnStage` 改变水波大小、粒子密度和水流丝线数量；`BrinyBaron_RightClick_Shuriken` 通过 `GrowthTier` 解锁 `BBShuriken_Hardmode_Effects`、`BBShuriken_Fishron_Effects`、`BBShuriken_BoomerDuke_Effects`；`BrinyBaron_SkillDashTornado_BladeDash` 通过 `ShortDashProfile` 改变速度倍率、接触伤害倍率、是否解锁敌人反弹。也就是说，这把武器不是只有数值成长，视觉也在成长。

如果后续重做大招，大招不应该突然变成完全不同的“蓝色核爆”或者“普通圆形水爆”。它最好继承这五个关键词：

**水蓝方向性、冰白冲击、光刃拖尾、符文旋涡、成长阶段强化。**

---

## 1. 特效命名原则：所有视觉效果必须可追踪、可复用、可解释

这批代码最值得保留的地方之一，是许多特效已经被明确拆成了具名模块。例如右键手里剑不是把所有效果都塞进 `AI()`，而是拆出了 `BBShuriken_Initial_Effects`、`BBShuriken_Hardmode_Effects`、`BBShuriken_Fishron_Effects`、`BBShuriken_BoomerDuke_Effects`。短冲刺也不是把所有粒子硬写在主类里，而是拆成了 `BrinyBaron_SkillDashTornado_FlightEffects` 和 `BrinyBaron_DashWaterPillar`。

这种结构对于重做大招非常关键。大招如果要做得好，也应该遵守相同原则：

- 每一种视觉效果都要有明确名字，例如 `XXX_ChargeRing`、`XXX_TideBladeTrail`、`XXX_ImpactFoamBurst`。
- 每一种视觉效果都要知道它使用的是 `GlowOrbParticle`、`LineParticle`、`DirectionalPulseRing`、`CustomSpark`，还是 `DustID.Water`。
- 每一个 `CustomSpark` 都必须写清楚贴图路径，因为 `CustomSpark` 的视觉差异几乎完全取决于贴图。
- 每一个释放函数都要知道它是“飞行持续释放”“命中瞬间释放”“第一帧爆发”“绘制层级”“消失反馈”，不能混成一坨。

本文下面所有章节都按这个原则展开。

---

## 2. 全局配色研究：主色是水蓝，亮部是冰白，暗部是深海蓝

这把武器的非大招特效颜色非常统一。最常出现的颜色大致可以归为三组。

### 2.1 主色：水蓝与青蓝

代表颜色：

```csharp
new Color(70, 180, 255)
new Color(75, 175, 255)
new Color(80, 195, 255)
new Color(82, 210, 255)
new Color(90, 205, 255)
new Color(95, 210, 255)
new Color(105, 205, 255)
new Color(115, 215, 255)
new Color(120, 220, 255)
Color.DeepSkyBlue
Color.Cyan
```

这些颜色主要用于 `DustID.Water`、`GlowOrbParticle`、`LineParticle`、`CustomSpark`、`DirectionalPulseRing`、`SparkParticle`、`CritSpark`、`GlowSparkParticle`。它们共同定义了武器的“海水主体”。

这不是很暗的深海蓝，也不是很绿的海藻色，而是偏亮、偏冷、偏高能的水蓝。它给人的感觉是“高速、清爽、锐利”，不是“粘稠、浑浊、厚重”。因此后续大招如果要延续现有普通攻击，主体色最好仍然使用 `Color.DeepSkyBlue`、`Color.Cyan`、`new Color(80, 195, 255)`、`new Color(120, 220, 255)` 这一组。

### 2.2 亮部：冰白与浅青白

代表颜色：

```csharp
new Color(185, 245, 255)
new Color(205, 248, 255)
new Color(210, 248, 255)
new Color(215, 248, 255)
new Color(220, 250, 255)
new Color(245, 255, 255)
Color.White
```

这些颜色主要用于 `DustID.Frost`、`GlowOrbParticle`、`ImpactParticle`、`WaterFoamParticle`、`CustomSpark`、`SimpleStar` 绘制、`GlowBlade` 核心绘制。亮部的作用不是“雪花”，而是让水刃具备锋利的切割核心。

尤其 `BBSwing_Slash` 的绘制非常典型：外层颜色是 `new Color(60, 170, 255, 0)` 到 `new Color(220, 250, 255, 0)`，内层 `coreColor` 使用 `new Color(220, 250, 255, 0)`。这说明“冰白”在这套武器中不是独立冰属性，而是水刃的高亮边缘。

### 2.3 暗部：深海蓝与深蓝阴影

代表颜色：

```csharp
new Color(12, 54, 110)
new Color(20, 86, 210)
new Color(25, 95, 205)
new Color(40, 90, 140)
new Color(15, 45, 85)
```

这些颜色主要用于 `BBSwing_Wave` 的 Primitive 拖尾、`BrinyBaron_TornadoWaterExplosion` 的 bloom 暗层、`BrinyBaron_TornadoBolt` 的符文暗层、`BrinyBaron_SkillDashTornado_BladeDash` 的 oldPos 残影。

暗部非常重要。没有暗部，所有青色都会糊成一片；有暗部之后，水蓝和冰白才有层次。后续大招如果规模很大，更不能只有白蓝一坨，而应该有深海蓝底层，让亮色从暗水中爆出来。

---

## 3. 全局特效资产清单

### 3.1 粒子类特效清单

| 特效名字 | 类型 | 主要出现位置 | 视觉功能 |
|---|---|---|---|
| `GlowOrbParticle` | Calamity 粒子 | `BBSwing_Wave`、`BrinyBaron_TornadoBolt`、`BrinyBaron_TornadoWaterExplosion`、`BBShuriken_Fishron_Effects`、`BrinyBaron_DashWaterPillar` | 水蓝光球、水珠、泡泡、螺旋轨迹、爆点余光 |
| `LineParticle` | Calamity 粒子 | `BBSwing_Wave`、`BrinyBaron_TornadoBolt`、`BBShuriken_Initial_Effects.SpawnTileDisappear` | 细长水流线、速度线、消散线 |
| `ImpactParticle` | Calamity 粒子 | `BBSwing_Wave.SpawnHitEffects` | 命中冲击爆点 |
| `DirectionalPulseRing` | Calamity 粒子 | `BrinyBaron_TornadoWaterExplosion`、`BrinyBaron_SkillDashTornado_FlightEffects` | 方向性冲击环、冲刺启动环、水爆扩散环 |
| `WaterFoamParticle` | Calamity 粒子 | `BBShuriken_Initial_Effects.SpawnWaterFoamHit` | 命中泡沫、水花碎沫 |
| `CritSpark` | Calamity 粒子 | `BrinyBaron_SkillSlashDash_SlashDash` | 启动锐光、挥砍爆发火花 |
| `CircularSmearVFX` | Calamity 粒子 | `BrinyBaron_SkillSlashDash_SlashDash` | 启动阶段圆形拖抹刀光 |
| `SparkParticle` | Calamity 粒子 | `BrinyBaron_SkillSlashDash_SlashDash` | 挥砍过程的海蓝散射光点 |
| `GlowSparkParticle` | Calamity 粒子 | `BrinyBaron_SkillSlashDash_SlashDash.OnHitNPC` | 命中双点亮光反馈 |
| `CustomSpark` | Calamity 粒子 | 短冲刺、右键手里剑、被动快斩 | 贴图驱动型自定义光刃、光斑、拖抹 |

### 3.2 Dust 特效清单

| 特效名字 | 类型 | 主要出现位置 | 视觉功能 |
|---|---|---|---|
| `DustID.Water` | Terraria Dust | 几乎所有水系攻击 | 主水花、主拖尾、水流填充 |
| `DustID.Frost` | Terraria Dust | 几乎所有水系攻击 | 冰白碎屑、亮部水雾、冷感 |
| `DustID.GemSapphire` | Terraria Dust | `BBShuriken_Initial_Effects.SpawnStickySliceBurst` | 蓝宝石碎片感、粘附切割爆裂 |

### 3.3 Gore 特效清单

| 特效名字 | 类型 | 主要出现位置 | 视觉功能 |
|---|---|---|---|
| `Gore 411` | Terraria Gore | `BrinyBaron_SkillDashTornado_FlightEffects.SpawnOuterWake` | 冲刺外侧泡泡 |
| `Gore 412` | Terraria Gore | `BrinyBaron_SkillDashTornado_FlightEffects.SpawnOuterWake` | 冲刺外侧泡泡 |

`Gore 411` 与 `Gore 412` 的意义是：短冲刺不是只用抽象蓝光，而是真的有泡泡被拖出来。这个细节让短冲刺更像“人在水流中高速破开水面”。

### 3.4 绘制贴图清单

| 贴图路径 | 使用位置 | 视觉意义 |
|---|---|---|
| `Terraria/Images/Projectile_0` | `BBSwing_Wave` | 主水波占位本体 |
| `CalamityMod/Projectiles/InvisibleProj` | `BBSwing_INV`、`BrinyBaron_TornadoWaterExplosion`、`BrinyBaron_WaterStream`、`BrinyBaron_DashWaterPillar` | 隐形判定体，不直接绘制 |
| `Terraria/Images/Extra_98` / `TextureAssets.Extra[ExtrasID.SharpTears]` | `BBSwing_Slash` | 薄水刃斩击 |
| `CalamityLegendsComeBack/Texture/KsTexture/light_03` | `BrinyBaron_TornadoBolt` | 水魔法核心弹主体 |
| `CalamityMod/ExtraTextures/BloomCirclePinpoint` | `BrinyBaron_TornadoBolt` | 中心 pinpoint bloom |
| `CalamityMod/ExtraTextures/SimpleStar` | `BrinyBaron_TornadoBolt`、`BrinyBaron_TornadoWaterExplosion` | 星芒高光 |
| `CalamityLegendsComeBack/Texture/KsTexture/magic_03` | `BrinyBaron_TornadoBolt` | 魔法符文层 |
| `CalamityLegendsComeBack/Texture/KsTexture/magic_04` | `BrinyBaron_TornadoBolt` | 旋转魔法符文层与拖尾螺旋片 |
| `CalamityLegendsComeBack/Texture/KsTexture/circle_03` | `BrinyBaron_TornadoBolt` | 中心圆环符文 |
| `CalamityLegendsComeBack/Texture/KsTexture/circle_04` | `BrinyBaron_TornadoWaterExplosion` | 水爆圆环 |
| `CalamityLegendsComeBack/Texture/KsTexture/twirl_02` | `BrinyBaron_TornadoBolt` | 旋涡核心 |
| `CalamityLegendsComeBack/Texture/KsTexture/star_04` | `BrinyBaron_TornadoBolt` | 符文星芒 |
| `CalamityMod/Particles/BloomCircle` | `BrinyBaron_TornadoWaterExplosion`、`BrinyBaron_DashWaterPillar`、部分 `CustomSpark` | 圆形 bloom 光斑 |
| `CalamityMod/Particles/ThinEndedLine` | `BrinyBaron_DashWaterPillar` | 竖向水柱线 |
| `CalamityMod/Particles/GlowBlade` | `BrinyBaron_SkillDashTornado_BladeDash.PreDraw` | 短冲刺光刃外层与核心 |
| `CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron` | `BrinyBaron_SkillDashTornado_BladeDash`、`BrinyBaron_SkillSlashDash_SlashDash` | 武器本体贴图 |
| `CalamityMod/Projectiles/TornadoProj` | `BrinyBaron_RightClick_Shuriken` | 右键手里剑主体 |
| `CalamityMod/Particles/CircularSmearSmokey` | `BBShuriken_BoomerDuke_Effects.DrawBladeDisc` | 高阶手里剑圆形旋转拖抹 |
| `CalamityMod/Particles/SemiCircularSmearSwipe` | `BBShuriken_BoomerDuke_Effects.DrawBladeDisc` | 高阶手里剑半圆挥砍拖抹 |

---

## 4. `CustomSpark` 专项清单：每一个 `CustomSpark` 必须带贴图路径

`CustomSpark` 是这套代码里最需要严格命名和标注贴图的特效。因为 `CustomSpark` 本身只是一个“可用任意贴图的粒子容器”，如果只说“用了 `CustomSpark`”，朋友完全不知道它看起来像刀光、光斑、圆形爆闪，还是竖向拖抹。因此所有 `CustomSpark` 必须写成“`CustomSpark` + 贴图路径 + 用途”。

| 所属模块 | 释放函数 | 特效名字 | 贴图路径 | 用途 |
|---|---|---|---|---|
| `BrinyBaron_SkillDashTornado_FlightEffects` | `SpawnDashStartEffects` | `CustomSpark` | `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade` | 短冲刺启动刀尖蓝白光刃线，释放 3 条 |
| `BrinyBaron_SkillDashTornado_FlightEffects` | `SpawnDashFlightEffects` | `CustomSpark` | `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade` | 短冲刺飞行中持续刀尖光刃线，释放 2 条 |
| `BrinyBaron_SkillDashTornado_FlightEffects` | `SpawnDashFlightEffects` | `CustomSpark` | `CalamityLegendsComeBack/Texture/KsTexture/window_04` | 短冲刺本体中心闪耀 flare，放在 `projectile.Center` |
| `BrinyBaron_SkillDashTornado_FlightEffects` | `SpawnReboundFlightEffects` | `CustomSpark` | `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade` | 反弹阶段较短较弱的回弹光刃线 |
| `BBShuriken_BoomerDuke_Effects` | `SpawnFlight` | `CustomSpark` | `CalamityMod/Particles/BloomCircle` | Boomer Duke 阶段右键手里剑两侧水蓝 bloom 光斑 |
| `BBShuriken_BoomerDuke_Effects` | `SpawnHitBurst` | `CustomSpark` | `CalamityMod/Particles/BloomCircle` | Boomer Duke 阶段右键手里剑命中时的水蓝 bloom 爆点 |
| `BrinyBaron_SkillSlashDash_SlashDash` | `AdditionalAI` 的 `inSwing` 段 | `CustomSpark` | `CalamityMod/Particles/VerticalSmearLarge` | 被动快斩进入挥砍时的大型竖向拖抹刀光 |

这些 `CustomSpark` 可以分成三种美术角色：

第一种是 **刀锋型 `CustomSpark`**，使用 `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade`。这种 `CustomSpark` 应该理解为短冲刺刀尖的光刃，不是普通光点。它通常有 `glowCenter: true`、`glowCenterScale`、`glowOpacity`、纵向拉伸 `new Vector2(0.56f, 2.15f)` 或类似参数，表现的是细长光刃。

第二种是 **符文 flare 型 `CustomSpark`**，使用 `CalamityLegendsComeBack/Texture/KsTexture/window_04`。这个 `CustomSpark` 放在短冲刺弹幕中心，颜色是 `new Color(160, 242, 255) * 1.96f`，亮度明显强于普通水蓝。它的任务是给短冲刺本体一个“核心亮点”，避免只有刀尖亮而本体不亮。

第三种是 **水蓝 bloom 型 `CustomSpark`**，使用 `CalamityMod/Particles/BloomCircle`。这个 `CustomSpark` 出现在 `BBShuriken_BoomerDuke_Effects`，用于高阶手里剑的飞行侧光与命中爆点。它不是刀形，而是圆形光斑，所以后续如果做大招时想表达“爆闪”而不是“切割”，可以参考这一类。

第四种是 **竖向拖抹型 `CustomSpark`**，使用 `CalamityMod/Particles/VerticalSmearLarge`。它只出现在 `BrinyBaron_SkillSlashDash_SlashDash` 的挥砍阶段，视觉上是 Lucrecia 式快速挥砍的海蓝改造版。它代表“快斩瞬间”，不是水柱，也不是泡沫。

后续大招重做时，`CustomSpark` 必须严格按这四种角色命名，不要混用。例如：

- 要表现刀锋，应使用 `CustomSpark` + `GlowBlade` 类贴图。
- 要表现爆闪，应使用 `CustomSpark` + `BloomCircle` 类贴图。
- 要表现挥砍拖抹，应使用 `CustomSpark` + `VerticalSmearLarge` 类贴图。
- 要表现符文中心，应使用 `CustomSpark` + `window_04` 或类似符文贴图。

---

## 5. 左键普通水波系统：`BBSwing_Wave`

`BBSwing_Wave` 是这把武器目前最核心的普通攻击特效之一。它的名字虽然叫 `Wave`，但它不是简单水波，而是一个带 Primitive Trail、阶段成长、飞行水雾、追踪泡泡派生、命中冲击的复合水刃弹幕。

### 5.1 `BBSwing_Wave` 的基础视觉定位

`BBSwing_Wave` 使用贴图 `Terraria/Images/Projectile_0`，但真实视觉并不依赖这张贴图本身。它的主体视觉来自两个部分：

1. `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:SideStreakTrail"]` 形成的水蓝宽拖尾。
2. `GlowOrbParticle`、`DustID.Water`、`DustID.Frost`、`LineParticle` 形成的飞行水花和水流丝线。

`BBSwing_Wave` 的基础参数非常能说明它的体量：

```csharp
BaseSize = 200
DefaultFinalWaveScale = 2.35f
WaveSizeFactor = 0.58f
Projectile.extraUpdates = 3
Projectile.timeLeft = 90 * Projectile.extraUpdates
TrailCacheLength = 8
TrailingMode = 2
```

也就是说 `BBSwing_Wave` 是一个大尺寸、高更新频率、短中距离滑行的水刃波。`Projectile.extraUpdates = 3` 使得 `BBSwing_Wave` 的运动和粒子释放更细腻，但也意味着飞行特效密度非常高。后续大招如果借鉴 `BBSwing_Wave`，要注意性能：不能直接把 `BBSwing_Wave.SpawnFlightEffects` 的密度按大招规模放大十倍。

### 5.2 `BBSwing_Wave` 的成长参数

`BBSwing_Wave` 的阶段逻辑由以下三个概念控制：

```csharp
SpawnStage => Utils.Clamp((int)Projectile.ai[1], 0, 3)
StageScale => Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultFinalWaveScale
StageIntensity => 1f + SpawnStage * 0.26f
```

`SpawnStage` 是视觉阶段，范围 0 到 3。`StageScale` 决定水波实际尺寸。`StageIntensity` 决定特效强度。这个设计非常适合大招借鉴：大招也应该拥有类似的“阶段强度”概念，而不是只有一个固定爆炸。

`BBSwing_Wave.ApplyStageStats` 中实际尺寸计算是：

```csharp
size = BaseSize * StageScale * WaveSizeFactor
Projectile.scale = (0.96f + SpawnStage * 0.08f) * WaveSizeFactor
```

因此 `BBSwing_Wave` 的成长不是只变大，还会轻微改变贴图缩放。重点是：`SpawnStage` 每升一级，视觉密度和光效也会变强。

### 5.3 `BBSwing_Wave.SpawnFlightEffects` 的四层飞行特效

`BBSwing_Wave.SpawnFlightEffects` 是这套普通攻击最值得研究的函数。它并不是随机撒粒子，而是围绕 `forward`、`right`、`wakeAnchor`、`edgeDistance`、`fillDistance` 组织视觉。

第一层是 **`GlowOrbParticle` 两侧水浪边缘光球**。代码会计算 `wakeAnchor`，再沿 `right` 的正负两侧生成 `GlowOrbParticle`。左侧颜色偏深：

```csharp
new Color(70, 180, 255)
```

右侧颜色偏亮：

```csharp
new Color(185, 245, 255)
```

这意味着 `GlowOrbParticle` 在 `BBSwing_Wave` 里不是普通泡泡，而是水浪边缘的高亮水珠。`edgeInterval = Math.Max(1, 3 - spawnStage)`，所以 `SpawnStage` 越高，`GlowOrbParticle` 释放越频繁。`edgeBursts = spawnStage >= 2 ? 2 : 1`，所以 `SpawnStage >= 2` 时两侧水浪边缘会出现双层光球。

第二层是 **`DustID.Water` 与 `DustID.Frost` 的内部填充水尘**。`BBSwing_Wave.SpawnFlightEffects` 每 2 帧生成 `2 + spawnStage` 个 `DustID.Water` / `DustID.Frost`。颜色从 `new Color(105, 205, 255)` 插值到 `new Color(215, 248, 255)`。这些 `DustID.Water` 与 `DustID.Frost` 不是用来表现命中，而是用来填充水波后方的体积，让 `BBSwing_Wave` 看起来不是一条空心光痕。

第三层是 **低速漂移 `GlowOrbParticle`**。当 `speedRatio < 0.78f` 时，`BBSwing_Wave.SpawnFlightEffects` 会额外生成 `GlowOrbParticle slowOrb`。颜色使用：

```csharp
Color.Lerp(new Color(80, 170, 255), new Color(220, 250, 255), 1f - speedRatio)
```

速度越慢，`GlowOrbParticle slowOrb` 越偏冰白。这个设计非常高级，因为它让 `BBSwing_Wave` 在高速时像水刃，在减速后像水花散开。大招如果要做“蓄力后释放的巨大潮汐斩”，也可以采用类似逻辑：高速阶段强调 `LineParticle` 和 `GlowBlade`，减速/结束阶段强调 `GlowOrbParticle` 和 `WaterFoamParticle`。

第四层是 **阶段 1 以上解锁的 `LineParticle` 水流丝线**。当 `SpawnStage >= 1` 且 `lifeTimer % 3 == 0` 时，`BBSwing_Wave.SpawnFlightEffects` 会释放 `LineParticle`。它使用 `goldenAngle = 2.3999631f`、`sin`、`cos` 来做曲线偏移，所以 `LineParticle` 并不是随机直线，而是带一点螺旋/流体组织感。颜色从 `new Color(95, 205, 255)` 插值到 `Color.White`。这组 `LineParticle` 是高级阶段水波的关键标识。

### 5.4 `BBSwing_Wave.TrySpawnTrackingBubbles` 的派生泡泡逻辑

`BBSwing_Wave.TrySpawnTrackingBubbles` 每 5 帧生成一个 `BrinyBaron_HomingLightOrb`，但只在 `Projectile.numUpdates == 0` 且 `Main.myPlayer == Projectile.owner` 时执行。这个限制很重要，因为 `BBSwing_Wave.extraUpdates = 3`，如果不限制 `Projectile.numUpdates == 0`，泡泡生成会过密。

`BrinyBaron_HomingLightOrb` 的生成位置不是弹幕中心，而是：

```csharp
Projectile.Center - forward * 随机距离 + right * 随机距离 + 圆形随机扰动
```

生成速度也不是直接朝目标，而是：

```csharp
baseDirection = (-forward).RotatedByRandom(0.35f ~ 0.95f)
```

因此 `BrinyBaron_HomingLightOrb` 在视觉上像被 `BBSwing_Wave` 甩出的水泡，然后自己再去追踪。这种“先被甩出，再逐渐追踪”的逻辑非常适合这把武器，因为它让追踪不显得机械。

这点对大招非常重要：如果大招需要派生追踪物，不建议让追踪物直接从玩家中心朝敌人直飞。更合理的做法是：从主水刃、主龙卷、主潮汐边缘甩出，再用延迟追踪。

### 5.5 `BBSwing_Wave.SpawnHitEffects` 的命中反馈

`BBSwing_Wave.SpawnHitEffects` 使用 `ImpactParticle` 作为主命中冲击。参数随 `SpawnStage` 增强：

```csharp
new ImpactParticle(
    pos,
    0.08f + spawnStage * 0.012f,
    18 + spawnStage * 2,
    0.9f + spawnStage * 0.08f,
    Color.Lerp(new Color(115, 220, 255), Color.White, 0.32f))
```

随后生成 `4 + spawnStage * 2` 个 `DustID.Water` / `DustID.Frost`。这说明 `ImpactParticle` 负责“主冲击”，`DustID.Water` 与 `DustID.Frost` 负责“碎水花”。这两者职责清楚。

后续大招如果需要命中反馈，可以按照这个结构放大：

- 中心冲击：`ImpactParticle`
- 外圈水花：`DustID.Water`
- 冰白碎屑：`DustID.Frost`
- 高级阶段追加：`DirectionalPulseRing` 或 `WaterFoamParticle`

### 5.6 `BBSwing_Wave.PreDraw` 的 Primitive 水蓝侧向拖尾

`BBSwing_Wave.PreDraw` 是这套普通攻击最核心的绘制逻辑。它使用：

```csharp
PrimitiveRenderer.RenderTrail
GameShaders.Misc["CalamityMod:SideStreakTrail"]
UseImage1("Images/Misc/Perlin")
```

颜色表是：

```csharp
new Color(220, 250, 255)
new Color(115, 215, 255)
new Color(48, 146, 235)
new Color(12, 54, 110)
```

这个颜色表非常关键：它从冰白到水蓝到深海蓝。`WidthFunc` 使用 `sin(t * Pi)^0.6`，让中段宽、两端收，形成水刃侧向拖尾。`ColorFunc` 使用 `(1f - t) * Projectile.Opacity * 1.2f`，让拖尾越远越透明。

这类 `PrimitiveRenderer.RenderTrail` 适合用于大招的主体轨迹，但要注意两点：第一，宽度不能过满屏，否则会遮挡；第二，深海蓝尾部必须保留，否则会变成一团白光。

---

## 6. 左键隐形命中框：`BBSwing_INV`

`BBSwing_INV` 使用贴图 `CalamityMod/Projectiles/InvisibleProj`，本体不绘制，`PreDraw` 直接 `return false`。它的作用是提供挥砍命中判定，并在命中时补充水花反馈。

`BBSwing_INV` 的几个关键参数是：

```csharp
SquareSize => Projectile.ai[0]
EncodedSwingScale => Projectile.ai[1]
SwingVisualScale => Abs(EncodedSwingScale)
AddsTide => EncodedSwingScale < 0f
SlashAngle => Projectile.ai[2]
```

`BBSwing_INV.OnHitNPC` 中视觉特效是 `DustID.Water` 和 `DustID.Frost`，数量固定为 8 个。释放方向根据 `SlashAngle.ToRotationVector2()` 反向喷射：

```csharp
velocity = -slashVelocity.RotatedByRandom(0.74f) * Random(5f, 18f)
```

颜色是：

```csharp
Color.DeepSkyBlue
Color.Cyan
```

音效是 `SoundID.Splash`。

`BBSwing_INV` 的意义是：它把“命中判定”和“视觉刀光”解耦。视觉刀光可以由 `BBSwing_Slash` 或其他绘制负责，命中只需要一个隐形方形区域，然后用 `DustID.Water` 与 `DustID.Frost` 做反馈。

另外，`BBSwing_INV` 里有一段潮汐增长逻辑被硬关掉：

```csharp
if (false && AddsTide)
```

这说明它原本可能被设计成某些挥砍命中时调用 `BBEXPlayer.AddTide()`，但目前禁用。这个信息对大招重做也有参考价值：如果大招需要和潮汐系统联动，应当明确是由命中框触发，还是由大招主弹幕触发，不要混在普通隐形命中框里。

---

## 7. 左键附加刀光：`BBSwing_Slash`

`BBSwing_Slash` 使用贴图 `Terraria/Images/Extra_98`，实际绘制中调用 `TextureAssets.Extra[ExtrasID.SharpTears]`。它是一个短生命周期、线段碰撞、水蓝外刃、冰白内芯的小型斩击弹幕。

### 7.1 `BBSwing_Slash` 的生命周期曲线

`BBSwing_Slash` 的生命周期是：

```csharp
Lifetime = 24
Projectile.MaxUpdates = 2
```

透明度曲线是：

```csharp
Projectile.Opacity = sin(lifeProgress * Pi)
```

所以 `BBSwing_Slash` 会从透明快速出现，中段最亮，随后消失。这是非常适合刀光的曲线，因为刀光不应该常亮，它应该“闪一下”。

### 7.2 `BBSwing_Slash.SpawnInitialBurst` 的初始水花

`BBSwing_Slash.SpawnInitialBurst` 在出生第一帧释放 6 组 `DustID.Water`，其中偶数序号额外释放 `DustID.Frost`。

`DustID.Water` 颜色：

```csharp
new Color(75, 175, 255)
```

`DustID.Frost` 颜色：

```csharp
new Color(210, 248, 255)
```

这说明 `BBSwing_Slash` 的出生不是无声贴图，而是先喷出一点水蓝和冰白碎屑，再由 `SharpTears` 贴图表现刀光。

### 7.3 `BBSwing_Slash.Colliding` 的线段判定

`BBSwing_Slash.Colliding` 使用：

```csharp
Collision.CheckAABBvLineCollision
```

线段长度：

```csharp
BaseSize * SlashScale * 0.95f
```

线宽：

```csharp
BaseSize * SlashScale * 0.18f
```

这说明 `BBSwing_Slash` 是真正的细长斩击，而不是一个圆形伤害球。后续如果大招要做多段刀光，不应该用大圆判定伪装斩击，最好也用线段碰撞或者长条判定。

### 7.4 `BBSwing_Slash.PreDraw` 的双层刀光

`BBSwing_Slash.PreDraw` 使用 `TextureAssets.Extra[ExtrasID.SharpTears]` 绘制两层。

第一层是长而扁的水蓝外刃：

```csharp
scale = new Vector2(1.9f, 0.34f) * Projectile.scale
color = Color.Lerp(new Color(60, 170, 255, 0), new Color(220, 250, 255, 0), 0.35f)
```

第二层是更细的冰白内芯：

```csharp
scale = new Vector2(0.85f, 0.12f) * Projectile.scale
color = new Color(220, 250, 255, 0) * 0.65f
rotation = Projectile.rotation + PiOver2
```

这个双层结构非常适合大招中的“主斩线”：外层水蓝，内层冰白，千万不要只画一层青色贴图。

---

## 8. 水魔法飞弹系统：`BrinyBaron_TornadoBolt`

`BrinyBaron_TornadoBolt` 是这批文件中最“魔法化”的普通弹幕。它使用贴图 `CalamityLegendsComeBack/Texture/KsTexture/light_03`，但真正的视觉由多层符文和水蓝光效组成。

### 8.1 `BrinyBaron_TornadoBolt` 的定位

`BrinyBaron_TornadoBolt` 可以理解为“水龙卷魔法核心弹”。它不是像 `BBSwing_Wave` 那样的大型水刃，也不是像 `BrinyBaron_RightClick_Shuriken` 那样的实体飞盘。它是一个小体积、高亮、带符文旋涡和半追踪逻辑的弹幕。

基础参数：

```csharp
VisualScale = 0.2f
HomingRange = 720f
HomingTurnRate = 0.15f
HomingSpeedLerp = 0.045f
HomingDelay = 8
WaterExplosionCount = 5
WaterExplosionScatterRadius = 54f
TrailCacheLength = 18
```

这说明 `BrinyBaron_TornadoBolt` 有长残影、有延迟追踪、有命中派生水爆。它是一个“打到后触发场面”的弹幕，而不是单点伤害。

### 8.2 `BrinyBaron_TornadoBolt.HomeTowardTarget` 的半追踪逻辑

`BrinyBaron_TornadoBolt.HomeTowardTarget` 在 `timer < HomingDelay` 时不追踪。8 帧后寻找 `HomingRange = 720f` 内最近目标。追踪强度由两个因素决定：

```csharp
loosen = Utils.GetLerpValue(HomingDelay, HomingDelay + 34f, timer, true)
closeBoost = Utils.GetLerpValue(260f, 72f, Projectile.Distance(target.Center), true)
```

`loosen` 代表飞行越久越允许转向，`closeBoost` 代表越接近目标越允许猛转。最后用：

```csharp
AngleTowards(desiredDirection, turnRate)
```

这是一种很适合水属性弹幕的追踪方式：不是导弹锁死，而是像水流被目标吸过去。后续大招如果有追踪水刃或追踪泡泡，可以参考 `BrinyBaron_TornadoBolt.HomeTowardTarget`。

### 8.3 `BrinyBaron_TornadoBolt.SpawnLightOrbFlightEffects` 的三层飞行特效

`BrinyBaron_TornadoBolt.SpawnLightOrbFlightEffects` 的第一层是 `GlowOrbParticle` 双侧螺旋光球。每 2 帧生成两枚 `GlowOrbParticle`，位置围绕 `right` 左右摆动，同时向 `-forward` 后方偏移。颜色是 `Color.Cyan` 或 `new Color(120, 220, 255)`。这让 `BrinyBaron_TornadoBolt` 看起来不是单点蓝球，而是周围有小水珠绕行。

第二层是 `LineParticle` 速度线。它随机生成在弹幕后方，颜色同样使用 `Color.Cyan` 或 `new Color(120, 220, 255)`。`LineParticle` 在这里负责表现高速水丝。

第三层是 `DustID.Water` 与 `DustID.Frost`。它们生成在弹幕后方，速度向 `-forward`，颜色使用同一主题色。`DustID.Water` 与 `DustID.Frost` 在这里不是主角，而是补足体积。

### 8.4 `BrinyBaron_TornadoBolt.PreDraw` 的符文层级

`BrinyBaron_TornadoBolt.PreDraw` 使用的贴图非常多：

```csharp
CalamityLegendsComeBack/Texture/KsTexture/light_03
CalamityMod/ExtraTextures/BloomCirclePinpoint
CalamityMod/ExtraTextures/SimpleStar
CalamityLegendsComeBack/Texture/KsTexture/magic_03
CalamityLegendsComeBack/Texture/KsTexture/magic_04
CalamityLegendsComeBack/Texture/KsTexture/circle_03
CalamityLegendsComeBack/Texture/KsTexture/twirl_02
CalamityLegendsComeBack/Texture/KsTexture/star_04
```

它的绘制顺序非常值得保留：

1. 用 `light_03` 画 oldPos 残影。
2. 每隔 3 个 oldPos 用 `magic_04` 画螺旋片。
3. 用 `circle_03` 画深蓝圆环底层。
4. 用 `twirl_02` 画旋涡核心。
5. 用 `magic_03` 画主符文层。
6. 用 `magic_04` 画反向符文层。
7. 用 `star_04` 画符文星芒。
8. 用 `BloomCirclePinpoint` 画中心 bloom。
9. 用 `light_03` 画主体。
10. 用 `SimpleStar` 画最终星芒。

这套绘制说明 `BrinyBaron_TornadoBolt` 的核心并不是“弹幕贴图”，而是“符文-旋涡-bloom-星芒”的叠层结构。后续大招如果要做蓄力核心，可以直接借鉴这种层级：圆环底层、旋涡层、魔法符文层、中心 bloom、星芒层。

### 8.5 `BrinyBaron_TornadoBolt.SpawnTyphoon` 的命中派生

`BrinyBaron_TornadoBolt.OnHitNPC` 调用 `SpawnTyphoon(target.Center)`。`SpawnTyphoon` 首先检查是否已经生成过，然后如果玩家场上没有 `BrinySpout`，生成 `BrinyTyphoonBubble`：

```csharp
ModContent.ProjectileType<BrinyTyphoonBubble>()
```

伤害是：

```csharp
Projectile.damage * 1.85f
```

然后调用 `SpawnWaterExplosions(center)`，生成 5 个 `BrinyBaron_TornadoWaterExplosion`。每个 `BrinyBaron_TornadoWaterExplosion` 的位置在 `WaterExplosionScatterRadius = 54f` 范围内随机，伤害是 `Projectile.damage * 0.46f`。

音效是：

```csharp
SoundID.Item84 with { Volume = 0.62f, Pitch = -0.18f }
```

这里的设计思路是：`BrinyBaron_TornadoBolt` 本身是触发器，命中后真正的视觉高潮来自 `BrinyTyphoonBubble` 和 `BrinyBaron_TornadoWaterExplosion`。这个思路也适合大招：大招主光束/主水刃命中后，可以派生多个具名爆点，而不是所有东西都塞进主弹幕。

### 8.6 `BrinyBaron_TornadoBolt.SpawnDisappearanceEffects` 的消失反馈

`BrinyBaron_TornadoBolt.SpawnDisappearanceEffects` 只生成 6 个 `DustID.Water` / `DustID.Frost`，颜色 `new Color(100, 220, 255)`。这是轻量消失，不是爆炸。这个设计很克制：如果 `BrinyBaron_TornadoBolt` 没命中，只是轻轻消散；如果命中，则走 `SpawnTyphoon` 的大反馈。

---

## 9. 小型水爆系统：`BrinyBaron_TornadoWaterExplosion`

`BrinyBaron_TornadoWaterExplosion` 使用贴图 `CalamityMod/Projectiles/InvisibleProj`，本体不可见，视觉完全来自 `DirectionalPulseRing`、`DustID.Water`、`DustID.Frost`、`GlowOrbParticle` 和 additive 绘制。

### 9.1 `BrinyBaron_TornadoWaterExplosion` 的伤害窗口

`BrinyBaron_TornadoWaterExplosion` 生命周期是：

```csharp
Lifetime = 18
```

只在前半段有伤害：

```csharp
CanDamage() => Projectile.timeLeft > Lifetime / 2
```

这说明 `BrinyBaron_TornadoWaterExplosion` 是典型的“瞬间爆点 + 后续视觉残留”。后半段的 bloom 和圆环只是视觉，不应该继续造成伤害。这个设计对大招也很重要：大招视觉可以持续，但伤害窗口必须清楚。

### 9.2 `BrinyBaron_TornadoWaterExplosion.SpawnBurstParticles`

第一帧释放 `DirectionalPulseRing`，颜色是：

```csharp
mainColor = new Color(82, 210, 255)
mainColor * 0.78f
```

`DirectionalPulseRing` 的缩放是 `Vector2.One`，旋转使用 `Projectile.ai[0]`，存活 14 帧。随后释放 12 个 `DustID.Water` / `DustID.Frost`，颜色是 `mainColor` 或 `accentColor`。最后释放 4 个 `GlowOrbParticle`。

这三个名字必须一起出现，才能构成完整水爆：

- `DirectionalPulseRing`：冲击环。
- `DustID.Water` / `DustID.Frost`：爆开的水雾和冰白碎屑。
- `GlowOrbParticle`：爆点余光和水珠。

### 9.3 `BrinyBaron_TornadoWaterExplosion.PreDraw`

`BrinyBaron_TornadoWaterExplosion.PreDraw` 使用：

```csharp
CalamityMod/Particles/BloomCircle
CalamityLegendsComeBack/Texture/KsTexture/circle_04
CalamityMod/ExtraTextures/SimpleStar
```

绘制层次是：

1. `BloomCircle` 深蓝大底光。
2. `BloomCircle` 水蓝小亮光。
3. `circle_04` 旋转水环。
4. `SimpleStar` 白色星芒。
5. `SimpleStar` 青色交叉星芒。

颜色是：

```csharp
water = new Color(72, 205, 255, 0)
deep = new Color(20, 86, 210, 0)
```

`pulse = 1f + sin(...) * 0.08f`，所以它有轻微脉动。这个水爆非常适合作为大招中的“小型命中节点”：不是主爆炸，但可以成群出现，填充大招的攻击范围。

---

## 10. 轻量追踪水流：`BrinyBaron_WaterStream`

`BrinyBaron_WaterStream` 使用贴图 `CalamityMod/Projectiles/InvisibleProj`，`PreDraw` 直接 `return false`。它的视觉只靠 `DustID.Water` 和 `DustID.Frost`。这说明 `BrinyBaron_WaterStream` 是一个“轻量追踪伤害体”，不是主视觉弹幕。

`BrinyBaron_WaterStream.HomeTowardTarget` 的追踪范围是：

```csharp
HomingRange = 760f
```

目标速度是：

```csharp
desiredVelocity = directionToTarget * 14.5f
Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f)
```

视觉释放是：

```csharp
DustID.Frost / DustID.Water
new Color(80, 195, 255)
```

生成位置在弹幕后方 10 像素附近，速度是 `-Projectile.velocity * 0.06f ~ 0.18f`。

`BrinyBaron_WaterStream` 的意义在于：它证明这把武器允许存在“几乎不可见但带水雾轨迹的追踪体”。如果后续大招要生成大量小追踪水流，应该参考 `BrinyBaron_WaterStream`，而不是给每个小水流都画复杂 bloom。否则性能和屏幕可读性都会崩。

---

## 11. 右键手里剑系统：`BrinyBaron_RightClick_Shuriken`

`BrinyBaron_RightClick_Shuriken` 使用贴图 `CalamityMod/Projectiles/TornadoProj`。它是一个成长型右键飞行物，具有普通飞行、潮汐强化追踪、命中粘附、周期切割、高阶旋转副本、高阶光盘绘制等多个视觉阶段。

### 11.1 `BrinyBaron_RightClick_Shuriken` 的成长阶段

`BrinyBaron_RightClick_Shuriken` 的视觉成长由 `GrowthTier` 控制：

```csharp
0 = 初始阶段
1 = Hardmode 阶段
2 = Fishron 阶段
3 = Boomer Duke 阶段
```

判定逻辑是：

```csharp
if downedBoomerDuke => 3
else if NPC.downedFishron => 2
else if Main.hardMode => 1
else => 0
```

成长参数包括：

```csharp
ShurikenStickySliceCounts = { 3, 4, 5, 6 }
ShurikenTideHomingRanges = { 900f, 1040f, 1180f, 1320f }
ShurikenRotationSpeeds = { 0.55f, 0.59f, 0.63f, 0.67f }
ShurikenStickySlashUnlocks = { false, false, true, true }
```

这说明 `BrinyBaron_RightClick_Shuriken` 的成长不是只增加伤害，而是：

- 切割次数从 3 到 6。
- 潮汐追踪范围从 900 到 1320。
- 旋转速度从 0.55 到 0.67。
- Fishron 阶段后理论上解锁 `BBSwing_Slash` 粘附切割派生，但 `StickySlashProjectilesEnabled = false`，目前关闭。

### 11.2 `BrinyBaron_RightClick_Shuriken.HandleFlightMovement`

`BrinyBaron_RightClick_Shuriken` 飞行时有两套逻辑。

非潮汐强化时，使用：

```csharp
Projectile.velocity *= NonEmpoweredShurikenAcceleration
ClampFlightSpeed(NonEmpoweredShurikenMaxSpeed)
```

其中：

```csharp
NonEmpoweredShurikenAcceleration = 1.0195f
NonEmpoweredShurikenMaxSpeed = 48f
```

潮汐强化时，`TideEmpowered` 为真，会寻找最近目标。前 144 帧只是缓慢加速：

```csharp
TideHomingDelayFrames = 144
TideHomingIdleAcceleration = 1.006f
```

144 帧后才进入真正追踪。追踪使用目标方向和原速度混合：

```csharp
Projectile.velocity = (Projectile.velocity * inertia + desiredDir * targetSpeed) / (inertia + 1f)
```

这让 `BrinyBaron_RightClick_Shuriken` 的追踪不像硬拐弯导弹，而像飞盘被潮汐逐渐牵引。这个追踪气质和 `BrinyBaron_TornadoBolt.HomeTowardTarget` 一致，都是“有追踪，但不是瞬间锁死”。

### 11.3 `BBShuriken_Initial_Effects.SpawnFlight`

`BBShuriken_Initial_Effects.SpawnFlight` 是右键手里剑的基础飞行特效。它使用 `DustID.Water`，颜色是：

```csharp
new Color(70, 180, 255)
```

释放概率是 `Main.rand.NextBool(3)`，也就是平均三帧一次。位置在手里剑后方，带有横向正弦摆动：

```csharp
spawnPos = projectile.Center - forward * backDistance + right * sin(phase) * projectile.width * 0.1f
```

这组 `DustID.Water` 是基础水尾，不会太吵。它非常适合低阶阶段。

### 11.4 `BBShuriken_Initial_Effects.SpawnWaterFoamHit`

`BBShuriken_Initial_Effects.SpawnWaterFoamHit` 是右键手里剑命中时最有水感的特效之一。它使用 `WaterFoamParticle`，颜色是：

```csharp
Color.Lerp(new Color(150, 230, 255), Color.White, 0.46f)
```

基础泡沫数量：

```csharp
baseFoamCount = 6 + highestUnlockedStage * 2
foamCount = ceil(baseFoamCount * 0.34f)
```

这说明它原本泡沫密度可能较高，但最终乘了 0.34，保留克制数量。`WaterFoamParticle` 的速度沿 `hitForward` 扩散，并加入 `hitRight` 与圆形随机扰动。

`WaterFoamParticle` 对这把武器非常重要，因为它让命中看起来是真正的水花，而不只是蓝色火花。后续大招如果有巨浪命中，可以加入 `WaterFoamParticle`，但要控制数量，避免泡沫糊屏。

### 11.5 `BBShuriken_Initial_Effects.SpawnHitBurst`

`BBShuriken_Initial_Effects.SpawnHitBurst` 使用 `DustID.Water` 和 `DustID.Frost`。数量是：

```csharp
hitBurstCount = 8 + highestUnlockedStage * 2
```

`DustID.Water` 每次都生成，颜色 `new Color(70, 180, 255)`。`DustID.Frost` 每隔一个生成，颜色 `new Color(210, 248, 255)`。

这和 `BBSwing_Slash.SpawnInitialBurst` 的逻辑一致：`DustID.Water` 是主体，`DustID.Frost` 是亮部。

### 11.6 `BBShuriken_Initial_Effects.SpawnStickyAmbient`

`BBShuriken_Initial_Effects.SpawnStickyAmbient` 是手里剑插在敌人身上时的持续特效。它使用 `DustID.Frost`，并有概率追加 `DustID.Water`。`ambientCount = 1 + Min(highestUnlockedStage, 1)`，也就是说高阶最多也只是 2 个，不会过密。

这组 `DustID.Frost` 和 `DustID.Water` 是“粘附中还在切割/冻结”的提示，不是主要爆点。

### 11.7 `BBShuriken_Initial_Effects.SpawnStickySliceBurst`

`BBShuriken_Initial_Effects.SpawnStickySliceBurst` 是粘附切割的周期爆发。它使用 `DustID.GemSapphire` 和 `DustID.Frost`，数量是：

```csharp
burstCount = 4 + highestUnlockedStage
```

这里的 `DustID.GemSapphire` 非常特别。整套文件大部分都是 `DustID.Water` 和 `DustID.Frost`，只有这里用了 `DustID.GemSapphire`。它让粘附切割有一种“蓝宝石碎片”或者“高压水刃切出晶状碎片”的感觉。

因此 `DustID.GemSapphire` 不应该滥用。它最好继续只用在“硬质切割 / 粘附切割 / 高阶碎裂”这种场景。如果大招里有“最终斩断”瞬间，可以少量使用 `DustID.GemSapphire`，但不要把它当普通水花。

### 11.8 `BBShuriken_Initial_Effects.SpawnTileDisappear`

`BBShuriken_Initial_Effects.SpawnTileDisappear` 是右键手里剑撞墙消失的特效。它使用 10 个 `DustID.Water` / `DustID.Frost`，再使用 3 个 `LineParticle`。颜色从 `new Color(90, 205, 255)` 插值到 `Color.White`。

这里的 `LineParticle` 很关键，因为撞墙消失如果只有 `DustID.Water` 会显得软，而 `LineParticle` 可以表现飞盘的锐利残余方向。

### 11.9 `BBShuriken_Hardmode_Effects.DrawRotatingCopies`

`BBShuriken_Hardmode_Effects.DrawRotatingCopies` 是 Hardmode 阶段解锁的绘制效果。它使用 `CalamityMod/Projectiles/TornadoProj` 贴图本身，围绕主手里剑画 4 个旋转副本。

关键参数：

```csharp
orbitRadius = projectile.width * 0.34f
spin = Main.GlobalTimeWrappedHourly * 8.5f + projectile.identity * 0.37f
```

颜色是：

```csharp
Color.Lerp(new Color(15, 45, 85, 0), new Color(70, 170, 255, 0), 0.68f) * 0.45f
```

这个效果让右键手里剑从“一枚飞盘”变成“带水流残影的旋转飞盘”。注意它不使用粒子，而是绘制多个半透明副本。大招如果需要表现“高速旋转武器本体”，也可以用这种副本绘制，不一定全靠粒子。

### 11.10 `BBShuriken_Fishron_Effects.SpawnFlight`

`BBShuriken_Fishron_Effects.SpawnFlight` 是 Fishron 阶段解锁的飞行特效。它使用 4 臂螺旋结构，每个 arm 有概率生成 `GlowOrbParticle`，并且有概率生成 `DustID.Frost`。

`GlowOrbParticle` 的颜色是：

```csharp
new Color(75, 190, 255)
Color.Cyan
```

`DustID.Frost` 的颜色是：

```csharp
new Color(220, 250, 255)
```

这组效果的核心是“围绕飞盘轨道绕行的光球”。它不是尾迹，而是旋转轨道。视觉上会明显提高阶段感。

### 11.11 `BBShuriken_Fishron_Effects.SpawnHitBurst`

`BBShuriken_Fishron_Effects.SpawnHitBurst` 命中时生成 4 个 `GlowOrbParticle`。位置在敌人中心附近，速度沿 `hitForward` 和 `hitRight` 发散。颜色是 `Color.DeepSkyBlue` 或 `Color.Cyan`。

这组 `GlowOrbParticle` 是 Fishron 阶段命中反馈的升级层，让命中不只是 `DustID.Water` 与 `DustID.Frost`，而有亮色水珠飞散。

### 11.12 `BBShuriken_BoomerDuke_Effects.SpawnFlight`

`BBShuriken_BoomerDuke_Effects.SpawnFlight` 是 Boomer Duke 阶段的飞行升级。它使用 `CustomSpark`，贴图路径必须明确写出：

```csharp
CustomSpark
Texture: CalamityMod/Particles/BloomCircle
```

它会在手里剑中心两侧各生成一个 `CustomSpark`，颜色分别是：

```csharp
new Color(90, 210, 255)
new Color(140, 235, 255)
```

这组 `CustomSpark` 表现的是飞盘两侧的水蓝 bloom 光斑。它让 Boomer Duke 阶段的手里剑显得更亮、更高级，但仍然不变成复杂符文。

### 11.13 `BBShuriken_BoomerDuke_Effects.SpawnHitBurst`

`BBShuriken_BoomerDuke_Effects.SpawnHitBurst` 命中时生成 3 个 `CustomSpark`，贴图路径仍然是：

```csharp
CustomSpark
Texture: CalamityMod/Particles/BloomCircle
```

颜色是：

```csharp
new Color(95, 210, 255)
Color.Cyan
```

速度沿 `hitForward` 随机扩散。这组 `CustomSpark` 是高阶命中爆闪层。

### 11.14 `BBShuriken_BoomerDuke_Effects.DrawBladeDisc`

`BBShuriken_BoomerDuke_Effects.DrawBladeDisc` 是 Boomer Duke 阶段最明显的绘制升级。它使用：

```csharp
CalamityMod/Particles/CircularSmearSmokey
CalamityMod/Particles/SemiCircularSmearSwipe
CalamityMod/Projectiles/TornadoProj
```

其中 `SemiCircularSmearSwipe` 负责半圆挥砍拖抹，`CircularSmearSmokey` 负责圆形旋转拖抹。颜色使用 `Color.DeepSkyBlue`、`Color.Cyan`、`Color.LightSeaGreen`、`Color.CornflowerBlue`。随后围绕本体画 6 个低透明度 `TornadoProj` 副本作为光环。

这让高阶手里剑拥有“旋转光盘”的感觉。大招如果需要召唤环绕水刃，可以直接参考 `BBShuriken_BoomerDuke_Effects.DrawBladeDisc` 的结构。

---

## 12. 短冲刺主弹幕：`BrinyBaron_SkillDashTornado_BladeDash`

`BrinyBaron_SkillDashTornado_BladeDash` 是这批代码里最完整的“技能级”非大招特效。它包含准备阶段、冲刺阶段、反弹阶段、水柱派生、飞行中发射右键手里剑、屏幕震动、武器本体绘制、`GlowBlade` 光刃绘制等内容。

### 12.1 `BrinyBaron_SkillDashTornado_BladeDash` 的状态机

`BrinyBaron_SkillDashTornado_BladeDash` 有三个状态：

```csharp
dashState = 0  // 准备阶段
DashState = 1  // 冲刺阶段
dashState = 2  // 反弹阶段
```

时间参数：

```csharp
PrepareTime = 8
DashTimeMax = 45
ReboundTimeMax = 12
```

速度参数：

```csharp
DashSpeed = 18f * 0.67f
ReboundSpeed = 9f
DashTurnRate = 0.01f
```

成长参数：

```csharp
ShortDashSpeedMultipliers = { 1.05f, 1.12f, 1.2f, 1.32f, 1.45f }
ShortDashContactDamageMultipliers = { 1.05f, 1.1f, 1.2f, 1.35f, 1.55f }
ShortDashEnemyReboundUnlocks = { false, true, true, true, true }
```

这说明短冲刺是“成长型技能”，阶段越高越快、越痛，并且从 Hardmode 起解锁敌人反弹。

### 12.2 `BrinyBaron_SkillDashTornado_BladeDash.InitializeDash`

初始化时播放 `SoundID.Item73`，然后调用：

```csharp
SpawnStartBurst()
SpawnChargeReadyBurst()
```

不过 `SpawnStartBurst()` 当前为空。`SpawnChargeReadyBurst()` 当前释放的是屏幕震动 `ApplyScreenShake(7f)`、`SoundID.Item122` 和 `SoundID.Splash`。也就是说准备阶段目前靠声音和屏幕震动提示，视觉准备特效没有在主类里写，而是之后进入冲刺时调用 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects`。

### 12.3 `BrinyBaron_SkillDashTornado_BladeDash.StartDash`

进入冲刺时播放 `SoundID.Item39`，然后调用：

```csharp
BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects
```

这说明短冲刺的启动视觉全部由 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects` 管理。

### 12.4 `BrinyBaron_SkillDashTornado_BladeDash.DoDashPhase`

冲刺阶段每帧调用：

```csharp
BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects
TryFireDashProjectile
```

`TryFireDashProjectile` 每 7 帧生成一个 `BrinyBaron_RightClick_Shuriken`，伤害为短冲刺伤害的 32%，击退为 40%，`ai[0] = 0.25f`。也就是说短冲刺不是单纯位移，而是在冲刺中不断甩出小型手里剑。这一点很重要：短冲刺的视觉里混入了右键手里剑的视觉语言，让技能之间有统一性。

### 12.5 `BrinyBaron_SkillDashTornado_BladeDash.OnHitNPC`

短冲刺命中敌人时：

- 给目标 `BuffID.Frostburn`。
- 调用 `SpawnWaterPillarBurst`。
- 给玩家 `BBEXPlayer.AddTide()`。
- 如果有 `ImpactRestarterEquipped`，清除右键冲刺冷却。
- 如果解锁敌人反弹，则调用 `StartRebound`。

视觉上最重要的是 `SpawnWaterPillarBurst`。它会生成 5 个 `BrinyBaron_DashWaterPillar`，沿命中方向的侧向分布。中心 lane 的 `ai[1] = 1f`，两侧 lane 的 `ai[1] = 0f`。

### 12.6 `BrinyBaron_SkillDashTornado_BladeDash.PreDraw`

短冲刺主绘制使用两个贴图：

```csharp
CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron
CalamityMod/Particles/GlowBlade
```

绘制分四层：

第一层是 `GlowBlade` 外层 halo，颜色：

```csharp
new Color(45, 205, 255, 0) * 1.1f
```

第二层是 `GlowBlade` shell，颜色：

```csharp
new Color(135, 238, 255, 0) * 0.92f
```

第三层是 `NewLegendBrinyBaron` oldPos 残影，颜色从深蓝 `new Color(40, 90, 140, 0)` 插值到亮蓝 `new Color(120, 220, 255, 0)`。

第四层是 `NewLegendBrinyBaron` 本体，走 AlphaBlend 正常绘制。

第五层是 `GlowBlade` 核心，颜色：

```csharp
new Color(245, 255, 255, 0) * 0.88f
```

这套结构特别适合“高速水刃冲刺”：先画外光，再画残影，再画本体，最后画白色核心。后续大招如果要做“巨型冲刺斩”或“潮汐贯穿”，可以直接放大这个层级。

---

## 13. 短冲刺飞行特效工具：`BrinyBaron_SkillDashTornado_FlightEffects`

`BrinyBaron_SkillDashTornado_FlightEffects` 是短冲刺特效的核心工具类。它定义了 `GlowBladeTexture`：

```csharp
CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade
```

这个贴图只用于 `CustomSpark`，不是主绘制里的 `CalamityMod/Particles/GlowBlade`。两者不要混淆：

- `CustomSpark` 使用的 `GlowBladeTexture` 是 `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade`。
- `PreDraw` 使用的 `glowBlade` 是 `CalamityMod/Particles/GlowBlade`。

### 13.1 `BrinyBaron_SkillDashTornado_FlightEffects.GetFrontAnchor`

`GetFrontAnchor` 计算刀尖锚点：

```csharp
Projectile.Center + forward * FrontAnchorDistance
FrontAnchorDistance = 16f * 3f
```

这说明短冲刺特效大部分不是从弹幕中心释放，而是从刀尖前方释放。大招如果要做刀刃型视觉，也应该有类似 `GetFrontAnchor` 的概念。

### 13.2 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects`

`SpawnDashStartEffects` 释放三类特效。

第一类是 `DirectionalPulseRing`。它连续释放 3 个 `DirectionalPulseRing`，位置在刀尖后方 `6f + i * 4f`，颜色是 `Color.Lerp(new Color(55, 175, 255), Color.White, 0.18f)`，缩放 `new Vector2(0.85f, 2.55f)`。这个 `DirectionalPulseRing` 不是圆形爆炸，而是拉长的方向性冲刺环。

第二类是 `CustomSpark`，贴图路径是：

```csharp
CustomSpark
Texture: CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade
```

它释放 3 条刀尖光刃线，颜色 `new Color(145, 235, 255) * 0.95f`，缩放 `new Vector2(0.58f, 2f)`，`glowCenter: true`。这组 `CustomSpark` 是短冲刺启动瞬间的“拔刀蓝光”。

第三类是 `SpawnOuterWake`。`SpawnOuterWake` 内部会释放 `DustID.Water`、`DustID.Frost`、`Gore 411`、`Gore 412`。它负责冲刺外侧水花和泡泡。

### 13.3 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects`

`SpawnDashFlightEffects` 是冲刺持续阶段。它释放四类具名特效。

第一类是 `DirectionalPulseRing`。每 2 帧释放一次，位置在刀尖后方，颜色 `Color.Lerp(new Color(80, 195, 255), Color.White, 0.16f)`，缩放 `new Vector2(0.88f, 2.5f)`，寿命 10 帧。它让冲刺中不断出现水蓝方向性脉冲。

第二类是 `CustomSpark`，贴图路径是：

```csharp
CustomSpark
Texture: CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade
```

每帧释放 2 条刀尖光刃线，颜色 `new Color(160, 242, 255)`，纵向拉伸约 `new Vector2(0.56f, 2.15f)` 到 `new Vector2(0.56f, 2.6f)`。这组 `CustomSpark` 是短冲刺飞行中最主要的刀刃残光。

第三类是 `CustomSpark`，贴图路径是：

```csharp
CustomSpark
Texture: CalamityLegendsComeBack/Texture/KsTexture/window_04
```

这个 `CustomSpark` 放在 `projectile.Center`，寿命 10 帧，大小 `0.26f`，颜色 `new Color(160, 242, 255) * 1.96f`。它是中心 flare，让短冲刺本体发光。

第四类是 `SpawnOuterWake`。`SpawnOuterWake` 释放 `DustID.Water`、`DustID.Frost`、`Gore 411`、`Gore 412`。冲刺阶段的 `SpawnOuterWake` 参数比启动阶段更轻，重点是持续维持两侧水花。

### 13.4 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnReboundFlightEffects`

`SpawnReboundFlightEffects` 是反弹阶段。它释放三类具名特效。

第一类是 `DirectionalPulseRing`。每 3 帧释放一次，颜色 `new Color(90, 190, 255)`，缩放 `new Vector2(0.7f, 1.8f)`，比冲刺阶段更小更短。

第二类是 `CustomSpark`，贴图路径是：

```csharp
CustomSpark
Texture: CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade
```

颜色 `new Color(120, 220, 255) * 0.7f`，缩放 `new Vector2(0.45f, 1.4f)`。它比冲刺阶段的 `CustomSpark` 弱，表现回弹而不是主冲锋。

第三类是 `SpawnOuterWake`。反弹阶段的 `SpawnOuterWake` 参数更小，泡泡和水花更少。

### 13.5 `BrinyBaron_SkillDashTornado_FlightEffects.SpawnOuterWake`

`SpawnOuterWake` 是短冲刺最有“水中穿行感”的函数。它左右两侧都释放：

- `DustID.Water`
- `DustID.Frost`
- `Gore 411`
- `Gore 412`

`DustID.Water` 颜色：

```csharp
new Color(110, 210, 255)
```

`DustID.Frost` 颜色：

```csharp
new Color(205, 248, 255)
```

`Gore 411` 与 `Gore 412` 是泡泡。它们的 `timeLeft` 很短，只有 7 到 14 帧左右，表现的是高速冲刺带出来的短命泡泡。

这个函数是大招重做时非常值得借鉴的部分。如果大招有“向前推进的水刃/潮汐冲锋”，应该使用类似 `SpawnOuterWake` 的左右翼水花结构，而不是只在中心撒 `DustID.Water`。

---

## 14. 短冲刺水柱派生：`BrinyBaron_DashWaterPillar`

`BrinyBaron_DashWaterPillar` 使用贴图 `CalamityMod/Projectiles/InvisibleProj`，实际视觉来自 `DustID.Water`、`DustID.Frost`、`GlowOrbParticle` 和 additive 绘制的 `ThinEndedLine` / `BloomCircle`。

### 14.1 `BrinyBaron_DashWaterPillar` 的生成逻辑

`BrinyBaron_DashWaterPillar` 由 `BrinyBaron_SkillDashTornado_BladeDash.SpawnWaterPillarBurst` 生成。每次命中生成 5 根，沿命中方向的侧向分布。中心水柱使用：

```csharp
Projectile.ai[1] = 1f
```

侧边水柱使用：

```csharp
Projectile.ai[1] = 0f
```

中心水柱 `CenterLane` 为真，会释放更多粒子，绘制透明度也更高。

### 14.2 `BrinyBaron_DashWaterPillar.SpawnWaterColumnParticles`

`SpawnWaterColumnParticles` 每帧释放 `DustID.Water` / `DustID.Frost`，数量是：

```csharp
CenterLane ? 5 : 3
```

颜色从 `new Color(65, 175, 255)` 插值到 `Color.White`，并乘以淡入淡出 `fade`。部分粒子会额外生成 `GlowOrbParticle`。

这里的 `GlowOrbParticle` 是水柱中的亮水珠，不是飞行尾迹。它的速度是 `velocity * 0.35f`，寿命 8 到 13 帧，大小 0.18 到 0.34。

### 14.3 `BrinyBaron_DashWaterPillar.PreDraw`

`BrinyBaron_DashWaterPillar.PreDraw` 使用：

```csharp
CalamityMod/Particles/ThinEndedLine
CalamityMod/Particles/BloomCircle
```

`ThinEndedLine` 画竖向水柱主体，颜色是：

```csharp
new Color(70, 190, 255, 0)
```

中心水柱透明度更高：

```csharp
CenterLane ? 0.58f : 0.42f
```

`BloomCircle` 画底部水花 bloom，位置在水柱下方 `Projectile.height * 0.38f`。这个结构说明 `BrinyBaron_DashWaterPillar` 是“竖线 + 底部 bloom”的简洁水柱，不是完整喷泉。

大招如果需要地面/命中点喷起水柱，可以直接复用这种结构，但应给大招水柱一个新的名字，例如 `BrinyBaron_UltimateWaterPillar`，不要直接把 `BrinyBaron_DashWaterPillar` 复制成一堆无名效果。

---

## 15. 被动快斩系统：`BrinyBaron_SkillSlashDash_SlashDash`

`BrinyBaron_SkillSlashDash_SlashDash` 是被动快斩弹幕，继承 `BaseSwordHoldoutProjectile`。它的视觉骨架明显参考 Lucrecia 式挥砍，但配色全部改成海蓝和冰白。

### 15.1 `BrinyBaron_SkillSlashDash_SlashDash` 的阶段

它分为：

- `inStartup`：前摇启动。
- `inSwing`：真正挥砍。
- `inCooldown`：后摇。

参数：

```csharp
StandardStartupTime = 8
StandardSwingTime = 10
StandardCooldownTime = 12
swingWidth = 310
lineCollisionLength = 235f
```

它会自动生成第二刀：第一刀 `SwingIndex = 0` 在 `CooldownCompletion >= 0.82f` 时生成第二刀 `SwingIndex = 1`。两刀颜色略有区别：第一刀偏 `Color.DeepSkyBlue`，第二刀偏 `Color.Cyan`。

### 15.2 前摇阶段的 `CritSpark` 与 `CircularSmearVFX`

`inStartup` 中，当 `StartupCompletion > 0.12f && StartupCompletion < 0.44f` 时，会释放两种特效。

第一种是 `CritSpark`。每 8 帧释放一次，位置在玩家中心沿刀光方向 110 像素处，速度是 `new Vector2(7f, 0f).RotatedBy(Projectile.rotation)`，颜色从 `Color.DeepSkyBlue` 到 `Color.Cyan` 随机插值，副色是 `Color.White * 0.33f`。这个 `CritSpark` 负责启动阶段的锐利闪光。

第二种是 `CircularSmearVFX`。它每次满足阶段条件都会释放，位置在 `player.MountedCenter`，颜色 `Color.DeepSkyBlue * 0.35f`，旋转使用 `Projectile.rotation`，大小是 `Projectile.scale * 1.25f`。这个 `CircularSmearVFX` 负责启动阶段的圆形拖抹刀影。

这两种名字必须区分：`CritSpark` 是点状锐光，`CircularSmearVFX` 是面状拖抹。

### 15.3 挥砍开始的 `CustomSpark`

进入 `inSwing` 后，第一次会释放 `CustomSpark`，贴图路径是：

```csharp
CustomSpark
Texture: CalamityMod/Particles/VerticalSmearLarge
```

它的位置是：

```csharp
player.MountedCenter - shootDir * 8f
```

速度是 `shootDir` 旋转一点后乘 1.22，颜色第一刀是 `Color.DeepSkyBlue * 0.9f`，第二刀是 `Color.Cyan * 0.85f`。缩放是 `new Vector2(1.1f, 1.3f)`。

这个 `CustomSpark` 是被动快斩的核心视觉。它应该被理解为“海蓝版 Lucrecia 竖向拖抹挥砍”，不是普通水花。

同时播放：

```csharp
SoundID.Item71
SoundID.Splash
```

`SoundID.Item71` 表现快速切割，`SoundID.Splash` 给它加湿润感。

### 15.4 挥砍过程中的 `DustID.Water`、`DustID.Frost`、`SparkParticle`、`CritSpark`

挥砍过程中，每 3 帧释放 `DustID.Water` 和 `DustID.Frost`，位置在玩家中心后方，速度向反方向喷出。

`DustID.Water`：

```csharp
Color.DeepSkyBlue
scale 1.15f ~ 1.7f
```

`DustID.Frost`：

```csharp
Color.Cyan
scale 0.95f ~ 1.45f
```

当 `t > 0.05f && t < 0.5f && timer % 5 == 0` 时，释放 `SparkParticle`。颜色是 `Color.Lerp(Color.DeepSkyBlue, Color.Cyan, random) * 0.72f`。`SparkParticle` 在这里负责挥砍中段的飞散光点。

第一次进入挥砍时，还会释放 6 个 `CritSpark`。这些 `CritSpark` 从 `Projectile.Center` 发出，速度高达 20 到 37，颜色从 `Color.DeepSkyBlue` 到 `Color.Cyan`，副色 `Color.White * 0.55f`，寿命 38 到 51 帧。它们是快斩中最强的粒子爆发。

### 15.5 命中时的 `GlowSparkParticle` 与 `DustID.Water`

`BrinyBaron_SkillSlashDash_SlashDash.OnHitNPC` 命中时播放 `SoundID.Item105`，然后释放两个 `GlowSparkParticle`。颜色第一刀使用 `Color.DeepSkyBlue`，第二刀使用 `Color.Cyan`。`GlowSparkParticle` 的寿命 9 帧，大小 0.05，缩放 `new Vector2(0.5f, 0.6f)`。

随后释放 10 个 `DustID.Water`，颜色随机 `Color.DeepSkyBlue` 或 `Color.Cyan`。这组 `DustID.Water` 是命中水花。

如果装备 `SurgeChainReactorEquipped`，还会生成 `SurgeChainWaterBurst`。虽然本文没有 `SurgeChainWaterBurst` 的文件，但从生成位置和名字看，它是额外的连锁水爆派生。后续大招如果要和配饰联动，应当把类似 `SurgeChainWaterBurst` 的派生独立命名，不要塞进主大招视觉。

---

## 16. 声音与屏幕震动的辅助特效

虽然本文重点是视觉，但这把武器的非大招反馈也明显依赖音效和屏幕震动。声音不是单独存在，它和视觉绑定得很紧。

| 名字 | 出现位置 | 功能 |
|---|---|---|
| `SoundID.Splash` | `BBSwing_INV`、`BrinyBaron_SkillDashTornado_BladeDash.SpawnChargeReadyBurst`、`BrinyBaron_SkillSlashDash_SlashDash` | 水花、水感、潮汐反馈 |
| `SoundID.Item71` | `BrinyBaron_RightClick_Shuriken` 粘附切割、`BrinyBaron_SkillDashTornado_BladeDash.StartRebound`、`BrinyBaron_SkillSlashDash_SlashDash` | 快速切割、刀刃划过 |
| `SoundID.Item39` | `BrinyBaron_RightClick_Shuriken` 扎入、`BrinyBaron_SkillDashTornado_BladeDash.StartDash` | 射出、突进、扎入 |
| `SoundID.Item73` | `BrinyBaron_SkillDashTornado_BladeDash.InitializeDash` | 技能准备启动 |
| `SoundID.Item84` | `BrinyBaron_TornadoBolt.SpawnTyphoon` | 水龙卷/水爆触发 |
| `SoundID.Item105` | `BrinyBaron_SkillSlashDash_SlashDash.OnHitNPC` | 命中锐利反馈 |
| `SoundID.Item107` | `BrinyBaron_RightClick_Shuriken.OnKill`、`BrinyBaron_SkillDashTornado_BladeDash.OnKill` | 消失、结束 |
| `SoundID.Item122` | `BrinyBaron_SkillDashTornado_BladeDash.SpawnChargeReadyBurst` | 充能完成、准备释放 |

屏幕震动函数是 `ApplyScreenShake`。`BrinyBaron_SkillDashTornado_BladeDash.SpawnChargeReadyBurst` 使用 `ApplyScreenShake(7f)`，`StartRebound` 使用 `ApplyScreenShake(10f)`。屏幕震动会根据玩家距离衰减：

```csharp
Utils.GetLerpValue(1200f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true)
```

这意味着短冲刺的冲击反馈有空间衰减，不是全屏无脑震。大招如果做大型冲击，也应该保留距离衰减，不然多人和远距离观感会很吵。

---

## 17. 从现有非大招特效推导大招重做方向

基于以上研究，大招重做时应该继承以下原则。

### 17.1 大招应该继续使用水蓝、冰白、深海蓝三层色彩

大招主色建议继续使用：

```csharp
Color.DeepSkyBlue
Color.Cyan
new Color(80, 195, 255)
new Color(120, 220, 255)
```

大招亮部建议继续使用：

```csharp
new Color(210, 248, 255)
new Color(220, 250, 255)
Color.White
```

大招暗部建议继续使用：

```csharp
new Color(12, 54, 110)
new Color(20, 86, 210)
new Color(25, 95, 205)
```

大招最好不要突然加入紫色、红色、金色，除非是剧情层面的特殊状态。否则会破坏 `NewLegendBrinyBaron` 当前的视觉统一性。

### 17.2 大招应该有方向性，不应该只是圆形蓝爆

现有普通攻击中，`BBSwing_Wave`、`BrinyBaron_TornadoBolt`、`BrinyBaron_RightClick_Shuriken`、`BrinyBaron_SkillDashTornado_BladeDash` 都非常依赖 `forward` 与 `right`。大招也应该如此。

如果大招是蓄力释放，可以使用：

- `DirectionalPulseRing` 表现蓄力方向性环。
- `CustomSpark` + `GlowBlade` 类贴图表现主刀尖光刃。
- `LineParticle` 表现水流丝线向前汇聚。
- `GlowOrbParticle` 表现水珠被卷入中心。
- `PrimitiveRenderer.RenderTrail` 表现主水刃轨迹。

如果大招是命中爆发，可以使用：

- `ImpactParticle` 做中心冲击。
- `WaterFoamParticle` 做泡沫扩散。
- `DustID.Water` 做主体水花。
- `DustID.Frost` 做冰白碎屑。
- `DirectionalPulseRing` 做方向性冲击波。
- `BrinyBaron_TornadoWaterExplosion` 类似的具名小水爆做范围节点。

### 17.3 大招应该继承“阶段成长”

现有代码里 `BBSwing_Wave` 有 `SpawnStage`，`BrinyBaron_RightClick_Shuriken` 有 `GrowthTier`，`BrinyBaron_SkillDashTornado_BladeDash` 有 `ShortDashProfile`。因此大招也不应该只有单一形态。即使大招最终只有一次释放，也可以在视觉上分阶段：

1. 蓄力阶段：`GlowOrbParticle` 向中心吸附，`LineParticle` 向刀尖汇聚，`DirectionalPulseRing` 缓慢收缩。
2. 临界阶段：`CustomSpark` + `window_04` 做中心 flare，`SimpleStar` 做冰白星芒，`SoundID.Item122` 做完成提示。
3. 释放阶段：`PrimitiveRenderer.RenderTrail` 或 `GlowBlade` 画主水刃，`CustomSpark` + `GlowBlade` 做刀尖光刃线。
4. 命中阶段：`ImpactParticle`、`WaterFoamParticle`、`DustID.Water`、`DustID.Frost`、`DirectionalPulseRing` 组合成命中爆发。
5. 消散阶段：`GlowOrbParticle` 慢速漂移，`DustID.Water` 和 `DustID.Frost` 少量散开，避免最后突然消失。

### 17.4 大招的 `CustomSpark` 必须有明确贴图身份

如果大招使用 `CustomSpark`，建议预先命名几种用途：

- `CustomSpark` + `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade`：用于主水刃或刀尖光刃。
- `CustomSpark` + `CalamityMod/Particles/BloomCircle`：用于水蓝爆点或中心 bloom。
- `CustomSpark` + `CalamityMod/Particles/VerticalSmearLarge`：用于瞬间大斩击拖抹。
- `CustomSpark` + `CalamityLegendsComeBack/Texture/KsTexture/window_04`：用于符文核心 flare。

绝对不要在文案或代码注释里只写“放一个 `CustomSpark`”。这句话没有视觉含义。必须写成“放一个 `CustomSpark`，贴图是某某路径，用途是某某视觉”。

### 17.5 大招可以把现有特效组合成“潮汐裁决”式结构

基于现有非大招特效，一个合理的大招视觉结构可以是：

- 使用 `GlowOrbParticle` 从屏幕周围向玩家刀尖汇聚，模拟潮汐回流。
- 使用 `LineParticle` 从两侧拉出水流丝线，表现海水被压缩成刃。
- 使用 `DirectionalPulseRing` 在刀尖前方连续收缩，表现水压临界。
- 使用 `CustomSpark` + `CalamityLegendsComeBack/Texture/KsTexture/window_04` 在玩家中心或刀尖生成符文 flare。
- 使用 `CustomSpark` + `CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade` 生成数条刀尖光刃。
- 使用 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:SideStreakTrail"]` 生成主水刃轨迹。
- 命中后使用 `ImpactParticle` 做中心冲击，`WaterFoamParticle` 做泡沫，`DustID.Water` 做水花，`DustID.Frost` 做冰白碎屑，`DirectionalPulseRing` 做扩散冲击环。
- 结束后使用 `GlowOrbParticle` 低速漂移和少量 `DustID.Water` / `DustID.Frost` 消散。

这样的大招不会脱离现有武器，而是把普通攻击、右键手里剑、短冲刺和被动快斩的视觉语言整合起来。

---

## 18. 结论：这把武器的非大招特效已经有成熟范式，大招应该继承而不是推翻

`NewLegendBrinyBaron` 目前的非大招特效已经形成了非常明确的美术范式：

- `BBSwing_Wave` 代表大型方向性水波和 Primitive 水蓝侧向拖尾。
- `BBSwing_INV` 代表隐形命中框与水花命中反馈。
- `BBSwing_Slash` 代表短生命周期水蓝外刃与冰白内芯刀光。
- `BrinyBaron_TornadoBolt` 代表符文水魔法核心弹与半追踪水流。
- `BrinyBaron_TornadoWaterExplosion` 代表短命水爆节点。
- `BrinyBaron_WaterStream` 代表轻量追踪水雾判定。
- `BrinyBaron_RightClick_Shuriken` 代表成长型飞盘、粘附切割、旋转副本和高阶光盘。
- `BrinyBaron_SkillDashTornado_BladeDash` 代表技能级高速水刃冲刺。
- `BrinyBaron_SkillDashTornado_FlightEffects` 代表短冲刺刀尖特效工具箱。
- `BrinyBaron_DashWaterPillar` 代表命中派生竖向水柱。
- `BrinyBaron_SkillSlashDash_SlashDash` 代表被动快斩、海蓝 Lucrecia 式拖抹挥砍。

这套体系的核心不是“多放点蓝色粒子”，而是 **每一个视觉效果都有名字、位置、方向、颜色、寿命和职责**。`GlowOrbParticle` 是水珠和余光，`LineParticle` 是水流丝线，`ImpactParticle` 是命中冲击，`DirectionalPulseRing` 是方向性水压环，`WaterFoamParticle` 是泡沫，`DustID.Water` 是主体水花，`DustID.Frost` 是冰白碎屑，`DustID.GemSapphire` 是高阶切割碎片，`CustomSpark` 则必须根据贴图路径区分为刀锋型、bloom 型、符文 flare 型或拖抹型。

因此后续大招重做时，最重要的不是“做得更大”，而是“把这套已有语言组织成一个更高等级的仪式”。大招应该看起来像这些普通攻击的终极汇总：先有潮汐吸附，再有符文临界，再有水刃释放，再有泡沫冲击，最后有冰白余光消散。这样玩家才会觉得：这不是另一把武器突然乱入，而是 `NewLegendBrinyBaron` 真正的最终释放。

---

## 附录 A：推荐给朋友的实现检查表

如果朋友要照着这份文档重做大招，可以按下面检查：

- 是否给每一个特效写了名字，例如 `GlowOrbParticle`、`LineParticle`、`DirectionalPulseRing`、`CustomSpark`？
- 如果用了 `CustomSpark`，是否写清楚了贴图路径？
- 是否区分了蓄力、释放、命中、消散四个阶段？
- 是否保留了水蓝、冰白、深海蓝三层配色？
- 是否有 `forward` / `right` 方向性，而不是原地圆形乱炸？
- 是否控制了 `extraUpdates` 下的粒子释放频率？
- 是否把 `DustID.Water` 用作主体水花，把 `DustID.Frost` 用作冰白亮部？
- 是否只在高阶切割或特殊爆裂中使用 `DustID.GemSapphire`？
- 是否用 `WaterFoamParticle` 表现真正的泡沫，而不是全靠蓝光？
- 是否用 `ImpactParticle` 或 `DirectionalPulseRing` 做命中核心反馈？
- 是否用 `PrimitiveRenderer.RenderTrail` 或 `GlowBlade` 做主水刃，而不是只有 Dust？
- 是否让大招看起来像 `BBSwing_Wave`、`BrinyBaron_TornadoBolt`、`BrinyBaron_RightClick_Shuriken`、`BrinyBaron_SkillDashTornado_BladeDash` 的终极集合？

只要这些问题答案大多是“是”，大招方向基本就不会偏。
