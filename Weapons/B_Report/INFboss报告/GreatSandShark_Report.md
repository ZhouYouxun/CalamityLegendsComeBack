# 大沙鲨 (Great Sand Shark) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `GreatSandShark`
- **重写的NPC目标**: `ModContent.NPCType<BereftVassal>()`, `ModContent.NPCType<GreatSandSharkNPC>()`
- **关联源文件**:
  - `BereftVassal.cs`
  - `BereftVassalBigBoom.cs`
  - `BereftVassalComboAttackManager.cs`
  - `BereftVassalSpear.cs`
  - `BereftVassalTeleportBoom.cs`
  - `DustDevil.cs`
  - `GreatSandBlast.cs`
  - `GreatSandSharkBehaviorOverride.cs`
  - `GroundSlamWave.cs`
  - `PressureSandnado.cs`
  - `SandBlob.cs`
  - `SparkTelegraphLine.cs`
  - `TorrentWave.cs`
  - `VassalLightning.cs`
  - `VassalSpark.cs`
  - `WaterSlice.cs`
  - `WaterSpear.cs`
  - `WaterTorrentBeam.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.6f`
- `Phase Ratio Array: Phase2LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `BereftVassalAttackType`
- `IdleState`
- `SandBlobSlam`
- `LongHorizontalCharges`
- `SpearWaterTorrent`
- `WaterWaveSlam`
- `FallingWaterCastBarrges`
- `SandnadoPressureCharges`
- `HypersonicWaterSlashes`
- `SummonGreatSandShark`
- `TransitionToFinalPhase`
- `RetreatAnimation`
### 状态机/枚举: `BereftVassalComboAttackType`
- `ParabolicLeaps`
- `HorizontalChargesAndLightningSpears`
- `PerpendicularSandBursts`
- `MantisLordCharges`
- `SandstormBulletHell`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Reset damage and other things.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `WaterSlice`
  - *实现细节*: `WaterSlice.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SparkTelegraphLine`
  - *实现细节*: `SparkTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `VassalSpark`
  - *实现细节*: `VassalSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `VassalLightning`
  - *实现细节*: `VassalLightning.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BereftVassalSpear`
  - *实现细节*: `BereftVassalSpear.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DustDevil`
  - *实现细节*: `DustDevil.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `WaterSpear`
  - *实现细节*: `WaterSpear.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GroundSlamWave`
  - *实现细节*: `GroundSlamWave.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `GreatSandBlast`
  - *实现细节*: `GreatSandBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PressureSandnado`
  - *实现细节*: `PressureSandnado.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SandBlob`
  - *实现细节*: `SandBlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TorrentWave`
  - *实现细节*: `TorrentWave.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `WaterTorrentBeam`
  - *实现细节*: `WaterTorrentBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in PressureSandnado.cs
- 着色器引用: `Shader/Overlay reference in GroundSlamWave.cs`
- Custom rendering found in SandBlob.cs
- 着色器引用: `var lightning = InfernumEffectsRegistry.GaleLightningShader;`
- Custom rendering found in GreatSandSharkBehaviorOverride.cs
- Custom rendering found in VassalSpark.cs
- Custom rendering found in SparkTelegraphLine.cs
- Custom rendering found in WaterTorrentBeam.cs
- 着色器引用: `TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.Wate`
- 着色器引用: `var tear = InfernumEffectsRegistry.RealityTearVertexShader;`
- 着色器引用: `Shader/Overlay reference in WaterTorrentBeam.cs`
- 着色器引用: `InfernumEffectsRegistry.WaterVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"))`
- Custom rendering found in TorrentWave.cs
- Custom rendering found in BereftVassal.cs
- Custom rendering found in VassalLightning.cs
- 着色器引用: `Shader/Overlay reference in VassalLightning.cs`
- Custom rendering found in WaterSpear.cs
- Custom rendering found in BereftVassalSpear.cs
- 着色器引用: `Shader/Overlay reference in BereftVassal.cs`
- 着色器引用: `Shader/Overlay reference in WaterSlice.cs`
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVerte`
- Custom rendering found in DustDevil.cs
- Custom rendering found in GroundSlamWave.cs
- 着色器引用: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`
- Custom rendering found in GreatSandBlast.cs
- Custom rendering found in WaterSlice.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in BereftVassal.cs
- Screen shake/effects found in BereftVassalSpear.cs
- Screen shake/effects found in BereftVassalBigBoom.cs
- Screen shake/effects found in BereftVassalTeleportBoom.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `GlobalNPCOverrides.HitEffectsEvent += UseCustomHitSound;`
- 屏幕震动/音效触发: `private void UseCustomHitSound(NPC npc, ref NPC.HitInfo hit)`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkHitSound with { Volume = 2f }, npc.Center);`
- 屏幕震动/音效触发: `npc.HitSound = null;`