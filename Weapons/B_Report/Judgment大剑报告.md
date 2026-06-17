# Judgment大剑报告

本报告对灾厄模组中 [GreatswordofJudgement](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Items/Weapons/Melee/GreatswordofJudgement.cs) 及其配套弹幕 [JudgementProj](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Projectiles/Melee/JudgementProj.cs) 和 [StarofJudgement](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Projectiles/Melee/StarofJudgement.cs) 的视觉特效实现方式进行深入分析。

---

## 一、武器整体结构概览

审判巨剑不走 `BaseCustomUseStyleProjectile` 基类路线，而是直接在物品类中用 `MeleeEffects` + `UseItemHitbox` 接管挥舞逻辑，属于**原生 ItemUseStyleID.Swing 挥舞 + 手动动画覆写**的混合方案。

核心文件清单：

| 文件 | 职责 |
| :--- | :--- |
| [GreatswordofJudgement.cs](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Items/Weapons/Melee/GreatswordofJudgement.cs) | 挥舞动画、判定盒、光波弹幕发射、近战星星生成 |
| [JudgementProj.cs](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Projectiles/Melee/JudgementProj.cs) | 光波弹幕：扩散、边缘粒子、5层叠绘渲染 |
| [StarofJudgement.cs](file:///d:/Documents/My%20Games/Terraria/tModLoader/ModSources/CalamityMod/Projectiles/Melee/StarofJudgement.cs) | 小星星弹幕：追踪、弧形天降激光、双层叠绘残影 |

---

## 二、挥舞动画实现（GreatswordofJudgement）

### 2.1 旋转插值逻辑

挥舞过程被 `completion`（0 → 1 的归一化进度）分为两段：

- **前摆阶段（completion ≤ 0.2）**：剑从 `startRot`（上扬）插值到 `minRot`（最大上扬顶点），使用 `CalamityUtils.EaseInOutExp(lerp, 4f, 4f)` 产生先快后慢的蓄力感。
- **斩击阶段（completion > 0.2）**：从 `minRot` 插值到 `endRot`（对侧），使用 `EaseInOutExp(lerp, 6f, 2f)`（前段极快、后段减速），模拟"用力甩刀 → 收刀"的真实物理感。

每次 `UseAnimation` 时 `swingCount++`，奇偶交替决定挥舞方向（左右往复）：

```csharp
float startRot = MathHelper.ToRadians(-110) * dir * (swingCount % 2 == 0 ? 1 : -1);
```

### 2.2 手臂动画

```csharp
player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, player.itemRotation + MathHelper.ToRadians(-130) * dir);
player.SetCompositeArmBack(true,  Player.CompositeArmStretchAmount.Full, player.itemRotation + MathHelper.ToRadians(-130) * dir);
```

双臂同步跟随 `itemRotation`，呈现双手持巨剑用力砍下的姿势。

### 2.3 碰撞判定盒

```csharp
bladeHitboxPos = player.Center + (player.itemRotation + extraRot).ToRotationVector2() * 180 * scaling;
```

`bladeHitboxPos` 每帧跟随刀刃实时位置，`UseItemHitbox` 将判定盒缩放8倍后对齐到该点，`CanHitNPC` 再用 `CheckAABBvLineCollision` 做线段精确检测——判定盒大但实际判定严格。

### 2.4 颜色随机

每次起挥时随机抽取一次主色：

```csharp
clr = Main.rand.NextBool() ? Color.MediumPurple : Color.MediumOrchid;
```

这个颜色传给刀光粒子和光波弹幕，让每次挥砍的紫色调有细微差异。

---

## 三、光波弹幕特效（JudgementProj）

这是整个武器视觉中最核心的技法，**不依赖任何 Shader，仅靠贴图叠绘实现扩散光波效果**。

### 3.1 发射时机

在 `completion >= 0.65f`（斩击进行到 65%）时，物品类发射光波弹幕：

```csharp
Projectile beam = Projectile.NewProjectileDirect(..., Item.shoot, ...);
beam.ai[1] = scaling;  // 把玩家体型缩放传入
```

同时生成一个 `CustomSpark`（`VerticalSmearLarge` 贴图）作为刀光拖尾。

### 3.2 膨胀扩散

光波每帧自然膨胀：

```csharp
Projectile.scale += 0.0026f;   // scale 从 0.0875 开始线性增长
hitboxSize += 0.4525f;          // 伤害判定圆半径同步扩大
Projectile.velocity *= 0.995f;  // 缓慢减速，模拟波形衰减
```

200 帧后只减速不再膨胀，300 帧后开始淡出（`fade -= 0.0065f`）。

### 3.3 5层叠绘渲染（核心手法）

```csharp
for (int i = 0; i < 5; i++)
    Main.spriteBatch.Draw(tex, pos + forwardDir * 9 * i, null,
        mainColor with { A = 0 } * fadeOut * 0.6f,
        rotation,
        tex.Size() / 2f,
        new Vector2(1 - (0.2f * waveFade) + 0.01f * i,
                    1 + (0.45f * waveFade) - 0.06f * i) * scale,
        SpriteEffects.None, 0);
```

**关键点拆解：**

- **`{ A = 0 }`**：将颜色的 Alpha 通道置零，配合 SpriteBatch 的加法混合，让颜色叠加时只增亮不透明度覆盖。
- **5次偏移（`forwardDir * 9 * i`）**：每层沿飞行方向偏移 9 像素，5层叠在一起形成前端厚重、后端拉长的光晕轮廓。
- **X/Y 独立缩放随 `waveFade` 变化**：
  - X 轴：`1 - 0.2 * waveFade`（贴图随时间横向收窄）
  - Y 轴：`1 + 0.45 * waveFade`（贴图随时间纵向拉长）
  - 效果：弹幕刚发出时横宽竖窄（冲击波形），随距离扩散变成竖长横窄（扩散的光柱）。
- **每层额外 `±0.01i / ±0.06i` 微调**：5层缩放比例稍有差异，堆叠后产生自然的边缘虚化，而非生硬的重叠。

### 3.4 边缘粒子（翼尖效果）

光波两侧各计算出一个"翼尖"位置：

```csharp
Vector2 topCorner    = Center + velocity.SafeNormalize().RotatedBy(100°)  * 137 * scale;
Vector2 bottomCorner = Center + velocity.SafeNormalize().RotatedBy(-100°) * 137 * scale;
```

在 `time < 200` 期间持续生成两种粒子：

| 粒子类型 | 频率 | 方向 | 视觉作用 |
| :--- | :--- | :--- | :--- |
| `SparkParticle` | 每4帧 | 朝外侧 185° 飞出 | 翼尖持续向外喷出细长火花 |
| `GlowOrbParticle` | 1/12 概率 | 朝外侧 170° 飞出，速度 10~30 | 随机溅射紫色发光球，形成两翼光晕 |

另外主体内每3帧生成一个 `SquashDust`，朝速度反方向飘散，形成弹幕后方的紫色尘埃拖尾。

---

## 四、小星星弹幕特效（StarofJudgement）

小星星在两处生成，行为由 `ai[2]` 区分：

| `ai[2]` 值 | 生成来源 | 行为模式 |
| :--- | :--- | :--- |
| `0` | `GreatswordofJudgement.OnHitNPC` — 击中敌人时 | 普通追踪星，穿透2次，击中后重置 timeLeft |
| `1` | `GreatswordofJudgement.OnHitNPC` — 同时生成 | 弧形天降激光模式（见下节） |
| `0` | `JudgementProj.AI` — 光波每85帧 | 从光波中心向后喷出，追踪敌人 |

### 4.1 双层叠绘渲染

```csharp
// 第1层：残影尾迹（TrailingMode = 2，记录旋转+位置）
CalamityUtils.DrawAfterimagesCentered(Projectile, 2, drawColor * 0.5f, 1, texture, true, true);
// 第2层：主体（紫色，A=0 加法混合）
Main.EntitySpriteDraw(texture, pos, null, drawColor, rotation, origin, scale, ...);
// 第3层：白色内核（0.8倍缩放）
Main.EntitySpriteDraw(texture, pos, null, Color.White with { A = 0 }, rotation, origin, scale * 0.8f, ...);
```

三层叠加：弧形残影尾迹 + 紫色主体 + 白色内核，产生"中心发白发亮、边缘渐变为紫色"的效果，视觉上像一颗真实的发光星体。

### 4.2 旋转动态

```csharp
Projectile.rotation += (Projectile.velocity.X + MathF.Abs(Projectile.velocity.Y) * Projectile.direction) * 0.01f;
```

旋转速度由速度分量驱动而非固定值，拐弯追踪时自然产生翻滚变速，弧线尾迹跟随旋转朝向弯曲——这是 `TrailingMode = 2` 的功劳。

### 4.3 弧形天降激光（`ai[2] == 1` 模式）

```csharp
// 前18帧：匀速弧形旋转（不追踪，只画圆弧）
Projectile.velocity = Projectile.velocity.RotatedBy(0.085f * Projectile.ai[1]);
// 18帧后：切换为HomeInOnNPC强力追踪（追踪范围900，速度25）
CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 25, MathHelper.Clamp(30 - time, 15, 30));
```

先弧形拉出弧线（看起来像从天边飞来），再急转追踪目标，轨迹呈现出"天降弧光"的视觉。每2帧在位置上生成一个竖向拉长（`Vector2(0.8f, 1.35f)`）的 `BloomCircle` 粒子，形成弧线轨迹上的光晕条带。

死亡时爆出5个方向均匀分布的 `PointParticle` 做收尾爆碎。

---

## 五、总结

| 特效 | 核心手法 |
| :--- | :--- |
| 光波扩散形态 | 5层相同贴图叠绘，X/Y 轴缩放独立随时间变化 |
| 光波加法混合亮度 | 颜色 `{ A = 0 }` 去透明，加法叠加越叠越亮 |
| 两翼光晕 | 实时计算翼尖坐标，持续生成 SparkParticle + GlowOrbParticle |
| 星星发光感 | 三层叠绘：半透明残影 + 紫色主体（A=0）+ 白色内核（0.8x） |
| 弧形天降轨迹 | 前18帧 `RotatedBy` 匀速画弧，之后 `HomeInOnNPC` 急转追踪 |
| 激光轨迹光晕 | 每2帧在路径上生成竖拉长 BloomCircle 粒子 |

整套效果全程无 Shader，完全依赖**贴图参数化叠绘**和**粒子精确布点**实现。审判巨剑是灾厄中少数不走 `BaseCustomUseStyleProjectile` 基类但视觉效果同样顶级的自定义挥舞武器，值得在制作同类效果时参考。
