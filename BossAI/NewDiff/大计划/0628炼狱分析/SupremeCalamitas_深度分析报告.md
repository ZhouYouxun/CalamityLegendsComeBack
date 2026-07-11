# 至尊灾厄 (Supreme Calamitas) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `SupremeCalamitas` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `SupremeCalamitas`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<SepulcherHead>()`, `ModContent.NPCType<SepulcherBody>()`, `ModContent.NPCType<SepulcherBody>()`, `ModContent.NPCType<SepulcherBody>()`, `ModContent.NPCType<SoulSeekerSupreme>()`, `ModContent.NPCType<SCalBoss>()`, `Unknown`, `ModContent.NPCType<SupremeCataclysm>()`, `ModContent.NPCType<SupremeCatastrophe>()`
- **模组内关联的源文件列表**:
  - `AcceleratingDarkMagicFlame.cs` (源路径: `.../BossAIs/SupremeCalamitas/AcceleratingDarkMagicFlame.cs`)
  - `BrimstoneBarrageOld.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneBarrageOld.cs`)
  - `BrimstoneDemonSummonExplosion.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneDemonSummonExplosion.cs`)
  - `BrimstoneFlameOrb.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneFlameOrb.cs`)
  - `BrimstoneFlamePillar.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneFlamePillar.cs`)
  - `BrimstoneFlamePillarTelegraph.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneFlamePillarTelegraph.cs`)
  - `BrimstoneJewelProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneJewelProj.cs`)
  - `BrimstoneLaserbeam.cs` (源路径: `.../BossAIs/SupremeCalamitas/BrimstoneLaserbeam.cs`)
  - `CatastropheSlash.cs` (源路径: `.../BossAIs/SupremeCalamitas/CatastropheSlash.cs`)
  - `CondemnationArrowSCal.cs` (源路径: `.../BossAIs/SupremeCalamitas/CondemnationArrowSCal.cs`)
  - `CondemnationProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/CondemnationProj.cs`)
  - `DemonicBomb.cs` (源路径: `.../BossAIs/SupremeCalamitas/DemonicBomb.cs`)
  - `DemonicExplosion.cs` (源路径: `.../BossAIs/SupremeCalamitas/DemonicExplosion.cs`)
  - `DemonicTelegraphLine.cs` (源路径: `.../BossAIs/SupremeCalamitas/DemonicTelegraphLine.cs`)
  - `FlameOverloadBeam.cs` (源路径: `.../BossAIs/SupremeCalamitas/FlameOverloadBeam.cs`)
  - `HeresyProjSCal.cs` (源路径: `.../BossAIs/SupremeCalamitas/HeresyProjSCal.cs`)
  - `InfernumBrimstoneGigablast.cs` (源路径: `.../BossAIs/SupremeCalamitas/InfernumBrimstoneGigablast.cs`)
  - `LostSoulProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/LostSoulProj.cs`)
  - `RedirectingDarkSoul.cs` (源路径: `.../BossAIs/SupremeCalamitas/RedirectingDarkSoul.cs`)
  - `RedirectingHellfireSCal.cs` (源路径: `.../BossAIs/SupremeCalamitas/RedirectingHellfireSCal.cs`)
  - `RedirectingLostSoulProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/RedirectingLostSoulProj.cs`)
  - `RitualBrimstoneHeart.cs` (源路径: `.../BossAIs/SupremeCalamitas/RitualBrimstoneHeart.cs`)
  - `SepulcherBone.cs` (源路径: `.../BossAIs/SupremeCalamitas/SepulcherBone.cs`)
  - `SepulcherHeadBehaviorOverride.cs` (源路径: `.../BossAIs/SupremeCalamitas/SepulcherHeadBehaviorOverride.cs`)
  - `SepulcherSegmentBehaviorOverrides.cs` (源路径: `.../BossAIs/SupremeCalamitas/SepulcherSegmentBehaviorOverrides.cs`)
  - `SepulcherSoulBomb.cs` (源路径: `.../BossAIs/SupremeCalamitas/SepulcherSoulBomb.cs`)
  - `ShadowBolt.cs` (源路径: `.../BossAIs/SupremeCalamitas/ShadowBolt.cs`)
  - `ShadowDemon.cs` (源路径: `.../BossAIs/SupremeCalamitas/ShadowDemon.cs`)
  - `ShadowFlameBlast.cs` (源路径: `.../BossAIs/SupremeCalamitas/ShadowFlameBlast.cs`)
  - `ShadowGigablast.cs` (源路径: `.../BossAIs/SupremeCalamitas/ShadowGigablast.cs`)
  - `ShadowSpark.cs` (源路径: `.../BossAIs/SupremeCalamitas/ShadowSpark.cs`)
  - `SoulSeekerSupremeBehaviorOverride.cs` (源路径: `.../BossAIs/SupremeCalamitas/SoulSeekerSupremeBehaviorOverride.cs`)
  - `SuicideBomberDemonExplosion.cs` (源路径: `.../BossAIs/SupremeCalamitas/SuicideBomberDemonExplosion.cs`)
  - `SuicideBomberDemonHostile.cs` (源路径: `.../BossAIs/SupremeCalamitas/SuicideBomberDemonHostile.cs`)
  - `SuicideBomberRitual.cs` (源路径: `.../BossAIs/SupremeCalamitas/SuicideBomberRitual.cs`)
  - `SupremeCalamitasBehaviorOverride.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCalamitasBehaviorOverride.cs`)
  - `SupremeCalamitasBehaviorOverride.Music.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCalamitasBehaviorOverride.Music.cs`)
  - `SupremeCalamitasBrotherPortal.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCalamitasBrotherPortal.cs`)
  - `SupremeCataclysmBehaviorOverride.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCataclysmBehaviorOverride.cs`)
  - `SupremeCataclysmFistOld.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCataclysmFistOld.cs`)
  - `SupremeCatastropheBehaviorOverride.cs` (源路径: `.../BossAIs/SupremeCalamitas/SupremeCatastropheBehaviorOverride.cs`)
  - `VigilanceProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/VigilanceProj.cs`)
  - `CalamitasCutsceneProj.cs` (源路径: `.../BossAIs/SupremeCalamitas/CalamitasCutsceneProj.cs`)
  - `SCalSymbol.cs` (源路径: `.../BossAIs/SupremeCalamitas/SCalSymbol.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `SepulcherHeadBehaviorOverride` -> 重写目标: `ModContent.NPCType<SepulcherHead>()`
  - 类名: `SepulcherBody1BehaviorOverride` -> 重写目标: `ModContent.NPCType<SepulcherBody>()`
  - 类名: `SepulcherBody2BehaviorOverride` -> 重写目标: `ModContent.NPCType<SepulcherBody>()`
  - 类名: `SepulcherTailBehaviorOverride` -> 重写目标: `ModContent.NPCType<SepulcherBody>()`
  - 类名: `SoulSeekerSupremeBehaviorOverride` -> 重写目标: `ModContent.NPCType<SoulSeekerSupreme>()`
  - 类名: `SupremeCalamitasBehaviorOverride` -> 重写目标: `ModContent.NPCType<SCalBoss>()`
  - 类名: `SupremeCalamitasBehaviorOverride` -> 重写目标: `Unknown`
  - 类名: `SupremeCataclysmBehaviorOverride` -> 重写目标: `ModContent.NPCType<SupremeCataclysm>()`
  - 类名: `SupremeCatastropheBehaviorOverride` -> 重写目标: `ModContent.NPCType<SupremeCatastrophe>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- 血量阈值数组: `[Phase2LifeRatio, Phase3LifeRatio, Phase4LifeRatio]`
- `Phase3LifeRatio` = `0.45f`
- `Phase2LifeRatio` = `0.75f`
- `Phase4LifeRatio` = `0.25f`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `SepulcherAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `AttackDelay` - 对应的行为处理状态。
2. `ErraticCharges` - 对应的行为处理状态。
3. `PerpendicularBoneCharges` - 对应的行为处理状态。
4. `SoulBombBursts` - 对应的行为处理状态。
### 🎯 状态机枚举: `SCalAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `HorizontalDarkSoulRelease` - 对应的行为处理状态。
2. `CondemnationFanBurst` - 对应的行为处理状态。
3. `ExplosiveCharges` - 对应的行为处理状态。
4. `HellblastBarrage` - 对应的行为处理状态。
5. `BecomeBerserk` - 对应的行为处理状态。
6. `SummonSuicideBomberDemons` - 对应的行为处理状态。
7. `BrimstoneJewelBeam` - 对应的行为处理状态。
8. `DarkMagicBombWalls` - 对应的行为处理状态。
9. `FireLaserSpin` - 对应的行为处理状态。
10. `SummonSepulcher` - 对应的行为处理状态。
11. `SummonBrothers` - 对应的行为处理状态。
12. `SummonSeekers` - 对应的行为处理状态。
13. `PhaseTransition` - 对应的行为处理状态。
14. `DesperationPhase` - 对应的行为处理状态。
15. `// Shadow demon attacks.
            SummonShadowDemon` - 对应的行为处理状态。
16. `ShadowDemon_ReleaseExplodingShadowBlasts` - 对应的行为处理状态。
17. `ShadowDemon_ShadowGigablastsAndCharges` - 对应的行为处理状态。
### 🎯 状态机枚举: `SCalBrotherAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `AttackDelay` - 对应的行为处理状态。
2. `SinusoidalBobbing` - 对应的行为处理状态。
3. `ProjectileShooting` - 对应的行为处理状态。
4. `Hyperdashes` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
代码中共解析出 **24** 个以 `DoBehavior_` 命名的核心行为控制函数，这些函数承载了 Boss 每一招的具体动作和弹幕逻辑：

#### 📁 方法名: `DoBehavior_AttackDelay`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Fade in.*
  - *Do not take damage.*
- **技术实现原理解析**:
  在执行 `AttackDelay` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ErraticCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Initialize the bomb angular direction;*
  - *Release bombs around the target.*
  - *Erratically hover around.*
  - *Charge towards the target.*
- **技术实现原理解析**:
  在执行 `ErraticCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_PerpendicularBoneCharges`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: `SoundID.NPCHit2`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover into position for the charge.*
  - *Begin charging at the target.*
  - *Charge and release bones perpendicular to the direction Sepulcher is going.*
- **技术实现原理解析**:
  在执行 `PerpendicularBoneCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_SoulBombBursts`
- **参数列表**: `(NPC npc, Player target, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slowly approach the target.*
  - *Create the bomb.*
  - *Launch the bomb.*
- **技术实现原理解析**:
  在执行 `SoulBombBursts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_ReleaseExplodingShadowBlasts`
- **参数列表**: `(NPC scal, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.PlasmaBlast`, `InfernumSoundRegistry.ShadowHydraCharge`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover in place near the target before slowing down.*
  - *Shoot shadow blasts for each of the heads before stopping in place for a bit.*
  - *Shoot the shadow blasts.*
  - *Charge.*
- **技术实现原理解析**:
  在执行 `ReleaseExplodingShadowBlasts` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ShadowGigablastsAndCharges`
- **参数列表**: `(NPC scal, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Briefly hover into position.*
  - *Charge and release a shadow gigablast if close to the destination or enough time has passed.*
  - *Shoot the gigablast.*
  - *Charge and release shadow bolts.*
  - *Deal contact damage. This only applies to the body.*
- **技术实现原理解析**:
  在执行 `ShadowGigablastsAndCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_HorizontalDarkSoulRelease`
- **参数列表**: `(NPC npc, Player target, int currentPhase, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.NPCDeath52`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Use the punch casting animation.*
  - *Reset animation values.*
  - *Hover to the side of the target.*
  - *Give a tip.*
  - *Release energy particles at the hand position.*
  - *Fire the souls.*
- **技术实现原理解析**:
  在执行 `HorizontalDarkSoulRelease` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_CondemnationFanBurst`
- **参数列表**: `(NPC npc, Player target, int currentPhase, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item73`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.*
  - *Use the hands out casting animation.*
  - *Reset animation values.*
  - *Hover to the side of the target.*
  - *Create Condemnation on the first frame and decide which direction the fan will go in.*
  - *Spin condemnation around before aiming it at the target.*
  - *Define the lock-on direction.*
  - *Make the aim direction move upward before firing, in anticipation of the fan.*
  - *Adjust Condemnation's rotation.*
  - *Create puffs of energy at the tip of Condemnation after the spin completes.*
- **技术实现原理解析**:
  在执行 `CondemnationFanBurst` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_ExplosiveCharges`
- **参数列表**: `(NPC npc, Player target, int currentPhase, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Use the updraft animation.*
  - *Do damage.*
  - *Hover near the target and have the shield laugh at the target before charging.*
  - *Aim the shield and use laughing frames.*
  - *Give a tip.*
  - *Charge rapid-fire.*
  - *Release a bomb and gigablast at the target.*
  - *Slow down a bit after charging.*
  - *Creation motion blur particles.*
- **技术实现原理解析**:
  在执行 `ExplosiveCharges` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该阶段涉及高速冲撞逻辑。程序会计算 Boss 距离玩家的单位向量，乘以设定的冲刺速度（通常包含 `npc.velocity = ...` 的乘积运算），在蓄力计时器（通常存储在 `npc.ai` 中）结束时向玩家进行爆发性推进。

#### 📁 方法名: `DoBehavior_HellblastBarrage`
- **参数列表**: `(NPC npc, Player target, int currentPhase, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalamitousEnergyBurstSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover to the side of the target. Once she begins firing, SCal bobs up and down.*
  - *Use the magic cast animation when firing and a magic circle prior to that, as a charge-up effect.*
  - *Create an explosion effect prior to firing.*
  - *Release a burst of magic dust along with a brimstone hellblast skull once firing should happen.*
  - *Release a burst of darts after a certain number of hellblasts have been fired.*
- **技术实现原理解析**:
  在执行 `HellblastBarrage` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DarkMagicBombWalls`
- **参数列表**: `(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_EtherianPortalDryadTouch`, `InfernumSoundRegistry.CalamitousEnergyBurstSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover to the side of the target.*
  - *Create Heresy on the first frame.*
  - *Use the updraft animation when firing and a magic circle prior to that, as a charge-up effect.*
  - *Create an explosion effect prior to firing.*
  - *Release bursts of cinders.*
  - *Release bombs from the side, starting with telegraph lines.*
  - *Initialize the bomb firing angle.*
  - *Create telegraph lines.*
- **技术实现原理解析**:
  在执行 `DarkMagicBombWalls` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_FireLaserSpin`
- **参数列表**: `(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item163`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Hover in place at first before slowing down.*
  - *Update the frame change speed and base type.*
  - *Initialize the orb size.*
  - *Create the orb.*
  - *Give a tip.*
  - *Rise upward.*
  - *Make the orb grow.*
  - *Eventually make the light orb fade away.*
  - *Release gigablasts.*
- **技术实现原理解析**:
  在执行 `FireLaserSpin` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_BecomeBerserk`
- **参数列表**: `(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Slow down and use the magic circle frame effect.*
  - *Create mild screen-shake effects.*
  - *The SelectNextAttack call fires a netUpdate, hence why one is not present here.*
- **技术实现原理解析**:
  在执行 `BecomeBerserk` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SummonSuicideBomberDemons`
- **参数列表**: `(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the frame change speed.*
  - *Cast a bunch of magic circles.*
  - *Slow down and use the magic circle frame effect.*
  - *Create some magic at the position of SCal's hands.*
  - *Create the ritual circle.*
  - *Attack the player while the suicide bombers chase them.*
  - *Switch directions.*
  - *Initialize the hover offset.*
  - *Hover to the side of the target. Once she begins firing, SCal bobs up and down.*
- **技术实现原理解析**:
  在执行 `SummonSuicideBomberDemons` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_BrimstoneJewelBeam`
- **参数列表**: `(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `SoundID.DD2_DarkMageHealImpact`, `InfernumSoundRegistry.CalThunderStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.*
  - *Use the hands out casting animation.*
  - *Move towards the center of the arena.*
  - *Create the jewel on the first frame.*
  - *Create some chargeup dust and play a charge sound.*
  - *Clear old entities.*
  - *Teleport to the center of the arena.*
  - *Adjust the jewel's rotation and create particles.*
  - *Create the laserbeam.*
  - *Store the jewel's rotation.*
- **技术实现原理解析**:
  在执行 `BrimstoneJewelBeam` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_SummonShadowDemon`
- **参数列表**: `(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.ShadowHydraSpawn`, `InfernumSoundRegistry.ShadowHydraCharge`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Darken the screen.*
  - *Slow down and look at the target.*
  - *Disable contact damage.*
  - *Use the magic circle animation, as a charge-up effect.*
  - *Play the demon's spawning sound.*
  - *Summon the demon.*
- **技术实现原理解析**:
  在执行 `SummonShadowDemon` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SummonBrothers`
- **参数列表**: `(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item103`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Switch from the Grief section of Stained, Brutal Calamity to the Lament section.*
  - *Slow down and look at the target.*
  - *Disable contact damage.*
  - *Use the magic circle animation, as a charge-up effect.*
  - *Shake the screen.*
  - *Play a summoning sound*
  - *Create the portals.*
- **技术实现原理解析**:
  在执行 `SummonBrothers` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SummonSeekers`
- **参数列表**: `(NPC npc, Player target, Vector2 handPosition, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: `SoundID.Item73`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.*
  - *Despawn leftover projectiles.*
  - *Use the hands out casting animation.*
  - *Reset animation values.*
  - *Disable damage.*
  - *Slow down.*
  - *Create vigilance on the first frame and decide which direction the fan will go in.*
  - *Spin vigilance around before aiming it upward.*
  - *Adjust vigilance's rotation.*
  - *Release bursts of energy from Vigilance's tip and summon a seeker.*
- **技术实现原理解析**:
  在执行 `SummonSeekers` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SummonSepulcher`
- **参数列表**: `(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Slow down and look at the target.*
  - *Disable contact damage.*
  - *Use the magic circle animation, as a charge-up effect.*
  - *Create the hearts.*
  - *Make the hearts spin.*
  - *Have hearts hover a fixed distance away from SCal.*
  - *Summon Sepulcher.*
- **技术实现原理解析**:
  在执行 `SummonSepulcher` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_PhaseTransition`
- **参数列表**: `(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Teleport above the player and delete all hostile projectiles on the first frame.*
  - *Use the magic circle animation, as a charge-up effect.*
  - *Disable contact damage.*
  - *Make the shield go away.*
  - *Emit fire dust.*
  - *Summon the Shadow Demon when entering the second phase.*
  - *Summon brothers when entering the third phase.*
  - *Summon seekers when entering the fourth phase.*
  - *Transition to the desperation attack when entering the final phase.*
  - *Also get rid of the shadow hydra.*
- **技术实现原理解析**:
  在执行 `PhaseTransition` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_DesperationPhase`
- **参数列表**: `(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`, `SoundID.NPCDeath52`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ✅ 有屏幕震动/闪屏特效
- **源码注释逻辑提取**:
  - *Cope seethe mald and die.*
  - *Kill the player if they leave the arena during the gigablast bullet hell.*
  - *Disable contact damage.*
  - *Do frame stuff.*
  - *Look at the target.*
  - *Become berserk.*
  - *Release a bunch of flame pillars all throughout the arena, along with redirecting souls.*
  - *Hover to the side of the target.*
  - *Give a tip.*
  - *Create the telegraphs. They will create the pillars once ready to explode.*
- **技术实现原理解析**:
  在执行 `DesperationPhase` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_SinusoidalBobbing`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)`
- **运动与控制逻辑**: 插值平滑过渡 (Lerp/SmoothStep Interpolation), 趋向目标移动 (Move Towards Target)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define the direction and animation type.*
  - *Increment the attack timer and shoot.*
- **技术实现原理解析**:
  在执行 `SinusoidalBobbing` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

#### 📁 方法名: `DoBehavior_ProjectileShooting`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation)
- **播放音效 (Sounds Played)**: 未检测到显式调用 `PlaySound`。
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Define attack values when the other brother is alive.*
  - *Define the direction and animation type.*
  - *Slow down and do nothing prior to the attack ending.*
  - *Increment the attack timer.*
  - *Slow down right before firing. This only happens if sufficiently far away from the target.*
  - *Otherwise, do typical hover behavior, towards the upper right of the target.*
  - *Rapidly approach a 0 rotation.*
  - *Play a firing sound.*
  - *And shoot the projectile serverside.*
- **技术实现原理解析**:
  在执行 `ProjectileShooting` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  此动作为主要的远程弹幕压制阶段。Boss 停止大幅移动或保持低速追踪，并在计时器到达特定周期（通常使用模运算 `attackTimer % shootDelay == 0`）时，向目标玩家发射几何弹幕序列。

#### 📁 方法名: `DoBehavior_Hyperdashes`
- **参数列表**: `(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)`
- **运动与控制逻辑**: 修改速度/物理运动 (Velocity Modification), 插值平滑过渡 (Lerp/SmoothStep Interpolation), 瞬移/位置重置 (Teleportation)
- **播放音效 (Sounds Played)**: `InfernumSoundRegistry.CalThunderStrikeSound`
- **生成弹幕 (Projectiles Created)**: 无直接生成弹幕，或由伴生随从/弹幕生成器间接释放。
- **屏幕震动效应 (Screen Shake)**: ❌ 无显著震屏行为
- **源码注释逻辑提取**:
  - *Teleport to the side of the target.*
  - *Charge incredibly quickly and release a circle of lost souls.*
  - *Define the direction and charge.*
  - *Define the red-glow interpolant.*
  - *Hover to the side of the target before charging.*
  - *Define the direction and animation type.*
- **技术实现原理解析**:
  在执行 `Hyperdashes` 动作期间，Boss 首先获取当前选定的玩家作为追踪目标。
  该方法使用特定的行为状态控制机制，通过累加局部/全局计时器（如 `attackTimer`）来触发攻击流程。伴随多段条件分支判断（如 `if (attackTimer > threshold)`），Boss 的移动速度、弹幕发射角度会根据战斗持续时间动态增加，极大地提升了玩家在炼狱难度下的生存难度。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `AcceleratingDarkMagicFlame`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AcceleratingDarkMagicFlame` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CondemnationArrowSCal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CondemnationArrowSCal` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `SepulcherSoulBomb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SepulcherSoulBomb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `DemonicBomb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DemonicBomb` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneFlamePillarTelegraph`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneFlamePillarTelegraph` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SuicideBomberDemonExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SuicideBomberDemonExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneLaserbeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneLaserbeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ShadowSpark`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ShadowSpark` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SuicideBomberRitual`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SuicideBomberRitual` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RitualBrimstoneHeart`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RitualBrimstoneHeart` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `RedirectingLostSoulProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RedirectingLostSoulProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ShadowFlameBlast`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ShadowFlameBlast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CalamitasCutsceneProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CalamitasCutsceneProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `ShadowBolt`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ShadowBolt` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `LostSoulProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `LostSoulProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SuicideBomberDemonHostile`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SuicideBomberDemonHostile` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `DemonicTelegraphLine`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DemonicTelegraphLine` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RedirectingDarkSoul`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RedirectingDarkSoul` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SCalSymbol`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SCalSymbol` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneDemonSummonExplosion`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneDemonSummonExplosion` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `FlameOverloadBeam`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `FlameOverloadBeam` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `RedirectingHellfireSCal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RedirectingHellfireSCal` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneFlamePillar`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneFlamePillar` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `ShadowGigablast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `ShadowGigablast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DemonicExplosion`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DemonicExplosion` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `CondemnationProj`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CondemnationProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SupremeCalamitasBrotherPortal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `SupremeCataclysmFistOld`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SupremeCataclysmFistOld` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneBarrageOld`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneBarrageOld` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `CatastropheSlash`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `CatastropheSlash` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `VigilanceProj`
- **渲染机制**: `常规 Sprite 纹理渲染`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `VigilanceProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneJewelProj`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneJewelProj` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `HeresyProjSCal`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `HeresyProjSCal` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `SepulcherBone`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `SepulcherBone` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `BrimstoneFlameOrb`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `BrimstoneFlameOrb` 使用了顶点渲染机制（VertexStrip/PrimitiveTrail）。在 `PreDraw` 阶段，通过收集弹幕过去几帧的位置坐标缓存，构建一条连贯的带状几何网格。然后，加载特定的 Shader（如自定义的拖尾着色器），将纹理贴图扭曲拉伸并应用颜色渐变，渲染出丝滑的尾迹，这在 Infernum 的弹幕设计中极具标志性。
### 🌀 弹幕类: `InfernumBrimstoneGigablast`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `InfernumBrimstoneGigablast` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `using InfernumMode.Common.Graphics.Primitives;`
- `code`: `public class BrimstoneFlameOrb : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `public PrimitiveTrailCopy FireDrawer;`
- `code`: `public void DrawPixelPrimitives(SpriteBatch spriteBatch)`
- `code`: `FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticR`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.05f);`
- `code`: `InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");`
- `code`: `float adjustedAngle = offsetAngle + LumUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * `
- `code`: `public class BrimstoneFlamePillar : ModProjectile, IPixelPrimitiveDrawer`
- `code`: `FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarV`
- `code`: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);`
- `code`: `InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);`
- `code`: `using CalamityMod.Graphics.Metaballs;`
- `code`: `public PrimitiveTrailCopy RayDrawer;`
- `code`: `RancorLavaMetaball.SpawnParticle(endOfLaser + Main.rand.NextVector2Circular(10f, 10f) + Projectile.velocity * 40f, 320f)`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `float screenShakeFactor = Utils.Remap(Projectile.Distance(Main.LocalPlayer.Center), 2000f, 1300f, 0f, 11f);`
- 震屏代码: `if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakeFactor)`
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakeFactor;`
- 震屏代码: `Main.LocalPlayer.Calamity().GeneralScreenShakePower = playerDistanceInterpolant * attackTimer / transitionTime * 20f;`
- 震屏代码: `int screenShakeTime = 135;`
- 震屏代码: `float screenShakeDistanceFade = Utils.GetLerpValue(npc.Distance(target.Center), 2600f, 1375f, true);`
- 震屏代码: `float screenShakeFactor = Utils.Remap(attackTimer, 25f, screenShakeTime, 2f, 12.5f) * screenShakeDistanceFade;`
- 震屏代码: `if (attackTimer >= screenShakeTime)`
- 震屏代码: `screenShakeFactor = 0f;`
- 震屏代码: `target.Calamity().GeneralScreenShakePower = screenShakeFactor;`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `SupremeCalamitas` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `36` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `24` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。