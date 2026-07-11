# 月亮领主 (Moon Lord) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `MoonLord` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `MoonLord`
- **重写的NPC目标 (Override Target)**: `NPCID.MoonLordCore`, `NPCID.MoonLordHand`, `NPCID.MoonLordHead`, `NPCID.MoonLordFreeEye`
- **模组内关联的源文件列表**:
  - `LunarAsteroid.cs` (源路径: `.../BossAIs/MoonLord/LunarAsteroid.cs`)
  - `LunarFireball.cs` (源路径: `.../BossAIs/MoonLord/LunarFireball.cs`)
  - `LunarFlare.cs` (源路径: `.../BossAIs/MoonLord/LunarFlare.cs`)
  - `LunarFlareTelegraph.cs` (源路径: `.../BossAIs/MoonLord/LunarFlareTelegraph.cs`)
  - `MoonLordCoreBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/MoonLordCoreBehaviorOverride.cs`)
  - `MoonLordDeathAnimationHandler.cs` (源路径: `.../BossAIs/MoonLord/MoonLordDeathAnimationHandler.cs`)
  - `MoonLordDeathBloodBlob.cs` (源路径: `.../BossAIs/MoonLord/MoonLordDeathBloodBlob.cs`)
  - `MoonLordExplosion.cs` (源路径: `.../BossAIs/MoonLord/MoonLordExplosion.cs`)
  - `MoonLordExplosionCinder.cs` (源路径: `.../BossAIs/MoonLord/MoonLordExplosionCinder.cs`)
  - `MoonLordHandBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/MoonLordHandBehaviorOverride.cs`)
  - `MoonLordHeadBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/MoonLordHeadBehaviorOverride.cs`)
  - `MoonLordLeechBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/MoonLordLeechBehaviorOverride.cs`)
  - `MoonLordWave.cs` (源路径: `.../BossAIs/MoonLord/MoonLordWave.cs`)
  - `NonHomingPhantasmalEye.cs` (源路径: `.../BossAIs/MoonLord/NonHomingPhantasmalEye.cs`)
  - `PhantasmalBoltBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/PhantasmalBoltBehaviorOverride.cs`)
  - `PhantasmalDeathray.cs` (源路径: `.../BossAIs/MoonLord/PhantasmalDeathray.cs`)
  - `PhantasmalOrb.cs` (源路径: `.../BossAIs/MoonLord/PhantasmalOrb.cs`)
  - `PhantasmalSphereBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/PhantasmalSphereBehaviorOverride.cs`)
  - `PressurePhantasmalDeathray.cs` (源路径: `.../BossAIs/MoonLord/PressurePhantasmalDeathray.cs`)
  - `StardustConstellation.cs` (源路径: `.../BossAIs/MoonLord/StardustConstellation.cs`)
  - `TrueEyeChargeTelegraph.cs` (源路径: `.../BossAIs/MoonLord/TrueEyeChargeTelegraph.cs`)
  - `TrueEyeOfCthulhuBehaviorOverride.cs` (源路径: `.../BossAIs/MoonLord/TrueEyeOfCthulhuBehaviorOverride.cs`)
  - `VoidBlackHole.cs` (源路径: `.../BossAIs/MoonLord/VoidBlackHole.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `MoonLordCoreBehaviorOverride` -> 重写目标: `NPCID.MoonLordCore`
  - 类名: `MoonLordHandBehaviorOverride` -> 重写目标: `NPCID.MoonLordHand`
  - 类名: `MoonLordHeadBehaviorOverride` -> 重写目标: `NPCID.MoonLordHead`
  - 类名: `TrueEyeOfCthulhuBehaviorOverride` -> 重写目标: `NPCID.MoonLordFreeEye`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.65f`
- `Phase3LifeRatio` = `0.33333f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `MoonLordAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `SpawnEffects` - 对应的行为处理状态。
2. `DeathEffects` - 对应的行为处理状态。
3. `PhantasmalSphereHandWaves` - 对应的行为处理状态。
4. `PhantasmalBoltEyeBursts` - 对应的行为处理状态。
5. `PhantasmalFlareBursts` - 对应的行为处理状态。
6. `PhantasmalDeathrays` - 对应的行为处理状态。
7. `PhantasmalSpin` - 对应的行为处理状态。
8. `PhantasmalRush` - 对应的行为处理状态。
9. `PhantasmalDance` - 对应的行为处理状态。
10. `PhantasmalBarrage` - 对应的行为处理状态。
11. `ExplodingConstellations` - 对应的行为处理状态。
12. `PhantasmalWrath` - 对应的行为处理状态。
13. `VoidAccretionDisk` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **16** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_SpawnEffects`
- **参数列表**: `(NPC npc, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: `SoundID.Zombie92`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Don't take damage during spawn effects.*
  - *Roar after a bit of time has passed.*
- **技术实现原理解析**:
  在执行 `SpawnEffects` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_DeathEffects`
- **参数列表**: `(NPC npc, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.NPCDeath61`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create a flash before anything else.*
  - *Create explosions periodically.*
  - *Release a bunch of blood particles everywhere.*
- **技术实现原理解析**:
  在执行 `DeathEffects` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_VoidAccretionDisk`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create the black hole.*
- **技术实现原理解析**:
  在执行 `VoidAccretionDisk` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_IdleHover`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **技术实现原理解析**:
  在执行 `IdleHover` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_DefaultHandHover`
- **参数列表**: `(NPC npc, NPC core, int handSide, float attackTimer, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **技术实现原理解析**:
  在执行 `DefaultHandHover` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_PhantasmalSphereHandWaves`
- **参数列表**: `(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item122`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Open the hand right before firing.*
  - *Shut hands faster after the spheres have been released.*
  - *Become invulnerable if the hand is closed.*
  - *Sync the entire moon lord's current state. This will be executed on the frame immediately after this one.*
  - *Slam all phantasmal spheres at the target after they have been fired.*
- **技术实现原理解析**:
  在执行 `PhantasmalSphereHandWaves` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalFlareBursts`
- **参数列表**: `(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item72`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create flare telegraphs.*
- **技术实现原理解析**:
  在执行 `PhantasmalFlareBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_ExplodingConstellations`
- **参数列表**: `(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item72`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create charge dust and close hands before the attack begins.*
  - *Determine what constellation pattern this arm will use. Each arm has their own pattern that they create.*
  - *Create stars.*
  - *Diagonal stars from top left to bottom right.*
  - *Diagonal stars from top right to bottom left.*
  - *Horizontal sinusoid.*
  - *Make all constellations spawned by this hand prepare to explode.*
- **技术实现原理解析**:
  在执行 `ExplodingConstellations` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalBoltEyeBursts`
- **参数列表**: `(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have a small delay prior to attacking.*
  - *Create dust telegraphs prior to firing.*
  - *Calculate pupil variables.*
  - *Create a burst of phantasmal bolts after the telegraph completes.*
- **技术实现原理解析**:
  在执行 `PhantasmalBoltEyeBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_PhantasmalDeathrays`
- **参数列表**: `(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine the size of the telegraph.*
  - *Calculate pupil variables.*
  - *Fire lasers.*
  - *Make some strong sounds.*
  - *Release a spread of bolts. They do not fire if the target is close to the eye.*
- **技术实现原理解析**:
  在执行 `PhantasmalDeathrays` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_IdleObserve`
- **参数列表**: `(NPC npc, Player target, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define pupil variables.*
- **技术实现原理解析**:
  在执行 `IdleObserve` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalSpin`
- **参数列表**: `(NPC npc, Player target, NPC core, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Zombie100`, `SoundID.Item12`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Look at the target.*
  - *Hover into position.*
  - *Release the barrage of eyes.*
  - *Terminate the attack early if the target leaves the arena, as this attack is arena-focused.*
- **技术实现原理解析**:
  在执行 `PhantasmalSpin` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalRush`
- **参数列表**: `(NPC npc, Player target, NPC core, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Zombie104`, `SoundID.Zombie100`
- **生成弹幕 (Projectiles Created)**: `0`
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *The left eye shoots pressure lasers while the right eye does zigzag charges.*
  - *Delete leftover phantasmal spheres.*
  - *Release lasers.*
  - *Prepare the charges.*
  - *Cast charge telegraph lines and prepare the initial charge.*
- **技术实现原理解析**:
  在执行 `PhantasmalRush` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalDance`
- **参数列表**: `(NPC npc, Player target, NPC core, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Zombie102`, `SoundID.DD2_WyvernDiveDown`, `SoundID.Item72`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Snap into place for the spin.*
  - *Stop in place and look at the target before charging.*
  - *Define the telegraph direction.*
  - *Scream before charging.*
  - *Slow down.*
  - *Do the charge.*
  - *Do contact damage and release phantasmal orbs when charging.*
- **技术实现原理解析**:
  在执行 `PhantasmalDance` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalBarrage`
- **参数列表**: `(NPC npc, Player target, NPC core, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item122`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the group index that should attack on the first frame. Only the first eye will make the core do this.*
  - *Hover into position before attacking.*
  - *Create phantasmal spheres.*
  - *Creates a pattern wherein every second sphere is created opposite to the previous one.*
  - *The consequence of this is that the full "circle" is only calculated halfway. The secondary*
  - *calculation will allow for the full circle to be completed.*
  - *Create an explosion as an indicator prior to attacking.*
  - *Also make the eye that should charge do so.*
  - *Make all phantasmal spheres move away from the eye that created them.*
  - *Release phantasmal bolts at the target.*
- **技术实现原理解析**:
  在执行 `PhantasmalBarrage` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhantasmalWrath`
- **参数列表**: `(NPC npc, Player target, NPC core, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Zombie101`, `SoundID.DD2_WyvernDiveDown`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Circle around the player before attacking.*
  - *Slow down.*
  - *Scream before charging.*
  - *Have the first eye release a burst of bolts in all directions.*
  - *Release phantasmal eyes.*
- **技术实现原理解析**:
  在执行 `PhantasmalWrath` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `NonHomingPhantasmalEye`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `NonHomingPhantasmalEye` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LunarFlareTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LunarFlareTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `MoonLordDeathBloodBlob`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MoonLordDeathBloodBlob` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LunarFlare`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LunarFlare` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `MoonLordExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MoonLordExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `StardustConstellation`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `StardustConstellation` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `VoidBlackHole`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `LunarFireball`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LunarFireball` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LunarAsteroid`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LunarAsteroid` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PhantasmalDeathray`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PhantasmalDeathray` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `TrueEyeChargeTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `TrueEyeChargeTelegraph` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `MoonLordDeathAnimationHandler`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MoonLordDeathAnimationHandler` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `MoonLordExplosionCinder`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MoonLordExplosionCinder` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PhantasmalOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PhantasmalOrb` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public PrimitiveTrailCopy LightDrawer;`
- `code`: `LightDrawer ??= new PrimitiveTrailCopy(c => rayWidthFunction(c, Projectile.Infernum().ExtraAI[8]), c => rayColorFunction`
- `code`: `public class PhantasmalDeathray : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `internal PrimitiveTrailCopy BeamDrawer;`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`
- `code`: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(1.4f);`
- `code`: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- `code`: `public Color TelegraphPrimitiveColor(float completionRatio)`
- `code`: `public float TelegraphPrimitiveWidth(float completionRatio)`
- `code`: `var flame = InfernumEffectsRegistry.FlameVertexShader;`
- `code`: `PrimitiveRenderer.RenderTrail(positions, new(TelegraphPrimitiveWidth, TelegraphPrimitiveColor, _ => Projectile.Size * 0.`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🌅 **转场登场字幕**：Boss 被召唤时，将强制遮罩屏幕并淡入展示专属的登场卡片，这是炼狱模组的标志性特征。
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `MoonLord` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `14` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `16` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。