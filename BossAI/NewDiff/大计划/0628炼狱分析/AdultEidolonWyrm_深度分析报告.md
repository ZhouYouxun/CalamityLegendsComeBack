# 原生幻海妖龙 (Adult Eidolon Wyrm) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `AdultEidolonWyrm` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `AdultEidolonWyrm`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<PrimordialWyrmHead>()`, `ModContent.NPCType<PrimordialWyrmBody>()`, `ModContent.NPCType<PrimordialWyrmBody>()`, `ModContent.NPCType<PrimordialWyrmBody>()`
- **模组内关联的源文件列表**:
  - `AbyssalSoul.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AbyssalSoul.cs`)
  - `AbyssalSoulTelegraph.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AbyssalSoulTelegraph.cs`)
  - `AEWHeadBehaviorOverride.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWHeadBehaviorOverride.cs`)
  - `AEWIllusionTelegraphLine.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWIllusionTelegraphLine.cs`)
  - `AEWNightmareWyrm.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWNightmareWyrm.cs`)
  - `AEWSegmentBehaviorOverride.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWSegmentBehaviorOverride.cs`)
  - `AEWSplitForm.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWSplitForm.cs`)
  - `AEWTelegraphLine.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/AEWTelegraphLine.cs`)
  - `BaseAttackingTerminusProjectile.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/BaseAttackingTerminusProjectile.cs`)
  - `CircleCenterTelegraph.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/CircleCenterTelegraph.cs`)
  - `ConvergingLumenylCrystal.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/ConvergingLumenylCrystal.cs`)
  - `DivineLightBolt.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/DivineLightBolt.cs`)
  - `DivineLightLaserbeam.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/DivineLightLaserbeam.cs`)
  - `DivineLightOrb.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/DivineLightOrb.cs`)
  - `HorizontalRayTerminus.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/HorizontalRayTerminus.cs`)
  - `LightCleaveTelegraph.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/LightCleaveTelegraph.cs`)
  - `PsychicBlast.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/PsychicBlast.cs`)
  - `TerminusDeathray.cs` (源路径: `.../BossAIs/AdultEidolonWyrm/TerminusDeathray.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `AEWHeadBehaviorOverride` -> 重写目标: `ModContent.NPCType<PrimordialWyrmHead>()`
  - 类名: `AEWBody1BehaviorOverride` -> 重写目标: `ModContent.NPCType<PrimordialWyrmBody>()`
  - 类名: `AEWBody2BehaviorOverride` -> 重写目标: `ModContent.NPCType<PrimordialWyrmBody>()`
  - 类名: `AEWTailBehaviorOverride` -> 重写目标: `ModContent.NPCType<PrimordialWyrmBody>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- `Phase4LifeRatio` = `0.15f`
- `Phase3LifeRatio` = `0.45f`
- `Phase2LifeRatio` = `0.75f`
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio]`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `AEWAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `// Spawn animation states.
            SnatchTerminus` - 对应的行为处理状态。
2. `ThreateninglyHoverNearPlayer` - 对应的行为处理状态。
3. `// Light attacks.
            BurningGaze` - 对应的行为处理状态。
4. `DisintegratingBeam` - 对应的行为处理状态。
5. `TerminusChase` - 对应的行为处理状态。
6. `// Dark attacks.
            AbyssalNightmareRitual` - 对应的行为处理状态。
7. `ForbiddenUnleash` - 对应的行为处理状态。
8. `ShadowIllusions` - 对应的行为处理状态。
9. `// Neutral attacks.
            SplitFormCharges` - 对应的行为处理状态。
10. `CrystalConstriction` - 对应的行为处理状态。
11. `HammerheadRams` - 对应的行为处理状态。
12. `// Enrage attack.
            RuthlesslyMurderTarget` - 对应的行为处理状态。
13. `// Death animation state.
            DeathAnimation` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **17** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_Despawn`
- **参数列表**: `(NPC npc)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **技术实现原理解析**:
  在执行 `Despawn` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SnatchTerminus`
- **参数列表**: `(NPC npc)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Fade in.*
  - *Disable damage.*
  - *Transition to the next attack if there are no more Terminus instances.*
  - *Fly very, very quickly towards the Terminus.*
  - *Delete the Terminus instance if it's being touched.*
  - *On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.*
- **技术实现原理解析**:
  在执行 `SnatchTerminus` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ThreateninglyHoverNearPlayer`
- **参数列表**: `(NPC npc, Player target, ref float eyeGlowOpacity, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable damage.*
  - *Attempt to hover to the top left/right of the target at first.*
  - *Don't let the attack timer increment.*
  - *Roar after a short delay.*
  - *Slow down and look at the target threateningly before attacking.*
  - *Become opaque.*
  - *Make the eye glowmask gradually fade in.*
- **技术实现原理解析**:
  在执行 `ThreateninglyHoverNearPlayer` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_BurningGaze`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `CommonCalamitySounds.LaserCannonSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Attempt to hover to the top left/right of the target at first.*
  - *Disable contact damage to prevent cheap hits.*
  - *Don't let the attack timer increment.*
  - *Slow down and look at the target threateningly.*
  - *Create a buffer before the attack properly begins, to ensure that the player can reasonably react to it.*
  - *Release eye bursts.*
  - *Be careful when adjusting the range of the offset angle. If it isn't just right then the attack might be invalidated by just literally*
  - *standing still and letting the bolts fly away.*
- **技术实现原理解析**:
  在执行 `BurningGaze` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DisintegratingBeam`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float lightFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item163`, `InfernumSoundRegistry.TerminusLaserbeamSound`, `InfernumSoundRegistry.ProvidenceHolyBlastShootSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Look at the target at first, charging power into the light orb.*
  - *Release light into the orb.*
  - *Cast the light laserbeam.*
  - *Slow spin around in an attempt to hit the player.*
  - *Release perpendicular bolts if the player isn't too close to the laser.*
  - *Make the orb fade away once the laser is going away.*
- **技术实现原理解析**:
  在执行 `DisintegratingBeam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_TerminusChase`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float lightFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Don't do damage if the laser hasn't been cast.*
  - *Summon the Terminus on the first frame.*
  - *Look at the player before attacking.*
- **技术实现原理解析**:
  在执行 `TerminusChase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_AbyssalNightmareRitual`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float darkFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disappear before attacking.*
  - *Teleport below the target and charge.*
  - *Bring the segments to the teleport position.*
  - *Move towards the target after charging.*
- **技术实现原理解析**:
  在执行 `AbyssalNightmareRitual` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ForbiddenUnleash`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float hammerHeadRotation, ref float darkFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item72`, `SoundID.DD2_EtherianPortalOpen`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Open the hammer head to reveal the dark portal.*
  - *Turn into shadow.*
  - *Disable contact damage.*
  - *Move towards the target.*
  - *Release arcing souls and telegraphs outward.*
- **技术实现原理解析**:
  在执行 `ForbiddenUnleash` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ShadowIllusions`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer, ref float darkFormInterpolant)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item165`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *The way this attack works is by keeping a counter for every charge, whether it's a real or fake one.*
  - *This represents which counter index is the one that will spawn the real, damaging AEW.*
  - *Turn to shadow and fade away.*
  - *Cast illusions.*
  - *Hover away from the target if not charging.*
  - *Release spirals of lumenyl crystals that converge in on the player, to make things a bit more complex.*
  - *Fade back in and transition to the next attack.*
- **技术实现原理解析**:
  在执行 `ShadowIllusions` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SplitFormCharges`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item158`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Don't let the attack cycle timer increment if still swimming.*
  - *Disable homing effects.*
  - *Swim away from the target. If they're close to the bottom of the abyss, swim up. Otherwise, swim down.*
  - *Fade out after enough time has passed, in anticipation of the attack.*
  - *Stay below the target once completely invisible.*
  - *Fade back in if ready to transition to the next attack.*
  - *Cast telegraph direction lines. Once they dissipate the split forms will appear and charge.*
  - *Plus-shaped cross.*
  - *X-shaped cross.*
- **技术实现原理解析**:
  在执行 `SplitFormCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_CrystalConstriction`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.Item9`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Decide the spin center on the first frame.*
  - *Spin around the target.*
  - *Disable attacks at first.*
  - *Orient the crystals such that the player starts out squarely in the middle of a gap, for gameplay fairness purposes.*
  - *Release spirals of crystals inward.*
  - *Spawn a lot of light bolts around the player if they leave the circle.*
- **技术实现原理解析**:
  在执行 `CrystalConstriction` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HammerheadRams`
- **参数列表**: `(NPC npc, Player target, float currentPhase, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.AEWIceBurst`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Roar on the first frame as a warning.*
  - *Look at the player before attacking.*
  - *Release an even spread of ice before charging.*
- **技术实现原理解析**:
  在执行 `HammerheadRams` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_RuthlesslyMurderTarget`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Scream very, very loudly on the first frame.*
  - *Be fully opaque.*
  - *The target must die.*
- **技术实现原理解析**:
  在执行 `RuthlesslyMurderTarget` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DeathAnimation`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.AEWDeathAnimationSound`, `InfernumSoundRegistry.TerminusPulseSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Disable damage.*
  - *Disable sounds.*
  - *Slowly attempt to approach the target.*
  - *Periodically emit shockwaves, similar to the crystal hearts in Celeste.*
  - *Item.NewItem(npc.GetSource_Death(), npc.Center, ModContent.ItemType<Terminus>());*
  - *Make the segments gradually fade away.*
  - *Transform into the Terminus if not already defeated.*
  - *If already defeated simply disspear and give loot.*
  - *Clear projectiles.*
- **技术实现原理解析**:
  在执行 `DeathAnimation` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  这是 Boss 的谢幕动画逻辑。此时 `npc.dontTakeDamage` 被强制设为 `true`，以防玩家在动画期间将其击杀或打断。程序会逐步清空所有危险弹幕，开启满屏粒子喷洒，最后执行死亡特效并生成战利品。

#### 📁 方法名: `DoBehavior_RiseAndGrowWings`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Rise upward.*
- **技术实现原理解析**:
  在执行 `RiseAndGrowWings` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_HoverToSideOfTarget`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover to the side of the target that's closest to an abyss wall.*
- **技术实现原理解析**:
  在执行 `HoverToSideOfTarget` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该动作主要负责重定位。Boss 会通过插值方法（例如 `Vector2.Lerp` 配合特定的弹性常数）平滑地移动到玩家的上方或侧面。此阶段一般伴随防御力提升或免伤保护。

#### 📁 方法名: `DoBehavior_AttackTarget`
- **参数列表**: `()`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.TerminusLaserbeamSound`, `InfernumSoundRegistry.AEWIceBurst`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Make the runes fade in.*
  - *Slow down and charge energy.*
  - *Spawn energy particles.*
  - *Jitter in place.*
  - *Fire the funny laser.*
  - *Move towards the target.*
  - *Move horizontally.*
  - *Prepare the bolt barrage.*
  - *Release barrages of bolts in the general direction of the target that they must evade while trying to not get eaten by the AEW.*
  - *Flap wings.*
- **技术实现原理解析**:
  在执行 `AttackTarget` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `AEWIllusionTelegraphLine`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AEWIllusionTelegraphLine` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `PsychicBlast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `PsychicBlast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AEWNightmareWyrm`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AEWNightmareWyrm` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AEWSplitForm`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AEWSplitForm` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CircleCenterTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `BaseAttackingTerminusProjectile`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BaseAttackingTerminusProjectile` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `LightCleaveTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LightCleaveTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ConvergingLumenylCrystal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ConvergingLumenylCrystal` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `AbyssalSoulTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AbyssalSoulTelegraph` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `DivineLightOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DivineLightOrb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AbyssalSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AbyssalSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DivineLightBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DivineLightBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `TerminusDeathray`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `TerminusDeathray` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `AEWTelegraphLine`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AEWTelegraphLine` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public PrimitiveTrailCopy TelegraphDrawer`
- `code`: `public Primitive3DStrip RuneStripDrawer`
- `code`: `public PrimitiveTrailCopy LaserDrawer`
- `code`: `LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);`
- `code`: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- `code`: `public PrimitiveTrailCopy FireDrawer;`
- `code`: `FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticR`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.25f);`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");`
- `code`: `float adjustedAngle = offsetAngle + LumUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * `
- `code`: `internal PrimitiveTrailCopy BeamDrawer;`
- `code`: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVert`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.Remap(Time, 10f, 90f, 20f, 3f);`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `AdultEidolonWyrm` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `14` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `17` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。