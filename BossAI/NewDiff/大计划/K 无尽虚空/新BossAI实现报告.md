# 无尽虚空 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/CeaselessVoid/Phases/**`
- 注册对象：`CeaselessVoid`
- 移动模型：`VoidCore`，围绕玩家做较慢的虚空核心悬浮，并持续自转。
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Mirror Blade | SpaceRift | 158 | `MirrorBlade` | Rift | 168 / 7 |
| 1 | 100%-90% | Void Concentration Staff | SpaceRift | 158 | `VoidConcentrationStaff` | Rift | 168 / 7 |
| 2 | 90%-70% | Dark Spark | SpaceRift | 158 | `DarkSpark` | Rift | 168 / 7 |
| 2 | 90%-70% | Event Horizon | StarField | 152 | `EventHorizon` | Star | 158 / 8 |
| 3 | 70%-50% | Mistlestorm | LightningChain | 146 | `Mistlestorm` | Rift | 168 / 7 |
| 3 | 70%-50% | Ontological Despoiler | SpaceRift | 158 | `OntologicalDespoiler` | Rift | 168 / 7 |
| 4 | 50%-30% | Sealed Singularity | SpaceRift | 158 | `SealedSingularity` | Rift | 168 / 7 |
| 4 | 50%-30% | Tactician's Trump Card | SummonCore | 174 | `TacticiansTrumpCard` | Sentry | 196 / 6 |
| 5 | 30%-12% | Eternity | MagicCore | 156 | `Eternity` | Spinning | 144 / 6 |
| 5 | 30%-12% | Phantasmal Fury | SpaceRift | 158 | `PhantasmalFury` | Rift | 168 / 7 |
| 6 | 12%-0% | Four Seasons Galaxia | StarField | 152 | `FourSeasonsGalaxia` | Star | 158 / 8 |
| 6 | 12%-0% | Reality Rupture | LightningChain | 146 | `RealityRupture` | Rift | 168 / 7 |

## 招式行为

- 无尽虚空当前以 `SpaceRift` 为主：多次在玩家中心附近打旋转裂隙环，形成核心吸压感。
- `StarField` 的 Event Horizon / Four Seasons Galaxia 会从外环向玩家中心收束。
- `LightningChain` 的 Mistlestorm / Reality Rupture 是随机裂隙线，叠在 VoidCore 自转上会比较接近虚空撕裂视觉。
- `SummonCore` 的 Tactician's Trump Card 是哨兵阵，和原 Dark Energy 小怪不是同一套逻辑。
- `MagicCore` 的 Eternity 是核心炮台加环形弹。

## 疑似问题

- 高风险：原 Ceaseless Void 的 Dark Energy、吸附/无敌、地牢或深度限制等核心机制没有在当前 AI 里体现；它现在是单核心武器弹幕循环。
- 中风险：SpaceRift 占比很高，阶段 1、2、3、4、5 都有，阶段间可能只是换贴图而非换玩法。
- 中风险：VoidCore 移动只围绕玩家悬浮，没有场地边界或玩家高度检查，可能绕过原本地形要求。
- 中风险：Tactician's Trump Card 使用 SummonCore 哨兵，若期待牌类/召唤类独特机制，目前没有展开。
- 低风险：物品贴图名均能在本地灾厄物品中找到。

