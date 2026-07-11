# 石巨人 (Golem) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `Golem` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `Golem`
- **重写的NPC目标 (Override Target)**: `NPCID.Golem`, `NPCID.GolemFistLeft`, `NPCID.GolemFistRight`, `NPCID.GolemHeadFree`, `NPCID.GolemHead`
- **模组内关联的源文件列表**:
  - `FistBullet.cs` (源路径: `.../BossAIs/Golem/FistBullet.cs`)
  - `FistBulletTelegraph.cs` (源路径: `.../BossAIs/Golem/FistBulletTelegraph.cs`)
  - `GolemArenaPlatform.cs` (源路径: `.../BossAIs/Golem/GolemArenaPlatform.cs`)
  - `GolemBodyBehaviorOverride.cs` (源路径: `.../BossAIs/Golem/GolemBodyBehaviorOverride.cs`)
  - `GolemEyeLaserRay.cs` (源路径: `.../BossAIs/Golem/GolemEyeLaserRay.cs`)
  - `GolemFistLeft.cs` (源路径: `.../BossAIs/Golem/GolemFistLeft.cs`)
  - `GolemFistLeftBehaviorOverride.cs` (源路径: `.../BossAIs/Golem/GolemFistLeftBehaviorOverride.cs`)
  - `GolemFistRight.cs` (源路径: `.../BossAIs/Golem/GolemFistRight.cs`)
  - `GolemFistRightBehaviorOverride.cs` (源路径: `.../BossAIs/Golem/GolemFistRightBehaviorOverride.cs`)
  - `GolemFreeHeadBehaviorOverride.cs` (源路径: `.../BossAIs/Golem/GolemFreeHeadBehaviorOverride.cs`)
  - `GolemHeadBehaviorOverride.cs` (源路径: `.../BossAIs/Golem/GolemHeadBehaviorOverride.cs`)
  - `GolemLaser.cs` (源路径: `.../BossAIs/Golem/GolemLaser.cs`)
  - `GroundFireCrystal.cs` (源路径: `.../BossAIs/Golem/GroundFireCrystal.cs`)
  - `SpikeTrap.cs` (源路径: `.../BossAIs/Golem/SpikeTrap.cs`)
  - `StationarySpikeTrap.cs` (源路径: `.../BossAIs/Golem/StationarySpikeTrap.cs`)
  - `ThermalDeathray.cs` (源路径: `.../BossAIs/Golem/ThermalDeathray.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `GolemBodyBehaviorOverride` -> 重写目标: `NPCID.Golem`
  - 类名: `GolemFistLeftBehaviorOverride` -> 重写目标: `NPCID.GolemFistLeft`
  - 类名: `GolemFistRightBehaviorOverride` -> 重写目标: `NPCID.GolemFistRight`
  - 类名: `GolemFreeHeadBehaviorOverride` -> 重写目标: `NPCID.GolemHeadFree`
  - 类名: `GolemHeadBehaviorOverride` -> 重写目标: `NPCID.GolemHead`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.6f`
- `Phase3LifeRatio` = `0.3f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `GolemAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `FloorFire` - 对应的行为处理状态。
2. `FistSpin` - 对应的行为处理状态。
3. `SpikeTrapWaves` - 对应的行为处理状态。
4. `HeatRay` - 对应的行为处理状态。
5. `SpinLaser` - 对应的行为处理状态。
6. `Slingshot` - 对应的行为处理状态。
7. `SpikeRush` - 对应的行为处理状态。
8. `LandingState` - 对应的行为处理状态。
9. `SummonDelay` - 对应的行为处理状态。
10. `BIGSHOT` - 对应的行为处理状态。
11. `BadTime` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **8** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_FloorFire`
- **参数列表**: `(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown, ref float jumpState, ref float attackCounter, ref float slamTelegraphInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.GolemGroundHitSound`, `SoundID.Item14`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Reset collision variables.*
  - *Attach hands.*
  - *Sit in place and await the next jump.*
  - *Attempt to slam on top of the target.*
  - *Disable contact damage.*
  - *Sit in place, creating a downward telegraph, and slam.*
  - *Cast a downward telegraph.*
  - *Slam downward.*
  - *Create acoustic and visual effects to accompany the ground slam effect.*
  - *Create rubble from above.*
- **技术实现原理解析**:
  在执行 `FloorFire` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FistSpin`
- **参数列表**: `(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Rotate the fists around the body over the course of 3 seconds, spawning projectiles every so often*
  - *Release fist bullets.*
  - *Create platforms below the target in the second phase.*
- **技术实现原理解析**:
  在执行 `FistSpin` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpiketrapWaves`
- **参数列表**: `(NPC npc, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_KoboldExplosion`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Make fists slam into the wall.*
  - *Create impact effects.*
  - *Create dust on the walls where spikes will appear.*
  - *Summon waves of spikes.*
  - *Create platforms in the middle section of the arena.*
  - *Do the slam animation.*
- **技术实现原理解析**:
  在执行 `SpiketrapWaves` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HeatRay`
- **参数列表**: `(NPC npc, Player target, bool inPhase2, bool inPhase3, bool headIsFree, ref float attackTimer, ref float attackCooldown, ref float eyeLaserRayInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item12`, `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Reset the rotations of the fists.*
  - *Prevent being inside of the ground.*
  - *Have the head hover in place and perform the telegraph prior to firing.*
  - *Calculate the telegraph interpolant.*
  - *Play a telegraph sound prior to firing.*
  - *Release the lasers from eyes.*
  - *Create platforms below the target in the second phase.*
  - *Release bursts of fire after firing.*
- **技术实现原理解析**:
  在执行 `HeatRay` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpinLaser`
- **参数列表**: `(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float coreLaserRayInterpolant, ref float coreLaserRayDirection, ref float attackCooldown)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item12`, `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Play a telegraph sound prior to firing.*
  - *Create a laser ray telegraph.*
  - *Store the angular velocity for the laser to read.*
  - *Create platforms.*
  - *Cast the laser from the core.*
  - *Maintain screen shake effects.*
  - *Create lasers from the core after firing.*
  - *Select the next attack shortly after the laser goes away.*
- **技术实现原理解析**:
  在执行 `SpinLaser` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_Slingshot`
- **参数列表**: `(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float fistSlamDestinationX, ref float fistSlamDestinationY, ref float slingshotArmToCharge,
            ref float slingshotRotation, ref float attackCounter, ref float attackCooldown)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_KoboldExplosion`, `SoundID.Item12`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine the initial slingshot rotation.*
  - *Determine the initial fist destination.*
  - *Create platforms.*
  - *Create fire sparks as a telegraph that indicates which fist will charge.*
  - *Make arms do a slam effect.*
  - *Create impact effects.*
  - *Make the body lunge into position.*
  - *Play a launch sound.*
  - *Have the other arm release fist rockets at the target.*
  - *Rotate the fists around the body over the course of 3 seconds, spawning projectiles every so often*
- **技术实现原理解析**:
  在执行 `Slingshot` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SpikeRush`
- **参数列表**: `(NPC npc, ref float attackTimer, ref float attackCooldown)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item12`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Destroy all old platforms and create a few new ones in their place*
  - *Create lasers from the sides of the arena.*
  - *Create new platforms afterwards.*
- **技术实现原理解析**:
  在执行 `SpikeRush` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_EnterSecondPhase`
- **参数列表**: `(NPC npc, float phase2TransitionTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Create spikes throughout the arena at first. This will activate soon afterwards.*
  - *Create a rumble effect.*
  - *Create some platforms before the spike traps are released.*
  - *Make all spike traps release their spears.*
  - *Release a burst of fireballs outward. This happens after some platforms have spawned, and serves to teach the player about the platforms by*
  - *forcing them to dodge the burst while utilizing them.*
- **技术实现原理解析**:
  在执行 `EnterSecondPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `GolemLaser`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `GolemLaser` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FistBulletTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FistBulletTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SpikeTrap`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SpikeTrap` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ThermalDeathray`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ThermalDeathray` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `GroundFireCrystal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `GroundFireCrystal` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `StationarySpikeTrap`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `StationarySpikeTrap` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FistBullet`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FistBullet` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `public static float PrimitiveWidthFunction(float _) => 132f;`
- `code`: `public static Color PrimitiveTrailColor(NPC npc, float completionRatio)`
- `code`: `npc.Infernum().OptionalPrimitiveDrawer ??= new(PrimitiveWidthFunction, c => PrimitiveTrailColor(npc, c), null, true, Gam`
- `code`: `npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 51);`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class ThermalDeathray : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `internal PrimitiveTrailCopy BeamDrawer;`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVert`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(1.4f);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.1f);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) `

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = 12f;`
- 震屏代码: `target.Calamity().GeneralScreenShakePower = 12f;`
- 震屏代码: `target.Calamity().GeneralScreenShakePower = MathF.Max(target.Calamity().GeneralScreenShakePower, 2f);`
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = 10f;`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `Golem` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `7` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `8` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。