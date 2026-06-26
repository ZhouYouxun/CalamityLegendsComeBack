# 犽戎 / 丛林龙 (Yharon) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Yharon`
- **重写的NPC目标**: `ModContent.NPCType<YharonBoss>()`
- **关联源文件**:
  - `DraconicBlossomPetal.cs`
  - `DraconicInfernado.cs`
  - `DragonFireball.cs`
  - `HomingFireball.cs`
  - `InfernadoSpawner.cs`
  - `LingeringDragonFlames.cs`
  - `MajesticSparkleBig.cs`
  - `RedirectingYharonMeteor.cs`
  - `VortexFireball.cs`
  - `VortexOfFlame.cs`
  - `VortexTelegraphBeam.cs`
  - `YharonBehaviorOverride.cs`
  - `YharonBoom.cs`
  - `YharonFlameExplosion.cs`
  - `YharonFlamethrower.cs`
  - `YharonHeatFlashFireball.cs`
  - `YharonMajesticSparkle.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.1f`
- `Subphase6LifeRatio: 0.4f`
- `Subphase5LifeRatio: 0.8f`
- `Subphase8LifeRatio: 0.025f`
- `Subphase4LifeRatio: Phase2LifeRatio`
- `Phase Ratio Array: Subphase2LifeRatio, Subphase3LifeRatio, Subphase4LifeRatio, Subphase5LifeRatio, Subphase6LifeRatio, Subphase7LifeRatio, Subphase8LifeRatio`
- `Subphase7LifeRatio: 0.15f`
- `Subphase3LifeRatio: 0.45f`
- `Subphase2LifeRatio: 0.75f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `YharonAttackType`
- `SpawnEffects`
- `Charge`
- `FastCharge`
- `FireballBurst`
- `FlamethrowerAndMeteors`
- `FlarenadoAndDetonatingFlameSpawn`
- `FireTrailCharge`
- `MassiveInfernadoSummon`
- `TeleportingCharge`
- `EnterSecondPhase`
- `CarpetBombing`
- `PhoenixSupercharge`
- `HeatFlashRing`
- `VorticesOfFlame`
- `FinalDyingRoar`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Factor for how much Yharon deceleratrs once a charge concludes.*
- **源码注释**: *This exists as a way of reducing Yharon's momentum after a charge so that he can more easily get into position for the next charge.*
- **源码注释**: *The closer to 1 this value is, the quicker his charges will be.*
- **源码注释**: *Prevent Yharon from showing himself amongst his illusions in the desperation phase.*
- **源码注释**: *Delete projectiles when disappearing, so that there isn't anything still around if the player wants to immediately challenge Yharon again.*
- **源码注释**: *Go to phase 2 if close to death.*
- **源码注释**: *Set Yharon's private phase 2 flag that base Calamity uses.*
- **源码注释**: *This is necessary to ensure that the special phase 2 name is used.*
- **源码注释**: *Enter the second phase animation state.*
- **源码注释**: *Say the phase2 joke entry tip.*
- **源码注释**: *Without this, the subphase table check will fail because none of the conditions will be valid since it checks this variable when running the*
- **源码注释**: *InSecondPhase property check. Once the table check fails Yharon's AI will throw an exception and the game will delete him from existence.*
- **源码注释**: *Perform the aforementioned attack pattern lookup.*
- **源码注释**: *Transition to the next subphase if necessary.*
- **源码注释**: *Clear away projectiles in subphase 4 and 7.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `VortexTelegraphBeam`
  - *实现细节*: `VortexTelegraphBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `YharonFlamethrower`
  - *实现细节*: `YharonFlamethrower.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `InfernadoSpawner`
  - *实现细节*: `InfernadoSpawner.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `YharonBoom`
  - *实现细节*: `YharonBoom.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `YharonFlameExplosion`
  - *实现细节*: `YharonFlameExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `VortexOfFlame`
  - *实现细节*: `VortexOfFlame.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `YharonHeatFlashFireball`
  - *实现细节*: `YharonHeatFlashFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LingeringDragonFlames`
  - *实现细节*: `LingeringDragonFlames.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HomingFireball`
  - *实现细节*: `HomingFireball.cs` (常规渲染)
- **弹幕类名/类型**: `DraconicInfernado`
  - *实现细节*: `DraconicInfernado.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `DragonFireball`
  - *实现细节*: `DragonFireball.cs` (常规渲染)
- **弹幕类名/类型**: `DraconicBlossomPetal`
  - *实现细节*: `DraconicBlossomPetal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SkyFlareRevenge`
- **弹幕类名/类型**: `RedirectingYharonMeteor`
  - *实现细节*: `RedirectingYharonMeteor.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `YharonMajesticSparkle`
  - *实现细节*: `YharonMajesticSparkle.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MajesticSparkleBig`
  - *实现细节*: `MajesticSparkleBig.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `VortexFireball`
  - *实现细节*: `VortexFireball.cs` (常规渲染)

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in YharonBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["subtractiveNoiseStrength"].SetValue(1.11f);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uNoiseReadZoomFactor"].SetValue(new Vector2(0.2f, 0.2f));`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uSecondaryLavaPower"].SetValue(10f);`
- Custom rendering found in YharonFlameExplosion.cs
- Custom rendering found in YharonMajesticSparkle.cs
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uNPCRectangle"].SetValue(new Vector4(npc.frame.X, npc.frame.`
- 着色器引用: `Shader/Overlay reference in YharonBoom.cs`
- Custom rendering found in YharonHeatFlashFireball.cs
- Custom rendering found in DraconicInfernado.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(1.4f);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.UseColor(burnColor * 0.7f);`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(5f);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uActualImageSize0"].SetValue(tex.Size());`
- Custom rendering found in LingeringDragonFlames.cs
- 着色器引用: `Shader/Overlay reference in YharonBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.UseSecondaryColor(Color.White * 0.12f);`
- Custom rendering found in YharonBoom.cs
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uZoomFactorSecondary"].SetValue(0.5f);`
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.SetShaderTexture2(InfernumTextureRegistry.SmokyNoise);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.UseOpacity(fireIntensity * 0.7f);`
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["edgeTaperPower"].SetValue(0.51f);`
- 着色器引用: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- 着色器引用: `TornadoDrawer ??= new(TornadoWidthFunction, TornadoColorFunction, null, true, InfernumEffectsRegistry.YharonInfernadoSha`
- Custom rendering found in InfernadoSpawner.cs
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uZoomFactor"].SetValue(new Vector2(1f, 1f));`
- Custom rendering found in YharonFlamethrower.cs
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.SetShaderTexture(InfernumTextureRegistry.WavyNoise);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uTimeFactor"].SetValue(1.1f);`
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- Custom rendering found in DraconicBlossomPetal.cs
- 着色器引用: `Shader/Overlay reference in DraconicInfernado.cs`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- 着色器引用: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(cameraPa`
- Custom rendering found in VortexOfFlame.cs
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["scrollSpeed"].SetValue(0.9f);`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- 着色器引用: `Shader/Overlay reference in VortexTelegraphBeam.cs`
- Custom rendering found in MajesticSparkleBig.cs
- 着色器引用: `InfernumEffectsRegistry.YharonBurnShader.Apply();`
- 着色器引用: `InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["additiveNoiseStrength"].SetValue(2.15f);`
- Custom rendering found in RedirectingYharonMeteor.cs
- Custom rendering found in VortexTelegraphBeam.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`
- 特效代码片段: `Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f);`
- 特效代码片段: `Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f + LumUtils.Convert01To010(competionRatio) * 3f);`
- 特效代码片段: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(cameraPa`
- 特效代码片段: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(5f);`
- 特效代码片段: `Filters.Scene["HeatDistortion"].GetShader().UseIntensity(3f);`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion();`
- 特效代码片段: `InfernumEffectsRegistry.YharonBurnShader.UseOpacity(fireIntensity * 0.7f);`
- 特效代码片段: `InfernumEffectsRegistry.YharonBurnShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in YharonBehaviorOverride.cs
- Screen shake/effects found in YharonBoom.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/YharonP2");`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);`
- 屏幕震动/音效触发: `npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.RoarSound);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.OrbSound);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.OrbSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.ShortRoarSound with { Pitch = -0.56f, Volume = 1.6f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(YharonBoss.FireSound, target.Center);`