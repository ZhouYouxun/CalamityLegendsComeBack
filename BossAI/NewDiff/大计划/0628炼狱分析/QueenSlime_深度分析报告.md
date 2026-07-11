# 史莱姆皇后 (Queen Slime) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `QueenSlime` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `QueenSlime`
- **重写的NPC目标 (Override Target)**: `NPCID.QueenSlimeBoss`
- **模组内关联的源文件列表**:
  - `BouncingSlimeProj.cs` (源路径: `.../BossAIs/QueenSlime/BouncingSlimeProj.cs`)
  - `FallingCrystal.cs` (源路径: `.../BossAIs/QueenSlime/FallingCrystal.cs`)
  - `FallingGel.cs` (源路径: `.../BossAIs/QueenSlime/FallingGel.cs`)
  - `FallingSpikeSlimeProj.cs` (源路径: `.../BossAIs/QueenSlime/FallingSpikeSlimeProj.cs`)
  - `HallowBlade.cs` (源路径: `.../BossAIs/QueenSlime/HallowBlade.cs`)
  - `HallowBladeLaserbeam.cs` (源路径: `.../BossAIs/QueenSlime/HallowBladeLaserbeam.cs`)
  - `HallowCrystalSpike.cs` (源路径: `.../BossAIs/QueenSlime/HallowCrystalSpike.cs`)
  - `HallowLaserbeam.cs` (源路径: `.../BossAIs/QueenSlime/HallowLaserbeam.cs`)
  - `QueenJewelBeam.cs` (源路径: `.../BossAIs/QueenSlime/QueenJewelBeam.cs`)
  - `QueenSlimeBehaviorOverride.cs` (源路径: `.../BossAIs/QueenSlime/QueenSlimeBehaviorOverride.cs`)
  - `QueenSlimeCrown.cs` (源路径: `.../BossAIs/QueenSlime/QueenSlimeCrown.cs`)
  - `QueenSlimeCrystalSpike.cs` (源路径: `.../BossAIs/QueenSlime/QueenSlimeCrystalSpike.cs`)
  - `QueenSlimeLightWave.cs` (源路径: `.../BossAIs/QueenSlime/QueenSlimeLightWave.cs`)
  - `QueenSlimeSplitFormProj.cs` (源路径: `.../BossAIs/QueenSlime/QueenSlimeSplitFormProj.cs`)
  - `SpinningLaserCrystal.cs` (源路径: `.../BossAIs/QueenSlime/SpinningLaserCrystal.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `QueenSlimeBehaviorOverride` -> 重写目标: `NPCID.QueenSlimeBoss`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.625f`
- 血量阈值数组: `[Phase2LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `QueenSlimeAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `SpawnAnimation` - 对应的行为处理状态。
2. `BasicHops` - 对应的行为处理状态。
3. `GeliticArmyStomp` - 对应的行为处理状态。
4. `FourThousandBlades` - 对应的行为处理状态。
5. `// :4000blades:
            CrystalMaze` - 对应的行为处理状态。
6. `SlimeCongregations` - 对应的行为处理状态。
7. `CrownLasers` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **7** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_SpawnAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float usingWings, ref float wingMotionState)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_WyvernDiveDown`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Teleport above the player on the first frame.*
  - *Interact with tiles again once past a certain point.*
  - *Accelerate downward.*
  - *Handle ground hit effects when ready.*
  - *Charge energy when on the ground.*
  - *Create visual effects to accompany the wings being made.*
- **技术实现原理解析**:
  在执行 `SpawnAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_BasicHops`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item28`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage until the slam, since the hops can be so fast as to be unfair.*
  - *Ignore tiles while jumping.*
  - *Decide wing stuff.*
  - *Perform ground checks. The attack does not begin until this is finished.*
  - *Jump above the target.*
  - *Accelerate downward.*
  - *Teleport above the player and slam down if very far from the target.*
  - *Release crystals while jumping.*
  - *Begin the slam.*
  - *Move above the target before slamming.*
- **技术实现原理解析**:
  在执行 `BasicHops` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_GeliticArmyStomp`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Decide wing stuff.*
  - *Decide which slimes should be spawned.*
  - *Hover into position before slamming downward.*
  - *Disable cheap hits from redirecting.*
  - *Slow down and rise up in anticipation of the slam.*
  - *Summon slimes as the anticipation begins.*
  - *Slow downward and make the summoned slimes do things.*
  - *Slam and accelerate.*
- **技术实现原理解析**:
  在执行 `GeliticArmyStomp` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FourThousandBlades`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float wingMotionState, ref float vibranceInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item163`, `InfernumSoundRegistry.QueenSlimeExplosionSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Disable contact damage universally. It is not relevant for this attack.*
  - *Increase DR due to being still.*
  - *Decide wing stuff.*
  - *Move to the top left/right of the player.*
  - *Rise upward and become increasingly vibrant.*
  - *Release the blade spawning laser thing.*
  - *Release blades towards the laser.*
  - *Create an explosion and make the blades go outward.*
  - *Release lasers outward.*
- **技术实现原理解析**:
  在执行 `FourThousandBlades` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CrystalMaze`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `SoundID.Item28`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage universally. It is not relevant for this attack.*
  - *Increase DR due to being still.*
  - *Decide wing stuff.*
  - *if (wingAnimationTimer >= WingUpdateCycleTime - 1f)*
  - *wingAnimationTimer = WingUpdateCycleTime - 1f;*
  - *Hover above the target.*
  - *Slow down before firing.*
  - *Interact with tiles.*
  - *Create the maze of crystals.*
  - *Give a tip.*
- **技术实现原理解析**:
  在执行 `CrystalMaze` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SlimeCongregations`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float wingMotionState, ref float vibranceInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Decide wing stuff.*
  - *Disable damage.*
  - *Jitter in place.*
  - *Split into flying slimes.*
  - *Prevent the attack timer from incrementing if in the split form.*
  - *Aim the crystal telegraphs once ready to reform.*
- **技术实现原理解析**:
  在执行 `SlimeCongregations` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CrownLasers`
- **参数列表**: `(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState, ref float crownIsAttached)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `SoundID.NPCHit1`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Decide wing stuff.*
  - *Disable contact damage.*
  - *Hover above the target.*
  - *Slow down before firing.*
  - *Release bursts of gel that fall downward.*
- **技术实现原理解析**:
  在执行 `CrownLasers` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `QueenJewelBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `QueenJewelBeam` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SpinningLaserCrystal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SpinningLaserCrystal` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HallowBladeLaserbeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HallowBladeLaserbeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `HallowCrystalSpike`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HallowCrystalSpike` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BouncingSlimeProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BouncingSlimeProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `QueenSlimeCrown`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `QueenSlimeCrown` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HallowBlade`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HallowBlade` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `QueenSlimeCrystalSpike`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `QueenSlimeCrystalSpike` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FallingSpikeSlimeProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FallingSpikeSlimeProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `QueenSlimeSplitFormProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `QueenSlimeSplitFormProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FallingGel`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FallingGel` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FallingCrystal`
- **渲染机制**: `常规 Sprite 纹理渲染 ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FallingCrystal` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class FallingCrystal : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `public PrimitiveTrailCopy TrailDrawer`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `TrailDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameT`
- `code`: `public class HallowBladeLaserbeam : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `public PrimitiveTrailCopy LaserDrawer`
- `code`: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.85f);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(LaserColorFunction(0f));`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.SmokyNoise);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue(1f / 2.7f);`
- `code`: `public class HallowLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.35f);`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;`
- 震屏代码: `if (target.Infernum_Camera().CurrentScreenShakePower < 1.85f)`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 3f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 8f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `QueenSlime` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `12` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `7` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。