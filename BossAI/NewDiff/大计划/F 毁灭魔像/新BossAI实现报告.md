# 毁灭魔像 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/Ravager/RavagerWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/Ravager/Phases/**`
- 注册对象：`RavagerBody`、`RavagerHead`、`RavagerClawLeft`、`RavagerClawRight`
- 移动模型：`HeavyHover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Ultimus Cleaver | Slash | 134 | `UltimusCleaver` | Slash | 144 / 6 |
| 1 | 100%-90% | Realm Ravager | MagicCore | 156 | `RealmRavager` | Spinning | 144 / 6 |
| 1 | 100%-90% | Hematemesis | BloodPulse | 140 | `Hematemesis` | Spinning | 144 / 8 |
| 2 | 90%-70% | Spikecrag Staff | SummonCore | 174 | `SpikecragStaff` | Sentry | 196 / 6 |
| 2 | 90%-70% | Cranium Smasher | BloodPulse | 140 | `CraniumSmasher` | Spinning | 144 / 8 |
| 2 | 90%-70% | Vesuvius | BombRain | 146 | `Vesuvius` | BombTrail | 166 / 9 |
| 3 | 70%-50% | Corpus Avertor | BloodPulse | 140 | `CorpusAvertor` | Spinning | 144 / 8 |
| 3 | 70%-50% | Flesh Totem | BloodPulse | 140 | `FleshTotem` | Spinning | 144 / 8 |
| 3 | 70%-50% | The Mutilator | ReturningBlade | 134 | `TheMutilator` | Yoyo | 154 / 6 |
| 4 | 50%-30% | Lacerator | ReturningBlade | 134 | `Lacerator` | Yoyo | 154 / 6 |
| 4 | 50%-30% | Claret Cannon | BloodPulse | 140 | `ClaretCannon` | Spinning | 144 / 8 |
| 4 | 50%-30% | Arterial Assault | BloodPulse | 140 | `ArterialAssault` | Spinning | 144 / 8 |
| 5 | 30%-12% | Blood Boiler | BloodPulse | 140 | `BloodBoiler` | Spinning | 144 / 8 |
| 5 | 30%-12% | Sanguine Flare | BloodPulse | 140 | `SanguineFlare` | Spinning | 144 / 8 |
| 5 | 30%-12% | Viscera | BloodPulse | 140 | `Viscera` | Spinning | 144 / 8 |
| 6 | 12%-0% | Dragonblood Disgorger | BloodPulse | 140 | `DragonbloodDisgorger` | Spinning | 144 / 8 |
| 6 | 12%-0% | Bloodsoaked Crasher | BloodPulse | 140 | `BloodsoakedCrasher` | Spinning | 144 / 8 |

## 招式行为

- Ravager 当前是血肉武器主题，`BloodPulse` 占绝对多数：每轮会同时打旋转散射和少量坠落弹，死亡后再爆 8 个 shard。
- `Slash` 的 Ultimus Cleaver 会触发 Boss 在 53-69 帧短冲刺，并打斩击散射；招式还会额外从侧边刷 3 个“回旋”武器。
- `SummonCore` 的 Spikecrag Staff 是哨兵阵，先公共生成左右哨兵，再补两枚哨兵和一圈 shard。
- `BombRain` 的 Vesuvius 是上方落弹，BombTrail 本体还会洒 falling 子弹。
- `ReturningBlade` 的 The Mutilator / Lacerator 是左右边线 yoyo 式回旋压迫。

## 疑似问题

- 极高风险：主 AI 同时注册了 `RavagerBody`、`RavagerHead`、左右爪。公共 AI 没有只允许主体执行攻击的判断，因此每个部件都可能独立悬停、独立计时、独立发射整套招式，弹幕量和移动都会被倍增。
- 高风险：Ravager 原本的身体/头/爪结构关系会被同一个 `HeavyHover` 覆盖，部件可能不再保持原有连接和攻击分工。
- 中风险：后半段几乎全是 BloodPulse，招式名称多，但行为高度接近，容易变成“红色散射 + 落弹”的重复循环。
- 中风险：Slash 额外调用 `FireSideWeapons(... Returning)`，但公共 `StyleToAI` 不会序列化 Returning，实际可能仍按该 projectile 的默认 Slash 样式行动。
- 低风险：物品贴图名已能在本地灾厄物品中找到。

