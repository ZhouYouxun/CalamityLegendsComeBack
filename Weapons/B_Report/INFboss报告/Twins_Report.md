# 双子魔眼 (The Twins) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Twins`
- **重写的NPC目标**: `NPCID.Retinazer`, `NPCID.Spazmatism`
- **关联源文件**:
  - `CursedCinder.cs`
  - `CursedFireballBomb.cs`
  - `CursedFlameBurst.cs`
  - `CursedFlameBurstTelegraph.cs`
  - `HomingCursedFlameBurst.cs`
  - `LaserGroundShock.cs`
  - `LightningTelegraph.cs`
  - `RedLightning.cs`
  - `RetinazerAIClass.cs`
  - `RetinazerAimedDeathray.cs`
  - `RetinazerAimedDeathray2.cs`
  - `RetinazerGroundDeathray.cs`
  - `RetinazerLaser.cs`
  - `SpazmatismAIClass.cs`
  - `SpazmatismFlamethrower.cs`
  - `TwinsAttackSynchronizer.cs`
  - `TwinsEnergyExplosion.cs`
  - `TwinsLensFlare.cs`
  - `TwinsShield.cs`
  - `TwinsSpriteExplosion.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatioThreshold: 0.425f`
- `Phase Ratio Array: Phase2LifeRatioThreshold, Phase3LifeRatioThreshold`
- `Phase2LifeRatioThreshold: 0.75f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `TwinsAttackState`
- `ChargeRedirect`
- `DownwardCharge`
- `SwitchCharges`
- `Spin`
- `FlamethrowerBurst`
- `ChaoticFireAndDownwardLaser`
- `LazilyObserve`
- `DeathAnimation`
### 状态机/枚举: `RetinazerAttackState`
- `SwiftLaserBursts`
- `BigAimedLaserbeam`
- `AgileLaserbeamSweeps`
### 状态机/枚举: `SpazmatismAttackState`
- `MobileChargePhase`
- `HellfireBursts`
- `CursedFlameSpin`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `RedLightning`
  - *实现细节*: `RedLightning.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CursedCinder`
  - *实现细节*: `CursedCinder.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `RetinazerLaser`
  - *实现细节*: `RetinazerLaser.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LaserGroundShock`
  - *实现细节*: `LaserGroundShock.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TwinsLensFlare`
  - *实现细节*: `TwinsLensFlare.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CursedFlameBurst`
  - *实现细节*: `CursedFlameBurst.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CursedFlameBurstTelegraph`
  - *实现细节*: `CursedFlameBurstTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SpazmatismFlamethrower`
  - *实现细节*: `SpazmatismFlamethrower.cs` (常规渲染)
- **弹幕类名/类型**: `HomingCursedFlameBurst`
  - *实现细节*: `HomingCursedFlameBurst.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `TwinsShield`
  - *实现细节*: `TwinsShield.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TwinsEnergyExplosion`
  - *实现细节*: `TwinsEnergyExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CursedFireballBomb`
  - *实现细节*: `CursedFireballBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 拥有专属的Boss登场展示界面 (Custom Boss Intro Screen)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage1("Images/Extra_197");`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(Projectile.velocity.Length() / 13f);`
- Custom rendering found in TwinsShield.cs
- 着色器引用: `Shader/Overlay reference in CursedFlameBurst.cs`
- 着色器引用: `Shader/Overlay reference in RetinazerAimedDeathray2.cs`
- Custom rendering found in RetinazerAimedDeathray2.cs
- 着色器引用: `Shader/Overlay reference in LightningTelegraph.cs`
- Custom rendering found in TwinsLensFlare.cs
- Custom rendering found in HomingCursedFlameBurst.cs
- 着色器引用: `Shader/Overlay reference in HomingCursedFlameBurst.cs`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.4f);`
- Custom rendering found in CursedFireballBomb.cs
- Custom rendering found in LaserGroundShock.cs
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);`
- 着色器引用: `Shader/Overlay reference in RetinazerAimedDeathray.cs`
- Custom rendering found in CursedFlameBurst.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- Custom rendering found in TwinsEnergyExplosion.cs
- Custom rendering found in SpazmatismAIClass.cs
- Custom rendering found in RetinazerAIClass.cs
- Custom rendering found in RetinazerLaser.cs
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- 着色器引用: `Shader/Overlay reference in TwinsEnergyExplosion.cs`
- 着色器引用: `Shader/Overlay reference in RetinazerGroundDeathray.cs`
- 着色器引用: `Shader/Overlay reference in SpazmatismAIClass.cs`
- 着色器引用: `Shader/Overlay reference in RetinazerAIClass.cs`
- Custom rendering found in TwinsAttackSynchronizer.cs
- 着色器引用: `InfernumEffectsRegistry.TwinsFlameTrailVertexShader.UseImage1("Images/Misc/Perlin");`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in RetinazerAimedDeathray.cs
- 着色器引用: `null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);`
- Custom rendering found in CursedFlameBurstTelegraph.cs
- Custom rendering found in RedLightning.cs
- Custom rendering found in RetinazerGroundDeathray.cs
- Custom rendering found in CursedCinder.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in TwinsAttackSynchronizer.cs
- Screen shake/effects found in TwinsEnergyExplosion.cs