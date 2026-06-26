# 渊海灾虫 (Aquatic Scourge) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `AquaticScourge`
- **重写的NPC目标**: `ModContent.NPCType<AquaticScourgeTail>()`, `ModContent.NPCType<AquaticScourgeHead>()`, `ModContent.NPCType<AquaticScourgeBody>()`
- **关联源文件**:
  - `AcceleratingArcingAcid.cs`
  - `AcidBubble.cs`
  - `AquaticScourgeBodyBehaviorOverride.cs`
  - `AquaticScourgeBodySpike.cs`
  - `AquaticScourgeGore.cs`
  - `AquaticScourgeHeadBehaviorOverride.cs`
  - `AquaticScourgeTailBehaviorOverride.cs`
  - `FallingAcid.cs`
  - `LeechFeeder.cs`
  - `RadiationPulse.cs`
  - `SulphuricGas.cs`
  - `SulphuricGasDebuff.cs`
  - `SulphuricTornado.cs`
  - `SulphurousRockRubble.cs`
  - `WaterClearingBubble.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.25f`
- `Phase2LifeRatio: 0.67f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `AquaticScourgeAttackType`
- `SpawnAnimation`
- `BubbleSpin`
- `RadiationPulse`
- `WallHitCharges`
- `GasBreath`
- `EnterSecondPhase`
- `PerpendicularSpikeBarrage`
- `EnterFinalPhase`
- `AcidRain`
- `SulphurousTyphoon`
- `DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `SulphuricTornado`
  - *实现细节*: `SulphuricTornado.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `WaterClearingBubble`
  - *实现细节*: `WaterClearingBubble.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AcceleratingArcingAcid`
  - *实现细节*: `AcceleratingArcingAcid.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LeechFeeder`
  - *实现细节*: `LeechFeeder.cs` (常规渲染)
- **弹幕类名/类型**: `AcidBubble`
  - *实现细节*: `AcidBubble.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AquaticScourgeGore`
  - *实现细节*: `AquaticScourgeGore.cs` (常规渲染)
- **弹幕类名/类型**: `AquaticScourgeBodySpike`
  - *实现细节*: `AquaticScourgeBodySpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FallingAcid`
  - *实现细节*: `FallingAcid.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SulphuricGas`
  - *实现细节*: `SulphuricGas.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SulphurousRockRubble`
  - *实现细节*: `SulphurousRockRubble.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SulphuricGasDebuff`
  - *实现细节*: `SulphuricGasDebuff.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in SulphuricTornado.cs
- 着色器引用: `InfernumEffectsRegistry.DukeTornadoVertexShader.UseImage1("Images/Misc/Perlin");`
- 着色器引用: `WaterDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVert`
- 着色器引用: `Shader/Overlay reference in FallingAcid.cs`
- Custom rendering found in SulphurousRockRubble.cs
- Custom rendering found in AcidBubble.cs
- Custom rendering found in AcceleratingArcingAcid.cs
- Custom rendering found in AquaticScourgeBodySpike.cs
- Custom rendering found in AquaticScourgeHeadBehaviorOverride.cs
- Custom rendering found in RadiationPulse.cs
- 着色器引用: `Shader/Overlay reference in WaterClearingBubble.cs`
- 着色器引用: `Shader/Overlay reference in AcidBubble.cs`
- 着色器引用: `Shader/Overlay reference in SulphurousRockRubble.cs`
- Custom rendering found in AquaticScourgeBodyBehaviorOverride.cs
- Custom rendering found in SulphuricGas.cs
- Custom rendering found in FallingAcid.cs
- Custom rendering found in SulphuricGasDebuff.cs
- Custom rendering found in WaterClearingBubble.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in AquaticScourgeHeadBehaviorOverride.cs