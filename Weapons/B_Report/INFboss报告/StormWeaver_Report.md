# 风暴编织者 (Storm Weaver) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `StormWeaver`
- **重写的NPC目标**: `ModContent.NPCType<StormWeaverBody>()`, `ModContent.NPCType<StormWeaverHead>()`
- **关联源文件**:
  - `HomingWeaverSpark.cs`
  - `StormWeaverHeadBehaviorOverride.cs`
  - `StormWeaverSceneInfernum.cs`
  - `StormWeaverSegmentBehaviorOverride.cs`
  - `WeaverSpark.cs`
  - `WindGust.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.5f`
- `Phase Ratio Array: Phase2LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `StormWeaverAttackType`
- `HuntSkyCreatures`
- `NormalMove`
- `SparkBurst`
- `IceStorm`
- `FakeoutCharge`
- `FogSneakAttackCharges`
- `AimedLightningBolts`
- `BerdlyWindGusts`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Update the hit and death sounds to account for the fact that there is no more phase 1.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Make a strong lightning effect on the first frame and teleport near the player, right before the storm rolls in.*
- **源码注释**: *Bring all segments to the weaver's position for the teleport.*
- **源码注释**: *It will have an incredibly high wind speed.*
- **源码注释**: *Do a tremendous amount of damage to the target if touching their hitbox.*
- **源码注释**: *Play a sound on the player getting frost waves rained on them, as a telegraph.*
- **源码注释**: *Move around the player at distance.*
- **源码注释**: *Do the charge teleport.*
- **源码注释**: *Handle post charge behaviors.*
- **源码注释**: *Have the weaver orient itself near the player at first, and become wreathed in lightning.*
- **源码注释**: *Slow down and arc around towards the player while the segments emit electricity in anticipation of the attack.*
- **源码注释**: *Change the wind speeds periodically.*
- **源码注释**: *Spin around the player while releasing wind gusts.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `WindGust`
  - *实现细节*: `WindGust.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `WeaverSpark`
  - *实现细节*: `WeaverSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HomingWeaverSpark`
  - *实现细节*: `HomingWeaverSpark.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in WindGust.cs
- Custom rendering found in WeaverSpark.cs
- 着色器引用: `Shader/Overlay reference in HomingWeaverSpark.cs`
- Custom rendering found in HomingWeaverSpark.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.HitSound = SoundID.NPCHit13;`
- 屏幕震动/音效触发: `npc.DeathSound = SoundID.NPCDeath13;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound with`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.StormWeaverElectricDischargeSound with`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CommonCalamitySounds.SwiftSliceSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item94, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item120, target.Center);`