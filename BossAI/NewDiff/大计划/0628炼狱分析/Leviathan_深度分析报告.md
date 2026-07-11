# 利维坦与阿娜希塔 (Leviathan & Anahita) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `Leviathan` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `Leviathan`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<Anahita>()`, `ModContent.NPCType<LeviathanNPC>()`
- **模组内关联的源文件列表**:
  - `AnahitaBehaviorOverride.cs` (源路径: `.../BossAIs/Leviathan/AnahitaBehaviorOverride.cs`)
  - `AnahitaWaterIllusion.cs` (源路径: `.../BossAIs/Leviathan/AnahitaWaterIllusion.cs`)
  - `AquaticAberrationProj.cs` (源路径: `.../BossAIs/Leviathan/AquaticAberrationProj.cs`)
  - `AtlantisSpear.cs` (源路径: `.../BossAIs/Leviathan/AtlantisSpear.cs`)
  - `AtlantisSpear2.cs` (源路径: `.../BossAIs/Leviathan/AtlantisSpear2.cs`)
  - `HeavenlyLullaby.cs` (源路径: `.../BossAIs/Leviathan/HeavenlyLullaby.cs`)
  - `LeviathanBehaviorOverride.cs` (源路径: `.../BossAIs/Leviathan/LeviathanBehaviorOverride.cs`)
  - `LeviathanComboAttackManager.cs` (源路径: `.../BossAIs/Leviathan/LeviathanComboAttackManager.cs`)
  - `LeviathanMeteor.cs` (源路径: `.../BossAIs/Leviathan/LeviathanMeteor.cs`)
  - `LeviathanSpawner.cs` (源路径: `.../BossAIs/Leviathan/LeviathanSpawner.cs`)
  - `LeviathanSpawnWave.cs` (源路径: `.../BossAIs/Leviathan/LeviathanSpawnWave.cs`)
  - `LeviathanVomit.cs` (源路径: `.../BossAIs/Leviathan/LeviathanVomit.cs`)
  - `RedirectingWaterBolt.cs` (源路径: `.../BossAIs/Leviathan/RedirectingWaterBolt.cs`)
  - `WaterBolt.cs` (源路径: `.../BossAIs/Leviathan/WaterBolt.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `AnahitaBehaviorOverride` -> 重写目标: `ModContent.NPCType<Anahita>()`
  - 类名: `LeviathanBehaviorOverride` -> 重写目标: `ModContent.NPCType<LeviathanNPC>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `LeviathanSummonLifeRatio` = `0.5f`
- `AnahitaReturnLifeRatio` = `0.5f`
- 血量阈值数组: `[LeviathanSummonLifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `AnahitaAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `// Alone attacks.
            FloatTowardsPlayer` - 对应的行为处理状态。
2. `CreateWaterIllusions` - 对应的行为处理状态。
3. `PlaySinusoidalSong` - 对应的行为处理状态。
4. `IceMistBarrages` - 对应的行为处理状态。
5. `ChargeAndCreateWaterCircle` - 对应的行为处理状态。
6. `// Alone and enraged attacks.
            AtlantisCharge` - 对应的行为处理状态。
### 🎯 状态机枚举: `LeviathanAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `// Alone attacks.
            VomitBlasts` - 对应的行为处理状态。
2. `HorizontalCharges` - 对应的行为处理状态。
3. `MeteorBelch` - 对应的行为处理状态。
4. `// Alone and enraged attacks.
            AberrationCharges` - 对应的行为处理状态。
### 🎯 状态机枚举: `LeviathanComboAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `UpwardRedirectingWaterSpears` - 对应的行为处理状态。
2. `ExoTwinsBasicShotsPrecursor` - 对应的行为处理状态。
3. `AngeringSong` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **14** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_SummonLeviathan`
- **参数列表**: `(NPC npc, ref float hasSummonedLeviathan)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *play forbidden lullaby at phase transition*
  - *Force Anahita to use charging frames.*
  - *Descend back into the ocean.*
  - *Set the whoAmI variable.*
- **技术实现原理解析**:
  在执行 `SummonLeviathan` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FloatTowardsPlayer`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **技术实现原理解析**:
  在执行 `FloatTowardsPlayer` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CreateWaterIllusions`
- **参数列表**: `(NPC npc, Player target, bool enraged, ref float attackTimer, ref float horizontalAfterimageInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.Item28`, `SoundID.Item165`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define rotation.*
  - *Play a wacky sound after descending.*
  - *Fade out before teleporting above the target and creating water illusions.*
  - *Teleport and create the illusions.*
  - *Hover to the top left/right of the target.*
  - *Shoot bolts of water at the target.*
  - *Yes, npc.spriteDirection is used here. Entities do not have a sprite direction defined, but all illusions*
  - *inherit their sprite direction from Anahita herself, so it is safe to do this.*
- **技术实现原理解析**:
  在执行 `CreateWaterIllusions` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PlaySinusoidalSong`
- **参数列表**: `(NPC npc, Player target, bool enraged, Vector2 headPosition, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover to the side of the target and bob up and down.*
  - *Shoot clefs of sound.*
- **技术实现原理解析**:
  在执行 `PlaySinusoidalSong` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_IceMistBarrages`
- **参数列表**: `(NPC npc, Player target, bool enraged, Vector2 headPosition, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LouderPhantomPhoenix`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Drift towards the top of the target.*
  - *Determine direction.*
  - *Periodically release mist.*
  - *Reset opacity and teleport after the delay is finished.*
- **技术实现原理解析**:
  在执行 `IceMistBarrages` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ChargeAndCreateWaterCircle`
- **参数列表**: `(NPC npc, Player target, bool enraged, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Use charging frames and do damage.*
  - *Hover before charging.*
  - *Charge.*
  - *Check to see if water or tiles have been hit. If they have, go to the next attack state and create a bunch of water spears.*
- **技术实现原理解析**:
  在执行 `ChargeAndCreateWaterCircle` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_AtlantisCharge`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_KoboldIgnite`, `SoundID.DD2_PhantomPhoenixShot`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Use charging frames.*
  - *Hover before charging.*
  - *Charge.*
  - *Use Atlantis after charging.*
  - *Do a bit more damage than usual when charging.*
  - *Release idle dust.*
  - *Poke the target with Atlantis if close to them and pointing towards them.*
- **技术实现原理解析**:
  在执行 `AtlantisCharge` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_VomitBlasts`
- **参数列表**: `(NPC npc, Player target, bool enraged, Vector2 mouthPosition, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item45`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine direction.*
  - *Roar before firing.*
  - *Hover to the side of the target.*
  - *Shoot bursts of vomit at the target.*
  - *Handle frame stuff.*
- **技术实现原理解析**:
  在执行 `VomitBlasts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HorizontalCharges`
- **参数列表**: `(NPC npc, Player target, bool enraged, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Roar before charging.*
  - *Initiate the charge.*
  - *Slow down after charging.*
- **技术实现原理解析**:
  在执行 `HorizontalCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_MeteorBelch`
- **参数列表**: `(NPC npc, Player target, bool enraged, Vector2 mouthPosition, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: `SoundID.Item45`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine direction.*
  - *Roar before firing.*
  - *Hover to the side of the target.*
  - *Shoot bursts of vomit at the target.*
  - *Handle frame stuff.*
- **技术实现原理解析**:
  在执行 `MeteorBelch` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_AberrationCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: `SoundID.NPCDeath19`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Determine direction.*
  - *Roar before firing.*
  - *Hover to the side of the target.*
  - *Shoot bursts of vomit at the target.*
  - *Handle frame stuff.*
- **技术实现原理解析**:
  在执行 `AberrationCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_UpwardRedirectingWaterSpears`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item66`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have the Leviathan hover a bit above the side of the target and have Anahita move towards riding on her back.*
  - *Determine direction.*
  - *Have the Leviathan roar before the charge.*
  - *Charge.*
  - *Glue Anahita's position to the back of the Leviathan and fire redirecting spears upward.*
  - *Release spears.*
- **技术实现原理解析**:
  在执行 `UpwardRedirectingWaterSpears` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ExoTwinsBasicShotsPrecursor`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: `SoundID.Item73`, `SoundID.Item60`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have Anahita use rotation.*
  - *Determine direction.*
  - *Hover.*
  - *Have Anahita shoot frost mist and have the Leviathan shoot an exploding meteor.*
- **技术实现原理解析**:
  在执行 `ExoTwinsBasicShotsPrecursor` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_AngeringSong`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Zombie54`, `SoundID.Item26`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Have the Leviathan charge back and forth and summon redirecting aberrations.*
  - *Roar before charging.*
  - *Initiate the charge.*
  - *Summon aberrations while charging.*
  - *Slow down after charging.*
  - *Have Anahita hover above the target, releasing occasional song notes.*
- **技术实现原理解析**:
  在执行 `AngeringSong` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `LeviathanSpawner`
- **渲染机制**: `常规 Sprite 纹理渲染 ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `HeavenlyLullaby`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HeavenlyLullaby` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AtlantisSpear2`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑 (继承或参考原版 AIStyle)`
- **代码级渲染实现分析**:
  `AtlantisSpear2` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LeviathanMeteor`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LeviathanMeteor` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AquaticAberrationProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AquaticAberrationProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LeviathanVomit`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LeviathanVomit` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AnahitaWaterIllusion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AnahitaWaterIllusion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LeviathanSpawnWave`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LeviathanSpawnWave` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `WaterBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `WaterBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RedirectingWaterBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RedirectingWaterBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AtlantisSpear`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑 (继承或参考原版 AIStyle)`
- **代码级渲染实现分析**:
  `AtlantisSpear` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class LeviathanSpawnWave : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `internal PrimitiveTrailCopy TornadoDrawer;`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.Du`
- `code`: `InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Per`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = Pow(Utils.GetLerpValue(180f, 290f, Time, true), 0.3f) * 20f`
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower += LumUtils.Convert01To010(Pow(Utils.GetLerpValue(300f, 440f,`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `Leviathan` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `11` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `14` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。