# 渊海灾虫 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/AquaticScourge/Phases/**`
- 注册对象：`AquaticScourgeHead`、`AquaticScourgeBody`、`AquaticScourgeBodyAlt`、`AquaticScourgeTail`
- 移动模型：`Worm`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Submarine Shocker | LightningChain | 146 | `SubmarineShocker` | Rift | 168 / 7 |
| 1 | 100%-90% | Barinautical | MagicCore | 156 | `Barinautical` | Spinning | 144 / 6 |
| 2 | 90%-70% | Downpour | AcidRain | 150 | `Downpour` | Falling | 150 / 6 |
| 2 | 90%-70% | Deepsea Staff | SummonCore | 174 | `DeepseaStaff` | Sentry | 196 / 6 |
| 3 | 70%-50% | Scourge of the Seas | AcidRain | 150 | `ScourgeoftheSeas` | Falling | 150 / 6 |
| 3 | 70%-50% | Flak Toxicannon | AcidRain | 150 | `FlakToxicannon` | Falling | 150 / 6 |
| 4 | 50%-30% | Slithering Eels | CreatureRush | 138 | `SlitheringEels` | Creature | 142 / 6 |
| 4 | 50%-30% | Caustic Croaker Staff | AcidRain | 150 | `CausticCroakerStaff` | Falling | 150 / 6 |
| 5 | 30%-12% | Skyfin Bombers | BombRain | 146 | `SkyfinBombers` | BombTrail | 166 / 9 |
| 5 | 30%-12% | Spent Fuel Container | AcidRain | 150 | `SpentFuelContainer` | Falling | 150 / 6 |
| 6 | 12%-0% | Sulphurous Grabber | AcidRain | 150 | `SulphurousGrabber` | Falling | 150 / 6 |

## 招式行为

- 渊海灾虫当前偏硫海武器雨：`AcidRain` 占很多阶段，反复从玩家上方生成 falling 武器并带横向修正。
- `LightningChain` 的 Submarine Shocker 是开场裂隙线。
- `MagicCore` 的 Barinautical 是核心炮台加环形弹。
- `SummonCore` 的 Deepsea Staff 是左右哨兵阵。
- `CreatureRush` 的 Slithering Eels 从侧边横穿，制造横向线压。
- `BombRain` 的 Skyfin Bombers 会落下 BombTrail 本体，并沿途洒落子弹。

## 疑似问题

- 极高风险：头、身体、两种身体段、尾巴都注册同一个 AI。没有看到 head-only 限制，所有节段可能各自执行攻击循环，造成弹幕量暴增和蠕虫结构破坏。
- 高风险：原 Aquatic Scourge 的蠕虫跟随、身体发射点、硫海/水中语义没有被保留；公共 Worm 会让每段直接追玩家。
- 中风险：AcidRain 占比过高，阶段 2、3、4、5、6 都出现，低血量阶段差异可能不足。
- 中风险：Phase06 只有 Sulphurous Grabber 一招，最终阶段可能显得单薄。
- 低风险：所有物品贴图名都能在本地灾厄物品中找到。

