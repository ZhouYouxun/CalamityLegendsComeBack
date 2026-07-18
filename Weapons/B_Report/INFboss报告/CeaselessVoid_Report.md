# 无尽虚空 (Ceaseless Void) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `CeaselessVoid`
- **重写的NPC目标**: `ModContent.NPCType<CeaselessVoidBoss>()`, `ModContent.NPCType<DarkEnergy>()`
- **关联源文件**:
  - `AcceleratingDarkEnergy.cs`
  - `CeaselessEnergyPulse.cs`
  - `CeaselessVoidBehaviorOverride.cs`
  - `CeaselessVoidLineTelegraph.cs`
  - `CeaselessVoidMusicSceneInfernum.cs`
  - `CeaselessVoidShell.cs`
  - `CeaselessVortex.cs`
  - `CeaselessVortexTear.cs`
  - `ConvergingDungeonRubble.cs`
  - `DarkEnergyBehaviorOverride.cs`
  - `EnergyTelegraph.cs`
  - `OtherworldlyBolt.cs`
  - `RealitySlice.cs`
  - `SpinningDarkEnergy.cs`
  - `TelegraphedOtherwordlyBolt.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.66667f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.15f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DarkEnergyAttackState`
- `HoverInPlace`
- `SpinInPlace`
- `AccelerateTowardsTarget`
### 状态机/枚举: `CeaselessVoidAttackType`
- `// Phase 1 startup.
            ChainedUp`
- `DarkEnergySwirl`
- `// Phase 1 attacks.
            RedirectingAcceleratingDarkEnergy`
- `DiagonalMirrorBolts`
- `CircularVortexSpawn`
- `SpinningDarkEnergy`
- `AreaDenialVortexTears`
- `// Phase 2 transition.
            ShellCrackTransition`
- `DarkEnergyTorrent`
- `// Phase 2 attacks.
            EnergySuck`
- `// Phase 3 transition.
            ChainBreakTransition`
- `// Phase 3 attacks.
            JevilDarkEnergyBursts`
- `MirroredCharges`
- `ConvergingEnergyBarrages`
- `// Death animation attack.
            DeathAnimation`
### 状态机/枚举: `OtherwordlyBoltAttackState`
- `LockIntoPosition`
- `FlyIntoBackground`
- `AccelerateFromBelow`
- `ArcAndAccelerate`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Phase 1 startup.*
- **源码注释**: *Phase 1 attacks.*
- **源码注释**: *Phase 2 transition.*
- **源码注释**: *Phase 2 attacks.*
- **源码注释**: *Phase 3 transition.*
- **源码注释**: *Phase 3 attacks.*
- **源码注释**: *Death animation attack.*
- **源码注释**: *Do phase transitions.*
- **源码注释**: *Check to see if a player is moving through the chains.*
- **源码注释**: *Teleport to the position.*
- **源码注释**: *Play the teleport sound.*
- **源码注释**: *Create a puff of dark energy at the teleport position.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Make the screen black to distract the player from the fact that some wacky things are going on in the background.*
- **源码注释**: *Grant the targets infinite flight time during the portal tear charge up attack, so that they don't run out and take an unfair hit.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `RealitySlice`
  - *实现细节*: `RealitySlice.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `EnergyTelegraph`
  - *实现细节*: `EnergyTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SpinningDarkEnergy`
  - *实现细节*: `SpinningDarkEnergy.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CeaselessVoidShell`
  - *实现细节*: `CeaselessVoidShell.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AcceleratingDarkEnergy`
  - *实现细节*: `AcceleratingDarkEnergy.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CeaselessVortex`
  - *实现细节*: `CeaselessVortex.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `OtherworldlyBolt`
  - *实现细节*: `OtherworldlyBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TelegraphedOtherwordlyBolt`
  - *实现细节*: `TelegraphedOtherwordlyBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CeaselessVoidLineTelegraph`
  - *实现细节*: `CeaselessVoidLineTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CeaselessVortexTear`
  - *实现细节*: `CeaselessVortexTear.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ConvergingDungeonRubble`
  - *实现细节*: `ConvergingDungeonRubble.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(InfernumTextureRegistry.Stars);`
- 着色器引用: `Shader/Overlay reference in CeaselessVortex.cs`
- Custom rendering found in EnergyTelegraph.cs
- 着色器引用: `PrimitiveRenderer.RenderCircle(npc.Center, new(_ => radius, _ => Color.White, Shader: InfernumEffectsRegistry.RealityTea`
- Custom rendering found in CeaselessVoidBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.RealityTearVertexShader.TrySetParameter("useOutline", true);`
- 着色器引用: `InfernumEffectsRegistry.CeaselessVoidCrackShader.UseShaderSpecificData(new(npc.frame.X, npc.frame.Y, npc.frame.Width, np`
- Custom rendering found in DarkEnergyBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.CeaselessVoidCrackShader.Shader.Parameters["sheetSize"].SetValue(metalTexture.Size());`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);`
- 着色器引用: `var tear = InfernumEffectsRegistry.RealityTearVertexShader;`
- 着色器引用: `Shader/Overlay reference in CeaselessVortexTear.cs`
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);`
- Custom rendering found in CeaselessVoidLineTelegraph.cs
- 着色器引用: `InfernumEffectsRegistry.CeaselessVoidCrackShader.Apply();`
- Custom rendering found in RealitySlice.cs
- Custom rendering found in CeaselessEnergyPulse.cs
- Custom rendering found in OtherworldlyBolt.cs
- 着色器引用: `Shader/Overlay reference in SpinningDarkEnergy.cs`
- 着色器引用: `Shader/Overlay reference in CeaselessVoidBehaviorOverride.cs`
- 着色器引用: `var portalShader = InfernumEffectsRegistry.CeaselessVoidPortalShader;`
- Custom rendering found in ConvergingDungeonRubble.cs
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(distorti`
- 着色器引用: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- Custom rendering found in CeaselessVortex.cs
- Custom rendering found in TelegraphedOtherwordlyBolt.cs
- 着色器引用: `InfernumEffectsRegistry.CeaselessVoidCrackShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in SpinningDarkEnergy.cs
- 着色器引用: `Shader/Overlay reference in RealitySlice.cs`
- 着色器引用: `PrimitiveSettings settings = new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Shader: InfernumEffectsRegis`
- Custom rendering found in AcceleratingDarkEnergy.cs
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- Custom rendering found in CeaselessVortexTear.cs
- Custom rendering found in CeaselessVoidShell.cs
- 特效代码片段: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(distorti`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);`
- 特效代码片段: `spriteBatch.EnterShaderRegion();`
- 特效代码片段: `spriteBatch.ExitShaderRegion();`
- 特效代码片段: `var portalShader = InfernumEffectsRegistry.CeaselessVoidPortalShader;`
- 特效代码片段: `portalShader.UseOpacity(npc.Opacity);`
- 特效代码片段: `portalShader.UseColor(Color.Black);`
- 特效代码片段: `portalShader.UseSecondaryColor(Color.Lerp(Color.HotPink, Color.DarkBlue, 0.58f));`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in CeaselessVoidBehaviorOverride.cs
- Screen shake/effects found in CeaselessEnergyPulse.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/CeaselessVoid");`
- 屏幕震动/音效触发: `npc.DeathSound = SoundID.NPCDeath14;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CeaselessVoidBoss.DeathSound);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidTeleportSound with { Volume = 0.6f, Pitch = -0.25f }, npc.Cente`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidChainSound with { Volume = 0.25f, PitchVariance = 0.05f }, e.Ce`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item103, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item164 with { Pitch = -0.7f }, target.Center);`