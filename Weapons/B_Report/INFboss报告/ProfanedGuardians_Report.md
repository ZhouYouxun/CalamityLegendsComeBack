# 神明守卫 (Profaned Guardians) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `ProfanedGuardians`
- **重写的NPC目标**: `ModContent.NPCType<ProfanedGuardianDefender>()`, `ModContent.NPCType<ProfanedGuardianCommander>()`, `ModContent.NPCType<ProfanedGuardianHealer>()`
- **关联源文件**:
  - `AttackerGuardianBehaviorOverride.cs`
  - `CommanderSpear.cs`
  - `CommanderSpearThrown.cs`
  - `DefenderGuardianBehaviorOverride.cs`
  - `DefenderShield.cs`
  - `EtherealHand.cs`
  - `GuardianComboAttackManager.cs`
  - `GuardianIndexManager.cs`
  - `GuardiansRodFailPulse.cs`
  - `HealerGuardianBehaviorOverride.cs`
  - `HealerShieldCrystal.cs`
  - `HolyAimedDeathray.cs`
  - `HolyAimedDeathrayTelegraph.cs`
  - `HolyDogmaFireball.cs`
  - `HolyFireRift.cs`
  - `HolyFireWall.cs`
  - `HolySineSpear.cs`
  - `HolySpinningFireBeam.cs`
  - `LavaEruptionPillar.cs`
  - `LingeringHolyFire.cs`
  - `MagicCrystalShot.cs`
  - `MagicSpiralCrystalShot.cs`
  - `ProfanedCirclingRock.cs`
  - `ProfanedRock.cs`
  - `ProfanedSpearInfernum.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- 无硬编码的血量比例常量，可能使用默认的阶段转换或动态AI。

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `GuardiansAttackType`
- `// Initial attacks.
            SpawnEffects`
- `FlappyBird`
- `// All 3 combo attacks.
            SoloHealer`
- `SoloDefender`
- `HealerAndDefender`
- `HealerDeathAnimation`
- `// Commander and Defender combo attacks
            SpearDashAndGroundSlam`
- `CrashRam`
- `FireballBulletHell`
- `DefenderDeathAnimation`
- `// Commander solo attacks.
            LargeGeyserAndCharge`
- `DogmaLaserBall`
- `BerdlySpears`
- `SpearSpinThrow`
- `RiftFireCharges`
- `CommanderDeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Summon the defender and healer guardian.*
- **源码注释**: *Don't take damage if other guardians are around.*
- **源码注释**: *Deal damage.*
- **源码注释**: *Give the player infinite flight time, and keep them in the bounds.*
- **源码注释**: *Force the player into the area if the opacity is drawn.*
- **源码注释**: *if (attackState >= (float)GuardiansAttackType.DefenderDeathAnimation)*
- **源码注释**: *Do attacks.*
- **源码注释**: *Disable damage.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `CommanderSpearThrown`
  - *实现细节*: `CommanderSpearThrown.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyFireRift`
  - *实现细节*: `HolyFireRift.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `MagicSpiralCrystalShot`
  - *实现细节*: `MagicSpiralCrystalShot.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ProfanedSpearInfernum`
  - *实现细节*: `ProfanedSpearInfernum.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolySpinningFireBeam`
  - *实现细节*: `HolySpinningFireBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolyFireWall`
  - *实现细节*: `HolyFireWall.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CommanderSpear`
  - *实现细节*: `CommanderSpear.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LavaEruptionPillar`
  - *实现细节*: `LavaEruptionPillar.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `LingeringHolyFire`
  - *实现细节*: `LingeringHolyFire.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MagicCrystalShot`
  - *实现细节*: `MagicCrystalShot.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HolyDogmaFireball`
  - *实现细节*: `HolyDogmaFireball.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ProfanedRock`
  - *实现细节*: `ProfanedRock.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolyAimedDeathray`
  - *实现细节*: `HolyAimedDeathray.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HolySineSpear`
  - *实现细节*: `HolySineSpear.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `DefenderShield`
  - *实现细节*: `DefenderShield.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in ProfanedSpearInfernum.cs
- Custom rendering found in GuardianComboAttackManager.cs
- Custom rendering found in HolyAimedDeathray.cs
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.5f * opacityScalar);`
- 着色器引用: `Shader/Overlay reference in HolyAimedDeathray.cs`
- Custom rendering found in ProfanedRock.cs
- 着色器引用: `TelegraphDrawer ??= new PrimitiveTrailCopy(TelegraphWidthFunction, TelegraphColorFunction, null, true, InfernumEffectsRe`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["pillarVarient"].SetValue(false);`
- 着色器引用: `FireColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);`
- 着色器引用: `Shader/Overlay reference in HolyFireWall.cs`
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.Shader.Parameters["flipY"].SetValue(flipY);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.SetTexture(InfernumTextureRegistry.HarshNoise, 1);`
- Custom rendering found in AttackerGuardianBehaviorOverride.cs
- 着色器引用: `LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVe`
- 着色器引用: `Shader/Overlay reference in HolySineSpear.cs`
- Custom rendering found in DefenderGuardianBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(true);`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.CultistRayMap);`
- 着色器引用: `Effect fireball = InfernumEffectsRegistry.FireballShader.GetShader().Shader;`
- Custom rendering found in HolySineSpear.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["scrollSpeed"].SetValue(BigVersion ? 1f : 1.8f);`
- Custom rendering found in LavaEruptionPillar.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uOpacity", alpha * npc.Infernum().ExtraAI[FireBorderInte`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(false);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("timeFactor", 2f);`
- 着色器引用: `PrimitiveRenderer.RenderCircleEdge(npc.Center, new(widthFunction, colorFunction, radiusFunction, false, InfernumEffectsR`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- Custom rendering found in DefenderShield.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["stretchAmount"].SetValue((BigVersion ? 0.6f : 1.3f`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["scrollSpeed"].SetValue(1.8f);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(3f);`
- Custom rendering found in CommanderSpear.cs
- Custom rendering found in HolyDogmaFireball.cs
- Custom rendering found in HolyFireRift.cs
- Custom rendering found in HolyAimedDeathrayTelegraph.cs
- 着色器引用: `Shader/Overlay reference in HolySpinningFireBeam.cs`
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(false);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(false);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(2.5f * commander.Infernum().ExtraAI[HealerConnectionsWi`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.CrustyNoise);`
- 着色器引用: `Shader/Overlay reference in HolyFireRift.cs`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(BrightFire);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("noiseSpeed", new Vector2(0.1f, 0.1f));`
- 着色器引用: `Shader/Overlay reference in ProfanedCirclingRock.cs`
- 着色器引用: `Shader/Overlay reference in LavaEruptionPillar.cs`
- 着色器引用: `Shader/Overlay reference in ProfanedRock.cs`
- 着色器引用: `Shader/Overlay reference in HealerGuardianBehaviorOverride.cs`
- Custom rendering found in HolySpinningFireBeam.cs
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBubbleGlow);`
- 着色器引用: `Effect portal = InfernumEffectsRegistry.ProfanedPortalShader.Shader;`
- Custom rendering found in HealerGuardianBehaviorOverride.cs
- Custom rendering found in CommanderSpearThrown.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.LavaNoise);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(MagicSpiralCrystalShot.ColorSet[0], Color.White, `
- Custom rendering found in HealerShieldCrystal.cs
- 着色器引用: `Shader/Overlay reference in AttackerGuardianBehaviorOverride.cs`
- 着色器引用: `Shader/Overlay reference in HealerShieldCrystal.cs`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uColor", WayfinderSymbol.Colors[2]);`
- 着色器引用: `null, true, InfernumEffectsRegistry.SideStreakVertexShader);`
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(true);`
- Custom rendering found in ProfanedCirclingRock.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["stretchAmount"].SetValue(4f * lengthScalar);`
- Custom rendering found in HolyFireWall.cs
- Custom rendering found in GuardianIndexManager.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVe`
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- Custom rendering found in EtherealHand.cs
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["pillarVarient"].SetValue(true);`
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.Apply(wall);`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(new Color(255, 255, 150) * Clamp(Projectile.Opacity * 2f, 0.1f`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(Color.LightGoldenrodYellow);`
- Custom rendering found in MagicCrystalShot.cs
- Custom rendering found in MagicSpiralCrystalShot.cs
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.Apply(overlay);`
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);`
- 着色器引用: `PrimitiveRenderer.RenderTrail(positions, new(_ => 300f, colorFunction, Shader: InfernumEffectsRegistry.AreaBorderVertexS`
- 着色器引用: `Shader/Overlay reference in HolyDogmaFireball.cs`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["flipY"].SetValue(false);`
- 着色器引用: `FlameDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVer`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.Shader.Parameters["strongerFade"].SetValue(true);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("flipY", false);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uOpacity", alpha);`
- 着色器引用: `InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(WayfinderSymbol.Colors[2]);`
- 着色器引用: `EnergyColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);`
- Custom rendering found in LingeringHolyFire.cs
- 着色器引用: `InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(shaderLayer);`
- 着色器引用: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("flipY", true);`
- 着色器引用: `Shader/Overlay reference in DefenderGuardianBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(new Color(255, 221, 135));`
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVert`
- 特效代码片段: `using InfernumMode.Common.Graphics.Primitives;`
- 特效代码片段: `internal PrimitiveTrailCopy DashTelegraphDrawer;`
- 特效代码片段: `Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(Color.Orange.ToVector3());`
- 特效代码片段: `Filters.Scene["CrystalDestructionColor"].GetShader().UseIntensity(Utils.GetLerpValue(0.96f, 1.92f, brightnessWidthFactor`
- 特效代码片段: `DashTelegraphDrawer ??= new PrimitiveTrailCopy(c => 65f,`
- 特效代码片段: `null, true, InfernumEffectsRegistry.SideStreakVertexShader);`
- 特效代码片段: `InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- 特效代码片段: `InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);`
- 特效代码片段: `Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;`
- 特效代码片段: `spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in GuardianComboAttackManager.cs
- Screen shake/effects found in ProfanedCirclingRock.cs
- Screen shake/effects found in ProfanedRock.cs
- Screen shake/effects found in DefenderGuardianBehaviorOverride.cs
- Screen shake/effects found in GuardiansRodFailPulse.cs
- 屏幕震动/音效触发: `SoundEngine.PlaySound(ProvidenceBoss.HolyRaySound with { Volume = 3f, Pitch = 0.4f });`