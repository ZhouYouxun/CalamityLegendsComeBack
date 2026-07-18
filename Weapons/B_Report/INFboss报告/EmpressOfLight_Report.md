# 光之女皇 (Empress of Light) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `EmpressOfLight`
- **重写的NPC目标**: `NPCID.HallowBoss`, `NPCID.EmpressButterfly`
- **关联源文件**:
  - `AcceleratingPrismaticBolt.cs`
  - `ArcingLightBolt.cs`
  - `EmpressAurora.cs`
  - `EmpressExplosion.cs`
  - `EmpressOfLightBehaviorOverride.cs`
  - `EmpressPrism.cs`
  - `EmpressSparkle.cs`
  - `EmpressSword.cs`
  - `EtherealLance.cs`
  - `LacewingBehaviorOverride.cs`
  - `LanceCreatingSword.cs`
  - `LightOverloadBeam.cs`
  - `PrismaticBolt.cs`
  - `PrismLaserbeam.cs`
  - `ShimmeringLightWave.cs`
  - `SpinningPrismLaserbeam.cs`
  - `StarBolt.cs`
  - `StolenCelestialObject.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio`
- `Phase3LifeRatio: 0.5f`
- `Phase4LifeRatio: 0.2f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `EmpressOfLightAttackType`
- `SpawnAnimation`
- `LanceBarrages`
- `PrismaticBoltCircle`
- `BackstabbingLances`
- `MesmerizingMagic`
- `HorizontalCharge`
- `EnterSecondPhase`
- `LightPrisms`
- `DanceOfSwords`
- `MajesticPierce`
- `LanceWallBarrage`
- `LargeRainbowStar`
- `UltimateRainbow`
- `DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Have the arm pointed towards the player aim downward, while the other hand points upward.*
- **源码注释**: *Teleport above the target on the first frame.*
- **源码注释**: *Wait before attacking.*
- **源码注释**: *Summon lances from behind the target from time to time to prevent rungod strats.*
- **源码注释**: *Fade out and teleport to the opposite side of the target halfway through the attack.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Summon swords and clap on the first frame.*
- **源码注释**: *Teleport above the player and release a bunch of stars.*
- **源码注释**: *If the player has a lot of momentum in a certain direction, it will be chosen in such a way that the player can simply retain their current direction, so as to*
- **源码注释**: *Suddenly summon a bunch of lances above the target.*
- **源码注释**: *Release lances from behind the player.*
- **源码注释**: *Summon the lance wall.*
- **源码注释**: *Teleport near the target if very far away.*
- **源码注释**: *Initialize the charge direction.*
- **源码注释**: *Charge.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `EmpressAurora`
  - *实现细节*: `EmpressAurora.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ArcingLightBolt`
  - *实现细节*: `ArcingLightBolt.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `EmpressPrism`
  - *实现细节*: `EmpressPrism.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `EmpressSword`
  - *实现细节*: `EmpressSword.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `EmpressSparkle`
  - *实现细节*: `EmpressSparkle.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PrismLaserbeam`
  - *实现细节*: `PrismLaserbeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `SpinningPrismLaserbeam`
  - *实现细节*: `SpinningPrismLaserbeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `StolenCelestialObject`
  - *实现细节*: `StolenCelestialObject.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `EmpressExplosion`
  - *实现细节*: `EmpressExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `EtherealLance`
  - *实现细节*: `EtherealLance.cs` (常规渲染)
- **弹幕类名/类型**: `LanceCreatingSword`
  - *实现细节*: `LanceCreatingSword.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `LightOverloadBeam`
  - *实现细节*: `LightOverloadBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `AcceleratingPrismaticBolt`
  - *实现细节*: `AcceleratingPrismaticBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PrismaticBolt`
  - *实现细节*: `PrismaticBolt.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `StarBolt`
  - *实现细节*: `StarBolt.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in EmpressSword.cs`
- 着色器引用: `RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVer`
- Custom rendering found in EmpressAurora.cs
- 着色器引用: `Shader/Overlay reference in PrismLaserbeam.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.HotPink * Projectile.Opacity);`
- Custom rendering found in PrismaticBolt.cs
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseOpacity(deathAnimationScreenShaderStrength);`
- 着色器引用: `Shader/Overlay reference in LightOverloadBeam.cs`
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseIntensity(screenShaderStrength);`
- 着色器引用: `Shader/Overlay reference in EmpressOfLightBehaviorOverride.cs`
- 着色器引用: `Shader/Overlay reference in SpinningPrismLaserbeam.cs`
- 着色器引用: `Shader/Overlay reference in StarBolt.cs`
- 着色器引用: `RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.ArtemisLaserVer`
- 着色器引用: `Shader/Overlay reference in StolenCelestialObject.cs`
- 着色器引用: `Shader/Overlay reference in LanceCreatingSword.cs`
- Custom rendering found in PrismLaserbeam.cs
- Custom rendering found in LacewingBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage(ModContent.Request<Texture2D>("InfernumMode/Content/Behavio`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(0.3f);`
- Custom rendering found in LightOverloadBeam.cs
- Custom rendering found in EmpressExplosion.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- 着色器引用: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.2f);`
- Custom rendering found in EmpressPrism.cs
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage("Images/Misc/Perlin", 2);`
- Custom rendering found in ArcingLightBolt.cs
- Custom rendering found in SpinningPrismLaserbeam.cs
- 着色器引用: `Shader/Overlay reference in ArcingLightBolt.cs`
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseColor(animationBackgroundColor);`
- Custom rendering found in ShimmeringLightWave.cs
- Custom rendering found in EtherealLance.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);`
- Custom rendering found in EmpressOfLightBehaviorOverride.cs
- 着色器引用: `TrailDrawer ??= new(WidthFunction, ColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);`
- Custom rendering found in AcceleratingPrismaticBolt.cs
- 着色器引用: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in StolenCelestialObject.cs
- 着色器引用: `Effect fireball = InfernumEffectsRegistry.FireballShader.GetShader().Shader;`
- Custom rendering found in StarBolt.cs
- Custom rendering found in EmpressSword.cs
- Custom rendering found in EmpressSparkle.cs
- 着色器引用: `Shader/Overlay reference in EmpressPrism.cs`
- 着色器引用: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage("Images/Misc/noise");`
- Custom rendering found in LanceCreatingSword.cs
- 着色器引用: `LightRayDrawer ??= new(LightRayWidthFunction, LightRayColorFunction, null, true, InfernumEffectsRegistry.SideStreakVerte`
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `public const int ScreenShaderIntensityIndex = 7;`
- 特效代码片段: `ref float screenShaderStrength = ref npc.localAI[3];`
- 特效代码片段: `ref float deathAnimationScreenShaderStrength = ref npc.Infernum().ExtraAI[ScreenShaderIntensityIndex];`
- 特效代码片段: `deathAnimationScreenShaderStrength = 0f;`
- 特效代码片段: `DoBehavior_UltimateRainbow(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame, ref animationBackgroundCol`
- 特效代码片段: `DoBehavior_DeathAnimation(npc, target, ref attackTimer, ref deathAnimationScreenShaderStrength);`
- 特效代码片段: `screenShaderStrength = 1f;`
- 特效代码片段: `screenShaderStrength = Utils.GetLerpValue(SecondPhaseFadeoutTime, SecondPhaseFadeoutTime + SecondPhaseFadeBackInTime, at`
- 特效代码片段: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage("Images/Misc/noise");`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in ShimmeringLightWave.cs
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item122, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item160, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item161, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item158, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item163, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item164, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item162 with { Volume = 2f }, target.Center);`
- 屏幕震动/音效触发: `lance.ModProjectile<EtherealLance>().PlaySoundOnFiring = i == 0;`
- 屏幕震动/音效触发: `lance.ModProjectile<EtherealLance>().SoundPitch = (npc.ai[1] - hoverRedirectTime) / horizontalShootTime * 0.35f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item162, npc.Center);`