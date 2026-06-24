# 教程：把 Exoblade 刀光移植到 GrandGuardianHoldout

> 前提：你已经熟悉 BaseCustomUseStyleProjectile，并且你的 GrandGuardianHoldout 继承了它。
> 本教程只改 GrandGuardianHoldout.cs，不动任何基类。

---

## 一、先搞清楚 Exoblade 的刀光到底有几层

打开 `ExobladeProj.cs` 的 `PreDraw`，它调了三个方法：

```
DrawSlash()      → 挥动时的弧形刀光（ExobladeSlash shader + PrimitiveRenderer）
DrawPierceTrail()→ 冲刺时的拖尾（和你无关，跳过）
DrawBlade()      → 刀身本体（SwingSprite shader 绘制方形纹理 + 刀尖光晕）
```

你要移植的是：
- **DrawSlash 里的弧形刀光**（最显眼，也是"刀光"的主体）
- **DrawBlade 末尾的刀尖光晕**（那个半星 lensFlare，很简单）
- **DrawBlade 里的 SwingSprite shader**（可选，有限制，见第七节）

---

## 二、变量映射表

在动手之前，先搞清楚 ExobladeProj 用的变量在 GrandGuardianHoldout 里叫什么：

| ExobladeProj 里 | GrandGuardianHoldout 里 | 说明 |
|---|---|---|
| `Progression` | `AnimationProgress / (float)useAnim` | 当前帧在整个动画中的进度 0→1 |
| `Direction` | `Owner.direction` | 左(-1)右(+1) |
| `BaseRotation` | `Projectile.rotation - MathHelper.ToRadians(45f)` | 朝向鼠标的基础角度（rotation 已含 45° 偏移）|
| `SwordDirection` | `(FinalRotation + (FlipAsSword ? MathHelper.PiOver2 : 0f)).ToRotationVector2()` | 当前刀尖的方向单位向量 |
| `BladeLength = 180f` | 自己定，约 `160f`（根据 HitboxOutset=112 + HitboxSize=180 估算）|
| `Owner.MountedCenter` | `Owner.MountedCenter`（完全一样）|
| `SquishFactor / SquishVector` | 不需要，GrandGuardian 不做拉伸，直接用 `Vector2.One` |

---

## 三、添加字段

在 `GrandGuardianHoldout` 类的字段区域（跟 `doSwing`、`postSwing` 那些放一起）加入：

```csharp
// ===== 刀光相关 =====

// 刀尖历史轨迹，最多 40 帧，每帧存一个相对于 Owner.MountedCenter 的偏移量
public Vector2[] bladeTipHistory = new Vector2[40];
public int bladeTipHistoryLength = 0;

// 用于刀光淡出的透明度
public float slashOpacity = 0f;

// 刀尖光晕纹理（和 ExobladeProj 共享同一张贴图）
public static Asset<Texture2D> LensFlare;

// 刀光实际长度（从 Owner.MountedCenter 到刀尖的像素距离，按比例调）
public const float BladeLength = 160f;
```

---

## 四、在 UseStyle() 末尾记录刀尖位置

在 `UseStyle()` 的**最后一行**（`ArmRotationOffset = ...` 之后）插入：

```csharp
// ===== 记录刀尖轨迹，用于弧形刀光 =====
if (CanHit)
{
    slashOpacity = MathHelper.Lerp(slashOpacity, 1f, 0.4f);

    // 计算当前刀尖相对于 Owner.MountedCenter 的偏移
    float visualRot = FinalRotation + (FlipAsSword ? MathHelper.PiOver2 : 0f);
    Vector2 currentTip = visualRot.ToRotationVector2() * BladeLength * Projectile.scale;

    // 把新点插到历史数组头部
    Array.Copy(bladeTipHistory, 0, bladeTipHistory, 1, bladeTipHistory.Length - 1);
    bladeTipHistory[0] = currentTip;
    if (bladeTipHistoryLength < bladeTipHistory.Length)
        bladeTipHistoryLength++;
}
else
{
    // 不在挥动时，逐渐淡出并清空历史
    slashOpacity = MathHelper.Lerp(slashOpacity, 0f, 0.2f);
    if (slashOpacity < 0.01f)
    {
        bladeTipHistoryLength = 0;
    }
}
```

---

## 五、写 DrawSlash() 方法

在 `GrandGuardianHoldout` 类里加一个新方法（放在 `PreDraw` 前面就行）：

```csharp
// ===== 弧形刀光 =====
// 需要 using: CalamityMod.Graphics.Primitives; Terraria.Graphics.Shaders;
private void DrawSlash()
{
    // 历史点不够，或者完全透明，就不画
    if (bladeTipHistoryLength < 2 || slashOpacity <= 0.01f)
        return;

    // --- 配置 ExobladeSlash shader ---
    Main.spriteBatch.EnterShaderRegion();

    var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];

    // 贴图：VoronoiShapes 是 Exoblade 原版用的噪声纹理，可以换别的试试
    slashShader.SetShaderTexture(ModContent.Request<Texture2D>(
        "CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));

    // ★ 在这里改颜色 ★
    // UseColor      → 刀光主色（Exoblade 原版是青色）
    // UseSecondaryColor → 刀光背景色（Exoblade 原版是深紫）
    // fireColor     → 内部"火焰"色（Exoblade 原版是橙红）
    slashShader.UseColor(new Color(80, 160, 255));          // 蓝色
    slashShader.UseSecondaryColor(new Color(60, 0, 100));   // 深紫
    slashShader.Shader.Parameters["fireColor"].SetValue(new Color(150, 80, 255).ToVector3()); // 亮紫

    // flipped：Exoblade 原版是 Direction == 1，这里保持一致
    slashShader.Shader.Parameters["flipped"].SetValue(Owner.direction == 1);
    slashShader.Apply();

    // --- 生成轨迹点列表 ---
    // PrimitiveRenderer 需要的是相对于 centerFunction 返回值的偏移量
    // 我们的 centerFunction 返回 Owner.MountedCenter，
    // 所以 bladeTipHistory[i] 本身已经是相对偏移，直接用
    List<Vector2> points = new List<Vector2>();
    for (int i = 0; i < bladeTipHistoryLength; i++)
        points.Add(bladeTipHistory[i]);

    // --- 宽度函数：头宽尾窄 ---
    float SlashWidthFunction(float completionRatio, Vector2 _)
    {
        // completionRatio: 0 = 最新的点（刀尖）, 1 = 最旧的点（弧尾）
        float fade = Utils.GetLerpValue(1f, 0f, completionRatio, true);
        return Projectile.scale * 55f * fade * slashOpacity;
    }

    // --- 颜色函数 ---
    Color SlashColorFunction(float completionRatio, Vector2 _)
    {
        // 尾部淡出
        float alpha = Utils.GetLerpValue(0.95f, 0.3f, completionRatio, true);
        return Color.White * alpha * slashOpacity;  // shader 会染色，这里保持白色
    }

    // --- 渲染 ---
    PrimitiveRenderer.RenderTrail(
        points,
        new PrimitiveSettings(
            SlashWidthFunction,
            SlashColorFunction,
            (_, _) => Owner.MountedCenter,  // 所有点的世界坐标原点
            shader: slashShader
        ),
        40  // 渲染段数，和历史点数保持一致
    );

    Main.spriteBatch.ExitShaderRegion();
}
```

---

## 六、写 DrawLensFlare() 方法（刀尖光晕）

这是 `DrawBlade()` 末尾那个半星效果，非常简单：

```csharp
// ===== 刀尖光晕 =====
private void DrawLensFlare()
{
    if (slashOpacity <= 0.01f)
        return;

    if (LensFlare == null)
        LensFlare = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar");

    Texture2D shineTex = LensFlare.Value;

    // 当前刀尖世界坐标
    float visualRot = FinalRotation + (FlipAsSword ? MathHelper.PiOver2 : 0f);
    Vector2 bladeTip = Owner.MountedCenter + visualRot.ToRotationVector2() * BladeLength * Projectile.scale;

    // 挥动进度（0→1）
    float progress = useAnim > 0 ? (AnimationProgress / (float)useAnim) : 0f;

    // 淡入淡出
    float lensFlareOpacity = (progress < 0.3f
        ? 0f
        : 0.2f + 0.8f * (float)Math.Sin(MathHelper.Pi * (progress - 0.3f) / 0.7f))
        * 0.6f * slashOpacity;

    // ★ 改这里的颜色 ★（Exoblade 原版是从黄绿到紫红的过渡）
    Color lensColor = Color.Lerp(Color.CornflowerBlue, Color.Violet, (float)Math.Pow(progress, 3));
    lensColor.A = 0; // 加法混合需要 A=0

    Vector2 shineScale = new Vector2(1f, 3f); // 横向窄、纵向长

    Main.EntitySpriteDraw(
        shineTex,
        bladeTip - Main.screenPosition,
        null,
        lensColor * lensFlareOpacity,
        MathHelper.PiOver2,             // 让半星竖着
        shineTex.Size() / 2f,
        shineScale * Projectile.scale,
        SpriteEffects.None,
        0
    );
}
```

---

## 七、修改 PreDraw() 调用上述方法

找到你现有的 `PreDraw()`，在 `return false;` 前插入两行调用：

```csharp
public override bool PreDraw(ref Color lightColor)
{
    if ((useAnim > 0 || DrawUnconditionally) && Owner.ItemAnimationActive)
    {
        // ... 你原有的绘制代码（ghost aura / 主纹理 / glow）保持不动 ...

        // ★ 加在最后，return false 之前 ★
        DrawSlash();      // 弧形刀光（在刀身上层）
        DrawLensFlare();  // 刀尖光晕（在最上层）
    }
    return false;
}
```

**顺序很重要**：先画主纹理，再画刀光，最后画光晕。这样光晕会覆盖在最上面。

---

## 八、添加 using 引用

在文件顶部加入（如果没有的话）：

```csharp
using System.Collections.Generic;              // List<Vector2>
using CalamityMod.Graphics.Primitives;         // PrimitiveRenderer, PrimitiveSettings
using Terraria.Graphics.Shaders;               // GameShaders.Misc
using ReLogic.Content;                         // Asset<Texture2D>（可能已有）
```

---

## 九、可选进阶：SwingSprite 刀身 shader

这是 `DrawBlade()` 里让刀身随弧度"拉伸弯曲"的那层 shader（`Filters.Scene["CalamityMod:SwingSprite"]`）。

**限制**：该 shader 只能用于**方形纹理**（ExobladeProj 的注释明确写了这一点）。GrandGuardian 的纹理是 `138×184`，不是正方形，所以**不能直接把 GrandGuardian 主纹理塞进去**。

**如果你想要这个效果**，有两种方案：

### 方案 A：准备一张专用的方形纹理
1. 制作一张正方形的"GrandGuardian 刀光版"纹理（比如 `GrandGuardianBlade.png`，256×256）
2. 在 `DrawBlade` 中用这张纹理（放在主纹理绘制之前）
3. 代码如下：

```csharp
private void DrawSwingSpriteBlade()
{
    // 计算当前挥动偏移量（SwingAngleShift 的等价物）
    // RotationOffset 就是从"基础朝向鼠标"偏移了多少弧度
    float swingAngleShift = RotationOffset * Owner.direction;
    // 如果左手（FlipAsSword），还要加 90°
    float shaderRotation = swingAngleShift + MathHelper.PiOver4
        + (Owner.direction == -1 ? MathHelper.Pi : 0f);

    var squareTex = ModContent.Request<Texture2D>(
        "你的MOD命名空间/Projectiles/Melee/GrandGuardianBlade" // 方形纹理路径
    ).Value;

    Effect swingFX = Filters.Scene["CalamityMod:SwingSprite"].GetShader().Shader;
    swingFX.Parameters["rotation"].SetValue(shaderRotation);
    swingFX.Parameters["pommelToOriginPercent"].SetValue(0.05f); // 剑柄占纹理比例
    swingFX.Parameters["color"].SetValue(Color.White.ToVector4());

    Main.spriteBatch.End();
    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
        Main.DefaultSamplerState, DepthStencilState.None,
        Main.Rasterizer, swingFX, Main.GameViewMatrix.TransformationMatrix);

    float baseRotation = Projectile.rotation - MathHelper.ToRadians(45f); // 朝向鼠标的基础角度
    SpriteEffects dir = Owner.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

    Main.EntitySpriteDraw(squareTex,
        Owner.MountedCenter - Main.screenPosition,
        null,
        Color.White,
        baseRotation,           // ← 朝向鼠标
        squareTex.Size() / 2f,
        Vector2.One * 3f * Projectile.scale, // ← 无拉伸，用 Vector2.One
        dir,
        0
    );

    // 恢复普通 SpriteBatch
    Main.spriteBatch.End();
    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
        Main.DefaultSamplerState, DepthStencilState.None,
        Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
}
```

**在 PreDraw 里**，在 ghost aura 绘制之前调用这个方法（最底层）：

```csharp
DrawSwingSpriteBlade(); // 最底层：SwingSprite 刀身
// ...ghost aura...
// ...主纹理...
// ...glow...
DrawSlash();
DrawLensFlare();
```

### 方案 B：直接跳过这层，只要弧线刀光
GrandGuardian 已经有自己的主纹理绘制了，视觉上已经很完整。弧形刀光（DrawSlash）+ 刀尖光晕就已经是很明显的视觉升级，不一定要 SwingSprite。

---

## 十、颜色定制速查

| 改这里 | 对应效果 |
|---|---|
| `slashShader.UseColor(...)` | 刀光弧线主色 |
| `slashShader.UseSecondaryColor(...)` | 刀光弧线背景/次色 |
| `slashShader.Shader.Parameters["fireColor"]` | 弧线内部"焰心"色 |
| `lensColor = Color.Lerp(A, B, progress)` | 刀尖光晕颜色（从 A 渐变到 B）|
| `SlashWidthFunction` 里的 `55f` | 刀光宽度（px）|
| `BladeLength = 160f` | 刀光从中心到刀尖的长度，太短的话弧线和刀身对不上 |
| `shineScale = new Vector2(1f, 3f)` | 光晕形状（1:3 是细长星芒）|

---

## 十一、常见问题

**Q：刀光方向反了（往不该去的方向画）**
→ 检查 `slashShader.Shader.Parameters["flipped"]` 的布尔值，试着把 `Owner.direction == 1` 改成 `Owner.direction == -1`

**Q：刀光滞后，和刀身对不上**
→ 在 `UseStyle()` 里，确保 `Array.Copy` 在 `RotationOffset` 更新之后执行（也就是 `ArmRotationOffset = ...` 那行之后）

**Q：刀光太短，弧线不明显**
→ 增大 `bladeTipHistory` 数组长度（从 40 改到 60），或者增大 `BladeLength`

**Q：刀光在不挥动时还残留**
→ 检查 `slashOpacity` 的淡出速率（`0.2f` 这个 lerp 系数），改大一点淡出更快

**Q：编译报错 `PrimitiveRenderer` 找不到**
→ 确认已加 `using CalamityMod.Graphics.Primitives;`

**Q：`GameShaders.Misc["CalamityMod:ExobladeSlash"]` 报空引用**
→ 这个 shader 是 Calamity 注册的，在引用 CalamityMod 的情况下是有的。确认你的 mod 依赖了 CalamityMod。

---

*教程基于 CalamityMod branch 1.4.4 / GrandGuardianHoldout 反编译版*
