# 图标闪烁效果

当前已启用：Mods 列表中的 `80×80` 图标每秒进行一次五段斜向扫光。

## 这次搞明白了什么

起因是想复刻 `StarsAbove` 模组的一个效果:玩家还没进游戏、甚至没选角色,在 tModLoader 的 **Mods 选择列表**里,`StarsAbove` 那个模组的封面(不是主菜单 Logo,是列表里的缩略图)会自己播放动画。

参考文件:
- `ModSources/StarsAbove/Systems/StarsAboveModSystem.cs` —— 关键就在这
- `ModSources/StarsAbove/Menu/StarsAboveMainMenu.cs` —— 另一个效果(主菜单 Logo 闪烁),别搞混了

结论:tModLoader **没有**给 modder 开放"Mods 列表缩略图"的官方钩子。StarsAbove 是用 **MonoMod 的 `ILHook`**,在运行时直接改写 tModLoader 自己内部的 `Terraria.ModLoader.UI.UIModItem.OnInitialize` 方法的 IL 代码:在这个方法把静态 `icon.png` 存进私有字段 `_modIcon` 的那一刻,插入一段判断——如果当前这一行正在处理的模组名是 `"StarsAbove"`,就把要存的图标换成他们自己写的 `UIAnimatedImageAlwaysHovering`(会自动播放帧动画的 `UIElement`),读取的是他们的 `Menu/AnimatedIcon.png`(160×2480 的帧序列图,32 帧,每帧 80×80)。

**当前视觉逻辑**：把图标按 `x + y` 分为五条等宽的 `/` 形斜带。每条都由原始图标像素生成，只提高对应区域原像素的 RGB 亮度并保留原始 alpha，因此透明像素始终透明，不会出现方形覆盖层。每条闪烁持续约 18 帧，相邻分区相隔 4 帧开始；五条结束后静止至两秒循环结束。遮罩严格在图标范围内，不再使用轨道环、彗星、字符或爆发粒子。

## 这个文件夹里有什么

| 文件 | 是什么 | 现在的状态 |
|---|---|---|
| `ShineEffectToolkit.cs` | 五段斜向扫光遮罩；只接收中心点和半径 | 当前由 Mods 列表图标调用 |
| `ModListIconShine.cs` | 劫持 `UIModItem` 的 `ILHook`，把本模组封面包装成会闪的 UI 元素 | 顶部 `private const bool Enabled = true;`，**已启用** |

## 开关与调整

### 场景一:让 Mods 列表里的封面停止闪烁
1. 打开 `ModListIconShine.cs`
2. 把 `private const bool Enabled = true;` 改成 `false`
3. 重新构建、重启 tModLoader,去 Mods 列表看
4. 想调整节奏，改 `ShineEffectToolkit.cs` 的 `SliceStartIntervalFrames`、`SliceFlashFrames` 或 `CycleFrames`；想调整分段数，改 `SliceCount`。

## 依赖的素材

当前扫光由运行时生成的五张透明遮罩完成，不依赖额外美术素材；原有 `Texture/Myown/ShineFX.png` 保留在仓库中，但不参与这版图标扫光。
