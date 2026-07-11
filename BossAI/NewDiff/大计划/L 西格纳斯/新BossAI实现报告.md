# 西格纳斯 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/Signus/SignusWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/Signus/Phases/**`
- 注册对象：`Signus`
- 移动模型：`Hover`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Cosmic Kunai | StarField | 152 | `CosmicKunai` | Star | 158 / 8 |
| 1 | 100%-90% | Cosmilamp | StarField | 152 | `Cosmilamp` | Star | 158 / 8 |
| 2 | 90%-70% | Aether's Whisper | StarField | 152 | `AethersWhisper` | Star | 158 / 8 |
| 2 | 90%-70% | Death's Ascension | MagicCore | 156 | `DeathsAscension` | Spinning | 144 / 6 |
| 3 | 70%-50% | Empyrean Knives | Slash | 134 | `EmpyreanKnives` | Slash | 144 / 6 |
| 3 | 70%-50% | King of Constellations, Tenryu | StarField | 152 | `KingofConstellationsTenryu` | Star | 158 / 8 |
| 4 | 50%-30% | Magnetic Meltdown | MagicCore | 156 | `MagneticMeltdown` | Spinning | 144 / 6 |
| 4 | 50%-30% | Nadir | SpaceRift | 158 | `Nadir` | Rift | 168 / 7 |
| 5 | 30%-12% | The Sevens Striker | MagicCore | 156 | `TheSevensStriker` | Spinning | 144 / 6 |
| 5 | 30%-12% | Venusian Trident | Slash | 134 | `VenusianTrident` | Slash | 144 / 6 |
| 6 | 12%-0% | Four Seasons Galaxia | StarField | 152 | `FourSeasonsGalaxia` | Star | 158 / 8 |
| 6 | 12%-0% | Reality Rupture | LightningChain | 146 | `RealityRupture` | Rift | 168 / 7 |

## 招式行为

- 前期以 `StarField` 为主，武器从外环向玩家中心收束，比较符合星界/暗影围杀。
- `MagicCore` 的 Death's Ascension、Magnetic Meltdown、The Sevens Striker 会变成核心炮台和环形弹。
- `Slash` 的 Empyrean Knives / Venusian Trident 会触发 Boss 短冲刺，再打斩击散射和侧边武器。
- `SpaceRift` 的 Nadir 是中心裂隙环。
- `LightningChain` 的 Reality Rupture 是随机裂隙线场。

## 疑似问题

- 高风险：Signus 原本的隐身、传送、贴脸突袭和暗影拖尾行为没有在当前 AI 中体现；Hover 模型会削弱刺客感。
- 中风险：StarField 在阶段 1、2、3、6 都出现，若贴图大小/颜色接近，玩家可能感觉重复。
- 中风险：The Sevens Striker 用 MagicCore，但本体武器特色可能没有体现，只是换成旋转核心弹。
- 中风险：Slash 额外侧边 Returning 调用可能因 `StyleToAI` 不序列化 Returning 而退回 projectile 默认 Slash。
- 低风险：`TheSevensStriker`、`KingofConstellationsTenryu` 等长名字本地物品文件存在，贴图解析暂时安全。

