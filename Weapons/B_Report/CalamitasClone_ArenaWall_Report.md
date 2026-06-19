# 灾厄克隆体（CalamitasClone）竞技场边界墙实现分析报告

## 概述

灾厄克隆体在战斗开始时会生成一个巨大的矩形边界框（Arena Box），将玩家困在其中。这个边界框的特殊之处在于：

1. **物理阻挡**：玩家无法穿越边界
2. **钩爪可以勾住**：行为如同真实方块
3. **纯粹代码实现**：完全不依赖任何真实 Tile（方块），而是通过逻辑碰撞和 IL 注入模拟固体表面

---

## 核心系统：`ArenaWallSystem`

文件：[`Systems/Mechanic/ArenaWallSystem.cs`](../../../../../../../CalamityModPublic/Systems/Mechanic/ArenaWallSystem.cs)

### Box 数据结构

```csharp
public class Box
{
    public Vector2 position;          // 中心位置（跟随boss）
    public Vector4 boxDimensions;     // (上距, 右距, 下距, 左距)
    public float borderThickness;     // 墙壁厚度（像素）
    public Color borderColor;         // 边框颜色

    public Vector2 TopLeft   => position + new Vector2(-DistanceLeft, -DistanceUp);
    public Vector2 BottomRight => position + new Vector2(DistanceRight, DistanceDown);
    public Vector2 Size      => new Vector2(DistanceLeft + DistanceRight, DistanceUp + DistanceDown);
}
```

`ArenaWallSystem` 维护一个静态列表 `ActiveBoxes`，所有活跃的边界框都在这里注册。

---

## 灾厄克隆体如何创建边界

文件：[`NPCs/CalClone/CalamitasClone.cs`](../../../../../../../CalamityModPublic/NPCs/CalClone/CalamitasClone.cs)

boss 在进入复仇模式（Rev+）战斗后创建边界：

```csharp
// NPCs/CalClone/CalamitasClone.cs 第 312-330 行
if (ArenaBox is null)
{
    ArenaBox = new()
    {
        position = NPC.Center,
        boxDimensions = GetArenaSize(...),
        borderThickness = 80,
        RemovalCondition = () => !NPC.active,
        UpdateBox = UpdateArena,
        DrawBox = DrawArena,
    };
    ArenaWallSystem.ActiveBoxes.Add(ArenaBox);
}
```

每帧更新 Box 的位置和大小：
```csharp
// 第 332 行
ArenaBox.NewDimensions = Vector4.Lerp(ArenaBox.boxDimensions, GetArenaSize(...), 0.025f);
```

边界框大小随战斗阶段动态变化（血量越低越小），且在 Death 模式下更窄。

### 边界大小（默认 Death 模式）

```csharp
public static Vector4 GetArenaSize(...)
{
    var baseSize = new Vector4(1600, 800, 0, 800); // (上, 右, 下, 左) 像素
    if (brothersActive) baseSize *= 1.25f;         // 兄弟出现时扩大
    if (!CalamityWorld.death) baseSize *= 1.25f;   // 非 death 模式更大
    // 低血量时极度缩小（0.4~0.22 倍）
}
```

即默认宽约 **1600px**，高约 **1600px**（上下各 800px）。

---

## 玩家被阻挡的机制

文件：[`Systems/Mechanic/ArenaWallSystem.cs`](../../../../../../../CalamityModPublic/Systems/Mechanic/ArenaWallSystem.cs) — `ArenaWallPlayer.PreUpdateMovement()`

通过 `ModPlayer.PreUpdateMovement()` 钩子，在每帧物理更新**之前**强制修正玩家位置和速度：

```csharp
public override void PreUpdateMovement()
{
    foreach (var box in ArenaWallSystem.ActiveBoxes)
    {
        if (box.ShouldEffectPlayer(Player))
        {
            if (box.Contains(Player.position, Player.Size))
                ContainPlayerLogic(box);
        }
    }
}
```

`ContainPlayerLogic` 做两件事：

### 1. 位置 Snapping（瞬间复位）

如果玩家超出边界，直接修正 `Player.position`：
```csharp
if (Player.Left.X < box.TopLeft.X)
    Player.position.X = box.TopLeft.X;
if (Player.Right.X > box.BottomRight.X)
    Player.position.X = box.BottomRight.X - Player.width;
// 同理处理上下边界
```

### 2. 速度截断

模拟预测下一帧位置，若会超出边界则截断速度：
```csharp
var originalVelocity = Player.velocity;
Player.position += originalVelocity; // 模拟移动

if (Player.Left.X < box.TopLeft.X)
    Player.velocity.X = box.TopLeft.X - originalTopLeft.X; // 截断

Player.position -= originalVelocity; // 还原
```

这与 Terraria 原生 Tile 碰撞逻辑几乎完全一致，因此玩家的手感与真实墙壁无异。

---

## 钩爪可以勾住的机制

文件：[`ILEditing/MechanicILChanges.cs`](../../../../../../../CalamityModPublic/ILEditing/MechanicILChanges.cs) — 第 1668 行

通过 `On_Projectile.orig_AI_007_GrapplingHooks` 钩子，完全接管所有钩爪的 AI 逻辑：

```csharp
public static void AllowHooksToGrabArenabox(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
{
    // ...
    foreach (var box in ArenaWallSystem.ActiveBoxes)
    {
        // 检测钩爪是否进入了"墙壁区域"（borderThickness 范围内但在外边界之外）
        bool inWall = !Vector2PointCollision(box.TopLeft, box.Size, self.Center)
                   && Vector2PointCollision(box.TopLeft - new Vector2(box.borderThickness),
                                            box.Size + new Vector2(box.borderThickness) * 2,
                                            self.Center);
        if (inWall)
        {
            if (self.ai[0] == 0) // 钩爪正在飞行中
            {
                self.ai[0] = 2;       // 设为"已抓住"状态
                self.velocity = Vector2.Zero;
                // 记录钩爪在box上的相对位置（0~1 归一化）
                self.Calamity().arenaBoxPosition = new Vector2(
                    Utils.Remap(self.Center.X, box.TopLeft.X, box.BottomRight.X, 0, 1),
                    Utils.Remap(self.Center.Y, box.TopLeft.Y, box.BottomRight.Y, 0, 1)
                );
                self.Calamity().arenaBox = box;
            }
            if (self.ai[0] == 2) // 已抓住，注册到玩家的 grappling 数组
            {
                Main.player[self.owner].grappling[Main.player[self.owner].grapCount] = self.whoAmI;
                Main.player[self.owner].grapCount++;
                intersectingWall = true;
            }
        }
    }

    if (!intersectingWall)
        orig(self); // 未勾到边界，执行正常AI
}
```

### 钩爪的跟随逻辑

边界框会随 boss 移动，因此每帧需要更新钩爪位置：
```csharp
if (self.Calamity().arenaBox is not null)
{
    var box = self.Calamity().arenaBox;
    if (ArenaWallSystem.ActiveBoxes.Contains(box))
    {
        // 用归一化坐标还原钩爪的世界坐标，实现"钩爪随边界移动"
        self.Center = box.TopLeft + box.Size * self.Calamity().arenaBoxPosition;
    }
}
```

---

## 弹射物碰撞（SolidCollision / TileCollision 的 IL 注入）

文件：[`ILEditing/MechanicILChanges.cs`](../../../../../../../CalamityModPublic/ILEditing/MechanicILChanges.cs) — 第 1736 行

使用 `On_Collision.orig_SolidCollision` 和 `On_Collision.orig_TileCollision` 钩子，让边界对**弹射物**也生效（不只是玩家）：

```csharp
// SolidCollision：检测某区域是否与固体碰撞
private static bool ArenaCollision_Vector2_int_int(On_Collision.orig_SolidCollision_Vector2_int_int orig, ...)
{
    foreach (var item in ArenaWallSystem.ActiveBoxes)
    {
        if (item.Vector2PairInWall(Position, new(Width, Height)))
            return true; // 视为碰到了固体
    }
    return orig(Position, Width, Height);
}

// TileCollision：修正速度向量
private static Vector2 ArenaCollision_TileCollision(On_Collision.orig_TileCollision orig, ...)
{
    Velocity = orig(...);
    foreach (var item in ArenaWallSystem.ActiveBoxes)
    {
        if (item.InnerEffect(Position, new Vector2(Width, Height)))
            Velocity = ArenaCollisionLogic(item, Position, Width, Height, Velocity);
    }
    return Velocity;
}
```

这使得边界对弹射物（如火球）也会产生物理阻挡，表现与真实 Tile 一致。

---

## 渲染：视觉效果

边界框通过 `ModSystem.PostDrawTiles()` 在所有 Tile 绘制完成后绘制，使用 `DrawLineBetter` 工具方法画出四条线：

- **内边框**（4px 偏移，8px 粗）：彩色（随时间在青色/紫色间插值，或红色系）
- **动态光晕**（4条向外扩散、透明度递减的线）：形成"能量屏障"效果
- **外边框**（borderThickness - 4px 偏移，4px 粗）

颜色会根据战斗状态变化：
- 正常：红色/橙色交替
- 兄弟（Cataclysm/Catastrophe）存活：青色/紫色
- 消失中：灰色

粒子效果也通过 `UpdateArena` 每帧在边框上生成向外散射的粉尘。

---

## 总结：整体架构

```
CalamitasClone.AI()
    └─ 创建/更新 ArenaWallSystem.Box，注册到 ActiveBoxes

ArenaWallSystem.PreUpdateEntities()
    └─ 每帧更新 Box 的位置和大小（线性插值）

ArenaWallPlayer.PreUpdateMovement()
    └─ 在玩家物理更新前，强制截断速度 + 修正位置

ILChanges.AllowHooksToGrabArenabox (On hook)
    └─ 接管钩爪AI，检测与Box边界的碰撞，模拟"抓住"行为

ILChanges.ArenaCollision_* (On hooks)
    └─ 接管 Collision.SolidCollision / TileCollision
    └─ 使弹射物、液体等也受到边界阻挡

ArenaWallSystem.PostDrawTiles()
    └─ 在 Tile 层之上绘制彩色线框
```

**没有使用任何真实 Tile**。整个系统是纯粹的运行时碰撞逻辑模拟，通过 MonoMod 的 `On_` 钩子和 `ModPlayer` 覆盖实现，优雅地复用了游戏原有的碰撞接口（`SolidCollision`、`TileCollision`）使得其他所有依赖这些接口的系统（钩爪、弹射物、玩家移动）都自动获得正确的边界行为。
