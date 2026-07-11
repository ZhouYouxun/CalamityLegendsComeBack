# Gael's Greatsword 与 Supreme Witch, Calamitas 贴图/弹幕路径报告

数据来源：本地 `CalamityMod` 源码与 PNG 文件尺寸实查。内部路径按 tModLoader 资源路径写法记录，通常不带 `.png`；文件路径列保留 `.png` 方便直接定位。

## 1. Boss 与掉落来源

- Boss 内部名：`SupremeCalamitas`
- 类文件：`CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitas.cs`
- 常规掉落武器组来源：`ModifyNPCLoot()` 中的 `weapons` 数组，7 把武器走 `DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, weapons)`。
- 常规掉落组：`Violence`、`Condemnation`、`Heresy`、`Vehemence`、`Perdition`、`Vigilance`、`Sacrifice`
- Death 模式额外掉落：`GaelsGreatsword`
- 宝藏袋对应：`Items/TreasureBags/CalamitasCoffer.cs` 中同样列出这 7 把常规武器。

Boss 本体/相关 NPC 贴图：

| 对象 | 内部名 | 内部路径 | 文件尺寸 | 帧数备注 |
|---|---|---|---:|---|
| 至尊灾厄本体 | `SupremeCalamitas` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitas` | 120x1260 | NPC 多帧，由 NPC frame 逻辑控制 |
| 至尊灾厄兜帽形态 | `SupremeCalamitasHooded` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitasHooded` | 120x1302 | NPC 多帧 |
| 头像 | `HoodlessHeadIcon` | `CalamityMod/NPCs/SupremeCalamitas/HoodlessHeadIcon` | 36x40 | 单图 |
| 头像 | `HoodedHeadIcon` | `CalamityMod/NPCs/SupremeCalamitas/HoodedHeadIcon` | 36x40 | 单图 |
| Soul Seeker | `SoulSeekerSupreme` | `CalamityMod/NPCs/SupremeCalamitas/SoulSeekerSupreme` | 96x780 | NPC 多帧 |
| Soul Seeker 发光层 | `SoulSeekerSupremeGlow` | `CalamityMod/NPCs/SupremeCalamitas/SoulSeekerSupremeGlow` | 96x780 | 与本体帧同步 |
| Sepulcher 头 | `SepulcherHead` | `CalamityMod/NPCs/SupremeCalamitas/SepulcherHead` | 62x88 | 单段 NPC |
| Sepulcher 身体 | `SepulcherBody` | `CalamityMod/NPCs/SupremeCalamitas/SepulcherBody` | 82x72 | 单段 NPC |
| Sepulcher 身体变体 | `SepulcherBodyAlt` | `CalamityMod/NPCs/SupremeCalamitas/SepulcherBodyAlt` | 86x82 | 单段 NPC |
| Sepulcher 尾 | `SepulcherTail` | `CalamityMod/NPCs/SupremeCalamitas/SepulcherTail` | 54x54 | 单段 NPC |
| Supreme Cataclysm | `SupremeCataclysm` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCataclysm` | 636x1872 | NPC 多帧 |
| Supreme Cataclysm 发光层 | `SupremeCataclysmGlow` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCataclysmGlow` | 636x1872 | 与本体帧同步 |
| Supreme Catastrophe | `SupremeCatastrophe` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCatastrophe` | 800x1840 | NPC 多帧 |
| Supreme Catastrophe 发光层 | `SupremeCatastropheGlow` | `CalamityMod/NPCs/SupremeCalamitas/SupremeCatastropheGlow` | 800x1840 | 与本体帧同步 |

## 2. 掉落武器本体贴图

| 武器显示名 | 内部名 | 物品类文件 | 物品贴图内部路径 | 文件尺寸 | 帧数 |
|---|---|---|---|---:|---|
| Gael's Greatsword | `GaelsGreatsword` | `Items/Weapons/Melee/GaelsGreatsword.cs` | `CalamityMod/Items/Weapons/Melee/GaelsGreatsword` | 108x100 | 1 |
| Violence | `Violence` | `Items/Weapons/Melee/Violence.cs` | `CalamityMod/Items/Weapons/Melee/Violence` | 142x142 | 1 |
| Condemnation | `Condemnation` | `Items/Weapons/Ranged/Condemnation.cs` | `CalamityMod/Items/Weapons/Ranged/Condemnation` | 130x42 | 1 |
| Heresy | `Heresy` | `Items/Weapons/Magic/Heresy.cs` | `CalamityMod/Items/Weapons/Magic/Heresy` | 48x312 | 6 帧，`DrawAnimationVertical(6, 6)` |
| Vehemence | `Vehemence` | `Items/Weapons/Magic/Vehemence.cs` | `CalamityMod/Items/Weapons/Magic/Vehemence` | 116x114 | 1 |
| Perdition | `Perdition` | `Items/Weapons/Summon/Perdition.cs` | `CalamityMod/Items/Weapons/Summon/Perdition` | 56x100 | 源码未注册动画，按 1 帧使用 |
| Vigilance | `Vigilance` | `Items/Weapons/Summon/Vigilance.cs` | `CalamityMod/Items/Weapons/Summon/Vigilance` | 104x98 | 源码未注册动画，按 1 帧使用 |
| Sacrifice | `Sacrifice` | `Items/Weapons/Rogue/Sacrifice.cs` | `CalamityMod/Items/Weapons/Rogue/Sacrifice` | 70x68 | 1 |

Gael 额外 UI 贴图：

| 用途 | 内部路径 | 文件尺寸 |
|---|---|---:|
| Gael rage 冷却图标 | `CalamityMod/Cooldowns/GaelsRage` | 22x22 |
| Gael rage 覆盖层 | `CalamityMod/Cooldowns/GaelsRageOverlay` | 22x22 |
| Gael rage 外框 | `CalamityMod/Cooldowns/GaelsRageOutline` | 26x26 |

## 3. 武器发射/召唤弹幕贴图链

### Gael's Greatsword

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `GaelSkull` | `Projectiles/Melee/GaelSkull.cs` | `CalamityMod/Projectiles/Melee/GaelSkull` | 60x300 | 5 | 普通左键按挥砍计数生成；小骷髅会寻敌，巨型骷髅 `ai1=1` 慢速变大并渐隐，死亡时造成 240x240 爆炸判定。 |
| `GaelSkull2` | `Projectiles/Melee/GaelSkull2.cs` | 复用 `CalamityMod/Projectiles/Melee/GaelSkull` | 60x300 | 5 | Rage 替代技能在 `CalamityPlayer` 中生成一圈骷髅，先抛射再寻敌，死亡同样爆炸。 |
| `GaelExplosion` | `Projectiles/Melee/GaelExplosion.cs` | `CalamityMod/Projectiles/InvisibleProj` | 1x1 | 1 | 低血量挥砍时小概率生成的隐形爆炸判定，靠血雨 dust 表现。 |
| `LightningThing` | `Projectiles/Melee/LightningThing.cs` | `CalamityMod/Projectiles/InvisibleProj` | 1x1 | 1 | 低血量且场上少于 3 个时生成，90 tick 后释放 3 个原版 `ProjectileID.CultistBossLightningOrbArc`，改成友方近战伤害。 |

### Violence

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 额外绘制贴图 | 逻辑简述 |
|---|---|---|---:|---|---|---|
| `ViolenceThrownProjectile` | `Projectiles/Melee/ViolenceThrownProjectile.cs` | `CalamityMod/Items/Weapons/Melee/Violence` | 142x142 | 1 | `CalamityMod/ExtraTextures/Trails/SylvestaffStreak` 256x256 | 左键是可控回收的 yoyo/长柄武器形态；右键是高速投枪。命中时有 hitstop、血液/火花粒子和拖尾。 |

备注：`ViolenceSlashProjectile` 存在，贴图同 `Violence`，额外使用 `CalamityMod/ExtraTextures/Trails/SwordSlashTexture` 256x256，但当前源码中没有其它文件引用/发射它，报告不把它算进当前掉落武器实际发射链。

### Condemnation

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `CondemnationHoldout` | `Projectiles/Ranged/CondemnationHoldout.cs` | 继承 `BaseGunHoldoutProjectile`，实际使用关联物品 `CalamityMod/Items/Weapons/Ranged/Condemnation` | 130x42 | 1 | 持弓蓄力，最多装填 9 支箭；装满后进入 homing 状态并发光。 |
| `CondemnationArrow` | `Projectiles/Ranged/CondemnationArrow.cs` | `CalamityMod/Projectiles/Ranged/CondemnationArrow` | 26x90 | 1 | 直线高速箭，带 9 段 afterimage；每 90 tick 向两侧释放 homing arrow。 |
| `CondemnationArrowHoming` | `Projectiles/Ranged/CondemnationArrowHoming.cs` | 复用 `CalamityMod/Projectiles/Ranged/CondemnationArrow` | 26x90 | 1 | 自动追踪 1500 像素内最近 NPC，渐隐并带 afterimage。 |

### Heresy

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `HeresyProj` | `Projectiles/Magic/HeresyProj.cs` | `CalamityMod/Projectiles/Magic/HeresyProj` | 28x176 | 8 | 持书通道弹幕，不直接造成伤害；按时间加快翻页，每隔约 20-28 tick 随强度释放火/魂类弹幕。 |
| `RedirectingFire` | `Projectiles/Magic/RedirectingFire.cs` | `CalamityMod/Projectiles/InvisibleProj` | 1x1 | 1 | 隐形火弹，主要靠 dust 表现，18 tick 后开始寻敌并加速。 |
| `RedirectingLostSoul` | `Projectiles/Magic/RedirectingLostSoul.cs` | `CalamityMod/Projectiles/Magic/RedirectingLostSoul` | 62x144 | 4 | 小魂弹，延迟后寻敌，带 10 段拖尾绘制。 |
| `RedirectingVengefulSoul` | `Projectiles/Magic/RedirectingVengefulSoul.cs` | `CalamityMod/Projectiles/Magic/RedirectingVengefulSoul` | 80x232 | 4 | 较强魂弹，复用公共 `DoSoulAI`，延迟追踪并多层 afterimage。 |
| `RedirectingGildedSoul` | `Projectiles/Magic/RedirectingGildedSoul.cs` | `CalamityMod/Projectiles/Magic/RedirectingGildedSoul` | 66x184 | 4 | 金色魂弹，速度权重更高，死亡时额外播放熄灭音效。 |

### Vehemence

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `VehemenceHoldout` | `Projectiles/Magic/VehemenceHoldout.cs` | `CalamityMod/Items/Weapons/Magic/Vehemence` | 116x114 | 1 | 法杖持握/蓄力弹幕，到达 `ChargeTime` 后发射 `VehemenceBolt`。 |
| `VehemenceBolt` | `Projectiles/Magic/VehemenceBolt.cs` | `CalamityMod/Projectiles/Magic/VehemenceBolt` | 32x96 | 源码未设置 `Main.projFrames`，按 1 帧使用 | 直线魔法弹，带 10 段 afterimage 和螺旋 brimstone dust；死亡时生成 18 个 skull。 |
| `VehemenceSkull` | `Projectiles/Magic/VehemenceSkull.cs` | `CalamityMod/Projectiles/Magic/VehemenceSkull` | 40x560 | 10 | 爆裂后散开的骷髅余焰，前 4 帧循环，后段播放消散帧并自毁。 |

### Perdition

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 额外绘制贴图 | 逻辑简述 |
|---|---|---|---:|---|---|---|
| `PerditionBeacon` | `Projectiles/Summon/PerditionBeacon.cs` | `CalamityMod/Projectiles/Summon/PerditionBeacon` | 54x1376 | 16 | `CalamityMod/Projectiles/Summon/PerditionCross` 30x40 | 哨兵型召唤物，跟随玩家上方；只有玩家指定召唤目标时才攻击。攻击时在目标处绘制十字标记并发射魂弹。 |
| `LostSoulGold` | `Projectiles/Summon/LostSoulGold.cs` | `CalamityMod/Projectiles/Summon/LostSoulGold` | 66x184 | 4 | 无额外贴图 | Perdition 随机发射的金色魂弹，延迟后召唤物寻敌。 |
| `LostSoulGiant` | `Projectiles/Summon/LostSoulGiant.cs` | `CalamityMod/Projectiles/Summon/LostSoulGiant` | 80x232 | 4 | 无额外贴图 | 大魂弹，公共 `DoSoulAI`，15 tick 后追踪。 |
| `LostSoulLarge` | `Projectiles/Summon/LostSoulLarge.cs` | `CalamityMod/Projectiles/Summon/LostSoulLarge` | 80x232 | 4 | 无额外贴图 | 大号魂弹变体，缩放 0.75。 |
| `LostSoulSmall` | `Projectiles/Summon/LostSoulSmall.cs` | `CalamityMod/Projectiles/Summon/LostSoulSmall` | 62x144 | 4 | 无额外贴图 | 小魂弹变体。 |

### Vigilance

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `SeekerSummonProj` | `Projectiles/Summon/SeekerSummonProj.cs` | `CalamityMod/Projectiles/Summon/SeekerSummonProj` | 88x520 | 5 | Soul Seeker 召唤物，平时环绕玩家；发现目标后短暂停顿并周期性发射 brimstone dart。 |
| `BrimstoneDartSummon` | `Projectiles/Summon/BrimstoneDartSummon.cs` | 复用 `CalamityMod/Projectiles/Boss/BrimstoneBarrage` | 18x176 | 4 | 召唤物射出的 brimstone dart，带短 afterimage，命中施加 Brimstone Flames。 |

### Sacrifice

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `SacrificeProjectile` | `Projectiles/Rogue/SacrificeProjectile.cs` | `CalamityMod/Items/Weapons/Rogue/Sacrifice` | 70x68 | 1 | 投出后可插入敌人；右键召回，返回玩家时按普通/潜伏打击治疗。带 8 段 afterimage。 |

## 4. Boss 战斗弹幕贴图链

`SupremeCalamitas.cs` 开头把主要弹幕类型缓存为：

- `bulletHellblast`：普通为 `BrimstoneHellblast2`，GFB/zenith 分支为 `BrimstoneWave`
- `barrage`：`BrimstoneBarrage`
- `gigablast`：普通为 `SCalBrimstoneGigablast`，GFB/zenith 分支为 `SCalBrimstoneFireblast`
- `fireblast`：普通为 `SCalBrimstoneFireblast`，GFB/zenith 分支为 `SCalBrimstoneGigablast`
- `wave`：普通为 `BrimstoneWave`，GFB/zenith 分支为 `BrimstoneHellblast2`
- `hellblast`：普通为 `BrimstoneHellblast`，GFB/zenith 分支为 `BrimstoneWave`

### Supreme Calamitas 主体直接生成/使用的弹幕

| 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `BrimstoneBarrage` | `Projectiles/Boss/BrimstoneBarrage.cs` | `CalamityMod/Projectiles/Boss/BrimstoneBarrage` | 18x176 | 4 | Brimstone dart。按 `ai` 可能加速/短时追踪，SCal/Seeker/Sepulcher 都会使用。 |
| `BrimstoneWave` | `Projectiles/Boss/BrimstoneWave.cs` | `CalamityMod/Projectiles/Boss/BrimstoneWave` | 48x128 | 4 | 横向飞行并让 Y 速度按正弦波摆动；淡入淡出，Permafrost 分支会改成冰蓝色。 |
| `BrimstoneHellblast` | `Projectiles/Boss/BrimstoneHellblast.cs` | `CalamityMod/Projectiles/Boss/BrimstoneHellblast` | 54x176 | 4 | 常规 hellblast，逐渐加速，尾部可生成螺旋火花粒子。 |
| `BrimstoneHellblast2` | `Projectiles/Boss/BrimstoneHellblast2.cs` | `CalamityMod/Projectiles/Boss/BrimstoneHellblast2` | 54x176 | 4 | bullet hell 用长寿命版本，额外更新、淡入淡出；专家/复仇时部分模式会继续加速。 |
| `SCalBrimstoneFireblast` | `Projectiles/Boss/SCalBrimstoneFireblast.cs` | `CalamityMod/Projectiles/Boss/SCalBrimstoneFireblast` | 36x250 | 5 | 小型追踪火球。接近玩家或寿命结束后停下、爆开；死亡时按难度释放 8-16 个 `BrimstoneBarrage`。 |
| `SCalBrimstoneGigablast` | `Projectiles/Boss/SCalBrimstoneGigablast.cs` | `CalamityMod/Projectiles/Boss/SCalBrimstoneGigablast` | 52x492 | 6 | 大型追踪火球。爆开时按难度释放 20-36 个 `BrimstoneBarrage`。 |
| `PermafrostMeat` | `Projectiles/Boss/PermafrostMeat.cs` | `CalamityMod/Projectiles/Boss/PermafrostMeat` | 32x30 | 1 | Permafrost 分支投掷物，命中/死亡后可分裂成 3 个较小同类弹幕。 |
| `PermafrostAbsoluteZeroProjectile` | `Projectiles/Boss/PermafrostAbsoluteZeroProjectile.cs` | `CalamityMod/Items/Weapons/Melee/AbsoluteZero` | 58x56 | 源码未设置 `Main.projFrames`，按 1 帧绘制 | Permafrost 持续链锯/冰刃 holdout，跟随 Permafrost 手部瞄准玩家，周期性释放 `PermafrostColdheartIcicle`。 |
| `PermafrostBlaster` | `Projectiles/Boss/PermafrostBlaster.cs` | `CalamityMod/Items/Accessories/PermafrostsConcoction` | 40x38 | 1 | Gaster Blaster 风格预警弹幕，本体不接触伤害，蓄力后生成 `PermafrostBlast` 激光。 |
| `DarkIceZero` | `Projectiles/Melee/DarkIceZero.cs` | `CalamityMod/Projectiles/Melee/DarkIceZero` | 22x46 | 1 | Permafrost/后段 AI 使用的冰弹；前 5 tick 不绘制，速度不足时加速，死亡产生大范围冰爆。 |
| `BrimstoneMonster` | `Projectiles/Boss/BrimstoneMonster.cs` | 主体 `CalamityMod/Projectiles/InvisibleProj`，另见下方绘制贴图 | 1x1 | 1 | 巨型追踪/压迫场，真正视觉由 shader、脸、旋涡、bloom 或 HAGE 贴图绘制；接触时按重叠/玩家速度持续扣血并清增益。 |
| `ProjectileID.BouncyBoulder` | 原版弹幕 | `Terraria/Images/Projectile_<BouncyBoulder ID>` | 原版资源 | 原版 | 在特定弹幕地狱段从 SCal 中心随机抛出，属于原版弹幕 ID，非 CalamityMod PNG。 |

`BrimstoneMonster` 额外绘制贴图：

| 用途 | 内部路径 | 文件尺寸 |
|---|---|---:|
| 代码中存在但主要绘制被注释的本体图 | `CalamityMod/Projectiles/Boss/BrimstoneMonster` | 360x360 |
| Permafrost/HAGE 分支实体图 | `CalamityMod/Projectiles/Boss/BrimstoneMonsterII` | 360x360 |
| shader 中心脸 | `CalamityMod/ExtraTextures/ScreamyFace` | 512x512 |
| 非 Permafrost 旋涡 | `CalamityMod/ExtraTextures/SoulVortex` | 408x408 |
| 中心黑色 bloom | `CalamityMod/Particles/LargeBloom` | 360x360 |

### Boss 弹幕内部继续生成的二级弹幕

| 弹幕内部名 | 来源 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---:|---|---|
| `PermafrostBlast` | `PermafrostBlaster` 发射 | `CalamityMod/Projectiles/InvisibleProj`，激光绘制用下方三段贴图 | 1x1 | 10 | 长 2400 的 hostile 激光，使用 `BaseLaserbeamProjectile` 绘制首段/中段/尾段。 |
| `PermafrostColdheartIcicle` | `PermafrostAbsoluteZeroProjectile` 周期性释放 | `CalamityMod/Items/Weapons/Typeless/ColdheartIcicle` | 24x24 | 1 | 围绕 Permafrost 武器方向形成摆动冰晶轨迹，带高速 oldPos 拖尾，命中冻结。 |
| `BrimstoneBarrage` | `SCalBrimstoneFireblast`/`SCalBrimstoneGigablast` 死亡环形释放 | `CalamityMod/Projectiles/Boss/BrimstoneBarrage` | 18x176 | 4 | 爆裂后的环形 dart。 |

`PermafrostBlast` 激光三段贴图：

| 段位 | 内部路径 | 文件尺寸 | 帧数 |
|---|---|---:|---|
| 激光开始段 | `CalamityMod/Projectiles/Rogue/SeraphimBeamLarge` | 40x280 | 10 |
| 激光中段 | `CalamityMod/ExtraTextures/Lasers/SeraphimBeamLargeMiddle` | 40x280 | 10 |
| 激光末段 | `CalamityMod/ExtraTextures/Lasers/SeraphimBeamLargeEnd` | 40x280 | 10 |

`PermafrostColdheartIcicle` 额外拖尾贴图：

| 用途 | 内部路径 | 文件尺寸 |
|---|---|---:|
| prismatic streak shader 贴图 | `CalamityMod/ExtraTextures/Trails/ScarletDevilStreak` | 256x256 |

### 兄弟 NPC/召唤单位在 SCal 战中生成的弹幕

| 生成者 | 弹幕内部名 | 类文件 | 实际贴图路径 | 文件尺寸 | 帧数 | 逻辑简述 |
|---|---|---|---|---:|---|---|
| `SoulSeekerSupreme` | `BrimstoneBarrage` | `Projectiles/Boss/BrimstoneBarrage.cs` | `CalamityMod/Projectiles/Boss/BrimstoneBarrage` | 18x176 | 4 | Soul Seeker 周期性向玩家方向发射 dart。 |
| `SepulcherHead` | `BrimstoneBarrage` | `Projectiles/Boss/BrimstoneBarrage.cs` | `CalamityMod/Projectiles/Boss/BrimstoneBarrage` | 18x176 | 4 | Sepulcher 头部发射 barrage。 |
| `SepulcherBodyEnergyBall` | `SepulcherSoul` | `Projectiles/Typeless/SepulcherSoul.cs` | `CalamityMod/Projectiles/Typeless/SepulcherSoul` | 16x66 | 3 | 视觉魂体，按三角函数漂浮上升并淡入淡出；源码未设 hostile/friendly 伤害。 |
| `SupremeCataclysm` | `SupremeCataclysmFist` | `Projectiles/Boss/SupremeCataclysmFist.cs` | `CalamityMod/Projectiles/Boss/SupremeCataclysmFist` | 126x224 | 4 | 拳头弹幕，按 `ai2` 区分普通拳、快速拳、篮球/GFB 特殊球。 |
| `SupremeCataclysm` | `SupremeCatastropheSlash` | `Projectiles/Boss/SupremeCatastropheSlash.cs` | `CalamityMod/Projectiles/Boss/SupremeCatastropheSlash` | 168x240 | 4 | 兄弟协同时可改用斩击弹幕。 |
| `SupremeCatastrophe` | `SupremeCatastropheSlash` | `Projectiles/Boss/SupremeCatastropheSlash.cs` | `CalamityMod/Projectiles/Boss/SupremeCatastropheSlash` | 168x240 | 4 | 斩击/冲刺轨迹弹幕，带 afterimage；部分模式生成 trail slash。 |
| `SupremeCatastrophe` | `SupremeCataclysmFist` | `Projectiles/Boss/SupremeCataclysmFist.cs` | `CalamityMod/Projectiles/Boss/SupremeCataclysmFist` | 126x224 | 4 | 兄弟协同时可改用拳头弹幕。 |

兄弟弹幕额外/变体贴图：

| 弹幕 | 条件 | 内部路径 | 文件尺寸 | 帧数 |
|---|---|---|---:|---|
| `SupremeCataclysmFist` | `Projectile.ai[1] == 1f` | `CalamityMod/Projectiles/Boss/SupremeCataclysmFistAlt` | 126x224 | 4 |
| `SupremeCataclysmFist` | `Main.zenithWorld && ai2 >= 3` | `CalamityMod/Projectiles/Boss/Basketball` | 340x340 | 1 |
| `SupremeCatastropheSlash` | `Projectile.ai[1] == 0f` | `CalamityMod/Projectiles/Boss/SupremeCatastropheSlashAlt` | 192x232 | 4，单帧高 58 |

## 5. 核心结论

1. 截图里 7 把常规武器是 SCal 普通掉落组；`GaelsGreatsword` 不是这 7 把之一，而是 Death 模式额外掉落。
2. 贴图复用很多：`CondemnationHoldout` 用物品本体图；`SacrificeProjectile` 用物品本体图；`BrimstoneDartSummon` 复用 Boss 的 `BrimstoneBarrage`；`GaelSkull2` 复用 `GaelSkull`。
3. 不能只按 `Projectiles/<ClassName>.png` 找：多个弹幕 override 到物品贴图或 `InvisibleProj`，部分视觉来自 `ModContent.Request<Texture2D>()` 的额外贴图。
4. 多帧重点：`Heresy` 物品 6 帧；`HeresyProj` 8 帧；`GaelSkull/GaelSkull2` 5 帧；`PerditionBeacon` 16 帧；`SeekerSummonProj` 5 帧；Boss 的 brimstone 系列大多 4-6 帧，`PermafrostBlast` 激光三段 10 帧。
