# 癫狂龙翁 (Dragonfolly) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Dragonfolly`
- **重写的NPC目标**: `ModContent.NPCType<DraconicSwarmer>()`, `ModContent.NPCType<CalamityMod.NPCs.Bumblebirb.Dragonfolly>()`
- **关联源文件**:
  - `BigFollyFeather.cs`
  - `BirbThunderAuraFlare.cs`
  - `DraconicSwarmerBehaviorOverride.cs`
  - `DragonfollyBehaviorOverride.cs`
  - `ExplodingEnergyOrb.cs`
  - `FollyFeather.cs`
  - `LightningCloud.cs`
  - `LightningCloud2.cs`
  - `LightningSuperchargeTelegraph.cs`
  - `RedLightningRedirectingFeather.cs`
  - `RedLightningSnipeFeather.cs`
  - `RedPlasmaEnergy.cs`
  - `RedSpark.cs`
  - `VolatileLightning.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.3333f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DragonfollyAttackType`
- `SpawnEffects`
- `FeatherSpreadRelease`
- `OrdinaryCharge`
- `FakeoutCharge`
- `ThunderCharge`
- `PlasmaBursts`
- `ElectricOverload`
- `RuffleFeathers`
- `ExplodingEnergyOrbs`
- `LightningSupercharge`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Begin the redshift phase in phase 2 if 15 seconds have passed.*
- **源码注释**: *Search for a player to target.*
- **源码注释**: *If the player is very far away, go to a different attack.*
- **源码注释**: *If somewhat close to the player and not stuck, go back to picking an attack.*
- **源码注释**: *Line up for a charge for a short amount of time.*
- **源码注释**: *And perform the charge.*
- **源码注释**: *If not stuck, just go back to picking a different attack.*
- **源码注释**: *do the idle search attack.*
- **源码注释**: *Try to go towards the player and charge, while fading red.*
- **源码注释**: *Release lightning clouds when charging if in phase 3.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `ExplodingEnergyOrb`
  - *实现细节*: `ExplodingEnergyOrb.cs` (常规渲染)
- **弹幕类名/类型**: `BirbThunderAuraFlare`
  - *实现细节*: `BirbThunderAuraFlare.cs` (常规渲染)
- **弹幕类名/类型**: `FollyFeather`
  - *实现细节*: `FollyFeather.cs` (常规渲染)
- **弹幕类名/类型**: `LightningSuperchargeTelegraph`
  - *实现细节*: `LightningSuperchargeTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `RedLightningRedirectingFeather`
  - *实现细节*: `RedLightningRedirectingFeather.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BigFollyFeather`
  - *实现细节*: `BigFollyFeather.cs` (常规渲染)
- **弹幕类名/类型**: `RedSpark`
  - *实现细节*: `RedSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `RedLightningSnipeFeather`
  - *实现细节*: `RedLightningSnipeFeather.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LightningCloud`
  - *实现细节*: `LightningCloud.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LightningCloud2`
  - *实现细节*: `LightningCloud2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `VolatileLightning`
  - *实现细节*: `VolatileLightning.cs` (常规渲染)

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in RedPlasmaEnergy.cs
- Custom rendering found in LightningSuperchargeTelegraph.cs
- Custom rendering found in RedLightningRedirectingFeather.cs
- Custom rendering found in LightningCloud2.cs
- 着色器引用: `Shader/Overlay reference in LightningSuperchargeTelegraph.cs`
- Custom rendering found in RedSpark.cs
- 着色器引用: `var flame = InfernumEffectsRegistry.FlameVertexShader;`
- Custom rendering found in LightningCloud.cs
- Custom rendering found in DragonfollyBehaviorOverride.cs
- Custom rendering found in DraconicSwarmerBehaviorOverride.cs
- Custom rendering found in RedLightningSnipeFeather.cs

### 屏幕特效 (Screen Effects):
- 常规屏幕震动或无显著震屏行为。