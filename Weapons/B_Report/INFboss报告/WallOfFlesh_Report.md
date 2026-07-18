# 肉山 / 肉之墙 (Wall of Flesh) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `WallOfFlesh`
- **重写的NPC目标**: `NPCID.WallofFleshEye`, `NPCID.WallofFlesh`
- **关联源文件**:
  - `CursedSoul.cs`
  - `FireBeamTelegraph.cs`
  - `FireBeamWoF.cs`
  - `TileTentacle.cs`
  - `WallOfFleshEyeBehaviorOverride.cs`
  - `WallOfFleshMouthBehaviorOverride.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio`
- `Phase2LifeRatio: 0.45f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
- 该Boss AI未使用特定的内部枚举，或者攻击模式直接通过 `npc.ai` 数值控制。

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Attack the target independently after being "killed".*
- **源码注释**: *Fire the laser. This doesn't happen if extremely close to players, to prevent cheap hits.*
- **源码注释**: *Do direct damage to the wall and have the eye "pop" out, as though it's detatching.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `FireBeamWoF`
  - *实现细节*: `FireBeamWoF.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CursedSoul`
  - *实现细节*: `CursedSoul.cs` (常规渲染)
- **弹幕类名/类型**: `FireBeamTelegraph`
  - *实现细节*: `FireBeamTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TileTentacle`
  - *实现细节*: `TileTentacle.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in TileTentacle.cs`
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.UseSecondaryColor(new Color(184, 78, 113));`
- 着色器引用: `TentacleDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.WoFTentacleV`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFire);`
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.UseColor(new Color(108, 23, 23));`
- Custom rendering found in WallOfFleshMouthBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.WoFTentacleVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`
- Custom rendering found in WallOfFleshEyeBehaviorOverride.cs
- Custom rendering found in FireBeamWoF.cs
- 着色器引用: `Shader/Overlay reference in FireBeamWoF.cs`
- Custom rendering found in TileTentacle.cs
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(Color.OrangeRed);`
- Custom rendering found in FireBeamTelegraph.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVert`

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item12, npc.Center);`