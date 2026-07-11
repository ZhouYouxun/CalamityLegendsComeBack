# Dragonfolly - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/Dragonfolly/DragonfollyWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/Dragonfolly/Phases/**`
- 注册对象：`Dragonfolly`
- 移动模型：`Hover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Gilded Proboscis | CreatureRush | 138 | `GildedProboscis` | Creature | 142 / 6 |
| 2 | 90%-70% | 空 | 回退到邻近阶段 | - | - | - | - |
| 3 | 70%-50% | Golden Eagle | Gunline | 134 | `GoldenEagle` | Straight | 144 / 6 |
| 4 | 50%-30% | 空 | 回退到邻近阶段 | - | - | - | - |
| 5 | 30%-12% | Rouge Slash | Slash | 134 | `RougeSlash` | Slash | 144 / 6 |
| 6 | 12%-0% | 空 | 回退到邻近阶段 | - | - | - | - |

## 招式行为

- `CreatureRush` 的 Gilded Proboscis 会从玩家一侧生成多条纵向飞行的 creature 武器，公共逻辑三轮，招式额外在 36/78/120 帧再补三轮。
- `Gunline` 的 Golden Eagle 是直线射击，公共 5 发散射三轮，加额外 4 发散射三轮。
- `Slash` 的 Rouge Slash 会触发短冲刺，并打斩击散射与侧边武器。
- 由于 Phase02、Phase04、Phase06 是空数组，公共 `CurrentCycle` 会向邻近非空阶段回退：Phase02 大概率使用 Phase01，Phase04 使用 Phase03，Phase06 使用 Phase05。

## 疑似问题

- 高风险：只有 3 个真实招式阶段，另外 3 个阶段靠公共回退，不像完整 6 阶段 BossAI；如果不是有意做轻量版，这就是明显未完工。
- 中风险：最终 12% 以下并没有新绝境招式，只会回退到 Phase05 的 Rouge Slash。
- 中风险：`RougeSlash` 物品名本地存在，拼写不是贴图错误；但玩家可能会误读为 `Rogue Slash`，报告和本地命名需要保持一致。
- 中风险：Dragonfolly 原本高速鸟类冲刺、羽毛/龙焰节奏没有保留，只剩通用 Hover + 武器模式。
- 低风险：当前三个物品贴图名均可解析。

