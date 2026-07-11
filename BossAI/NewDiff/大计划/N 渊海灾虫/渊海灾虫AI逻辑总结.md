# 渊海灾虫AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/AquaticScourge/AquaticScourgeHead.cs`
- `CalamityMod/NPCs/AquaticScourge/AquaticScourgeBody.cs`
- `CalamityMod/NPCs/AcidRain/*.cs`

## 行为复述

- AquaticScourgeHead.cs 是蠕虫式 Boss，Body/Tail 跟随头部，整体围绕海水/硫磺海主题的穿场移动。
- 它的压力来自长身体封路、毒云/硫磺弹、水流弹幕和阶段性速度变化。
- 作为酸雨阶段推进点，击败后酸雨事件进入第二阶段，更多酸雨敌怪和武器掉落被激活。
- 设计上可以把本体武器写成海灾核心，把第二阶段酸雨武器写成生态被污染升级后的外延。
- 酸雨二阶段武器普遍适合做成毒雾、高射、燃料、酸液召唤物和边界抓取这类场地题。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
