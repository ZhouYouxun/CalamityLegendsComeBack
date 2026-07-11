# 灾厄克隆体AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/CalClone/CalamitasClone.cs`
- `CalamityMod/NPCs/CalClone/Cataclysm.cs`
- `CalamityMod/NPCs/CalClone/Catastrophe.cs`
- `CalamityMod/Items/TreasureBags/CalamitasCloneBag.cs`

## 行为复述

- CalamitasClone.cs 以 lifeRatio 切阶段：约 70%、35%、10% 附近改变节奏，并在关键血线启动弹幕地狱/兄弟支援。
- 常规循环是悬停、侧向定位、预备冲刺、硫火弹幕。她会先把自己放在玩家上方或侧上方，再用速度插值进入攻击点。
- 弹幕核心包括 BurningFireblast、BurningGigablast、CalamitousDart、CalamitousFireball 等，很多攻击先给玩家可读位置，再用延迟爆发惩罚横向逃跑。
- ArenaBox / Bullet Hell 逻辑会把战斗临时收进更小的空间，重点不是单发速度，而是持续封路和方向切换。
- Cataclysm 与 Catastrophe 在中后段作为灾厄兄弟介入：一个偏冲刺/近身，一个偏火焰喷吐/远程，迫使玩家同时读本体和副目标。
- 最后低血量会更频繁使用高密度硫火、快速换位和死亡前弹幕，适合在改写时做成“克隆体不稳定过载”的视觉主题。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
