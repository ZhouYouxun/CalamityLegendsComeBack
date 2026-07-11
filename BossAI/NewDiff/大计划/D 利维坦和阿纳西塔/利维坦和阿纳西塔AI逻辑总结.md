# 利维坦和阿纳西塔AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/Leviathan/Leviathan.cs`
- `CalamityMod/NPCs/Leviathan/Anahita.cs`
- `CalamityMod/Items/TreasureBags/LeviathanBag.cs`

## 行为复述

- Anahita.cs 先作为海妖单体战斗，按 70%、40%、20% 等血量推进阶段；进入关键阶段后召唤 Leviathan，战斗变成双 Boss 压力。
- Anahita 的行为偏空中定位、冲刺、歌声/水矛/寒霜雾弹幕，并会有 Ice Shield 之类保护或转场逻辑。
- Leviathan.cs 体型巨大，主要通过侧向贴近、预备后高速冲撞、吐出水弹/巨石/炸弹式弹幕形成大范围压力。
- 双 Boss 同场时，Anahita 更像节奏器和封路器，Leviathan 更像空间压缩器；源码中大量用距离、目标位置和阶段变量决定谁主攻。
- 设计复述时可以保留“海妖先奏乐、巨兽后碾压”的结构：前半读细线，后半读大体积冲撞与落物。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
