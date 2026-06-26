# 月亮领主 (Moon Lord) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `MoonLord`
- **重写的NPC目标**: `NPCID.MoonLordHead`, `NPCID.MoonLordFreeEye`, `NPCID.MoonLordCore`, `NPCID.MoonLordHand`
- **关联源文件**:
  - `LunarAsteroid.cs`
  - `LunarFireball.cs`
  - `LunarFlare.cs`
  - `LunarFlareTelegraph.cs`
  - `MoonLordCoreBehaviorOverride.cs`
  - `MoonLordDeathAnimationHandler.cs`
  - `MoonLordDeathBloodBlob.cs`
  - `MoonLordExplosion.cs`
  - `MoonLordExplosionCinder.cs`
  - `MoonLordHandBehaviorOverride.cs`
  - `MoonLordHeadBehaviorOverride.cs`
  - `MoonLordLeechBehaviorOverride.cs`
  - `MoonLordWave.cs`
  - `NonHomingPhantasmalEye.cs`
  - `PhantasmalBoltBehaviorOverride.cs`
  - `PhantasmalDeathray.cs`
  - `PhantasmalOrb.cs`
  - `PhantasmalSphereBehaviorOverride.cs`
  - `PressurePhantasmalDeathray.cs`
  - `StardustConstellation.cs`
  - `TrueEyeChargeTelegraph.cs`
  - `TrueEyeOfCthulhuBehaviorOverride.cs`
  - `VoidBlackHole.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase3LifeRatio: 0.33333f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.65f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `MoonLordAttackState`
- `SpawnEffects`
- `DeathEffects`
- `PhantasmalSphereHandWaves`
- `PhantasmalBoltEyeBursts`
- `PhantasmalFlareBursts`
- `PhantasmalDeathrays`
- `PhantasmalSpin`
- `PhantasmalRush`
- `PhantasmalDance`
- `PhantasmalBarrage`
- `ExplodingConstellations`
- `PhantasmalWrath`
- `VoidAccretionDisk`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Player variable.*
- **源码注释**: *Clear projectiles, go to the desperation attack, and do some visual effects when ready to enter the final phase.*
- **源码注释**: *Forcefully switch attacks if the mechanism variable for it is activated.*
- **源码注释**: *Don't take damage during spawn effects.*
- **源码注释**: *Sometimes a netUpdate can coincide with the end of the attack phase, resulting in the attack phase being switched by the update*
- **源码注释**: *and then immediately being switched again here. I guess this prevents ML from telegraphing the big laser attacks sometimes*
- **源码注释**: *If the third phase was just reached, use the void accretion disk attack next.*
- **源码注释**: *Use the void accretion disk for every fourth attack when in the third phase.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `LunarFlare`
  - *实现细节*: `LunarFlare.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LunarAsteroid`
  - *实现细节*: `LunarAsteroid.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LunarFlareTelegraph`
  - *实现细节*: `LunarFlareTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MoonLordDeathBloodBlob`
  - *实现细节*: `MoonLordDeathBloodBlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `MoonLordExplosion`
  - *实现细节*: `MoonLordExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PhantasmalDeathray`
  - *实现细节*: `PhantasmalDeathray.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `VoidBlackHole`
  - *实现细节*: `VoidBlackHole.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `MoonLordExplosionCinder`
  - *实现细节*: `MoonLordExplosionCinder.cs` (常规渲染)
- **弹幕类名/类型**: `MoonLordDeathAnimationHandler`
  - *实现细节*: `MoonLordDeathAnimationHandler.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `StardustConstellation`
  - *实现细节*: `StardustConstellation.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LunarFireball`
  - *实现细节*: `LunarFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `NonHomingPhantasmalEye`
  - *实现细节*: `NonHomingPhantasmalEye.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `PhantasmalOrb`
  - *实现细节*: `PhantasmalOrb.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `TrueEyeChargeTelegraph`
  - *实现细节*: `TrueEyeChargeTelegraph.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 拥有专属的Boss登场展示界面 (Custom Boss Intro Screen)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in VoidBlackHole.cs
- Custom rendering found in MoonLordExplosion.cs
- Custom rendering found in LunarFlare.cs
- Custom rendering found in NonHomingPhantasmalEye.cs
- Custom rendering found in MoonLordCoreBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in MoonLordDeathAnimationHandler.cs`
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(1.4f);`
- 着色器引用: `Shader/Overlay reference in TrueEyeChargeTelegraph.cs`
- Custom rendering found in MoonLordDeathBloodBlob.cs
- 着色器引用: `Shader/Overlay reference in VoidBlackHole.cs`
- Custom rendering found in MoonLordLeechBehaviorOverride.cs
- Custom rendering found in PhantasmalSphereBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in PressurePhantasmalDeathray.cs`
- Custom rendering found in StardustConstellation.cs
- Custom rendering found in MoonLordDeathAnimationHandler.cs
- Custom rendering found in LunarAsteroid.cs
- Custom rendering found in PhantasmalOrb.cs
- 着色器引用: `var flame = InfernumEffectsRegistry.FlameVertexShader;`
- Custom rendering found in TrueEyeChargeTelegraph.cs
- 着色器引用: `Shader/Overlay reference in PhantasmalDeathray.cs`
- Custom rendering found in LunarFireball.cs
- Custom rendering found in MoonLordHandBehaviorOverride.cs
- Custom rendering found in PhantasmalDeathray.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- Custom rendering found in LunarFlareTelegraph.cs
- Custom rendering found in MoonLordHeadBehaviorOverride.cs
- Custom rendering found in TrueEyeOfCthulhuBehaviorOverride.cs
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in MoonLordWave.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `public const int IntroSoundLength = 107;`
- 屏幕震动/音效触发: `ref float introSoundTimer = ref npc.Infernum().ExtraAI[10];`
- 屏幕震动/音效触发: `if (introSoundTimer < IntroSoundLength)`
- 屏幕震动/音效触发: `if (introSoundTimer == 0f)`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.MoonLordIntroSound, target.Center);`
- 屏幕震动/音效触发: `introSoundTimer++;`
- 屏幕震动/音效触发: `var sounds = new SoundStyle[]`
- 屏幕震动/音效触发: `SoundID.Zombie92,`
- 屏幕震动/音效触发: `SoundID.Zombie93,`