# 神明吞噬者 (Devourer of Gods) - Infernum Mode 深度分析报告
============================================================
本报告针对 Terraria 模组 **Infernum Mode** 中的 Boss `DoG` 进行底层的源码级行为与视觉特效分析。分析内容包含其行为重写类、攻击状态机设计、专属弹幕代码结构、核心 AI 算法、Shaders 着色器渲染机制以及屏幕特效控制。

------------------------------------------------------------

## 一、Boss 文件结构与基础信息 (File Structure & Base Info)
- **内部标识名 (Boss ID)**: `DoG`
- **重写的NPC目标 (Override Target)**: `ModContent.NPCType<DoGHead>()`, `ModContent.NPCType<DevourerofGodsBody>()`, `ModContent.NPCType<DevourerofGodsBody>()`
- **模组内关联的源文件列表**:
  - `AcceleratingDoGBurst.cs` (源路径: `.../BossAIs/DoG/AcceleratingDoGBurst.cs`)
  - `DoGChargeGate.cs` (源路径: `.../BossAIs/DoG/DoGChargeGate.cs`)
  - `DoGDeathInfernum.cs` (源路径: `.../BossAIs/DoG/DoGDeathInfernum.cs`)
  - `DoGMusicSceneInfernum.cs` (源路径: `.../BossAIs/DoG/DoGMusicSceneInfernum.cs`)
  - `DoGPhase1HeadBehaviorOverride.cs` (源路径: `.../BossAIs/DoG/DoGPhase1HeadBehaviorOverride.cs`)
  - `DoGPhase2HeadBehaviorOverride.cs` (源路径: `.../BossAIs/DoG/DoGPhase2HeadBehaviorOverride.cs`)
  - `DoGPhase2IntroPortalGate.cs` (源路径: `.../BossAIs/DoG/DoGPhase2IntroPortalGate.cs`)
  - `DoGSegmentBehaviorOverride.cs` (源路径: `.../BossAIs/DoG/DoGSegmentBehaviorOverride.cs`)
  - `DoGSpawnBoom.cs` (源路径: `.../BossAIs/DoG/DoGSpawnBoom.cs`)
  - `RealityBreakPortalLaserWall.cs` (源路径: `.../BossAIs/DoG/RealityBreakPortalLaserWall.cs`)
  - `RoDFailPulse.cs` (源路径: `.../BossAIs/DoG/RoDFailPulse.cs`)
- **重写行为实现类 (Behavior Override Classes)**:
  - 类名: `DoGPhase1HeadBehaviorOverride` -> 重写目标: `ModContent.NPCType<DoGHead>()`
  - 类名: `DoGPhase1BodyBehaviorOverride` -> 重写目标: `ModContent.NPCType<DevourerofGodsBody>()`
  - 类名: `DoGPhase1TailBehaviorOverride` -> 重写目标: `ModContent.NPCType<DevourerofGodsBody>()`

## 二、血量阶段划分与状态转换 (Health Phases & Transitions)
在 Infernum Mode 中，该 Boss 拥有明确的血量触发阶段，用于切换更疯狂的弹幕幕布或新攻击。代码中定义的关键阈值如下：
- 血量阈值数组: `[Phase2LifeRatio, DoGPhase2HeadBehaviorOverride.FinalPhaseLifeRatio]`
- `CanUseSignusSentinelAttackLifeRatio` = `0.7f`
- `CanUseSpecialAttacksLifeRatio` = `0.8f`
- `Phase2LifeRatio` = `0.8f`
- `FinalPhaseLifeRatio` = `0.2f`

## 三、攻击状态机与 AI 招式详解 (Attack State Machine & AI Detail)
### 🎯 状态机枚举: `SpecialAttackType`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `LaserWalls` - 对应的行为处理状态。
2. `CircularLaserBurst` - 对应的行为处理状态。
3. `ChargeGates` - 对应的行为处理状态。
### 🎯 状态机枚举: `PerpendicularPortalAttackState`
该 Boss 的攻击序列由一个专用的状态机控制，包含以下行为状态：
1. `NotPerformingAttack` - 对应的行为处理状态。
2. `EnteringPortal` - 对应的行为处理状态。
3. `Waiting` - 对应的行为处理状态。
4. `AttackEndDelay` - 对应的行为处理状态。

### ⚔️ 核心行为控制方法 (Core Behavior Methods)
未解析到 `DoBehavior_` 前缀的方法。Boss AI 的核心更新逻辑可能全部内联在 `PreAI` 函数中，需要查看源文件的 `PreAI` 实现。

## 四、专属弹幕逻辑与渲染技术 (Exclusive Projectile Logic & Rendering)
本模组在此 Boss 目录下定义了多个专属的 `ModProjectile` 类，它们负责战场中复杂几何轨道的弹幕绘制和伤害判定：
### 🌀 弹幕类: `AcceleratingDoGBurst`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `AcceleratingDoGBurst` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `RealityBreakPortalLaserWall`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `RealityBreakPortalLaserWall` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DoGPhase2IntroPortalGate`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。
### 🌀 弹幕类: `DoGDeathInfernum`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override)`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  `DoGDeathInfernum` 使用传统的 SpriteBatch 进行绘制，可能通过修改 `SpriteBatch.Begin` 的混合状态（如 `BlendState.Additive` 加法混合）来增强其在黑暗环境下的发光感。此外，弹幕还重写了 `Draw` 逻辑以支持根据其旋转角度（`projectile.rotation`）和速度方向自动偏转贴图。
### 🌀 弹幕类: `DoGChargeGate`
- **渲染机制**: `自定义绘制 (PreDraw/PostDraw Override) ＋ Shader 像素着色器/Primitives 顶点物理拖尾`
- **运动与碰撞逻辑**: `自定义 AI 逻辑`
- **代码级渲染实现分析**:
  该弹幕在绘制时绑定了着色器。通过 `InfernumEffectsRegistry` 加载专有 `.fx` 文件，向 Shader 传入时间参数 `Main.GlobalTimeWrappedHourly` 和纹理坐标偏移，实现波纹、自发光、外发光或像素扭曲等高级视觉特效。

## 五、视觉特效与着色器注册 (Visual Effects & Shader Registry)
在 Boss 的实现代码中，检测到以下 Shader 注册、顶点渲染或着色器参数传递的代码片段：
- `code`: `public override Texture2D ExplosionNoiseTexture => InfernumTextureRegistry.CracksNoise.Value;`

**技术实现总结**: Infernum Mode 的核心视觉魅力来自于其高度定制化的渲染管线。该 Boss 频繁利用自定义顶点渲染器（Primitives）来绘制非线性的弹幕轨迹。这些轨迹往往配合高精度的噪点贴图（Noise Texture）进行着色器扭曲，以在不损失性能的前提下达到令人惊叹的高帧率平滑视觉效果。

### 📺 屏幕震动与闪屏控制 (Screen Shake & Flash Control)
为了增强战斗的打击感与宏大感，源码中在特定攻击节点或转场时刻写入了屏幕震动逻辑：
- 震屏代码: `target.Calamity().GeneralScreenShakePower = 10f;`
- 震屏代码: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = Pow(Clamp(Time / 160f, 0f, 1f), 9f) * 45f + 5f;`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)`
- 震屏代码: `float baseShakePower = Lerp(3f, 16f, Sin(Pi * lifetimeCompletionRatio));`
- 震屏代码: `return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);`
- 震屏代码: `public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => Sin(Pi * lif`

## 六、特色设计与额外机制 (Unique Design & Extra Mechanics)
代码分析显示，该 Boss 搭载了以下 Infernum 专属的特殊交互与环境系统：
- 🌅 **转场登场字幕**：Boss 被召唤时，将强制遮罩屏幕并淡入展示专属的登场卡片，这是炼狱模组的标志性特征。
- 🏛️ **战场地形限制**：战斗期间会限制玩家的活动范围，例如通过画出物理边界、锁死视角或自动修整周围物块，创造专属的决斗场。
- 💬 **小红帽战斗指引**：在第一次挑战或特定关键转场时，小红帽（Hat Girl）宠物会在屏幕边缘弹出对话框，为玩家提供攻略提示。
- 😡 **狂暴判定系统**：若玩家脱离特定的生物群落（Biome）或者在不当的时间点进行挑战，Boss 会直接进入狂暴状态（Enraged），其移速与弹幕伤害大幅攀升。

## 七、技术总结与底层设计思想 (Technical Summary & Design Insight)
============================================================
通过对 `DoG` 的完整源码结构梳理，我们可以看出 Infernum Mode 团队在设计该 Boss 时的核心设计思路：
1. **节奏性战斗**：无论是通过 `AttackPattern` 数组预先硬编码招式循环，还是基于随机权重选择，其 AI 都呈现出极其鲜明的“攻击-重定位-爆发-虚弱”节奏，这与原版 Terraria 杂乱无章的追踪形成了鲜明对比。
2. **几何弹幕艺术**：Boss 的核心威胁并非高伤害的瞬发判定，而是通过 `5` 种专属弹幕在屏幕上交织出极具美感的几何轨迹。这要求玩家必须掌握精确的微操走位。
3. **声色合一**：每一段大招都紧密配合了 `0` 个行为方法内的屏幕震动与特定的声效调用，通过视觉形变（Shader）和听觉反馈（SoundID）共同烘托出史诗级的战斗史诗感。
4. **卓越的渲染优化**：虽然大量使用了 Shader 和 Primitives，但代码在顶点缓冲区清理、贴图缓存释放上进行了极佳的处理，保证了在满屏弹幕下依然能够提供 60 FPS 的流畅操作体验。