# 无尽虚空 (Ceaseless Void) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `CeaselessVoid` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `CeaselessVoid`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<CeaselessVoidBoss>()`, `ModContent.NPCType<DarkEnergy>()`
- **模组内关联的源文件列表**:
  - `AcceleratingDarkEnergy.cs` (源路径: `.../BossAIs/CeaselessVoid/AcceleratingDarkEnergy.cs`)
  - `CeaselessEnergyPulse.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessEnergyPulse.cs`)
  - `CeaselessVoidBehaviorOverride.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVoidBehaviorOverride.cs`)
  - `CeaselessVoidLineTelegraph.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVoidLineTelegraph.cs`)
  - `CeaselessVoidMusicSceneInfernum.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVoidMusicSceneInfernum.cs`)
  - `CeaselessVoidShell.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVoidShell.cs`)
  - `CeaselessVortex.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVortex.cs`)
  - `CeaselessVortexTear.cs` (源路径: `.../BossAIs/CeaselessVoid/CeaselessVortexTear.cs`)
  - `ConvergingDungeonRubble.cs` (源路径: `.../BossAIs/CeaselessVoid/ConvergingDungeonRubble.cs`)
  - `DarkEnergyBehaviorOverride.cs` (源路径: `.../BossAIs/CeaselessVoid/DarkEnergyBehaviorOverride.cs`)
  - `EnergyTelegraph.cs` (源路径: `.../BossAIs/CeaselessVoid/EnergyTelegraph.cs`)
  - `OtherworldlyBolt.cs` (源路径: `.../BossAIs/CeaselessVoid/OtherworldlyBolt.cs`)
  - `RealitySlice.cs` (源路径: `.../BossAIs/CeaselessVoid/RealitySlice.cs`)
  - `SpinningDarkEnergy.cs` (源路径: `.../BossAIs/CeaselessVoid/SpinningDarkEnergy.cs`)
  - `TelegraphedOtherwordlyBolt.cs` (源路径: `.../BossAIs/CeaselessVoid/TelegraphedOtherwordlyBolt.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `CeaselessVoidBehaviorOverride` -> 重写目标: `ModContent.NPCType<CeaselessVoidBoss>()`
  - 类名: `DarkEnergyBehaviorOverride` -> 重写目标: `ModContent.NPCType<DarkEnergy>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.66667f`
- `Phase3LifeRatio` = `0.15f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `DarkEnergyAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `HoverInPlace` - 对应的行为处理状态。
2. `SpinInPlace` - 对应的行为处理状态。
3. `AccelerateTowardsTarget` - 对应的行为处理状态。
### 🎯 状态机枚举: `CeaselessVoidAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `// Phase 1 startup.
            ChainedUp` - 对应的行为处理状态。
2. `DarkEnergySwirl` - 对应的行为处理状态。
3. `// Phase 1 attacks.
            RedirectingAcceleratingDarkEnergy` - 对应的行为处理状态。
4. `DiagonalMirrorBolts` - 对应的行为处理状态。
5. `CircularVortexSpawn` - 对应的行为处理状态。
6. `SpinningDarkEnergy` - 对应的行为处理状态。
7. `AreaDenialVortexTears` - 对应的行为处理状态。
8. `// Phase 2 transition.
            ShellCrackTransition` - 对应的行为处理状态。
9. `DarkEnergyTorrent` - 对应的行为处理状态。
10. `// Phase 2 attacks.
            EnergySuck` - 对应的行为处理状态。
11. `// Phase 3 transition.
            ChainBreakTransition` - 对应的行为处理状态。
12. `// Phase 3 attacks.
            JevilDarkEnergyBursts` - 对应的行为处理状态。
13. `MirroredCharges` - 对应的行为处理状态。
14. `ConvergingEnergyBarrages` - 对应的行为处理状态。
15. `// Death animation attack.
            DeathAnimation` - 对应的行为处理状态。
### 🎯 状态机枚举: `OtherwordlyBoltAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `LockIntoPosition` - 对应的行为处理状态。
2. `FlyIntoBackground` - 对应的行为处理状态。
3. `AccelerateFromBelow` - 对应的行为处理状态。
4. `ArcAndAccelerate` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **22** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_HoverInPlace`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position.*
  - *Fade in.*
- **技术实现原理解析**:
  在执行 `HoverInPlace` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_SpinInPlace`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Accelerate once done spinning.*
  - *SPEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEN!!*
  - *Spawn particles.*
- **技术实现原理解析**:
  在执行 `SpinInPlace` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_AccelerateTowardsTarget`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Accelerate.*
  - *Arc towards the target.*
  - *Spawn particles.*
- **技术实现原理解析**:
  在执行 `AccelerateTowardsTarget` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ChainedUp`
- **参数列表**: `(NPC npc, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize Ceaseless Void's binding chains on the first frame.*
  - *Determine how far off the chains should go.*
  - *Disable damage.*
  - *Prevent hovering over the Void's name to reveal what it is.*
  - *Disable boss behaviors.*
- **技术实现原理解析**:
  在执行 `ChainedUp` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DarkEnergySwirl`
- **参数列表**: `(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CeaselessVoidSwirlSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make the screen black to distract the player from the fact that some wacky things are going on in the background.*
  - *Give a tip.*
  - *Grant the targets infinite flight time during the portal tear charge up attack, so that they don't run out and take an unfair hit.*
  - *Initialize by creating the dark energy ring.*
  - *Disable damage.*
  - *Calculate the life ratio of all dark energy combined.*
  - *If it is sufficiently low then all remaining dark energy fades away and CV goes to the next attack.*
  - *Shoot accelerating dark energy.*
- **技术实现原理解析**:
  在执行 `DarkEnergySwirl` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_RedirectingAcceleratingDarkEnergy`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item103`, `InfernumSoundRegistry.CeaselessVoidSwirlSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Release energy balls from above.*
  - *Give a tip.*
  - *Make energy balls accelerate.*
- **技术实现原理解析**:
  在执行 `RedirectingAcceleratingDarkEnergy` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DiagonalMirrorBolts`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Play funny sounds.*
  - *Release energy bolts that fly outward.*
  - *Release a rain of energy bolts.*
- **技术实现原理解析**:
  在执行 `DiagonalMirrorBolts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CircularVortexSpawn`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CeaselessVoidStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Grant the target infinite flight time during the portal tear charge up attack, so that they don't run out and take an unfair hit.*
  - *Play a shoot sound if ready.*
  - *Create convergence particles.*
  - *Create pulse rungs and bloom periodically.*
  - *Create energy sparks at the center of Ceaseless Void.*
  - *Play a convergence sound.*
  - *Release accelerating bolts outward.*
  - *Create impact effects.*
  - *Create bloom and pulse rings while firing.*
- **技术实现原理解析**:
  在执行 `CircularVortexSpawn` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_SpinningDarkEnergy`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CeaselessVoidSwirlSound`, `SoundID.Item104`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Release energy balls from the Ceaseless Void's center.*
  - *Create bloom and pulse rings while firing.*
  - *Create bursts of energy outward.*
  - *Make energy balls accelerate.*
- **技术实现原理解析**:
  在执行 `SpinningDarkEnergy` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_AreaDenialVortexTears`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item104`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Wait before creating vortices.*
  - *Give a tip before the torrent is fired.*
  - *Periodically release vortices that strike at the target.*
- **技术实现原理解析**:
  在执行 `AreaDenialVortexTears` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ShellCrack`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float voidIsCracked)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Disable damage during this attack.*
  - *Charge up energy before performing whitening.*
  - *Create a slice effect through the void right before the screen whitening happens.*
  - *Make the whitening effect draw the Ceaseless Void.*
  - *Break the metal.*
- **技术实现原理解析**:
  在执行 `ShellCrack` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DarkEnergyTorrent`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Disable damage during this attack.*
  - *Play sounds at sections of the attack.*
  - *Perform charge-up effects.*
  - *Create light streaks that converge inward.*
  - *Create a pulsating energy orb.*
  - *Create a pulse particle before firing.*
  - *Give a tip before the torrent is fired.*
  - *Release a spiral of dark energy.*
  - *Periodically emit energy sparks.*
- **技术实现原理解析**:
  在执行 `DarkEnergyTorrent` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_EnergySuck`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Grant the target infinite flight time so that they don't run out and take an unfair hit.*
  - *Create a dark energy circle on the first frame.*
  - *Do contact damage so that the player is punished for being sucked in.*
  - *Calculate the relative intensity of the suck effect.*
  - *Make the screen shake at first.*
  - *Play a suck sound.*
  - *Create various energy particles.*
  - *Create pulse rungs and bloom periodically.*
  - *Release rubble around the arena.*
  - *Suck the player in towards the Ceaseless Void.*
- **技术实现原理解析**:
  在执行 `EnergySuck` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ChainBreakTransition`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Disable damage.*
  - *Play a buildup sound prior to the whitening effect.*
  - *Give a tip.*
  - *Enable the distortion filter if it isnt active and the player's config permits it.*
  - *Charge up energy before performing whitening.*
  - *Create pulse rings and bloom periodically.*
  - *Make the whitening effect draw the Ceaseless Void's chains.*
  - *Break the chains.*
- **技术实现原理解析**:
  在执行 `ChainBreakTransition` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_JevilDarkEnergyBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float teleportEffectInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CeaselessVoidSwirlSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Decide the teleport effect interpolant.*
  - *Teleport next to the target.*
- **技术实现原理解析**:
  在执行 `JevilDarkEnergyBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_MirroredCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float teleportEffectInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Do contact damage.*
  - *Initialize acceleration. The underlying calculus necessary to decide this value can be a bit complex, and as such it is only done once for*
  - *performance reasons. This calculates the acceleration the Ceaseless Void must move at every frame to ensure that it travels an exact distance in a given*
  - *amount of time from a specific starting speed.*
  - *Decide the teleport effect interpolant.*
  - *Teleport on top of the player before the split charges happen.*
  - *Do funny screen stuff.*
  - *Release energy sparks at the impact point.*
  - *Teleport at an offset perpendicular to the player's current velocity.*
  - *Accelerate.*
- **技术实现原理解析**:
  在执行 `MirroredCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_ConvergingEnergyBarrages`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item28`, `InfernumSoundRegistry.CeaselessVoidStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover before firing.*
  - *Prepare particle line telegraphs.*
  - *Shoot.*
  - *Create a puff of dark energy.*
- **技术实现原理解析**:
  在执行 `ConvergingEnergyBarrages` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Completely stop in place.*
  - *Close the HP bar.*
  - *Teleport above the player on the first frame. If there's tiles above, teleport below them instead.*
  - *Jitter in place and shake the screen at first.*
  - *Charge energy.*
  - *Create pulse rings and bloom periodically.*
  - *Prepere a shockwave and destroy the metal shell.*
  - *Perform screen effects.*
  - *Play impactful sounds.*
  - *Periodically emit energy sparks and circles.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_LockIntoPosition`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position, offset from the ceaseless void.*
  - *Aim in the direction that the bolt will fire in.*
  - *Begin flying into the background once ready.*
- **技术实现原理解析**:
  在执行 `LockIntoPosition` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FlyIntoBackground`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Prepare death effects before disappearing into the background.*
- **技术实现原理解析**:
  在执行 `FlyIntoBackground` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_AccelerateFromBelow`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Accelerate.*
  - *Aim in the direction that the bolt is accelerating.*
- **技术实现原理解析**:
  在执行 `AccelerateFromBelow` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ArcAndAccelerate`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Arc and accelerate.*
  - *Aim in the direction that the bolt is accelerating.*
- **技术实现原理解析**:
  在执行 `ArcAndAccelerate` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `CeaselessVortexTear`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CeaselessVortexTear` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `CeaselessVortex`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `CeaselessVoidShell`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CeaselessVoidShell` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `OtherworldlyBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `OtherworldlyBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ConvergingDungeonRubble`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ConvergingDungeonRubble` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AcceleratingDarkEnergy`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AcceleratingDarkEnergy` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `TelegraphedOtherwordlyBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `TelegraphedOtherwordlyBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `EnergyTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EnergyTelegraph` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `RealitySlice`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RealitySlice` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `CeaselessVoidLineTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CeaselessVoidLineTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SpinningDarkEnergy`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeat`
- `code`: `Filters.Scene.Activate("InfernumMode:ScreenDistortion", Main.LocalPlayer.Center);`
- `code`: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");`
- `code`: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(distorti`
- `code`: `InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);`
- `code`: `var portalShader = InfernumEffectsRegistry.CeaselessVoidPortalShader;`
- `code`: `var tear = InfernumEffectsRegistry.RealityTearVertexShader;`
- `code`: `PrimitiveRenderer.RenderCircle(npc.Center, new(_ => radius, _ => Color.White, Shader: InfernumEffectsRegistry.RealityTea`
- `code`: `InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(InfernumTextureRegistry.Stars);`
- `code`: `InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);`
- `code`: `InfernumEffectsRegistry.CeaselessVoidCrackShader.UseShaderSpecificData(new(npc.frame.X, npc.frame.Y, npc.frame.Width, np`
- `code`: `InfernumEffectsRegistry.CeaselessVoidCrackShader.UseImage1("Images/Misc/Perlin");`
- `code`: `InfernumEffectsRegistry.CeaselessVoidCrackShader.Shader.Parameters["sheetSize"].SetValue(metalTexture.Size());`
- `code`: `InfernumEffectsRegistry.CeaselessVoidCrackShader.Apply();`
- `code`: `Texture2D noise = InfernumTextureRegistry.WavyNoise.Value;`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) =>`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 8f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 10f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = attackTimer / chargeUpTime * 3f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 24f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = Utils.GetLerpValue(attackDelay + suckTime - 90f, attackDelay + suckTi`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 18f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = attackTimer / chargeUpTime * 8f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 25f;`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `CeaselessVoid` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `11` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `22` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。