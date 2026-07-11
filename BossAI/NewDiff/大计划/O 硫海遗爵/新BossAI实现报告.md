# 硫海遗爵 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/OldDuke/OldDukeWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/OldDuke/Phases/**`
- 注册对象：`OldDuke`
- 移动模型：`Hover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Insidious Impaler | Slash | 134 | `InsidiousImpaler` | Slash | 144 / 6 |
| 1 | 100%-90% | Fetid Emesis | BloodPulse | 140 | `FetidEmesis` | Spinning | 144 / 8 |
| 1 | 100%-90% | Septic Skewer | AcidRain | 150 | `SepticSkewer` | Falling | 150 / 6 |
| 2 | 90%-70% | Vitriolic Viper | AcidRain | 150 | `VitriolicViper` | Falling | 150 / 6 |
| 2 | 90%-70% | Mutated Truffle | CreatureRush | 138 | `MutatedTruffle` | Creature | 142 / 6 |
| 2 | 90%-70% | Cadaverous Carrion | BloodPulse | 140 | `CadaverousCarrion` | Spinning | 144 / 8 |
| 3 | 70%-50% | Toxicant Twister | AcidRain | 150 | `ToxicantTwister` | Falling | 150 / 6 |
| 3 | 70%-50% | The Old Reaper | Slash | 134 | `TheOldReaper` | Slash | 144 / 6 |
| 4 | 50%-30% | Sulphuric Acid Cannon | AcidRain | 150 | `SulphuricAcidCannon` | Falling | 150 / 6 |
| 4 | 50%-30% | Gamma Heart | SummonCore | 174 | `GammaHeart` | Sentry | 196 / 6 |
| 4 | 50%-30% | Phosphorescent Gauntlet | Slash | 134 | `PhosphorescentGauntlet` | Slash | 144 / 6 |
| 5 | 30%-12% | Flak Toxicannon | AcidRain | 150 | `FlakToxicannon` | Falling | 150 / 6 |
| 5 | 30%-12% | Slithering Eels | CreatureRush | 138 | `SlitheringEels` | Creature | 142 / 6 |
| 5 | 30%-12% | Skyfin Bombers | BombRain | 146 | `SkyfinBombers` | BombTrail | 166 / 9 |
| 6 | 12%-0% | Spent Fuel Container | AcidRain | 150 | `SpentFuelContainer` | Falling | 150 / 6 |
| 6 | 12%-0% | Sulphurous Grabber | AcidRain | 150 | `SulphurousGrabber` | Falling | 150 / 6 |

## 招式行为

- Old Duke 当前大量使用 `AcidRain`：硫海武器从玩家上方落下，并有轻微水平漂移。
- `BloodPulse` 的 Fetid Emesis / Cadaverous Carrion 是旋转散射加小规模落弹，死亡后爆 8 shard。
- `Slash` 的 Insidious Impaler / The Old Reaper / Phosphorescent Gauntlet 会触发 Boss 短冲刺和斩击散射。
- `CreatureRush` 的 Mutated Truffle / Slithering Eels 是从侧边横穿的 creature 武器。
- `SummonCore` 的 Gamma Heart 是哨兵阵。
- `BombRain` 的 Skyfin Bombers 是高密度落弹，BombTrail 还会沿途洒落子弹。

## 疑似问题

- 高风险：原 Old Duke 的海洋/硫海环境、冲刺链、鲨牙球、三叶虫尖刺等机制没有在当前 AI 中体现；现在是 Hover 武器雨。
- 中风险：AcidRain 占比非常高，阶段 1、2、3、4、5、6 都有，最终阶段也只有两个 AcidRain，变化不足。
- 中风险：Slash 侧边 Returning 调用可能不会按 Returning 样式执行，因公共样式编码只保留 `Shard/Held/Sentry`。
- 中风险：Skyfin Bombers 的 BombTrail 加死亡爆裂会显著提高低血量弹幕量，需要实战确认不会过密。
- 低风险：全部物品贴图名都能在本地灾厄物品中找到。

