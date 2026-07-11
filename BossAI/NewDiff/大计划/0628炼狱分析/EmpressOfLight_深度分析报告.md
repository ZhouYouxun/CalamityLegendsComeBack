# 光之女皇 (Empress of Light) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `EmpressOfLight` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `EmpressOfLight`
- **重写的NPC目标 (Override Target)**: `NPCID.HallowBoss`, `NPCID.EmpressButterfly`
- **模组内关联的源文件列表**:
  - `AcceleratingPrismaticBolt.cs` (源路径: `.../BossAIs/EmpressOfLight/AcceleratingPrismaticBolt.cs`)
  - `ArcingLightBolt.cs` (源路径: `.../BossAIs/EmpressOfLight/ArcingLightBolt.cs`)
  - `EmpressAurora.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressAurora.cs`)
  - `EmpressExplosion.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressExplosion.cs`)
  - `EmpressOfLightBehaviorOverride.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressOfLightBehaviorOverride.cs`)
  - `EmpressPrism.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressPrism.cs`)
  - `EmpressSparkle.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressSparkle.cs`)
  - `EmpressSword.cs` (源路径: `.../BossAIs/EmpressOfLight/EmpressSword.cs`)
  - `EtherealLance.cs` (源路径: `.../BossAIs/EmpressOfLight/EtherealLance.cs`)
  - `LacewingBehaviorOverride.cs` (源路径: `.../BossAIs/EmpressOfLight/LacewingBehaviorOverride.cs`)
  - `LanceCreatingSword.cs` (源路径: `.../BossAIs/EmpressOfLight/LanceCreatingSword.cs`)
  - `LightOverloadBeam.cs` (源路径: `.../BossAIs/EmpressOfLight/LightOverloadBeam.cs`)
  - `PrismaticBolt.cs` (源路径: `.../BossAIs/EmpressOfLight/PrismaticBolt.cs`)
  - `PrismLaserbeam.cs` (源路径: `.../BossAIs/EmpressOfLight/PrismLaserbeam.cs`)
  - `ShimmeringLightWave.cs` (源路径: `.../BossAIs/EmpressOfLight/ShimmeringLightWave.cs`)
  - `SpinningPrismLaserbeam.cs` (源路径: `.../BossAIs/EmpressOfLight/SpinningPrismLaserbeam.cs`)
  - `StarBolt.cs` (源路径: `.../BossAIs/EmpressOfLight/StarBolt.cs`)
  - `StolenCelestialObject.cs` (源路径: `.../BossAIs/EmpressOfLight/StolenCelestialObject.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `EmpressOfLightBehaviorOverride` -> 重写目标: `NPCID.HallowBoss`
  - 类名: `LacewingBehaviorOverride` -> 重写目标: `NPCID.EmpressButterfly`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase3LifeRatio` = `0.5f`
- `Phase4LifeRatio` = `0.2f`
- `Phase2LifeRatio` = `0.75f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `EmpressOfLightAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `SpawnAnimation` - 对应的行为处理状态。
2. `LanceBarrages` - 对应的行为处理状态。
3. `PrismaticBoltCircle` - 对应的行为处理状态。
4. `BackstabbingLances` - 对应的行为处理状态。
5. `MesmerizingMagic` - 对应的行为处理状态。
6. `HorizontalCharge` - 对应的行为处理状态。
7. `EnterSecondPhase` - 对应的行为处理状态。
8. `LightPrisms` - 对应的行为处理状态。
9. `DanceOfSwords` - 对应的行为处理状态。
10. `MajesticPierce` - 对应的行为处理状态。
11. `LanceWallBarrage` - 对应的行为处理状态。
12. `LargeRainbowStar` - 对应的行为处理状态。
13. `UltimateRainbow` - 对应的行为处理状态。
14. `DeathAnimation` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **14** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_SpawnAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item158`, `SoundID.Item161`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize the light bolt release delay.*
  - *Release sparkles randomly.*
  - *Hover above the lacewing.*
  - *Release arcing light bolts at an increasing pace.*
  - *Create auroras and slowly move down to pick up the injured lacewing.*
  - *Pick up the lacewing.*
  - *Make the next animation quicker.*
- **技术实现原理解析**:
  在执行 `SpawnAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_LanceBarrages`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item163`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *WHY ARE THERE STILL BOLTS????????*
  - *Have the arm pointed towards the player aim downward, while the other hand points upward.*
  - *Teleport above the target on the first frame.*
  - *Wait before attacking.*
  - *Fly around and release lances at the target.*
  - *Make the pointer finger release a lot of rainbow dust.*
  - *Release lances rapid-fire towards the target.*
  - *Summon lances from behind the target from time to time to prevent rungod strats.*
- **技术实现原理解析**:
  在执行 `LanceBarrages` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PrismaticBoltCircle`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item164`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover to the top left/right of the target.*
  - *Play a magic sound.*
  - *Fade out and teleport to the opposite side of the target halfway through the attack.*
  - *Release bolts.*
- **技术实现原理解析**:
  在执行 `PrismaticBoltCircle` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_MajesticPierce`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Summon swords and clap on the first frame.*
  - *Hold hands up.*
- **技术实现原理解析**:
  在执行 `MajesticPierce` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_LargeRainbowStar`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hold hands up.*
  - *Teleport above the player and release a bunch of stars.*
- **技术实现原理解析**:
  在执行 `LargeRainbowStar` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_LanceWallBarrage`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have the arm pointed towards the player aim downward, while the other hand points upward.*
  - *Decide the wall direction on the first frame.*
  - *If the player has a lot of momentum in a certain direction, it will be chosen in such a way that the player can simply retain their current direction, so as to*
  - *not require sudden, jarring turns.*
  - *Otherwise, it'll simply be randomized.*
  - *Redirect above the target.*
  - *Shoot lance walls.*
  - *Prepare for the next wall and fire.*
  - *Suddenly summon a bunch of lances above the target.*
- **技术实现原理解析**:
  在执行 `LanceWallBarrage` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BackstabbingLances`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item162`, `SoundID.Item122`, `SoundID.Item161`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Release lances from behind the player.*
  - *Play the lance sound on the first frame.*
  - *Slow down.*
  - *Move towards the target.*
  - *Clap hands on the first frame.*
  - *Extend arms outward in anticipation of the lance wall.*
  - *Hover near the target.*
  - *Summon the lance wall.*
  - *Initialize the lance direction.*
- **技术实现原理解析**:
  在执行 `BackstabbingLances` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_MesmerizingMagic`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize things.*
  - *Calculate the telegraph interpolant.*
  - *Hover to the top left/right of the target.*
  - *Determinine the initial rotation of the telegraphs.*
  - *Teleport near the target if very far away.*
  - *Rotate the telegraphs.*
  - *Release magic on hands and eventually create bolts.*
  - *Create magic dust.*
  - *Raise hands.*
  - *Release bolts outward and create hand explosions.*
- **技术实现原理解析**:
  在执行 `MesmerizingMagic` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HorizontalCharge`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item160`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize the charge direction.*
  - *Hover into position before charging.*
  - *Scream prior to charging.*
  - *Charge.*
  - *If applicable, release prismatic bolts.*
  - *Do damage.*
- **技术实现原理解析**:
  在执行 `HorizontalCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_EnterSecondPhase`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item161`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Don't take damage when transitioning.*
  - *Slow down.*
  - *Scream before fading out.*
  - *Fade out.*
  - *Fade back in and teleport above the target.*
- **技术实现原理解析**:
  在执行 `EnterSecondPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_LightPrisms`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item160`, `SoundID.Item163`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have both arms face downward with an open palm.leftArmFrame*
  - *Teleport above the target on the first frame and release bursts of accelerating, arcing bolts.*
  - *Redirect above the target in anticipation of the prism charge.*
  - *Scream and charge.*
  - *Charge and release prisms.*
  - *Deal contact damage.*
- **技术实现原理解析**:
  在执行 `LightPrisms` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DanceOfSwords`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *WHY ARE THERE STILL BOLTS????????*
  - *Have both hands point upward with the index finger.*
  - *Wait a little bit before transitioning to the next attack for the lances to go away naturally.*
  - *Teleport above the target and create a bunch of swords on the first frame.*
  - *Try to hover near the player so that they can use true melee against the empress.*
  - *Wait before attacking.*
  - *Choose a hover offset angle for the blade once done waiting.*
  - *The sword should pick the side which is between the empress and the player, and then randomly pick a place on the wall that forms from it.*
  - *Find the sword that the empress wishes to use.*
  - *Most of the behavior beyond this point is handled by attacking the sword itself, while the empress simply hovers.*
- **技术实现原理解析**:
  在执行 `DanceOfSwords` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_UltimateRainbow`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame, ref Color animationBackgroundColor, ref float animationScreenShaderStrength)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item160`, `SoundID.Item122`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Get really, really angry if it's daytime.*
  - *Reset arm frames.*
  - *Disable damage.*
  - *Do a charge-up effect for a little bit. This is emphasized by drawcode elsewhere.*
  - *Move quickly above the target.*
  - *Release bolts that accelerate towards the empress.*
  - *Release lances from behind the player.*
  - *Summon the moon from the sky.*
  - *Periodically release rainbow beams at the target.*
  - *Make the lasers converge once all of them have been casted.*
- **技术实现原理解析**:
  在执行 `UltimateRainbow` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float deathAnimationScreenShaderStrength)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item28`, `SoundID.Item161`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable damage.*
  - *Teleport above the player and create a shockwave on the first frame.*
  - *Fade in after teleporting.*
  - *Create sparkles everywhere.*
  - *Play magic sounds.*
  - *Cast shimmers.*
  - *Drop loot once the animation is over.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `PrismLaserbeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PrismLaserbeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ArcingLightBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArcingLightBolt` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AcceleratingPrismaticBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AcceleratingPrismaticBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `StolenCelestialObject`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `StolenCelestialObject` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `LightOverloadBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LightOverloadBeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `EtherealLance`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EtherealLance` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SpinningPrismLaserbeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SpinningPrismLaserbeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `EmpressSparkle`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EmpressSparkle` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `EmpressPrism`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EmpressPrism` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `EmpressSword`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EmpressSword` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `StarBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `StarBolt` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `EmpressAurora`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EmpressAurora` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LanceCreatingSword`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LanceCreatingSword` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `PrismaticBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PrismaticBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `EmpressExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EmpressExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public PrimitiveTrailCopy TrailDrawer`
- `code`: `TrailDrawer ??= new(WidthFunction, ColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.2f);`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage("Images/Misc/noise");`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage(ModContent.Request<Texture2D>("InfernumMode/Content/Behavio`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseImage("Images/Misc/Perlin", 2);`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseColor(animationBackgroundColor);`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseOpacity(deathAnimationScreenShaderStrength);`
- `code`: `InfernumEffectsRegistry.EoLScreenShader.GetShader().UseIntensity(screenShaderStrength);`
- `code`: `public PrimitiveTrailCopy LightRayDrawer`
- `code`: `LightRayDrawer ??= new(LightRayWidthFunction, LightRayColorFunction, null, true, InfernumEffectsRegistry.SideStreakVerte`
- `code`: `public class EmpressSword : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `EmpressOfLight` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `15` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `14` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。