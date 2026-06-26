# 猪鲨公爵 (Duke Fishron) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `DukeFishron`
- **重写的NPC目标**: `NPCID.DukeFishron`, `NPCID.Sharkron2`
- **关联源文件**:
  - `ChargeTyphoon.cs`
  - `DukeFishronBehaviorOverride.cs`
  - `RedirectingBubble.cs`
  - `SharkronBehaviorOverride.cs`
  - `SharkSummoner.cs`
  - `SmallWave.cs`
  - `TidalWave.cs`
  - `Tornado.cs`
  - `TyphoonBlade.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatio: 0.4f`
- `Phase2LifeRatio: 0.7f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase4LifeRatio: 0.2f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DukeAttackType`
- `Charge`
- `ChargeWait`
- `BubbleSpit`
- `BubbleSpin`
- `StationaryBubbleCharge`
- `SharkTornadoSummon`
- `TidalWave`
- `ChargeTeleport`
- `RazorbladeRazorstorm`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Phase transitions.*
- **源码注释**: *Start the water background in phase 3.*
- **源码注释**: *Go to the next attack state.*
- **源码注释**: *Reset the attack timer.*
- **源码注释**: *Charge at the target.*
- **源码注释**: *Release typhoons in phase 3.*
- **源码注释**: *Disable contact damage while redirecting.*
- **源码注释**: *Disable contact damage while redirecting, to prevent cheap hits.*
- **源码注释**: *Charge.*
- **源码注释**: *Slow down before the attack ends.*
- **源码注释**: *Summon tornadoes on the ground/water.*
- **源码注释**: *Summon sharks in the ocean.*
- **源码注释**: *Disable contact damage while hovering.*
- **源码注释**: *Teleport to the destination at the end of the attack.*
- **源码注释**: *Summon tornadoes.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `Tornado`
  - *实现细节*: `Tornado.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SmallWave`
  - *实现细节*: `SmallWave.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TyphoonBlade`
  - *实现细节*: `TyphoonBlade.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ChargeTyphoon`
  - *实现细节*: `ChargeTyphoon.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SharkSummoner`
  - *实现细节*: `SharkSummoner.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TidalWave`
  - *实现细节*: `TidalWave.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in Tornado.cs`
- Custom rendering found in TyphoonBlade.cs
- 着色器引用: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(InfernumTextureRegistry.VoronoiShapes);`
- Custom rendering found in SmallWave.cs
- 着色器引用: `Shader/Overlay reference in DukeFishronBehaviorOverride.cs`
- Custom rendering found in SharkSummoner.cs
- 着色器引用: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`
- Custom rendering found in TidalWave.cs
- Custom rendering found in Tornado.cs
- 着色器引用: `npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffe`
- 着色器引用: `TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.Du`
- 着色器引用: `Shader/Overlay reference in TidalWave.cs`
- Custom rendering found in DukeFishronBehaviorOverride.cs
- 着色器引用: `TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVe`
- Custom rendering found in ChargeTyphoon.cs
- 特效代码片段: `using InfernumMode.Common.Graphics.Primitives;`
- 特效代码片段: `using Terraria.GameContent.Shaders;`
- 特效代码片段: `WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffe`
- 特效代码片段: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(InfernumTextureRegistry.VoronoiShapes);`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos.Take((int)afterimageCount * 2), npc.Size * 0.5f - Main.screenPosi`

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item45, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item84, npc.Center);`