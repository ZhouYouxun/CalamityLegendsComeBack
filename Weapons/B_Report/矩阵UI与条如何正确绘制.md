# 矩阵 UI 与条如何正确绘制

## 1. 本文目的

本文是一份面向 `tModLoader` 开发者的矩阵 UI、玩家头顶状态条、充能条、冷却条和护盾框绘制规范。

它解决的不是“怎样随便画出一条进度条”，而是下面几个在实际开发中反复出现的问题：

- 为什么某些矩阵条会伸出边界，形成很长的横线、竖线或杂乱符号。
- 为什么一套在大型护盾上很好看的结构，缩成头顶短条后会变成完全错误的形状。
- 为什么限制线段起点和终点，仍然不能保证最终像素位于边界内。
- 为什么有些仓库内的条从未出错，而 BB 与 BF 的旧矩阵条会反复出错。
- 怎样让一个条既不夸张，又不是毫无设计感的普通长方形。
- 怎样写出以后可以安全复用、不会再次产生越界绘制的公共实现。

本文所说的“边界”是一个明确的屏幕空间矩形。任何背景、描边、节点、扫光、分段填充和命中闪光，都必须满足：

> 最终提交给 `SpriteBatch` 或 `DrawDataCache` 的每一个像素，都位于这个矩形内部。

仅仅让计算公式看起来位于边界附近，不算完成边界约束。

---

## 2. 本次错误发生在哪里

本次问题同时出现在两个短条中：

- `ModSources\CalamityLegendsComeBack\Weapons\BrinyBaron\BrinyBaronRightClickDashCooldownBarLayer.cs`
- `ModSources\CalamityLegendsComeBack\Weapons\BlossomFlux\RightClick\BRecov\BFRecoveryShieldVisual.cs`

二者都曾使用过类似下面的结构：

1. 先确定一个约 `56～60` 像素宽、`12` 像素高的头顶矩形。
2. 沿矩形四边绘制移动虚线。
3. 在四个角绘制 L 形支架。
4. 在四个角绘制十字节点。
5. 在上边缘再绘制移动十字节点。
6. 在内部绘制分段进度。

单独看每一项似乎都能表达“矩阵科技感”，但把它们全部压进只有 `12` 像素高的空间后，结构必然发生碰撞。

BF 旧实现中的典型比例是：

```csharp
const float halfW = 28f;
const float halfH = 6f;

float bSize = 6f + hit * 2f;
float nSize = 4.5f + hit * 2.5f;
```

整个条的高度只有：

```text
2 × halfH = 12 像素
```

而角支架的竖臂在未受击时就长 `6` 像素：

```text
上方支架：从 y = -6 画到 y = 0
下方支架：从 y = +6 画到 y = 0
```

它们在条的正中间准确相接。

受击时 `bSize` 可以增长到 `8`：

```text
上方支架：从 y = -6 画到 y = +2
下方支架：从 y = +6 画到 y = -2
```

此时两条竖臂在 `-2～+2` 区间直接重叠。再叠加角落十字节点、移动十字节点和横向虚线，最终轮廓自然会拼成非常糟糕的符号。

这不是随机 Bug，也不是显卡偶发问题，而是确定的几何结果。

---

## 3. 真正的根因

### 3.1 把大型护盾模板误当成短条模板

优秀的大型矩阵护盾范例位于：

`ModSources\CalamityLegendsComeBack\Accssory\SHPC\Skill\Barrier\BarrierShieldVisual.cs`

其主体尺寸约为：

```csharp
const float halfW = 34f;
const float halfH = 52f;
```

即：

```text
宽度 68 像素
高度 104 像素
```

它的角支架基础长度约为 `14` 像素。相对于 `104` 像素的总高度，顶部与底部支架之间存在大量留白，不会互相拼接。

大型护盾的视觉结构是：

- 四边代表护盾范围。
- 四角支架代表固定节点。
- 中间扫描线代表护盾内部活动。
- 四角闪光代表受击反馈。

这些元素围绕玩家身体展开，空间足够，因此层级清楚。

短条只有十几像素高。它的视觉结构应该是：

- 一个清楚的有限外轮廓。
- 内部进度或分段。
- 极少量、严格位于内部的动态反馈。

把完整护盾的四边、四角、十字节点和扫描系统原样压缩到短条，是错误的设计迁移。

正确原则是：

> 学习大型护盾的“明确边界、层级清楚、装饰服从主体”，不能把大型护盾的每一种装饰原封不动塞进短条。

### 3.2 限制线段端点，不等于裁切最终像素

旧实现会这样限制虚线：

```csharp
float s = Math.Max(0f, pos);
float e = Math.Min(totalLen, pos + segLen);
DrawLineSegment(a + dir * s, a + dir * e, color, width);
```

这只能保证线段的中心轴起点和终点位于 `a～b` 之间，不能保证线条实际覆盖范围位于边界内。

假设线条宽度为 `3`：

- 中心轴位于上边界。
- 线条会向中心轴上下各扩展约 `1.5` 像素。
- 上半部分自然会伸出矩形。

同理，一个放在角点上的十字节点，以角点为中心绘制：

```csharp
DrawNode(tl, color, 5f);
```

节点会向左、右、上、下同时扩展。左上角节点必然有一部分位于外框左侧和上方。

所以：

> “端点在边界内”是几何约束；“最终像素在边界内”是光栅化约束。两者不是一回事。

### 3.3 `DrawDataCache` 不会替开发者自动裁切

`PlayerDrawLayer` 中常见：

```csharp
drawInfo.DrawDataCache.Add(new DrawData(...));
```

`DrawDataCache` 只是把绘制命令加入玩家绘制队列。它不会知道开发者心中的条形边界，也不会自动把线条、节点或贴图限制在某个矩形内。

只要提交的 `DrawData` 超出预期区域，它就会照常显示。

因此以下想法是错误的：

> “这是 PlayerDrawLayer，原版应该会帮我限制在玩家附近。”

不会。玩家绘制层决定顺序，不决定裁切区域。

### 3.4 旋转与缩放会扩大实际覆盖范围

这种写法很常见：

```csharp
float rotation = (end - start).ToRotation();
Vector2 scale = new(distance, width);
spriteBatch.Draw(pixel, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
```

问题包括：

- `scale.X` 决定长度。
- `scale.Y` 决定厚度。
- `origin` 决定厚度向哪一侧扩展。
- 一旦旋转，最终包围盒不再等于起点和终点形成的普通矩形。
- 没有 Scissor 或手动裁切时，SpriteBatch 不会限制旋转后的像素。

短条没有必要使用自由旋转线段。水平、垂直和分段结构完全可以用有限目标矩形完成。

### 3.5 动态放大让静态勉强可用的结构彻底越界

BF 旧实现会根据受击状态扩大：

```csharp
edgeWidth = baseWidth + hit * extraWidth;
bracketSize = baseSize + hit * extraSize;
nodeSize = baseSize + hit * extraSize;
```

如果基础结构已经紧贴边界，任何受击放大都会向边界外扩张。

正确做法不是禁止受击反馈，而是：

- 不扩大几何范围。
- 在固定范围内部提高亮度。
- 在固定范围内部改变颜色。
- 在固定范围内部产生短促扫光。

也就是说，命中反馈应改变“亮度与色彩”，而不是改变“占地范围”。

### 3.6 为什么之前反复修改仍然不彻底

之前的处理主要修改了外观：

- 删除一部分节点。
- 换成普通条贴图。
- 调整线段长度。
- 调整角支架尺寸。

这些做法可以让某一帧看起来好一些，但没有建立一个所有绘制命令都必须遵守的最终边界。

真正缺少的是一条不可绕过的程序约束：

```csharp
Rectangle clipped = Rectangle.Intersect(outerBounds, requestedRectangle);
```

只要所有输出都必须经过这一层，才算真正解决问题。

---

## 4. 仓库内为什么有些条从不出错

本次审计覆盖了仓库内与以下关键字有关的活动 C# 文件：

- `PlayerDrawLayer`
- `GenericBarBack`
- `GenericBarFront`
- `DrawCornerBracket`
- `DrawDashedEdge`
- `DrawNode`
- `MagicPixel`
- 各类 `StatusBar`、`ChargeBar`、`CooldownBar`

共筛出 22 个重点候选文件。其中 15 个常规条使用 `GenericBarBack` 与 `GenericBarFront`。

### 4.1 固定贴图 + 源矩形裁剪

典型范例：

- `ModSources\CalamityLegendsComeBack\Weapons\A_Dev\SHPBow\SHPBowChargeBarLayer.cs`
- `ModSources\CalamityLegendsComeBack\Weapons\A_Dev\DesertEagle\DesertEagleChargeBarLayer.cs`
- `ModSources\CalamityLegendsComeBack\Weapons\BlossomFlux\RightUI\BFRightHoldChargeBarLayer.cs`
- `ModSources\CalamityLegendsComeBack\Weapons\A_Upgrade\P90\P90CooldownBar.cs`
- `ModSources\CalamityLegendsComeBack\Weapons\LeonidProgenitor\EXSkill\LeonidUltimateUI.cs`

核心方式是：

```csharp
Texture2D background = ModContent.Request<Texture2D>(
    "CalamityMod/UI/MiscTextures/GenericBarBack").Value;
Texture2D foreground = ModContent.Request<Texture2D>(
    "CalamityMod/UI/MiscTextures/GenericBarFront").Value;

float progress = MathHelper.Clamp(value, 0f, 1f);
Rectangle crop = new(
    0,
    0,
    (int)(foreground.Width * progress),
    foreground.Height);
```

这种方式安全，是因为：

- 背景贴图尺寸固定。
- 前景使用源矩形裁剪。
- `progress` 被限制在 `0～1`。
- 没有从四角向外生长的节点。
- 没有任意长度的自由线段。

它的缺点是造型比较通用。如果需要更强的武器个性，可以在贴图范围内部改变颜色、增加内部扫光，但不要在外部叠加四角十字。

### 4.2 大型 SHPC 屏障

`BarrierShieldVisual.cs` 是大型矩阵框的优秀范例，主要优点是：

- 尺寸与装饰比例匹配。
- 四角支架之间存在充足距离。
- 扫描线端点明确。
- 玩家身体位于框内，框本身承担“护盾范围”的语义。
- 命中闪光围绕明确角点发生，不会把整个结构压成一团。

但它是大型盾牌范例，不是短条代码模板。

短条可以学习它的设计原则：

- 主体边界优先。
- 动态装饰服从主体。
- 不让装饰抢走结构辨识度。

短条不能直接照搬：

- 四角 L 支架。
- 四角十字节点。
- 上下左右四边同时运动。
- 大范围扫描线。

### 4.3 大型 Boss 面板

`ModSources\CalamityLegendsComeBack\UI\CLCBBossHealthBar.cs` 的面板宽约 `460`、高约 `56`，角支架长度约 `9`。

角支架只占总高度的一小部分，而且带有内缩量，因此上下支架不会连接。这里的 L 形装饰是安全的，因为面板确实有足够空间。

再次强调：

> L 形装饰不是绝对禁止；把 L 形装饰塞进高度不足的短条才是错误。

---

## 5. 当前采用的正确修复

公共实现位于：

`ModSources\CalamityLegendsComeBack\UI\BoundedHeadBarRenderer.cs`

BB 与 BF 都调用这一份代码：

- BB：`BrinyBaronRightClickDashCooldownBarLayer.cs`
- BF：`BFRecoveryShieldVisual.cs`

### 5.1 固定外框

```csharp
public const int OuterWidth = 64;
public const int OuterHeight = 14;
```

所有绘制均围绕同一个 `64 × 14` 的屏幕空间矩形进行。

### 5.2 阶梯削角，而不是完美长方形

外轮廓由五组互不重叠的目标矩形组成：

```text
第 1 层：左右各缩进 4 像素
第 2 层：左右各缩进 2 像素
中间层：完整宽度
第 4 层：左右各缩进 2 像素
第 5 层：左右各缩进 4 像素
```

视觉上形成轻微削角：

```text
    ┌────────────────────────┐
  ┌─┘                        └─┐
┌─┘                            └─┐
  └─┐                        ┌─┘
    └────────────────────────┘
```

实际实现是像素阶梯，不使用斜线，也不需要旋转。

它满足两个要求：

- 不是完全单调的普通长方形。
- 没有任何向外伸出的支架、十字或长线。

### 5.3 七段内部填充

进度位于外框内部，并拆分为七段。

七段而不是连续纯色，可以保留矩阵/能量槽的机械感；又不会像四角节点那样干扰外轮廓。

所有段都以 `fillBounds` 为上限：

```csharp
Rectangle fillBounds = new(
    bounds.X + 5,
    bounds.Y + 5,
    bounds.Width - 10,
    bounds.Height - 10);
```

进度必须先限制：

```csharp
progress = MathHelper.Clamp(progress, 0f, 1f);
```

### 5.4 扫光只能出现在已填充区域

扫光不是在整个外边缘绕行，而是在内部填充区移动。

并且只有当扫光坐标小于当前已填充终点时才绘制：

```csharp
if (sweepX < filledRight)
{
    // Draw the one-pixel glint.
}
```

这让低进度状态不会出现一条脱离进度的亮线。

### 5.5 最终强制裁切

所有矩形在提交前必须经过：

```csharp
Rectangle clipped = Rectangle.Intersect(bounds, rectangle);
```

然后检查：

```csharp
if (clipped.Width <= 0 || clipped.Height <= 0)
    return;
```

只有 `clipped` 可以被提交：

```csharp
drawDataCache.Add(new DrawData(pixel, clipped, color));
```

或者：

```csharp
spriteBatch.Draw(pixel, clipped, color);
```

完整安全链路是：

```text
状态值
  ↓
Clamp 到 0～1
  ↓
建立唯一 outerBounds
  ↓
生成背景、填充、扫光的 requestedRectangle
  ↓
Rectangle.Intersect(outerBounds, requestedRectangle)
  ↓
只提交 clippedRectangle
```

只要任何新装饰绕过 `Rectangle.Intersect`，就不应合入这套短条。

---

## 6. 两种正确调用方式

### 6.1 在 `PlayerDrawLayer` 中绘制

适合玩家头顶充能条、冷却条。

```csharp
using CalamityLegendsComeBack.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

internal sealed class ExampleCooldownBarLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() =>
        new AfterParent(PlayerDrawLayers.Head);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return true; // Replace with the real visibility condition.
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        float progress = MathHelper.Clamp(GetProgress(player), 0f, 1f);

        Vector2 center =
            player.Center -
            Main.screenPosition +
            new Vector2(0f, player.gfxOffY - 58f);

        BoundedHeadBarRenderer.AddToPlayerDrawCache(
            drawInfo.DrawDataCache,
            center,
            progress,
            new Color(5, 22, 42, 224),
            new Color(48, 155, 224),
            new Color(192, 246, 255),
            0.92f,
            0f,
            Main.GlobalTimeWrappedHourly + player.whoAmI * 0.17f);
    }
}
```

注意：

- 传入的是条的中心，不是左上角。
- 坐标已经减去 `Main.screenPosition`。
- `gfxOffY` 必须纳入垂直位置，避免玩家上下浮动时条与身体错位。
- 不要在调用后额外绘制角落十字。

### 6.2 在 `ModProjectile.PreDraw` 中绘制

适合由隐形弹幕维护的护盾值、临时生命值条。

```csharp
public override bool PreDraw(ref Color lightColor)
{
    Player owner = Main.player[Projectile.owner];
    float progress = MathHelper.Clamp(GetShieldRatio(owner), 0f, 1f);
    float hitFlash = MathHelper.Clamp(GetHitFlash(owner), 0f, 1f);

    Vector2 center =
        owner.Center +
        new Vector2(0f, owner.gfxOffY - 48f) -
        Main.screenPosition;

    BoundedHeadBarRenderer.DrawImmediate(
        Main.spriteBatch,
        center,
        progress,
        new Color(10, 33, 19, 224),
        new Color(60, 220, 120),
        new Color(190, 255, 215),
        0.9f,
        hitFlash,
        Main.GlobalTimeWrappedHourly);

    return false;
}
```

注意：

- 简单矩形短条不应为了“发光”随意 `End()` / `Begin()` 切换 SpriteBatch。
- 受击反馈通过 `hitFlash` 在边界内部混白，不扩大条的尺寸。
- 如果维护弹幕本身使用隐形贴图，应返回 `false`，避免额外绘制无意义本体。

---

## 7. 如果不用公共绘制器，至少遵守这三种安全方案

### 方案 A：固定贴图 + 源矩形裁剪

适合普通、可靠、开发成本低的条。

必须做到：

- `progress` Clamp 到 `0～1`。
- 裁剪前景源矩形。
- 背景与前景使用相同缩放。
- 不在外侧追加自由线条。

### 方案 B：有限目标矩形 + 最终求交

适合自定义矩阵条。

必须做到：

- 先建立唯一 `outerBounds`。
- 所有元素使用 `Rectangle` 表达。
- 每个元素与 `outerBounds` 求交。
- 只绘制求交后的矩形。

这是当前 BB/BF 使用的方案。

### 方案 C：Scissor 裁切

只有在必须绘制旋转贴图、Shader、复杂曲线或无法拆成目标矩形的内容时，才考虑 Scissor。

必须做到：

- 建立 `RasterizerState`，启用 `ScissorTestEnable`。
- 设置 `GraphicsDevice.ScissorRectangle`。
- 明确当前坐标属于屏幕空间还是世界空间。
- 保存并恢复原始 ScissorRectangle。
- 正确恢复 SpriteBatch 的混合、采样、深度和矩阵状态。

Scissor 的状态管理成本较高。普通短条优先使用方案 A 或 B。

---

## 8. 明确禁止的短条写法

### 8.1 禁止在四个角同时放置十字节点

错误：

```csharp
DrawNode(topLeft, color, size);
DrawNode(topRight, color, size);
DrawNode(bottomLeft, color, size);
DrawNode(bottomRight, color, size);
```

原因：

- 十字以角点为中心，天然向外越界。
- 条的高度不足时，上下节点会连在一起。
- 四个十字加四个 L 支架极易形成错误符号。

### 8.2 禁止让上下角支架长度达到半高

危险条件：

```text
bracketLength >= halfHeight
```

一旦相等，上下支架会在中心相接；一旦大于，就会重叠。

大型面板建议：

```text
bracketLength <= totalHeight × 0.25
```

高度不超过 `18` 像素的短条，建议完全不用上下 L 支架。

### 8.3 禁止把线宽中心放在外边界上

错误：

```csharp
DrawLine(topLeft, topRight, width: 3f);
```

如果线条以边界为中心，至少一半厚度在外面。

如果必须绘制描边，应当：

- 将中心线向内部偏移 `width / 2`；或
- 使用最终矩形裁切；或
- 使用固定边框贴图。

### 8.4 禁止让命中反馈扩大外轮廓

错误：

```csharp
size = baseSize + hitFlash * 4f;
```

正确：

```csharp
color = Color.Lerp(baseColor, Color.White, hitFlash);
opacity = MathHelper.Clamp(baseOpacity + hitFlash * 0.2f, 0f, 1f);
```

### 8.5 禁止用“看起来应该没越界”代替裁切

只要使用了下面任意一种元素，就必须检查最终包围范围：

- 旋转线条。
- 以边界点为中心的节点。
- Bloom。
- 软边贴图。
- Shader。
- 大于 1 像素的描边。
- 根据命中或蓄力动态放大的贴图。

---

## 9. 视觉设计规范：不能太夸张，也不能太单调

一个优秀的短条应当先回答“玩家能不能瞬间看懂”，再回答“它够不够华丽”。

推荐层级：

1. **轮廓**：固定、有轻微削角、永不改变尺寸。
2. **底槽**：低亮度深色，明确尚未填充区域。
3. **进度**：连续裁剪或有限分段。
4. **颜色**：与武器身份一致。
5. **轻动态**：仅在内部发生的扫光、颜色脉冲或命中变白。

不推荐同时使用：

- 四边移动虚线。
- 四角支架。
- 四角十字。
- 中点十字。
- 外围 Bloom。
- 外围游走节点。
- 横纵双扫描线。

短条面积很小，同时叠加这些内容不会显得高级，只会让轮廓失去辨识度。

### 推荐的个性化方式

BB：

- 深海蓝底槽。
- 蓝到冰白的分段填充。
- 内部短扫光。

BF：

- 深绿底槽。
- 叶绿到薄荷白的分段填充。
- 受击时内部短暂变白。

其他武器可以换颜色和段数，但不要改变边界规则。

---

## 10. 开发时必须检查的数值

### 10.1 进度

必须测试：

```text
0
0.001
0.01
0.5
0.99
1
大于 1
小于 0
NaN 的来源是否可能存在
```

代码必须保证最终使用值位于 `0～1`。

### 10.2 尺寸

必须检查：

- 目标矩形宽高是否可能为 `0` 或负数。
- 分段间隙是否可能大于单段宽度。
- 动态值是否会改变外框尺寸。
- 线宽是否会向边界外扩展。
- 节点是否以边界点为中心。

### 10.3 坐标空间

玩家头顶世界绘制通常需要：

```csharp
worldPosition - Main.screenPosition
```

`PlayerDrawLayer` 还应考虑：

```csharp
player.gfxOffY
```

不要混用：

- 世界坐标。
- 屏幕坐标。
- UI 坐标。
- 已经减过一次 `Main.screenPosition` 的坐标。

重复减去屏幕位置会让条飞离玩家；完全不减会让条出现在世界绝对坐标位置。

### 10.4 SpriteBatch 状态

必须确认当前绘制处于：

- 正确的 `BlendState`。
- 正确的 `SamplerState`。
- 正确的 TransformationMatrix。
- 正确的绘制层。

如果手动调用 `End()` / `Begin()`，必须完整恢复原状态。简单状态条应尽量避免切换。

---

## 11. 运行时测试清单

每一个新矩阵条至少要测试以下场景：

- 玩家站立。
- 玩家跳跃。
- 玩家坐骑状态。
- 玩家受击产生 `gfxOffY` 或屏幕震动。
- 进度为空。
- 进度刚开始增长。
- 进度一半。
- 进度已满。
- 进度快速下降。
- 命中闪光最大值。
- UI 缩放不是 100%。
- 不同分辨率。
- 游戏缩放变化。
- 多人模式下观察其他玩家。
- 玩家死亡或传送时条是否及时消失。
- 条处于屏幕左右边缘时是否出现异常长线。

重点截图帧：

```text
0%
1%
50%
99%
100%
最大命中闪光
```

只看满进度状态是不够的。很多错误只会在极低进度、移动节点经过角落或受击放大时出现。

---

## 12. 代码审查清单

提交新的头顶条或矩阵 UI 前，逐项回答：

- [ ] 是否存在一个唯一、明确的 `outerBounds`？
- [ ] `progress` 是否 Clamp 到 `0～1`？
- [ ] 是否有任何元素以边界点为中心向外扩展？
- [ ] 是否使用了旋转线段？
- [ ] 是否使用了四角十字节点？
- [ ] 上下角支架是否可能相接？
- [ ] 命中反馈是否会扩大几何尺寸？
- [ ] 所有自定义矩形是否经过 `Rectangle.Intersect`？
- [ ] 所有宽高是否大于 `0` 后才绘制？
- [ ] 世界坐标和屏幕坐标是否只转换一次？
- [ ] `gfxOffY` 是否正确处理？
- [ ] 是否无必要地切换 SpriteBatch？
- [ ] 是否测试了 0%、1%、50%、99%、100%？
- [ ] 是否测试了最大受击闪光？
- [ ] 是否确认轮廓既不是普通死板矩形，也没有过度装饰？

任何一项无法确定，都不应认为绘制已经完成。

---

## 13. 最终结论

本次 BB 与 BF 的问题由三个因素共同造成：

1. 把适用于大型 SHPC 护盾的矩阵框结构压缩成极矮短条。
2. 上下 L 支架、四角十字和边缘虚线在极小高度中互相连接。
3. 只限制了线段端点，没有对最终绘制像素执行真正裁切。

正确修复不是继续微调支架长度，而是更换短条的构造方式：

- 使用固定外框。
- 使用阶梯削角而非外围支架。
- 使用内部七段填充保留矩阵感。
- 使用内部扫光和颜色变化表现动态。
- 所有输出经过 `Rectangle.Intersect`。
- BB 与 BF 共用同一个绘制器，避免两份代码再次漂移。

以后遇到任何玩家头顶矩阵条，优先复用：

`ModSources\CalamityLegendsComeBack\UI\BoundedHeadBarRenderer.cs`

如果需求只是普通进度条，使用固定 `GenericBarBack` / `GenericBarFront` 贴图裁剪。

如果需求是大型护盾或大型面板，可以参考：

- `ModSources\CalamityLegendsComeBack\Accssory\SHPC\Skill\Barrier\BarrierShieldVisual.cs`
- `ModSources\CalamityLegendsComeBack\UI\CLCBBossHealthBar.cs`

但必须重新核对装饰长度与整体尺寸的比例，不能把大型结构原样压缩。

最重要的规则只有一句：

> 任何矩阵 UI 的装饰都必须服从主体边界；没有最终像素裁切的“边界”，不是真正的边界。
