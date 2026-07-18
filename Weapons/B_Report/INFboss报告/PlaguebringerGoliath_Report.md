# 瘟疫使者歌利亚 (Plaguebringer Goliath) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `PlaguebringerGoliath`
- **重写的NPC目标**: `ModContent.NPCType<PlaguebringerBoss>()`
- **关联源文件**:
  - `BombingTelegraph.cs`
  - `BuilderDroneBig.cs`
  - `BuilderDroneSmall.cs`
  - `ExplosivePlagueCharger.cs`
  - `HostilePlagueSeeker.cs`
  - `LargePlagueExplosion.cs`
  - `PlaguebringerGoliathBehaviorOverride.cs`
  - `PlagueCloud.cs`
  - `PlagueDeathray.cs`
  - `PlagueMissile.cs`
  - `PlagueMissile2.cs`
  - `PlagueNuclearExplosion.cs`
  - `PlagueNuke.cs`
  - `PlagueVomit.cs`
  - `PlagueWave.cs`
  - `RedirectingPlagueMissile.cs`
  - `SmallDrone.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.3f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `PBGAttackType`
- `Charge`
- `MissileLaunch`
- `PlagueVomit`
- `CarpetBombing`
- `ExplodingPlagueChargers`
- `DroneSummoning`
- `CarpetBombing2`
- `CarpetBombing3`
- `BombConstructors`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Do the charge.*
- **源码注释**: *Charge behavior.*
- **源码注释**: *Make the attack go by way quicker once in position.*
- **源码注释**: *Slow down and summon a bunch of explosive plague chargers.*
- **源码注释**: *Summon drones once ready.*
- **源码注释**: *Do more contact damage than usual.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `LargePlagueExplosion`
  - *实现细节*: `LargePlagueExplosion.cs` (常规渲染)
- **弹幕类名/类型**: `PlagueCloud`
  - *实现细节*: `PlagueCloud.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PlagueMissile2`
  - *实现细节*: `PlagueMissile2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PlagueNuclearExplosion`
  - *实现细节*: `PlagueNuclearExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HostilePlagueSeeker`
  - *实现细节*: `HostilePlagueSeeker.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BombingTelegraph`
  - *实现细节*: `BombingTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `RedirectingPlagueMissile`
  - *实现细节*: `RedirectingPlagueMissile.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PlagueMissile`
  - *实现细节*: `PlagueMissile.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PlagueVomit`
  - *实现细节*: `PlagueVomit.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in PlagueMissile2.cs
- Custom rendering found in PlagueCloud.cs
- Custom rendering found in HostilePlagueSeeker.cs
- Custom rendering found in PlagueDeathray.cs
- Custom rendering found in BombingTelegraph.cs
- Custom rendering found in PlagueMissile.cs
- 着色器引用: `Shader/Overlay reference in PlagueDeathray.cs`
- Custom rendering found in RedirectingPlagueMissile.cs
- Custom rendering found in PlaguebringerGoliathBehaviorOverride.cs
- Custom rendering found in PlagueNuke.cs
- Custom rendering found in PlagueVomit.cs
- Custom rendering found in PlagueNuclearExplosion.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in PlagueWave.cs
- Screen shake/effects found in PlagueNuke.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item11, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.PBGMissileLaunchSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item45, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.PBGMechanicalWarning, target.Center);`