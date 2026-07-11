# 极地之灵 (Cryogen) - Calamity Mod 原版 AI 深度分析报告

## 一、 概述与设计架构

在 **Calamity Mod** 官方原版中，极地之灵（Cryogen）被设计为一个**多阶段碰撞冲撞型与弹幕铺场混合型**的 Boss。它的战斗非常考验玩家的横向拉扯能力和机动性，特别是在复仇模式（Revengeance）和死亡模式（Death）中，其频繁的高速传送冲撞是其主要杀招。

### 核心设计支柱
1.  **护盾无敌机制 (Shield Invulnerability)**：Boss 自身并不直接承伤，而是周期性生成一个名为 `CryogenShield` 的独立随从 NPC。当护盾存活时，极地之灵本体处于无敌状态（`dontTakeDamage = true`）。
2.  **逐层破壳 (Phase Textures)**：随着血量降低，极地之灵会更换其贴图（由 Phase 1 依次切换至 Phase 6），代表外层坚冰的逐渐剥离。
3.  **突进与传送 (Charges & Teleports)**：Boss 的移动极具侵略性，拥有长距离的高速冲撞和预测性传送定位。

---

## 二、 代码文件结构与基础信息

在灾厄源码中，极地之灵的逻辑封装在以下类中：
*   **Boss 本体类**: `CalamityMod.NPCs.Cryogen.Cryogen`（继承自 `ModNPC`）
*   **护盾随从类**: `CalamityMod.NPCs.Cryogen.CryogenShield`（继承自 `ModNPC`）
*   **关联图层与纹理**:
    *   `Cryogen_Phase1` 至 `Cryogen_Phase6` (本体各阶段贴图)
    *   `CryogenShield` (护盾贴图)
    *   `Cryogen_Phase1_Head_Boss` 与 `Pyrogen_Head_Boss` (小地图 Boss 头标)
*   **原生发射弹幕**:
    *   `IceBlast`（冰爆弹）
    *   `IceBomb`（冰炸弹）
    *   `IceRain`（冰雨）
    *   *Zenith/GFB 种子下替换为*: `BrimstoneBarrage`（硫磺弹雨）与 `SCalBrimstoneFireblast`（灾厄至尊火球）

---

## 三、 多维度阶段与血量阈值划分

灾厄原版极地之灵的阶段触发极为复杂，会根据当前世界的难度（普通、专家、复仇、死亡）动态调整血量阈值：

| 阶段 (Phase) | 对应贴图 (Asset) | 普通模式 HP | 复仇模式 HP | 死亡模式 HP | 招式变化与解锁 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Phase 1** | `Cryogen_Phase1` | 100% - 80% | 100% - 85% | 立即跳过 | 慢速飞向玩家，发射 16 向 `IceBlast` 环形弹。 |
| **Phase 2** | `Phase2Texture` | 80% - 60% | 85% - 70% | 100% - 80% | 解锁冰雨（`IceRain`）和护盾冲撞。 |
| **Phase 3** | `Phase3Texture` | 60% - 40% | 70% - 55% | 80% - 60% | 弹幕速度提升，冲撞准备时间缩短。 |
| **Phase 4** | `Phase4Texture` | 40% - 30% | 55% - 45% | 60% - 50% | 频繁在玩家斜上方进行中距离传送，伴随抛射冰弹。 |
| **Phase 5** | `Phase5Texture` | 30% - 0% | 45% - 25% | 50% - 35% | 解锁冰炸弹（`IceBomb`）大范围散布，冲撞速度最大。 |
| **Phase 6** | `Phase6Texture` | N/A (不触发) | 25% - 15% | 35% - 25% | 护盾破碎时间变长，冲撞和传送交替周期降低至 1.5 秒。 |
| **Phase 7** | `Phase6Texture` | N/A (不触发) | 15% - 0% | 25% - 0% | 终极狂暴。无护盾生成，Boss 疯狂锁定玩家进行超高速对冲。 |

---

## 四、 核心 AI 行为机制与数学公式

### 1. 基础物理悬停 (Normal Hover Movement)
当 Boss 处于非冲撞、非传送状态时，其运动采用经典的阻尼插值跟踪：
```csharp
float playerXDist = player.Center.X - cryogenCenter.X;
float playerYDist = player.Center.Y - cryogenCenter.Y;
float playerDistance = Math.Sqrt(playerXDist * playerXDist + playerYDist * playerYDist);

// 根据难度和阶段决定速度上限
float cryogenSpeed = death ? 7f : revenge ? 5f : 4f; 
playerDistance = cryogenSpeed / playerDistance;
playerXDist *= playerDistance;
playerYDist *= playerDistance;

float inertia = 50f; // 惯性系数，越大移动越迟缓
if (Main.getGoodWorld)
    inertia *= 0.5f; // GFB 难度下惯性减半，追踪极度灵敏

NPC.velocity.X = (NPC.velocity.X * inertia + playerXDist) / (inertia + 1f);
NPC.velocity.Y = (NPC.velocity.Y * inertia + playerYDist) / (inertia + 1f);
```

### 2. 传送逻辑 (Teleportation Math)
Boss 在进入第 4 阶段后会开始频繁传送。
*   **传送触发**：当 `NPC.ai[1]` 计数器达到特定间隔（受难度缩放，在 300 帧左右）时触发传送。
*   **落点选择**：以玩家位置为基准，向玩家移动方向的相反侧（或随机斜角）偏移 350 - 500 像素，并在目标点生成大量冰晶微粒。
*   **同步与防卡死**：传送前清空 `NPC.velocity`，并记录 `teleportLocationX` 进行多端网络同步（`BinaryWriter` 写入）。

### 3. 盾牌冲撞 (Shield Dash Attack)
这是极地之灵最危险的杀招。冲撞分为三个子阶段：
1.  **蓄力蓄能 (Telegraphing Phase)**：
    *   Boss 速度在 45 帧内线性减慢，最终近乎静止悬停在空中。
    *   自转速度 `NPC.rotation` 暴增，且伴随着刺耳的破冰尖叫声。
2.  **暴力突刺 (Dash Phase)**：
    *   瞬间向玩家当前方向射出。
    *   最大冲刺速度 `chargeVelocityMax` 在 Revengeance 难度下高达 **30f/帧**（在 GFB 下可达 **42f/帧**）。
    *   冲刺持续约 60 帧，此期间 Boss 的碰撞伤害判定提升 1.5 倍。
3.  **线性收招 (Slowdown Phase)**：
    *   冲刺结束后，Boss 速度以每帧 `* 0.92` 的阻尼线性衰减，直到其速度低于 4f 时，重新切回普通悬停逻辑。

---

## 五、 FTW 与 GFB 种子（Get Good World）特异性逻辑

当极地之灵在 FTW（For the Worthy）或 GFB（Zenith）种子世界中生成时，会触发以下底层代码级的特异性重载：

1.  **形体急剧缩小**：
    `NPC.scale *= 0.8f`。由于碰撞体积变小，避让空间被压缩，但追踪时惯性受体型影响极小。
2.  **属性抗性反转**：
    在常规世界中，极地之灵易燃（`VulnerableToHeat = true`），对冰免疫。但在 GFB（天顶世界）中，由于其化身为带有火焰属性的 **Pyrogen（火之灵）**，它的抗性彻底反转：
    *   `VulnerableToHeat = false`
    *   `VulnerableToCold = true` (弱冰)
    *   `VulnerableToWater = true` (弱水)
3.  **地狱之魔随从召唤**：
    在 GFB 模式下，Boss 会在开场和阶段转换时，在地表检测并强制生成一只**冰雪巨人 (Ice Golem)** 或在 Zenith 种子下生成**红魔鬼 (Red Devil)**。如果随从被击杀，Boss 会立刻开启寻路在 900 帧后重新召唤，施加极强的场外干扰。
4.  **弹幕全面红化 (Pyrogen Theme)**：
    小地图图标切换为 `Pyrogen_Head_Boss`。弹幕全部转为火焰粒子，冰弹转为灾至尊同款火球，全屏伤害极高。
