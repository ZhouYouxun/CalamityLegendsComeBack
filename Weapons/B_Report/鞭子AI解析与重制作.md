# 泰拉瑞亚：鞭子 AI 深度解析与从零构件重制作指南

本报告旨在帮助开发者和学生**彻底理解泰拉瑞亚原版鞭子（aiStyle 165）的底层逻辑**，并提供**从零开始构建完全自定义鞭子 AI** 的理论支撑与实战代码。

---

## 目录
1. [为什么需要从零重构鞭子 AI？](#1-为什么需要从零重构鞭子-ai)
2. [原版鞭子核心机制解析](#2-原版鞭子核心机制解析)
    * [2.1 AI 运动状态生命周期](#21-ai-运动状态生命周期)
    * [2.2 核心数学模型：控制点曲线计算（FillWhipControlPoints）](#22-核心数学模型控制点曲线计算fillwhipcontrolpoints)
    * [2.3 多段碰撞检测原理](#23-多段碰撞检测原理)
    * [2.4 五帧切片绘制系统](#24-五帧切片绘制系统)
3. [实战案例：三段式连击刃鞭设计](#3-实战案例三段式连击刃鞭设计)
    * [3.1 连击框架与 ModPlayer 设计](#31-连击框架与-modplayer-设计)
    * [3.2 武器物品类（ModItem）实现](#32-武器物品类moditem实现)
    * [3.3 自定义弹幕类（ModProjectile）重构](#33-自定义弹幕类modprojectile重构)
4. [鞭子美术贴图制作与配置规范](#4-鞭子美术贴图制作与配置规范)

---

## 1. 为什么需要从零重构鞭子 AI？

在 tModLoader 中，如果我们只想做一把属性不同的普通鞭子，直接继承 `ModProjectile` 并设置 `Projectile.aiStyle = ProjAIStyleID.Whip` 即可。然而，**原版鞭子 AI 的自由度极低**：
1. **轨迹死板**：原版鞭子只能按照固定的波浪形左右轮流扫击，无法实现突刺、回旋、多段折线等复杂物理轨迹。
2. **状态机受限**：原版鞭子的攻击阶段（伸出与收回）在代码中高度硬编码，无法在“收回阶段”动态调整伤害、无敌帧（i-frames），也无法在特定帧触发独特的弹幕动作。
3. **渲染绑定**：原版鞭子的纹理调用和骨骼帧数（5 帧）是完全写死的，无法使用更多帧的动态图或非标准尺寸贴图。

为了实现诸如**“第一打上方扫击且回收削弱、第二打下方反扫、第三打朝鼠标突刺并尖端爆血”**的动作游戏级连击效果，我们必须脱离 `aiStyle = 165`，在 `ModProjectile` 中**从零手写 AI、碰撞与绘制逻辑**。

---

## 2. 原版鞭子核心机制解析

### 2.1 AI 运动状态生命周期

原版鞭子的运动主要依赖以下几个变量：
* `ai[0]`：当前帧计数器（从 `0` 开始递增）。
* `timeToFlyOut`：鞭子从挥出到完全收回的总生命周期（单位为帧）。
  $$timeToFlyOut = player.itemAnimationMax * proj.MaxUpdates$$
* 伸展与回收阶段：
  * **伸出阶段（Extension Phase）**：前 $2/3$ 的生命周期。鞭子长度从 $0$ 逐渐伸展到最大，形状由弯曲的“蛇形”逐渐绷紧变直。
  * **收回阶段（Retreat Phase）**：后 $1/3$ 的生命周期。鞭子长度迅速收回至 $0$（缩回玩家手中），并在结束时调用 `Kill()`。

---

### 2.2 核心数学模型：控制点曲线计算（FillWhipControlPoints）

鞭子好看的波浪轨迹是由一系列**控制点**组成的折线。原版在 [Projectile.cs:FillWhipControlPoints()](file:///d:/Documents/My%20Games/Terraria/拆解模组/tML%202024.04源码与ExampleMod/tModLoader/Terraria/Projectile.cs#L34305) 中实现了这一算法。

我们可以用以下公式来拆解它：

1. **归一化进度 $t$**：
   $$t = \frac{\text{ai}[0]}{\text{timeToFlyOut}} \quad (t \in [0, 1])$$

2. **缩放伸展因子 $p$**：
   设置常数变量 $num3 = 1.5$（对应延伸占总时间的 $1 / 1.5 = 2/3$）。
   $$p = t \times 1.5$$
   若 $p > 1.0$（进入收回阶段）：
   计算收回进度 $r = \frac{p - 1.0}{0.5}$，此时将 $p$ 插值减小：$p = 1.0 - r$。
   > [!NOTE]
   > $p$ 的变化轨迹是：从 $0 \to 1.0$（前 $2/3$ 时间），再从 $1.0 \to 0$（后 $1/3$ 时间）。这控制了鞭子的整体拉伸长度。

3. **关节间偏角 $\theta_d$**：
   $$\theta_d = \frac{10\pi * (1 - p) * (-\text{spriteDirection})}{N}$$
   其中 $N$ 为鞭子关节总数（`Segments`）。
   > [!TIP]
   > 当 $p \to 0$ 时，$\theta_d$ 极大，关节之间产生极大的回旋角（鞭子卷曲）；当 $p \to 1.0$（鞭子伸到最长）时，$\theta_d \to 0$，关节偏角消失（鞭子绷直成一条直线）。这就是原版鞭子“甩开”的数学原理。

4. **坐标计算循环**：
   从玩家手部位置（`playerArmPosition`）出发，循环 $N$ 次，每次利用上一个节点的坐标加上旋转向量，计算出下一个节点的位置：
   ```csharp
   // 伪代码：计算第 i 个关节的位置
   float ratio = (float)i / segments;
   Vector2 offset = rotationVector.ToRotationVector2() * segmentLength;
   // 结合正弦曲线插值，平滑手部到鞭梢的过渡
   Vector2 segmentPos = lastPos + offset;
   ```
   最后，整根鞭子还会绕着手部进行基于 `projectile.rotation` 的整体弧形扫掠。

---

### 2.3 多段碰撞检测原理

因为鞭子是一条细长的折线，普通的矩形碰撞盒（Hitbox）无法满足精度。原版在 [Projectile.cs:Colliding()](file:///d:/Documents/My%20Games/Terraria/拆解模组/tML%202024.04源码与ExampleMod/tModLoader/Terraria/Projectile.cs#L12304) 中采用了**逐节点碰撞盒检测**：

```csharp
if (ProjectileID.Sets.IsAWhip[type]) {
    WhipPointsForCollision.Clear();
    FillWhipControlPoints(this, WhipPointsForCollision);
    // 遍历每一个关节
    for (int m = 0; m < WhipPointsForCollision.Count; m++) {
        Point point = WhipPointsForCollision[m].ToPoint();
        // 将弹幕的 Hitbox（通常是 20x20 或 30x30 的宽度）中心贴在当前关节坐标上
        myRect.Location = new Point(point.X - myRect.Width / 2, point.Y - myRect.Height / 2);
        // 如果这个关节的碰撞盒与怪物的碰撞盒重合，则判定击中
        if (myRect.Intersects(targetRect))
            return true;
    }
    return false;
}
```

---

### 2.4 五像素切片绘制系统

原版鞭子并不使用普通的单帧贴图绘制，而是将一张贴图纵向等分为 **5 帧**：
* **第 0 帧**：手柄（Handle）
* **第 1, 2, 3 帧**：身体段（Body Segment），绘制时通常循环滚动或按长度占比调用（如 `1 + i % 3`）
* **第 4 帧**：鞭头（Tip）

在 [Main.cs:DrawWhip()](file:///d:/Documents/My%20Games/Terraria/拆解模组/tML%202024.04源码与ExampleMod/tModLoader/Terraria/Main.cs#L30122) 中，通过计算关节 $i$ 和 $i+1$ 之间的方向向量：
$$\theta_{seg} = (pos_{i+1} - pos_i).\text{ToRotation}() - \frac{\pi}{2}$$
使该节贴图以此角度旋转，拼接出一条连续平滑的鞭子。

---

## 3. 实战案例：三段式连击刃鞭设计

我们要设计一把独特的连击刃鞭：
1. **第一打**：从上方劈下。在**收回阶段**伤害降低 80%，且无敌帧变为极长（防止回收阶段疯狂骗伤）。
2. **第二打**：从下方撩起。其余逻辑与第一打相同。
3. **第三打**：朝鼠标方向长距离突刺，尖端（Tip）造成 2.5 倍暴击伤害，鞭身无伤害。

### 3.1 连击框架与 ModPlayer 设计

由于连击状态需要在玩家多次点击之间传递，我们需要在 `ModPlayer` 中存储当前的连击序号。

```csharp
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.B_Report
{
    public class WhipComboPlayer : ModPlayer
    {
        // 0 = 第一打(上扫), 1 = 第二打(下扫), 2 = 第三打(突刺)
        public int ComboIndex = 0;
        public int ComboTimer = 0;

        public override void PostUpdate()
        {
            // 如果玩家超过一定时间没有攻击，重置连击
            if (ComboTimer > 0)
            {
                ComboTimer--;
                if (ComboTimer == 0)
                {
                    ComboIndex = 0;
                }
            }
        }
    }
}
```

---

### 3.2 武器物品类（ModItem）实现

```csharp
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.B_Report
{
    public class ComboWhipItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<ComboWhipProj>(), 40, 2f, 4f, 30);
            Item.useTurn = false;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var comboPlayer = player.GetModPlayer<WhipComboPlayer>();

            // 将当前的连击序号通过 ai[1] 传递给弹幕
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, comboPlayer.ComboIndex);

            // 推进连击阶段
            comboPlayer.ComboIndex = (comboPlayer.ComboIndex + 1) % 3;
            comboPlayer.ComboTimer = Item.useAnimation * 3; // 预留3倍攻速时间宽限期

            return false; // 禁用默认射击
        }
    }
}
```

---

### 3.3 自定义弹幕类（ModProjectile）重构

以下是核心弹幕的完整手写重构代码：

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace CalamityLegendsComeBack.Weapons.B_Report
{
    public class ComboWhipProj : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/B_Report/ComboWhipProj"; // 路径根据实际替换

        // 用于记录碰撞检测中，到底是哪一个关节碰到了怪物
        private int lastHitSegmentIndex = -1;

        public override void SetStaticDefaults()
        {
            // 避开原版 AI 判定，我们自己写逻辑，但依然保留鞭子分类标签
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30; // 默认击中冷却
        }

        // 连击序号属性
        public int ComboIndex => (int)Projectile.ai[1];
        
        // 动作伸缩与回收状态计算
        private void GetWhipProgress(out float progress, out bool isRetreating, out float retreatProgress)
        {
            Player player = Main.player[Projectile.owner];
            float timeToFlyOut = player.itemAnimationMax * Projectile.MaxUpdates;
            
            float t = Projectile.ai[0] / timeToFlyOut;
            float num3 = 1.5f; // 前 2/3 时间延伸，后 1/3 收回
            float p = t * num3;
            
            if (p > 1.0f)
            {
                isRetreating = true;
                retreatProgress = (p - 1.0f) / 0.5f;
                progress = MathHelper.Lerp(1f, 0f, retreatProgress);
            }
            else
            {
                isRetreating = false;
                retreatProgress = 0f;
                progress = p;
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active)
            {
                Projectile.Kill();
                return;
            }

            // 保持手持
            player.heldProj = Projectile.whoAmI;
            player.MatchItemTimeToItemAnimation();

            // 旋转与朝向
            Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
            Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f) ? 1 : -1;

            // 推进时间戳
            Projectile.ai[0] += 1f;

            float timeToFlyOut = player.itemAnimationMax * Projectile.MaxUpdates;
            if (Projectile.ai[0] >= timeToFlyOut)
            {
                Projectile.Kill();
                return;
            }

            // 在攻击极点播放抽击声效
            if (Projectile.ai[0] == (float)(int)(timeToFlyOut / 2f))
            {
                List<Vector2> tempPoints = new List<Vector2>();
                GenerateWhipPoints(tempPoints);
                SoundEngine.PlaySound(SoundID.Item153, tempPoints[tempPoints.Count - 1]);
            }
        }

        /// <summary>
        /// 自定义曲线控制点计算函数（支持三种不同的动作曲线）
        /// </summary>
        private void GenerateWhipPoints(List<Vector2> points)
        {
            GetWhipProgress(out float progress, out bool isRetreating, out float retreatProgress);

            Player player = Main.player[Projectile.owner];
            int segments = 30; // 关节数量
            float rangeMultiplier = 1.2f; // 长度系数

            Vector2 startPos = Main.GetPlayerArmPosition(Projectile);
            points.Add(startPos);

            // 连击 2：下方反扫，只需将扫击方向反转
            int directionSign = Projectile.spriteDirection;
            if (ComboIndex == 1)
            {
                directionSign *= -1; // 反向扫击
            }

            // 连击 0 & 1：普通与反向扫击曲线（原版变体）
            if (ComboIndex == 0 || ComboIndex == 1)
            {
                float totalLength = Projectile.velocity.Length() * (player.HeldItem.useAnimation * 2) * (Projectile.ai[0] / (player.itemAnimationMax * Projectile.MaxUpdates)) * player.whipRangeMultiplier;
                float segLen = totalLength * progress * rangeMultiplier / segments;
                
                // 控制关节卷曲程度的角度变化率
                float angleStep = (float)Math.PI * 10f * (1f - progress) * (float)(-directionSign) / segments;

                Vector2 currentPos = startPos;
                float baseAngle = - (float)Math.PI / 2f;
                float leftAngle = baseAngle - (float)Math.PI / 2f;
                float rightAngle = baseAngle + (float)Math.PI / 2f;

                for (int i = 0; i < segments; i++)
                {
                    float ratio = (float)i / segments;
                    float currentStep = angleStep * ratio;

                    Vector2 extendVec = currentPos + baseAngle.ToRotationVector2() * segLen;
                    Vector2 leftVec = currentPos + leftAngle.ToRotationVector2() * (segLen * 2f);
                    Vector2 rightVec = currentPos + rightAngle.ToRotationVector2() * (segLen * 2f);

                    float invProgress = 1f - progress;
                    float lerpWeight = 1f - invProgress * invProgress;

                    Vector2 temp = Vector2.Lerp(leftVec, extendVec, lerpWeight * 0.9f + 0.1f);
                    Vector2 targetVec = Vector2.Lerp(rightVec, temp, lerpWeight * 0.7f + 0.3f);

                    Vector2 rawPoint = startPos + (targetVec - startPos) * new Vector2(1f, 1.5f);
                    
                    // 回收时的旋转下砸角度
                    float rotFactor = retreatProgress * retreatProgress;
                    Vector2 finalPoint = rawPoint.RotatedBy(Projectile.rotation + 4.712389f * rotFactor * (float)directionSign, startPos);

                    points.Add(finalPoint);

                    baseAngle += currentStep;
                    leftAngle += currentStep;
                    rightAngle += currentStep;
                    currentPos = extendVec;
                }
            }
            // 连击 2：刺击曲线（突刺）
            else if (ComboIndex == 2)
            {
                float maxReach = 450f * player.whipRangeMultiplier; // 最大刺出距离
                float currentReach = maxReach * progress; // 根据进度伸缩

                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

                for (int i = 1; i <= segments; i++)
                {
                    float ratio = (float)i / segments;
                    // 计算基础直线坐标
                    Vector2 linePos = startPos + direction * (currentReach * ratio);

                    // 为突刺添加高频细微震颤，使其有“钻刺”的动态质感
                    Vector2 perpVec = direction.RotatedBy(Math.PI / 2);
                    float shake = (float)Main.rand.NextFloat() * 2f - 1f; // 随机轻微抖动
                    Vector2 finalPos = linePos + perpVec * shake;

                    points.Add(finalPos);
                }
            }
        }

        // 碰撞检测重写
        public override bool Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            List<Vector2> points = new List<Vector2>();
            GenerateWhipPoints(points);

            // 连击 2 (突刺)：只有鞭子最末端（Tip）具有攻击判定
            if (ComboIndex == 2)
            {
                Vector2 tipPos = points[points.Count - 1];
                Rectangle tipHitbox = new Rectangle((int)tipPos.X - 25, (int)tipPos.Y - 25, 50, 50); // 尖端碰撞盒略大
                if (tipHitbox.Intersects(targetHitbox))
                {
                    lastHitSegmentIndex = points.Count - 1; // 标记击中点为鞭梢
                    return true;
                }
                return false;
            }

            // 连击 0 & 1 (扫击)：全段检测
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 pos = points[i];
                projHitbox.Location = new Point((int)pos.X - projHitbox.Width / 2, (int)pos.Y - projHitbox.Height / 2);
                if (projHitbox.Intersects(targetHitbox))
                {
                    lastHitSegmentIndex = i; // 记录发生碰撞的节点索引
                    return true;
                }
            }

            return false;
        }

        // 修改伤害属性与无敌帧逻辑
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            GetWhipProgress(out float progress, out bool isRetreating, out float retreatProgress);

            // 需求1：一、二段攻击在“收回阶段”伤害降低 80%
            if ((ComboIndex == 0 || ComboIndex == 1) && isRetreating)
            {
                modifiers.SourceDamage *= 0.2f;
            }

            // 需求3：第三段刺击尖端造成 2.5 倍高额伤害
            if (ComboIndex == 2 && lastHitSegmentIndex == 29) // 29 为末段索引 (segments - 1)
            {
                modifiers.SourceDamage *= 2.5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            GetWhipProgress(out float progress, out bool isRetreating, out float retreatProgress);

            // 需求1：一、二段攻击在收回阶段无敌帧增加（即降低攻击频率，防止单次挥动多段判定）
            if ((ComboIndex == 0 || ComboIndex == 1) && isRetreating)
            {
                // 将该目标的本地无敌帧设置为极长
                target.localNPCImmunity[Projectile.whoAmI] = 120;
            }
            else
            {
                target.localNPCImmunity[Projectile.whoAmI] = 15; // 正常无敌帧
            }
        }

        // 自定义绘制：拼接 5 帧切片贴图
        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> points = new List<Vector2>();
            GenerateWhipPoints(points);

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, 5); // 分割为 5 帧
            int frameHeight = frame.Height;
            frame.Height -= 2; // 除去接缝处的噪点干扰
            
            Vector2 origin = frame.Size() / 2f;
            
            // 绘制底线（模仿钓鱼线防止缝隙）
            Texture2D lineTex = TextureAssets.FishingLine.Value;
            Rectangle lineFrame = lineTex.Frame();
            Vector2 lineOrigin = new Vector2(lineFrame.Width / 2, 2f);
            Vector2 drawPos = points[0];
            
            Color lineColor = (ComboIndex == 2) ? Color.Red : Color.Goldenrod; // 突刺变红，扫击变黄

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 current = points[i];
                Vector2 diff = points[i + 1] - current;
                float rot = diff.ToRotation() - (float)Math.PI / 2f;
                Vector2 scale = new Vector2(1f, (diff.Length() + 2f) / lineFrame.Height);
                
                Main.EntitySpriteDraw(lineTex, drawPos - Main.screenPosition, lineFrame, Lighting.GetColor(current.ToTileCoordinates(), lineColor), rot, lineOrigin, scale, SpriteEffects.None, 0);
                drawPos += diff;
            }

            // 绘制贴图关节
            Vector2 currentDrawPos = points[0];
            for (int i = 0; i < points.Count - 1; i++)
            {
                bool shouldDraw = false;
                Vector2 cur = points[i];
                Vector2 next = points[i + 1];
                Vector2 diff = next - cur;
                float rot = diff.ToRotation() - (float)Math.PI / 2f;

                // 确定当前节点绘制哪一个 Frame
                if (i == 0) // 玩家手持处
                {
                    frame.Y = 0;
                    origin.Y = frameHeight / 2f - 4f; // 微调手部贴图重心
                    shouldDraw = true;
                }
                else if (i == points.Count - 2) // 鞭子末梢
                {
                    frame.Y = frameHeight * 4;
                    origin.Y = frameHeight / 2f;
                    shouldDraw = true;
                }
                else // 身体关节
                {
                    if (ComboIndex == 2)
                    {
                        // 突刺时，只隔段绘制身体，减轻视觉堆叠感
                        shouldDraw = (i % 3 == 0);
                        frame.Y = frameHeight * (1 + (i / 3) % 3);
                    }
                    else
                    {
                        // 扫击时，全部循环绘制
                        shouldDraw = true;
                        frame.Y = frameHeight * (1 + i % 3); // 在 1, 2, 3 帧中轮流循环
                    }
                }

                if (shouldDraw)
                {
                    Color drawColor = Lighting.GetColor(cur.ToTileCoordinates());
                    Main.EntitySpriteDraw(tex, currentDrawPos - Main.screenPosition, frame, drawColor, rot, origin, 1f, SpriteEffects.None, 0);
                }

                currentDrawPos += diff;
            }

            return false; // 禁用自动绘制
        }
    }
}
```

---

## 4. 鞭子美术贴图制作与配置规范

无论你使用的是原版 AI 还是以上的重写 AI，贴图的格式与制作要求都是完全一致的：

1. **纵向 5 帧网格 (1 x 5)**：
   你必须准备一张高宽比很大的条形图片。将整张图在高度上平分成 5 份：
   * **帧 0 (第一格)**：鞭柄（Handle）。
   * **帧 1 (第二格)**：前段鞭节（Body Segment 1）。
   * **帧 2 (第三格)**：中段鞭节（Body Segment 2）。
   * **帧 3 (第四格)**：后段鞭节（Body Segment 3）。
   * **帧 4 (第五格)**：鞭尖（Tip）。

2. **方向朝向规范**：
   * 所有关节的设计**必须垂直朝下**绘制。
   * 旋转的核心逻辑是取下一个控制点的方向向量并**减去 $90^\circ$ ($\pi / 2$)**。这代表原版绘制系统默认你的关节贴图朝向是“向下（$90^\circ$）”的。如果你的贴图是横向画的，游戏里甩动时关节会发生 90 度的诡异偏折。

3. **接缝处理**：
   * 绘制身体中段贴图（帧 1-3）时，关节首尾的连接处应尽量使用柔和过渡，并在代码中使用 `frame.Height -= 2` 截掉上下各 1 像素的像素接缝，避免因为浮点数缩放出现白线。
