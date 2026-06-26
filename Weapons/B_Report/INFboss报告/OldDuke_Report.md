# 硫海遗爵 (Old Duke) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `OldDuke`
- **重写的NPC目标**: `ModContent.NPCType<OldDukeToothBall>()`, `ModContent.NPCType<OldDukeBoss>()`
- **关联源文件**:
  - `HomingAcid.cs`
  - `OldDukeBehaviorOverride.cs`
  - `OldDukeTooth.cs`
  - `OldDukeToothBallBehaviorOverride.cs`
  - `SharkSummonVortex.cs`
  - `SulphuricBlob.cs`
  - `ToothBallSpikeBehaviorOverride.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase4LifeRatio: 0.2f`
- `Phase3LifeRatio: 0.375f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `OldDukeAttackState`
- `SpawnAnimation`
- `AttackSelectionWait`
- `ChargeIndicatorSound`
- `Charge`
- `AcidBelch`
- `SharkronSpinSummon`
- `ToothBallVomit`
- `GoreAndAcidSpit`
- `FastRegularCharge`
- `TeleportPause`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Enter new phases.*
- **源码注释**: *Handle phase transitions.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Roar and summon sharks below the boss.*
- **源码注释**: *Speed up the farther away the target is.*
- **源码注释**: *Do the charge on the first frame.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Prepare a charge to reset speed.*
- **源码注释**: *Teleport.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `SharkSummonVortex`
  - *实现细节*: `SharkSummonVortex.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SulphuricBlob`
  - *实现细节*: `SulphuricBlob.cs` (常规渲染)
- **弹幕类名/类型**: `OldDukeTooth`
  - *实现细节*: `OldDukeTooth.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HomingAcid`
  - *实现细节*: `HomingAcid.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in HomingAcid.cs
- Custom rendering found in OldDukeBehaviorOverride.cs
- Custom rendering found in OldDukeTooth.cs
- Custom rendering found in OldDukeToothBallBehaviorOverride.cs
- Custom rendering found in SharkSummonVortex.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `ChargeIndicatorSound,`
- 屏幕震动/音效触发: `OldDukeAttackState.ChargeIndicatorSound,`
- 屏幕震动/音效触发: `case OldDukeAttackState.ChargeIndicatorSound:`
- 屏幕震动/音效触发: `DoBehavior_ChargeIndicatorSound(npc, target, attackTimer);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(OldDukeBoss.RoarSound, Main.player[npc.target].Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(OldDukeBoss.VomitSound, npc.Center);`
- 屏幕震动/音效触发: `public static void DoBehavior_ChargeIndicatorSound(NPC npc, Player target, float attackTimer)`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(OldDukeBoss.VomitSound with { Volume = 1.5f, Pitch = -0.225f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(OldDukeBoss.RoarSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(OldDukeBoss.RoarSound, target.Center);`