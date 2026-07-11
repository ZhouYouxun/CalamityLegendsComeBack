# 普罗维登斯 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/Providence/ProvidenceWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/Providence/Phases/**`
- 注册对象：`Providence`
- 移动模型：`HeavyHover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Holy Collider | Slash | 134 | `HolyCollider` | Slash | 144 / 6 |
| 1 | 100%-90% | Burning Revelation | ReturningBlade | 134 | `BurningRevelation` | Yoyo | 154 / 6 |
| 1 | 100%-90% | Telluric Glare | MagicCore | 156 | `TelluricGlare` | Spinning | 144 / 6 |
| 1 | 100%-90% | Blissful Bombardier | BombRain | 146 | `BlissfulBombardier` | BombTrail | 166 / 9 |
| 2 | 90%-70% | Purge Guzzler | CreatureRush | 138 | `PurgeGuzzler` | Creature | 142 / 6 |
| 2 | 90%-70% | Dazzling Stabber Staff | SummonCore | 174 | `DazzlingStabberStaff` | Sentry | 196 / 6 |
| 2 | 90%-70% | Molten Amputator | Slash | 134 | `MoltenAmputator` | Slash | 144 / 6 |
| 3 | 70%-50% | Pristine Fury | Gunline | 134 | `PristineFury` | Straight | 144 / 6 |
| 3 | 70%-50% | Aetherflux Cannon | StarField | 152 | `AetherfluxCannon` | Star | 158 / 8 |
| 3 | 70%-50% | Angelic Shotgun | Gunline | 134 | `AngelicShotgun` | Straight | 144 / 6 |
| 4 | 50%-30% | Dark Spark | SpaceRift | 158 | `DarkSpark` | Rift | 168 / 7 |
| 4 | 50%-30% | Galactus Blade | StarField | 152 | `GalactusBlade` | Star | 158 / 8 |
| 4 | 50%-30% | Handheld Tank | Gunline | 134 | `HandheldTank` | Straight | 144 / 6 |
| 5 | 30%-12% | Mirror of Kalandra | SummonCore | 174 | `MirrorofKalandra` | Sentry | 196 / 6 |
| 5 | 30%-12% | Mourningstar | StarField | 152 | `Mourningstar` | Star | 158 / 8 |
| 5 | 30%-12% | Shattered Dawn | MagicCore | 156 | `ShatteredDawn` | Spinning | 144 / 6 |
| 6 | 12%-0% | Seeking Scorcher | MagicCore | 156 | `SeekingScorcher` | Spinning | 144 / 6 |
| 6 | 12%-0% | The Maelstrom | LightningChain | 146 | `TheMaelstrom` | Rift | 168 / 7 |
| 6 | 12%-0% | The Prince | MagicCore | 156 | `ThePrince` | Spinning | 144 / 6 |

## 招式行为

- 普罗维登斯当前招式覆盖很广：斩击、回旋、炮台、落弹、星场、裂隙和生物冲锋都有。
- `Slash` 招式会让 Boss 在 53-69 帧短冲刺，并打斩击散射。
- `Gunline` 是直线枪线，Pristine Fury、Angelic Shotgun、Handheld Tank 都属于此类。
- `SummonCore` 是左右哨兵阵，Dazzling Stabber Staff 和 Mirror of Kalandra 都按哨兵逻辑执行。
- `StarField` 和 `SpaceRift` 分别是环外收束与中心裂隙环，负责中后期场控。

## 疑似问题

- 高风险：原 Providence 的祭坛/地狱或神圣环境限制、昼夜激怒、守卫、治疗守卫、圣火弹幕等没有在当前 AI 中体现；`PreAI` 被接管后这些原流程大概率被绕开。
- 中风险：招式数量很多，但每个模式都是公共模板，武器之间的独特机制不一定充分，容易成为“换皮模板合集”。
- 中风险：BombRain、StarField、MagicCore 都有死亡爆 shard，普罗维登斯招式多，场面密度需要实战压测。
- 中风险：`MainProjectileStyle` 未被完整写入 projectile AI，Slash 的侧边 Returning、MagicCore 的 Star 等调用名可能和实际样式不一致。
- 低风险：物品贴图名均通过本地 CalamityMod 物品名检查。

