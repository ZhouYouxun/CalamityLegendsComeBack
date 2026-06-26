# 毁灭者 (The Destroyer) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Destroyer`
- **重写的NPC目标**: `NPCID.Probe`, `NPCID.TheDestroyerTail`, `NPCID.TheDestroyerBody`, `NPCID.TheDestroyer`
- **关联源文件**:
  - `DestroyerBodyBehaviorOverride.cs`
  - `DestroyerBomb.cs`
  - `DestroyerHeadBehaviorOverride.cs`
  - `DestroyerPierceLaser.cs`
  - `DestroyerPierceLaserTelegraph.cs`
  - `DestroyerTailBehaviorOverride.cs`
  - `EnergyBlast2.cs`
  - `EnergySpark.cs`
  - `EnergySpark2.cs`
  - `ProbeBehaviorOverride.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.825f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase4LifeRatio: 0.2f`
- `Phase3LifeRatio: 0.45f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DestroyerAttackType`
- `RegularCharge`
- `UpwardBombLunge`
- `LaserWalls`
- `ProbeBombing`
- `SuperchargedProbeBombing`
- `DiveBombing`
- `EnergyBlasts`
- `LaserSpin`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `EnergySpark2`
  - *实现细节*: `EnergySpark2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DestroyerBomb`
  - *实现细节*: `DestroyerBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `EnergyBlast2`
  - *实现细节*: `EnergyBlast2.cs` (常规渲染)
- **弹幕类名/类型**: `DestroyerPierceLaser`
  - *实现细节*: `DestroyerPierceLaser.cs` (常规渲染)
- **弹幕类名/类型**: `EnergySpark`
  - *实现细节*: `EnergySpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DestroyerPierceLaserTelegraph`
  - *实现细节*: `DestroyerPierceLaserTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in EnergySpark.cs
- Custom rendering found in DestroyerBomb.cs
- Custom rendering found in DestroyerPierceLaser.cs
- Custom rendering found in DestroyerPierceLaserTelegraph.cs
- Custom rendering found in ProbeBehaviorOverride.cs
- Custom rendering found in EnergySpark2.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in DestroyerHeadBehaviorOverride.cs