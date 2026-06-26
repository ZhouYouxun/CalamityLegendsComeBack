# 原生幻海妖龙 (Adult Eidolon Wyrm) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `AdultEidolonWyrm`
- **重写的NPC目标**: `ModContent.NPCType<PrimordialWyrmHead>()`, `ModContent.NPCType<PrimordialWyrmBody>()`
- **关联源文件**:
  - `AbyssalSoul.cs`
  - `AbyssalSoulTelegraph.cs`
  - `AEWHeadBehaviorOverride.cs`
  - `AEWIllusionTelegraphLine.cs`
  - `AEWNightmareWyrm.cs`
  - `AEWSegmentBehaviorOverride.cs`
  - `AEWSplitForm.cs`
  - `AEWTelegraphLine.cs`
  - `BaseAttackingTerminusProjectile.cs`
  - `CircleCenterTelegraph.cs`
  - `ConvergingLumenylCrystal.cs`
  - `DivineLightBolt.cs`
  - `DivineLightLaserbeam.cs`
  - `DivineLightOrb.cs`
  - `HorizontalRayTerminus.cs`
  - `LightCleaveTelegraph.cs`
  - `PsychicBlast.cs`
  - `TerminusDeathray.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase4LifeRatio: 0.15f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase3LifeRatio: 0.45f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `AEWAttackType`
- `// Spawn animation states.
            SnatchTerminus`
- `ThreateninglyHoverNearPlayer`
- `// Light attacks.
            BurningGaze`
- `DisintegratingBeam`
- `TerminusChase`
- `// Dark attacks.
            AbyssalNightmareRitual`
- `ForbiddenUnleash`
- `ShadowIllusions`
- `// Neutral attacks.
            SplitFormCharges`
- `CrystalConstriction`
- `HammerheadRams`
- `// Enrage attack.
            RuthlesslyMurderTarget`
- `// Death animation state.
            DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Light attacks.*
- **源码注释**: *Dark attacks.*
- **源码注释**: *Neutral attacks.*
- **源码注释**: *Enrage attack.*
- **源码注释**: *Projectile damage values.*
- **源码注释**: *Disable obnoxious water mechanics so that the player can fight the boss without interruption.*
- **源码注释**: *Make the player emit a lot of light.*
- **源码注释**: *This isn't an attack, how kind of you to notice.*
- **源码注释**: *Increment the attack timer.*
- **源码注释**: *If the player is somehow not dead after enough time has passed they're just manually killed.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Transition to the next attack if there are no more Terminus instances.*
- **源码注释**: *On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.*
- **源码注释**: *Don't let the attack timer increment.*
- **源码注释**: *Slow down and look at the target threateningly before attacking.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `CircleCenterTelegraph`
  - *实现细节*: `CircleCenterTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `PsychicBlast`
  - *实现细节*: `PsychicBlast.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AEWSplitForm`
  - *实现细节*: `AEWSplitForm.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AbyssalSoul`
  - *实现细节*: `AbyssalSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AEWTelegraphLine`
  - *实现细节*: `AEWTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DivineLightBolt`
  - *实现细节*: `DivineLightBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TerminusDeathray`
  - *实现细节*: `TerminusDeathray.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BaseAttackingTerminusProjectile`
  - *实现细节*: `BaseAttackingTerminusProjectile.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `DivineLightOrb`
  - *实现细节*: `DivineLightOrb.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ConvergingLumenylCrystal`
  - *实现细节*: `ConvergingLumenylCrystal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AbyssalSoulTelegraph`
  - *实现细节*: `AbyssalSoulTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AEWNightmareWyrm`
  - *实现细节*: `AEWNightmareWyrm.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LightCleaveTelegraph`
  - *实现细节*: `LightCleaveTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AEWIllusionTelegraphLine`
  - *实现细节*: `AEWIllusionTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in DivineLightBolt.cs
- 着色器引用: `Shader/Overlay reference in AEWHeadBehaviorOverride.cs`
- 着色器引用: `Shader/Overlay reference in DivineLightBolt.cs`
- 着色器引用: `Shader/Overlay reference in AEWSplitForm.cs`
- 着色器引用: `Shader/Overlay reference in BaseAttackingTerminusProjectile.cs`
- 着色器引用: `Shader/Overlay reference in DivineLightOrb.cs`
- Custom rendering found in AEWSegmentBehaviorOverride.cs
- Custom rendering found in HorizontalRayTerminus.cs
- Custom rendering found in CircleCenterTelegraph.cs
- Custom rendering found in PsychicBlast.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- Custom rendering found in AEWHeadBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in AbyssalSoul.cs`
- Custom rendering found in AbyssalSoulTelegraph.cs
- Custom rendering found in AEWSplitForm.cs
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- Custom rendering found in AbyssalSoul.cs
- Custom rendering found in DivineLightLaserbeam.cs
- Custom rendering found in AEWTelegraphLine.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVert`
- 着色器引用: `Shader/Overlay reference in CircleCenterTelegraph.cs`
- Custom rendering found in LightCleaveTelegraph.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) `
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticR`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);`
- 着色器引用: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.25f);`
- 着色器引用: `Shader/Overlay reference in TerminusDeathray.cs`
- Custom rendering found in AEWNightmareWyrm.cs
- Custom rendering found in DivineLightOrb.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(1.4f);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(0.1f);`
- 着色器引用: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in TerminusDeathray.cs
- Custom rendering found in BaseAttackingTerminusProjectile.cs
- 着色器引用: `Shader/Overlay reference in DivineLightLaserbeam.cs`
- Custom rendering found in ConvergingLumenylCrystal.cs
- 着色器引用: `Shader/Overlay reference in PsychicBlast.cs`
- Custom rendering found in AEWIllusionTelegraphLine.cs
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion();`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Purple);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.HotPink);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].Apply();`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity * 0.7f);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Cyan);`
- 特效代码片段: `Main.spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in TerminusDeathray.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item163, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Volume = 1.3f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.TerminusLaserbeamSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CalamityMod.NPCs.Providence.Providence.HolyRaySound, target.Center);`