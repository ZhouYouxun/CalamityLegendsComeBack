# 克苏鲁之眼 (Eye of Cthulhu) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `EyeOfCthulhu`
- **重写的NPC目标**: `NPCID.EyeofCthulhu`
- **关联源文件**:
  - `BloodShot.cs`
  - `EoCTooth.cs`
  - `EoCTooth2.cs`
  - `ExplodingServant.cs`
  - `EyeOfCthulhuBehaviorOverride.cs`
  - `SittingBlood.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase4LifeRatio: 0.15f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase3LifeRatio: 0.35f`
- `Phase2LifeRatio: 0.8f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `EoCAttackType`
- `HoverCharge`
- `ChargingServants`
- `HorizontalBloodCharge`
- `TeethSpit`
- `SpinDash`
- `BloodShots`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Reset damage and defense.*
- **源码注释**: *Handle the Phase 2 transition.*
- **源码注释**: *Ensure that the gleam doesn't linger in multiplayer due to desyncs.*
- **源码注释**: *Don't do damage while redirecting.*
- **源码注释**: *Create blood particles in the third phase onward.*
- **源码注释**: *Do a chain of multiple charges that become slower and slower.*
- **源码注释**: *Create blood particles in the third phase onward. This happens on a timer to prevent particle clutter.*
- **源码注释**: *Attempt to get close to the player.*
- **源码注释**: *Charge up.*
- **源码注释**: *Reset the attack timer.*
- **源码注释**: *The quantity of afterimages is changed by attacks on a case-by-case basis.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `EoCTooth2`
  - *实现细节*: `EoCTooth2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SittingBlood`
  - *实现细节*: `SittingBlood.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BloodShot`
  - *实现细节*: `BloodShot.cs` (常规渲染)
- **弹幕类名/类型**: `EoCTooth`
  - *实现细节*: `EoCTooth.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in EyeOfCthulhuBehaviorOverride.cs
- Custom rendering found in SittingBlood.cs
- Custom rendering found in EoCTooth.cs
- Custom rendering found in EoCTooth2.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);`