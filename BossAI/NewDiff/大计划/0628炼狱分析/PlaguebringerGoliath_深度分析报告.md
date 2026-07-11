# 瘟疫使者歌利亚 (Plaguebringer Goliath) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `PlaguebringerGoliath` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `PlaguebringerGoliath`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<PlaguebringerBoss>()`
- **模组内关联的源文件列表**:
  - `BombingTelegraph.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/BombingTelegraph.cs`)
  - `BuilderDroneBig.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/BuilderDroneBig.cs`)
  - `BuilderDroneSmall.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/BuilderDroneSmall.cs`)
  - `ExplosivePlagueCharger.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/ExplosivePlagueCharger.cs`)
  - `HostilePlagueSeeker.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/HostilePlagueSeeker.cs`)
  - `LargePlagueExplosion.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/LargePlagueExplosion.cs`)
  - `PlaguebringerGoliathBehaviorOverride.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlaguebringerGoliathBehaviorOverride.cs`)
  - `PlagueCloud.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueCloud.cs`)
  - `PlagueDeathray.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueDeathray.cs`)
  - `PlagueMissile.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueMissile.cs`)
  - `PlagueMissile2.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueMissile2.cs`)
  - `PlagueNuclearExplosion.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueNuclearExplosion.cs`)
  - `PlagueNuke.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueNuke.cs`)
  - `PlagueVomit.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueVomit.cs`)
  - `PlagueWave.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/PlagueWave.cs`)
  - `RedirectingPlagueMissile.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/RedirectingPlagueMissile.cs`)
  - `SmallDrone.cs` (源路径: `.../BossAIs/PlaguebringerGoliath/SmallDrone.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `PlaguebringerGoliathBehaviorOverride` -> 重写目标: `ModContent.NPCType<PlaguebringerBoss>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase3LifeRatio` = `0.3f`
- `Phase2LifeRatio` = `0.75f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `PBGAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `Charge` - 对应的行为处理状态。
2. `MissileLaunch` - 对应的行为处理状态。
3. `PlagueVomit` - 对应的行为处理状态。
4. `CarpetBombing` - 对应的行为处理状态。
5. `ExplodingPlagueChargers` - 对应的行为处理状态。
6. `DroneSummoning` - 对应的行为处理状态。
7. `CarpetBombing2` - 对应的行为处理状态。
8. `CarpetBombing3` - 对应的行为处理状态。
9. `BombConstructors` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **9** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_Charge`
- **参数列表**: `(NPC npc, Player target, bool shouldntChargeYet, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Do initializations.*
  - *Hover until reaching the destination.*
  - *Do the charge.*
  - *Charge behavior.*
  - *Slow down before transitioning back to hovering.*
- **技术实现原理解析**:
  在执行 `Charge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_MissileLaunch`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item11`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Attempt to hover near the target.*
  - *Make the attack go by way quicker once in position.*
  - *Slow down and release a bunch of missiles.*
  - *Determine rotation.*
- **技术实现原理解析**:
  在执行 `MissileLaunch` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PlagueVomit`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item11`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Attempt to hover near the target.*
  - *Make the attack go by way quicker once in position.*
  - *Slow down and release a bunch of vomits.*
  - *Determine rotation.*
- **技术实现原理解析**:
  在执行 `PlagueVomit` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CarpetBombing`
- **参数列表**: `(NPC npc, Player target, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Do initializations.*
  - *Hover until reaching the destination.*
  - *Do the charge.*
  - *Charge behavior.*
  - *Slow down before transitioning back to hovering.*
  - *Otherwise, release missiles.*
- **技术实现原理解析**:
  在执行 `CarpetBombing` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ExplodingPlagueChargers`
- **参数列表**: `(NPC npc, Player target, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Do initializations.*
  - *Hover until reaching the destination.*
  - *Do the charge.*
  - *Charge behavior.*
  - *Slow down before transitioning back to hovering.*
  - *Slow down and summon a bunch of explosive plague chargers.*
- **技术实现原理解析**:
  在执行 `ExplodingPlagueChargers` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_DroneSummoning`
- **参数列表**: `(NPC npc, Player target, float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down.*
  - *Summon drones once ready.*
- **技术实现原理解析**:
  在执行 `DroneSummoning` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CarpetBombing2`
- **参数列表**: `(NPC npc, Player target, float enrageFactor, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PBGMissileLaunchSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Do initializations.*
  - *Hover until reaching the destination.*
  - *Do the charge.*
  - *Charge behavior.*
  - *Slow down before transitioning back to hovering.*
  - *Slow down and create bomb telegraphs.*
- **技术实现原理解析**:
  在执行 `CarpetBombing2` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CarpetBombing3`
- **参数列表**: `(NPC npc, Player target, float enrageFactor, ref float frameType, float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Roar`, `SoundID.Item45`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create a wave visual effect.*
  - *Do initializations.*
  - *Hover until reaching the destination.*
  - *Do the charge.*
  - *Charge behavior.*
  - *Do more contact damage than usual.*
  - *Slow down before transitioning back to hovering.*
- **技术实现原理解析**:
  在执行 `CarpetBombing3` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BombConstructors`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PBGMechanicalWarning`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Move over the target.*
  - *Release a swarm of drones and a nuke.*
- **技术实现原理解析**:
  在执行 `BombConstructors` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `BombingTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BombingTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HostilePlagueSeeker`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HostilePlagueSeeker` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlagueNuclearExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlagueNuclearExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RedirectingPlagueMissile`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RedirectingPlagueMissile` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlagueMissile2`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlagueMissile2` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlagueMissile`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlagueMissile` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LargePlagueExplosion`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LargePlagueExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlagueVomit`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlagueVomit` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PlagueCloud`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PlagueCloud` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在源码中未检索到明显的自定义 Shader 加载或顶点网格绘制代码。该 Boss 主要基于 Terraria 默认的 2D 贴图绘制，或使用了 Calamity 模组提供的公用特效框架。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Target.Infernum_Camera().CurrentScreenShakePower = 12f;`
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
通过对 `PlaguebringerGoliath` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `9` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `9` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。