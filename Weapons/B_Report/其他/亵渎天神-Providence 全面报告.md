# 亵渎天神——普罗维登斯 (Providence, the Profaned Goddess) 细节报告

本报告系统性地整理了《泰拉瑞亚》灾厄模组（Calamity Mod）中后期关键 Boss——**亵渎天神**的所有核心机制、音效资源路径以及弹幕细节。

---

## 一、 Boss 基本属性与设定
*   **英文名称**：Providence, the Profaned Goddess (旧称 Providence, the Profaned God)
*   **中文名称**：亵渎天神 / 普罗维登斯
*   **生成方式**：在**白天**的神圣之地或地狱使用**亵渎晶核 (Profaned Core)** 召唤。
*   **特别机制**：
    *   **无接触伤害**：亵渎天神本体不具备接触伤害，完全依赖密集的弹幕地狱与召唤物进行攻击。
    *   **环境限制**：如果离开召唤所在的生物群落（神圣之地或地狱）过久，她将进入无敌状态。
    *   **恶意模式 (夜晚)**：在夜晚召唤她会触发完美怒火（Enraged），其弹幕颜色会变为夜间专属贴图，且难度极大幅度提升。

---

## 二、 释放音效详细对应表
在游戏的音频定义中，亵渎天神及其战斗机制涉及了多种自定义音效（`.ogg` / `.wav`）。以下为完整的音效映射：

| 音效中文名称 | 游戏内所用音效/文件路径 (相对 `CalamityMod` 路径) | 详细说明 |
| :--- | :--- | :--- |
| **受伤** | `Sounds/NPCHit/ProvidenceHurt.ogg` | 受到伤害时播放的受击音效 |
| **死亡** | `Terraria/Sounds/NPC_Killed_55.wav` | 生命值归零彻底死亡时播放的声效 (Vanilla 默认的 Moon Lord 死亡吼叫声 `SoundID.NPCDeath55`) |
| **生成** | `Sounds/Custom/Providence/ProvidenceSpawn.ogg` | 降临/出现时的降临声效 |
| **发射弹幕** | `Sounds/Custom/Providence/ProvidenceHolyBlastShoot.ogg` | 发射圣之爆炎（Holy Blast）时的发射声效 |
| **圣之耀光** | `Sounds/Custom/Providence/ProvidenceHolyRay.ogg` | 释放圣之激光柱（扫射激光）时的声效 |
| **圣之爆炎** | `Sounds/Custom/Providence/ProvidenceHolyBlastShoot.ogg` | 指圣之爆炎（Holy Blast）的弹幕音效 |
| **圣之爆炎爆炸** | `Sounds/Custom/Providence/ProvidenceHolyBlastImpact.ogg` | 圣之爆炎（Holy Blast）在触碰玩家或消散时发生的爆炸音效 |
| **发射神圣新星** | `Terraria/Sounds/Item_20.wav` (vanilla) | 释放神圣炸弹（Holy Bomb）阶段的发射音效 |
| **神圣新星爆炸** | `Terraria/Sounds/Item_100.wav` / `DD2_BetsyFireballImpact` (vanilla) | 神圣新星炸裂时的爆炸声 |
| **发射神圣之矛** | `Terraria/Sounds/Item_74.wav` / `Item_105.wav` (vanilla) | 释放神圣之矛（Holy Spear）时的蓄力发射声 |
| **圣狱神火警示** | `Sounds/Custom/Providence/ProvidenceBurn.ogg` | 玩家距离 Boss 过远、进入圣狱神火（Holy Inferno）边缘警告时的灼烧前兆声 |
| **圣狱神火燃烧** | `Sounds/Custom/Providence/ProvidenceBurnLoop.ogg` | 玩家在圣狱神火（Holy Inferno）范围外持续受到高额灼烧伤害时的循环声效 |
| **召唤亵渎守卫** | `Terraria/Sounds/Item_105.wav` (vanilla) | 战斗进行中，亵渎天神进入茧形状态并召唤三只亵渎守卫时的声效 |
| **亵渎守卫光环** | `Sounds/Custom/ProfanedGuardians/GuardianRockShieldActivate.ogg` | 守护者为 Boss 开启神圣岩石护盾时的音效 |
| **亵渎守卫治疗** | `Sounds/Custom/ProfanedGuardians/GuardianHeal.ogg` | 治疗型守护者在场对 Boss 进行圣光回血时的治疗声效 |
| **死亡动画** | `Sounds/Custom/Providence/ProvidenceDeathAnimation.ogg` | 生命值归零后，处于逐渐崩裂、释放圣光能量（约 6 秒）死亡演出时的音效 |

---

## 三、 弹幕 (Projectiles) 详细对照表
以下是亵渎天神在不同战斗阶段（包含白天常态与夜晚恶意模式）所释放的弹幕、中文对照以及其对应的贴图路径：

| 弹幕英文名称 | 弹幕中文名称 | 游戏内类名 | 材质贴图路径 (相对 `CalamityMod` 路径) |
| :--- | :--- | :--- | :--- |
| **Holy Spear** | 神圣之矛 / 圣矛 | `HolySpear` | `Projectiles/Boss/HolySpear.png` |
| **Holy Blast** | 圣之爆炎 / 圣安爆破 | `HolyBlast` | `Projectiles/Boss/HolyBlast.png`<br>`Projectiles/Boss/HolyBlastNight.png` |
| **Holy Blast Fragment** | 圣之爆炎碎片 | `HolyBlastFrags` | *无独立贴图，由代码粒子生成绘制* |
| **Holy Orb** | 闪耀之弹 | `HolyBurnOrb` | `Projectiles/StarProj.png` *(公用贴图)* |
| **Holy Light** | 圣之烁光 | `HolyLight` | `Projectiles/StarProj.png` *(公用贴图)* |
| **Holy Ray** | 圣之激光 / 圣之耀光 | `ProvidenceHolyRay` | `Projectiles/Boss/ProvidenceHolyRay.png`<br>`Projectiles/Boss/ProvidenceHolyRayNight.png` |
| **Holy Crystal** | 圣洁水晶 | `ProvidenceCrystal` | `Projectiles/Boss/ProvidenceCrystal.png` |
| **Holy Crystal Shard** | 圣洁碎晶 | `ProvidenceCrystalShard` | `Projectiles/Boss/ProvidenceCrystalShard.png` |
| **Holy Fire** | 神圣之火 / 圣火 | `HolyFire` / `HolyFire2` | `Projectiles/Boss/HolyFire.png` / `HolyFire2.png`<br>`Projectiles/Boss/HolyFireNight.png` / `HolyFire2Night.png` |
| **Holy Flare** | 神圣耀斑 / 圣耀斑 | `HolyFlare` | `Projectiles/Boss/HolyFlare.png`<br>`Projectiles/Boss/HolyFlareNight.png` |
| **Holy Bomb** | 神圣新星 / 神圣炸弹 | `HolyBomb` | `Projectiles/Boss/HolyBomb.png`<br>`Projectiles/Boss/HolyBombNight.png` |
| **Holy Aura** | 神圣光环 | `HolyAura` | `Projectiles/Boss/HolyAura.png` |
| **Molten Blast** | 熔岩爆裂 | `MoltenBlast` | `Projectiles/Boss/MoltenBlast.png`<br>`Projectiles/Boss/MoltenBlastNight.png` |
| **Molten Blob** | 熔岩球 | `MoltenBlob` | `Projectiles/Boss/MoltenBlob.png`<br>`Projectiles/Boss/MoltenBlobNight.png` |
| **Swirling Fire** | 旋火粒子 | `SwirlingFire` | *代码粒子绘制* |
| **Majestic Sparkle** | 庄严烁光 | `MajesticSparkle` | *代码粒子绘制* |

---

## 四、 核心攻击阶段与逃避细节
1.  **爆焱阶段**
    *   **机制**：发射巨大的圣之爆炎（`HolyBlast`），在其消散或撞击后分裂成多个追踪的神圣之火（`HolyBlastFrags`）袭向玩家。
2.  **茧形阶段**
    *   **机制**：在此阶段，Boss 获得 90% 的超高伤害减免。她会停在空中释放神圣新星（`HolyBomb`），偶尔掺杂绿色的治愈新星，玩家碰撞绿色新星可以恢复生命。
3.  **极光阶段 (白天)**
    *   **机制**：Boss 在白天血量降至 75% 以下时触发。释放大范围、缓慢横扫的圣之耀光（`ProvidenceHolyRay`），造成极高的防具穿透伤害。
4.  **水晶阶段 (地狱)**
    *   **机制**：Boss 在地狱血量降至 75% 以下时触发。在头顶召唤圣洁水晶（`ProvidenceCrystal`），向玩家泼洒受重力影响的碎晶片（`ProvidenceCrystalShard`）。
