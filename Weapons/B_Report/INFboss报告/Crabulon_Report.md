# 菌生蟹 (Crabulon) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Crabulon`
- **重写的NPC目标**: `ModContent.NPCType<CrabShroom>()`, `ModContent.NPCType<CrabulonNPC>()`
- **关联源文件**:
  - `CrabShroomBehaviorOverride.cs`
  - `CrabulonBehaviorOverride.cs`
  - `HomingSpore.cs`
  - `MushBombInfernum.cs`
  - `MushroomPillar.cs`
  - `SporeCloud.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.85f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.45f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `CrabulonAttackState`
- `SpawnWait`
- `JumpToTarget`
- `WalkToTarget`
- `CreateGroundMushrooms`
- `ClawSlamMushroomWaves`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `MushBombInfernum`
  - *实现细节*: `MushBombInfernum.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MushroomPillar`
  - *实现细节*: `MushroomPillar.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HomingSpore`
  - *实现细节*: `HomingSpore.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SporeCloud`
  - *实现细节*: `SporeCloud.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(ClawArmWidthFunction, c => ClawArmColorFunction(npc, c`
- Custom rendering found in HomingSpore.cs
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.UseSecondaryColor(new Color(113, 255, 233));`
- Custom rendering found in SporeCloud.cs
- 着色器引用: `Shader/Overlay reference in CrabulonBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.UseColor(new Color(70, 90, 166));`
- Custom rendering found in MushroomPillar.cs
- Custom rendering found in CrabulonBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`
- Custom rendering found in MushBombInfernum.cs

### 屏幕特效 (Screen Effects):
- 常规屏幕震动或无显著震屏行为。