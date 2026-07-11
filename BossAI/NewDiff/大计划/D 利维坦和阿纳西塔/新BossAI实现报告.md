# 利维坦和阿纳西塔 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/LeviathanAnahita/LeviathanAnahitaWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/LeviathanAnahita/Phases/**`
- 注册对象：`Leviathan`、`Anahita`
- 移动模型：`HeavyHover`，两者都会使用同一套重型悬停逻辑和同一套武器招式。
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Greentide | MagicCore | 156 | `Greentide` | Spinning | 144 / 6 |
| 1 | 100%-90% | Leviatitan | MagicCore | 156 | `Leviatitan` | Spinning | 144 / 6 |
| 2 | 90%-70% | Anahita's Arpeggio | Gunline | 134 | `AnahitasArpeggio` | Straight | 144 / 6 |
| 3 | 70%-50% | Atlantis | Gunline | 134 | `Atlantis` | Straight | 144 / 6 |
| 4 | 50%-30% | Gastric Belcher Staff | CreatureRush | 138 | `GastricBelcherStaff` | Creature | 142 / 6 |
| 5 | 30%-12% | Whitewater | MagicCore | 156 | `Whitewater` | Spinning | 144 / 6 |
| 6 | 12%-0% | Leviathan Teeth | CreatureRush | 138 | `LeviathanTeeth` | Creature | 142 / 6 |

## 招式行为

- `MagicCore` 的 Greentide / Leviatitan / Whitewater 会在玩家附近布置核心武器并打环形弹，主要是中场压制。
- `Gunline` 的 Arpeggio / Atlantis 是直线弹幕，公共逻辑三轮 5 发，额外逻辑三轮 4 发，实际输出频率偏高。
- `CreatureRush` 的 Gastric Belcher Staff / Leviathan Teeth 会从玩家一侧生成 4 条纵向分布的 creature 武器，并在 36/78/120 帧额外补一轮，像横向生物潮。

## 疑似问题

- 高风险：`Leviathan` 和 `Anahita` 都注册到同一个 AI，没有看到“谁先出场、谁退场、谁主攻”的配对协调。若两只同时存活，可能各自独立跑完整 6 阶段循环，弹幕和移动都会叠倍。
- 高风险：原本海妖与利维坦的双 Boss 节奏被统一悬停模型抹平，Anahita 的歌唱/海妖特征和 Leviathan 的巨体压迫都不明显。
- 中风险：阶段 2 和 3 都是 Gunline，阶段 4 和 6 都是 CreatureRush，阶段功能分工比较粗。
- 中风险：HeavyHover 对 Leviathan 这种大体型 Boss 可能会导致身体/碰撞与玩家距离不稳定。
- 低风险：物品贴图名均能在本地灾厄物品中找到。

