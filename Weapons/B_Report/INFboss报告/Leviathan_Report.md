# 利维坦与阿娜希塔 (Leviathan & Anahita) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Leviathan`
- **重写的NPC目标**: `ModContent.NPCType<Anahita>()`, `ModContent.NPCType<LeviathanNPC>()`
- **关联源文件**:
  - `AnahitaBehaviorOverride.cs`
  - `AnahitaWaterIllusion.cs`
  - `AquaticAberrationProj.cs`
  - `AtlantisSpear.cs`
  - `AtlantisSpear2.cs`
  - `HeavenlyLullaby.cs`
  - `LeviathanBehaviorOverride.cs`
  - `LeviathanComboAttackManager.cs`
  - `LeviathanMeteor.cs`
  - `LeviathanSpawner.cs`
  - `LeviathanSpawnWave.cs`
  - `LeviathanVomit.cs`
  - `RedirectingWaterBolt.cs`
  - `WaterBolt.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `LeviathanSummonLifeRatio: 0.5f`
- `Phase Ratio Array: LeviathanSummonLifeRatio`
- `AnahitaReturnLifeRatio: 0.5f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `AnahitaAttackType`
- `// Alone attacks.
            FloatTowardsPlayer`
- `CreateWaterIllusions`
- `PlaySinusoidalSong`
- `IceMistBarrages`
- `ChargeAndCreateWaterCircle`
- `// Alone and enraged attacks.
            AtlantisCharge`
### 状态机/枚举: `LeviathanAttackType`
- `// Alone attacks.
            VomitBlasts`
- `HorizontalCharges`
- `MeteorBelch`
- `// Alone and enraged attacks.
            AberrationCharges`
### 状态机/枚举: `LeviathanComboAttackType`
- `UpwardRedirectingWaterSpears`
- `ExoTwinsBasicShotsPrecursor`
- `AngeringSong`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Alone attacks.*
- **源码注释**: *Alone and enraged attacks.*
- **源码注释**: *Select a target and reset damage and invulnerability.*
- **源码注释**: *Don't take damage if the target leaves the ocean.*
- **源码注释**: *Summon the Leviathan once ready.*
- **源码注释**: *play forbidden lullaby at phase transition*
- **源码注释**: *Fade out before teleporting above the target and creating water illusions.*
- **源码注释**: *Teleport and create the illusions.*
- **源码注释**: *Reset opacity and teleport after the delay is finished.*
- **源码注释**: *Use charging frames and do damage.*
- **源码注释**: *Charge.*
- **源码注释**: *Check to see if water or tiles have been hit. If they have, go to the next attack state and create a bunch of water spears.*
- **源码注释**: *Do a bit more damage than usual when charging.*
- **源码注释**: *Reset the attack timer.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `AtlantisSpear`
  - *实现细节*: `AtlantisSpear.cs` (常规渲染)
- **弹幕类名/类型**: `LeviathanMeteor`
  - *实现细节*: `LeviathanMeteor.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LeviathanSpawnWave`
  - *实现细节*: `LeviathanSpawnWave.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `RedirectingWaterBolt`
  - *实现细节*: `RedirectingWaterBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AtlantisSpear2`
  - *实现细节*: `AtlantisSpear2.cs` (常规渲染)
- **弹幕类名/类型**: `HeavenlyLullaby`
  - *实现细节*: `HeavenlyLullaby.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LeviathanVomit`
  - *实现细节*: `LeviathanVomit.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `WaterBolt`
  - *实现细节*: `WaterBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AnahitaWaterIllusion`
  - *实现细节*: `AnahitaWaterIllusion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LeviathanSpawner`
  - *实现细节*: `LeviathanSpawner.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AquaticAberrationProj`
  - *实现细节*: `AquaticAberrationProj.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in LeviathanMeteor.cs
- 着色器引用: `Shader/Overlay reference in LeviathanSpawner.cs`
- Custom rendering found in LeviathanBehaviorOverride.cs
- Custom rendering found in HeavenlyLullaby.cs
- Custom rendering found in AquaticAberrationProj.cs
- Custom rendering found in LeviathanVomit.cs
- Custom rendering found in LeviathanSpawnWave.cs
- Custom rendering found in WaterBolt.cs
- 着色器引用: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`
- 着色器引用: `TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.Du`
- Custom rendering found in RedirectingWaterBolt.cs
- Custom rendering found in AnahitaWaterIllusion.cs
- Custom rendering found in AnahitaBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in LeviathanSpawnWave.cs`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in LeviathanSpawner.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item165, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item28, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.AnahitaSingSound with { Volume = 0.4f, PitchVariance = 0f, MaxInstances = 6 `
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CommonCalamitySounds.LouderPhantomPhoenix, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite, target.Center);`