# SHPC 左键弹幕机制分析报告 (SHPC Left-Click Ammunition Effects Analysis)

本报告对 SHPC 武器左键所有已注册的 **41 种弹药特效** 进行了全面静态代码走走与机制梳理，主要分析各弹幕的**生命周期管理模式**（直接发射 vs 保留原有光球）以及**死亡/爆炸时的特效与伤害弹幕生成逻辑**。

---

## 概述与统计 (Summary Table)

| 指标 | 统计结果 | 占比 |
| :--- | :--- | :--- |
| **总注册弹药特效数** | 41 | 100% |
| **直接发射 (Direct Launch)** | 25 | 61.0% |
| **保留原有光球 (Retain Original Orb)** | 16 | 39.0% |

---

## 第一部分：弹幕发射模式分析 (Launch Mode Analysis)

### 1. 直接发射 (Direct Launch) - 占比 61.0% (25个)
**定义**：指在 `Effect` 文件中，弹幕出生时（`OnSpawn`）或第一帧更新时（`AI`），将原有的 SHPC 光球弹幕生命周期直接缩短为 1 或 2 帧（`projectile.timeLeft = 1;` / `2;`），或者立即调用销毁方法（`projectile.Kill();`），使得本体不进行实质性飞行或碰撞，而是立刻将控制权交给新生成的一个或多个子弹幕进行战斗。

**直接发射的 25 个弹药类型及接管弹幕列表**：
1. **纯净凝胶 (PurifiedGelEffect)**：生成 1 个 `PurifiedGel_Ball` 纯净凝胶球。
2. **珍珠碎片 (PearlShardEffect)**：生成 3 个珍珠碎片弹幕。
3. **恐惧之魂 (BossSoulofFrightEffect)**：生成追踪金属小齿轮弹幕。
4. **力量之魂 (BossSoulofMightEffect)**：生成 1 个 `BossSoulofMight_Ball` 能量球。
5. **视觉之魂 (BossSoulofSightEffect)**：生成高速激光或眼睛追踪弹幕。
6. **暗影之魂 (SoulofNightEffect)**：生成追踪暗魂弹幕。
7. **泰坦之星 (StarblightSootEffect)**：生成 5~8 个 `StarblightSootShard` 凋星微尘星魂。
8. **灾厄尘 (AshesofCalamityEffect)**：生成追踪的灾厄之火。
9. **深渊细胞 (DepthCellsEffect)**：生成 4 个深渊射线/水流追踪弹幕。
10. **瘟疫罐 (PlagueCellEffect)**：生成 1 个 `PlagueCell_Marked` 追踪瘟疫细胞。
11. **灾劫核心 (CoreOfCalamityEffect)**：生成融合能量弹幕。
12. **冥思溶剂 (FragmentEntropyEffect)**：生成 3 个冥思粘液球。
13. **星云碎片 (FragmentNebulaEffect)**：生成追踪的星云漂浮珠。
14. **星尘碎片 (FragmentStardustEffect)**：生成 `FragmentStardust_Cell` 星尘细胞。
15. **浊火精华 (UnholyEssenceEffect)**：生成 3 个浊火追踪羽毛。
16. **装甲外壳 (ArmoredShellEffect)**：生成追踪的装甲刺碎片。
17. **暗离子体 (DarkPlasmaEffect)**：生成 2 个暗物质能量球。
18. **扭曲虚空 (TwistingNetherEffect)**：生成 `TwistingNether_Blade` 虚空旋转之刃。
19. **化神魂晶 (AscendantSpiritEffect)**：生成化神追踪灵光。
20. **湮灭余烬 (AshesofAnnEffect)**：生成湮灭红黑追踪火球。
21. **唯一材料 Cynosure (CynosureEffect)**：生成全屏追踪的光子弹幕。
22. **恒温能量 (EndothermicEnergyEffect)**：生成冷冻冰锥追踪弹幕。
23. **星流棱晶 (ExoPrismEffect)**：生成 3 个不同的星流激光/导弹弹幕。
24. **梦魇魔能 (NightmareFuelEffect)**：生成多发追踪的夜魔火球。
25. **龙魂碎片 (YharonSoulFragmentEffect)**：生成高速追踪的烈焰龙魂。

---

### 2. 保留原有光球 (Retain Original Orb) - 占比 39.0% (16个)
**定义**：原有光球弹幕不会在出生或首帧立刻死亡，而是正常维持其完整生命周期（保留原有的 `timeLeft` 且在 `AI` 中自主移动），在空中飞行并由该 `Effect` 脚本进行重力模拟、角度控制或额外弹幕发射，最终由于撞击敌人、墙壁或时间耗尽而正常触发 `OnKill` 逻辑。

下面我们将对这 **16 种保留原有光球的弹药** 的爆炸特效和爆炸弹幕生成进行详细分析。

---

## 第二部分：保留光球效果的爆炸机制与特效分析

对于这 16 种保留原有光球的弹幕，其死亡时是否产生爆炸取决于两个核心设计：
1. **默认爆炸特效 (ExplosionPulseFactor)**：决定原版 SHPC 死亡时的光圈与能量脉冲收缩扩散效果。如果为 `0f`，则关闭默认爆炸特效；如果大于 `0f`，则开启且对应缩放。
2. **释放爆炸弹幕 (NewLegendSHPE)**：决定是否在死亡位置生成一个具有独立伤害判定范围的 `NewLegendSHPE` 范围伤害弹幕。

### 16 个保留原有光球特效详细属性表：

| 序号 | 弹药/材料名称 | 对应类名 | 默认爆炸特效<br>`ExplosionPulseFactor` | 是否释放爆炸弹幕<br>`NewLegendSHPE` | 死亡自定义/附加行为说明 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **钨钢能源核心** | `EnergyCoreEffect` | **1f (开启)** | **是 (手动生成)** | 死亡时向四周射出 7 个 `EnergyCore_Spark` 电火花碎片，并生成一个 80x80 的 `NewLegendSHPE`。 |
| 2 | **风暴之颚** | `StormlionMandibleEffect` | **1f (开启)** | **否 (注释化屏蔽)** | 死亡时不触发任何逻辑（生成爆炸弹幕的代码已被注释屏蔽）。但其在**命中敌人**时会扇形发射 5 条 `StormlionMandible_ARC` 连锁电弧。 |
| 3 | **硫磺鳞片** | `SulphuricScaleEffect` | **0f (关闭)** | **是 (手动生成)** | 死亡时生成 75x75 的 `NewLegendSHPE`，伴随大量的离子辐射绿色脉冲和毒雾粒子，并释放 21~29 个带有魔法伤害属性的剧毒云团（`ToxicCloud` 1/2/3）。 |
| 4 | **飞翔之魂** | `SoulofFlightEffect` | **1.1f (开启)** | **否 (重写为空)** | 死亡时不释放爆炸弹幕（`OnKill` 被重写为空）。但其在**飞行过程中**每 3 帧会向下垂投一发 `NewSHPS` 能量弹幕。 |
| 5 | **光明之魂** | `SoulofLightEffect` | **0f (关闭)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。取而代之的是，在死亡点以粒子特效（`SquishyLightParticle` 和 `SparkParticle`）渲染一个华丽的正五角星星爆效果。 |
| 6 | **混沌精华** | `EssenceofHavocEffect` | **1.35f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时释放十字形扩散的红橘色粒子，并向上下左右 4 个方向各射出 3 发 `EssenceofHavoc_INV` 混乱能量弹幕。 |
| 7 | **冰川精华** | `EssenceofSnowEffect` | **1.35f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时以冰蓝和白色粒子呈现前向扇形冷冻爆发，并在前方生成 1 个液氮极寒区域弹幕 `EssenceofSnow_N2`。 |
| 8 | **日光精华** | `EssenceofSunlightEffect` | **1.35f (开启)** | **是 (继承基类)** | 死亡时调用 `base.OnKill` 释放默认爆炸弹幕。同时，在其**正上方 30 格**的位置召唤一个 `EssenceofSunlight_BurstRelay` 精华中继核心，向下发射 7 道强力瞄准的追踪日光激光。 |
| 9 | **生命碎片** | `LivingShardEffect` | **1.55f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时释放 12 方向散射的绿色光粒子与双椭圆交叉的绿色粉尘。它的主要治疗逻辑在**命中敌人**时触发（释放吸血泡泡 `LivingShard_Healing` 飞回玩家）。 |
| 10 | **日耀碎片** | `FragmentSolarEffect` | **1.55f (开启)** | **是 (限制触发)** | 只有在**命中敌人死亡**时才会生成一个 224x224 的巨大 `NewLegendSHPE` 爆炸弹幕。同时在死亡处上方召唤 7 支 `FragmentSolar_Spear` 日耀长枪插下。 |
| 11 | **漩涡碎片** | `FragmentVortexEffect` | **1.55f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时生成漩涡状像素块粒子，并以前向傅里叶一阶调制的角度朝前方扇形发射 7 发 `VortexBeaterRocket` 星旋导弹。 |
| 12 | **血石核心** | `BloodstoneCoreEffect` | **1.05f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时爆发出阿基米德螺旋和玫瑰花瓣状的血石粒子阵列以及血雾扩散。其吸血核心通过**飞行碰撞**后由 `BloodstoneCore_BloodOrb` 追踪判定。 |
| 13 | **神圣晶石** | `DivineGeodeEffect` | **1.85f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时爆发出神圣金光粒子，并向 8 方向以 60 度角间距等分射出 8 道折射激光 `DivineGeode_Lazer`。 |
| 14 | **灵质** | `NecroplasmEffect` | **1.85f (开启)** | **是 (手动生成)** | 死亡时手动生成一个 250x250 的超大范围 `NewLegendSHPE` 爆炸弹幕，并从爆炸边缘生成 6 个向内收缩并带追踪伤害的 `SHPCNecroplasmDamage` 灵魂碎片。 |
| 15 | **毁灭之灵** | `RuinousSoulEffect` | **1.85f (开启)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时爆发出叹息鬼魂的面部轮廓粒子，并生成 4 个（对超大血量 Boss 为 8 个）朝四周随机角度发射的追踪幻影碎屑 `RuinousSoul_GhastlyES`。 |
| 16 | **日蚀之阴** | `DarksunFragmentEffect` | **0f (关闭)** | **否 (无生成逻辑)** | 死亡时不释放爆炸弹幕。死亡时在原地生成淡出的日蚀金轮和十字准星，并生成 12 个随机散射的黑金碎屑粒子。其伤害主要通过命中敌人时在目标头顶生成/升级黑日 `DarksunFragmentBlackSun` 产生。 |

---

## 第三部分：结论与设计建议 (Conclusion & Recommendations)

1. **直接发射占比过高**：41 个特效中多达 25 个属于出生立刻自杀，然后直接用纯魔改追踪弹幕替代。这导致了 SHPC 原有的抛物线/光球弹道玩法在中后期由于材质更替而被大部分剥夺。
2. **OnKill 注释屏蔽问题**：
   * **风暴之颚 (`StormlionMandibleEffect`)**：它的死亡逻辑 `OnKill` 生成爆炸的代码是全注释掉的，使得该光球自然死亡或撞地时极其安静，没有任何多余判定。
   * **飞翔之魂 (`SoulofFlightEffect`)**：`OnKill` 重写为空，没有任何撞击波或判定。
3. **ExplosionPulseFactor 与 SHPE 的脱钩**：
   * 共有 **4 种**保留光球的弹幕（硫磺鳞片、光明之魂、日蚀之阴、星旋碎片）彻底关闭了默认爆炸光圈特效 (`ExplosionPulseFactor => 0f`)。
   * 很多保留光球的弹幕为了展示自己独特的爆炸演出（例如光明之魂的五角星粒子，神圣晶石的 8 向激光，血石核心的玫瑰阵列），**重写并阻断了默认爆炸弹幕 `NewLegendSHPE` 的产生**。这意味着它们虽然是“爆炸”，但纯粹是粒子特效演出，真正造成伤害判定的是它们放出的子弹幕，而不是一个瞬间爆开的圆形范围力场。
