# Malachite 弹幕特效笔记

范围：当前 `Weapons/Malachite` 下的弹幕类；不统计 `Player`、Cooldown/UI 类。下面把特效分成“绘制函数特效”和“AI/命中粒子特效”。

## MalachiteKunai
- 绘制函数特效：`PreDraw` 手动画飞刀本体；飞行态用 `Projectile.oldPos` 画残影轨迹，普通/未激活飞刀额外用 `Main.DiscoR,203,103` 的原版孔雀翎发光色；存储态会画三层染色堆叠；Ace 视觉再叠一层放大的黄绿色光。
- AI/命中粒子特效：非存储态生成 `DustID.Terra` 运动尾尘；原版轨迹模式额外生成无速度 `Terra` glow dust；死亡时爆一圈 `DustID.Terra`；Ace 或 Ace 变体命中时生成 `MalachiteGreenExplosion`。

## MalachiteGreenExplosion
- 绘制函数特效：`PreDraw` 用 Malachite 自身贴图绕中心画 6 层旋转绿光；终结技版本使用更大的范围和更长持续时间。
- AI/命中粒子特效：持续生成 `DustID.Terra`，普通爆炸半径较小，终结技爆炸半径和速度更高；终结技版本会周期性 `Projectile.Damage()`。

## MalachiteFinaleController
- 绘制函数特效：`PreDraw` 画终结技充能场景；`DrawSpotlight` 使用 `BloomCircle` 形成从屏幕上方向玩家落下的绿色聚光和环形光晕，使用 `CalamityMod/Projectiles/StarProj` 画十字星芒，并用 Malachite 贴图画中心旋转光；`DrawPetals` 用 Malachite 贴图在屏幕空间生成 52 片花瓣。
- AI/命中粒子特效：充能时周期性生成 `CustomPulse("CalamityMod/Particles/SoftRoundExplosion")`、向玩家聚拢的 `SparkParticle` 和 `DustID.Terra`；装备 Gale Ace 时会持续生成 `MalachiteFinalePetal`；释放时生成 `MalachiteFinaleSlash`，并对 1600 范围内敌人生成终结技版 `MalachiteGreenExplosion`。

## MalachiteFinaleSlash
- 绘制函数特效：`PreDraw` 用 Malachite 贴图拉伸成 7 道并排绿色 slash，中心一条偏白。
- AI/命中粒子特效：自身没有额外粒子，只加绿光 `Lighting.AddLight`，第 2 帧手动触发伤害。

## MalachiteFinalePetal
- 绘制函数特效：`PreDraw` 用 Malachite 贴图压成小花瓣，颜色从粉色向黄绿色插值。
- AI/命中粒子特效：自身没有额外粒子，只做下落/横向飘移和淡出，并加微弱绿光。
