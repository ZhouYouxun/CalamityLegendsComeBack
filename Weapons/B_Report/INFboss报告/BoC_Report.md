# 克苏鲁之脑 (Brain of Cthulhu) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `BoC`
- **重写的NPC目标**: `NPCID.BrainofCthulhu`, `NPCID.Creeper`
- **关联源文件**:
  - `BloodGeyser2.cs`
  - `BoCBehaviorOverride.cs`
  - `BrainIllusion.cs`
  - `BrainIllusion2.cs`
  - `CreeperBehaviorOverride.cs`
  - `IchorSpit.cs`
  - `PsionicLightningBolt.cs`
  - `PsionicOrb.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.45f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `BoCAttackState`
- `IdlyFloat`
- `DiagonalCharge`
- `BloodDashSwoop`
- `CreeperBloodDripping`
- `DashingIllusions`
- `PsionicBombardment`
- `SpinPull`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Summon creepers.*
- **源码注释**: *Make the attack go much faster though to prevent annoying telefragging.*
- **源码注释**: *Creepers do most of the interesting stuff with this attack.*
- **源码注释**: *Fade out and teleport after a bit.*
- **源码注释**: *Teleport when completely transparent.*
- **源码注释**: *Fade back in after teleporting.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `IchorSpit`
  - *实现细节*: `IchorSpit.cs` (常规渲染)
- **弹幕类名/类型**: `BloodGeyser2`
  - *实现细节*: `BloodGeyser2.cs` (常规渲染)
- **弹幕类名/类型**: `PsionicLightningBolt`
  - *实现细节*: `PsionicLightningBolt.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `PsionicOrb`
  - *实现细节*: `PsionicOrb.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in PsionicOrb.cs`
- 着色器引用: `Shader/Overlay reference in PsionicLightningBolt.cs`
- 着色器引用: `InfernumEffectsRegistry.AresLightningVertexShader.Apply();`
- Custom rendering found in CreeperBehaviorOverride.cs
- Custom rendering found in PsionicLightningBolt.cs
- Custom rendering found in BoCBehaviorOverride.cs
- 着色器引用: `OrbDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.BrainPsychicVerte`
- Custom rendering found in PsionicOrb.cs
- 着色器引用: `LightningDrawer ??= new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, false, InfernumEffectsR`
- 着色器引用: `InfernumEffectsRegistry.AresLightningVertexShader.UseImage1("Images/Misc/Perlin");`

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item92, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.ForceRoarPitched, target.Center);`