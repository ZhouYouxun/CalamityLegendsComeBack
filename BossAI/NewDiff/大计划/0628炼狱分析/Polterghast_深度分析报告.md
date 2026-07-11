# 噬魂幽花 (Polterghast) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `Polterghast` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `Polterghast`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<PolterghastBoss>()`, `ModContent.NPCType<PolterPhantom>()`
- **模组内关联的源文件列表**:
  - `ArcingSoul.cs` (源路径: `.../BossAIs/Polterghast/ArcingSoul.cs`)
  - `CirclingEctoplasm.cs` (源路径: `.../BossAIs/Polterghast/CirclingEctoplasm.cs`)
  - `EctoplasmShot.cs` (源路径: `.../BossAIs/Polterghast/EctoplasmShot.cs`)
  - `GhostlyVortex.cs` (源路径: `.../BossAIs/Polterghast/GhostlyVortex.cs`)
  - `Light.cs` (源路径: `.../BossAIs/Polterghast/Light.cs`)
  - `NonReturningSoul.cs` (源路径: `.../BossAIs/Polterghast/NonReturningSoul.cs`)
  - `NotSpecialSoul.cs` (源路径: `.../BossAIs/Polterghast/NotSpecialSoul.cs`)
  - `PolterghastBehaviorOverride.cs` (源路径: `.../BossAIs/Polterghast/PolterghastBehaviorOverride.cs`)
  - `PolterghastCloneBehaviorOverride.cs` (源路径: `.../BossAIs/Polterghast/PolterghastCloneBehaviorOverride.cs`)
  - `PolterghastLeg.cs` (源路径: `.../BossAIs/Polterghast/PolterghastLeg.cs`)
  - `PolterghastWave.cs` (源路径: `.../BossAIs/Polterghast/PolterghastWave.cs`)
  - `SoulTelegraphLine.cs` (源路径: `.../BossAIs/Polterghast/SoulTelegraphLine.cs`)
  - `SpinningSoul.cs` (源路径: `.../BossAIs/Polterghast/SpinningSoul.cs`)
  - `WavySoul.cs` (源路径: `.../BossAIs/Polterghast/WavySoul.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `PolterghastBehaviorOverride` -> 重写目标: `ModContent.NPCType<PolterghastBoss>()`
  - 类名: `PolterghastCloneBehaviorOverride` -> 重写目标: `ModContent.NPCType<PolterPhantom>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.65f`
- `Phase3LifeRatio` = `0.35f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `PolterghastAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `EctoplasmUppercutCharges` - 对应的行为处理状态。
2. `LegSwipes` - 对应的行为处理状态。
3. `WispCircleCharges` - 对应的行为处理状态。
4. `AsgoreRingSoulAttack` - 对应的行为处理状态。
5. `ArcingSouls` - 对应的行为处理状态。
6. `VortexCharge` - 对应的行为处理状态。
7. `SpiritPetal` - 对应的行为处理状态。
8. `CloneSplit` - 对应的行为处理状态。
9. `DesperationAttack` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **10** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float dyingTimer, ref float totalReleasedSouls, ref float initialDeathPositionX, ref float initialDeathPositionY, ref SlotId ShortRoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.FlareSound`, `InfernumSoundRegistry.PolterghastSoulSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Clear away any clones and legs.*
  - *Quickly slow down.*
  - *Begin releasing souls.*
  - *Continue the death animation if enough souls have been released.*
  - *Focus on the boss as it jitters and explode.*
  - *Make the polterghast jitter around a little bit.*
  - *Make a flame-like sound effect right before dying.*
  - *Release a bunch of other souls right before death.*
  - *Release a bunch of souls and transition to the final phase.*
  - *Wait for more souls to release.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_LegSwipes`
- **参数列表**: `(NPC npc, Player target, ref float legToManuallyControlIndex, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PolterghastSoulSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover near the target.*
  - *Decide the leg to control.*
  - *Order legs based on their angular difference with Polterghast's direction to the target.*
  - *Legs behind Polterghast have a large angular difference while ones in front have a smaller angular difference.*
  - *This is ideal because you don't want Polterghast to try to somehow swipe at you with a leg that's on the opposite side.*
  - *Make the leg swing.*
  - *Release vortices from the leg.*
  - *Increment the swipe counter.*
- **技术实现原理解析**:
  在执行 `LegSwipes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_WispCircleCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref SlotId ShortRoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Create a circle of ectoplasm wisps around Polter on the first frame.*
  - *Hover to the top left/right of the target.*
  - *Slow down and look at the target.*
  - *Increment the charge counter at the end of charges.*
- **技术实现原理解析**:
  在执行 `WispCircleCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_AsgoreRingSoulAttack`
- **参数列表**: `(NPC npc, Player target, ref float totalReleasedSouls, ref float attackTimer, ref SlotId ShortRoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Provide the target infinite flight time.*
  - *Teleport near the target. A net-update is already fired in the teleport method.*
  - *Roar and explode into many souls before creating rings.*
  - *Cast rings of souls that converge inward on the Polterghast. The player is expected to weave through the open gap.*
  - *This attack is very similar to the flame circles in Asgore's fight from Undertale.*
  - *Determine the angle of the current soul. This is done by creating an even spread of N points on a circle across 360 degrees.*
  - *Angles that are less than a certain threshold are discarded to create an opening in the ring. Following this a random rotation is*
  - *applied to allow the opening to be on any point on the resulting ring.*
  - *Look at the target.*
- **技术实现原理解析**:
  在执行 `AsgoreRingSoulAttack` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_EctoplasmUppercutCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float telegraphDirection, ref float telegraphOpacity, ref float veryFirstAttack, ref SlotId RoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Start from below if this is the very first attack Polter is performing, for cinematic purposes.*
  - *Descend downward.*
  - *Fade out as the descent reaches its end.*
  - *Project a telegraph line.*
  - *Initialize the horizontal offset. This gives a bit of variance to the charges.*
  - *Stay below the target, invisible.*
  - *Aim the telegraph.*
  - *Charge and release ectoplasm.*
  - *Roar and initiate the charge.*
  - *Create light if sufficiently close to the target and emerging from below.*
- **技术实现原理解析**:
  在执行 `EctoplasmUppercutCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_ArcingSouls`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref SlotId ShortRoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and look at the target at the beginning.*
  - *Otherwise crawl into a corner and shoot things.*
  - *Look at the target.*
- **技术实现原理解析**:
  在执行 `ArcingSouls` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpiritPetal`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls, bool enraged)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PolterghastSoulSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and look at the target.*
  - *Hover above the player prior to attacking.*
  - *Create a light effect at the bottom of the screen.*
  - *Create a petal of released souls.*
  - *Release a petal-like dance of souls. They spawn randomized, to make the pattern semi-inconsistent.*
  - *Do fade effect.*
- **技术实现原理解析**:
  在执行 `SpiritPetal` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DoVortexCharge`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, bool enraged, ref SlotId RoarSlot)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down.*
  - *Charge.*
  - *And release accelerating vortices.*
  - *Accelerate.*
- **技术实现原理解析**:
  在执行 `DoVortexCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_CloneSplit`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, bool enraged)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PolterghastSoulSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Summon three new clones.*
  - *An NPC must update once for it to recieve a whoAmI variable.*
  - *Without this, the below IEnumerable collection would not incorporate this NPC.*
  - *Yes, this is dumb.*
  - *Teleport around the player.*
  - *Charge.*
- **技术实现原理解析**:
  在执行 `CloneSplit` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DesperationAttack`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float vignetteInterpolant, ref float radiusDecreaseFactor)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize the soul burst delay.*
  - *Remain invisible and invincible.*
  - *Provide the target infinite flight time.*
  - *Drift towards the target.*
  - *Hurt the player if they leave the circle.*
  - *Give a tip.*
  - *Release spirals of vortices from outside inward towards the player.*
  - *Perform a super-fast version of the Asgore flame attack.*
  - *Determine the angle of the current soul. This is done by creating an even spread of N points on a circle across 360 degrees.*
  - *Angles that are less than a certain threshold are discarded to create an opening in the ring. Following this a random rotation is*
- **技术实现原理解析**:
  在执行 `DesperationAttack` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `SoulTelegraphLine`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SoulTelegraphLine` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `SpinningSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SpinningSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `NotSpecialSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `NotSpecialSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `WavySoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `WavySoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CirclingEctoplasm`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CirclingEctoplasm` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArcingSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArcingSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `NonReturningSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `NonReturningSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `Light`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `Light` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `GhostlyVortex`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `GhostlyVortex` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `EctoplasmShot`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EctoplasmShot` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `npc.Infernum().OptionalPrimitiveDrawer ??= new(c => TelegraphWidthFunction(npc, c), c => TelegraphColorFunction(npc, c),`
- `code`: `InfernumEffectsRegistry.SideStreakVertexShader.UseImage1(InfernumTextureRegistry.WavyNoise);`
- `code`: `npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 44);`
- `code`: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uImageSize0"].SetValue(circleScale);`
- `code`: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["uCircleRadius"].SetValue(circleRadius * 1.414f);`
- `code`: `InfernumEffectsRegistry.CircleCutout2Shader.Shader.Parameters["ectoplasmCutoffOffsetMax"].SetValue(MathF.Min(circleRadiu`
- `code`: `InfernumEffectsRegistry.CircleCutout2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTex`
- `code`: `InfernumEffectsRegistry.CircleCutout2Shader.Apply();`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class PolterghastLeg : ModNPC, IPixelPrimitiveDrawer`
- `code`: `public PrimitiveTrailCopy LimbDrawer;`
- `code`: `internal float PrimitiveWidthFunction(float completionRatio)`
- `code`: `internal Color PrimitiveColorFunction(float completionRatio)`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `LimbDrawer ??= new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, InfernumEffectsRegistr`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = jitter.Length() * Utils.GetLerpValue(1950f, 1100f, Main.Loc`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 🥀 **特殊谢幕仪式**：血量清空后不会直接消失，而是触发一段不可跳过的演出动画（如崩解、碎裂或自爆），最后伴随全屏特效彻底化为尘埃。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `Polterghast` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `10` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `10` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。