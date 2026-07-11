# 西格纳斯AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/Signus/Signus.cs`
- `CalamityMod/NPCs/Signus/CosmicLantern.cs`
- `CalamityMod/NPCs/Signus/CosmicMine.cs`
- `CalamityMod/Items/Materials/TwistingNether.cs`

## 行为复述

- Signus.cs 的战斗语言是暗影瞬移、冲刺、宇宙灯/宇宙地雷和短促弹幕。
- 它经常消失、重定位、从玩家侧后方出现，然后用冲刺或苦无式弹幕切断逃跑路线。
- CosmicLantern 和 CosmicMine 是重要副压力：一个像漂浮火力点，一个像延迟触发的空间陷阱。
- 源码用透明度、位置突变、冲刺计时和弹幕计时制造幽灵感；复述时要强调“读预兆”而不是只写高速。
- Twisting Nether 系列适合承接它的暗杀主题：低语、死亡升华、天体武器和现实撕裂都来自同一种扭曲空间感。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
