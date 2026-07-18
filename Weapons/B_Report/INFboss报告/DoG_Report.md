# 神明吞噬者 (Devourer of Gods) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `DoG`
- **重写的NPC目标**: `ModContent.NPCType<DevourerofGodsBody>()`, `ModContent.NPCType<DoGHead>()`
- **关联源文件**:
  - `AcceleratingDoGBurst.cs`
  - `DoGChargeGate.cs`
  - `DoGDeathInfernum.cs`
  - `DoGMusicSceneInfernum.cs`
  - `DoGPhase1HeadBehaviorOverride.cs`
  - `DoGPhase2HeadBehaviorOverride.cs`
  - `DoGPhase2IntroPortalGate.cs`
  - `DoGSegmentBehaviorOverride.cs`
  - `DoGSpawnBoom.cs`
  - `RealityBreakPortalLaserWall.cs`
  - `RoDFailPulse.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `FinalPhaseLifeRatio: 0.2f`
- `CanUseSignusSentinelAttackLifeRatio: 0.7f`
- `CanUseSpecialAttacksLifeRatio: 0.8f`
- `Phase Ratio Array: Phase2LifeRatio, DoGPhase2HeadBehaviorOverride.FinalPhaseLifeRatio`
- `Phase2LifeRatio: 0.8f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `SpecialAttackType`
- `LaserWalls`
- `CircularLaserBurst`
- `ChargeGates`
### 状态机/枚举: `PerpendicularPortalAttackState`
- `NotPerformingAttack`
- `EnteringPortal`
- `Waiting`
- `AttackEndDelay`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Make DoG enter the second phase once ready.*
- **源码注释**: *HandleDoGLifeBasedHitTriggers will never be run serverside, thus ensuring DoG will never properly change phase. This sends a packet to the server to run the intended health phase change calculations.*
- **源码注释**: *Disable damage and start the death animation if the hit would kill DoG.*
- **源码注释**: *Disable damage and enter phase 2 if the hit would bring DoG down to a sufficiently low quantity of HP.*
- **源码注释**: *Defer all further execution to the second phase AI manager if in the second phase.*
- **源码注释**: *Do through the portal once ready to enter the second phase.*
- **源码注释**: *Teleport to the sides of the target on the very first frame. This ensures that DoG will always be in a consistent spot before the fight begins.*
- **源码注释**: *Bring segments to the teleport position.*
- **源码注释**: *Chomping after attempting to eat the player.*
- **源码注释**: *Summon the portal and become fully opaque if the portal hasn't been created yet.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `RealityBreakPortalLaserWall`
  - *实现细节*: `RealityBreakPortalLaserWall.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DoGPhase2IntroPortalGate`
  - *实现细节*: `DoGPhase2IntroPortalGate.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `DoGDeathInfernum`
  - *实现细节*: `DoGDeathInfernum.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DoGChargeGate`
  - *实现细节*: `DoGChargeGate.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AcceleratingDoGBurst`
  - *实现细节*: `AcceleratingDoGBurst.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 拥有专属的Boss登场展示界面 (Custom Boss Intro Screen)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in DoGPhase2IntroPortalGate.cs`
- Custom rendering found in RealityBreakPortalLaserWall.cs
- Custom rendering found in DoGPhase1HeadBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in DoGPhase2HeadBehaviorOverride.cs`
- Custom rendering found in AcceleratingDoGBurst.cs
- Custom rendering found in DoGChargeGate.cs
- Custom rendering found in DoGPhase2HeadBehaviorOverride.cs
- Custom rendering found in DoGSegmentBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in DoGChargeGate.cs`
- Custom rendering found in DoGPhase2IntroPortalGate.cs
- Custom rendering found in DoGDeathInfernum.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in DoGPhase2IntroPortalGate.cs
- Screen shake/effects found in RoDFailPulse.cs
- Screen shake/effects found in DoGPhase2HeadBehaviorOverride.cs
- Screen shake/effects found in DoGSpawnBoom.cs
- 屏幕震动/音效触发: `SoundEngine.PlaySound(DoGHead.SpawnSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item12, target.position);`