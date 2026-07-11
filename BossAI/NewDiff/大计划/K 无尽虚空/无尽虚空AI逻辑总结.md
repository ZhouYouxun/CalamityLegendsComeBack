# 无尽虚空AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/CeaselessVoid/CeaselessVoid.cs`
- `CalamityMod/NPCs/CeaselessVoid/DarkEnergy.cs`
- `CalamityMod/Items/Materials/DarkPlasma.cs`

## 行为复述

- CeaselessVoid.cs 以中心虚空核心、Dark Energy 环绕物和阶段性吸引/释放为核心。
- 它不是传统追人 Boss，而是通过场地中心、能量体数量、吸力和环形弹幕控制玩家位置。
- DarkEnergy 小体会在战斗中承担保护、攻击和阶段推进功能；玩家需要处理外圈能量，才能稳定输出核心。
- 源码中可见多种计时器控制收缩、扩张、旋转和弹幕释放，非常适合复述为“黑洞呼吸”：吸入、静止、爆发、再吸入。
- Dark Plasma 武器库应强调虚空物理：奇点、镜像、事件视界、幽魂火力和空间撕裂。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
