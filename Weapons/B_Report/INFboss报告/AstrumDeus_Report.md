# 星神游龙 (Astrum Deus) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `AstrumDeus`
- **重写的NPC目标**: `ModContent.NPCType<AstrumDeusTail>()`, `ModContent.NPCType<AstrumDeusBody>()`, `ModContent.NPCType<AstrumDeusHead>()`
- **关联源文件**:
  - `AstralBlackHole.cs`
  - `AstralConstellation.cs`
  - `AstralCrystal.cs`
  - `AstralFlame2.cs`
  - `AstralPlasmaFireball.cs`
  - `AstralPlasmaSpark.cs`
  - `AstralRubble.cs`
  - `AstralSparkle.cs`
  - `AstralTelegraphLine.cs`
  - `AstralVortex.cs`
  - `AstrumDeusBodyBehaviorOverride.cs`
  - `AstrumDeusHeadBehaviorOverride.cs`
  - `AstrumDeusTailBehaviorOverride.cs`
  - `DarkGodLaser.cs`
  - `DarkStar.cs`
  - `DeusSpawn.cs`
  - `DeusSpawnerBehaviorOverride.cs`
  - `InfectionGlob.cs`
  - `MassiveInfectedStar.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.6f`
- `Phase3LifeRatio: 0.33333f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DeusAttackType`
- `WarpCharge`
- `AstralMeteorShower`
- `RubbleFromBelow`
- `VortexLemniscate`
- `PlasmaAndCrystals`
- `AstralSolarSystem`
- `InfectedStarWeave`
- `DarkGodsOutburst`
- `AstralGlobRush`
- `ConstellationExplosions`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `AstralPlasmaFireball`
  - *实现细节*: `AstralPlasmaFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralTelegraphLine`
  - *实现细节*: `AstralTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralPlasmaSpark`
  - *实现细节*: `AstralPlasmaSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralSparkle`
  - *实现细节*: `AstralSparkle.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MassiveInfectedStar`
  - *实现细节*: `MassiveInfectedStar.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AstralConstellation`
  - *实现细节*: `AstralConstellation.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralVortex`
  - *实现细节*: `AstralVortex.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AstralFlame2`
  - *实现细节*: `AstralFlame2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralCrystal`
  - *实现细节*: `AstralCrystal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralRubble`
  - *实现细节*: `AstralRubble.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkStar`
  - *实现细节*: `DarkStar.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `InfectionGlob`
  - *实现细节*: `InfectionGlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralBlackHole`
  - *实现细节*: `AstralBlackHole.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in AstralBlackHole.cs
- Custom rendering found in DarkGodLaser.cs
- 着色器引用: `Shader/Overlay reference in AstralBlackHole.cs`
- Custom rendering found in DarkStar.cs
- Custom rendering found in AstrumDeusTailBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.45f);`
- Custom rendering found in DeusSpawnerBehaviorOverride.cs
- Custom rendering found in AstralConstellation.cs
- Custom rendering found in AstralPlasmaFireball.cs
- Custom rendering found in AstralVortex.cs
- Custom rendering found in InfectionGlob.cs
- 着色器引用: `Shader/Overlay reference in MassiveInfectedStar.cs`
- 着色器引用: `Shader/Overlay reference in DarkGodLaser.cs`
- Custom rendering found in AstralFlame2.cs
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- 着色器引用: `Shader/Overlay reference in AstralVortex.cs`
- Custom rendering found in AstralPlasmaSpark.cs
- Custom rendering found in AstralCrystal.cs
- Custom rendering found in AstralTelegraphLine.cs
- Custom rendering found in MassiveInfectedStar.cs
- Custom rendering found in AstrumDeusBodyBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);`
- Custom rendering found in AstralRubble.cs
- Custom rendering found in AstrumDeusHeadBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Turquoise);`
- Custom rendering found in AstralSparkle.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, InfernumEffectsRegistry.FireVertex`
- Custom rendering found in DeusSpawn.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in MassiveInfectedStar.cs
- 屏幕震动/音效触发: `npc.HitSound = SoundID.NPCHit1;`