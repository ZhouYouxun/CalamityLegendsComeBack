# 噬魂幽花 (Polterghast) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Polterghast`
- **重写的NPC目标**: `ModContent.NPCType<PolterPhantom>()`, `ModContent.NPCType<PolterghastBoss>()`
- **关联源文件**:
  - `ArcingSoul.cs`
  - `CirclingEctoplasm.cs`
  - `EctoplasmShot.cs`
  - `GhostlyVortex.cs`
  - `Light.cs`
  - `NonReturningSoul.cs`
  - `NotSpecialSoul.cs`
  - `PolterghastBehaviorOverride.cs`
  - `PolterghastCloneBehaviorOverride.cs`
  - `PolterghastLeg.cs`
  - `PolterghastWave.cs`
  - `SoulTelegraphLine.cs`
  - `SpinningSoul.cs`
  - `WavySoul.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.35f`
- `Phase2LifeRatio: 0.65f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `PolterghastAttackType`
- `EctoplasmUppercutCharges`
- `LegSwipes`
- `WispCircleCharges`
- `AsgoreRingSoulAttack`
- `ArcingSouls`
- `VortexCharge`
- `SpiritPetal`
- `CloneSplit`
- `DesperationAttack`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Use a ghostly hit sound in the third phase.*
- **源码注释**: *Play phase transition sounds.*
- **源码注释**: *Always disable contact damage if not drawing at all.*
- **源码注释**: *Release a bunch of souls and transition to the final phase.*
- **源码注释**: *Teleport the Polterghast to the desired location.*
- **源码注释**: *Teleport the legs as well.*
- **源码注释**: *Increment the charge counter at the end of charges.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Teleport near the target. A net-update is already fired in the teleport method.*
- **源码注释**: *Cast rings of souls that converge inward on the Polterghast. The player is expected to weave through the open gap.*
- **源码注释**: *This attack is very similar to the flame circles in Asgore's fight from Undertale.*
- **源码注释**: *Disable contact damage and have a much higher DR than usual.*
- **源码注释**: *Start from below if this is the very first attack Polter is performing, for cinematic purposes.*
- **源码注释**: *Initialize the horizontal offset. This gives a bit of variance to the charges.*
- **源码注释**: *Charge and release ectoplasm.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `EctoplasmShot`
  - *实现细节*: `EctoplasmShot.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SpinningSoul`
  - *实现细节*: `SpinningSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GhostlyVortex`
  - *实现细节*: `GhostlyVortex.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SoulTelegraphLine`
  - *实现细节*: `SoulTelegraphLine.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `WavySoul`
  - *实现细节*: `WavySoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `NonReturningSoul`
  - *实现细节*: `NonReturningSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `NotSpecialSoul`
  - *实现细节*: `NotSpecialSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CirclingEctoplasm`
  - *实现细节*: `CirclingEctoplasm.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `Light`
  - *实现细节*: `Light.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ArcingSoul`
  - *实现细节*: `ArcingSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.UseSaturation(i);`
- Custom rendering found in Light.cs
- 着色器引用: `Shader/Overlay reference in PolterghastLeg.cs`
- Custom rendering found in CirclingEctoplasm.cs
- Custom rendering found in ArcingSoul.cs
- 着色器引用: `TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, false, InfernumEffectsRegistry.SideStreakV`
- 着色器引用: `Shader/Overlay reference in PolterghastBehaviorOverride.cs`
- Custom rendering found in NotSpecialSoul.cs
- 着色器引用: `InfernumEffectsRegistry.SideStreakVertexShader.UseImage1(InfernumTextureRegistry.WavyNoise);`
- Custom rendering found in SpinningSoul.cs
- Custom rendering found in EctoplasmShot.cs
- 着色器引用: `npc.Infernum().OptionalPrimitiveDrawer ??= new(c => TelegraphWidthFunction(npc, c), c => TelegraphColorFunction(npc, c),`
- 着色器引用: `Shader/Overlay reference in SoulTelegraphLine.cs`
- 着色器引用: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uImageSize0"].SetValue(circleScale);`
- Custom rendering found in PolterghastBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.CircleCutout2Shader.Apply();`
- 着色器引用: `LimbDrawer ??= new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, InfernumEffectsRegistr`
- 着色器引用: `InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images`
- Custom rendering found in WavySoul.cs
- Custom rendering found in SoulTelegraphLine.cs
- 着色器引用: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uCircleRadius"].SetValue(circleRadius * 1.414f);`
- Custom rendering found in PolterghastLeg.cs
- Custom rendering found in GhostlyVortex.cs
- 着色器引用: `InfernumEffectsRegistry.CircleCutout2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTex`
- 着色器引用: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["ectoplasmCutoffOffsetMax"].SetValue(MathF.Min(circleRadiu`
- 着色器引用: `InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.UseOpacity(Pow(Lerp(0.9f, 0.05f, j / 4f), 4f));`
- Custom rendering found in NonReturningSoul.cs
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer ??= new(c => TelegraphWidthFunction(npc, c), c => TelegraphColorFunction(npc, c),`
- 特效代码片段: `InfernumEffectsRegistry.SideStreakVertexShader.UseImage1(InfernumTextureRegistry.WavyNoise);`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 44);`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion();`
- 特效代码片段: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uImageSize0"].SetValue(circleScale);`
- 特效代码片段: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uCircleRadius"].SetValue(circleRadius * 1.414f);`
- 特效代码片段: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["ectoplasmCutoffOffsetMax"].SetValue(MathF.Min(circleRadiu`
- 特效代码片段: `InfernumEffectsRegistry.CircleCutout2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTex`
- 特效代码片段: `InfernumEffectsRegistry.CircleCutout2Shader.Apply();`
- 特效代码片段: `Main.spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in PolterghastBehaviorOverride.cs
- Screen shake/effects found in PolterghastWave.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.HitSound = InfernumSoundRegistry.PolterghastSoulSound;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(PolterghastBoss.P2Sound with { Volume = 3f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(PolterghastBoss.P3Sound with { Volume = 3f }, target.Center);`
- 屏幕震动/音效触发: `if (SoundEngine.TryGetActiveSound(roarSlot, out var r) && r.IsPlaying)`
- 屏幕震动/音效触发: `if (SoundEngine.TryGetActiveSound(shortRoarSlot, out var sr) && sr.IsPlaying)`
- 屏幕震动/音效触发: `npc.DeathSound = InfernumSoundRegistry.PolterghastDeathEchoSound;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.PolterghastSoulSound, target.Center);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = jitter.Length() * Utils.GetLerpValue(1950f, 1100f, Main.Loc`