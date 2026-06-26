# 硫磺火元素 (Brimstone Elemental) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `BrimstoneElemental`
- **重写的NPC目标**: `ModContent.NPCType<BrimmyNPC>()`
- **关联源文件**:
  - `Brimrose.cs`
  - `BrimstoneDeathray.cs`
  - `BrimstoneElementalBehaviorOverride.cs`
  - `BrimstoneFireball.cs`
  - `BrimstonePetal.cs`
  - `BrimstonePetal2.cs`
  - `BrimstoneRose.cs`
  - `BrimstoneSkull.cs`
  - `BrimstoneTelegraphRay.cs`
  - `HomingBrimstoneSkull.cs`
  - `RedFlameTelegraph.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.5f`
- `Phase Ratio Array: Phase2LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `BrimmyAttackType`
- `FlameTeleportBombardment`
- `BrimstoneRoseBurst`
- `FlameChargeSkullBlasts`
- `GrimmBulletHellCopyLmao`
- `EyeLaserbeams`
- `DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Adjust sprite direction to look at the player.*
- **源码注释**: *Create particles at the teleport position.*
- **源码注释**: *Go to the next attack substate and teleport once completely invisible.*
- **源码注释**: *Hurt the player if they walk into the vines.*
- **源码注释**: *Create the charge dust.*
- **源码注释**: *Teleport near the target and immediately go to the next attack state.*
- **源码注释**: *Charge prior to firing.*
- **源码注释**: *Explode and go to the next attack state once done charging.*
- **源码注释**: *Look at the player.*
- **源码注释**: *Sit in place for a bit prior to going to the next attack.*
- **源码注释**: *Teleport below the player.*
- **源码注释**: *Hover near the player for a bit and create charge dust.*
- **源码注释**: *Go to the next attack state after hovering for a small amount of time.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Teleport above the player on the first frame.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `BrimstoneRose`
  - *实现细节*: `BrimstoneRose.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BrimstonePetal`
  - *实现细节*: `BrimstonePetal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BrimstoneFireball`
  - *实现细节*: `BrimstoneFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `RedFlameTelegraph`
  - *实现细节*: `RedFlameTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BrimstonePetal2`
  - *实现细节*: `BrimstonePetal2.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BrimstoneSkull`
  - *实现细节*: `BrimstoneSkull.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HomingBrimstoneSkull`
  - *实现细节*: `HomingBrimstoneSkull.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `Brimrose`
  - *实现细节*: `Brimrose.cs` (常规渲染)

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in BrimstoneDeathray.cs`
- Custom rendering found in BrimstoneSkull.cs
- 着色器引用: `Shader/Overlay reference in RedFlameTelegraph.cs`
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFire);`
- Custom rendering found in BrimstoneDeathray.cs
- 着色器引用: `InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(middleColor2 * 2f);`
- Custom rendering found in HomingBrimstoneSkull.cs
- Custom rendering found in RedFlameTelegraph.cs
- 着色器引用: `Shader/Overlay reference in BrimstoneTelegraphRay.cs`
- Custom rendering found in BrimstonePetal.cs
- Custom rendering found in BrimstoneElementalBehaviorOverride.cs
- Custom rendering found in BrimstonePetal2.cs
- Custom rendering found in BrimstoneFireball.cs
- Custom rendering found in BrimstoneRose.cs
- 着色器引用: `Shader/Overlay reference in BrimstoneElementalBehaviorOverride.cs`
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVertexShader`
- 特效代码片段: `Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion(BlendState.Additive);`
- 特效代码片段: `Main.spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in BrimstoneElementalBehaviorOverride.cs
- 屏幕震动/音效触发: `using CalamityMod.Sounds;`
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 3f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item20, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CalamitasEnchantUI.EnchSound with { Pitch = 0.15f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item72, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, npc.Center);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 1f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item100, npc.Center);`