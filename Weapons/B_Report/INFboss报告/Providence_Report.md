# 亵渎天神 (Providence) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Providence`
- **重写的NPC目标**: `ModContent.NPCType<ProvidenceBoss>()`, `ModContent.NPCType<ProfanedRocks>()`, `ModContent.NPCType<ProvSpawnHealer>()`, `ModContent.NPCType<ProvSpawnOffense>()`
- **关联源文件**:
  - `AcceleratingCrystalShard.cs`
  - `AcceleratingMagicProfanedRock.cs`
  - `CleansingFireball.cs`
  - `CommanderSpear2.cs`
  - `CrystalTelegraphLine.cs`
  - `DyingSun.cs`
  - `FallingCrystalShard.cs`
  - `HolyBasicFireball.cs`
  - `HolyBomb.cs`
  - `HolyCinder.cs`
  - `HolyCross.cs`
  - `HolyCrystalSpike.cs`
  - `HolyMagicLaserbeam.cs`
  - `HolyRitual.cs`
  - `HolySpear.cs`
  - `HolySpearFirePillar.cs`
  - `HolySunExplosion.cs`
  - `ProfanedLava.cs`
  - `ProfanedLavaBlob.cs`
  - `ProfanedRocksBehaviorOverride.cs`
  - `ProvBoomDeath.cs`
  - `ProviBurnPulseRing.cs`
  - `ProvidenceArenaBorder.cs`
  - `ProvidenceAttackerGuardianBehaviorOverride.cs`
  - `ProvidenceBehaviorOverride.cs`
  - `ProvidenceHealerGuardianBehaviorOverride.cs`
  - `ProvidenceMusicSceneInfernum.cs`
  - `ProvidenceWave.cs`
  - `ProvSummonFlameExplosion.cs`
  - `StrongProfanedCrack.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.7f`
- `Phase Ratio Array: Phase2LifeRatio`
- `DeathAnimationLifeRatio: 0.04f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `ProvidenceAttackType`
- `// Phase 1.
            FireEnergyCharge`
- `CinderAndBombBarrages`
- `AcceleratingCrystalFan`
- `AttackGuardiansSpearSlam`
- `HealerGuardianCrystalBarrage`
- `// Phase 2.
            EnterFireFormBulletHell`
- `EnvironmentalFireEffects`
- `CleansingFireballBombardment`
- `CooldownState`
- `ExplodingSpears`
- `SpiralOfExplodingHolyBombs`
- `EnterHolyMagicForm`
- `RockMagicRitual`
- `ErraticMagicBursts`
- `DogmaLaserBursts`
- `// Blast TBOI attack idea real???

            EnterLightForm`
- `FinalPhaseRadianceBursts`
- `CrystalForm`
### 状态机/枚举: `SpearAttackState`
- `LookAtTarget`
- `SpinInPlace`
- `Charge`
### 状态机/枚举: `HealerGuardianAttackState`
- `SpinInPlace`
- `WaitAndReleaseTelegraph`
- `ShootCrystals`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Only damage damage once really close to Providence.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `HolySpear`
  - *实现细节*: `HolySpear.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyCrystalSpike`
  - *实现细节*: `HolyCrystalSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyCross`
  - *实现细节*: `HolyCross.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AcceleratingMagicProfanedRock`
  - *实现细节*: `AcceleratingMagicProfanedRock.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolySpearFirePillar`
  - *实现细节*: `HolySpearFirePillar.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ProvSummonFlameExplosion`
  - *实现细节*: `ProvSummonFlameExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CleansingFireball`
  - *实现细节*: `CleansingFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyCinder`
  - *实现细节*: `HolyCinder.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `StrongProfanedCrack`
  - *实现细节*: `StrongProfanedCrack.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CrystalTelegraphLine`
  - *实现细节*: `CrystalTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FallingCrystalShard`
  - *实现细节*: `FallingCrystalShard.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DyingSun`
  - *实现细节*: `DyingSun.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolyBasicFireball`
  - *实现细节*: `HolyBasicFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AcceleratingCrystalShard`
  - *实现细节*: `AcceleratingCrystalShard.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CommanderSpear2`
  - *实现细节*: `CommanderSpear2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ProfanedLavaBlob`
  - *实现细节*: `ProfanedLavaBlob.cs` (常规渲染)
- **弹幕类名/类型**: `HolySunExplosion`
  - *实现细节*: `HolySunExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ProvidenceArenaBorder`
  - *实现细节*: `ProvidenceArenaBorder.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyBomb`
  - *实现细节*: `HolyBomb.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ProfanedLava`
  - *实现细节*: `ProfanedLava.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolyRitual`
  - *实现细节*: `HolyRitual.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in ProvidenceAttackerGuardianBehaviorOverride.cs
- Custom rendering found in HolyBomb.cs
- 着色器引用: `Shader/Overlay reference in ProvidenceBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.45f);`
- Custom rendering found in FallingCrystalShard.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["pillarVarient"].SetValue(true);`
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.5f * opacityScalar);`
- Custom rendering found in CommanderSpear2.cs
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(strength`
- 着色器引用: `Effect fireballShader = InfernumEffectsRegistry.FireballShader.GetShader().Shader;`
- 着色器引用: `TelegraphDrawer ??= new PrimitiveTrailCopy(TelegraphWidthFunction, TelegraphColorFunction, null, true, InfernumEffectsRe`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);`
- 着色器引用: `Shader/Overlay reference in ProvidenceArenaBorder.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakMagma);`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(Color.LightGoldenrodYellow);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- 着色器引用: `Shader/Overlay reference in AcceleratingMagicProfanedRock.cs`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.LavaNoise);`
- Custom rendering found in ProvidenceBehaviorOverride.cs
- Custom rendering found in AcceleratingMagicProfanedRock.cs
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- Custom rendering found in ProfanedLava.cs
- Custom rendering found in CleansingFireball.cs
- Custom rendering found in HolySpear.cs
- Custom rendering found in HolyCrystalSpike.cs
- Custom rendering found in ProfanedRocksBehaviorOverride.cs
- Custom rendering found in ProvidenceHealerGuardianBehaviorOverride.cs
- Custom rendering found in HolySpearFirePillar.cs
- Custom rendering found in HolyMagicLaserbeam.cs
- 着色器引用: `Shader/Overlay reference in HolyMagicLaserbeam.cs`
- Custom rendering found in HolyRitual.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["flipY"].SetValue(false);`
- Custom rendering found in HolySunExplosion.cs
- 着色器引用: `Shader/Overlay reference in ProfanedLava.cs`
- Custom rendering found in AcceleratingCrystalShard.cs
- Custom rendering found in HolyCross.cs
- Custom rendering found in ProvSummonFlameExplosion.cs
- 着色器引用: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- 着色器引用: `LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVe`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["stretchAmount"].SetValue(1.3f + StretchOffset * le`
- Custom rendering found in CrystalTelegraphLine.cs
- Custom rendering found in StrongProfanedCrack.cs
- 着色器引用: `InfernumEffectsRegistry.ProfanedLavaVertexShader.SetShaderTexture(InfernumTextureRegistry.Smudges);`
- Custom rendering found in HolyBasicFireball.cs
- 着色器引用: `Shader/Overlay reference in HolyBomb.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Wheat);`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.CultistRayMap);`
- 着色器引用: `Shader/Overlay reference in HolySpearFirePillar.cs`
- 着色器引用: `InfernumEffectsRegistry.ProfanedLavaVertexShader.Shader.Parameters["lavaHeightInterpolant"].SetValue(LavaHeight / 1400f)`
- 着色器引用: `Shader/Overlay reference in HolySunExplosion.cs`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, InfernumEffectsRegistry.FireVertex`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["scrollSpeed"].SetValue(1.75f);`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in ProvidenceArenaBorder.cs
- 着色器引用: `LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.Profa`
- Custom rendering found in DyingSun.cs
- 着色器引用: `Shader/Overlay reference in DyingSun.cs`
- Custom rendering found in HolyCinder.cs
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in HolySpear.cs
- Screen shake/effects found in CommanderSpear2.cs
- Screen shake/effects found in ProvBoomDeath.cs
- Screen shake/effects found in HolyCrystalSpike.cs
- Screen shake/effects found in ProvidenceBehaviorOverride.cs
- Screen shake/effects found in HolyBomb.cs
- Screen shake/effects found in ProvidenceAttackerGuardianBehaviorOverride.cs
- Screen shake/effects found in ProvidenceWave.cs
- Screen shake/effects found in ProviBurnPulseRing.cs