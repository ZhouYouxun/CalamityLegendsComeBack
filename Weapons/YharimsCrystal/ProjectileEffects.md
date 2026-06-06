# YharimsCrystal 弹幕特效笔记

范围：当前 `Weapons/YharimsCrystal` 下的活跃实现；不统计 `Abandoned` 旧目录、Cooldown/UI、Player 类。下面把特效分成“绘制函数特效”和“AI/命中粒子特效”。源码里有少量保留但当前未看到生成调用的弹幕，单独标注。

## 主水晶与 Drone

### YC_TyrantPrismHoldout
- 绘制函数特效：继承 `YC_BaseHoldout` 的 `DrawPrism`，绘制 `YharimsCrystalPrism` 6 帧水晶；自身 `PreDraw` 额外在 `MainMuzzle` 叠两层 `CalamityMod/Particles/BloomCircle` 金白枪口光。
- AI/命中粒子特效：收束阶段 `EmitConvergenceFX` 生成 `DustID.GoldFlame`；战斗阶段 `EmitCombatFX` 生成金白 `GlowOrbParticle`；重火力休息阶段 `EmitRestFX` 生成 `DustID.SteampunkSteam`。

### YC_TyrantPrismDrone
- 绘制函数特效：`PreDraw` 画 drone 本体和旧位置金色残影；终极技等待开火时 `DrawUltimateCrosshair` 使用 `BloomRing` 和 `HalfStar` 在 drone 身上画准星。
- AI/命中粒子特效：普通待机/战斗时生成 `DustID.GoldFlame`；重火力枪口用 `DustID.YellowTorch`/`DustID.GoldFlame` 和 `GlowSparkParticle`；终极技充能/开火时生成 `GlowOrbParticle` 和 `DirectionalPulseRing`。

### YC_YharimsCrystalBeam
- 绘制函数特效：`PreDraw` 使用 `Utils.DrawLaser` 绘制 `CalamityMod/Projectiles/Magic/YharimsCrystalBeam`，先画彩色外层，再画白色细核心；没有 primitive shader。
- AI/命中粒子特效：光束末端生成 `DustID.CopperCoin`，颜色来自玩家名/光束编号 hue；充能后向 `Filters.Scene["WaterDistortion"]` 的 `WaterShaderData.QueueRipple` 排水波扰动；同时沿光束投光。

## TyrantPrism 攻击弹幕

### YC_TyrantPrismBolt
- 绘制函数特效：`PreDraw` 调 `CalamityUtils.DrawAfterimagesCentered` 画 centered afterimage，弹幕本体是隐形贴图。
- AI/命中粒子特效：飞行时生成 `LineParticle`；命中时生成 `GenericSparkle` 和 `SparkParticle`，死亡时生成 `DustID.Electric`，并播放 AuricBulletHit。

### YC_TyrantPrismMissile
- 绘制函数特效：使用 `ThePackMissile` 9 帧动画；`PreDraw` 用 `CalamityUtils.DrawAfterimagesCentered` 画金色 afterimage，再画本体。
- AI/命中粒子特效：飞行尾焰使用 `DustID.YellowTorch`/`DustID.GoldFlame` 和 `CustomSpark("CalamityMod/Particles/BloomCircle")`；死亡爆炸生成 `CustomPulse("CalamityMod/Particles/FlameExplosion")`、`DirectionalPulseRing`、大量金火 Dust 和 `SparkParticle`。

### YC_TyrantPrismConvergeBeam
- 绘制函数特效：使用 `YC_YharimBeamVisuals.DrawYharimBeam`，底层是 `Utils.DrawLaser` 绘制 `YharimsCrystalBeam` 外层与白色核心。
- AI/命中粒子特效：本体没有额外粒子，主要负责从 drone 指向收束焦点的收束光束、投光和碰撞。

### YC_TyrantPrismLaserLance
- 绘制函数特效：使用 `YC_YharimBeamVisuals.DrawYharimBeam` 绘制短生命周期金色光束。
- AI/命中粒子特效：光束线上周期性生成 `GlowOrbParticle`。当前源码里只看到类定义，未看到主动生成调用。

### YC_TyrantPrismMainBeam
- 绘制函数特效：使用 `YC_YharimBeamVisuals.DrawYharimBeam` 绘制主光束，随 `MainBeamStrength` 放大和变亮。
- AI/命中粒子特效：调用 `YC_YharimBeamVisuals.EmitYharimBeamDust`，在光束末端生成 `DustID.CopperCoin`；同时沿光束投光和切草。当前源码里只看到清理逻辑，未看到主动生成调用。

## 右键屠戮

### YC_TyrantSlaughterHoldout
- 绘制函数特效：`PreDraw` 使用 `BloomLineAngled` 画拖尾光带，挥砍时使用 `VerticalSmearLarge` 画橙金双层 slash，水晶本体叠 8 向金色描边，枪口再叠 `BloomCircle`。
- AI/命中粒子特效：持有时 `EmitHoldFlameFX` 生成 `CustomSpark("CalamityMod/Particles/BloomCircle")` 并维护跟随玩家位移；挥砍边缘 `EmitSlashEdgeFX` 生成 `CustomSpark("CalamityMod/Particles/DemonSigilParticle")`；命中时生成 `DustID.GoldFlame`、`SparkParticle` 和两层 `CustomPulse("BloomCircle")`；结束时如果有目标，会追加 `CustomPulse("HighResHollowCircleHardEdge")` 并生成 Calamity 的 `DevilsStrike`/`DirectStrike`。

## EX 技

### YC_EX_VIP
- 绘制函数特效：本体为隐藏控制器，没有自绘。
- AI/命中粒子特效：召唤阶段生成 `DustID.GoldFlame`；Drone 充能阶段生成向中心收拢的 `GlowOrbParticle`；等待开火阶段生成 `DirectionalPulseRing`；开火阶段生成 `SquishyLightParticle`。实际激光和弹幕由已有 `YC_TyrantPrismDrone` 与 `YC_YharimsCrystalBeam` 表现。

## 共享光束工具

### YC_YharimBeamVisuals
- 绘制函数特效：`DrawYharimBeam` 统一使用 `Utils.DrawLaser` 画 `YharimsCrystalBeam`，外层颜色乘 0.75，内层白色核心乘 0.1。
- AI/命中粒子特效：`EmitYharimBeamDust` 统一在光束末端生成 `DustID.CopperCoin`，含主末端 Dust 和少量横向 Dust。
