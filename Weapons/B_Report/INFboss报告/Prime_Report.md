# 机械骷髅王 (Skeletron Prime) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Prime`
- **重写的NPC目标**: `NPCID.SkeletronPrime`, `Unknown`
- **关联源文件**:
  - `EvenlySpreadPrimeLaserRay.cs`
  - `LightningStrike.cs`
  - `MetallicSpike.cs`
  - `PrimeCannonBehaviorOverride.cs`
  - `PrimeHandBehaviorOverride.cs`
  - `PrimeHeadBehaviorOverride.cs`
  - `PrimeLaserBehaviorOverride.cs`
  - `PrimeMissile.cs`
  - `PrimeSawBehaviorOverride.cs`
  - `PrimeShield.cs`
  - `PrimeSmallLaser.cs`
  - `PrimeViceBehaviorOverride.cs`
  - `SawSpark.cs`
  - `SmallElectricGasGloud.cs`
  - `TeslaBomb.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.5f`
- `ForcedLaserRayLifeRatio: 0.2f`
- `Phase2LifeRatio: 0.4f`
- `Phase Ratio Array: Phase2LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `PrimeAttackType`
- `SpawnEffects`
- `GenericCannonAttacking`
- `SynchronizedMeleeArmCharges`
- `SlowSparkShrapnelMeleeCharges`
- `MetalBurst`
- `RocketRelease`
- `HoverCharge`
- `LightningSupercharge`
- `ReleaseTeslaMines`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `SmallElectricGasGloud`
  - *实现细节*: `SmallElectricGasGloud.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PrimeMissile`
  - *实现细节*: `PrimeMissile.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SawSpark`
  - *实现细节*: `SawSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MetallicSpike`
  - *实现细节*: `MetallicSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PrimeSmallLaser`
  - *实现细节*: `PrimeSmallLaser.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PrimeShield`
  - *实现细节*: `PrimeShield.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TeslaBomb`
  - *实现细节*: `TeslaBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);`
- Custom rendering found in PrimeHeadBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(ColorFunction(0.1f));`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(false);`
- 着色器引用: `Shader/Overlay reference in PrimeMissile.cs`
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(ColorFunction(0.1f));`
- 着色器引用: `Shader/Overlay reference in EvenlySpreadPrimeLaserRay.cs`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(ColorFunction(0.5f), Color.White, 0.5f));`
- Custom rendering found in EvenlySpreadPrimeLaserRay.cs
- Custom rendering found in PrimeSmallLaser.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1.5f);`
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(ColorFunction(0.5f), Color.White, 0.2f));`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigInner);`
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigBackground);`
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(3);`
- Custom rendering found in SmallElectricGasGloud.cs
- Custom rendering found in TeslaBomb.cs
- Custom rendering found in PrimeShield.cs
- Custom rendering found in MetallicSpike.cs
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigBackground);`
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVe`
- Custom rendering found in SawSpark.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1);`
- Custom rendering found in PrimeHandBehaviorOverride.cs
- Custom rendering found in PrimeMissile.cs
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigInner);`
- 着色器引用: `//InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);`

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `﻿using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `public static SoundStyle MissileShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/Apollo`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(MissileShootSound with { Volume = 1.4f }, npc.Center);`