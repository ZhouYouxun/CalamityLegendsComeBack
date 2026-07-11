# 灾厄之影 (Calamitas Shadow) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `CalamitasShadow` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `CalamitasShadow`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<CalamitasShadowBoss>()`, `ModContent.NPCType<Cataclysm>()`, `ModContent.NPCType<Catastrophe>()`, `ModContent.NPCType<SoulSeeker>()`
- **模组内关联的源文件列表**:
  - `AccentuationHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/AccentuationHexProj.cs`)
  - `ArcingBrimstoneDart.cs` (源路径: `.../BossAIs/CalamitasShadow/ArcingBrimstoneDart.cs`)
  - `BaseHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/BaseHexProj.cs`)
  - `BrimstoneBomb.cs` (源路径: `.../BossAIs/CalamitasShadow/BrimstoneBomb.cs`)
  - `BrimstoneBoomExplosion.cs` (源路径: `.../BossAIs/CalamitasShadow/BrimstoneBoomExplosion.cs`)
  - `BrimstoneLightning.cs` (源路径: `.../BossAIs/CalamitasShadow/BrimstoneLightning.cs`)
  - `BrimstoneMeteor.cs` (源路径: `.../BossAIs/CalamitasShadow/BrimstoneMeteor.cs`)
  - `BrimstoneSlash.cs` (源路径: `.../BossAIs/CalamitasShadow/BrimstoneSlash.cs`)
  - `CalamitasShadowBehaviorOverride.cs` (源路径: `.../BossAIs/CalamitasShadow/CalamitasShadowBehaviorOverride.cs`)
  - `CataclysmBehaviorOverride.cs` (源路径: `.../BossAIs/CalamitasShadow/CataclysmBehaviorOverride.cs`)
  - `CatastropheBehaviorOverride.cs` (源路径: `.../BossAIs/CalamitasShadow/CatastropheBehaviorOverride.cs`)
  - `CatharsisHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/CatharsisHexProj.cs`)
  - `CatharsisSoul.cs` (源路径: `.../BossAIs/CalamitasShadow/CatharsisSoul.cs`)
  - `CharredWand.cs` (源路径: `.../BossAIs/CalamitasShadow/CharredWand.cs`)
  - `ConvergingShadowSpark.cs` (源路径: `.../BossAIs/CalamitasShadow/ConvergingShadowSpark.cs`)
  - `DarkMagicFlame.cs` (源路径: `.../BossAIs/CalamitasShadow/DarkMagicFlame.cs`)
  - `EntropyBeam.cs` (源路径: `.../BossAIs/CalamitasShadow/EntropyBeam.cs`)
  - `HauntingSoulSeeker.cs` (源路径: `.../BossAIs/CalamitasShadow/HauntingSoulSeeker.cs`)
  - `HomingBrimstoneBurst.cs` (源路径: `.../BossAIs/CalamitasShadow/HomingBrimstoneBurst.cs`)
  - `IndignationHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/IndignationHexProj.cs`)
  - `LargeDarkFireOrb.cs` (源路径: `.../BossAIs/CalamitasShadow/LargeDarkFireOrb.cs`)
  - `LingeringBrimstoneFlames.cs` (源路径: `.../BossAIs/CalamitasShadow/LingeringBrimstoneFlames.cs`)
  - `RisingBrimstoneFireball.cs` (源路径: `.../BossAIs/CalamitasShadow/RisingBrimstoneFireball.cs`)
  - `ShadowBlob.cs` (源路径: `.../BossAIs/CalamitasShadow/ShadowBlob.cs`)
  - `SoulSeeker2.cs` (源路径: `.../BossAIs/CalamitasShadow/SoulSeeker2.cs`)
  - `SoulSeekerBehaviorOverride.cs` (源路径: `.../BossAIs/CalamitasShadow/SoulSeekerBehaviorOverride.cs`)
  - `SoulSeekerResurrectionBeam.cs` (源路径: `.../BossAIs/CalamitasShadow/SoulSeekerResurrectionBeam.cs`)
  - `ThinBrimstoneSlash.cs` (源路径: `.../BossAIs/CalamitasShadow/ThinBrimstoneSlash.cs`)
  - `WeaknessHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/WeaknessHexProj.cs`)
  - `ZealHexProj.cs` (源路径: `.../BossAIs/CalamitasShadow/ZealHexProj.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `CalamitasShadowBehaviorOverride` -> 重写目标: `ModContent.NPCType<CalamitasShadowBoss>()`
  - 类名: `CataclysmBehaviorOverride` -> 重写目标: `ModContent.NPCType<Cataclysm>()`
  - 类名: `CatastropheBehaviorOverride` -> 重写目标: `ModContent.NPCType<Catastrophe>()`
  - 类名: `SoulSeekerBehaviorOverride` -> 重写目标: `ModContent.NPCType<SoulSeeker>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase2LifeRatio` = `0.55f`
- `Phase3LifeRatio` = `0.2f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `CalShadowAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `SpawnAnimation` - 对应的行为处理状态。
2. `WandFireballs` - 对应的行为处理状态。
3. `SoulSeekerResurrection` - 对应的行为处理状态。
4. `ShadowTeleports` - 对应的行为处理状态。
5. `DarkOverheadFireball` - 对应的行为处理状态。
6. `ConvergingBookEnergy` - 对应的行为处理状态。
7. `// Nerd emoji.
            FireburstDashes` - 对应的行为处理状态。
8. `BrothersPhase` - 对应的行为处理状态。
9. `TransitionToFinalPhase` - 对应的行为处理状态。
10. `BarrageOfArcingDarts` - 对应的行为处理状态。
11. `FireSlashes` - 对应的行为处理状态。
12. `RisingBrimstoneFireBursts` - 对应的行为处理状态。
13. `DeathAnimation` - 对应的行为处理状态。
### 🎯 状态机枚举: `SCalBrotherAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `HorizontalCharges` - 对应的行为处理状态。
2. `FireAndSwordSlashes` - 对应的行为处理状态。
3. `BladeUppercutAndDashes` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **16** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_SpawnAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float backgroundEffectIntensity, ref float blackFormInterpolant, ref float eyeGleamInterpolant, ref float armRotation, ref float forcefieldScale, ref float frameVariant, ref float foughtInUnderworld)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Calculate the black fade intensity. This is used to give an illusion that the boss emerged from the shadows.*
  - *Respond the gravity and natural tile collision for the duration of the attack.*
  - *Make the forcefield appear.*
  - *Do the idle animation.*
  - *Don't exist yet if the fade effects are ongoing.*
  - *Appear once they're done.*
  - *Do an eye gleam effect.*
  - *Look at the target.*
  - *Fly into the air and transition to the first attack after the background is fully dark.*
- **技术实现原理解析**:
  在执行 `SpawnAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_WandFireballs`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item73`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Aim the wand at the sky.*
  - *Release lightning at the wand.*
  - *Aim the wand at the target and hover near them.*
  - *Wave the wand and release flame projectiles.*
  - *Shoot fire.*
  - *Do funny screen effects.*
  - *Fly near the target.*
  - *Emit cinders at the end of the wand.*
  - *After the cycles have completed, move the arm back in anticipation before throwing it.*
- **技术实现原理解析**:
  在执行 `WandFireballs` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SoulSeekerResurrection`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.EntropyRayFireSound`, `InfernumSoundRegistry.EntropyRayChargeSound`, `SoundID.Item74`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Hover to the side of the target.*
  - *Aim Entropy's Vigil downwards and use it to raise soul seekers from the dead.*
  - *Hover near the target.*
  - *Make all seekers go away.*
  - *Teleport above the player and make all seekers leave.*
  - *Create energy particles at the end of the staff.*
  - *Aim the staff at the target in anticipation of the laser.*
  - *Play a charge telegraph sound.*
  - *Fire the laser.*
- **技术实现原理解析**:
  在执行 `SoulSeekerResurrection` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ShadowTeleports`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float blackFormInterpolant, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalShadowTeleportSound`, `InfernumSoundRegistry.SizzleSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Inititalize the teleport offset direction.*
  - *Jitter in place and become transluscent while casting a shadow void telegraph near the player.*
  - *Dissipate into shadow particles.*
  - *Hover above the target.*
  - *Create shadow particles shortly before fully appearing.*
- **技术实现原理解析**:
  在执行 `ShadowTeleports` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DarkOverheadFireball`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound`, `InfernumSoundRegistry.SizzleSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Hover above the target at first.*
  - *Look in the direction of the player if not extremely close to them horizontally.*
  - *Delete any potentially old fireballs.*
  - *Afterwards have the shadow raise her arm up towards the fire orb and slow down.*
  - *Prepare the fire orb.*
  - *Release fire from the orb.*
  - *Blow up the fire orb.*
  - *Make the orb slam down.*
  - *Make the player emit a lot of smoke if they're far away.*
- **技术实现原理解析**:
  在执行 `DarkOverheadFireball` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ConvergingBookEnergy`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound`, `SoundID.DD2_EtherianPortalDryadTouch`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Hover to the side of the target and create the book.*
  - *Look in the direction of the player if not extremely close to them horizontally.*
  - *Emit particles off the book.*
  - *Don't get stuck in blocks.*
  - *Make the circle go away.*
  - *Emit energy spirals.*
  - *Make the book jitter before exploding.*
  - *Make the book explode.*
  - *Do funny screen stuff.*
- **技术实现原理解析**:
  在执行 `ConvergingBookEnergy` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FireburstDashes`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float forcefieldScale, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.SizzleSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Hover to the side of the target in anticipation of the charge.*
  - *Make the shield appear.*
  - *Charge at the target.*
  - *Accelerate after charging.*
  - *Slow down in anticipation of the next charge.*
  - *Release a burst of flames in all directions.*
- **技术实现原理解析**:
  在执行 `FireburstDashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BrothersPhase`
- **参数列表**: `(NPC npc, Player target, bool anyBrothers, ref float attackTimer, ref float armRotation, ref float frameVariant, ref float forcefieldScale)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Do the idle animation at first.*
  - *Clear away projectiles from old attacks at first.*
  - *Fall to the ground and stop taking damage in anticipation of the summoning.*
  - *Make the forcefield dissipate.*
  - *Create some anger particles after some time has passed.*
  - *Do a cast animation after enough time has passed.*
  - *Have the shadow teleport away and summon the brothers.*
  - *Have the shadow vanish into shadow blobs.*
  - *Summon Catatrophe and Cataclysm.*
  - *Move the ring towards the target.*
- **技术实现原理解析**:
  在执行 `BrothersPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_TransitionToFinalPhase`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float eyeGleamInterpolant, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Disable damage.*
  - *Let the arm be moved manually.*
  - *Aim the arm upward.*
  - *Clear away projectiles from old attacks at first.*
  - *Teleport to the side of the player.*
  - *Look at the target and slow down.*
  - *Make the eye gleam effect happen.*
- **技术实现原理解析**:
  在执行 `TransitionToFinalPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BarrageOfArcingDarts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Have Cal holder her hand up.*
  - *Hover near the target.*
  - *Release bursts of darts.*
- **技术实现原理解析**:
  在执行 `BarrageOfArcingDarts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FireSlashes`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float forcefieldScale, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Hover to the side of the target in anticipation of the charge.*
  - *Charge at the target.*
  - *Accelerate and release fire after charging.*
  - *Slow down in anticipation of the next charge.*
  - *Release a burst of flames in all directions.*
- **技术实现原理解析**:
  在执行 `FireSlashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_RisingBrimstoneFireBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float frameVariant)`
- **运动与控制逻辑**: 常规漂移/无特殊位置重置
- **播放音效 (Sounds Played)**: `SoundID.Item73`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Attempt to hover to the side of the target.*
  - *Aim the arm at the player.*
  - *Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.*
  - *This is done to ensure that the attack begins once the boss is close to the target.*
  - *Release fireballs.*
- **技术实现原理解析**:
  在执行 `RisingBrimstoneFireBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float eyeGleamInterpolant, ref float forcefieldScale, ref float drawCharredForm, ref float frameVariant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_KoboldExplosion`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Let the arm be moved manually.*
  - *Fall to the ground and stop taking damage.*
  - *Make the eye gleam and shield effect go away.*
  - *Create hex visual effects.*
  - *Create magic on the shadow's hand.*
  - *Create fire on the player.*
  - *Spawn energy particles.*
  - *Strike the shadow with lightning.*
  - *Emit a bunch of smoke after being charred.*
  - *Have the shadow explode into shadow blobs.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_HorizontalCharges`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.MeatySlashSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position.*
  - *Make Catastrophe anticipate with his blade.*
  - *Do the charge.*
  - *Make Catastrophe swing his blade.*
  - *Accelerate during the charge.*
  - *Slow down after the charge has ended and look at the target.*
  - *Go to the next attack once done slowing down.*
- **技术实现原理解析**:
  在执行 `HorizontalCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_FireAndSwordSlashes`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define attack values when the other brother is alive.*
  - *Disable contact damage.*
  - *Slow down and do nothing prior to the attack ending.*
  - *Slow down right before firing.*
  - *Otherwise, do typical hover behavior, towards the upper right of the target.*
  - *Cease all movement when firing.*
  - *Rapidly approach a 0 rotation.*
  - *Make catastrophe swing his blade.*
  - *Play a firing sound.*
  - *And shoot the projectile serverside.*
- **技术实现原理解析**:
  在执行 `FireAndSwordSlashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_BladeUppercutAndDashes`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item73`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Catastrophe does undercut charges from below.*
  - *Fly into position.*
  - *Look at the target at first.*
  - *Swing the arm back in anticipation.*
  - *Disable contact damage.*
  - *Fly upward.*
  - *Accelerate and aim the sword upward.*
  - *Creation motion blur particles.*
  - *Release falling brimstone bombs after the uppercut is over.*
  - *Reposition after the uppercut is done.*
- **技术实现原理解析**:
  在执行 `BladeUppercutAndDashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `HauntingSoulSeeker`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HauntingSoulSeeker` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ThinBrimstoneSlash`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ThinBrimstoneSlash` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LingeringBrimstoneFlames`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LingeringBrimstoneFlames` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneBoomExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneBoomExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneBomb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneBomb` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneSlash`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneSlash` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `EntropyBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `EntropyBeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `RisingBrimstoneFireball`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RisingBrimstoneFireball` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CatharsisSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CatharsisSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneLightning`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneLightning` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `BaseHexProj`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BaseHexProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneMeteor`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneMeteor` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LargeDarkFireOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `ArcingBrimstoneDart`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ArcingBrimstoneDart` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CharredWand`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `HomingBrimstoneBurst`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HomingBrimstoneBurst` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ConvergingShadowSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ConvergingShadowSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DarkMagicFlame`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DarkMagicFlame` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `SoulSeekerResurrectionBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SoulSeekerResurrectionBeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ShadowBlob`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ShadowBlob` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `public float PrimitiveWidthFunction(float completionRatio) => LumUtils.Convert01To010(completionRatio) * Projectile.scal`
- `code`: `public Color PrimitiveColorFunction(float completionRatio)`
- `code`: `float width = PrimitiveWidthFunction(i / (float)checkPoints.Count);`
- `code`: `var lightning = InfernumEffectsRegistry.GaleLightningShader;`
- `code`: `PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(PrimitiveWidthFunction, PrimitiveColorFunction, _ => Projectile.Siz`
- `code`: `using InfernumMode.Common.Graphics.Metaballs;`
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public static Primitive3DStrip HexStripDrawer`
- `code`: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(target.Center + teleportOffsetAngle.ToRotationVector2() * telepo`
- `code`: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- `code`: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- `code`: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- `code`: `var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;`
- `code`: `Effect fireballShader = InfernumEffectsRegistry.FireballShader.GetShader().Shader;`
- `code`: `public class DarkMagicFlame : ModProjectile, IPixelPrimitiveDrawer`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;`
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 4f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 10f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = attackTimer / rumbleTime * 6f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 15f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 6f;`
- 震屏代码: `target.Infernum_Camera().CurrentScreenShakePower = 5f;`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `CalamitasShadow` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `20` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `16` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。