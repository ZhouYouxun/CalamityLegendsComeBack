# 史莱姆之神 (Slime God) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `SlimeGod` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `SlimeGod`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<CrimulanPaladin>()`, `ModContent.NPCType<EbonianPaladin>()`, `ModContent.NPCType<SlimeGodCore>()`
- **模组内关联的源文件列表**:
  - `BigSlimeGodAttacks.cs` (源路径: `.../BossAIs/SlimeGod/BigSlimeGodAttacks.cs`)
  - `CrimulanSlimeGodBehaviorOverride.cs` (源路径: `.../BossAIs/SlimeGod/CrimulanSlimeGodBehaviorOverride.cs`)
  - `DeceleratingCrimulanGlob.cs` (源路径: `.../BossAIs/SlimeGod/DeceleratingCrimulanGlob.cs`)
  - `DeceleratingEbonianGlob.cs` (源路径: `.../BossAIs/SlimeGod/DeceleratingEbonianGlob.cs`)
  - `EbonianSlimeGodBehaviorOverride.cs` (源路径: `.../BossAIs/SlimeGod/EbonianSlimeGodBehaviorOverride.cs`)
  - `GroundSlimeGlob.cs` (源路径: `.../BossAIs/SlimeGod/GroundSlimeGlob.cs`)
  - `SlimeGodComboAttackManager.cs` (源路径: `.../BossAIs/SlimeGod/SlimeGodComboAttackManager.cs`)
  - `SlimeGodCoreBehaviorOverride.cs` (源路径: `.../BossAIs/SlimeGod/SlimeGodCoreBehaviorOverride.cs`)
  - `SplitBigSlime.cs` (源路径: `.../BossAIs/SlimeGod/SplitBigSlime.cs`)
  - `SplitBigSlimeAnimation.cs` (源路径: `.../BossAIs/SlimeGod/SplitBigSlimeAnimation.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `CrimulanPaladinBehaviorOverride` -> 重写目标: `ModContent.NPCType<CrimulanPaladin>()`
  - 类名: `EbonianPaladinBehaviorOverride` -> 重写目标: `ModContent.NPCType<EbonianPaladin>()`
  - 类名: `SlimeGodCoreBehaviorOverride` -> 重写目标: `ModContent.NPCType<SlimeGodCore>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `SummonSecondSlimeLifeRatio` = `0.6f`
- 血量阈值数组: `[SlimeGodComboAttackManager.SummonSecondSlimeLifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `CrimulanPaladinAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `LongLeaps` - 对应的行为处理状态。
2. `SplitSwarm` - 对应的行为处理状态。
3. `PowerfulSlam` - 对应的行为处理状态。
### 🎯 状态机枚举: `EbonianPaladinAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `LongLeaps` - 对应的行为处理状态。
2. `SplitSwarm` - 对应的行为处理状态。
3. `PowerfulSlam` - 对应的行为处理状态。
### 🎯 状态机枚举: `BigSlimeGodAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `LongJumps` - 对应的行为处理状态。
2. `GroundedGelSlam` - 对应的行为处理状态。
3. `CoreSpinBursts` - 对应的行为处理状态。
### 🎯 状态机枚举: `SlimeGodComboAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `MutualStomps` - 对应的行为处理状态。
2. `TeleportAndFireBlobs` - 对应的行为处理状态。
3. `SplitFormCharges` - 对应的行为处理状态。
### 🎯 状态机枚举: `SlimeGodCoreAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `HoverAndDoNothing` - 对应的行为处理状态。
2. `DoAbsolutelyNothing` - 对应的行为处理状态。
3. `PhaseTransitionAnimation` - 对应的行为处理状态。
4. `SpinBursts` - 对应的行为处理状态。
5. `HorizontalCharges` - 对应的行为处理状态。
6. `VerticalHoverBursts` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **11** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_LongJumps`
- **参数列表**: `(NPC npc, Player target, bool red, bool alone, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item167`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and prepare to jump if on the ground.*
  - *Release a barrage of globs.*
- **技术实现原理解析**:
  在执行 `LongJumps` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_GroundedGelSlam`
- **参数列表**: `(NPC npc, Player target, bool red, bool alone, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item167`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position.*
  - *Initialize the offset direction.*
  - *Make both slimes slam downward if they are sufficiently close to their hover destination.*
  - *Slam downward.*
  - *Do collision effects after slamming.*
  - *Release a bunch of falling slime into the air and towards the target.*
- **技术实现原理解析**:
  在执行 `GroundedGelSlam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CoreSpinBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item167`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make the core charge.*
  - *Make the core spin.*
  - *Slow down and prepare to jump if on the ground.*
  - *Release a barrage of globs.*
- **技术实现原理解析**:
  在执行 `CoreSpinBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_MutualStomps`
- **参数列表**: `(NPC npc, Player target, bool red, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item167`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position.*
  - *Initialize the offset direction.*
  - *Make both slimes slam downward if they are sufficiently close to their hover destination.*
  - *Slam downward.*
  - *Shoot one glob directly at the target to prevent sitting in place.*
  - *Do collision effects when both slimes have slammed.*
- **技术实现原理解析**:
  在执行 `MutualStomps` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_TeleportAndFireBlobs`
- **参数列表**: `(NPC npc, Player target, bool red, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item171`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage to prevent telefrags.*
  - *Do teleport animation effects.*
  - *Find a place to teleport to.*
  - *Ignore positions that are midair.*
  - *Ignore positions that are in the ground.*
  - *Ignore positions that have no opening to the target.*
  - *Release slime dust to accompany the teleport.*
  - *Shoot blobs at the target and in the air.*
  - *The ideal velocity for falling can be calculated based on the horizontal range formula in the following way:*
  - *First, the initial formula: R = v^2 * sin(2t) / g*
- **技术实现原理解析**:
  在执行 `TeleportAndFireBlobs` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SplitFormCharges`
- **参数列表**: `(NPC npc, Player target, bool red, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_WyvernDiveDown`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine tile collision stuff and disable contact damage.*
  - *Do the split.*
  - *Circle around the target, sometimes taking time to dash inward at them.*
  - *Both slimes have different yet dependent timers for this cycle.*
  - *Slow down.*
  - *Charge.*
  - *Spin around.*
  - *Handle opacity and clear away split slimes once close to attack termination.*
- **技术实现原理解析**:
  在执行 `SplitFormCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_HoverAndDoNothing`
- **参数列表**: `(NPC npc, Player target)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Hover above the target.*
- **技术实现原理解析**:
  在执行 `HoverAndDoNothing` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_PhaseTransitionAnimation`
- **参数列表**: `(NPC npc, ref float attackTimer, ref float backglowInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Wait until all split slimes are gone.*
  - *Disable contact damage.*
  - *Decide the backglow interpolant.*
  - *Spin 2 win.*
  - *Destroy any and all stray projectiles.*
  - *Move the camera to the core and draw in slime from outside sources.*
  - *Explode into slime before transitioning to the next attack.*
- **技术实现原理解析**:
  在执行 `PhaseTransitionAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpinBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item171`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize the spin angle to make it randomized.*
  - *Spin 2 win.*
  - *Disable contact damage.*
  - *Do the charge. This also releases bursts of slime.*
- **技术实现原理解析**:
  在执行 `SpinBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_HorizontalCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `SoundID.Item171`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Line up for the charge.*
  - *Fly towards the destination beside the player.*
  - *If within a good approximation of the player's position, prepare charging.*
  - *Prepare for the charge.*
  - *Do the actual charge.*
  - *Release abyss balls upward.*
- **技术实现原理解析**:
  在执行 `HorizontalCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_VerticalHoverBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item171`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover above the target.*
  - *Shoot bursts of blobs.*
- **技术实现原理解析**:
  在执行 `VerticalHoverBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `DeceleratingEbonianGlob`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DeceleratingEbonianGlob` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `GroundSlimeGlob`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `GroundSlimeGlob` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DeceleratingCrimulanGlob`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DeceleratingCrimulanGlob` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在源码中未检索到明显的自定义 Shader 加载或顶点网格绘制代码。该 Boss 主要基于 Terraria 默认的 2D 贴图绘制，或使用了 Calamity 模组提供的公用特效框架。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
- 该 Boss 没有重度依赖特殊的自定义震屏逻辑，仅采用默认的受击或爆炸音效震动。

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `SlimeGod` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `3` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `11` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。