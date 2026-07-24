# 暴风雨支配者 (Storm Ruler) 落地龙卷风特效与逻辑分析报告

本报告详细解析了 Calamity Mod 中武器**暴风雨支配者 (Storm Ruler)** 落地释放的持续性龙卷风（Tornado）特效的实现原理。该特效的核心是通过自定义数学算法在 `PreDraw` 中以螺旋形（Helix/Spiral）分段绘制云朵贴图，并通过随高度和时间变化的旋转、缩放与渐变，实现极具立体感与动态感的龙卷风视觉效果。

---

## 一、 整体逻辑调用链路

当暴风雨支配者发射的弹幕落地或击中目标时，其产生龙卷风的调用链如下：
1. **`StormRulerProj.cs`**:
   * 这是武器直接发射的飞刀弹幕。
   * 当其消亡时（在 `OnKill` 中），如果当前是本地玩家（`Projectile.owner == Main.myPlayer`），会触发生成一个过渡弹幕 `StormMark`：
     ```csharp
     Projectile.NewProjectile(..., ModContent.ProjectileType<StormMark>(), ...);
     ```
2. **`StormMark.cs` (风暴印记)**:
   * 这是一个隐形的过渡弹幕（`Texture => "CalamityMod/Projectiles/InvisibleProj"`）。
   * 它的主要作用是产生地面沙尘/风暴聚集的粒子特效（通过 `DustID.Flare_Blue` 粒子），起到聚气提示的作用。
   * 当其生命周期达到第 60 帧（`Projectile.localAI[1] == 60f`）时，会在其中心点正式召唤出持续性龙卷风弹幕 `Tornado`：
     ```csharp
     Projectile.NewProjectile(..., ModContent.ProjectileType<Tornado>(), ...);
     ```
   * 并在第 120 帧时自我销毁。
3. **`Tornado.cs` (龙卷风)**:
   * 真正的龙卷风伤害与特效载体。
   * 弹幕生命周期 `timeLeft = 600`（10秒），最多允许同时存在 3 个（在 `AI` 中会检测并清除多余的老龙卷风）。
   * 该弹幕在 `PreDraw` 中利用螺旋式绘制效果渲染出龙卷风本体。

---

## 二、 龙卷风绘制核心：螺旋式绘制特效实现 (PreDraw)

在 `Tornado.cs` 中，并没有使用复杂的 Shader 或是 3D 渲染，而是完全基于 **2D 贴图的分段重叠旋转与缩放数学公式** 模拟出的 3D 旋转龙卷风。

下面是 `PreDraw` 的详细数学与代码逻辑剖析：

### 1. 垂直高度动态适配与边界检测
龙卷风不会穿过实心方块，它会在当前地面和上方的天花板之间自动拉伸：
```csharp
Point centerPoint = Projectile.Center.ToTileCoordinates();
int sizeModding;
int sizeModding2;
Collision.ExpandVertically(centerPoint.X, centerPoint.Y, out sizeModding, out sizeModding2, 15, 15);
sizeModding++;
sizeModding2--;
```
* **高度检测**：使用 `Collision.ExpandVertically` 从弹幕中心出发，分别向上和向下探测最多 15 个物块（总计最大 30 个物块，即 480 像素）。
* **端点坐标**：
  * `sizeModdingVector`：龙卷风的顶部（天花板）像素坐标。
  * `sizeModdingVector2`：龙卷风的底部（地面）像素坐标。
* **物理高度**：`sizeModdingPos.Y = sizeModdingVector2.Y - sizeModdingVector.Y`。
* **宽度比例**：`sizeModdingPos.X = sizeModdingPos.Y * 0.2f`（横向基准宽度为总高度的 20%）。

### 2. 随时间渐变（Fade In/Out）与呼吸震荡
通过 `trackerClamp` 控制龙卷风生成时的淡入与消亡时的淡出：
```csharp
float aiTracker = Projectile.ai[0];
float trackerClamp = MathHelper.Clamp(aiTracker / 30f, 0f, 1f);
if (aiTracker > 540f)
{
    trackerClamp = MathHelper.Lerp(1f, 0f, (aiTracker - 540f) / 60f);
}
```
* **前 30 帧**：从 0 渐变到 1（平滑淡入）。
* **540 帧到 600 帧**：从 1 渐变到 0（平滑淡出）。

### 3. 基础旋转角与自转相位的计算
```csharp
float aiTrackMult = -0.06283186f * aiTracker;
Vector2 spinningpoint = Vector2.UnitY.RotatedBy((double)(aiTracker * 0.1f), default);
```
* **单个贴图的自转 (`aiTrackMult`)**：每帧旋转约 `-0.0628` 弧度（即 `-2 * pi / 100`），使得每一个绘制上去的云朵贴图自身在进行顺时针/逆时针的持续自转，模拟风中的云气翻滚。
* **螺旋整体自转基础相位 (`spinningpoint`)**：一个模长为 1 且随时间以速度 `0.1` 弧度/帧旋转的二维向量。它是所有螺旋分段的旋转基准，决定了龙卷风整体的旋转律动。

### 4. 螺旋堆叠循环与漏斗状拉伸 (重点)
渲染的核心是一个从底部（地面 `sizeModdingVector2.Y`）逐步递增到顶部（天花板 `sizeModdingVector.Y`）的 `for` 循环：
```csharp
float increment = 5.1f;
for (float j = (float)(int)sizeModdingVector2.Y; j > (float)(int)sizeModdingVector.Y; j -= increment)
{
    ...
}
```
每次循环绘制一个云朵贴图，步长为 `5.1` 像素。在循环内部，通过精妙的数学计算调整每一个贴图的**横向偏移（螺旋）**、**缩放大小**和**透明度**：

#### (1) 计算螺旋相位差与角度 (`incStorageMult`)
```csharp
incrementStorage += increment; // 当前分段距离底部的垂直高度 (像素)
float colorChanger = incrementStorage / sizeModdingPos.Y; // 高度百分比 (0.0 到 1.0)
float incStorageMult = incrementStorage * 6.28318548f / -20f;
```
* `incStorageMult` 代表由于高度差带来的相位旋转角。
* 这里的数学含义是：**每往上延伸 20 个像素，螺旋线就额外旋转 \(2\pi\) 弧度（即 360 度）**。这使得龙卷风在垂直方向上呈现极其紧密的螺线缠绕结构。

#### (2) 模拟 3D 旋转与漏斗状外扩（Funnel Effect）
```csharp
Vector2 spinArea = spinningpoint.RotatedBy((double)incStorageMult, default);
Vector2 colorChangeVector = new Vector2(0f, colorChanger + 1f);
colorChangeVector.X = colorChangeVector.Y * vectorMult; // vectorMult = 0.2f
```
* **螺旋偏移向量 (`spinArea`)**：将旋转基准向量 `spinningpoint` 进一步旋转 `incStorageMult`，从而获得该垂直高度分段的 3D 旋转偏移方向。
* **漏斗状渐宽 (`colorChangeVector`)**：
  * 该向量控制了螺旋的半径。
  * `colorChangeVector.Y` 为 `colorChanger + 1f`（底部为 1.0，顶部为 2.0）。
  * `colorChangeVector.X` 为 `(colorChanger + 1f) * 0.2`（底部为 0.2，顶部为 0.4）。
  * 随着高度百分比 `colorChanger` 从 0 增长到 1，螺旋的横向宽度比例也随之翻倍。这非常完美地模拟了龙卷风**下窄上宽的漏斗型/锥形外观**。

#### (3) 三维至二维的投影计算（反汇编 Bug 分析）
在将螺旋计算出的偏移量应用 to 2D 绘制坐标时，代码中存在如下处理：
```csharp
spinArea *= colorChangeVector * 100f; // 缩放螺旋半径
spinArea.Y = 0f;
spinArea.X = 0f; // <--- 反汇编残留或写法失误
spinArea += new Vector2(sizeModdingVector2.X, j) - Main.screenPosition;
```
* **正常投影逻辑（如原版猪鲨龙卷）**：
  * 因为垂直坐标由循环变量 `j` 严格控制，所以 3D 螺旋的 Y 轴旋转偏移必须被投影掉（设为 0）：`spinArea.Y = 0f;`。
  * 仅保留 X 轴上的左右摆动（横向偏移 `spinArea.X`），从而在 2D 屏幕上完美模拟 3D 螺旋线的左右摆动视觉效果。
* **Calamity 源码中的特殊残留**：
  * 代码中额外包含了一句 `spinArea.X = 0f;`。这导致原本计算出的横向螺旋偏移量也被归零了，导致所有云朵贴图在物理位置上被强制对齐在同一条垂直中心线上（没有横向的波浪状摇摆）。
  * **为什么依然看起来像龙卷风？**
    1. **贴图自转**：绘制时传入的角度是 `aiTrackMult + incStorageMult`，不同高度的云朵贴图初始角度不同且都在高速旋转。
    2. **贴图缩放**：越往上贴图越大（`1f + lowerColorChanger`），并且有透明度过渡。
    3. **视觉错觉**：由于贴图巨大且重叠，即使位置处于一条直线上，通过旋转与缩放的差异，人眼也会自动脑补出旋转的立体风暴感。
    * *注：若去除 `spinArea.X = 0f;`，龙卷风的云朵会在左右方向上像蛇一样呈螺旋状扭摆，龙卷风的体积和立体感会更加夸装。*

#### (4) 渐变透明度（Lerp）与边缘软化
为了防止龙卷风的顶部和底部生硬地切断，使用了双向渐变透明度：
```csharp
Color newCloudColor = Microsoft.Xna.Framework.Color.Lerp(Microsoft.Xna.Framework.Color.Transparent, cloudColor, colorChanger * 2f);
if (colorChanger > 0.5f)
{
    newCloudColor = Microsoft.Xna.Framework.Color.Lerp(Microsoft.Xna.Framework.Color.Transparent, cloudColor, 2f - colorChanger * 2f);
}
newCloudColor.A = (byte)((float)newCloudColor.A * 0.5f);
newCloudColor *= trackerClamp;
```
* 当 `colorChanger` 在 `0.0 ~ 0.5`（下半段）时，从全透明渐变到最亮。
* 当 `colorChanger` 在 `0.5 ~ 1.0`（上半段）时，从最亮又平滑渐变回全透明。
* 这样可以使龙卷风在**最底部（贴近地面）**和**最顶部（贴近天花板）**都呈现自然消散的半透明雾状效果，极其柔和。

#### (5) 最终绘制
```csharp
Main.spriteBatch.Draw(
    texture2D23, 
    spinArea, 
    new Microsoft.Xna.Framework.Rectangle?(drawRectangle), 
    newCloudColor, 
    aiTrackMult + incStorageMult, // 旋转：自转 + 高度导致的初始相位偏移
    smallRect, 
    1f + lowerColorChanger, // 缩放：底部缩放为 0.85，顶部缩放为 1.85，呈现上宽下窄的漏斗状
    SpriteEffects.None, 
    0
);
```

---

## 三、 特效参数提取对照表

为了方便在本模组中复刻或微调该特效，以下整理了关键的控制参数：

| 参数名称 | 默认值 | 作用说明 |
| :--- | :--- | :--- |
| `increment` | `5.1f` | 垂直堆叠绘制的步长（单位像素）。值越小，云朵越密集遮挡越强；值越大，性能越好但会有断层。 |
| `vectorMult` | `0.2f` | 龙卷风的宽高比系数，用于决定螺旋的横向外扩幅度。 |
| `incStorageMult` 缩放分母 | `-20f` | 控制螺旋的缠绕紧密程度。分母越小（如 -20f），旋转缠绕越紧；分母越大，则螺线越舒展。 |
| `aiTrackMult` 系数 | `-0.06283186f` | 云朵贴图自转的速度。 |
| 顶部/底部最大探测格数 | `15` | 龙卷风最大能延伸的格数（天花板到地面的探测范围，15格 = 240像素）。 |
| 整体淡入淡出时间 | `30` / `60` 帧 | 龙卷风出生与消亡时的透明度缓冲时间。 |

---

## 四、 本模组复刻与改进建议

如果在 `CalamityLegendsComeBack` 中想重做或者复刻这个螺旋龙卷风特效，建议进行如下优化：

1. **修正反汇编残留（让龙卷风真正地“扭动”起来）**：
   * 在绘制时，**不要**将 `spinArea.X` 设为 `0f`。
   * 保留 `spinArea.X`，并将其乘上合适的外扩系数。这会使龙卷风不仅有旋转贴图，而且其**路径本体也会呈现 3D 螺旋线投影在 2D 上的左右摆动（S形扭动）**，视觉表现力会提升一个档次。
2. **多层混合渲染**：
   * 可以通过使用两层循环或不同的自转速度/方向绘制双层螺旋。例如，内层使用较小、较快、较暗的云朵逆时针自转，外层使用较大、较慢、较亮的云朵顺时针自转，可以极大地增强风暴的层次感和混乱感。
3. **增加粒子喷射**：
   * 在 `AI` 的每一帧，可以在 `sizeModdingVector2`（地面）和 `sizeModdingVector`（天花板）的螺旋边缘，朝切线方向喷射一些细小的风暴粒子（Dust），配合绘制的螺旋线，会使龙卷风显得更加写实和危险。
