# 史莱姆皇后 (Queen Slime) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `QueenSlime`
- **重写的NPC目标**: `NPCID.QueenSlimeBoss`
- **关联源文件**:
  - `BouncingSlimeProj.cs`
  - `FallingCrystal.cs`
  - `FallingGel.cs`
  - `FallingSpikeSlimeProj.cs`
  - `HallowBlade.cs`
  - `HallowBladeLaserbeam.cs`
  - `HallowCrystalSpike.cs`
  - `HallowLaserbeam.cs`
  - `QueenJewelBeam.cs`
  - `QueenSlimeBehaviorOverride.cs`
  - `QueenSlimeCrown.cs`
  - `QueenSlimeCrystalSpike.cs`
  - `QueenSlimeLightWave.cs`
  - `QueenSlimeSplitFormProj.cs`
  - `SpinningLaserCrystal.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.625f`
- `Phase Ratio Array: Phase2LifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `QueenSlimeAttackType`
- `SpawnAnimation`
- `BasicHops`
- `GeliticArmyStomp`
- `FourThousandBlades`
- `// :4000blades:
            CrystalMaze`
- `SlimeCongregations`
- `CrownLasers`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Teleport above the player on the first frame.*
- **源码注释**: *Charge energy when on the ground.*
- **源码注释**: *Disable contact damage until the slam, since the hops can be so fast as to be unfair.*
- **源码注释**: *Perform ground checks. The attack does not begin until this is finished.*
- **源码注释**: *Teleport above the player and slam down if very far from the target.*
- **源码注释**: *Summon slimes as the anticipation begins.*
- **源码注释**: *Slow downward and make the summoned slimes do things.*
- **源码注释**: *Disable contact damage universally. It is not relevant for this attack.*
- **源码注释**: *Move to the top left/right of the player.*
- **源码注释**: *Disable damage.*
- **源码注释**: *Prevent the attack timer from incrementing if in the split form.*
- **源码注释**: *Disable contact damage.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `BouncingSlimeProj`
  - *实现细节*: `BouncingSlimeProj.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `QueenSlimeSplitFormProj`
  - *实现细节*: `QueenSlimeSplitFormProj.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `FallingCrystal`
  - *实现细节*: `FallingCrystal.cs` (常规渲染 ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `FallingSpikeSlimeProj`
  - *实现细节*: `FallingSpikeSlimeProj.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `QueenSlimeCrystalSpike`
  - *实现细节*: `QueenSlimeCrystalSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SpinningLaserCrystal`
  - *实现细节*: `SpinningLaserCrystal.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HallowCrystalSpike`
  - *实现细节*: `HallowCrystalSpike.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HallowBlade`
  - *实现细节*: `HallowBlade.cs` (常规渲染)
- **弹幕类名/类型**: `FallingGel`
  - *实现细节*: `FallingGel.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `QueenSlimeCrown`
  - *实现细节*: `QueenSlimeCrown.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `QueenJewelBeam`
  - *实现细节*: `QueenJewelBeam.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HallowBladeLaserbeam`
  - *实现细节*: `HallowBladeLaserbeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in HallowBladeLaserbeam.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.SmokyNoise);`
- Custom rendering found in HallowLaserbeam.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue(1f / 2.7f);`
- Custom rendering found in HallowCrystalSpike.cs
- Custom rendering found in QueenSlimeBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in FallingCrystal.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.85f);`
- Custom rendering found in BouncingSlimeProj.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- Custom rendering found in SpinningLaserCrystal.cs
- Custom rendering found in FallingCrystal.cs
- Custom rendering found in QueenSlimeSplitFormProj.cs
- 着色器引用: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- Custom rendering found in FallingSpikeSlimeProj.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.35f);`
- 着色器引用: `Shader/Overlay reference in HallowLaserbeam.cs`
- Custom rendering found in FallingGel.cs
- Custom rendering found in HallowBlade.cs
- Custom rendering found in QueenSlimeCrown.cs
- Custom rendering found in QueenSlimeCrystalSpike.cs
- Custom rendering found in QueenJewelBeam.cs
- Custom rendering found in HallowBladeLaserbeam.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(LaserColorFunction(0f));`
- Custom rendering found in QueenSlimeLightWave.cs
- 着色器引用: `Shader/Overlay reference in QueenSlimeBehaviorOverride.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.FireNoise);`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.LightPink);`
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `GameShaders.Misc["QueenSlime"].Apply();`
- 特效代码片段: `spriteBatch.EnterShaderRegion();`
- 特效代码片段: `GameShaders.Misc["QueenSlime"].Apply(drawData);`
- 特效代码片段: `spriteBatch.ExitShaderRegion();`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in QueenSlimeBehaviorOverride.cs
- Screen shake/effects found in QueenSlimeLightWave.cs
- Screen shake/effects found in HallowCrystalSpike.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.DeathSound = SoundID.NPCDeath1;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SlimeGodCore.ExitSound, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item28, npc.Center);`
- 屏幕震动/音效触发: `if (target.Infernum_Camera().CurrentScreenShakePower < 1.85f)`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 3f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.QueenSlimeExplosionSound, target.Center);`
- 屏幕震动/音效触发: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item163, target.Center);`