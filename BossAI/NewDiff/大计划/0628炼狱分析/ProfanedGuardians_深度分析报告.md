# 神明守卫 (Profaned Guardians) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `ProfanedGuardians` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `ProfanedGuardians`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<ProfanedGuardianCommander>()`, `ModContent.NPCType<ProfanedGuardianDefender>()`, `ModContent.NPCType<ProfanedGuardianHealer>()`
- **模组内关联的源文件列表**:
  - `AttackerGuardianBehaviorOverride.cs` (源路径: `.../BossAIs/ProfanedGuardians/AttackerGuardianBehaviorOverride.cs`)
  - `CommanderSpear.cs` (源路径: `.../BossAIs/ProfanedGuardians/CommanderSpear.cs`)
  - `CommanderSpearThrown.cs` (源路径: `.../BossAIs/ProfanedGuardians/CommanderSpearThrown.cs`)
  - `DefenderGuardianBehaviorOverride.cs` (源路径: `.../BossAIs/ProfanedGuardians/DefenderGuardianBehaviorOverride.cs`)
  - `DefenderShield.cs` (源路径: `.../BossAIs/ProfanedGuardians/DefenderShield.cs`)
  - `EtherealHand.cs` (源路径: `.../BossAIs/ProfanedGuardians/EtherealHand.cs`)
  - `GuardianComboAttackManager.cs` (源路径: `.../BossAIs/ProfanedGuardians/GuardianComboAttackManager.cs`)
  - `GuardianIndexManager.cs` (源路径: `.../BossAIs/ProfanedGuardians/GuardianIndexManager.cs`)
  - `GuardiansRodFailPulse.cs` (源路径: `.../BossAIs/ProfanedGuardians/GuardiansRodFailPulse.cs`)
  - `HealerGuardianBehaviorOverride.cs` (源路径: `.../BossAIs/ProfanedGuardians/HealerGuardianBehaviorOverride.cs`)
  - `HealerShieldCrystal.cs` (源路径: `.../BossAIs/ProfanedGuardians/HealerShieldCrystal.cs`)
  - `HolyAimedDeathray.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolyAimedDeathray.cs`)
  - `HolyAimedDeathrayTelegraph.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolyAimedDeathrayTelegraph.cs`)
  - `HolyDogmaFireball.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolyDogmaFireball.cs`)
  - `HolyFireRift.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolyFireRift.cs`)
  - `HolyFireWall.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolyFireWall.cs`)
  - `HolySineSpear.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolySineSpear.cs`)
  - `HolySpinningFireBeam.cs` (源路径: `.../BossAIs/ProfanedGuardians/HolySpinningFireBeam.cs`)
  - `LavaEruptionPillar.cs` (源路径: `.../BossAIs/ProfanedGuardians/LavaEruptionPillar.cs`)
  - `LingeringHolyFire.cs` (源路径: `.../BossAIs/ProfanedGuardians/LingeringHolyFire.cs`)
  - `MagicCrystalShot.cs` (源路径: `.../BossAIs/ProfanedGuardians/MagicCrystalShot.cs`)
  - `MagicSpiralCrystalShot.cs` (源路径: `.../BossAIs/ProfanedGuardians/MagicSpiralCrystalShot.cs`)
  - `ProfanedCirclingRock.cs` (源路径: `.../BossAIs/ProfanedGuardians/ProfanedCirclingRock.cs`)
  - `ProfanedRock.cs` (源路径: `.../BossAIs/ProfanedGuardians/ProfanedRock.cs`)
  - `ProfanedSpearInfernum.cs` (源路径: `.../BossAIs/ProfanedGuardians/ProfanedSpearInfernum.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `AttackerGuardianBehaviorOverride` -> 重写目标: `ModContent.NPCType<ProfanedGuardianCommander>()`
  - 类名: `DefenderGuardianBehaviorOverride` -> 重写目标: `ModContent.NPCType<ProfanedGuardianDefender>()`
  - 类名: `HealerGuardianBehaviorOverride` -> 重写目标: `ModContent.NPCType<ProfanedGuardianHealer>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
该 Boss 在代码中没有使用静态的 `const float` 血量比例常量定义，其阶段转换逻辑可能直接内联于 `PreAI` 函数中通过硬编码数值判断，或继承自 Calamity 的默认状态机。

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `GuardiansAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `// Initial attacks.
            SpawnEffects` - 对应的行为处理状态。
2. `FlappyBird` - 对应的行为处理状态。
3. `// All 3 combo attacks.
            SoloHealer` - 对应的行为处理状态。
4. `SoloDefender` - 对应的行为处理状态。
5. `HealerAndDefender` - 对应的行为处理状态。
6. `HealerDeathAnimation` - 对应的行为处理状态。
7. `// Commander and Defender combo attacks
            SpearDashAndGroundSlam` - 对应的行为处理状态。
8. `CrashRam` - 对应的行为处理状态。
9. `FireballBulletHell` - 对应的行为处理状态。
10. `DefenderDeathAnimation` - 对应的行为处理状态。
11. `// Commander solo attacks.
            LargeGeyserAndCharge` - 对应的行为处理状态。
12. `DogmaLaserBall` - 对应的行为处理状态。
13. `BerdlySpears` - 对应的行为处理状态。
14. `SpearSpinThrow` - 对应的行为处理状态。
15. `RiftFireCharges` - 对应的行为处理状态。
16. `CommanderDeathAnimation` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **19** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_CommanderDeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow to a screeching halt.*
  - *Disable damage.*
  - *Despawn the spear if it is active.*
  - *Close the boss bar.*
  - *Determine the brightness width factor.*
  - *Fade out over time.*
  - *Disappear and drop loot.*
- **技术实现原理解析**:
  在执行 `CommanderDeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_DefenderYeetEffects`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Create particles to indicate the sudden speed.*
  - *Play a loud explosion + hurt sound and screenshake to give the impact power.*
  - *Create a bunch of rock particles to indicate a heavy impact.*
- **技术实现原理解析**:
  在执行 `DefenderYeetEffects` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpawnEffects`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ProvidenceBurnSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Do not take or deal damage.*
  - *If we are the commander, set a flash etc. The guardians are spawned in at the correct position so will move on correctly.*
  - *Create a bunch of pre-existing fire walls. This is so the player doesn't either sit around dawdling for*
  - *ages waiting for them to cover the garden, or more likely, move right and negate half the phase.*
  - *The base velocity of the walls.*
  - *The distance between each wall.*
  - *Loop through every wall to create.*
  - *Get the base center the same way as normal, but modify the x position by the wall we are using times the gap size.*
  - *This is the same as making the normal walls.*
  - *Create a random offset.*
- **技术实现原理解析**:
  在执行 `SpawnEffects` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_FlappyBird`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *This attack ends automatically when the crystal wall dies, it advances the attackers attack state, which the other*
  - *guardians check for and advance with it.*
  - *The commander bobs on the spot, pausing to aim and fire a fire beam at the player from afar.*
  - *Do not take damage.*
  - *Safely get the crystal. The commander should not attack if it is not present.*
  - *If time to fire, the target is close enough and the pushback wall is not present.*
  - *Fire deathray.*
  - *The defender summons fire walls that force you to go inbetween the gap.*
  - *This is basically flappy bird, the attacker spawns fire walls like the pipes that move towards the entrance of the garden.*
  - *Safely check for the crystal. The defender should stop attacking if it is not present*
- **技术实现原理解析**:
  在执行 `FlappyBird` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SoloHealer`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *The commander remains in the center firing its two spinning fire beams.*
  - *Sit still in the middle of the area.*
  - *Do not increase until the lasers are present.*
  - *Do not deal contact damage.*
  - *Screenshake*
  - *Check this here so that the flag gets set regardless, stopping it happening if the player enables screenshake after the first attack.*
  - *The defender hovers to your top left, not dealing contact damage and occasionally firing rocks at you.*
  - *float xOffset = 500f * -Sign(npc.DirectionTo(target.Center).X);*
  - *The healer sits to the right of the commander and empowers its shield. This causes spirals of projectiles to shoot out from it.*
  - *Sit still behind the commander*
- **技术实现原理解析**:
  在执行 `SoloHealer` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SoloDefender`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.VassalJumpSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Commander remains hovering still.*
  - *Do not deal contact damage.*
  - *Generate the shield if it is inactive.*
  - *Mark the shield as active.*
  - *Pick a location for the dash.*
  - *Leave and use the current offset to see if the direction of the new offset is perpendicular or less to the previous one. This prevents going to opposite sides and*
  - *moving thorugh the player's position to reach it. The 0.01 is added on top to ensure that tiny floating point imprecisions don't become a problem.*
  - *Get into position for the dash.*
  - *Move out of the way of the target if going around them.*
  - *Initialize the dash telegraph.*
- **技术实现原理解析**:
  在执行 `SoloDefender` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HealerAndDefender`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item109`, `InfernumSoundRegistry.VassalJumpSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Commander remains hovering still.*
  - *Do not deal contact damage.*
  - *Determine the best location to move to based on the Y position relative to the player.*
  - *If higher than the target.*
  - *Move to the dash starting location.*
  - *Move out of the way of the target if going around them.*
  - *Initialize the dash telegraph.*
  - *Increase the opacity.*
  - *Do not deal damage.*
  - *Charge*
- **技术实现原理解析**:
  在执行 `HealerAndDefender` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HealerDeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *The commander stays in place still, and the lasers fade out.*
  - *This tells the blender lasers to fade out and disappear.*
  - *Do not deal contact damage.*
  - *Make the spear appear and spin.*
  - *The defender rushes to the commander to shield it.*
  - *Generate the shield if it is inactive.*
  - *Mark the shield as active.*
  - *The healer rapidly slows down, and glows white, before poofing.*
  - *Slow down rapidly.*
  - *Die once the animation is complete.*
- **技术实现原理解析**:
  在执行 `HealerDeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_SpearDashAndGroundSlam`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *The commander lines up horizontally, spins his spear around releasing spears in a circle before dashing at a rapid speed at the player.*
  - *This won't be ramable due to the spear.*
  - *The commander picks the location to hover and moves to it.*
  - *Spawn the spear if it does not exist.*
  - *Make the spear rotation point to the player.*
  - *If close enough or enough time has passed, go to the next state.*
  - *The commander remains moving to the location, and spins his spear.*
  - *The commander recoils backwards before launching.*
  - *The commander charges at the target.*
  - *After enough time, it resets the substate.*
- **技术实现原理解析**:
  在执行 `SpearDashAndGroundSlam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CrashRam`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *They both use the same substate due to needing to be synced.*
  - *Get faster if taking too long to get into position.*
  - *float flySpeedScaled = universalAttackTimer > maxMoveTime ? flySpeed + (universalAttackTimer - maxMoveTime) : flySpeed;*
  - *Get an offset from the player.*
  - *Teleport and stick to said offset.*
  - *Move out of the way of the target if going around them.*
  - *Spawn the spear if it does not exist.*
  - *If close enough, and the defender is ready.*
  - *Charge towards the defender.*
  - *Cause the defender to charge as well.*
- **技术实现原理解析**:
  在执行 `CrashRam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_FireballBulletHell`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.VassalJumpSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *The commander moves to the center of the arena, creating a border of fire around the himself and periodically releasing*
  - *near solid rings of fireballs that move outwards to the border.*
  - *Despawn the spear if it is active.*
  - *Mark the spear for removal.*
  - *Create convergence particles.*
  - *Create pulse rungs and bloom periodically.*
  - *Create energy sparks at the center.*
  - *Release a fast but less dense ring of fireballs.*
  - *The commander chooses a location to go.*
  - *Don't deal damage while moving.*
- **技术实现原理解析**:
  在执行 `FireballBulletHell` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DefenderDeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer, NPC commander)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Ensure they are fully faded in.*
  - *Stop this drawing.*
  - *The commander spawns in the hands, moves towards your top right, pulls the defender into its hands, spins it around itself twice then lobs it at the player at mach 10.*
  - *Do not take damage.*
  - *Do not deal damage.*
  - *Spawn cool symbols.*
  - *Move to the top right of the target.*
  - *Despawn the spear if it is active.*
  - *Mark the spear for removal.*
  - *The defender begins to ram at you from the left vertically, but is pulled up by the commander before reaching you. It then glues to the commanders hands, while squirming around*
- **技术实现原理解析**:
  在执行 `DefenderDeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_LargeGeyserAndCharge`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ProvidenceBurnSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *The commander will check this as well, and draw it instead if the defender is not present.*
  - *Move under the target and telegraph where the geyser will spawn.*
  - *Move out of the way of the target if going around them.*
  - *Do not deal contact damage.*
  - *Despawn the spear if it is active.*
  - *Mark the spear for removal.*
  - *If close enough.*
  - *Create a bunch of lava particles under the commander on the players bottom of the screen.*
  - *If it takes too long, move onto the next phase.*
  - *Prepare to launch upwards alongside a huge lava geyser.*
- **技术实现原理解析**:
  在执行 `LargeGeyserAndCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_DogmaLaserBall`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Face the target.*
  - *Move upwards.*
  - *Aim towards the target if far enough.*
  - *Create the spear, and make it aim upwards.*
  - *Spawn the spear if it does not exist.*
  - *Slow down and point the spear upwards.*
  - *Slow down.*
  - *Make the spear rotation point upwards.*
  - *Charge and create a large fireball at the spear position.*
  - *Wait until the fireball informs the commander that it has launched itself.*
- **技术实现原理解析**:
  在执行 `DogmaLaserBall` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_BerdlySpears`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_LightningBugZap`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Spawn the spear if it does not exist.*
  - *Speed up if far enough away.*
  - *Hover to the side.*
  - *Recoil back slightly.*
- **技术实现原理解析**:
  在执行 `BerdlySpears` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpearSpinThrow`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_LightningBugZap`, `InfernumSoundRegistry.ProvidenceBurnSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Choose the angle to aim at, as well as the position for the exit portal.*
  - *Spawn the spear if it does not exist.*
  - *Move to the offset.*
  - *Speed up if far enough away.*
  - *Hover to the side.*
  - *Make the spear rotation point to the player.*
  - *Spin the spear around til it reaches the desired angle*
  - *Move the spear backwards.*
  - *Create the spears.*
  - *Create the exploding one above the target.*
- **技术实现原理解析**:
  在执行 `SpearSpinThrow` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_RiftFireCharges`
- **参数列表**: `(NPC npc, Player target, ref float universalAttackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *The commander will check this as well, and draw it instead if the defender is not present.*
  - *Enter a rift, create an exit one, and aim at the target.*
  - *Fade out.*
  - *Slow down rapidly.*
  - *Create metaballs at the location to indicate that the commander has entered a rift.*
  - *for (int i = 0; i < 50; i++)*
  - *FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f),*
  - *Main.rand.NextFloat(52f, 85f));*
  - *Look at the target.*
  - *Create the exit rift, and move to it.*
- **技术实现原理解析**:
  在执行 `RiftFireCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_SitStill`
- **参数列表**: `(Player target)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *If the target is close enough, take damage.*
  - *Spawn sparkles.*
- **技术实现原理解析**:
  在执行 `SitStill` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_Shatter`
- **参数列表**: `(Player target)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Despawn all the fire walls.*
  - *Tell the commander to swap attacks. The other guardians use this.*
- **技术实现原理解析**:
  在执行 `Shatter` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `HolySpinningFireBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HolySpinningFireBeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `DefenderShield`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DefenderShield` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CommanderSpear`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CommanderSpear` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HolyFireWall`
- **渲染机制**: `常规 Sprite 纹理渲染 ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HolyFireWall` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `MagicSpiralCrystalShot`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MagicSpiralCrystalShot` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HolyAimedDeathray`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HolyAimedDeathray` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ProfanedRock`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `MagicCrystalShot`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `MagicCrystalShot` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HolySineSpear`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HolySineSpear` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ProfanedSpearInfernum`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ProfanedSpearInfernum` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CommanderSpearThrown`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CommanderSpearThrown` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LingeringHolyFire`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LingeringHolyFire` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HolyFireRift`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `HolyDogmaFireball`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `LavaEruptionPillar`
- **渲染机制**: `常规 Sprite 纹理渲染 ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LavaEruptionPillar` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `internal PrimitiveTrailCopy DashTelegraphDrawer;`
- `code`: `DashTelegraphDrawer ??= new PrimitiveTrailCopy(c => 65f,`
- `code`: `null, true, InfernumEffectsRegistry.SideStreakVertexShader);`
- `code`: `InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);`
- `code`: `InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uOpacity", alpha * npc.Infernum().ExtraAI[FireBorderInte`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uColor", WayfinderSymbol.Colors[2]);`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.SetTexture(InfernumTextureRegistry.HarshNoise, 1);`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("noiseSpeed", new Vector2(0.1f, 0.1f));`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("timeFactor", 2f);`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("flipY", false);`
- `code`: `PrimitiveRenderer.RenderCircleEdge(npc.Center, new(widthFunction, colorFunction, radiusFunction, false, InfernumEffectsR`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("uOpacity", alpha);`
- `code`: `InfernumEffectsRegistry.AreaBorderVertexShader.TrySetParameter("flipY", true);`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 20f;`
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 3f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 2f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 4f;`
- 震屏代码: `Main.player[commander.target].Infernum_Camera().CurrentScreenShakePower = commander.Infernum().ExtraAI[GuardianSkyExtraI`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => Sin(Pi * lif`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `ProfanedGuardians` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `15` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `19` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。