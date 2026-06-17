# CosmicDischarge 全面 DoG 化改造计划表

> 目标：将 CosmicDischarge 的全部特效从"冰霜系"彻底重建为"神明吞噬者系"。  
> 保持现有的攻击结构（三模式×三连招+QuickDraw+大招+右键切换），仅替换/重建视觉与音效层。  
> 参考来源：见《神明吞噬者及其所有周边的特效完整分析.md》

---

## 架构总览

```
右键切换（Galaxia 风格幽影刃）
      ↓
┌─────────────────────────────────────────────────────────┐
│  模式 A: 吞噬者之鞭（Devourer Whip）   [原 Whip]       │
│  模式 B: 维度裂隙剑（Rift Blade）      [原 Sword]       │
│  模式 C: 段体锁链（Segment Chain）     [原 ChainKnife]  │
└─────────────────────────────────────────────────────────┘
      ↓ 任意模式
QuickDraw: 裂隙刺穿（Rift Pierce）       [原 QuickDraw]
大招: 绝命激光墙（Lethal Laser Grid）   [原 UltimateField]

被动: GodSlayerInferno + DoG 段体护盾   [原 Frost debuff 系]
```

---

## 第一部分：全局颜色与视觉基础系统重建

### 1.1 颜色系统替换（`CosmicDischargeCommon.cs` 改动）

**现状：** 全局使用四种冰蓝色
**目标：** 换成 DoG 动态双色体系

```csharp
// 旧颜色（全部废弃）
FrostCoreColor  = new(150, 255, 255)  // 冰蓝
FrostGlowColor  = new(110, 175, 255)  // 蓝紫
FrostDarkColor  = new(58, 84, 150)    // 深蓝
FrostWhiteColor = new(225, 250, 255)  // 冷白

// 新颜色（全部新建）
DoGCyanColor    = Color.Cyan                          // DoG 青色
DoGFuchsiaColor = Color.Fuchsia                       // DoG 洋红
DoGPurpleColor  = new Color(145, 0, 255)              // DoG 紫（传送色）
DoGWhiteColor   = Color.White                         // 激光芯白
DoGBlackColor   = Color.Black with { A = 0 }          // additive 暗底（透明黑）

// 动态色（使用与 DevourerofGodsHead.cs 完全相同的公式）
public static Color DoGSpecialColor => Color.Lerp(
    Color.Fuchsia, Color.Cyan,
    MathHelper.SmoothStep(0, 1,
        (MathF.Sin(Main.GlobalTimeWrappedHourly * 2) + 1) * 0.5f));

// 根据模式锁定颜色（被动/主动/传送）
public static Color GetModeColor(CosmicDischargeAttackMode mode) => mode switch
{
    CosmicDischargeAttackMode.Whip      => DoGCyanColor,     // 被动/鞭 → 青
    CosmicDischargeAttackMode.Sword     => DoGFuchsiaColor,  // 主动/剑 → 洋红
    CosmicDischargeAttackMode.ChainKnife => DoGPurpleColor,  // 传送/链 → 紫
    _ => DoGSpecialColor
};
```

### 1.2 Debuff 系统替换

**现状：** Nightwither + Frozen + Frostburn2 + Chilled  
**目标：** 换成 DoG 专属

```
新 debuff 组合：
  - GodSlayerInferno（主要，等同于原 Nightwither 的地位）
  - GodSlayerInferno 持续时间延长版（重击/终结技：15秒）
  - 普通命中：5秒 GodSlayerInferno
  - QuickDraw 命中：7秒 GodSlayerInferno

删除：
  - Nightwither
  - Frozen
  - Frostburn2
  - Chilled
  - CosmicDischargeFrostMarkDebuff（整个 Frost Mark 体系废弃）
```

### 1.3 链条/弹幕贴图体系

**现状：** 链条用冰蓝色贴图，尖端是刀刃形  
**目标：** 视觉上改造成 DoG 段体感

**链条贴图设计方向：**
- 把每节段体画成 DoG 身体节的缩小版（青色底 + 青/紫双色 glowmask）
- 尖端（Tail 区域）改成类 DoG 下颚形状（开合感）
- 整体色调：暗底 + 青色边光

**绘制层叠（修改 `DrawCurvedBladeGlow`）：**
```
1. 最外层 glow（宽）：DoGPurpleColor × 0.15，additive
2. 外层 glow（中宽）：DoGCyanColor × 0.22，additive
3. 洋红辉光线（窄）：DoGFuchsiaColor × 0.18，additive
4. 白色芯（极窄）：White × 0.28，additive
5. tip 处 bloom：DoGSpecialColor × 0.22，additive

（注：所有 glow 层都用 alpha=0 的颜色 + additive 混合，和 StreamGougePortal 一样）
```

### 1.4 音效全面替换表

| 触发场景 | 旧音效 | 新音效 |
|---|---|---|
| 鞭型发布/弹出 | `SoundID.Item71` | `SoundID.Item71`（保留，但音调改+0.4，更尖锐） |
| 刺击/贯穿 | `SoundID.Item122` | `DevourerAttack.ogg`（DoG 攻击音效） |
| 剑型挥击 | `SoundID.Item71`（不同音调） | `CalamityMod/Sounds/Item/HeavySwing`（保留）+ `DevourerAttack.ogg`（叠加） |
| 剑型终结技蓄力 | `SoundID.Item15` | `DevourerRiftBuilding.ogg` |
| 剑型终结技出招 | `SoundID.Item71 -0.35` | `DoGLaserWallBigAttack.ogg`（HyperdeathRiftScepter 同款） |
| QuickDraw 起手 | `SoundID.Item119 0.38` | `DevourerRiftOpen.ogg` |
| 右键切换 | `SoundID.Item119 / SoundID.Item4` | `DemonSwordKillMode.ogg` |
| 大招激活 | `SoundID.Item29` | `DevourerRiftOpen.ogg` + `DevourerSpawn.ogg`（同时） |
| 大招激光发射 | （无） | `DoGLaserWallBigAttack.ogg` |
| 重型命中 | `DemonSwordInsaneImpact` | `DemonSwordInsaneImpact`（保留）+`DevourerAttack.ogg`（同时） |
| 普通命中 | `LanceofDestinyStrong` | `LanceofDestinyStrong`（保留）|

---

## 第二部分：左键三形态详细特效方案

### 模式 A：吞噬者之鞭（Devourer Whip）
> DoG 感核心：「被动蛇行 → 青色为主，每次爆发留洋红残影」

#### A-1：WhipOver / WhipUnder（鞭扫）

**蓄力阶段（WindUp 10帧）：**
- 链条沿曲线展开时，每节段体之间 spawn 1 个 `SparkParticle`
- 粒子颜色：`DoGCyanColor × 0.7`，随机速度 ±1.5
- 链条 glow 以 `DoGCyanColor` 为主（被动态对应青色）

**弹出阶段（Snap 9帧）：**
- 链条全速展开时切换成 `DoGSpecialColor`（动态青洋红）
- 在每帧从尖端向手柄方向放 2 个 `LineParticle`（模拟 DoGFire trail）：
  - 外层：`DoGCyanColor × 0.5`，宽度 2.5
  - 内层：`DoGFuchsiaColor × 0.3`，宽度 0.8
- 到达最远点时（`!impactEffectsPlayed && t >= 0.7f`）：
  - `DirectionalPulseRing`（颜色：`DoGFuchsiaColor`，方向：攻击方向，大小：0.25）
  - 8 个 `SparkParticle`（随机 Fuchsia/Cyan，速度 4–12）
  - **新增：1 个微型裂隙效果**（用 3 条 `LineParticle` 从 TipPosition 向外辐射，模拟 DoGRiftCrack）

**收回阶段：**
- glow 颜色渐变回 `DoGCyanColor`（退潮回青色）
- 链条尖端留 3 个 `SparkParticle` 轨迹（`DoGCyanColor`，小速度）

**命中特效（替换 `SpawnHitEffects`）：**
- 删除：StrongBloom + FrostCoreColor 蓝色系
- 新增：
  - `StrongBloom`（`DoGFuchsiaColor × 0.55`，重击；`DoGCyanColor × 0.34`，普通）
  - `DirectionalPulseRing`（`DoGSpecialColor × 0.45`）
  - `SparkParticle` × 12–22 个（随机 Fuchsia/Cyan，和 DimensionTearingDisk 命中粒子一致）
  - `GodSlayerInferno` 5秒

**Ultimate 激活时额外效果（原 IceBolt 扇形 → 改为能量喷射）：**
- 原来：发射 5 个 `IceBolt`
- 改为：发射 3 个 `DoGCyanEnergyBolt`（新建，视觉为青色 primitive trail 弹幕，参考 DoGFire）

---

#### A-2：WhipThrust（鞭刺）

**发动风格：** 这一击模拟 DoG 正面冲刺咬合

**蓄力阶段：**
- 链条在玩家前聚拢，spawn `HeavySmokeParticle` 效果（模拟 DoG 喷火前的烟雾）
- 改为：在手柄位置持续 spawn `SquishyLightParticle`（DoGFire 喷发前同款）

**刺出阶段：**
- 到达最远点：
  - `PulseRing`（`DoGFuchsiaColor`，中型，15帧）
  - 20 个 `SparkParticle`（全 Fuchsia，速度向四面散射，模拟 DoG 咬合特效）
  - 屏幕震动强化（原 4.4 → 改为 6.5）
  - 播放 `DevourerAttack.ogg`

**命中尖端时（tip = true）：**
- 额外效果：模拟 DoG 咬合
  - 20 个 SparkParticle（同 `DevourerofGodsHead.cs line 2081` 数量）
  - 额外 `StrongBloom`（更大，`DoGFuchsiaColor`）
  - 播放 `DevourerAttack.ogg` with Pitch 0.2

**Frost Mark 系统废弃，改为：**
- WhipThrust 尖端命中 → 施加 7秒 GodSlayerInferno
- 施加 `DoGMarkDebuff`（新建，DoG 主题的标记）：被剑形模式击中时额外触发裂隙爆炸

---

### 模式 B：维度裂隙剑（Rift Blade）
> DoG 感核心：「现实撕裂感，每次攻击都是在用 DoG 的下颚劈砍空间」

**链条视觉在剑模式下的绘制改动：**
- 剑形刀刃不用链条贴图，改用纯发光原始几何体（类似 MawOfInfinity 的持剑弹幕）
- 刀刃颜色整体 `DoGFuchsiaColor`（主动攻击对应洋红）
- 两侧各有一条细 glow 线：左 `DoGCyanColor`，右 `DoGPurpleColor`

#### B-1：SwordSwingOne（一段剑）

**挥击轨迹（替换 SwordSmear DrawSwordSmear）：**
- tipHistory 渐变：
  - 头部（最新）：`DoGFuchsiaColor × 0.8`，宽 55px
  - 中部：`DoGSpecialColor × 0.5`，宽 35px
  - 尾部：`DoGCyanColor × 0.3`，宽 20px
- 轨迹本身还要加一层极细白芯（2px），像激光束在划过

**命中时（改 OnHitNPC，SwordSwingOne 分支）：**
- 原来：发射 IceBolt 方向弹
- 改为：
  - 沿挥击方向发射 1–2 个 `DoGCyanEnergyBolt`（青色 DoGFire 样式弹幕）
  - 在目标位置爆出 `DirectionalPulseRing`（`DoGFuchsiaColor`，沿挥击方向）
  - 3 条"现实裂缝" LineParticle 从命中点向外辐射（模拟 DoGRiftCrack）
  - 如果目标有 `DoGMarkDebuff`：额外爆发 `PulseRing`（`DoGSpecialColor`） + 6个 SparkParticle

#### B-2：SwordSwingTwo（二段剑）

**特色改动（原来是冰刺从地面生出）：**
- 删除：`IceSpike` 从地面弹出 + 目标弹飞
- 改为：**维度锁**
  - 在目标头顶召唤一个"DoG传送门"（用 StreamGougePortal 三层叠旋）
  - 从传送门中心射出 1 条 `FriendlyLaserWallBeam`（0.35x scale，0.5x 伤害，`DoGFuchsiaColor`）
  - 目标被向上吸引（而不是弹飞），速度 -7
  - 传送门持续 15 帧后消散

#### B-3：SwordFinisher（终结剑）

这是整个剑形态的高潮技，也是 DoG 风格最浓烈的一击。

**蓄力旋转阶段（WindUp 34帧）：**
- 删除：冰晶 SparkParticle 旋转蓄力
- 改为：DoG 传送准备演出
  - 每帧从玩家周围随机位置 spawn 1 个 `SparkParticle`（随机 Cyan/Fuchsia，螺旋向内收）
  - 类比 DoGTeleportRift 蓄力：星芒 + 内缩 bloom ring
  - 蓄力进度 t 越大，颜色越偏 Fuchsia（攻击态）
  - 每 8帧 ApplyScreenShake（1.2 + t * 1.5），和原来完全相同
  - 旋转轨迹由 `DoGFuchsiaColor` 替代蓝色

**旋转轨迹绘制（tipHistory）：**
- 头部（最新）：`DoGFuchsiaColor × 0.9`，宽 65px
- 尾部：`DoGCyanColor × 0.4`，宽 25px
- 极细白芯 3px 贯通全轨迹

**真空吸取阶段（WindUp 期间的 NPC 吸引）：**
- 保留原有的半径 260/320 吸引逻辑（优秀机制）
- 吸引期间对被吸目标每 6 帧 append 1 个微型 `DoGMarkDebuff`（叠层可视化提示）

**出招爆发瞬间（SlamFrame 击中）：**
- 删除：CosmicIceBurst 冰雪爆炸 × 8/16 个
- 改为：DoG 大爆炸套餐
  - `StrongBloom`（`DoGFuchsiaColor`，0.75，大尺寸）
  - `PulseRing`（`DoGSpecialColor`，中型，22帧）
  - 20 个 `SparkParticle`（全随机 Fuchsia/Cyan，速度 6–14，爆炸状）
  - **3 条 DoGRiftCrack**：从命中点向三个方向辐射（间距 120°）
  - `DirectionalPulseRing`（`DoGFuchsiaColor × 0.5`，沿出招方向，0.4尺寸）
  - 屏幕震动 7.2（保留原值）
  - 播放 `DoGLaserWallBigAttack.ogg`（极具冲击感）

**命中后额外效果（OnHitNPC）：**
- 删除：CosmicIceBurst × 8/16 弹幕
- 改为：  
  - 在目标位置生成"DoG 下颚咬合"视觉：在目标中心上下各一个 `StrongBloom`，然后合拢（上下各生成一个 `DoGJawParticle`，这是新粒子，代表 DoG 张口咬下）
  - 从目标中心向四面发射 4 个 `DoGCyanEnergyBolt`（代替冰爆）
  - 如果大招激活（UltimateFieldActive）：改为 8 个，并从正上方额外下落 1 条 `FriendlyLaserWallBeam`（0.3x scale）

---

### 模式 C：段体锁链（Segment Chain）
> DoG 感核心：「这是 DoG 的身体本身，锁链每一节都是 DoG 的体节」

**链条贴图专属视觉（Chain Knife 模式下的特殊绘制）：**
- 保留当前 ChainKnife 的大弧扫攻击结构（最像 DoG 身体挥扫）
- 每一节链条绘制时额外叠一层细小 `DoGPurpleColor` 的 glow dot（模拟 DoG 体节发光点）
- 链条尖端（Tail）改成深色 + 双侧 Cyan 光边，像 DoG 的尾巴

#### C-1：ChainKnifeSingle（单次扫）

**弧扫特效：**
- 弧扫过程中，沿弧度每隔 6px 路径 spawn 1 个 `SparkParticle`（`DoGPurpleColor`，小速度）
- 到达弧顶（`!impactEffectsPlayed && t >= 0.46f`）：
  - `EmitAirCrack` 替换为 DoG 版：
    - `DirectionalPulseRing`（`DoGCyanColor`）
    - 4 个从尖端辐射的 LineParticle（模拟 DoGRiftCrack 效果）
  - 原来的 IceBolt 扇形 → 改为 1 个 `DoGCyanEnergyBolt`（直射最近目标方向）

**命中特效：**
- 删除：冰晶爆炸
- 新增：
  - `SparkParticle` × 8（随机 Purple/Cyan）
  - `StrongBloom`（`DoGPurpleColor × 0.45`）

#### C-2：ChainKnifeScatter（散射扫）

**弧扫反向（更剧烈）：**
- 弧扫至最远时：
  - 3 条 DoGRiftCrack 辐射（比 Single 多）
  - 3 个 `DoGCyanEnergyBolt` 散射（扇形 ±15°）
  - 屏幕震动 7.0（比 Single 更强）

#### C-3：ChainKnifeBiteAll（吞噬全咬）

**这是模式 C 的终结技，视觉上模拟 DoG 的咬合突击。**

- 弧扫最大范围（480px，比其他大）
- 到达弧顶时：
  - **DoG 咬合演出全套**（参照 DoGTeleportRift 爆开）：
    - `PulseRing`（`DoGFuchsiaColor`，大型，22帧）
    - `StrongBloom`（`DoGFuchsiaColor × 0.6`）
    - 15 个 SparkParticle（随机 Fuchsia/Cyan/Purple）
    - 5 条 DoGRiftCrack 辐射
    - 屏幕震动 8.5
  - 5 个 `DoGCyanEnergyBolt` 扇形（empActive 时 8 个）
  - 播放 `DevourerAttack.ogg`

**命中特效（OnHitNPC）：**
- 删除：原有冰晶链
- 改为：
  - 每次命中：DoG 体节爆裂感（2个 SparkParticle（Fuchsia/Cyan）+ 1个小型 `StrongBloom`）
  - 如果目标有 `DoGMarkDebuff`（现改名，逻辑保留）：
    - 引爆 DoG 裂隙：`PulseRing`（`DoGSpecialColor`，中型）+ 6个 SparkParticle
    - 从目标位置发射 3 个 `DoGCyanEnergyBolt`（向四面）

---

## 第三部分：QuickDraw（裂隙刺穿）

> DoG 感核心：「瞬间传送 + 裂隙贯穿，像 DoG 的传送突击」

**当前：** 右键在特定时机按下 → 触发高速刺击  
**改造后：** 触发等同于 DoGTeleportRift 的小型裂隙穿刺

### 起手演出（`BeginQuickDraw` 改造）：

删除：`DirectionalPulseRing（FrostGlowColor × 0.48）`

新增（完整 DoGTeleportRift 蓄力迷你版）：
1. `DirectionalPulseRing`（`DoGFuchsiaColor`，方向 = 攻击方向，内缩效果：`maxRadius` 从 1.2 缩到 0）
2. 8 个 `SparkParticle`（Cyan，从玩家周围向内聚拢，速度 = -方向 * 2.5）
3. 2 条 DoGRiftCrack（从玩家位置向两侧辐射，持续 5 帧）
4. `StrongBloom`（`DoGFuchsiaColor × 0.4`，origin = 玩家中心）
5. 屏幕震动 5.8（保留原值）
6. 音效：`DevourerRiftOpen.ogg`（替代 SoundID.Item119）
7. 玩家获得 8 帧无敌（保留）

### 刺穿阶段：

- 链条沿刺穿路径 spawn 连续 `LineParticle`（`DoGFuchsiaColor`，细，模拟 DoGFire trail）
- 到达最远点：
  - 完整 DoGTeleportRift 爆开（小号）：
    - 5 条 DoGRiftCrack（从尖端辐射）
    - `PulseRing`（`DoGFuchsiaColor`，中型）
    - 12 个 SparkParticle（全 Fuchsia，速度 5–15）
    - `StrongBloom`（`DoGFuchsiaColor × 0.65`）
    - 屏幕震动 6.2

### 刺穿沿途效果（QuickDrawBombs 系统改造）：

删除：`CosmicDischargeIceBomb` 冰炸弹  
改为：`DoGRiftBomb`（新建弹幕，视觉如同 DoGFire 火球）：
- 无贴图
- 短时存在（12–24帧）
- 爆炸时：2 个 SparkParticle（Cyan/Fuchsia）+ 1 个微型 PulseRing
- 伤害类型：`GodSlayerInferno` 3秒

### 命中特效（QuickDraw tip 命中）：

这是伤害最高的时机（×3.35），演出要相应最强：
- 完整 DoGTeleportRift 爆开套餐（和 ChainKnifeBiteAll 弧顶同级）
- 20 个 SparkParticle（最多，随机 Fuchsia/Cyan/Purple）
- 5 条 DoGRiftCrack
- 屏幕震动最强（10.0）
- `GodSlayerInferno` 7秒

---

## 第四部分：右键切换（Galaxia 风格 + DoG 裂隙演出）

> 这是整个改造中最有仪式感的部分

**当前机制：**  
- 右键（未命中 QuickDraw 条件时）→ 调用 `ToggleAttackMode()`，直接切换，没有任何视觉

**改造目标：**  
- 右键按下 → 生成一个纯视觉幽影弹幕（类比 `GalaxiaHoldout`）
- 幽影弹幕播放"裂隙传送门开合"动画
- 动画结束时（约 18帧）才真正完成模式切换
- 期间武器无法使用（防止切换+攻击重叠）

### 新建弹幕：`CosmicDischargeModeShift`

**参数：**
- `timeLeft = 18`
- `ai[0]` = 目标模式编号
- `tileCollide = false`，`friendly = false`，`CanDamage() = false`
- 位置：`player.Top`（参考 GalaxiaHoldout）

**AI（18帧完整演出）：**

**帧 0–6（传送门开启）：**
- 在玩家中心生成**三层叠旋传送门**（参考 StreamGougePortal）：
  - 黑底层（旋转正向）
  - 洋红层（旋转正向 × 0.6）
  - 青色层（旋转反向 × 0.7）
  - 随时间增大（scale 0 → 1.2）
- 同时：10 个 SparkParticle 从玩家四周向传送门汇聚（方向向内，颜色随机 Cyan/Fuchsia）
- 音效：`DemonSwordKillMode.ogg`（DevilsDevastation 右键同款，非常适合）

**帧 7–11（传送门最大）：**
- 传送门维持最大尺寸（scale 1.2）
- 每帧 spawn 2 个 SparkParticle 从传送门中心向外散射（DoGFuchsiaColor）
- 屏幕轻微震动（2.5 强度）

**帧 12–17（传送门关闭 + 模式切换生效）：**
- 帧 12：真正调用 `ToggleAttackMode()`（此时才切换）
- 传送门随时间缩小（scale 1.2 → 0）
- 爆发最终闪光：
  - `StrongBloom`（`DoGSpecialColor × 0.5`）
  - 6 个 SparkParticle（全 DoGSpecialColor，向外爆散）
  - 1 条 DoGRiftCrack（垂直）
- `DevourerRiftOpen.ogg` 短促版（PlaySound at low Volume 0.4）

**绘制（PreDraw）：**
```
三层叠旋纹理（和 StreamGougePortal 完全相同结构）：
  portalTexture = StreamGougePortal.png（可复用 CalamityMod 资源）
  rotation = GlobalTimeWrappedHourly * 8f（更快旋转，紧张感）
  
  layer 1: Color.Black × 0.55 * opacity, scale × 1.4, rotation
  layer 2: DoGCyanColor × 1.4 * opacity, scale × 1.4, rotation × 0.6
  layer 3: DoGFuchsiaColor × 1.4 * opacity, scale × 1.4, -rotation × 0.7
  
  中心额外: DoGSpecialColor bloom, 随 scale 脉冲
```

**武器侧（`NewLegendCosmicDischarge.cs` HoldItem 修改）：**
```csharp
if (player.Calamity().mouseRight && CanUseItem(player) && player.whoAmI == Main.myPlayer)
{
    // 防重复（Galaxia 同款）
    if (Main.projectile.Any(n => n.active &&
        n.type == ModContent.ProjectileType<CosmicDischargeModeShift>() &&
        n.owner == player.whoAmI))
        return;

    var source = player.GetSource_ItemUse(Item);
    CosmicDischargeAttackMode nextMode = GetNextMode(dischargePlayer.AttackMode);
    Projectile.NewProjectile(source, player.Top, Vector2.Zero,
        ModContent.ProjectileType<CosmicDischargeModeShift>(),
        0, 0, player.whoAmI, (float)nextMode);
}

// 切换逻辑从 ToggleAttackMode 中移出，改由弹幕 AI 在帧 12 时调用
```

### 武器贴图随模式变化（参考 GalaxiaDawn/GalaxiaDusk）

三个模式各有独立发光外框颜色：
- **Whip 模式**：武器图标外框 DoGCyanColor（青色轮廓）
- **Sword 模式**：武器图标外框 DoGFuchsiaColor（洋红轮廓）
- **ChainKnife 模式**：武器图标外框 DoGPurpleColor（紫色轮廓）

实现：在 `NewLegendCosmicDischarge.PostDrawInWorld` / `PreDrawInInventory` 根据当前 AttackMode 动态选择 glowmask 颜色绘制，不需要三张贴图，只用着色即可。

---

## 第五部分：大招重建（绝命激光墙）

> DoG 感核心：「这是 DoG 的激光墙进攻，整个世界都在燃烧」

**当前大招（`CosmicDischargeUltimateField`）：**
- 一个以玩家为中心的"冰雪圆场"
- 慢化周围敌人 + 拉起护盾
- 特效：蓝色光晕圆 + 节点粒子

**改造目标：**  
完全重建成 DoG 激光墙风格，但保留核心机制（玩家中心范围增益 + 慢化效果）

### 5.1 激活瞬间（Time == 1f）

删除：冰冻 Glacial Age 特效  
改为（完整 DoGTeleportRift 爆开 × 3 倍）：
1. `DoGRiftCrack` × 25 条（完全复刻 DoGTeleportRift.cs line 158）
   - 从玩家位置向外辐射，间距均匀
   - 长度 200–400px，颜色 Cyan/Fuchsia 随机
2. `PulseRing`（`DoGSpecialColor`，从玩家中心向外扩张，maxRadius = GlacialAgeRadius/80）
3. `StrongBloom`（`DoGFuchsiaColor × 0.7`，大型）
4. 40 个 SparkParticle（向外爆散，全色随机 Fuchsia/Cyan/Purple）
5. 屏幕震动 12.0
6. 对范围内（原 GlacialAgeRadius = 25×16 = 400px）所有敌人：
   - 施加 `GodSlayerInferno` 10秒（替代冰冻）
7. 音效：`DevourerRiftOpen.ogg` + `DevourerSpawn.ogg` 同时播放

### 5.2 持续期间（激光墙预警 30–60帧后循环）

**新增：激光墙预警循环系统**

每 90 帧触发一轮"激光墙警告"：
- 从玩家为中心，在 ±屏幕边缘位置生成 2–4 条预警线（`LineParticle`，Cyan × 0.35，低透明度）
- 30帧后，同位置的预警线转换为实际 `FriendlyLaserWallBeam`（向内扫射）
  - 伤害：`Projectile.damage * 0.4f`
  - 颜色：Cyan → Fuchsia
  - 屏幕震动 4.0

（注：这个系统完全参考 DoGLaserWalls 的预警→攻击流程，只是缩小到以玩家为中心）

**持续期间的场域绘制（替换 `PreDraw`）：**

删除：蓝色圆光晕 + 42个节点  
改为：
```
三层场域视觉：

Layer 1（内圈）：
  - 在玩家中心画一个小型 bloom circle（DoGFuchsiaColor × 0.18 * pulse）
  - 半径 = FieldRadius × 0.3

Layer 2（外圈激光预警环）：
  - 42个节点仍保留，但颜色改为随时间在 Cyan/Fuchsia 间切换
  - 每个节点是小型 bloom dot 而非像素点
  - 旋转速度 +0.5（更快）
  - 节点亮度随脉冲周期变化（0.25–0.55之间）

Layer 3（随机裂隙）：
  - 每 3帧在场域圆周上随机位置生成 1 条短 DoGRiftCrack（2–3帧消失）
  - 颜色：DoGSpecialColor × 0.4

Layer 4（中心涡旋）：
  - 在玩家中心绘制一个缩小的三层叠旋传送门（StreamGougePortal 同款）
  - 只有内圈版本（scale 0.35，不扩散）
  - 颜色：Cyan/Fuchsia/Black 三层
```

### 5.3 大招结束（timeLeft → 0）

当前：无任何结束演出  
改为：

```
最终爆发（参考 DoGDeathBoom 思路）：
1. 最后 10帧：场域逐渐缩小（radius × (timeLeft/10)）
2. timeLeft == 1：
   - PulseRing（DoGSpecialColor，大型，向外扩张）
   - StrongBloom（DoGFuchsiaColor × 0.5）
   - 15个 SparkParticle 向外散射
   - 屏幕震动 6.0
   - 音效：DoGLaserWallBigAttack.ogg（低音量 0.5）
```

---

## 第六部分：被动系统重建

**当前被动（`CosmicDischargeBuffs.cs` + `CosmicDischargeScytheProjectile.cs`）：**
- 触发条件 + 镰刀弹幕
- 特效：冰蓝色

**改造目标：** 镰刀弹幕视觉 DoG 化

### 6.1 镰刀弹幕（ScytheProjectile）

- 删除：冰蓝 glowmask
- 新增：双层 glowmask（Cyan 层 + Fuchsia 层，additive 叠加）
- 轨迹：DoG primitive trail 结构（外层 Cyan，内层 Fuchsia 细芯）
- 命中特效：
  - 2 个 SparkParticle（Cyan/Fuchsia 随机）
  - `GodSlayerInferno` 3秒

### 6.2 CosmicDischargeDoGMarkDebuff（新建，替代 FrostMarkDebuff）

**视觉：** 目标身上显示 DoG 风格的"吞噬印记"（Cyan 边框 + Fuchsia 核心的发光 buff icon）  
**机制（与原 FrostMark 完全对应）：**
- Whip 模式命中 → 施加 DoGMark（3秒）
- Sword 模式命中 DoGMark 目标 → 引爆裂隙（5条 DoGRiftCrack + PulseRing + 4个 DoGCyanEnergyBolt）
- Chain 模式命中 DoGMark 目标 → 爆发（PulseRing + 3个 DoGCyanEnergyBolt）

---

## 第七部分：新建弹幕清单

以下弹幕需要新建（替代旧冰系弹幕）：

| 新建弹幕 | 替代 | 视觉描述 | 参考 |
|---|---|---|---|
| `DoGCyanEnergyBolt` | IceBolt | 无贴图，Cyan primitive trail | DoGFire.cs |
| `DoGRiftBomb` | CosmicDischargeIceBomb | 无贴图，小型 PulseRing 爆炸 | DoGTeleportRift 简化版 |
| `DoGRiftCrack`（粒子弹幕） | （无对应） | 直线辐射 LineParticle，2–5帧 | DoGRiftCrack.cs |
| `DoGJawParticle` | CosmicIceBurst | 双侧光点合拢动画 | DoG 下颚咬合 |
| `CosmicDischargeModeShift` | （无） | 三层叠旋传送门，18帧演出 | GalaxiaHoldout + StreamGougePortal |

---

## 第八部分：改造优先级与实施顺序

### Phase 1（高优先，奠定基础）
1. ☐ `CosmicDischargeCommon.cs`：颜色系统全替换（删除 Frost 四色，加入 DoG 五色 + 动态色公式）
2. ☐ `CosmicDischargeCommon.cs`：`ApplyColdDebuffs` → `ApplyDoGDebuffs`（全换 GodSlayerInferno）
3. ☐ `CosmicDischargeComboHoldout.cs`：`SpawnHitEffects` 改色（SparkParticle 颜色 Fuchsia/Cyan）
4. ☐ `CosmicDischargeComboHoldout.cs`：`DrawCurvedBladeGlow` 改色（五层 DoG glow 结构）
5. ☐ 音效全替换（见音效表）

### Phase 2（核心演出）
6. ☐ `CosmicDischargeComboHoldout.cs`：`DrawSwordSmear` 改色（tipHistory 渐变 Fuchsia→Cyan）
7. ☐ `CosmicDischargeComboHoldout.cs`：SwordFinisher 蓄力 → DoG 传送蓄力特效
8. ☐ `CosmicDischargeComboHoldout.cs`：SwordFinisher 命中 → DoG 爆炸套餐（删冰雪弹幕）
9. ☐ `CosmicDischargeComboHoldout.cs`：WhipThrust 尖端命中 → DoG 咬合 20粒子
10. ☐ `CosmicDischargeComboHoldout.cs`：`BeginQuickDraw` → DoGTeleportRift 迷你版起手

### Phase 3（右键 + 大招）
11. ☐ 新建 `CosmicDischargeModeShift.cs`（18帧传送门切换弹幕）
12. ☐ `NewLegendCosmicDischarge.cs`：HoldItem 右键改为触发 ModeShift 弹幕
13. ☐ `CosmicDischargeUltimateField.cs`：激活瞬间改为 DoGRiftCrack × 25 爆发
14. ☐ `CosmicDischargeUltimateField.cs`：场域 PreDraw 改为三层 + 裂隙动态

### Phase 4（新弹幕 + 细节打磨）
15. ☐ 新建 `DoGCyanEnergyBolt.cs`（替代所有 IceBolt）
16. ☐ 新建 `DoGRiftBomb.cs`（替代 CosmicDischargeIceBomb）
17. ☐ 修改 `CosmicDischargeScytheProjectile.cs`（镰刀被动 DoG 化）
18. ☐ 修改 `CosmicDischargeBuffs.cs`（FrostMark → DoGMark）
19. ☐ SwordSwingTwo 分支：删冰刺 → 加 StreamGougePortal 式传送门 + 激光

---

## 附录：关键 API 速查

```csharp
// 动态 DoG 颜色
Color.Lerp(Color.Fuchsia, Color.Cyan,
    MathHelper.SmoothStep(0, 1, (MathF.Sin(Main.GlobalTimeWrappedHourly * 2) + 1) * 0.5f))

// 播放 DoG 音效
SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen")
    { Volume = 0.8f }, position);
SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack")
    { Volume = 0.6f }, position);
SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordKillMode")
    { Volume = 0.95f }, position);

// 裂隙涟漪
GeneralParticleHandler.SpawnParticle(new PulseRing(
    center, Vector2.Zero, DoGSpecialColor, 0.04f, 1.4f, 22));

// 方向冲击波
GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
    center, direction * 0.7f, DoGFuchsiaColor * 0.45f,
    Vector2.One, direction.ToRotation(), 0.035f, 0.22f, 14));

// GodSlayerInferno debuff
target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);

// FriendlyLaserWallBeam（全屏激光）
Projectile.NewProjectile(source,
    player.Center + direction * 2016f, direction,
    ModContent.ProjectileType<FriendlyLaserWallBeam>(),
    damage, kb, player.whoAmI, -1, 1);

// 三层叠旋传送门绘制（StreamGougePortal 同款）
Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
float r = Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 1.45f;
Main.EntitySpriteDraw(portal, pos, null, Color.Black with { A=0 } * 0.55f * opacity,  r,  origin, scale * 1.4f, 0, 0);
Main.EntitySpriteDraw(portal, pos, null, Color.Cyan  with { A=0 } * 1.4f * opacity,   r * 0.6f, origin, scale * 1.4f, 0, 0);
Main.EntitySpriteDraw(portal, pos, null, Color.Fuchsia with { A=0 } * 1.4f * opacity, -r * 0.7f, origin, scale * 1.4f, 0, 0);
```

---

*计划制定日期：2026-06-17*  
*本计划基于《神明吞噬者及其所有周边的特效完整分析.md》*
