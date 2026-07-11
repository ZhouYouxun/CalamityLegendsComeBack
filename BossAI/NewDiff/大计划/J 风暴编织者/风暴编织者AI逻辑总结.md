# 风暴编织者AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/StormWeaver/StormWeaverHead.cs`
- `CalamityMod/NPCs/StormWeaver/StormWeaverBody.cs`
- `CalamityMod/NPCs/StormWeaver/StormWeaverTail.cs`
- `CalamityMod/Items/Materials/ArmoredShell.cs`

## 行为复述

- StormWeaverHead.cs 控制头部蠕虫 AI，Body/Tail 作为身体链条跟随；第一阶段有装甲，第二阶段失去装甲后速度和攻击性提高。
- 它的核心节奏是高速穿场、从屏幕外回切、配合闪电/风暴弹幕封锁玩家上升或横移路线。
- 装甲阶段更像耐久考验，裸露阶段更强调速度和贴身威胁；源码通过不同头部贴图/状态表现这一转折。
- Armored Shell 武器库可以作为“剥落装甲后被玩家利用”的设计来源：电、海、风暴、机械召唤都能合理归入。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
