# 蜂王 (Queen Bee) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `QueenBee`
- **重写的NPC目标**: `NPCID.QueenBee`
- **关联源文件**:
  - `BeeWave.cs`
  - `ConvergingHornet.cs`
  - `HoneyBlast.cs`
  - `HornetHive.cs`
  - `QueenBeeBehaviorOverride.cs`
  - `TinyBee.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `FinalPhaseLifeRatio: 0.225f`
- `Phase Ratio Array: FinalPhaseLifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `HornetAttackState`
- `MoveTowardsQueen`
- `HoverAroundQueen`
- `FlyOutward`
### 状态机/枚举: `QueenBeeAttackState`
- `HorizontalCharge`
- `StingerBurst`
- `HoneyBlast`
- `CreateMinionsFromAbdomen`
- `InwardMovingBees`
- `BeeletHell`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Do the initial stuff before attacking.*
- **源码注释**: *Do the charge.*
- **源码注释**: *Hover to the side of the target before beginning the attack.*
- **源码注释**: *Summon bees that converge inward.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `TinyBee`
  - *实现细节*: `TinyBee.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HornetHive`
  - *实现细节*: `HornetHive.cs` (常规渲染)
- **弹幕类名/类型**: `ConvergingHornet`
  - *实现细节*: `ConvergingHornet.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HoneyBlast`
  - *实现细节*: `HoneyBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in QueenBeeBehaviorOverride.cs
- Custom rendering found in TinyBee.cs
- Custom rendering found in ConvergingHornet.cs
- Custom rendering found in HoneyBlast.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in BeeWave.cs
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item17, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, target.Center);`