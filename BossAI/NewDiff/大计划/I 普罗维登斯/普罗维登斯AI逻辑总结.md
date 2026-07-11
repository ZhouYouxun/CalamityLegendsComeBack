# 普罗维登斯AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/Providence/Providence.cs`
- `CalamityMod/NPCs/Providence/ProvSpawn*.cs`
- `CalamityMod/Items/Materials/DivineGeode.cs`

## 行为复述

- Providence.cs 是多状态大型 Boss：白天/夜晚、神圣地/地狱环境、攻击/防御外观都会影响视觉和部分行为。
- 常规循环包括悬停、冲刺、Holy Blast、Holy Ray、熔火爆弹、治愈/防御/进攻守卫召唤等。
- 她会召唤 ProvSpawnDefense、ProvSpawnHealer、ProvSpawnOffense 等守卫，守卫让战斗从单体弹幕变成“本体 + 功能型小目标”。
- 源码里大量使用攻击计时器和阶段变量切换招式；生命降低后圣光束、爆弹和位移频率都会增强。
- 死亡动画和昼夜贴图很重要，文案设计可以把 Divine Geode 武器库理解为她神圣/亵渎能量结晶后的扩展。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
