# BrinyBaron 弹幕特效笔记

范围：当前 `Weapons/BrinyBaron` 下的弹幕类；不统计 `Cooldown`、`Player`、纯辅助 UI 类，也不统计 `SkillB_SpinDash已删除` 旧目录。下面把特效分成“绘制函数特效”和“AI/命中粒子特效”。

## CommonAttack

### BrinyBaron_LeftClick_Swing
- 绘制函数特效：`PreDraw` 手动画武器本体；命中窗口用 `CalamityMod/Particles/VerticalSmearLarge` 水蓝挥砍 smear；叠 18 层 `NewLegendBrinyBaronGoest` 幽灵刀影形成外发光。
- AI/命中粒子特效：挥砍时生成 `LineParticle` 和 `DustID.Water`；右键蓄力旋转时生成 `DustID.Water`/`DustID.Frost` 环绕水尘，满蓄力后维护一个 `CircularSmearSmokeyVFX` 圆形烟雾拖影；阶段攻击会生成 `BBSwing_Wave`、`BrinyBaron_RightClick_Shuriken`、`BrinyBaron_TornadoBolt`。

### BBSwing_Wave
- 绘制函数特效：核心视觉是 `PrimitiveRenderer.RenderTrail`，使用 `GameShaders.Misc["CalamityMod:SideStreakTrail"]` 并绑定 `Images/Misc/Perlin`；随后画一个不可见 projectile 贴图占位。
- AI/命中粒子特效：飞行时生成两侧 `GlowOrbParticle` 尾浪、`DustID.Water`/`DustID.Frost` 水雾，以及高阶段的 `GlowSparkParticle`；减速时额外生成漂移 `GlowOrbParticle`；定时释放 `BrinyBaron_HomingBubble`，命中时爆水/霜 Dust。

### BBSwing_Slash
- 绘制函数特效：`PreDraw` 使用 `TextureAssets.Extra[ExtrasID.SharpTears]` 画两层尖刺 slash，一层横向水蓝主体，一层垂直白蓝核心。
- AI/命中粒子特效：生成初始爆发，包含 `DustID.Water`、`DustID.Frost` 和一个白色 `GlowSparkParticle`。

### BBSwing_INV
- 绘制函数特效：无可见绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：这是隐形命中框；命中时生成 `DustID.Water`/`DustID.Frost`，并在需要时给 `BBEXPlayer` 加 Tide 和屏幕震动。

### BrinyBaron_RightClick_Shuriken
- 绘制函数特效：本体用 `TornadoProj` 贴图；`BBShuriken_Initial_Effects.DrawOutlineAndBody` 画 8 向蓝色描边和本体；困难模式阶段 `DrawRotatingCopies` 画 4 个旋转副本；猪鲨后阶段 `PostDraw` 用 `CircularSmearSmokey`、`SemiCircularSmearSwipe` 和额外光环画旋转刃盘。
- AI/命中粒子特效：初始飞行用 `DustID.Water`；命中/死亡用水尘和霜尘爆发；可粘附阶段会持续吐 `DustID.Frost`/`DustID.Water`/`DustID.GemSapphire` 切割粒子；鱼龙阶段加 `GlowOrbParticle` 螺旋轨迹，猪鲨阶段加 `CustomSpark("CalamityMod/Particles/BloomCircle")` 双线火花。

### BrinyBaron_HomingBubble
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：飞行时生成泡泡 `Gore` 411/412；命中或消失时生成 `DustID.Water` 爆泡。

### BrinyBaron_TornadoBolt
- 绘制函数特效：没有自定义 `PreDraw`，使用默认 `Projectile_407` 动画帧。
- AI/命中粒子特效：飞行时生成 `DustID.Water`；命中、撞墙或死亡时生成 `BrinyBaron_Tornado`。

### BrinyBaron_Tornado
- 绘制函数特效：`PreDraw` 画两个 `TornadoProj`，一层按 `Projectile.rotation` 旋转，一层反向旋转的青蓝透明叠影。
- AI/命中粒子特效：环绕生成 `DustID.Water`/`DustID.Frost`，模拟龙卷内部水雾。

### BrinyBaron_WaterStream
- 绘制函数特效：无可见绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：追踪飞行时生成 `DustID.Water`/`DustID.Frost` 尾流。当前源码里只看到类定义，未看到主动生成调用。

### BBShuriken_Light
- 绘制函数特效：`PreDraw` 使用 `TextureAssets.Extra[ExtrasID.ThePerfectGlow]` 画旧位置残影和中心 4 层旋转光环。
- AI/命中粒子特效：飞行时生成 `DustID.Water`/`DustID.Frost`、`GlowOrbParticle` 和 `GlowSparkParticle`；死亡时再爆一圈水/霜 Dust 和 `GlowSparkParticle`。它由高成长阶段手里剑死亡时生成。

### BBShuriken_Lazer
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：隐形高速 hitbox；飞行时生成双螺旋 `GlowOrbParticle`、`CustomSpark("CalamityMod/Particles/ThinEndedLine")` 和水/霜 Dust；命中时爆 `GlowSparkParticle` 与水/霜 Dust。当前源码里只看到类定义，未看到主动生成调用。

## SkillA_ShortDash

### BrinyBaron_SkillDashTornado_BladeDash
- 绘制函数特效：`PreDraw` 手动切换 Additive/AlphaBlend；使用 `CalamityMod/Particles/GlowBlade` 画冲刺前端的外层光刃、壳层光刃和核心光刃；旧位置画武器残影。
- AI/命中粒子特效：开始、飞行/回弹阶段调用 `BrinyBaron_SkillDashTornado_FlightEffects`，生成 `DirectionalPulseRing`、`CustomSpark(GlowBlade)`，水/霜 Dust 和泡泡 Gore；命中后生成 `BBASD_Lighting`，并按成长阶段喷出手里剑。

### BBASD_Lighting
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`，碰撞也关闭，主要作为视觉电痕。
- AI/命中粒子特效：高速 extraUpdates 的曲线电弧，周期性生成 `CustomSpark("CalamityMod/Particles/BloomCircle")`，并可递归分叉成更小电弧。

## SkillD_SuperDash

### Z_BrinyBaron_SkillSuperCharge_SuperDash
- 绘制函数特效：锁定阶段调用 `BBSD_Lock_Effects.DrawLockBeam`，使用 `ThinEndedLine` 和 `BloomCircle` 画锁定线；充能/锁定阶段调用 `DrawTargetingReticle`，使用 `BloomCircle`、`magic_03`、`magic_04` 画准星；冲刺阶段画武器本体和 4 向青蓝描边。
- AI/命中粒子特效：充能开始、充能中、充能完成分别调用 `BBSD_ChargeBegan_Effects`、`BBSD_Charge_Effects`、`BBSD_ChargeFinish_Effects`，主要是 `DirectionalPulseRing`、`CustomSpark`、`GlowOrbParticle`、`LineParticle`、`HeavySmokeParticle` 和 `DustID.Frost`/`GemSapphire`/`GemTopaz`；锁定使用 `BBSD_Lock_Effects` 的 `DirectionalPulseRing`、`CustomSpark`、`LineParticle`、`GlowOrbParticle` 和蓝宝石 Dust；瞬移、冲刺使用 `BBSD_Teleport_Effects`、`BBSD_Strike_Effects` 的 `CustomSpark`、`CustomPulse`、`DirectionalPulseRing`、`GlowOrbParticle`，水/霜/黄火 Dust；终段会生成 `BBSD_Final_INV`。

### BBSD_VirtualPROJ
- 绘制函数特效：随机使用 `KsTexture/star_01` 到 `star_09`，再画 `BloomCircle`；轨迹用 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + `Images/Misc/Perlin`。
- AI/命中粒子特效：沿贝塞尔曲线飞回玩家，飞行时生成 `GlowOrbParticle` 和 `LineParticle`；抵达时生成 `DirectionalPulseRing` 和一个 `GlowOrbParticle`。

### BBSD_Star
- 绘制函数特效：用 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + Perlin 绘制星轨；本体用 `StarofJudgement`、`BloomCircle` 和 `ThePerfectGlow` 叠加星芒。
- AI/命中粒子特效：飞行时双螺旋 `GlowOrbParticle`，并混用 `DustID.Frost`、`DustID.YellowTorch`；命中/消失时爆 `DustID.Water`、`DustID.Frost`、旋转 `GlowOrbParticle` 和 `DirectionalPulseRing`。

### BBSD_Final_INV
- 绘制函数特效：`PreDraw` 在目标身上画两层 `BloomCircle` 终结标记。
- AI/命中粒子特效：目标周围周期性生成 `CustomSpark(".../SkillA_ShortDash/GlowBlade")`、`DirectionalPulseRing`、`DustID.Water`/`DustID.YellowTorch`；每 5 帧释放一个放大的 `BBSwing_Slash`。
