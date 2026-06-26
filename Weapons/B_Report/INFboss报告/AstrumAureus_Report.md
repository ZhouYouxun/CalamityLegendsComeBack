# 白金星舰 (Astrum Aureus) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `AstrumAureus`
- **重写的NPC目标**: `ModContent.NPCType<AureusSpawn>()`, `ModContent.NPCType<AureusBoss>()`
- **关联源文件**:
  - `AstralBlueComet.cs`
  - `AstralLaserInfernum.cs`
  - `AstralMissile.cs`
  - `AstrumAureusBehaviorOverride.cs`
  - `AureusSpawnBehaviorOverride.cs`
  - `BlueLaserbeam.cs`
  - `MissileTelegraphLine.cs`
  - `OrangeLaserbeam.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatio: 0.45f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.6f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `AureusAttackType`
- `SpawnActivation`
- `WalkAndShootLasers`
- `LeapAtTarget`
- `RocketBarrage`
- `AstralLaserBursts`
- `AstralDrillLaser`
- `Recharge`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Disable extra damage from the astral infection debuff. The attacks themselves hit hard enough.*
- **源码注释**: *Start glowing in Phase 2.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Go to the next attack after 1.5 seconds, or if hit before then.*
- **源码注释**: *Go to the next attack after a brief period of time.*
- **源码注释**: *Make the attack go by more quickly if close to the target horizontally.*
- **源码注释**: *Shoot bursts of lasers periodically. This has a short delay to give the player some time to reposition.*
- **源码注释**: *Determine whether the attack should be repeated.*
- **源码注释**: *Enrage if the player moves too far away.*
- **源码注释**: *Charge up astral energy and gain a good amount of extra defense.*
- **源码注释**: *Play a charge sound as a telegraph prior to firing.*
- **源码注释**: *Increment the attack counter.*
- **源码注释**: *Always use a consistent attack after the spawn activation.*
- **源码注释**: *Recharge once the attack counter every few attacks.*
- **源码注释**: *Set the drill laser flag to true once the attack is performed.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `AstralMissile`
  - *实现细节*: `AstralMissile.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AstralBlueComet`
  - *实现细节*: `AstralBlueComet.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AstralLaserInfernum`
  - *实现细节*: `AstralLaserInfernum.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MissileTelegraphLine`
  - *实现细节*: `MissileTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- 未检测到显著的独立场地/特殊提示系统，主要依赖其高强度弹幕和动态AI进行战斗。

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in AstralLaserInfernum.cs
- Custom rendering found in BlueLaserbeam.cs
- Custom rendering found in AstrumAureusBehaviorOverride.cs
- Custom rendering found in AstralMissile.cs
- 着色器引用: `Shader/Overlay reference in OrangeLaserbeam.cs`
- Custom rendering found in OrangeLaserbeam.cs
- 着色器引用: `Shader/Overlay reference in AstralMissile.cs`
- Custom rendering found in AstralBlueComet.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);`
- 着色器引用: `Shader/Overlay reference in BlueLaserbeam.cs`
- Custom rendering found in MissileTelegraphLine.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(187, 220, 237);`
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- 着色器引用: `Shader/Overlay reference in AstrumAureusBehaviorOverride.cs`
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `public static float PrimitiveWidthFunction(float completionRatio) => 150f;`
- 特效代码片段: `public static Color PrimitiveTrailColor(NPC npc, float completionRatio)`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer ??= new(PrimitiveWidthFunction, c => PrimitiveTrailColor(npc, c), null, true, Gam`
- 特效代码片段: `GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 51);`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in AstrumAureusBehaviorOverride.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.AstrumAureusLaserSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(AureusBoss.JumpSound with { Volume = 0.4f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.AstrumAureusStompSound with { Volume = 4f }, npc.Center);`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CommonCalamitySounds.PlasmaBoltSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.PBGMechanicalWarning, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CrystylCrusher.ChargeSound, target.Center);`