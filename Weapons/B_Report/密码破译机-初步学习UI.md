# 密码破译机 UI 初步学习报告

> 基于 CalamityMod 源码分析，分析对象：`CodebreakerUI`

---

## 一、结论先行

**不需要继承任何 UIState/UIElement！**

Calamity 的密码破译机界面完全是手写绘制的（Raw SpriteBatch Drawing），没有用 tModLoader 的 `UIState`/`UIElement` 框架，而是通过 `ModSystem` 挂钩到原版的界面层（InterfaceLayer）里，然后在每帧手动调用 `SpriteBatch.Draw()` 画出所有东西。

---

## 二、核心架构：三个文件协作

### 1. Tile 本体 —— `CodebreakerTile.cs`
继承 `ModTile`，负责：
- 定义方块的尺寸（5×8 格）
- 在 `RightClick()` 里打开/关闭界面
- 在 `PreDraw()` 里手动绘制方块贴图（含升级零件叠图）

### 2. TileEntity —— `TECodebreaker.cs`
继承 `ModTileEntity`，负责：
- 存储方块的状态数据（图纸ID、电池数量、解密倒计时、各零件是否安装……）
- 每帧 `Update()` 推进解密倒计时、消耗电池
- 网络同步（`SyncContainedStuff()`、`SyncDecryptCountdown()`）
- 存档/读档（`SaveData`/`LoadData`）

### 3. UI 绘制系统 —— `CodebreakerUI.cs`
继承 `ModSystem`，负责：
- 持有 `ViewedTileEntityID`（当前正在看哪个 TileEntity，-1 表示关闭）
- 提供静态方法 `Draw(SpriteBatch spriteBatch)` 做所有绘制和点击检测
- 通过 `UIManagementSystem` 注册到原版 InterfaceLayer

---

## 三、界面如何被"打开"

在 `CodebreakerTile.RightClick()` 里：

```csharp
public override bool RightClick(int i, int j)
{
    TECodebreaker codebreakerTileEntity = CalamityUtils.FindTileEntity<TECodebreaker>(i, j, Width, Height, SheetSquare);
    Player player = Main.LocalPlayer;
    player.CancelSignsAndChests();

    // 如果已经打开了同一个，或者没有解密电脑零件，就关闭
    if (codebreakerTileEntity is null || codebreakerTileEntity.ID == CodebreakerUI.ViewedTileEntityID || !codebreakerTileEntity.ContainsDecryptionComputer)
    {
        CodebreakerUI.ViewedTileEntityID = -1;
        SoundEngine.PlaySound(SoundID.MenuClose);
    }
    else
    {
        // 记录当前正在查看的 TileEntity 的 ID
        SoundEngine.PlaySound(SoundID.MenuOpen);
        CodebreakerUI.ViewedTileEntityID = codebreakerTileEntity.ID;
        Main.playerInventory = true;   // 打开背包（让 UI 可以交互）
        Main.recBigList = false;
    }

    Recipe.FindRecipes();
    return true;
}
```

**关键：** `Main.playerInventory = true` 很重要，它让游戏进入"背包打开"状态，否则很多 UI 交互会被屏蔽。

---

## 四、界面如何被注册到游戏

在 `UIManagementSystem.cs` 的 `ModifyInterfaceLayers()` 里：

```csharp
layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("Codebreaker Decryption GUI", () =>
{
    CodebreakerUI.Draw(Main.spriteBatch);
    return true;
}, InterfaceScaleType.None));
```

`mouseIndex` 是 `"Vanilla: Mouse Text"` 层的位置，Insert 到它之前，保证 UI 在鼠标文字下面渲染。

`InterfaceScaleType.None` 表示不随 UI 缩放变化（界面自己管理缩放）。

---

## 五、UI 绘制逻辑（手写，无 UIState）

`CodebreakerUI.Draw()` 每帧被调用，流程：

```
1. 检查 ViewedTileEntityID 是否有效、玩家是否还在范围内
   → 无效就重置所有状态，return

2. 画背景贴图（DraedonDecrypterBackground.png）
   → spriteBatch.Draw(backgroundTexture, BackgroundCenter, ...)

3. 画电池槽图标（PowerCellSlot_Empty / PowerCellSlot_Filled）
   → 根据 TileEntity.InputtedCellCount 决定显示哪张图

4. 画图纸槽图标（EncryptedSchematicSlotBackground + 对应图纸图标）
   → 根据 TileEntity.HeldSchematicID 切换贴图

5. 处理点击交互（HandleCellSlotInteractions / HandleSchematicSlotInteractions）
   → 用 Rectangle 检测鼠标区域，Main.mouseLeft && Main.mouseLeftRelease 判断点击

6. 根据状态显示费用文字、确认按钮、取消按钮、解密进度条……

7. 画退出按钮
```

---

## 六、需要哪些贴图

**需要贴图**，但都是普通的 png 文件，放在模组的 `UI/DraedonSummoning/` 目录下：

| 贴图文件 | 用途 |
|---|---|
| `DraedonDecrypterBackground.png` | 界面的主背景板 |
| `DraedonDecrypterScreen.png` | 右侧文字显示屏幕区域 |
| `EncryptedSchematicSlotBackground.png` | 图纸放置槽的背景框 |
| `DecryptIcon.png` | 确认解密按钮图标 |
| `DecryptCancelIcon.png` | 取消/退出按钮图标 |
| `ContactIcon.png` | 召唤按钮图标 |
| `CommunicateIcon.png` | 对话按钮图标 |
| `CodebreakerDecyptionBar.png` | 解密进度条边框 |
| `CodebreakerDecyptionBarCharge.png` | 解密进度条填充 |

另外还有在 `UI/DraedonsArsenal/` 里的：

| 贴图文件 | 用途 |
|---|---|
| `PowerCellSlot_Empty.png` | 电池槽（空） |
| `PowerCellSlot_Filled.png` | 电池槽（有电池） |

**没有用 UIState 自带的任何样式贴图**，所有视觉元素都靠这些自定义 png。

---

## 七、点击交互的实现方式

没有用 UIElement 的 `OnLeftClick` 事件，而是完全手动检测：

```csharp
// 定义一个矩形区域
Rectangle clickArea = Utils.CenteredRectangle(drawPosition, texture.Size() * scale);

// 检测鼠标是否在区域内
if (MouseScreenArea.Intersects(clickArea))
{
    // 鼠标悬停效果（按钮放大）
    ButtonScale = MathHelper.Clamp(ButtonScale + 0.035f, 1f, 1.35f);

    // 左键点击判定（mouseLeftRelease 防止长按持续触发）
    if (Main.mouseLeft && Main.mouseLeftRelease)
    {
        // 执行逻辑...
    }
}
else
{
    ButtonScale = MathHelper.Clamp(ButtonScale - 0.05f, 1f, 1.35f);
}
```

`MouseScreenArea` 定义为：
```csharp
public static Rectangle MouseScreenArea => Utils.CenteredRectangle(Main.MouseScreen, Vector2.One * 2f);
```

---

## 八、鼠标穿透阻止

为了防止点击 UI 区域时操作到背后的世界（比如挖方块），需要设置：

```csharp
if (MouseScreenArea.Intersects(backgroundArea))
    Main.blockMouse = Main.LocalPlayer.mouseInterface = true;
```

---

## 九、关闭条件

在 `Draw()` 开头判断，以下任意一条成立就自动关闭界面：

- `ViewedTileEntityID` 对应的 TileEntity 不存在
- TileEntity 类型不对（不是 `TECodebreaker`）
- 玩家距离方块超过 270 格像素（`!Main.LocalPlayer.WithinRange(center, 270f)`）
- `Main.playerInventory` 为 false（背包关闭）
- `Main.LocalPlayer.channel` 为 true（玩家正在持续使用物品）

---

## 十、自己做类似 UI 的最简框架

```
1. 创建 ModTileEntity，存状态数据

2. 创建 ModTile，在 RightClick() 里设置 MyUI.ViewedEntityID = entity.ID

3. 创建继承 ModSystem 的 UI 类，写静态 Draw(SpriteBatch sb) 方法

4. 在另一个 ModSystem.ModifyInterfaceLayers() 里注册 LegacyGameInterfaceLayer

5. 在 Draw() 里：
   - 先校验状态，无效就 return
   - spriteBatch.Draw() 画背景贴图
   - 用 Rectangle + Main.mouseLeft 手动处理点击
   - Main.blockMouse = Main.LocalPlayer.mouseInterface = true 阻止穿透
```

不需要继承 UIState/UIElement，也不需要 UserInterface，整个 UI 就是一个每帧手动画的绘制函数。

---

*分析来源：CalamityMod 1.4.4 分支源码*
*核心文件：`UI/DraedonSummoning/CodebreakerUI.cs`、`Tiles/DraedonSummoner/CodebreakerTile.cs`、`TileEntities/TECodebreaker.cs`、`Systems/UIManagementSystem.cs`*
