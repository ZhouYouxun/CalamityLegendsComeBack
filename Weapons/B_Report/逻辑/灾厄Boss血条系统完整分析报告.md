# 灾厄 Boss 血条系统完整分析报告

> 参考来源：`CalamityMod/UI/BossHealthBarManager.cs`

---

## 一、整体架构

灾厄的 Boss 血条通过继承 tModLoader 的 `ModBossBarStyle` 来**完全替换**原版血条系统（`PreventDraw => true` 屏蔽原版绘制），自己接管所有 Boss 血条的 Update 和 Draw 流程。

核心类结构：

```
BossHealthBarManager : ModBossBarStyle   ← 全局管理器，每帧遍历所有活跃 NPC
    └── BossHPUI                         ← 单个 Boss 血条实例（含数据 + 绘制逻辑）
```

同时存在另一套 `ModBossBar` 子类（`VanillaBossBars/` 目录），用于给**原版 Boss** 覆盖图标显示和 HP 聚合逻辑（如 ExoMechs 的三合一血条），这是 tModLoader 原生 API，与上述自定义体系并行。

---

## 二、需要哪些贴图

共 **3 张贴图** + **1 个自定义字体**，全部在 `SetStaticDefaults()` 里用 `ImmediateLoad` 提前加载：

| 资源 | 路径 | 用途 | 图像描述 |
|---|---|---|---|
| `BossHPMainBar` | `UI/MiscTextures/BossHPMainBar.png` | 主血量条填充纹理 | 金黄色横向渐变细长条，高度约 10px |
| `BossComboHPBar` | `UI/MiscTextures/BossHPComboBar.png` | 连击差值红条 | 深红色横向渐变细长条，同高度 |
| `BossSeperatorBar` | `UI/MiscTextures/BossHPSeperatorBar.png` | 分隔装饰线 | 浅蓝白色细线，高度 6px |
| `HPBarFont` | `Fonts/HPBarFont.xnb` | 血量百分比专用字体 | MonoGame XNB 格式自定义位图字体 |

**注意**：这 3 张贴图在绘制时都是被 `Rectangle` 裁切或拉伸显示，因此：
- 主血条和连击条只需做成**横向渐变纹理**，宽度即为最大血条宽度（400px）；系统按当前血量比例裁取宽度。
- 分隔线同理，满宽绘制即可。

**如果你想偷懒不做贴图**：完全可以用 `TextureAssets.MagicPixel`（1×1 白像素）+ `Color` 参数来画纯色矩形代替，功能上没有区别，只是观感更朴素。

---

## 三、血条的屏幕位置

```csharp
// BossHealthBarManager.Draw()
int startHeight = 100;
int x = Main.screenWidth - 420;    // 右边距 420px（从屏幕左沿算）
int y = Main.screenHeight - 100;   // 距离底部 100px

// 有物品栏/入侵事件时向左偏移
if (Main.playerInventory || Main.invasionType > 0 || ...)
    x -= 250;

// 每个血条向上叠加
y -= BossHPUI.VerticalOffsetPerBar;  // = 70px 间距
```

效果：右下角，血条从下往上排列，最多显示 **4 个**（`MaximumBars = 4`）。

血条最大宽度：**400px**（`BarMaxWidth = 400`）。

---

## 四、单条血条的内部布局

```
y + SeparatorBarYOffset (18px)  → 分隔线（宽400px，高6px，蓝白/红/灰）
y + MainBarYOffset      (28px)  → 主血条（宽随血量变化，最大400px）
                                  → 连击红条（紧接在主条右侧）
y + 22 - textSize.Y             → 左侧：血量百分比文字（金色，自定义字体）
y + 23 - nameSize.Y             → 右侧：Boss 名称（白色，MouseText 字体）
y + MainBarYOffset + 17 (45px)  → 血条右下方：精确 HP 数值 或 附属体计数
```

整体高度约 **50px**，加间距 70px 一组。

---

## 五、绘制流程逐步拆解（`BossHPUI.Draw()`）

### 5.1 开关动画

```csharp
float animationCompletionRatio = OpenAnimationTimer / (float)OpenAnimationTime;  // 0→1，80帧
// 关闭时反向
if (CloseAnimationTimer > 0)
    animationCompletionRatio = 1f - CloseAnimationTimer / (float)CloseAnimationTime;
```

开启时有**闪烁效果**：在第 3、4、7、8、15、16 帧强制把比例随机设为 0.4~0.8，模拟电子屏幕接通的不稳定感。

### 5.2 主血条绘制

```csharp
int mainBarWidth = (int)MathHelper.Min(
    BarMaxWidth * animationCompletionRatio,   // 开场动画限制宽度
    BarMaxWidth * NPCLifeRatio                // 实际血量比例
);
sb.Draw(BossMainHPBar, new Rectangle(x, y + 28, mainBarWidth, BossMainHPBar.Height), Color.White);
```

`BossMainHPBar` 的原始宽度是 400px，`Rectangle` 的 Width 参数直接控制显示多少。

### 5.3 连击差值条（Combo Bar）

30 帧内连续受伤时触发，在主条右侧显示红色差值：

```csharp
if (ComboDamageCountdown > 0)
{
    int comboBarWidth = (int)(BarMaxWidth * HealthAtStartOfCombo / InitialMaxLife) - mainBarWidth;
    if (ComboDamageCountdown < 6)
        comboBarWidth = (int)(comboBarWidth * ComboDamageCountdown / 6f);  // 最后6帧缩小消失
    sb.Draw(BossComboHPBar, new Rectangle(x + mainBarWidth, y + 28, comboBarWidth, ...), Color.White);
}
```

### 5.4 分隔线（暴怒变色）

```csharp
Color separatorColor = new Color(240, 240, 255);  // 默认蓝白

if (NPCIsEnraged)
    // 白→红 渐变（120帧）
    separatorColor = Color.Lerp(蓝白, Color.Red * 0.5f, EnrageTimer / 120f);
else if (NPCIsIncreasingDefenseOrDR)
    // 白→灰 渐变
    separatorColor = Color.Lerp(蓝白, Color.LightGray * 0.5f, IncreasingDefenseOrDRTimer / 120f);

sb.Draw(BossSeperatorBar, new Rectangle(x, y + 18, 400, 6), separatorColor);
```

### 5.5 文字（暴怒时 Boss 名发光脉冲）

暴怒时 Boss 名称有 **4 方向偏移脉冲光晕**：

```csharp
float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f) * 0.5f + 0.5f;
float outwardness = EnrageTimer / 120f * 1.5f + pulse * 2f;
for (int i = 0; i < 4; i++)
{
    Vector2 offset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * outwardness;
    DrawBorderStringEightWay(..., offset, Color.Red * 0.6f, ...);
}
// 然后再在正中央绘制白色本体文字
DrawBorderStringEightWay(..., Vector2.Zero, Color.White, ...);
```

---

## 六、HP 的计算逻辑

### 6.1 多部件 Boss（OneToMany）

```
EoW:   Head + Body × N + Tail  → 全部段的 HP 相加
BoC:   BrainOfCthulhu + Creeper × N  → 相加
Skele: Head + Hand × 2  → 相加
Ravager: Body + Claw×2 + Leg×2 + Head  → 相加
...
```

以 `NPCType` 为 key 存在字典里，每帧遍历所有活跃 NPC 求和。

### 6.2 特殊规则（SpecialHPRequirements）

- **分裂蠕虫类**（`SplittingWorm = true`）：沿 ai[0]/ai[1] 链表从头到尾遍历所有节点累加 HP。
- **月球领主**（`MoonLordCore`）：Head + Hand × 2 + Core，且核心进入 `ai[0] == 2`（死亡动画阶段）时 HP 归零不计入。

### 6.3 排除列表（BossExclusionList）

下列 NPC **不会**生成血条：
- 蠕虫身体/尾巴段（AquaticScourge、AstrumDeus、DesertScourge、StormWeaver、DoG...）
- 月球领主手和头（只有 Core 有条）
- ExoMechs 武器臂
- 虚空之终裂脑等伪 Boss

---

## 七、血条的生命周期

```
AttemptToAddBar() 被调用
    → 检查：Bars 数量 < 4，且该 NPC 不在已有列表中，且 NPC 活跃
    → 通过：new BossHPUI(index) 加入 Bars 列表

每帧 Update():
    → OpenAnimationTimer 递增（0→80）
    → 检测 NPC 是否消失/ShouldCloseHPBar
        → 是：CloseAnimationTimer 递增（0→120）
        → CloseAnimationTimer >= 120：从 Bars 列表移除
```

---

## 八、如果你要原创一个类似的血条

### 最简实现（纯代码，不需要任何外部贴图）

```csharp
// 在 ModSystem 的 PostDrawInterface 或 ModBossBarStyle.Draw 里
SpriteBatch sb = Main.spriteBatch;
Texture2D pixel = TextureAssets.MagicPixel.Value;

// 背景框
sb.Draw(pixel, new Rectangle(x - 2, y - 2, 404, 14), Color.Black * 0.5f);
// 血量条
sb.Draw(pixel, new Rectangle(x, y, (int)(400 * lifeRatio), 10), new Color(200, 60, 60));
// 分隔线
sb.Draw(pixel, new Rectangle(x, y - 3, 400, 2), Color.White * 0.8f);
```

### 需要贴图的情况

如果想做 MGR 那种风格感（冷峻金属感、倾斜线条、扫光），则需要：

| 贴图 | 说明 | 建议尺寸 |
|---|---|---|
| **血条主体纹理** | 横向渐变，带金属质感或发光 | 400 × 10 px |
| **血条边框/底槽** | 血条背景（可以有斜切边角） | 404 × 14 px |
| **连击差值条** | 颜色区分，通常用红/橙 | 400 × 10 px |
| **装饰线** | 血条上方或下方的细线 | 400 × 2 px |

如果想有**扫光动画**，可以在贴图上叠加一个斜向的白色高光纹理，用偏移 UV 模拟流动。

---

## 九、设计建议（区别于灾厄的创意方向）

灾厄血条的特点：金色填充 + 蓝白细线分隔 + 右对齐 Boss 名 + 左侧百分比。风格上偏内敛典雅。

如果要做出差异化：

- **科技/机械感**：横向条改为分段式（像电量格），或加倒计时数字
- **魔幻感**：血条两端加装饰纹章图案，填充色随 Boss 阶段变色
- **恐怖感**：血条本身略微抖动，低血量时出现裂纹效果（叠加半透明贴图）
- **简洁感**：完全不要边框，只有细线+纯色矩形，字体加大

---

## 十、关键常量速查

```csharp
BarMaxWidth          = 400    // 血条最大像素宽度
OpenAnimationTime    = 80     // 开启动画帧数
CloseAnimationTime   = 120    // 关闭动画帧数
EnrageAnimationTime  = 120    // 暴怒变色渐变帧数
VerticalOffsetPerBar = 70     // 每条血条向上的间距
MaximumBars          = 4      // 最多同时显示几条
SmallTextScale       = 0.75f  // 小字（精确HP/附属体）缩放
MainColor            = new Color(229, 189, 62)   // 百分比字体颜色（金色）
```

---

*报告生成日期：2026-06-19*
