# 独眼巨鹿 (Deerclops) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Deerclops`
- **重写的NPC目标**: `NPCID.Deerclops`
- **关联源文件**:
  - `AcceleratingShadowHand.cs`
  - `ArenaIcicle.cs`
  - `DeathAnimationShadowHand.cs`
  - `DeerclopsBehaviorOverride.cs`
  - `DeerclopsEyeLaserbeam.cs`
  - `DeerclopsP2Wave.cs`
  - `GroundIcicleSpike.cs`
  - `IcicleDrawer.cs`
  - `LightSnuffingHand.cs`
  - `ShadowHandArena.cs`
  - `SpinningShadowHand.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.35f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `DeerclopsAttackState`
- `DecideArena`
- `WalkToTarget`
- `TallIcicles`
- `WideIcicles`
- `BidirectionalIcicleSlam`
- `UpwardDebrisLaunch`
- `TransitionToNextPhase`
- `FeastclopsEyeLaserbeam`
- `AimedAheadShadowHands`
- `DyingBeaconOfLight`
- `DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Transition to the second phase.*
- **源码注释**: *Reset the attack cycle.*
- **源码注释**: *Disappear if the player is really far away or dead.*
- **源码注释**: *Become invincible in phase 1 if the player leaves the spike area.*
- **源码注释**: *Teleport above the target in a burst of snow on the first frame.*
- **源码注释**: *Make the attack go by quicker if really close to the target.*
- **源码注释**: *Don't increment the attack timer until the dig effect has happened.*
- **源码注释**: *Summon shadow hands.*
- **源码注释**: *Shadow hands are launched upwards instead in the third phase.*
- **源码注释**: *Disable contact damage.*
- **源码注释**: *To make the attack better than a simple DPS check the hands will target nearby players if close to deerclops.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Disappear and give the player their loot once gone.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `DeathAnimationShadowHand`
  - *实现细节*: `DeathAnimationShadowHand.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ShadowHandArena`
  - *实现细节*: `ShadowHandArena.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `GroundIcicleSpike`
  - *实现细节*: `GroundIcicleSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SpinningShadowHand`
  - *实现细节*: `SpinningShadowHand.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AcceleratingShadowHand`
  - *实现细节*: `AcceleratingShadowHand.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ArenaIcicle`
  - *实现细节*: `ArenaIcicle.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in DeerclopsBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);`
- Custom rendering found in GroundIcicleSpike.cs
- Custom rendering found in LightSnuffingHand.cs
- 着色器引用: `Shader/Overlay reference in DeerclopsEyeLaserbeam.cs`
- Custom rendering found in IcicleDrawer.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);`
- Custom rendering found in ShadowHandArena.cs
- Custom rendering found in DeathAnimationShadowHand.cs
- 着色器引用: `Shader/Overlay reference in IcicleDrawer.cs`
- Custom rendering found in DeerclopsEyeLaserbeam.cs
- 着色器引用: `Shader/Overlay reference in ShadowHandArena.cs`
- Custom rendering found in AcceleratingShadowHand.cs
- Custom rendering found in ArenaIcicle.cs
- 着色器引用: `Shader/Overlay reference in DeerclopsBehaviorOverride.cs`
- 着色器引用: `var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;`
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- Custom rendering found in SpinningShadowHand.cs
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion();`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(dragPortalAppearInterpolant);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.BlueViolet);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.SlateGray);`
- 特效代码片段: `GameShaders.Misc["CalamityMod:DoGPortal"].Apply();`
- 特效代码片段: `Main.spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in DeerclopsBehaviorOverride.cs
- Screen shake/effects found in DeerclopsP2Wave.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `target.Calamity().GeneralScreenShakePower = 10f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Volume = 1.9f }, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Volume = 1.9f }, npc.Bottom);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, npc.Bottom);`
- 屏幕震动/音效触发: `var sound = shadowHandCount > 0 ? InfernumSoundRegistry.DeerclopsRubbleAttackDistortedSound : SoundID.DeerclopsRubbleAtt`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(sound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);`