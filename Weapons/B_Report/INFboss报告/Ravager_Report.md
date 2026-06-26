# 毁灭魔像 (Ravager) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Ravager`
- **重写的NPC目标**: `ModContent.NPCType<RavagerClawRight>()`, `ModContent.NPCType<RavagerLegRight>()`, `ModContent.NPCType<RavagerHead2>()`, `ModContent.NPCType<RavagerLegLeft>()`, `ModContent.NPCType<RavagerClawLeft>()`, `ModContent.NPCType<RavagerBody>()`, `ModContent.NPCType<RavagerHead>()`
- **关联源文件**:
  - `DarkFlamePillar.cs`
  - `DarkFlamePillarTelegraph.cs`
  - `DarkMagicCinder.cs`
  - `DarkMagicFireball.cs`
  - `GroundBloodSpike.cs`
  - `GroundBloodSpikeCreator.cs`
  - `RavagerBodyBehaviorOverride.cs`
  - `RavagerClawLeftBehaviorOverride.cs`
  - `RavagerClawRightBehaviorOverride.cs`
  - `RavagerFlame.cs`
  - `RavagerFreeHeadBehaviorOverride.cs`
  - `RavagerHeadOverride.cs`
  - `RavagerLegLeftBehaviorOverride.cs`
  - `RavagerLegRightBehaviorOverride.cs`
  - `SlammingRockPillar.cs`
  - `StompShockwave.cs`
  - `UnholyBloodGlob.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- 无硬编码的血量比例常量，可能使用默认的阶段转换或动态AI。

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `RavagerAttackType`
- `SingleBurstsOfBlood`
- `RegularJumps`
- `BarrageOfBlood`
- `SingleBurstsOfUpwardDarkFlames`
- `DownwardFistSlam`
- `SlamAndCreateMovingFlamePillars`
- `WallSlams`
- `DetachedHeadCinderRain`
### 状态机/枚举: `RavagerClawAttackState`
- `StickToBody`
- `Punch`
- `Hover`
- `AccelerationPunch`
- `SlamIntoGround`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Determine phase information.*
- **源码注释**: *Make the attack delay pass.*
- **源码注释**: *Create the horizontal walls and reset the phase cycle once in the second phase.*
- **源码注释**: *Perform attacks.*
- **源码注释**: *While the player needs to be near Ravager to see the particles, it should still be fine due to*
- **源码注释**: *Wait before shooting and at the end of the attack. The arms will attack during this period if they are present, however.*
- **源码注释**: *The head itself does the attack.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `GroundBloodSpike`
  - *实现细节*: `GroundBloodSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `RitualFlame`
- **弹幕类名/类型**: `StompShockwave`
  - *实现细节*: `StompShockwave.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GroundBloodSpikeCreator`
  - *实现细节*: `GroundBloodSpikeCreator.cs` (常规渲染)
- **弹幕类名/类型**: `DarkFlamePillarTelegraph`
  - *实现细节*: `DarkFlamePillarTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkMagicCinder`
  - *实现细节*: `DarkMagicCinder.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SlammingRockPillar`
  - *实现细节*: `SlammingRockPillar.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkMagicFireball`
  - *实现细节*: `DarkMagicFireball.cs` (常规渲染)
- **弹幕类名/类型**: `UnholyBloodGlob`
  - *实现细节*: `UnholyBloodGlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkFlamePillar`
  - *实现细节*: `DarkFlamePillar.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in SlammingRockPillar.cs
- 着色器引用: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);`
- Custom rendering found in RavagerClawLeftBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in DarkFlamePillar.cs`
- Custom rendering found in StompShockwave.cs
- Custom rendering found in DarkFlamePillar.cs
- Custom rendering found in UnholyBloodGlob.cs
- Custom rendering found in RavagerBodyBehaviorOverride.cs
- Custom rendering found in GroundBloodSpike.cs
- Custom rendering found in RavagerFreeHeadBehaviorOverride.cs
- Custom rendering found in DarkMagicCinder.cs
- Custom rendering found in RavagerClawRightBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in RavagerBodyBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);`
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarV`
- 着色器引用: `npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(widthFunction, colorFunction, null, true, InfernumEffe`
- Custom rendering found in DarkFlamePillarTelegraph.cs
- 特效代码片段: `using InfernumMode.Common.Graphics.Primitives;`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(widthFunction, colorFunction, null, true, InfernumEffe`
- 特效代码片段: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);`
- 特效代码片段: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer.Draw(points, -Main.screenPosition, 166);`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in RavagerBodyBehaviorOverride.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item45, npc.Center);`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 10f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.RavagerFlamePillarEruptSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(RavagerBody.JumpSound, npc.Bottom);`