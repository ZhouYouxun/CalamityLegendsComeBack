# 荒漠灾虫 (Desert Scourge) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `DesertScourge`
- **重写的NPC目标**: `ModContent.NPCType<DesertScourgeTail>()`, `ModContent.NPCType<DesertScourgeBody>()`, `ModContent.NPCType<DesertScourgeHead>()`, `ModContent.NPCType<DesertNuisanceHead>()`
- **关联源文件**:
  - `DesertScourgeBodyBigBehaviorOverride.cs`
  - `DesertScourgeHeadBigBehaviorOverride.cs`
  - `DesertScourgeHeadSmallBehaviorOverride.cs`
  - `DesertScourgeTailBigBehaviorOverride.cs`
  - `SandBlastInfernum.cs`
  - `Sandnado.cs`
  - `SandstormBlast.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.55f`
- `Phase3LifeRatio: 0.25f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DesertScourgeAttackType`
- `SpawnAnimation`
- `SandSpit`
- `SandRushCharge`
- `SandstormParticles`
- `GroundSlam`
- `SummonVultures`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `SandBlastInfernum`
  - *实现细节*: `SandBlastInfernum.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SandstormBlast`
  - *实现细节*: `SandstormBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `Sandnado`
  - *实现细节*: `Sandnado.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in DesertScourgeHeadBigBehaviorOverride.cs
- Custom rendering found in SandstormBlast.cs
- Custom rendering found in Sandnado.cs
- Custom rendering found in SandBlastInfernum.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in DesertScourgeHeadBigBehaviorOverride.cs