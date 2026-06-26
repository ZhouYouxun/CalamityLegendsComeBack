# 西格纳斯 (Signus) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Signus`
- **重写的NPC目标**: `ModContent.NPCType<SignusBoss>()`
- **关联源文件**:
  - `CosmicExplosion.cs`
  - `CosmicKunai.cs`
  - `CosmicMine.cs`
  - `DarkCosmicBomb.cs`
  - `EldritchScythe.cs`
  - `ShadowDashTelegraph.cs`
  - `ShadowSlash.cs`
  - `SignusBehaviorOverride.cs`
  - `SignusMusicSceneInfernum.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatio: 0.3f`
- `Phase2LifeRatio: 0.7f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `SignusAttackType`
- `Patrol`
- `KunaiDashes`
- `ScytheTeleportThrow`
- `ShadowDash`
- `FastHorizontalCharge`
- `CosmicFlameChargeBombs`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Disable damage.*
- **源码注释**: *Why don't turrets check for don't take damage..*
- **源码注释**: *Teleport to the side of the target.*
- **源码注释**: *Disappear if Signus is in the gardens and the player gets too close.*
- **源码注释**: *Fade in after an initial teleport.*
- **源码注释**: *Select a location to teleport near the target.*
- **源码注释**: *Perform movement during the charge.*
- **源码注释**: *Fade out after the charge has completed.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *Teleport near the target and fade in.*
- **源码注释**: *Charge quickly at the target, slow down, and create a bunch of scythes.*
- **源码注释**: *Look at the player and create the telegraph line after the redirect is over.*
- **源码注释**: *Speed up after the initial charge has happened. This does not apply once the black screen fade has concluded.*
- **源码注释**: *Don't do damage after the telegraph is gone.*
- **源码注释**: *Teleport in front of the target and create a mine between Signus and them.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `CosmicKunai`
  - *实现细节*: `CosmicKunai.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `EldritchScythe`
  - *实现细节*: `EldritchScythe.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkCosmicBomb`
  - *实现细节*: `DarkCosmicBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CosmicExplosion`
  - *实现细节*: `CosmicExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ShadowDashTelegraph`
  - *实现细节*: `ShadowDashTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ShadowSlash`
  - *实现细节*: `ShadowSlash.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CosmicMine`
  - *实现细节*: `CosmicMine.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in CosmicExplosion.cs`
- Custom rendering found in CosmicMine.cs
- Custom rendering found in ShadowSlash.cs
- Custom rendering found in SignusBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in SignusBehaviorOverride.cs`
- Custom rendering found in EldritchScythe.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.45f);`
- Custom rendering found in ShadowDashTelegraph.cs
- 着色器引用: `Shader/Overlay reference in ShadowSlash.cs`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseImage1("Images/Misc/Perlin");`
- Custom rendering found in DarkCosmicBomb.cs
- 着色器引用: `var slashShader = InfernumEffectsRegistry.DoGDashIndicatorVertexShader;`
- Custom rendering found in CosmicKunai.cs
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, InfernumEffectsRegistry.FireVertex`
- Custom rendering found in CosmicExplosion.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in DarkCosmicBomb.cs
- Screen shake/effects found in SignusBehaviorOverride.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.VassalTeleportSound, npc.Center);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 屏幕震动/音效触发: `int ambienceMusicID = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/SignusAmbience");`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.SignusSlashSound with { Volume = 0.3f, Pitch = 0.4f, MaxInstances = 20 }, ta`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.SignusFlameBombShootSound with { Volume = 0.45f }, npc.Center);`