# 灾厄克隆体 - 当前新 BossAI 实现报告

## 读取范围

- 主 AI：`Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasCloneWeaponizedAI.cs`
- 招式与弹幕：`Content/BehaviorOverrides/BossAIs/CalamitasClone/Phases/**`
- 注册对象：`CalamitasClone`
- 移动模型：`Hover`，绕玩家上方移动；没有看到兄弟、灵魂墙、分身等原灾厄克隆体流程。
- 阶段阈值：90%、70%、50%、30%、12%，每阶段目前只有 1 个招式。

## 当前招式表

| 阶段 | HP 区间 | 招式 | 模式 | 时长 | 绘制物品 | 默认弹幕样式 | 生命周期 / 爆裂 |
|---|---:|---|---|---:|---|---|---|
| 1 | 100%-90% | Oblivion | MagicCore | 156 | `Oblivion` | Spinning | 144 / 6 |
| 2 | 90%-70% | Animosity | MagicCore | 156 | `Animosity` | Spinning | 144 / 6 |
| 3 | 70%-50% | Lashes of Chaos | MagicCore | 156 | `LashesofChaos` | Spinning | 144 / 6 |
| 4 | 50%-30% | Entropy's Vigil | SpaceRift | 158 | `EntropysVigil` | Rift | 168 / 7 |
| 5 | 30%-12% | Crushsaw Crasher | MagicCore | 156 | `CrushsawCrasher` | Spinning | 144 / 6 |
| 6 | 12%-0% | Havoc's Breath | MagicCore | 156 | `HavocsBreath` | Spinning | 144 / 6 |

## 招式行为

- `MagicCore` 是本 Boss 的主体：每个招式开场有两个 held 武器展示，然后在玩家附近生成哨兵武器；哨兵每隔约 34 帧吐 shard，公共逻辑还会在 58/118 帧打环形弹，招式本身在 42/94 帧再补一轮 7 发环形弹。
- `Entropy's Vigil` 是唯一不同模式，使用 `SpaceRift`：在玩家中心附近多次生成 rift 环，并用旋转裂隙弹形成短时间围压。
- 每阶段只有一招，所以阶段循环非常短：跨阶段后会反复播放同一把武器直到下一个血量阈值。

## 疑似问题

- 高风险：六个阶段有五个都是 `MagicCore`，玩法骨架几乎一致，只换武器贴图；作为灾厄克隆体会显得招式重复。
- 高风险：灾厄克隆体原本的重要阶段事件没有在当前 AI 中体现，尤其是兄弟召唤、弹幕墙、阶段转场压迫。
- 中风险：每阶段只有 1 招，没有循环组合；如果玩家输出不足，会在同一招上停留很久。
- 中风险：MagicCore 里传给 `FireRadial` 的 `Star` 样式可能被 `StyleToAI` 丢掉，实际更多依赖 projectile 的 `DefaultStyle=Spinning`。
- 低风险：武器物品名都能解析到本地灾厄物品，贴图路径暂时安全。

