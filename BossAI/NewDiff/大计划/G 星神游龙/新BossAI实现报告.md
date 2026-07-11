# 星神游龙 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/AstrumDeus/Phases/**`
- 注册对象：`AstrumDeusHead`、`AstrumDeusBody`、`AstrumDeusTail`
- 移动模型：`Worm`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | The Microwave | LightningChain | 146 | `TheMicrowave` | Rift | 168 / 7 |
| 1 | 100%-90% | Star Sputter | StarField | 152 | `StarSputter` | Star | 158 / 8 |
| 2 | 90%-70% | Star Shower | StarField | 152 | `StarShower` | Star | 158 / 8 |
| 2 | 90%-70% | Starspawn Helix Staff | StarField | 152 | `StarspawnHelixStaff` | Star | 158 / 8 |
| 3 | 70%-50% | Regulus Riot | StarField | 152 | `RegulusRiot` | Star | 158 / 8 |
| 4 | 50%-30% | Astral Pike | StarField | 152 | `AstralPike` | Star | 158 / 8 |
| 4 | 50%-30% | Astral Blaster | StarField | 152 | `AstralBlaster` | Star | 158 / 8 |
| 5 | 30%-12% | Astral Staff | StarField | 152 | `AstralStaff` | Star | 158 / 8 |
| 5 | 30%-12% | Radiant Star | StarField | 152 | `RadiantStar` | Star | 158 / 8 |
| 6 | 12%-0% | True Biome Blade | Slash | 134 | `TrueBiomeBlade` | Slash | 144 / 6 |

## 招式行为

- 绝大多数阶段都是 `StarField`：在玩家周围半径约 420 的圆上生成 8 个武器并向中心收束，公共三轮加额外三轮，形成持续星环围杀。
- `The Microwave` 使用 `LightningChain`，会在玩家周边随机生成裂隙线，属于开场的闪电场压力。
- 最终阶段 `True Biome Blade` 使用 `Slash`：Boss 有短冲刺窗口，同时打斩击散射和侧边武器。

## 疑似问题

- 极高风险：`AstrumDeusHead/Body/Tail` 全部注册同一个 AI。公共 `Worm` 移动没有头部专属保护，身体和尾巴可能各自追玩家、各自发射武器，破坏蠕虫节段跟随结构。
- 高风险：星神游龙作为蠕虫 Boss 的身体编队、分裂、头尾判定没有在当前实现里复刻；当前更像多个独立武器发射点。
- 中风险：StarField 占比过高，阶段 2-5 几乎全是同一空间结构，只换星辉武器贴图。
- 中风险：最终阶段只有 True Biome Blade 一招，如果血量压得慢，会重复 Slash 循环，强度和观感都比较单一。
- 低风险：物品贴图名对照本地灾厄物品无明显缺失。

