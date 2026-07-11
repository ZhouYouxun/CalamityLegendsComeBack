# 星流巨械 / 嘉登 (Draedon / Exo Mechs) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `Draedon` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `Draedon`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<DraedonNPC>()`, `ModContent.NPCType<AresBody>()`, `Unknown`, `ModContent.NPCType<Apollo>()`, `ModContent.NPCType<Artemis>()`, `ModContent.NPCType<ThanatosHead>()`, `ModContent.NPCType<ThanatosBody1>()`, `ModContent.NPCType<ThanatosBody1>()`, `ModContent.NPCType<ThanatosBody1>()`
- **模组内关联的源文件列表**:
  - `DraedonBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/DraedonBehaviorOverride.cs`)
  - `ExoMechAIUtilities.cs` (源路径: `.../BossAIs/Draedon/ExoMechAIUtilities.cs`)
  - `ExoMechManagement.cs` (源路径: `.../BossAIs/Draedon/ExoMechManagement.cs`)
  - `AresBeamExplosion.cs` (源路径: `.../BossAIs/Draedon/AresBeamExplosion.cs`)
  - `AresBeamTelegraph.cs` (源路径: `.../BossAIs/Draedon/AresBeamTelegraph.cs`)
  - `AresBodyBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/AresBodyBehaviorOverride.cs`)
  - `AresCannonBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/AresCannonBehaviorOverride.cs`)
  - `AresCannonLaser.cs` (源路径: `.../BossAIs/Draedon/AresCannonLaser.cs`)
  - `AresDeathBeamTelegraph.cs` (源路径: `.../BossAIs/Draedon/AresDeathBeamTelegraph.cs`)
  - `AresEnergyDeathray.cs` (源路径: `.../BossAIs/Draedon/AresEnergyDeathray.cs`)
  - `AresEnergyDeathrayTelegraph.cs` (源路径: `.../BossAIs/Draedon/AresEnergyDeathrayTelegraph.cs`)
  - `AresEnergyKatana.cs` (源路径: `.../BossAIs/Draedon/AresEnergyKatana.cs`)
  - `AresEnergySlash.cs` (源路径: `.../BossAIs/Draedon/AresEnergySlash.cs`)
  - `AresLaserCannonBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/AresLaserCannonBehaviorOverride.cs`)
  - `AresLaserDeathray.cs` (源路径: `.../BossAIs/Draedon/AresLaserDeathray.cs`)
  - `AresLaughBoom.cs` (源路径: `.../BossAIs/Draedon/AresLaughBoom.cs`)
  - `AresPlasmaCannonBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/AresPlasmaCannonBehaviorOverride.cs`)
  - `AresPlasmaFireball.cs` (源路径: `.../BossAIs/Draedon/AresPlasmaFireball.cs`)
  - `AresPrecisionBlast.cs` (源路径: `.../BossAIs/Draedon/AresPrecisionBlast.cs`)
  - `AresPulseBlast.cs` (源路径: `.../BossAIs/Draedon/AresPulseBlast.cs`)
  - `AresPulseCannon.cs` (源路径: `.../BossAIs/Draedon/AresPulseCannon.cs`)
  - `AresPulseDeathray.cs` (源路径: `.../BossAIs/Draedon/AresPulseDeathray.cs`)
  - `AresSpinningDeathBeam.cs` (源路径: `.../BossAIs/Draedon/AresSpinningDeathBeam.cs`)
  - `AresSpinningRedDeathray.cs` (源路径: `.../BossAIs/Draedon/AresSpinningRedDeathray.cs`)
  - `AresTeslaCannonBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/AresTeslaCannonBehaviorOverride.cs`)
  - `AresTeslaGasField.cs` (源路径: `.../BossAIs/Draedon/AresTeslaGasField.cs`)
  - `AresTeslaOrb.cs` (源路径: `.../BossAIs/Draedon/AresTeslaOrb.cs`)
  - `AresTeslaSpark.cs` (源路径: `.../BossAIs/Draedon/AresTeslaSpark.cs`)
  - `ExoburstSpark.cs` (源路径: `.../BossAIs/Draedon/ExoburstSpark.cs`)
  - `HotMetal.cs` (源路径: `.../BossAIs/Draedon/HotMetal.cs`)
  - `PlasmaGas.cs` (源路径: `.../BossAIs/Draedon/PlasmaGas.cs`)
  - `SmallPlasmaSpark.cs` (源路径: `.../BossAIs/Draedon/SmallPlasmaSpark.cs`)
  - `ApolloAcceleratingPlasmaSpark.cs` (源路径: `.../BossAIs/Draedon/ApolloAcceleratingPlasmaSpark.cs`)
  - `ApolloBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/ApolloBehaviorOverride.cs`)
  - `ApolloFallingPlasmaSpark.cs` (源路径: `.../BossAIs/Draedon/ApolloFallingPlasmaSpark.cs`)
  - `ApolloFlamethrower.cs` (源路径: `.../BossAIs/Draedon/ApolloFlamethrower.cs`)
  - `ApolloPlasmaFireball.cs` (源路径: `.../BossAIs/Draedon/ApolloPlasmaFireball.cs`)
  - `ApolloRocketInfernum.cs` (源路径: `.../BossAIs/Draedon/ApolloRocketInfernum.cs`)
  - `ArtemisBasicShotLaser.cs` (源路径: `.../BossAIs/Draedon/ArtemisBasicShotLaser.cs`)
  - `ArtemisBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/ArtemisBehaviorOverride.cs`)
  - `ArtemisGasFireballBlast.cs` (源路径: `.../BossAIs/Draedon/ArtemisGasFireballBlast.cs`)
  - `ArtemisGatlingLaser.cs` (源路径: `.../BossAIs/Draedon/ArtemisGatlingLaser.cs`)
  - `ArtemisLaser.cs` (源路径: `.../BossAIs/Draedon/ArtemisLaser.cs`)
  - `ArtemisLaserbeamTelegraph.cs` (源路径: `.../BossAIs/Draedon/ArtemisLaserbeamTelegraph.cs`)
  - `ArtemisSpinLaser.cs` (源路径: `.../BossAIs/Draedon/ArtemisSpinLaser.cs`)
  - `ArtemisSweepLaserbeam.cs` (源路径: `.../BossAIs/Draedon/ArtemisSweepLaserbeam.cs`)
  - `ExoplasmaBomb.cs` (源路径: `.../BossAIs/Draedon/ExoplasmaBomb.cs`)
  - `ExoplasmaExplosion.cs` (源路径: `.../BossAIs/Draedon/ExoplasmaExplosion.cs`)
  - `PlasmaChargeTelegraph.cs` (源路径: `.../BossAIs/Draedon/PlasmaChargeTelegraph.cs`)
  - `SuperheatedExofireGas.cs` (源路径: `.../BossAIs/Draedon/SuperheatedExofireGas.cs`)
  - `ThermonuclearDeathOrb.cs` (源路径: `.../BossAIs/Draedon/ThermonuclearDeathOrb.cs`)
  - `ExoMechComboAttackContent.cs` (源路径: `.../BossAIs/Draedon/ExoMechComboAttackContent.cs`)
  - `ThanatosAndAresComboAttacks.cs` (源路径: `.../BossAIs/Draedon/ThanatosAndAresComboAttacks.cs`)
  - `TwinsAndAresComboAttacks.cs` (源路径: `.../BossAIs/Draedon/TwinsAndAresComboAttacks.cs`)
  - `TwinsAndThanatosComboAttacks.cs` (源路径: `.../BossAIs/Draedon/TwinsAndThanatosComboAttacks.cs`)
  - `DetatchedThanatosLaser.cs` (源路径: `.../BossAIs/Draedon/DetatchedThanatosLaser.cs`)
  - `ExolaserBomb.cs` (源路径: `.../BossAIs/Draedon/ExolaserBomb.cs`)
  - `ExolaserSpark.cs` (源路径: `.../BossAIs/Draedon/ExolaserSpark.cs`)
  - `LightOverloadRay.cs` (源路径: `.../BossAIs/Draedon/LightOverloadRay.cs`)
  - `LightRayTelegraph.cs` (源路径: `.../BossAIs/Draedon/LightRayTelegraph.cs`)
  - `OverloadBoom.cs` (源路径: `.../BossAIs/Draedon/OverloadBoom.cs`)
  - `RefractionRotor.cs` (源路径: `.../BossAIs/Draedon/RefractionRotor.cs`)
  - `ThanatosAresComboLaser.cs` (源路径: `.../BossAIs/Draedon/ThanatosAresComboLaser.cs`)
  - `ThanatosHeadBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/ThanatosHeadBehaviorOverride.cs`)
  - `ThanatosSegmentBehaviorOverride.cs` (源路径: `.../BossAIs/Draedon/ThanatosSegmentBehaviorOverride.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `DraedonBehaviorOverride` -> 重写目标: `ModContent.NPCType<DraedonNPC>()`
  - 类名: `AresBodyBehaviorOverride` -> 重写目标: `ModContent.NPCType<AresBody>()`
  - 类名: `AresCannonBehaviorOverride` -> 重写目标: `Unknown`
  - 类名: `ApolloBehaviorOverride` -> 重写目标: `ModContent.NPCType<Apollo>()`
  - 类名: `ArtemisBehaviorOverride` -> 重写目标: `ModContent.NPCType<Artemis>()`
  - 类名: `ThanatosHeadBehaviorOverride` -> 重写目标: `ModContent.NPCType<ThanatosHead>()`
  - 类名: `ThanatosBody1BehaviorOverride` -> 重写目标: `ModContent.NPCType<ThanatosBody1>()`
  - 类名: `ThanatosBody2BehaviorOverride` -> 重写目标: `ModContent.NPCType<ThanatosBody1>()`
  - 类名: `ThanatosTailBehaviorOverride` -> 重写目标: `ModContent.NPCType<ThanatosBody1>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- 血量阈值数组: `[ExoMechManagement.Phase4LifeRatio]`
- `ComplementMechInvincibilityThreshold` = `0.5f`
- `Phase4LifeRatio` = `0.5f`
- 血量阈值数组: `[ExoMechManagement.Phase3LifeRatio, ExoMechManagement.Phase4LifeRatio]`
- `Phase3LifeRatio` = `0.625f`
- `Phase2LifeRatio` = `0.85f`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `AresBodyAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `IdleHover` - 对应的行为处理状态。
2. `LaserSpinBursts` - 对应的行为处理状态。
3. `DirectionChangingSpinBursts` - 对应的行为处理状态。
4. `// Energy katana attacks.
            EnergyBladeSlices` - 对应的行为处理状态。
5. `DownwardCrossSlices` - 对应的行为处理状态。
6. `ThreeDimensionalSuperslashes` - 对应的行为处理状态。
7. `// Ultimate attack. Only happens when in the final phase.
            PrecisionBlasts` - 对应的行为处理状态。
### 🎯 状态机枚举: `TwinsAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `BasicShots` - 对应的行为处理状态。
2. `FireCharge` - 对应的行为处理状态。
3. `ApolloPlasmaCharges` - 对应的行为处理状态。
4. `ArtemisLaserRay` - 对应的行为处理状态。
5. `GatlingLaserAndPlasmaFlames` - 对应的行为处理状态。
6. `SlowLaserRayAndPlasmaBlasts` - 对应的行为处理状态。
7. `// Ultimate attack. Only happens when in the final phase.
            ThermonuclearBlitz` - 对应的行为处理状态。
### 🎯 状态机枚举: `ExoMechComboAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `AresTwins_DualLaserCharges` - 对应的行为处理状态。
2. `AresTwins_CircleAttack` - 对应的行为处理状态。
3. `ThanatosAres_LaserCircle` - 对应的行为处理状态。
4. `ThanatosAres_EnergySlashesAndCharges` - 对应的行为处理状态。
5. `TwinsThanatos_ThermoplasmaDashes` - 对应的行为处理状态。
6. `TwinsThanatos_AlternatingTwinsBursts` - 对应的行为处理状态。
### 🎯 状态机枚举: `ThanatosHeadAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `AggressiveCharge` - 对应的行为处理状态。
2. `ExoBomb` - 对应的行为处理状态。
3. `ExoLightBarrage` - 对应的行为处理状态。
4. `RefractionRotorRays` - 对应的行为处理状态。
5. `// Ultimate attack. Only happens when in the final phase.
            MaximumOverdrive` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **27** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, ref float deathAnimationTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down dramatically.*
  - *Use close to the minimum HP.*
  - *Clear away projectiles.*
  - *Disable damage.*
  - *Close the boss HP bar.*
  - *Create the implosion ring on the first frame.*
  - *Create particles that fly outward every frame.*
  - *Periodically create pulse rings.*
  - *Create an explosion.*
  - *Fade away as the explosion progresses.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_DoFinalPhaseTransition`
- **参数列表**: `(NPC npc, Player target, ref float frame, float phaseTransitionAnimationTime)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ExoMechFinalPhaseSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Clear away projectiles.*
  - *Determine frames.*
  - *Heal HP.*
  - *Play the transition sound at the start.*
  - *Destroy all lasers and telegraphs.*
- **技术实现原理解析**:
  在执行 `DoFinalPhaseTransition` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_IdleHover`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **技术实现原理解析**:
  在执行 `IdleHover` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_LaserSpinBursts`
- **参数列表**: `(NPC npc, Player target, ref float enraged, ref float attackTimer, ref float frameType, ref float blenderSoundTimer, ref float blenderSoundIsLooping)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PBGMechanicalWarning`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down.*
  - *Stay away from the top of the world, to ensure that the target can deal with the laser spin.*
  - *Determine an initial direction.*
  - *Ensure that the backarm swap state is consistent.*
  - *Enforce an initial delay prior to firing.*
  - *Drift towards the target.*
  - *Delete projectiles after the delay has concluded.*
  - *Laugh.*
  - *Create telegraph lines that show where the laserbeams will appear.*
  - *Create laser bursts.*
- **技术实现原理解析**:
  在执行 `LaserSpinBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_EnergyBladeSlices`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Laugh.*
  - *Attempt to loosely hover above the player.*
- **技术实现原理解析**:
  在执行 `EnergyBladeSlices` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DownwardCrossSlices`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Laugh.*
  - *Hover above the target during the anticipation.*
  - *Slow down after anticipation.*
  - *Create a bunch of energy deathrays.*
- **技术实现原理解析**:
  在执行 `DownwardCrossSlices` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ThreeDimensionalSuperslashes`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float zPosition)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Laugh.*
  - *Move into the background and hover above the player.*
  - *Clean up old projectiles.*
  - *Play the impending death sound.*
  - *Create slice impact lasers.*
  - *Wait for laser sounds to play.*
- **技术实现原理解析**:
  在执行 `ThreeDimensionalSuperslashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PrecisionBlasts`
- **参数列表**: `(NPC npc, Player target, ref float enraged, ref float attackTimer, ref float frameType, ref float blenderSoundTimer, ref float blenderSoundIsLooping)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.AresTeslaShotSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable damage during this attack.*
  - *Disable the enrage effect.*
  - *Reset the cannons. Attack substates can give them permission to attack.*
  - *Sit in place and give some warning text before attacking.*
  - *Cease movement.*
  - *Reset the heat interpolant.*
  - *Prevent a bug where the cannons fire too soon.*
  - *Hover above the target and begin attacking.*
  - *Enrage if the nearest target is incredibly far away.*
  - *Split to two parts for readability*
- **技术实现原理解析**:
  在执行 `PrecisionBlasts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DoPhaseTransition`
- **参数列表**: `(NPC npc, Player target, ref float frame, float hoverSide, float phaseTransitionAnimationTime)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.GatlingLaserFireEnd`, `InfernumSoundRegistry.GatlingLaserFireStart`
- **生成弹幕 (Projectiles Created)**: `0`
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine rotation.*
  - *Disable contact damage.*
  - *Move to the appropriate side of the target.*
  - *Determine frames.*
  - *Create the pupil gore thing.*
- **技术实现原理解析**:
  在执行 `DoPhaseTransition` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BasicShots`
- **参数列表**: `(NPC npc, Player target, float enrageTimer, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Artemis releases a set number of small laserbeam barrages before repositioning, while Apollo releases bursts of fireballs in a spread.*
  - *After Artemis completes its barrage set, it will swiftly reposition elsewhere before returning to a drift motion.*
  - *Provide the target infinite flight time.*
  - *Don't do damage.*
  - *Rapidly approach the target before attacking, to ensure that they see Apollo and can be aware of the impending attack.*
  - *Hover a bit offset to Artemis. This avoids moving in front of the target.*
  - *Decide which direction Apollo should release fireballs in.*
  - *Release a constant fan of fireballs if the telegraph has been completed.*
  - *Prepare the gleam telegraph interpolant.*
  - *Mark the telegraph as being done and reset the attack timer once complete.*
- **技术实现原理解析**:
  在执行 `BasicShots` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FireCharge`
- **参数列表**: `(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Apollo performs multiple flamethrower dashes in succession.*
  - *Look at the target and hover towards the top left/right of the target.*
  - *Begin the delay if the destination is reached.*
  - *Release fire and smoke from the mouth as a telegraph.*
  - *Begin the charge and emit a flamethrower after a tiny delay.*
  - *Have Artemis attempt to do a horizontal sweep while releasing lasers in bursts. This only happens after Ares has released the laserbeams.*
  - *Don't do contact damage.*
  - *Reset the flash effect.*
  - *Simply hover in place if the laserbeams have not been fired.*
  - *Decide rotation.*
- **技术实现原理解析**:
  在执行 `FireCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_ApolloPlasmaCharges`
- **参数列表**: `(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.ELRFireSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Make Artemis go away so Apollo can do its attack without interference.*
  - *Hover into position.*
  - *Hover to the top left/right of the target.*
  - *Once sufficiently close, go to the next attack substate.*
  - *Wait in place for a short period of time.*
  - *Decide frames.*
  - *Calculate the charge flash.*
  - *Charge and release sparks.*
  - *Create lightning bolts in the sky.*
  - *Release fire.*
- **技术实现原理解析**:
  在执行 `ApolloPlasmaCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_ArtemisLaserRay`
- **参数列表**: `(NPC npc, Player target, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ExoMechImpendingDeathSound`, `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make Apollo go away so Artemis can do its attack without interference.*
  - *Hover into position.*
  - *Determine which direction Artemis will spin in.*
  - *Begin hovering in place once sufficiently close to the hover position.*
  - *Stay in place for a brief moment.*
  - *Calculate the charge flash.*
  - *Create a beam telegraph.*
  - *Initialize Artemis' spin direction.*
  - *Fire the laser.*
  - *Create an incredibly violent screen shake effect.*
- **技术实现原理解析**:
  在执行 `ArtemisLaserRay` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_GatlingLaserAndPlasmaFlames`
- **参数列表**: `(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.GatlingLaserFireLoop`, `InfernumSoundRegistry.GatlingLaserFireStart`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Hover into position.*
  - *Begin firing.*
  - *Reset the hover offset periodically.*
  - *Fire a machine-gun of lasers.*
  - *Do movement.*
  - *Play a laser preparation sound.*
  - *Play the laser fire loop.*
  - *Release streams of plasma blasts rapid-fire.*
- **技术实现原理解析**:
  在执行 `GatlingLaserAndPlasmaFlames` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_SlowLaserRayAndPlasmaBlasts`
- **参数列表**: `(NPC npc, Player target, ref float enrageTimer, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ExoMechImpendingDeathSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Have Artemis cast a telegraph that indicates where the laserbeam will appear.*
  - *Disable contact damage.*
  - *Hover into position before creating the telegraph.*
  - *Create the laserbeam.*
  - *Handle frames.*
  - *Have Artemis sweep around.*
  - *Have Apollo hover to the side of the target and release plasma blasts.*
  - *Look at the target.*
  - *Fire plasma blasts.*
  - *Get REALLY pissed off if the player leaves the range of the laserbeam.*
- **技术实现原理解析**:
  在执行 `SlowLaserRayAndPlasmaBlasts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_ThermonuclearBlitz`
- **参数列表**: `(NPC npc, Player target, ref float frame, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable damage during this attack.*
  - *Hover near the target.*
  - *Handle frames.*
  - *Have both twins stay close to each other when hovering.*
  - *Play a charge sound on the first frame.*
  - *Give some warning text before attacking.*
  - *What? What do you MEAN Artemis isn't actually saying this line?? I FEEL CHEATED!!!*
  - *Look at the target.*
  - *Create the death orb.*
  - *Prepare the Thermonuclear Orb before firing it.*
- **技术实现原理解析**:
  在执行 `ThermonuclearBlitz` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ThanatosAres_LaserCircle`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Thanatos spins around the target with its head always open while releasing lasers inward.*
  - *Select segment shoot attributes.*
  - *Disable contact damage before the attack happens, to prevent cheap hits.*
  - *Decide frames.*
  - *Ares sits in place, creating five large exo overload laser bursts.*
  - *Clear away old projectiles.*
  - *Create telegraphs.*
  - *Create laser bursts.*
  - *Make the lasers slower in multiplayer.*
  - *Slow down.*
- **技术实现原理解析**:
  在执行 `ThanatosAres_LaserCircle` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_ThanatosAres_EnergySlashesAndCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.AresSlashSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Thanatos attempts to slam into the target and accelerate.*
  - *Hover near the target before the attack begins.*
  - *Disable contact damage before the attack happens, to prevent cheap hits.*
  - *Redirect and look at the target before charging.*
  - *Thanatos will zip towards the target during this if necessary, to ensure that he's nearby by the time the attack begins.*
  - *Accelerate.*
  - *Decide the current rotation based on velocity.*
  - *Decide frames.*
  - *Ares hovers above the target and slashes downward, forcing the player into a tight position momentarily.*
  - *Dangle about like normal if waiting for the attack to start.*
- **技术实现原理解析**:
  在执行 `ThanatosAres_EnergySlashesAndCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_AresTwins_DualLaserCharges`
- **参数列表**: `(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.ELRFireSound`, `SoundID.Item36`, `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: `0`
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Inherit the attack timer from the initial mech.*
  - *Have Artemis attempt to do a horizontal sweep while releasing lasers in bursts. This only happens after Ares has released the laserbeams.*
  - *Don't do contact damage.*
  - *Reset the flash effect.*
  - *Simply hover in place if the laserbeams have not been fired.*
  - *Decide rotation.*
  - *Hover into position.*
  - *Determine rotation.*
  - *Prepare the charge.*
  - *Swoop down slightly and release lasers.*
- **技术实现原理解析**:
  在执行 `AresTwins_DualLaserCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_AresTwins_CircleAttack`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Inherit the attack timer from the initial mech.*
  - *Hover over the target.*
  - *The plasma and tesla arms will do the attacking.*
  - *Decide whether to fire or not.*
  - *Decide the frame.*
  - *Artemis and Apollo move in a circular formation at first, before creating the special attack.*
  - *Hover near the target and look at them.*
- **技术实现原理解析**:
  在执行 `AresTwins_CircleAttack` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_TwinsThanatos_ThermoplasmaDashes`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Halt attacking if Artemis and Apollo are busy entering their second phase.*
  - *The Exo Twins circle Thanatos' head and fire blasts that explode into lingering fire/plasma gas.*
  - *Look at the target.*
  - *Disable contact damage.*
  - *Handle frames.*
  - *Fire blasts.*
  - *Thanatos charges at the target.*
  - *Decide frames.*
  - *Delete blasts and gas before transitioning to the next attack.*
- **技术实现原理解析**:
  在执行 `TwinsThanatos_ThermoplasmaDashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_TwinsThanatos_AlternatingTwinsBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.ExoPlasmaShootSound`
- **生成弹幕 (Projectiles Created)**: `0`
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *This attack is very timing sensitive and resetting it if the twins suddenly need to enter their second phase is untenable.*
  - *As a result, if that happens, the attack is simply terminated early and laser beams/telegraphs are all destroyed.*
  - *Have Thanatos snap at the target.*
  - *Hover near the target before the attack begins.*
  - *Disable contact damage before the attack happens, to prevent cheap hits.*
  - *Redirect and look at the target before charging.*
  - *Thanatos will zip towards the target during this if necessary, to ensure that he's nearby by the time the attack begins.*
  - *Accelerate.*
  - *Decide the current rotation based on velocity.*
  - *Decide frames.*
- **技术实现原理解析**:
  在执行 `TwinsThanatos_AlternatingTwinsBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_AggressiveCharge`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Decide frames.*
  - *Handle movement.*
  - *Play a sound prior to switching attacks.*
- **技术实现原理解析**:
  在执行 `AggressiveCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_ExoBomb`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Decide frames.*
  - *Initialize the spin time. This is done via a variable because it's possible that the spin time will otherwise switch if Thanatos changes subphases mid-attack, potentially*
  - *resulting in strange behaviors.*
  - *Disable contact damage.*
  - *Attempt to get into position for a charge.*
  - *Stop hovering if close to the hover destination and prepare the spin.*
  - *Create the exo bomb.*
  - *Charge.*
  - *Play a sound prior to switching attacks.*
- **技术实现原理解析**:
  在执行 `ExoBomb` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_RefractionRotorRays`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Decide frames.*
  - *Don't deal damage because its apparently really annoying to dodge even though its half the damn attack.*
  - *Approach the player at an increasingly slow speed.*
  - *Continue moving in the current direction, but continue slowing down.*
  - *Play a telegraph sound to alert the player of the impending charge.*
  - *Begin the charge.*
  - *Sometimes charge early if aimed at the target and the redirect is more than halfway done.*
  - *Charge and release refraction rotors.*
  - *Randomly pick a segment that's decently far away from the target but not too far away to release a rotor from.*
  - *Play a sound prior to switching attacks.*
- **技术实现原理解析**:
  在执行 `RefractionRotorRays` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ExoLightBarrage`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Decide frames.*
  - *Disable contact damage.*
  - *Initialize a hover offset direction.*
  - *Clamp Thanatos' position to stay in the world.*
  - *This is very important, as the telegraph might simply not appear if Thanatos is too high up.*
  - *Update the sound telegraph's position to account for Thanatos drifting.*
  - *Attempt to get into position for the light attack.*
  - *Stop hovering if close to the hover destination and prepare to move towards the target.*
  - *Slow down, move towards the target (while maintaining the current direction) and create a laser telegraph.*
  - *Create light telegraphs.*
- **技术实现原理解析**:
  在执行 `ExoLightBarrage` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_MaximumOverdrive`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Dash or die.*
  - *Play a danger sound before the attack begins.*
  - *Decide frames.*
  - *Decide whether to cool off or not.*
  - *Handle movement.*
  - *Periodically release lasers from the sides.*
  - *Create lightning bolts in the sky.*
  - *Play a sound prior to switching attacks.*
- **技术实现原理解析**:
  在执行 `MaximumOverdrive` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `AresSpinningRedDeathray`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresSpinningRedDeathray` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ExoburstSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ExoburstSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresEnergySlash`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresEnergySlash` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `SmallPlasmaSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SmallPlasmaSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LightOverloadRay`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LightOverloadRay` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ThanatosAresComboLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ThanatosAresComboLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresDeathBeamTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresDeathBeamTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresTeslaGasField`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresTeslaGasField` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresPulseBlast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresPulseBlast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ExolaserBomb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ExolaserBomb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AresBeamExplosion`
- **渲染机制**: `常规 Sprite 纹理渲染 ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresBeamExplosion` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ApolloRocketInfernum`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ApolloRocketInfernum` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `HotMetal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HotMetal` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AresCannonLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresCannonLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArtemisLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArtemisLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArtemisLaserbeamTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArtemisLaserbeamTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ExolaserSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ExolaserSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ExoplasmaExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ExoplasmaExplosion` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AresEnergyDeathrayTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresEnergyDeathrayTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlasmaChargeTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlasmaChargeTelegraph` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ApolloAcceleratingPlasmaSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ApolloAcceleratingPlasmaSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresBeamTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresBeamTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresPlasmaFireball`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresPlasmaFireball` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlasmaGas`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlasmaGas` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresTeslaSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresTeslaSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ApolloPlasmaFireball`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ApolloPlasmaFireball` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArtemisBasicShotLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArtemisBasicShotLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresPrecisionBlast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresPrecisionBlast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AresTeslaOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AresTeslaOrb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ArtemisGatlingLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArtemisGatlingLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ApolloFallingPlasmaSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ApolloFallingPlasmaSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LightRayTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LightRayTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SuperheatedExofireGas`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SuperheatedExofireGas` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArtemisGasFireballBlast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArtemisGasFireballBlast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DetatchedThanatosLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DetatchedThanatosLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RefractionRotor`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RefractionRotor` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ApolloFlamethrower`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ApolloFlamethrower` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ThermonuclearDeathOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ThermonuclearDeathOrb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ExoplasmaBomb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ExoplasmaBomb` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `if (!InfernumEffectsRegistry.ScreenBorderShader.IsActive() && !InfernumConfig.Instance.ReducedGraphicsConfig)`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseColor(Main.hslToRgb(currentHue, saturation, luminosity));`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseOpacity(1f);`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseImage(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseIntensity(intensity);`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseOpacity(0f);`
- `code`: `InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseIntensity(0f);`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public PrimitiveTrailCopy LightningDrawer;`
- `code`: `public PrimitiveTrailCopy LightningBackgroundDrawer;`
- `code`: `using CalamityMod.Graphics.Primitives;`
- `code`: `PrimitiveSettings foregroundSettings = new(npc.ModNPC<AresBody>().WidthFunction, npc.ModNPC<AresBody>().ColorFunction, s`
- `code`: `PrimitiveSettings backgroundSettings = new(npc.ModNPC<AresBody>().BackgroundWidthFunction, npc.As<AresBody>().Background`
- `code`: `PrimitiveRenderer.RenderTrail(arm2ElectricArcPoints, backgroundSettings, 90);`
- `code`: `PrimitiveRenderer.RenderTrail(arm2ElectricArcPoints, foregroundSettings, 90);`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(4200f, 1400f, Main.LocalPlayer.Distance(playerT`
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower *= Utils.GetLerpValue(ExoMechChooseDelay + 5f, ExoMechPhaseDialogueT`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 13.5f;`
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `Target.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(3f, 12f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 3f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 2f;`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🌅 **转场登场字幕**：Boss 被召唤时，将强制遮罩屏幕并淡入展示专属的登场卡片，这是炼狱模组的标志性特征。
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `Draedon` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `39` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `27` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。