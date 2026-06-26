# 血肉宿主 (The Perforators) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Perforators`
- **重写的NPC目标**: `ModContent.NPCType<PerforatorHeadLarge>()`, `ModContent.NPCType<PerforatorHeadMedium>()`, `ModContent.NPCType<PerforatorBodyLarge>()`, `ModContent.NPCType<PerforatorHive>()`, `ModContent.NPCType<PerforatorBodyMedium>()`, `ModContent.NPCType<PerforatorBodySmall>()`, `ModContent.NPCType<PerforatorHeadSmall>()`
- **关联源文件**:
  - `BloodGlob.cs`
  - `Crimera.cs`
  - `FallingIchor.cs`
  - `FallingIchorBlast.cs`
  - `FlyingIchor.cs`
  - `IchorBlast.cs`
  - `IchorBolt.cs`
  - `LargePerforatorBodyBehaviorOverride.cs`
  - `LargePerforatorHeadBehaviorOverride.cs`
  - `MediumPerforatorBodyBehaviorOverride.cs`
  - `MediumPerforatorHeadBehaviorOverride.cs`
  - `PerforatorHiveBehaviorOverride.cs`
  - `PerforatorWave.cs`
  - `SmallPerforatorBodyBehaviorOverride.cs`
  - `SmallPerforatorHeadBehaviorOverride.cs`
  - `ToothBall.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.7f`
- `Phase3LifeRatio: 0.5f`
- `Phase4LifeRatio: 0.25f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `PerforatorHiveAttackState`
- `DiagonalBloodCharge`
- `HorizontalCrimeraSpawnCharge`
- `IchorBlasts`
- `IchorSpinDash`
- `SmallWormBursts`
- `CrimeraWalls`
- `MediumWormBursts`
- `IchorRain`
- `LargeWormBursts`
- `IchorFountainCharge`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `FallingIchorBlast`
  - *实现细节*: `FallingIchorBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `Crimera`
  - *实现细节*: `Crimera.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FallingIchor`
  - *实现细节*: `FallingIchor.cs` (常规渲染)
- **弹幕类名/类型**: `ToothBall`
  - *实现细节*: `ToothBall.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FlyingIchor`
  - *实现细节*: `FlyingIchor.cs` (常规渲染)
- **弹幕类名/类型**: `IchorBolt`
  - *实现细节*: `IchorBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BloodGlob`
  - *实现细节*: `BloodGlob.cs` (常规渲染)
- **弹幕类名/类型**: `IchorBlast`
  - *实现细节*: `IchorBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in FallingIchorBlast.cs
- Custom rendering found in PerforatorHiveBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in PerforatorHiveBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.BasicTintShader.UseColor(Color.Red);`
- 着色器引用: `InfernumEffectsRegistry.BasicTintShader.UseSaturation(opacityInterpolent);`
- Custom rendering found in ToothBall.cs
- 着色器引用: `InfernumEffectsRegistry.BasicTintShader.UseOpacity(lightColor.ToGreyscale());`
- Custom rendering found in Crimera.cs
- Custom rendering found in IchorBolt.cs
- 着色器引用: `InfernumEffectsRegistry.BasicTintShader.Apply();`
- Custom rendering found in IchorBlast.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in SmallPerforatorHeadBehaviorOverride.cs
- Screen shake/effects found in PerforatorHiveBehaviorOverride.cs
- Screen shake/effects found in PerforatorWave.cs