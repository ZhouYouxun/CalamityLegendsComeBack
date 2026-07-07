# 图标/Logo 闪烁效果 —— 技术报告(当前状态:全部禁用)

一句话总结:**这个技术我们已经搞懂了、也写出来能编译通过的代码了,但现在全部关掉,不影响正常游戏。想要的时候随时能打开。**

## 这次搞明白了什么

起因是想复刻 `StarsAbove` 模组的一个效果:玩家还没进游戏、甚至没选角色,在 tModLoader 的 **Mods 选择列表**里,`StarsAbove` 那个模组的封面(不是主菜单 Logo,是列表里的缩略图)会自己播放动画。

参考文件:
- `ModSources/StarsAbove/Systems/StarsAboveModSystem.cs` —— 关键就在这
- `ModSources/StarsAbove/Menu/StarsAboveMainMenu.cs` —— 另一个效果(主菜单 Logo 闪烁),别搞混了

结论:tModLoader **没有**给 modder 开放"Mods 列表缩略图"的官方钩子。StarsAbove 是用 **MonoMod 的 `ILHook`**,在运行时直接改写 tModLoader 自己内部的 `Terraria.ModLoader.UI.UIModItem.OnInitialize` 方法的 IL 代码:在这个方法把静态 `icon.png` 存进私有字段 `_modIcon` 的那一刻,插入一段判断——如果当前这一行正在处理的模组名是 `"StarsAbove"`,就把要存的图标换成他们自己写的 `UIAnimatedImageAlwaysHovering`(会自动播放帧动画的 `UIElement`),读取的是他们的 `Menu/AnimatedIcon.png`(160×2480 的帧序列图,32 帧,每帧 80×80)。

**核心视觉技巧**(不管用在哪个场景都是这套):同一张光斑贴图 `ShineFX.png` 画两遍,转速不同。两份贴图的角度对齐的瞬间,叠加出来的亮度会明显更亮——这个"对齐时更亮"的节拍感,才是看起来像"闪烁/闪光"的真正原因,不是靠写死的亮度曲线控制的。

## 这个文件夹里有什么

| 文件 | 是什么 | 现在的状态 |
|---|---|---|
| `ShineEffectToolkit.cs` | 可复用的闪烁效果库,只认"中心点 + 半径"两个参数,不关心你是要用在物品图标、主菜单 Logo 还是 Mods 列表 | 没人调用它,纯库代码,零开销,**天然处于禁用状态** |
| `ModListIconShine.cs` | 真正劫持 `UIModItem` 的 `ILHook`,把 Mods 列表里我们自己的封面换成会闪的版本 | 顶部 `private const bool Enabled = false;`,**已禁用** |

被砍掉的原因不是技术不行,是我做出来之后套在 80×80 的小图标上,又是轨道环、又是彗星拖尾、又是爆发脉冲,层数叠太多,看起来太滑稽了("有点太好笑了")。代码本身没问题,`dotnet build` 编译通过、0 语法错误。

## 以后想要的话,怎么重新打开

### 场景一:让 Mods 列表里的封面重新闪起来
1. 打开 `ModListIconShine.cs`
2. 把 `private const bool Enabled = false;` 改成 `true`
3. 重新构建、重启 tModLoader,去 Mods 列表看
4. 嫌效果太花,就去改 `ShineEffectToolkit.Draw` 里调用的那几行(比如先注释掉 `DrawComet`、`DrawBurst`,只留 `DrawFlareCross` 试试更克制的版本)

### 场景二:想要主菜单 Logo 闪(不是 Mods 列表,是选中我们自己主菜单皮肤之后的标题)
这次做过一版,后来跟着这批一起撤了(改动在 `MainMenu/MatrixMainMenu.cs` 的 `DrawProgramLogo` 里)。想要的话,在 `DrawProgramLogo` 画完标题文字和下划线之后加一行:

```csharp
ShineEffectToolkit.Draw(spriteBatch, logoShineState, drawCenter, titleSize.X * titleScale * 0.5f);
```

别忘了:
- 在 `MatrixMainMenu` 类里加一个字段 `private readonly ShineEffectToolkit.State logoShineState = new();`
- 在 `Update(bool isOnTitleScreen)` 里每帧调用一次 `ShineEffectToolkit.Advance(logoShineState);`

### 场景三:想要某个物品在背包里的图标闪(比如给 `LegendaryCodex` 这种传奇物品加特效)
在对应 `ModItem` 里:

```csharp
private readonly ShineEffectToolkit.State fancyIcon = new();

public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
{
    ShineEffectToolkit.Advance(fancyIcon);

    float iconRadius = Math.Max(frame.Width, frame.Height) * scale * 0.5f;
    ShineEffectToolkit.Draw(spriteBatch, fancyIcon, position, iconRadius);

    spriteBatch.Draw(TextureAssets.Item[Item.type].Value, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
    return false;
}
```

（`position` 在这里默认是图标的中心点，对应 `origin == frame.Size() * 0.5f` 的常见情况。）

## 依赖的素材

`Texture/Myown/ShineFX.png`(项目根目录下,还没提交到 git)—— 这是 `ShineEffectToolkit` 唯一依赖的额外美术资源,来自 `StarsAbove/Menu/ShineFX.png` 的同款星芒贴图,用 `Color` 相乘重新上了矩阵绿的色,没有另外改图。如果以后想要不同颜色/形状的闪光,直接换这张图或者在 `ShineEffectToolkit.ShineTexturePath` 处指向新贴图即可。
