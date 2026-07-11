# 噬魂幽花 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/Polterghast/PolterghastWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/Polterghast/Phases/**`
- 注册对象：`Polterghast`
- 移动模型：`Hover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Terror Blade | Slash | 134 | `TerrorBlade` | Slash | 144 / 6 |
| 1 | 100%-90% | Banshee Hook | ReturningBlade | 134 | `BansheeHook` | Yoyo | 154 / 6 |
| 1 | 100%-90% | Daemon's Flame | MagicCore | 156 | `DaemonsFlame` | Spinning | 144 / 6 |
| 2 | 90%-70% | Fate's Reveal | MagicCore | 156 | `FatesReveal` | Spinning | 144 / 6 |
| 2 | 90%-70% | Ghastly Visage | SummonCore | 174 | `GhastlyVisage` | Sentry | 196 / 6 |
| 3 | 70%-50% | Ethereal Subjugator | SummonCore | 174 | `EtherealSubjugator` | Sentry | 196 / 6 |
| 3 | 70%-50% | Ghoulish Gouger | CreatureRush | 138 | `GhoulishGouger` | Creature | 142 / 6 |
| 3 | 70%-50% | Galileo Gladius | Slash | 134 | `GalileoGladius` | Slash | 144 / 6 |
| 4 | 50%-30% | Crescent Moon | ReturningBlade | 134 | `CrescentMoon` | Yoyo | 154 / 6 |
| 4 | 50%-30% | Halley's Inferno | MagicCore | 156 | `HalleysInferno` | Spinning | 144 / 6 |
| 5 | 30%-12% | Alpha Draconis | MagicCore | 156 | `AlphaDraconis` | Spinning | 144 / 6 |
| 5 | 30%-12% | Stratus Sphere | ReturningBlade | 134 | `StratusSphere` | Yoyo | 154 / 6 |
| 5 | 30%-12% | Sirius | StarField | 152 | `Sirius` | Star | 158 / 8 |
| 6 | 12%-0% | Warloks' Moon Fist | MagicCore | 156 | `WarloksMoonFist` | Spinning | 144 / 6 |
| 6 | 12%-0% | Vega | StarField | 152 | `Vega` | Star | 158 / 8 |

## 招式行为

- Polterghast 的武器来源很杂，当前覆盖了 Slash、ReturningBlade、MagicCore、SummonCore、CreatureRush、StarField。
- `Slash` 的 Terror Blade / Galileo Gladius 负责短冲刺和斩击散射。
- `ReturningBlade` 的 Banshee Hook、Crescent Moon、Stratus Sphere 从左右侧生成 yoyo 式武器。
- `SummonCore` 的 Ghastly Visage / Ethereal Subjugator 是哨兵阵。
- `CreatureRush` 的 Ghoulish Gouger 是横向 creature 潮。
- `StarField` 的 Sirius / Vega 是外环收束。

## 疑似问题

- 高风险：原 Polterghast 的克隆、腿部、地牢/场地压力和三阶段狂暴没有在当前 AI 中体现；现在是 Hover 武器循环。
- 中风险：武器数量多但模式模板有限，多个武器只是在公共运动样式上换贴图。
- 中风险：SummonCore 的两个召唤类武器没有独立召唤物 AI，实际只是哨兵点。
- 中风险：Slash 侧边 Returning 调用可能不会以 Returning 样式生效，原因同公共 `StyleToAI` 限制。
- 低风险：Alpha Draconis、Vega、Halley's Inferno 等物品名均可在本地灾厄物品中解析。

