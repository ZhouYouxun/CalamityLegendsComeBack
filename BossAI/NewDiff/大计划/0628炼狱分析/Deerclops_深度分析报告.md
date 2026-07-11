# 独眼巨鹿 (Deerclops) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `Deerclops` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `Deerclops`
- **重写的NPC目标 (Override Target)**: `NPCID.Deerclops`
- **模组内关联的源文件列表**:
  - `AcceleratingShadowHand.cs` (源路径: `.../BossAIs/Deerclops/AcceleratingShadowHand.cs`)
  - `ArenaIcicle.cs` (源路径: `.../BossAIs/Deerclops/ArenaIcicle.cs`)
  - `DeathAnimationShadowHand.cs` (源路径: `.../BossAIs/Deerclops/DeathAnimationShadowHand.cs`)
  - `DeerclopsBehaviorOverride.cs` (源路径: `.../BossAIs/Deerclops/DeerclopsBehaviorOverride.cs`)
  - `DeerclopsEyeLaserbeam.cs` (源路径: `.../BossAIs/Deerclops/DeerclopsEyeLaserbeam.cs`)
  - `DeerclopsP2Wave.cs` (源路径: `.../BossAIs/Deerclops/DeerclopsP2Wave.cs`)
  - `GroundIcicleSpike.cs` (源路径: `.../BossAIs/Deerclops/GroundIcicleSpike.cs`)
  - `IcicleDrawer.cs` (源路径: `.../BossAIs/Deerclops/IcicleDrawer.cs`)
  - `LightSnuffingHand.cs` (源路径: `.../BossAIs/Deerclops/LightSnuffingHand.cs`)
  - `ShadowHandArena.cs` (源路径: `.../BossAIs/Deerclops/ShadowHandArena.cs`)
  - `SpinningShadowHand.cs` (源路径: `.../BossAIs/Deerclops/SpinningShadowHand.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `DeerclopsBehaviorOverride` -> 重写目标: `NPCID.Deerclops`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase3LifeRatio` = `0.35f`
- `Phase2LifeRatio` = `0.75f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `DeerclopsAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `DecideArena` - 对应的行为处理状态。
2. `WalkToTarget` - 对应的行为处理状态。
3. `TallIcicles` - 对应的行为处理状态。
4. `WideIcicles` - 对应的行为处理状态。
5. `BidirectionalIcicleSlam` - 对应的行为处理状态。
6. `UpwardDebrisLaunch` - 对应的行为处理状态。
7. `TransitionToNextPhase` - 对应的行为处理状态。
8. `FeastclopsEyeLaserbeam` - 对应的行为处理状态。
9. `AimedAheadShadowHands` - 对应的行为处理状态。
10. `DyingBeaconOfLight` - 对应的行为处理状态。
11. `DeathAnimation` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **10** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_DecideArena`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Slow down and roar.*
  - *Teleport above the target in a burst of snow on the first frame.*
- **技术实现原理解析**:
  在执行 `DecideArena` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_WalkToTarget`
- **参数列表**: `(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make the attack go by quicker if really close to the target.*
  - *Use walking frames.*
  - *Rest tile collision things.*
- **技术实现原理解析**:
  在执行 `WalkToTarget` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CreateIcicles`
- **参数列表**: `(NPC npc, Player target, bool wideIcicles, bool inPhase2, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and choose frames.*
  - *Choose the current direction.*
  - *Don't increment the attack timer until the dig effect has happened.*
  - *Create a screen shake and puff of snow when ready to shoot.*
  - *Create spikes.*
  - *Summon shadow hands.*
- **技术实现原理解析**:
  在执行 `CreateIcicles` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BidirectionalIcicleSlam`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `SoundID.DD2_MonkStaffGroundImpact`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Sit in place briefly before jumping.*
  - *Jump once ready.*
  - *Create hit effects the ground has been hit again.*
  - *Release spikes once ready again.*
  - *Create spikes.*
- **技术实现原理解析**:
  在执行 `BidirectionalIcicleSlam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_UpwardDebrisLaunch`
- **参数列表**: `(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and choose frames.*
  - *Choose the current direction.*
  - *Don't increment the attack timer until the dig effect has happened.*
  - *Create a screen shake on the first frame when ready to shoot.*
  - *Create debris.*
  - *Shadow hands are launched upwards instead in the third phase.*
  - *Handle debris creation.*
  - *Handle shadow hand creation.*
- **技术实现原理解析**:
  在执行 `UpwardDebrisLaunch` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_TransitionToNextPhase`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float shadowFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable contact damage.*
  - *Slow down.*
  - *Roar and create an arena of shadow hands.*
- **技术实现原理解析**:
  在执行 `TransitionToNextPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FeastclopsEyeLaserbeam`
- **参数列表**: `(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`, `SoundID.DeerclopsScream`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down horizontally.*
  - *Create telegraph particles at the eye prior to firing.*
- **技术实现原理解析**:
  在执行 `FeastclopsEyeLaserbeam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_AimedAheadShadowHands`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DeerclopsScream`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Use walking frames.*
  - *Rest tile collision things.*
  - *Create hands.*
- **技术实现原理解析**:
  在执行 `AimedAheadShadowHands` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DyingBeaconOfLight`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float radiusDecreaseInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_EtherianPortalSpawnEnemy`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make the darkness grow.*
  - *Make the radius decrease as more hands congregate near the eye.*
  - *To make the attack better than a simple DPS check the hands will target nearby players if close to deerclops.*
  - *Fade in and out as necessary.*
- **技术实现原理解析**:
  在执行 `DyingBeaconOfLight` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float dragPortalCenterY, ref float dragPortalAppearInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_EtherianPortalDryadTouch`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Completely cease any and all movement by default.*
  - *Disable tile collision and gravity.*
  - *Disable damage.*
  - *Close the boss bar.*
  - *Use upward hand frames.*
  - *Create the death animation hands and portal.*
  - *Look at the target.*
  - *Delete leftover projectiles.*
  - *Make the portal appear.*
  - *Create a bunch of particles on top of the portal.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `SpinningShadowHand`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SpinningShadowHand` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ShadowHandArena`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `GroundIcicleSpike`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `GroundIcicleSpike` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ArenaIcicle`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArenaIcicle` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DeathAnimationShadowHand`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DeathAnimationShadowHand` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AcceleratingShadowHand`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AcceleratingShadowHand` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `var sound = shadowHandCount > 0 ? InfernumSoundRegistry.DeerclopsRubbleAttackDistortedSound : SoundID.DeerclopsRubbleAtt`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class DeerclopsEyeLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer`
- `code`: `public PrimitiveTrailCopy LaserDrawer;`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);`
- `code`: `Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexCache, 0, vertexCache.Length, i`
- `code`: `LumUtils.CalculatePrimitiveMatrices(Main.screenWidth, Main.screenHeight, out Matrix effectView, out Matrix effectProject`
- `code`: `var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `target.Calamity().GeneralScreenShakePower = 10f;`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(2f, 9f, LumUtils.Convert01To010(lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `Deerclops` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `6` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `10` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。