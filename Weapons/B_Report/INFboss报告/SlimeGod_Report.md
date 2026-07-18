# 史莱姆之神 (Slime God) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `SlimeGod`
- **重写的NPC目标**: `ModContent.NPCType<CrimulanPaladin>()`, `ModContent.NPCType<EbonianPaladin>()`, `ModContent.NPCType<SlimeGodCore>()`
- **关联源文件**:
  - `BigSlimeGodAttacks.cs`
  - `CrimulanSlimeGodBehaviorOverride.cs`
  - `DeceleratingCrimulanGlob.cs`
  - `DeceleratingEbonianGlob.cs`
  - `EbonianSlimeGodBehaviorOverride.cs`
  - `GroundSlimeGlob.cs`
  - `SlimeGodComboAttackManager.cs`
  - `SlimeGodCoreBehaviorOverride.cs`
  - `SplitBigSlime.cs`
  - `SplitBigSlimeAnimation.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: SlimeGodComboAttackManager.SummonSecondSlimeLifeRatio`
- `SummonSecondSlimeLifeRatio: 0.6f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `CrimulanPaladinAttackType`
- `LongLeaps`
- `SplitSwarm`
- `PowerfulSlam`
### 状态机/枚举: `EbonianPaladinAttackType`
- `LongLeaps`
- `SplitSwarm`
- `PowerfulSlam`
### 状态机/枚举: `BigSlimeGodAttackType`
- `LongJumps`
- `GroundedGelSlam`
- `CoreSpinBursts`
### 状态机/枚举: `SlimeGodComboAttackType`
- `MutualStomps`
- `TeleportAndFireBlobs`
- `SplitFormCharges`
### 状态机/枚举: `SlimeGodCoreAttackType`
- `HoverAndDoNothing`
- `DoAbsolutelyNothing`
- `PhaseTransitionAnimation`
- `SpinBursts`
- `HorizontalCharges`
- `VerticalHoverBursts`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Summon the second slime.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `GroundSlimeGlob`
  - *实现细节*: `GroundSlimeGlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DeceleratingCrimulanGlob`
  - *实现细节*: `DeceleratingCrimulanGlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DeceleratingEbonianGlob`
  - *实现细节*: `DeceleratingEbonianGlob.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in CrimulanSlimeGodBehaviorOverride.cs
- Custom rendering found in EbonianSlimeGodBehaviorOverride.cs
- Custom rendering found in DeceleratingEbonianGlob.cs
- Custom rendering found in SlimeGodCoreBehaviorOverride.cs
- Custom rendering found in GroundSlimeGlob.cs
- Custom rendering found in DeceleratingCrimulanGlob.cs

### 屏幕特效 (Screen Effects):
- 常规屏幕震动或无显著震屏行为。