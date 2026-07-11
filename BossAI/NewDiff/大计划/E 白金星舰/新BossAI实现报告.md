# 白金星舰 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/AstrumAureus/AstrumAureusWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/AstrumAureus/Phases/**`
- 注册对象：`AstrumAureus`
- 移动模型：`HeavyHover`，不再是原本跳跃、落地、召唤小怪的地面 Boss 节奏。
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Nebulash | StarField | 152 | `Nebulash` | Star | 158 / 8 |
| 1 | 100%-90% | Aurora Blazer | StarField | 152 | `AuroraBlazer` | Star | 158 / 8 |
| 2 | 90%-70% | Alula Australis | StarField | 152 | `AlulaAustralis` | Star | 158 / 8 |
| 2 | 90%-70% | Borealis Bomber | BombRain | 146 | `BorealisBomber` | BombTrail | 166 / 9 |
| 3 | 70%-50% | Auroradical Throw | StarField | 152 | `AuroradicalThrow` | Star | 158 / 8 |
| 3 | 70%-50% | Astral Scythe | StarField | 152 | `AstralScythe` | Star | 158 / 8 |
| 4 | 50%-30% | Titan Arm | MagicCore | 156 | `TitanArm` | Spinning | 144 / 6 |
| 4 | 50%-30% | Stellar Cannon | StarField | 152 | `StellarCannon` | Star | 158 / 8 |
| 5 | 30%-12% | Stellar Knife | StarField | 152 | `StellarKnife` | Star | 158 / 8 |
| 5 | 30%-12% | Astralachnea Staff | StarField | 152 | `AstralachneaStaff` | Star | 158 / 8 |
| 6 | 12%-0% | Abandoned Slime Staff | SummonCore | 174 | `AbandonedSlimeStaff` | Sentry | 196 / 6 |
| 6 | 12%-0% | Hive Pod | BombRain | 146 | `HivePod` | BombTrail | 166 / 9 |

## 招式行为

- 白金星舰当前高度偏 `StarField`：大多数阶段都是 8 个星辉武器从玩家外圈向内收束，并在 38/78/118 帧额外补一组星场。
- `Borealis Bomber` / `Hive Pod` 是 BombRain，下落武器本体还会沿途继续生成 falling 子弹。
- `Titan Arm` 是 MagicCore，在玩家附近摆核心和环形弹。
- `Abandoned Slime Staff` 是 SummonCore，在玩家上方左右生成哨兵，并继续吐 shard。

## 疑似问题

- 高风险：原 Astrum Aureus 的跳跃压迫、踩地、Aureus Spawn、星辉地形感在当前实现中基本消失，Boss 变成空中星场炮台。
- 中风险：StarField 占比过高，阶段 1、2、3、4、5 都有，实际战斗可能缺少阶段节奏差异。
- 中风险：BombRain 的 `BurstCount=9`，加上 BombTrail 每 12 帧洒弹，低血量 Hive Pod 可能突然把场地塞得很满。
- 中风险：`MainProjectileStyle` 未真正参与公共 Spawn 序列化，星场以外的跨样式调用仍需实测确认。
- 低风险：当前物品名均能在本地 `CalamityMod/Items` 找到。

