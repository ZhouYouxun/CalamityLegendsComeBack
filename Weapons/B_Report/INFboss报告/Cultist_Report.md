# 拜月教邪教徒 (Lunatic Cultist) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Cultist`
- **重写的NPC目标**: `NPCID.CultistBossClone`, `NPCID.CultistDragonHead`, `NPCID.AncientLight`, `NPCID.CultistBoss`, `NPCID.AncientCultistSquidhead`
- **关联源文件**:
  - `AncientDoom.cs`
  - `AncientLightBehaviorOverride.cs`
  - `AncientVisionBehaviorOverride.cs`
  - `CultistBehaviorOverride.cs`
  - `CultistCloneBehaviorOverride.cs`
  - `CultistFireBeamTelegraph.cs`
  - `CultistRitual.cs`
  - `DarkBolt.cs`
  - `DarkBoltLarge.cs`
  - `DarkPulse.cs`
  - `DeathExplosion.cs`
  - `DoomBeam.cs`
  - `FireballLineTelegraph.cs`
  - `FireBeam.cs`
  - `IceMass.cs`
  - `IceShard.cs`
  - `LightBeam.cs`
  - `LightBurst.cs`
  - `PhantasmDragonHeadBehaviorOverride.cs`
  - `TeleportTelegraph.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio`
- `Phase2LifeRatio: 0.65f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
- 该Boss AI未使用特定的内部枚举，或者攻击模式直接通过 `npc.ai` 数值控制。

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `DarkPulse`
  - *实现细节*: `DarkPulse.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `IceShard`
  - *实现细节*: `IceShard.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkBoltLarge`
  - *实现细节*: `DarkBoltLarge.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DoomBeam`
  - *实现细节*: `DoomBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `IceMass`
  - *实现细节*: `IceMass.cs` (常规渲染)
- **弹幕类名/类型**: `AncientDoom`
  - *实现细节*: `AncientDoom.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LightBeam`
  - *实现细节*: `LightBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `LightBurst`
  - *实现细节*: `LightBurst.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CultistFireBeamTelegraph`
  - *实现细节*: `CultistFireBeamTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CultistRitual`
  - *实现细节*: `CultistRitual.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FireballLineTelegraph`
  - *实现细节*: `FireballLineTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TeleportTelegraph`
  - *实现细节*: `TeleportTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FireBeam`
  - *实现细节*: `FireBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `DarkBolt`
  - *实现细节*: `DarkBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in CultistBehaviorOverride.cs`
- Custom rendering found in DarkPulse.cs
- Custom rendering found in CultistRitual.cs
- Custom rendering found in FireBeam.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(1.4f);`
- Custom rendering found in LightBurst.cs
- 着色器引用: `InfernumEffectsRegistry.CultistDeathVertexShader.Apply();`
- Custom rendering found in FireballLineTelegraph.cs
- Custom rendering found in TeleportTelegraph.cs
- Custom rendering found in DoomBeam.cs
- Custom rendering found in CultistBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.CultistDeathVertexShader.UseOpacity((1f - Utils.GetLerpValue(120f, 305f, deathTimer, true)) * 0.`
- Custom rendering found in AncientDoom.cs
- Custom rendering found in IceMass.cs
- 着色器引用: `Shader/Overlay reference in FireBeam.cs`
- Custom rendering found in CultistFireBeamTelegraph.cs
- Custom rendering found in IceShard.cs
- Custom rendering found in LightBeam.cs
- 着色器引用: `InfernumEffectsRegistry.CultistDeathVertexShader.UseImage1("Images/Misc/Perlin");`
- 着色器引用: `Shader/Overlay reference in DoomBeam.cs`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- Custom rendering found in CultistCloneBehaviorOverride.cs
- 着色器引用: `Effect shield = InfernumEffectsRegistry.CultistShieldShader.Shader;`
- Custom rendering found in DarkBolt.cs
- Custom rendering found in DarkBoltLarge.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in CultistBehaviorOverride.cs
- Screen shake/effects found in DeathExplosion.cs