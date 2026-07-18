# 恐惧金螯 (Dreadnautilus) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Dreadnautilus`
- **重写的NPC目标**: `NPCID.BloodNautilus`
- **关联源文件**:
  - `BloodBolt.cs`
  - `BloodShot2.cs`
  - `DreadnautilusBehaviorOverride.cs`
  - `GoreSpike.cs`
  - `GoreSpitBall.cs`
  - `SanguineBat.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.55f`
- `Phase3LifeRatio: 0.25f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DreadnautilusAttackState`
- `InitialSummonDelay`
- `BloodSpitToothBalls`
- `EyeGleamEyeFishSummon`
- `UpwardPerpendicularBoltCharge`
- `EquallySpreadBloodBolts`
- `HorizontalCharge`
- `SanguineBatSwarm`
- `SquidGames`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Fade away and despawn if the player dies.*
- **源码注释**: *Transition to the first attack after a short period of time has passed.*
- **源码注释**: *Increase the intensity of the gleam and summon wandering eye fish from the sky.*
- **源码注释**: *Stop the attack early if hit in time.*
- **源码注释**: *Summon wandering eye fish.*
- **源码注释**: *Make the gleam fade away again and eventually transition to the next attack.*
- **源码注释**: *Create a weird sound as the attack starts.*
- **源码注释**: *Immediately transition to the charge state if the minimum hover time has elapsed and sufficiently within range for the upward charge.*
- **源码注释**: *Charge upward. Also release a spread of bolts in the third phase.*
- **源码注释**: *Initialize the charge direction.*
- **源码注释**: *Slow down drastically prior to charge and release an arc of homing spikes away from the target.*
- **源码注释**: *Do damage and become temporarily invulnerable. This is done to prevent dash-cheese.*
- **源码注释**: *Summon bats in the sky.*
- **源码注释**: *Hover near the target after summoning the bats.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `BloodShot2`
  - *实现细节*: `BloodShot2.cs` (常规渲染)
- **弹幕类名/类型**: `SanguineBat`
  - *实现细节*: `SanguineBat.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GoreSpitBall`
  - *实现细节*: `GoreSpitBall.cs` (常规渲染)
- **弹幕类名/类型**: `GoreSpike`
  - *实现细节*: `GoreSpike.cs` (常规渲染)
- **弹幕类名/类型**: `BloodBolt`
  - *实现细节*: `BloodBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in DreadnautilusBehaviorOverride.cs
- Custom rendering found in BloodBolt.cs
- Custom rendering found in SanguineBat.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `npc.HitSound = SoundID.NPCHit1;`
- 屏幕震动/音效触发: `npc.DeathSound = SoundID.NPCDeath1;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item17, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.NPCHit35 with { Volume = 1.75f, Pitch = -0.85f }, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.NPCHit18, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item122, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item170, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Zombie63, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item171, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);`