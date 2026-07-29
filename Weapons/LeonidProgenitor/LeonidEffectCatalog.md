# LeonidProgenitor 特效资产说明

本武器的原创无伤害大幕与粒子特效必须放在 `Effects/`，每个新特效各自使用独立文件；弹幕逻辑仍放在 `LeftClick/` 或 `RightClick/`。

- **狮子小星光** — `LeonidStarlightMote` + `LeonidStarlightShape.Mote`：细长、缓慢旋转上浮的十字星芒。
- **狮子大星光** — `LeonidStarlightMote` + `LeonidStarlightShape.Shard`：较大的四芒星碎片；每批最多一颗可进入纯视觉索敌冲刺。
- **狮子座北极星大幕** — `LeonidPolarStarBurst`：五节点数学星爆、柔光核心与定向脉冲环；纯视觉，不造成伤害。

统一入口是 `Effects/LeonidStarlight.cs` 与 `Effects/LeonidPolarStarBurst.cs`。不要把新的原创粒子大幕直接内联到弹幕类中。
