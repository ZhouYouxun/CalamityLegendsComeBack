# 风暴编织者 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/StormWeaver/StormWeaverWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/StormWeaver/Phases/**`
- 注册对象：`StormWeaverHead`、`StormWeaverBody`、`StormWeaverTail`
- 移动模型：`Worm`
- 阶段阈值：90%、70%、50%、30%、12%。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Skytide Dragoon | Gunline | 134 | `SkytideDragoon` | Straight | 144 / 6 |
| 1 | 100%-90% | The Storm | LightningChain | 146 | `TheStorm` | Rift | 168 / 7 |
| 1 | 100%-90% | Volterion | LightningChain | 146 | `Volterion` | Rift | 168 / 7 |
| 2 | 90%-70% | Aqua's Scepter | SummonCore | 174 | `AquasScepter` | Sentry | 196 / 6 |
| 2 | 90%-70% | Corinth Prime | Gunline | 134 | `CorinthPrime` | Straight | 144 / 6 |
| 3 | 70%-50% | Stellar Torus Staff | StarField | 152 | `StellarTorusStaff` | Star | 158 / 8 |
| 3 | 70%-50% | Tesla Staff | LightningChain | 146 | `Teslastaff` | Rift | 168 / 7 |
| 4 | 50%-30% | Twisting Thunder | LightningChain | 146 | `TwistingThunder` | Rift | 168 / 7 |
| 4 | 50%-30% | The Pack | Gunline | 134 | `ThePack` | Straight | 144 / 6 |
| 5 | 30%-12% | Shadowbolt Staff | SummonCore | 174 | `ShadowboltStaff` | Sentry | 196 / 6 |
| 5 | 30%-12% | Seadragon | CreatureRush | 138 | `Seadragon` | Creature | 142 / 6 |
| 6 | 12%-0% | Four Seasons Galaxia | StarField | 152 | `FourSeasonsGalaxia` | Star | 158 / 8 |
| 6 | 12%-0% | Reality Rupture | LightningChain | 146 | `RealityRupture` | Rift | 168 / 7 |

## 招式行为

- 风暴编织者偏闪电/远程武器组合：`LightningChain` 很多，会从玩家附近随机生成旋转裂隙线。
- `Gunline` 的 Skytide Dragoon、Corinth Prime、The Pack 都是直线散射，节奏稳定。
- `SummonCore` 的 Aqua's Scepter / Shadowbolt Staff 是哨兵阵。
- `StarField` 的 Stellar Torus Staff / Four Seasons Galaxia 从外环向玩家收束。
- `CreatureRush` 的 Seadragon 会从侧边刷生物型武器横穿。

## 疑似问题

- 极高风险：头、身体、尾巴全部注册同一个 `Worm` AI；公共逻辑没有头部限定，因此 body/tail 也可能独立发招、独立追玩家，蠕虫结构会被打散。
- 高风险：原 Storm Weaver 的护甲阶段、电流、段节受击逻辑没有在当前文件体现。
- 中风险：LightningChain 占比较高，阶段 1、3、4、6 都有，视觉上可能很像同一类蓝色裂隙。
- 中风险：`Teslastaff` 这个 ItemInternalName 本地能找到同名文件，所以不是贴图错误；但显示名写作 `Tesla Staff`，文件名小写 s 的命名需要后续统一认知。
- 低风险：本地物品名检查未发现缺失。

