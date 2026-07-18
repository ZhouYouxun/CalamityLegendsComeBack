# 石巨人 (Golem) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Golem`
- **重写的NPC目标**: `NPCID.Golem`, `NPCID.GolemFistLeft`, `NPCID.GolemHeadFree`, `NPCID.GolemHead`, `NPCID.GolemFistRight`
- **关联源文件**:
  - `FistBullet.cs`
  - `FistBulletTelegraph.cs`
  - `GolemArenaPlatform.cs`
  - `GolemBodyBehaviorOverride.cs`
  - `GolemEyeLaserRay.cs`
  - `GolemFistLeft.cs`
  - `GolemFistLeftBehaviorOverride.cs`
  - `GolemFistRight.cs`
  - `GolemFistRightBehaviorOverride.cs`
  - `GolemFreeHeadBehaviorOverride.cs`
  - `GolemHeadBehaviorOverride.cs`
  - `GolemLaser.cs`
  - `GroundFireCrystal.cs`
  - `SpikeTrap.cs`
  - `StationarySpikeTrap.cs`
  - `ThermalDeathray.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatio: 0.3f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.6f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `GolemAttackState`
- `FloorFire`
- `FistSpin`
- `SpikeTrapWaves`
- `HeatRay`
- `SpinLaser`
- `Slingshot`
- `SpikeRush`
- `LandingState`
- `SummonDelay`
- `BIGSHOT`
- `BadTime`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Constantly reset damage.*
- **源码注释**: *Create a boom as a phase transition for phase 3.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Enter the second phase.*
- **源码注释**: *Mark this so that if the player re-enters the arena then the AI will know to resync*
- **源码注释**: *It's fine if the head was unattached before enraging, the attack will continue like normal*
- **源码注释**: *Attack swapping*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Summon crystals on the floor that accelerate upward.*
- **源码注释**: *As well as on the sides of the arena in the third phase.*
- **源码注释**: *Sit in place until the next attack.*
- **源码注释**: *Create platforms below the target in the third phase.*
- **源码注释**: *Create platforms below the target in the second phase.*
- **源码注释**: *Summon waves of spikes.*
- **源码注释**: *Select the next attack shortly after the laser goes away.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `FistBulletTelegraph`
  - *实现细节*: `FistBulletTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GroundFireCrystal`
  - *实现细节*: `GroundFireCrystal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ThermalDeathray`
  - *实现细节*: `ThermalDeathray.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `FistBullet`
  - *实现细节*: `FistBullet.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `StationarySpikeTrap`
  - *实现细节*: `StationarySpikeTrap.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SpikeTrap`
  - *实现细节*: `SpikeTrap.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `GolemLaser`
  - *实现细节*: `GolemLaser.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in GolemEyeLaserRay.cs`
- Custom rendering found in SpikeTrap.cs
- Custom rendering found in FistBullet.cs
- Custom rendering found in GolemFreeHeadBehaviorOverride.cs
- Custom rendering found in GolemHeadBehaviorOverride.cs
- Custom rendering found in GolemBodyBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.1f);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- Custom rendering found in GroundFireCrystal.cs
- Custom rendering found in GolemLaser.cs
- Custom rendering found in GolemFistLeftBehaviorOverride.cs
- Custom rendering found in ThermalDeathray.cs
- Custom rendering found in GolemFistLeft.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVert`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) `
- 着色器引用: `Shader/Overlay reference in ThermalDeathray.cs`
- 着色器引用: `Shader/Overlay reference in GolemBodyBehaviorOverride.cs`
- Custom rendering found in StationarySpikeTrap.cs
- Custom rendering found in GolemFistRight.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(1.4f);`
- Custom rendering found in FistBulletTelegraph.cs
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `public static float PrimitiveWidthFunction(float _) => 132f;`
- 特效代码片段: `public static Color PrimitiveTrailColor(NPC npc, float completionRatio)`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer ??= new(PrimitiveWidthFunction, c => PrimitiveTrailColor(npc, c), null, true, Gam`
- 特效代码片段: `GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");`
- 特效代码片段: `npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 51);`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in GolemBodyBehaviorOverride.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.GolemSansSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.GolemSpamtonSound, target.Center);`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item14, npc.Bottom);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.GolemGroundHitSound, npc.Bottom);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = 12f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, leftImpactPoint);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, rightImpactPoint);`