# 极地之灵 (Cryogen) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Cryogen`
- **重写的NPC目标**: `ModContent.NPCType<CryogenBoss>()`
- **关联源文件**:
  - `AimedIcicleSpike.cs`
  - `AuroraSpirit.cs`
  - `AuroraSpirit2.cs`
  - `CryogenBehaviorOverride.cs`
  - `IceBomb2.cs`
  - `IcePillar.cs`
  - `IceRain2.cs`
  - `IcicleSpike.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase5LifeRatio: 0.4f`
- `Phase6LifeRatio: 0.25f`
- `Phase4LifeRatio: 0.55f`
- `Phase3LifeRatio: 0.7f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio, Phase5LifeRatio, Phase6LifeRatio`
- `Phase2LifeRatio: 0.9f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `CryogenAttackState`
- `IcicleCircleBurst`
- `PredictiveIcicles`
- `TeleportAndReleaseIceBombs`
- `ShatteringIcePillars`
- `IcicleTeleportDashes`
- `HorizontalDash`
- `AuroraBulletHell`
- `EternalWinter`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Why does this boss have so many subphases anyway?*
- **源码注释**: *Handle subphase transitions.*
- **源码注释**: *Reset damage every frame.*
- **源码注释**: *Determine the attack power and cycle pattern to use based on the current subphase.*
- **源码注释**: *Stop the multiplayer client getting stuck in an inf while loop and crashing.*
- **源码注释**: *Create a seconnd set of rings in later phases.*
- **源码注释**: *Decide a teleport postion and emit teleport particles there.*
- **源码注释**: *Do the teleport.*
- **源码注释**: *Reset opacity and teleport after the delay is finished.*
- **源码注释**: *Line up for the charge.*
- **源码注释**: *Fly towards the destination beside the player.*
- **源码注释**: *If within a good approximation of the player's position, prepare charging.*
- **源码注释**: *Prepare for the charge.*
- **源码注释**: *Play a charge sound.*
- **源码注释**: *Do the actual charge.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `AuroraSpirit2`
  - *实现细节*: `AuroraSpirit2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `IcicleSpike`
  - *实现细节*: `IcicleSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AuroraSpirit`
  - *实现细节*: `AuroraSpirit.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AimedIcicleSpike`
  - *实现细节*: `AimedIcicleSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `IceRain2`
  - *实现细节*: `IceRain2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `IceBomb2`
  - *实现细节*: `IceBomb2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `IcePillar`
  - *实现细节*: `IcePillar.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in IcicleSpike.cs
- Custom rendering found in AuroraSpirit.cs
- Custom rendering found in IcePillar.cs
- Custom rendering found in AuroraSpirit2.cs
- Custom rendering found in CryogenBehaviorOverride.cs
- Custom rendering found in IceRain2.cs
- Custom rendering found in IceBomb2.cs
- Custom rendering found in AimedIcicleSpike.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CryogenBoss.TransitionSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CryogenBoss.ShieldRegenSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item28, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item8, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item72, npc.Center);`