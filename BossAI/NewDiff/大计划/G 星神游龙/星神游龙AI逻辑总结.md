# 星神游龙AI逻辑总结

> 依据当前本地 CalamityMod 源码复述；这里只总结行为结构和可转写成 Boss 招式的设计信息。

## 源码入口

- `CalamityMod/NPCs/AstrumDeus/AstrumDeusHead.cs`
- `CalamityMod/NPCs/AstrumDeus/AstrumDeusBody.cs`
- `CalamityMod/NPCs/AstrumDeus/AstrumDeusTail.cs`

## 行为复述

- AstrumDeusHead.cs 管理整条蠕虫式 Boss，Body/Tail 负责多节身体跟随和分段绘制/碰撞。
- 它的核心 AI 是长身体追踪、转向、分裂/多段协同，以及在不同阶段穿插星辉弹幕。
- 与常规蠕虫不同，Astrum Deus 更强调星辉主题的远程压制：星弹、激光、碎片会从身体节奏中穿插出来。
- 源码中头部决定目标追踪和速度，身体节段跟随前一节位置，因此攻击设计要避免只看头部，还要利用身体轨迹封路。
- 低血量或特殊阶段会提升移动与弹幕密度，适合做成“游龙盘场，星雨补缝”的战斗语言。

## 改写提示

- 第一份文件里的武器路径负责找贴图和弹幕原型；第二份文件里的招式负责把这些原型转成 Boss 可用语法。
- 若后续真正实现代码，建议优先保留源码 AI 的阶段节奏，再把武器招式作为阶段内的攻击模块插入。
