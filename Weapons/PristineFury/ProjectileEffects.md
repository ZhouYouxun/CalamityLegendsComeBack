# PristineFury 弹幕特效笔记

范围：当前 `Weapons/PristineFury` 下的弹幕类；不统计 `Player`、Mark/规则 helper、纯触发类。下面把特效分成“绘制函数特效”和“AI/命中粒子特效”。

## 本体、右键、钩爪与被动

### NewLegendPristineFuryHoldOut
- 绘制函数特效：`PreDraw` 手动画武器本体和 glow 贴图；龙眼、龙口烟雾、FakeCalamity 充能、右键 Arc Nova 充能、枪口光和钩爪蓄力条分别用 `BloomCircle`、`HalfStar`、`magic_03`、`smoke_04`、`ForwardSmear`、`BloomRing`、`GenericBarBack/Front`。
- AI/命中粒子特效：持有和左、右键准备阶段生成 `GlowOrbParticle`、`PointParticle` 等小光点；具体攻击粒子主要由各阶段 effect projectile 承担。

### PristineFuryPassiveTentacle
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：被动触手的可见部分靠粒子，主要用 `CustomSpark("CalamityMod/Particles/BloomCircle")`、`DustID.Shadowflame` 和 `DustID.Torch`。

### PristineFuryHook
- 绘制函数特效：`PreDraw` 使用 `CalamityMod/Particles/ThinEndedLine` 逐段画链线，再画钩爪本体。
- AI/命中粒子特效：命中/释放时生成 `SparkParticle` 和 `DirectionalPulseRing`。

### PristineFuryRightPellet
- 绘制函数特效：没有自定义绘制，使用默认弹幕绘制和 trail cache。
- AI/命中粒子特效：飞行时生成 `DustID.Torch`，落地生成 `PristineFuryGroundFlame`。

### PristineFuryGroundFlame
- 绘制函数特效：没有自定义绘制。
- AI/命中粒子特效：用 `MediumMistParticle` 做地面火雾，并用 `SparkParticle` 点缀火星。

### PristineFuryImpactExplosion
- 绘制函数特效：`PreDraw` 画爆炸贴图两层，一层主题色，一层白色核心。
- AI/命中粒子特效：爆炸启动时生成 `SparkParticle`、`DirectionalPulseRing`、`CustomPulse("SoftRoundExplosion")`、`CustomPulse("FlameExplosion")`。

### PristineFuryRightNovaChargeOrb
- 绘制函数特效：调用 `PristineFuryRightNovaVisuals.DrawArcNovaOrb`，使用 `BloomCircle`、`ForwardSmear`、`BloomRing`、`FullStar` 画右键蓄力球。
- AI/命中粒子特效：充能时生成 `SparkParticle` 或 `PointParticle`，满蓄力/脉冲时生成 `DirectionalPulseRing`、`CustomPulse("BloomCircle")` 和 `DustID.Torch`。

### PristineFuryRightNovaFireball
- 绘制函数特效：`PreDraw` 画旧位置 `BloomCircle` 残影、火球本体，并调用 `DrawArcNovaOrb` 画 Arc Nova 核心。
- AI/命中粒子特效：飞行时生成 `MediumMistParticle`、`CustomSpark("SmallBloom")`；爆炸时生成 `PristineFuryImpactExplosion`，并生成 `DirectionalPulseRing`、`CustomPulse("SoftRoundExplosion")`、`SparkParticle`。

### PristineFuryRightNovaPseudoLaser
- 绘制函数特效：`PreDraw` 用 `Utils.DrawLaser` 绘制 `CalamityMod/Projectiles/LaserProj` 两层激光，并在起点/终点画 `BloomCircle`。
- AI/命中粒子特效：沿激光生成 `PointParticle` 和 `DustID.Torch`。

## APreHardMode

### PFIdle_Flame
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：使用 `SparkParticle` 和 `ModContent.DustType<FinalFlame>()`。

### PFEvilT2_Flame
- 绘制函数特效：`PreDraw` 使用 `BloomCircle`、`ThinEndedLine`、`HalfStar`、`magic_03`、`magic_04` 画法阵/准星式火焰。
- AI/命中粒子特效：生成 `DustID.GoldFlame`/`DustID.YellowTorch` 和 `GlowOrbParticle`。

### PFSlimeGod_Flame
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 画黏液神主题火团。
- AI/命中粒子特效：生成 `DustID.GoldFlame`/`DustID.YellowTorch` 和 `GlowOrbParticle`。

## BPrePlantera

### PFBrimstoneElemental_Flame
- 绘制函数特效：`PreDraw` 使用 `CalamityMod/Particles/MediumMist` 画硫火雾。
- AI/命中粒子特效：生成 `ModContent.DustType<BrimstoneFlame>()` 和 `MediumMistParticle`。

### PFBrimstoneElemental_Barrage
- 绘制函数特效：`PreDraw` 使用 `CalamityUtils.DrawAfterimagesCentered` 和 `BloomCircle` 画 Hellborn 弹体/光晕。
- AI/命中粒子特效：生成 `DiamondDust`、`SquashDust`，爆发时生成 `CustomPulse("BloomCircle")`。

### PFBrimstoneElemental_Laser
- 绘制函数特效：`PreDraw` 分段绘制 `BrimstoneRayMid` 和 `BrimstoneRayEnd`，不使用 `Utils.DrawLaser`。
- AI/命中粒子特效：光束末端生成 `CalamityDusts.Brimstone`、`GlowOrbParticle` 和 `DirectionalPulseRing`。

### PFFakeCalamity_ChargeOrb
- 绘制函数特效：`PreDraw` 使用 `BloomCircle`、`ForwardSmear`、`BloomRing` 画蓄力球。
- AI/命中粒子特效：蓄力完成时生成 `DirectionalPulseRing`，并生成 `DustID.GoldFlame`/`DustID.YellowTorch`。

### PFFakeCalamity_NovaOrb
- 绘制函数特效：`PreDraw` 使用旧位置 `BloomCircle` 残影和 `HalfStar` 星芒。
- AI/命中粒子特效：生成 `SquashDust`、`GlowOrbParticle`、`CustomSpark("BloomCircle")`、`CustomSpark("SmallBloom")`。

### PFFakeCalamity_NovaExplosion
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：生成 `DirectionalPulseRing`、`CustomPulse("SoftRoundExplosion")`、`CustomPulse("BloomCircle")`、`SparkParticle`、`PointParticle` 和金色 Dust。

### PFHardMode_TotalityFire
- 绘制函数特效：使用 `CalamityUtils.DrawAfterimagesCentered`，整个 `PreDraw` 返回 `false`。
- AI/命中粒子特效：生成 `DustID.GoldFlame`/`DustID.YellowTorch`、`DirectionalPulseRing`、`CustomPulse`、`SparkParticle`、`PointParticle`、`MediumMistParticle`。

### PFHardMode_GroundFire
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：使用 `MediumMistParticle` 做地面火。

### PFHardMode_HeavyFireball
- 绘制函数特效：`PreDraw` 画火球本体和光晕。
- AI/命中粒子特效：生成 `DustID.GoldFlame`/`DustID.YellowTorch`、`GlowOrbParticle`、`HeavySmokeParticle`、`MediumMistParticle`、`SparkParticle`。

### PFPlantera_PseudoLaser
- 绘制函数特效：`PreDraw` 手动画伪激光线段。
- AI/命中粒子特效：没有额外粒子，主要是伪激光 hitbox。

### PFPlantera_Flame
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：主要是逻辑/伤害弹幕，源码中没有额外粒子调用。

### PFPrime_Flame
- 绘制函数特效：`PreDraw` 使用 `MediumMist` 画火雾。
- AI/命中粒子特效：生成 `DustID.Torch`/`DustID.SolarFlare`、`CustomSpark`、`SparkParticle`。

### PFPrime_BounceExplosion
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：生成扩散爆发粒子，主要服务 `PFPrime_Flame` 的弹跳爆炸。

## CPreMoodLord

### PFAurora_Flame
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 和 `ThinEndedLine` 画极光式光束。
- AI/命中粒子特效：生成 `SparkParticle` 和 `CustomSpark("ThinEndedLine")`。

### PFAurora_MuzzleOrb
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 和 `HalfStar` 画枪口球。
- AI/命中粒子特效：生成 `GlowOrbParticle`。

### PFGoliath_ReaperDrone
- 绘制函数特效：`PreDraw` 使用 drone 本体、`BloomCircle`、`XykWingOrange1`、`XykWingOrange2` 画翅膀和发光层。
- AI/命中粒子特效：生成 `DustID.GoldFlame`/`DustID.GreenTorch`、`GlowOrbParticle`、`HeavySmokeParticle`。

### PFGoliath_MouseCrosshair
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：没有粒子，主要是鼠标准星定位控制弹幕。

### PFGoliath_HiveNukeMissile
- 绘制函数特效：`PreDraw` 使用 `CalamityUtils.DrawAfterimagesCentered`，并结合 `StarProj` 和 `FlameExplosion` 光效。
- AI/命中粒子特效：生成 `DustID.GreenTorch`、`GlowOrbParticle`、`HeavySmokeParticle`。

### PFGoliath_Flame
- 绘制函数特效：`PreDraw` 手动画火焰本体。
- AI/命中粒子特效：生成 `DustID.GemDiamond` 和 `MediumMistParticle`。

## DPreDog

### PFDog_ChargeOrb
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 、`BloomRing`、`CircularSmear` 画吞噬者蓄力球。
- AI/命中粒子特效：生成 `SparkParticle`、`GlowOrbParticle`、`SquishyLightParticle`、`HeavySmokeParticle`、`DirectionalPulseRing`、`CustomPulse`、`DustID.GoldFlame`/`SquashDust`。

### PFDog_Flame
- 绘制函数特效：实现 `IPixelatedPrimitiveRenderer`；使用 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]`，分别绑定 `ScarletDevilStreak` 和 `SylvestaffStreak`；常规 `PreDraw` 还用 `BloomCircle`、`BloomRing`、`CircularSmear` 画核心。
- AI/命中粒子特效：生成 `GlowOrbParticle`、`SquishyLightParticle`、`HeavySmokeParticle`、`SparkParticle`、`DirectionalPulseRing`、`CustomPulse` 和 SquashDust。

### PFMoonlord_Flame
- 绘制函数特效：`PreDraw` 使用 `ThinEndedLine` 画月总主题火线。
- AI/命中粒子特效：生成 `GlowOrbParticle`。

### PFMoonlord_VortexScorpioRocket
- 绘制函数特效：`PreDraw` 画 `ScorpioRocket_Glow`，并通过 `IPixelatedPrimitiveRenderer` 使用 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + `SylvestaffStreak`。
- AI/命中粒子特效：生成 `DustID.Vortex`、`NanoParticle` 和 `DirectionalPulseRing`。

### PFMoonlord_SolarLaser
- 绘制函数特效：`PreDraw` 使用 `Utils.DrawLaser` 绘制太阳激光。
- AI/命中粒子特效：沿光束生成 `BloomLineVFX`、`GlowOrbParticle`、`GlowSparkParticle`、`SparkParticle`，末端生成 `PFMoonlord_SolarExplosion`。

### PFMoonlord_SolarExplosion
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：生成 `CustomPulse("SoftRoundExplosion")`、`DirectionalPulseRing` 和 `CustomSpark("SmallBloom")`。

### PFPolterghast_Flame
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 画鬼妖村正主题火团。
- AI/命中粒子特效：生成 `SparkParticle`、`DirectionalPulseRing`、`DustID.GoldFlame`/`DustID.YellowTorch`。

### PFProvidence_Flame
- 绘制函数特效：`PreDraw` 使用 `ProvidenceMarkParticle` 和 `SmallBloom`。
- AI/命中粒子特效：生成 `CustomPulse("ProvidenceMarkParticle")`、`CustomSpark("ProvidenceMarkParticle")`、`CustomSpark("SmallBloom")`。

### PFProvidence_NukeOfBliss
- 绘制函数特效：`PreDraw` 使用 `SoftRoundExplosion` 画大范围核爆视觉。
- AI/命中粒子特效：生成 `LightDust`、`GlowOrbParticle`、`HeavySmokeParticle`、`SquishyLightParticle`。

### PFProvidence_HolyShrapnel
- 绘制函数特效：`PreDraw` 使用 `BloomCircle` 和 `HalfStar` 画圣火碎片。
- AI/命中粒子特效：主要是运动逻辑，源码中没有额外粒子调用。

### PFProvidence_HolyFireField
- 绘制函数特效：`PreDraw` 使用 `ProvidenceMarkParticle` 和 `SoftRoundExplosion` 画圣火场地。
- AI/命中粒子特效：主要是区域持续伤害，源码中没有额外粒子调用。

### PFProvidence_HolyRainOrb
- 绘制函数特效：`PreDraw` 手动画雨弹/光球。
- AI/命中粒子特效：生成 `GlowOrbParticle` 和 `SparkParticle`。

### PFProvidence_MoltenRainBlob
- 绘制函数特效：`PreDraw` 手动画熔雨弹体。
- AI/命中粒子特效：生成 `GlowOrbParticle`、`HeavySmokeParticle`、`MediumMistParticle`。

### PFRavager_BloodBoilerOrb
- 绘制函数特效：没有自定义绘制，主要靠粒子表现。
- AI/命中粒子特效：生成 `MediumMistParticle`、`SparkParticle`，并使用 `DetailedExplosion`、`DustyCircleHardEdge` 贴图相关爆发。

### PFRavager_Laser
- 绘制函数特效：`PreDraw` 使用 `BloomCircle`、`ThinEndedLine`、`PearlParticleGlow`、`WaterFoam` 画血沸激光。
- AI/命中粒子特效：生成 `DustID.GoldFlame`、`DustID.LifeDrain`。

## EAfterDog

### PFDragon_Flame
- 绘制函数特效：无传统 projectile 绘制，`PreDraw` 返回 `false`，视觉主要靠粒子。
- AI/命中粒子特效：生成 `DustID.Torch`/`DustID.GoldFlame`、`SmallSmokeParticle`、`CustomSpark("SmallBloom")`、`CustomPulse("BloomRing")`、`SparkParticle`。

### PFDragon_Burst
- 绘制函数特效：无绘制，`PreDraw` 返回 `false`。
- AI/命中粒子特效：生成 `SparkParticle`、`SmallSmokeParticle`、`CustomPulse("BloomRing")`。

### PFExoTwins_ArtemisLaser
- 绘制函数特效：使用 `CalamityUtils.DrawAfterimagesCentered` 和 `LaserWallTelegraphBeam` 绘制 Artemis 激光与预警线。
- AI/命中粒子特效：主要是激光逻辑，源码中没有额外粒子调用。

### PFExoTwins_ApolloRocket
- 绘制函数特效：使用 `CalamityUtils.DrawAfterimagesCentered`，并绘制 `ApolloRocketGlow`。
- AI/命中粒子特效：生成 Exo 主题尘埃，使用 `ExoMechEffects` 内部随机 dust type。

### PFExoAresLaserBeamStart
- 绘制函数特效：无常规 `PreDraw`；内部绘制使用 `AresLaserBeamMiddle` 和 `AresLaserBeamEnd` 分段激光贴图。
- AI/命中粒子特效：沿光束生成 Exo 主题 dust。

### PFExoThanatosBeamTelegraph
- 绘制函数特效：`PreDraw` 使用 `LaserWallTelegraphBeam` 画 Thanatos 预警线。
- AI/命中粒子特效：主要是预警控制，源码中没有额外粒子调用。

### PFExoThanatosBeamStart
- 绘制函数特效：无常规 `PreDraw`；内部绘制使用 `ThanatosBeamMiddle` 和 `ThanatosBeamEnd` 分段激光贴图。
- AI/命中粒子特效：光束末端生成 `BrimstoneFlame` Dust。

### PFExoThanatosSideLaser
- 绘制函数特效：使用 `CalamityUtils.DrawAfterimagesCentered` 和 `LaserWallTelegraphBeam` 绘制侧向激光。
- AI/命中粒子特效：主要是激光逻辑，源码中没有额外粒子调用。
