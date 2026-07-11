# 瘟疫使者歌莉娅 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlaguebringerGoliathWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/PlaguebringerGoliath/Phases/**`
- 注册对象：`PlaguebringerGoliath`
- 移动模型：`Hover`，围绕玩家上方高速悬停，并在 Slash / CreatureRush 模式中短暂冲向玩家。
- 阶段阈值：90%、70%、50%、30%、12%，跨阶段有 36 帧 PhaseShift。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Virulence | MagicCore | 156 | `Virulence` | Spinning | 144 / 6 |
| 1 | 100%-90% | Malevolence | MagicCore | 156 | `Malevolence` | Spinning | 144 / 6 |
| 1 | 100%-90% | Plague Staff | SummonCore | 174 | `PlagueStaff` | Sentry | 196 / 6 |
| 2 | 90%-70% | Fuel Cell Bundle | BombRain | 146 | `FuelCellBundle` | BombTrail | 166 / 9 |
| 2 | 90%-70% | Infected Remote | SummonCore | 174 | `InfectedRemote` | Sentry | 196 / 6 |
| 3 | 70%-50% | The Syringe | MagicCore | 156 | `TheSyringe` | Spinning | 144 / 6 |
| 3 | 70%-50% | The Hive | MagicCore | 156 | `TheHive` | Spinning | 144 / 6 |
| 4 | 50%-30% | Pestilent Defiler | Gunline | 134 | `PestilentDefiler` | Straight | 144 / 6 |
| 4 | 50%-30% | Malachite | MagicCore | 156 | `Malachite` | Spinning | 144 / 6 |
| 4 | 50%-30% | Toxic Heart | AcidRain | 150 | `ToxicHeart` | Falling | 150 / 6 |
| 5 | 30%-12% | Plague Caller | SummonCore | 174 | `PlagueCaller` | Sentry | 196 / 6 |
| 5 | 30%-12% | Blight Spewer | MagicCore | 156 | `BlightSpewer` | Spinning | 144 / 6 |
| 6 | 12%-0% | Pandemic | ReturningBlade | 134 | `Pandemic` | Yoyo | 154 / 6 |
| 6 | 12%-0% | Plague Tainted SMG | Gunline | 134 | `PlagueTaintedSMG` | Straight | 144 / 6 |

## 招式行为

- 前三阶段主要是 `MagicCore` 和 `SummonCore`：Boss 持有武器后，在玩家附近摆炮台、打环形弹，再由哨兵吐 shard。瘟疫法杖、感染遥控器和瘟疫召唤器都表现为定点哨兵阵。
- `BombRain` 的 Fuel Cell Bundle 会从玩家头顶宽范围落下，并由 BombTrail 本体沿途继续生成 falling 子弹，属于纵向封路。
- `Gunline` 的 Pestilent Defiler / Plague Tainted SMG 是六段直线射击：公共 5 发散射三轮，加额外 4 发散射三轮。
- `AcidRain` 的 Toxic Heart 是更稳定的上方坠落弹，每轮 4 枚，带轻微水平修正。
- `ReturningBlade` 的 Pandemic 从左右远端刷 yoyo 式回旋武器，并在 34/76/116 帧再补边路武器。

## 疑似问题

- 高风险：原 PBG 的冲刺、蜂群/无人机、瘟疫导弹、丛林空间压力基本都没有保留；当前是通用悬停炮台，Boss 身份主要靠武器贴图和绿色主题色维持。
- 中风险：阶段 1、3、5 都有大量 MagicCore/SummonCore，节奏相似度高；如果视觉颜色接近，阶段差异会弱于设计稿。
- 中风险：Plague Staff、Infected Remote、Plague Caller 三个 SummonCore 都是同一套哨兵逻辑，缺少各自召唤物的差异行为。
- 中风险：`MainProjectileStyle` 未被公共生成函数完整序列化，除 `Shard/Held/Sentry` 外基本依赖 projectile 默认样式；报告里的模式名不一定等于运行时实际 `ActiveStyle`。
- 低风险：全部 `ItemInternalName` 均能在本地灾厄物品文件中找到，暂未发现贴图名拼错。

