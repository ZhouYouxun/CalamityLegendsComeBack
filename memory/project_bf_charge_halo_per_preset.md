---
name: project_bf_charge_halo_per_preset
description: BlossomFlux右键蓄力环绕光环已改为按5战术各自一套轨迹风格;风格表与调参入口
metadata:
  type: project
---

BlossomFlux 右键蓄力时环绕自身的无伤着色器弹幕(`BFRightChargeHaloProj`)已从"单一斜圆投影"重构为**按 5 种战术各自独立的轨迹风格**。用户要求:每种战术的这枚小弹幕在 轨迹/数量/生成位置/速度·加速度·减速度 上都不同,且各有设计风格。

**架构:** `BFRightChargeHaloProj` 是通用着色拖尾载体(CalamityMod:TrailStreak),运动由 `BFHaloStyle` 枚举分派;所有每战术参数集中在生成器 `NewLegendBlossomFluxHoldOut.RightChargeLattice.cs` 的 `UpdateRightChargeHaloSpawning` 里,用 `HaloSpawnParams` 结构一次性传入(数量/半径/寿命/速度档/加减速跟随率/淡出率/形状参数)。改手感就改那里。

**5 风格(preset→style):**
- A 破甲 SlashStar:玫瑰线尖瓣星,快转+锐利半径脉冲,少量短命像刀光
- B 恢复 RisingHelix:慢转小圆+持续上浮+呼吸,柔和长命
- C 侦测 GyroRing:复用斜圆投影,但整环 5 枚同倾角同朝向按相位均分→陀螺仪/雷达环,匀速精密(min=max速度)
- D 爆破 EmberBurst:绝对坐标弹道余烬,近枪口高速外抛+强阻力减速+微重力,成簇脉冲
- E 瘟疫 WobbleCloud:慢转+垂直正弦抖动(叠层伪噪声)+半径缓扩,粘稠长命

遵守 [[feedback_no_delete_effects]](未删任何原特效,斜圆投影保留给C)、[[feedback_design_sense]]、[[feedback_scope_discipline]](只动这两个文件)。参见 [[reference_effect_teardown_standard]]。
