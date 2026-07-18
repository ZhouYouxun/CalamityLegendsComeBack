# CalamityMod 音效资源盘点报告

生成时间：2026-06-09 08:39:15

源目录：`D:\Documents\My Games\Terraria\tModLoader\ModSources\CalamityMod\Sounds`
明细 CSV：`D:\Documents\My Games\Terraria\tModLoader\ModSources\CalamityLegendsComeBack\Weapons\B_Report\CalamityMod_Sounds_Inventory.csv`

> 说明：本报告依据目录结构、文件名、Ogg 元数据和源码中的资源路径引用自动归纳；未逐个听辨音频内容。`代码引用` 只统计源码中直接出现的 `CalamityMod/Sounds/...` 或 `Sounds/...` 字符串，动态拼接路径可能未被计入。

## 总览

- `.ogg` 音频文件：968 个
- 总大小：34.74 MB
- 总时长约：30.9 分钟
- 源码直接引用覆盖：946/968 个，未直接匹配：22 个
- 扫描到的源码资源字符串：1302 条

## 非音频辅助文件

| 文件 | 用途 |
|---|---|
| `CommonCalamitySounds.cs` | 定义常用 `SoundStyle` 静态别名，便于源码复用常见音效。 |
| `Custom\BEES\Names.txt` | 记录 `Custom\BEES\bees1-12.ogg` 对应的名称：Aqua、CIT、Lilac、Metarex、moonburn、Ozzatron、Rebecca、StipulatedVenus、Xyk、Shade、YuH、ENNWAY。 |


## 顶层分类

| 子目录 | 数量 | 大小 MB | 总时长 | 用途概括 |
|---|---:|---:|---:|---|
| `Custom` | 432 | 17.48 | 15.9 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Item` | 422 | 11.95 | 10.3 分钟 | 物品、武器、弹幕和装备动作音效 |
| `Music` | 4 | 2.42 | 2.5 分钟 | 音乐、静音或特殊音乐轨 |
| `NPCHit` | 69 | 1.03 | 0.5 分钟 | NPC/敌怪受击音效 |
| `NPCKilled` | 41 | 1.87 | 1.6 分钟 | NPC/敌怪死亡、破碎或部位损毁音效 |

## 功能分类

| 分类 | 数量 | 说明 |
|---|---:|---|
| 物品/武器 | 422 | 武器开火、挥动、命中、装填、工具、乐器和召唤物动作。 |
| 自定义/Boss/系统 | 403 | Boss、事件、UI、环境块、剧情或特殊机制音效。 |
| NPC受击 | 69 | 受击、护盾受击、敌怪被命中反馈。 |
| NPC死亡 | 41 | 死亡、破碎、部位断裂、特殊击杀反馈。 |
| 玩家能力 | 29 | 怒气、肾上腺素、护甲/套装能力、冷却提示。 |
| 音乐 | 4 | 音乐轨、静音轨、特殊系统音乐。 |

## 子目录汇总

| 子目录 | 数量 | 大小 MB | 总时长 | 用途概括 |
|---|---:|---:|---:|---|
| `Custom` | 196 | 7.83 | 7.2 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\AbilitySounds` | 29 | 1.09 | 1.0 分钟 | 玩家能力、套装能力或冷却提示 |
| `Custom\AstrumAureus` | 6 | 0.14 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\AstrumDeus` | 6 | 0.12 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\BEES` | 12 | 0.16 | 0.2 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\BossRush` | 11 | 0.61 | 0.7 分钟 | Boss Rush 事件流程 |
| `Custom\BrainOfCthulhu` | 15 | 0.65 | 0.7 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\BrimstoneElemental` | 12 | 0.15 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\CalamitasClone` | 18 | 0.64 | 0.6 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\Codebreaker` | 14 | 0.46 | 0.3 分钟 | Codebreaker UI、Draedon 对话和部件安装 |
| `Custom\Crabulon` | 3 | 0.04 | 0.0 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\DesertScourge` | 3 | 0.12 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\DifficultySelection` | 4 | 0.22 | 0.2 分钟 | 难度选择 UI 音效 |
| `Custom\ExoMechs` | 26 | 1.28 | 1.1 分钟 | Exo Mechs / Draedon 机械 Boss 音效 |
| `Custom\GFB` | 6 | 1.20 | 0.9 分钟 | Get fixed boi / Zenith world 彩蛋音效 |
| `Custom\Perforator` | 6 | 0.26 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\PlagueSounds` | 10 | 0.25 | 0.2 分钟 | 瘟疫主题攻击、爆炸和机械虫群音效 |
| `Custom\Polterghast` | 4 | 0.43 | 0.2 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\ProfanedGuardians` | 6 | 0.19 | 0.1 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\Providence` | 8 | 0.55 | 0.4 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\Ravager` | 11 | 0.30 | 0.2 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Custom\SCalSounds` | 18 | 0.56 | 0.7 分钟 | Supreme Calamitas / SCal 战斗与状态音效 |
| `Custom\Yharon` | 8 | 0.24 | 0.3 分钟 | 自定义系统/敌怪/Boss/环境音效 |
| `Item` | 387 | 11.12 | 9.5 分钟 | 物品、武器、弹幕和装备动作音效 |
| `Item\GFBScreams` | 8 | 0.24 | 0.1 分钟 | 物品、武器、弹幕和装备动作音效 |
| `Item\MittWelding` | 5 | 0.13 | 0.1 分钟 | 物品、武器、弹幕和装备动作音效 |
| `Item\NanoblackReaper` | 7 | 0.23 | 0.3 分钟 | 物品、武器、弹幕和装备动作音效 |
| `Item\Saxophone` | 6 | 0.05 | 0.0 分钟 | Saxophone 乐器音效组 |
| `Item\Summon` | 2 | 0.03 | 0.0 分钟 | 召唤武器、仆从或召唤物动作音效 |
| `Item\UnstableCastersGauntlet` | 7 | 0.14 | 0.1 分钟 | Unstable Caster's Gauntlet 元素符印音效 |
| `Music` | 4 | 2.42 | 2.5 分钟 | 音乐、静音或特殊音乐轨 |
| `NPCHit` | 69 | 1.03 | 0.5 分钟 | NPC/敌怪受击音效 |
| `NPCKilled` | 41 | 1.87 | 1.6 分钟 | NPC/敌怪死亡、破碎或部位损毁音效 |

## 最长音频

| 资源 | 时长 | 大小 KB | 是什么 / 用途说明 |
|---|---:|---:|---|
| `Music\DraedonExoSelect.ogg` | 1:09.8 | 1168.2 | 音乐或静音占位轨；主题：Draedon Exo Select。 首处代码上下文：DraedonExoSelectMusicScene.cs。 |
| `Music\DraedonTalk.ogg` | 1:09.8 | 1151.4 | 音乐或静音占位轨；主题：Draedon Talk。 首处代码上下文：DraedonCommunicationMusicScene.cs。 |
| `Custom\GFB\SevenTrebleClefSouls.ogg` | 40.30s | 606.4 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：Seven Treble Clef Souls。 首处代码上下文：AnahitasArpeggioNote.cs。 |
| `Custom\ORDER.ogg` | 39.71s | 635.5 | 自定义系统/敌怪/Boss/环境音效；动作：音乐/静音轨；主题：ORDER。 代码标识：ORDERTrack。 |
| `Custom\GungeonCreditMusic.ogg` | 37.65s | 986.7 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Gungeon Credit Music。 代码标识：GungeonTrack。 |
| `Custom\BoomBoomKawaii.ogg` | 34.16s | 264.4 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Boom Boom Kawaii。 首处代码上下文：ObliteratorYoyo.cs。 |
| `Item\SoupConsumption.ogg` | 16.13s | 389.5 | 物品/武器音效；动作：激活/使用/UI；主题：Soup Consumption。 代码标识：UseSound。 |
| `Item\MarniteLiftHumm.ogg` | 12.03s | 197.3 | 物品/武器音效；动作：通用/特殊；主题：Marnite Lift Humm。 代码标识：LiftHummSound。 |
| `Item\NanoblackReaper\NanoblackReaper_LightspeedSlash.ogg` | 10.09s | 83.6 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Lightspeed Slash。 代码标识：LightspeedSlashBaseSound。 |
| `Custom\ArianeShot.ogg` | 10.02s | 80.4 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Ariane Shot。 首处代码上下文：LiliesOfFinalityBolt.cs。 |
| `Music\MarniteOrgan.ogg` | 9.96s | 152.9 | 音乐或静音占位轨；主题：Marnite Organ。 代码标识：MarniteOrganSound。 |
| `Custom\Providence\ProvidenceDeathAnimation.ogg` | 9.91s | 194.6 | Custom\Providence 专用音效；动作：死亡/击杀；主题：Providence Death Animation。 代码标识：DeathAnimationSound。 |
| `Custom\AstralStarFall.ogg` | 9.65s | 130.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Astral Star Fall。 代码标识：MeteorSound。 |
| `Custom\GFB\YouAreNotSafe.ogg` | 9.06s | 424.5 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：You Are Not Safe。 代码标识：h。 |
| `Custom\ExoMechs\THanosGFBeam.ogg` | 8.93s | 121.7 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：T Hanos GF Beam。 代码标识：GFBeam。 |

## 变体和引用说明

- 许多音效通过 tModLoader 的 `SoundStyle(".../Name", n)` 约定引用，同一资源名前缀的 `Name1.ogg`、`Name2.ogg` 会作为随机变体播放。
- `NPCHit` 和 `NPCKilled` 多数是 NPC 受击/死亡变体；`Item` 多数对应具体物品、武器或弹幕动作；`Custom` 下的子目录通常对应 Boss、事件、UI 或特殊机制。
- `未直接匹配` 不等于无用资源，可能来自动态拼接、资源包约定、外部音乐系统或当前扫描范围外的调用。

## 完整音效明细

### Custom

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 30 | `Custom\AbyssDrown.ogg` | 自定义/Boss/系统 | 2.54s | 46.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Abyss Drown。 代码标识：DrownSound。 | CalPlayer\CalamityPlayer.cs:482 (DrownSound) |
| 31 | `Custom\AbyssGravelMine1.ogg` | 自定义/Boss/系统 | 0.30s | 10.5 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Abyss Gravel Mine。 代码标识：MineSound。 | Tiles\Abyss\AbyssGravel.cs:15 (MineSound); Tiles\Abyss\SulphurousShale.cs:17 (MineSound) |
| 32 | `Custom\AbyssGravelMine2.ogg` | 自定义/Boss/系统 | 0.29s | 10.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Abyss Gravel Mine。 代码标识：MineSound。 | Tiles\Abyss\AbyssGravel.cs:15 (MineSound); Tiles\Abyss\SulphurousShale.cs:17 (MineSound); Projectiles\Melee\AbyssBladeProjectile.cs:213 (HitSound); 另 1 处 |
| 33 | `Custom\AbyssGravelMine3.ogg` | 自定义/Boss/系统 | 0.27s | 9.5 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Abyss Gravel Mine。 代码标识：MineSound。 | Tiles\Abyss\AbyssGravel.cs:15 (MineSound); Tiles\Abyss\SulphurousShale.cs:17 (MineSound) |
| 34 | `Custom\AdrenalineMajorLoss.ogg` | 自定义/Boss/系统 | 1.86s | 32.7 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Adrenaline Major Loss。 代码标识：AdrenalineHurtSound。 | CalPlayer\CalamityPlayer.cs:474 (AdrenalineHurtSound) |
| 35 | `Custom\AdrenalineMajorLossGFB.ogg` | 自定义/Boss/系统 | 1.08s | 15.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Adrenaline Major Loss GFB。 代码标识：AdrenalineHurtGFB。 | CalPlayer\CalamityPlayer.cs:475 (AdrenalineHurtGFB) |
| 36 | `Custom\AndromedaCripple.ogg` | 自定义/Boss/系统 | 0.38s | 7.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Andromeda Cripple。 代码标识：CrippleSound。 | Items\Weapons\Summon\FlamsteedRing.cs:24 (CrippleSound) |
| 37 | `Custom\ArianeShot.ogg` | 自定义/Boss/系统 | 10.02s | 80.4 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Ariane Shot。 首处代码上下文：LiliesOfFinalityBolt.cs。 | Projectiles\Summon\LiliesOfFinalityBolt.cs:104 |
| 38 | `Custom\AstralBeaconOrbPulse.ogg` | 自定义/Boss/系统 | 0.36s | 7.7 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Astral Beacon Orb Pulse。 代码标识：PulseSound。 | Projectiles\Boss\DeusRitualDrama.cs:17 (PulseSound) |
| 39 | `Custom\AstralBeaconUse.ogg` | 自定义/Boss/系统 | 0.88s | 17.0 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Astral Beacon Use。 代码标识：UseSound。 | Tiles\Astral\AstralBeacon.cs:23 (UseSound) |
| 40 | `Custom\AstralStarFall.ogg` | 自定义/Boss/系统 | 9.65s | 130.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Astral Star Fall。 代码标识：MeteorSound。 | World\AstralBiome.cs:26 (MeteorSound) |
| 53 | `Custom\AtlasIdle1.ogg` | 自定义/Boss/系统 | 0.67s | 11.7 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Atlas Idle。 代码标识：IdleSound。 | NPCs\Astral\Atlas.cs:56 (IdleSound) |
| 54 | `Custom\AtlasIdle2.ogg` | 自定义/Boss/系统 | 0.77s | 13.1 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Atlas Idle。 代码标识：IdleSound。 | NPCs\Astral\Atlas.cs:56 (IdleSound) |
| 55 | `Custom\AtlasSadAggro.ogg` | 自定义/Boss/系统 | 0.71s | 12.6 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Atlas Sad Aggro。 代码标识：AggroSound。 | NPCs\Astral\Atlas.cs:53 (AggroSound) |
| 56 | `Custom\AtlasSadUnaggro.ogg` | 自定义/Boss/系统 | 0.87s | 15.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Atlas Sad Unaggro。 代码标识：UnaggroSound。 | NPCs\Astral\Atlas.cs:54 (UnaggroSound) |
| 57 | `Custom\AtlasSwing.ogg` | 自定义/Boss/系统 | 0.72s | 12.9 | 自定义系统/敌怪/Boss/环境音效；动作：近战挥击/撞击；主题：Atlas Swing。 代码标识：SwingSound。 | NPCs\Astral\Atlas.cs:55 (SwingSound) |
| 58 | `Custom\AuricMine1.ogg` | 自定义/Boss/系统 | 0.34s | 10.2 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Auric Mine。 代码标识：MineSound。 | Tiles\Ores\AuricOre.cs:11 (MineSound) |
| 59 | `Custom\AuricMine2.ogg` | 自定义/Boss/系统 | 0.25s | 9.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Auric Mine。 代码标识：MineSound。 | Tiles\Ores\AuricOre.cs:11 (MineSound) |
| 60 | `Custom\AuricMine3.ogg` | 自定义/Boss/系统 | 0.28s | 9.7 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Auric Mine。 代码标识：MineSound。 | Tiles\Ores\AuricOre.cs:11 (MineSound) |
| 73 | `Custom\BloodPactCrit.ogg` | 自定义/Boss/系统 | 0.77s | 19.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Blood Pact Crit。 首处代码上下文：TheMutilatorHoldoutProjectile.cs。 | Projectiles\Melee\TheMutilatorHoldoutProjectile.cs:105; Projectiles\Typeless\ClaretCannonProj.cs:51 |
| 74 | `Custom\BoomBoomKawaii.ogg` | 自定义/Boss/系统 | 34.16s | 264.4 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Boom Boom Kawaii。 首处代码上下文：ObliteratorYoyo.cs。 | Projectiles\Melee\Yoyos\ObliteratorYoyo.cs:75 |
| 113 | `Custom\BubblyBurst.ogg` | 自定义/Boss/系统 | 0.12s | 8.9 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Bubbly Burst。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 114 | `Custom\BubblyPop.ogg` | 自定义/Boss/系统 | 0.14s | 4.6 | 自定义系统/敌怪/Boss/环境音效；动作：水体/气泡；主题：Bubbly Pop。 代码标识：PopSound。 | Projectiles\Ranged\ArcherfishRing.cs:16 (PopSound) |
| 115 | `Custom\BuzzsawCharge.ogg` | 自定义/Boss/系统 | 2.39s | 38.8 | 自定义系统/敌怪/Boss/环境音效；动作：蓄力/充能/冷却；主题：Buzzsaw Charge。 首处代码上下文：BuzzkillHoldout.cs。 | Projectiles\Ranged\BuzzkillHoldout.cs:148; Projectiles\Ranged\SuperradiantSlaughtererHoldout.cs:211 |
| 116 | `Custom\BuzzsawIdle.ogg` | 自定义/Boss/系统 | 6.40s | 92.3 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Buzzsaw Idle。 首处代码上下文：BuzzkillHoldout.cs。 | Projectiles\Ranged\BuzzkillHoldout.cs:156; Projectiles\Ranged\SuperradiantSlaughtererHoldout.cs:216 |
| 135 | `Custom\CeaselessVoidDeathBuild.ogg` | 自定义/Boss/系统 | 4.86s | 89.2 | 自定义系统/敌怪/Boss/环境音效；动作：死亡/击杀；主题：Ceaseless Void Death Build。 代码标识：BuildupSound。 | NPCs\CeaselessVoid\CeaselessVoid.cs:37 (BuildupSound); Items\Weapons\Ranged\PolarisParrotfish.cs:65 (roar) |
| 136 | `Custom\CeramicImpact1.ogg` | 自定义/Boss/系统 | 0.63s | 11.8 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Ceramic Impact。 首处代码上下文：AbyssBladeProjectile.cs。 | Projectiles\Melee\AbyssBladeProjectile.cs:153; Projectiles\Melee\BladecrestOathswordThrownBlade.cs:363; Projectiles\Melee\ExaltedOathbladeThrownBlade.cs:313; 另 5 处 |
| 137 | `Custom\CeramicImpact2.ogg` | 自定义/Boss/系统 | 0.51s | 10.2 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Ceramic Impact。 首处代码上下文：AbyssBladeProjectile.cs。 | Projectiles\Melee\AbyssBladeProjectile.cs:153; Projectiles\Melee\BladecrestOathswordThrownBlade.cs:363; Projectiles\Melee\ExaltedOathbladeThrownBlade.cs:313; 另 5 处 |
| 138 | `Custom\ChainLightning1.ogg` | 自定义/Boss/系统 | 0.52s | 11.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Chain Lightning。 首处代码上下文：ArcZap.cs。 | Projectiles\Typeless\ArcZap.cs:96 |
| 139 | `Custom\ChainLightning2.ogg` | 自定义/Boss/系统 | 0.49s | 10.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Chain Lightning。 首处代码上下文：ArcZap.cs。 | Projectiles\Typeless\ArcZap.cs:96 |
| 140 | `Custom\ChainLightning3.ogg` | 自定义/Boss/系统 | 0.48s | 10.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Chain Lightning。 首处代码上下文：ArcZap.cs。 | Projectiles\Typeless\ArcZap.cs:96 |
| 141 | `Custom\ChainLightning4.ogg` | 自定义/Boss/系统 | 0.55s | 10.6 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Chain Lightning。 首处代码上下文：ArcZap.cs。 | Projectiles\Typeless\ArcZap.cs:96 |
| 142 | `Custom\ChainsawEnd.ogg` | 自定义/Boss/系统 | 2.62s | 74.3 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Chainsaw End。 代码标识：ChainsawEndSound。 | NPCs\Crags\DespairStone.cs:28 (ChainsawEndSound) |
| 143 | `Custom\ChainsawStart.ogg` | 自定义/Boss/系统 | 7.66s | 213.2 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Chainsaw Start。 代码标识：ChainsawStartSound。 | NPCs\Crags\DespairStone.cs:26 (ChainsawStartSound) |
| 158 | `Custom\CodebreakerBeam.ogg` | 自定义/Boss/系统 | 4.44s | 97.3 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Codebreaker Beam。 代码标识：SummonSound。 | UI\DraedonSummoning\CodebreakerUI.cs:98 (SummonSound) |
| 159 | `Custom\CorvinaScream.ogg` | 自定义/Boss/系统 | 1.55s | 22.3 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Corvina Scream。 代码标识：ScreamSound。 | NPCs\Abyss\LuminousCorvina.cs:27 (ScreamSound) |
| 163 | `Custom\CrateBreak1.ogg` | 自定义/Boss/系统 | 0.26s | 8.2 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Crate Break。 代码标识：MineSound。 | Tiles\Abyss\AbyssAmbient\PirateCrate.cs:18 (MineSound); Tiles\Abyss\AbyssAmbient\PirateCrate.cs:83 (MineSound) |
| 164 | `Custom\CrateBreak2.ogg` | 自定义/Boss/系统 | 0.30s | 8.2 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Crate Break。 代码标识：MineSound。 | Tiles\Abyss\AbyssAmbient\PirateCrate.cs:18 (MineSound); Tiles\Abyss\AbyssAmbient\PirateCrate.cs:83 (MineSound) |
| 165 | `Custom\CrateBreak3.ogg` | 自定义/Boss/系统 | 0.32s | 8.3 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Crate Break。 代码标识：MineSound。 | Tiles\Abyss\AbyssAmbient\PirateCrate.cs:18 (MineSound); Tiles\Abyss\AbyssAmbient\PirateCrate.cs:83 (MineSound) |
| 166 | `Custom\Crow1.ogg` | 自定义/Boss/系统 | 2.36s | 28.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Crow。 代码标识：CrowNoises。 | Projectiles\VanillaProjectileOverrides\RavenMinionAI.cs:26 (CrowNoises) |
| 167 | `Custom\Crow2.ogg` | 自定义/Boss/系统 | 1.22s | 17.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Crow。 代码标识：CrowNoises。 | Projectiles\VanillaProjectileOverrides\RavenMinionAI.cs:26 (CrowNoises) |
| 168 | `Custom\Crow3.ogg` | 自定义/Boss/系统 | 1.84s | 22.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Crow。 代码标识：CrowNoises。 | Projectiles\VanillaProjectileOverrides\RavenMinionAI.cs:26 (CrowNoises) |
| 169 | `Custom\CryogenShieldRegenerate.ogg` | 自定义/Boss/系统 | 1.18s | 18.1 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Cryogen Shield Regenerate。 代码标识：ShieldRegenSound。 | NPCs\Cryogen\Cryogen.cs:51 (ShieldRegenSound) |
| 170 | `Custom\CuteSqueak.ogg` | 自定义/Boss/系统 | 0.27s | 6.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Cute Squeak。 代码标识：Squeak。 | Items\Weapons\Ranged\PolarisParrotfish.cs:22 (Squeak) |
| 171 | `Custom\DefenseDamage.ogg` | 自定义/Boss/系统 | 0.75s | 22.7 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Defense Damage。 代码标识：DefenseDamageSound。 | CalPlayer\CalamityPlayer.cs:479 (DefenseDamageSound); Projectiles\Melee\MajesticGuardHoldout.cs:190 (fire2); Projectiles\Melee\SkytideDragoonHoldout.cs:297 (fire2) |
| 175 | `Custom\DevilMaskBreak.ogg` | 自定义/Boss/系统 | 0.94s | 11.9 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Devil Mask Break。 代码标识：MaskBreakSound。 | NPCs\Abyss\DevilFish.cs:23 (MaskBreakSound) |
| 176 | `Custom\DevourerAttack.ogg` | 自定义/Boss/系统 | 2.57s | 47.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Devourer Attack。 代码标识：AttackSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:146 (AttackSound) |
| 177 | `Custom\DevourerRiftBuilding.ogg` | 自定义/Boss/系统 | 2.23s | 31.6 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Devourer Rift Building。 代码标识：RiftBuildingSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:148 (RiftBuildingSound) |
| 178 | `Custom\DevourerRiftOpen.ogg` | 自定义/Boss/系统 | 2.76s | 25.3 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Devourer Rift Open。 代码标识：RiftOpenSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:147 (RiftOpenSound) |
| 179 | `Custom\DevourerSpawn.ogg` | 自定义/Boss/系统 | 7.63s | 105.6 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Devourer Spawn。 代码标识：SpawnSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:145 (SpawnSound) |
| 184 | `Custom\DoGFireball.ogg` | 自定义/Boss/系统 | 0.96s | 22.5 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Do G Fireball。 代码标识：SpawnSound。 | Projectiles\Boss\DoGFire.cs:28 (SpawnSound) |
| 185 | `Custom\DoGLaserWallBigAttack.ogg` | 自定义/Boss/系统 | 1.73s | 54.5 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Do G Laser Wall Big Attack。 代码标识：attack。 | Projectiles\Magic\HyperdeathRiftScepterBeam.cs:75 (attack); Projectiles\Typeless\FriendlyLaserWallBeam.cs:80 (attack) |
| 186 | `Custom\DoGLaserWallBigAttack2.ogg` | 自定义/Boss/系统 | 3.70s | 53.9 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Do G Laser Wall Big Attack。 代码标识：attack。 | Projectiles\Boss\DoGLaserWallsBigBeam.cs:74 (attack); Projectiles\Magic\HyperdeathRiftScepterBeam.cs:75 (attack); Projectiles\Typeless\FriendlyLaserWallBeam.cs:80 (attack) |
| 187 | `Custom\DoGLaserWallLightAttack.ogg` | 自定义/Boss/系统 | 1.13s | 32.1 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Do G Laser Wall Light Attack。 代码标识：attack。 | Projectiles\Boss\DoGLaserWalls.cs:70 (attack); Projectiles\Typeless\FriendlyLaserWallBeam.cs:78 (attack) |
| 188 | `Custom\DoGLaserWallSpawn.ogg` | 自定义/Boss/系统 | 0.76s | 27.1 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Do G Laser Wall Spawn。 代码标识：appear。 | Projectiles\Boss\DoGLaserWalls.cs:63 (appear) |
| 189 | `Custom\DraedonLaugh.ogg` | 自定义/Boss/系统 | 2.94s | 62.3 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Draedon Laugh。 代码标识：LaughSound。 | NPCs\ExoMechs\Draedon.cs:82 (LaughSound) |
| 190 | `Custom\DraedonTeleport.ogg` | 自定义/Boss/系统 | 2.64s | 49.1 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Draedon Teleport。 代码标识：TeleportSound。 | NPCs\ExoMechs\Draedon.cs:83 (TeleportSound) |
| 191 | `Custom\ElsterShot1.ogg` | 自定义/Boss/系统 | 0.66s | 13.1 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Elster Shot。 首处代码上下文：LiliesOfFinalityElster.cs。 | Projectiles\Summon\LiliesOfFinalityElster.cs:332 |
| 192 | `Custom\ElsterShot2.ogg` | 自定义/Boss/系统 | 0.72s | 14.1 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Elster Shot。 首处代码上下文：LiliesOfFinalityElster.cs。 | Projectiles\Summon\LiliesOfFinalityElster.cs:332 |
| 193 | `Custom\ElsterShot3.ogg` | 自定义/Boss/系统 | 0.70s | 17.2 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Elster Shot。 首处代码上下文：LiliesOfFinalityElster.cs。 | Projectiles\Summon\LiliesOfFinalityElster.cs:332 |
| 194 | `Custom\ElsterShot4.ogg` | 自定义/Boss/系统 | 0.74s | 17.7 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Elster Shot。 首处代码上下文：LiliesOfFinalityElster.cs。 | Projectiles\Summon\LiliesOfFinalityElster.cs:332 |
| 221 | `Custom\FlamethrowerTurret.ogg` | 自定义/Boss/系统 | 0.51s | 35.1 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Flamethrower Turret。 首处代码上下文：FireShot.cs。 | Projectiles\Turret\FireShot.cs:52 |
| 228 | `Custom\GildedAxolotlAlert.ogg` | 自定义/Boss/系统 | 0.34s | 8.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Gilded Axolotl Alert。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 229 | `Custom\GildedAxolotlNeuronActivation.ogg` | 自定义/Boss/系统 | 0.57s | 12.5 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Gilded Axolotl Neuron Activation。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 230 | `Custom\GildedAxolotlVocalStim1.ogg` | 自定义/Boss/系统 | 0.36s | 8.7 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Gilded Axolotl Vocal Stim。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 231 | `Custom\GildedAxolotlVocalStim2.ogg` | 自定义/Boss/系统 | 0.60s | 12.0 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Gilded Axolotl Vocal Stim。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 232 | `Custom\GravistarCharge.ogg` | 自定义/Boss/系统 | 1.23s | 53.7 | 自定义系统/敌怪/Boss/环境音效；动作：蓄力/充能/冷却；主题：Gravistar Charge。 首处代码上下文：CalamityPlayer.cs。 | CalPlayer\CalamityPlayer.cs:4347 |
| 233 | `Custom\GravistarSlam.ogg` | 自定义/Boss/系统 | 2.64s | 139.0 | 自定义系统/敌怪/Boss/环境音效；动作：近战挥击/撞击；主题：Gravistar Slam。 首处代码上下文：StomperSlam.cs。 | Projectiles\Typeless\StomperSlam.cs:45 |
| 234 | `Custom\GreatSandSharkRoar.ogg` | 自定义/Boss/系统 | 2.67s | 103.4 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Great Sand Shark Roar。 代码标识：RoarSound。 | NPCs\GreatSandShark\GreatSandShark.cs:27 (RoarSound) |
| 235 | `Custom\GungeonCreditMusic.ogg` | 自定义/Boss/系统 | 37.65s | 986.7 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Gungeon Credit Music。 代码标识：GungeonTrack。 | Systems\Sound\GungeonMusicSystem.cs:12 (GungeonTrack) |
| 236 | `Custom\HeavenlyGaleLightningStrike.ogg` | 自定义/Boss/系统 | 2.97s | 53.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Heavenly Gale Lightning Strike。 代码标识：LightningStrikeSound。 | Items\Weapons\Ranged\HeavenlyGale.cs:36 (LightningStrikeSound) |
| 237 | `Custom\HiveMindRoar.ogg` | 自定义/Boss/系统 | 2.28s | 42.0 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Hive Mind Roar。 代码标识：RoarSound。 | NPCs\HiveMind\HiveMind.cs:94 (RoarSound) |
| 238 | `Custom\HiveMindRoarFast.ogg` | 自定义/Boss/系统 | 1.20s | 24.0 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Hive Mind Roar Fast。 代码标识：FastRoarSound。 | NPCs\HiveMind\HiveMind.cs:95 (FastRoarSound) |
| 239 | `Custom\IjiDies.ogg` | 自定义/Boss/系统 | 0.90s | 9.5 | 自定义系统/敌怪/Boss/环境音效；动作：死亡/击杀；主题：Iji Dies。 代码标识：IjiDeathSound。 | CalPlayer\CalamityPlayer.cs:481 (IjiDeathSound) |
| 240 | `Custom\Kickball.ogg` | 自定义/Boss/系统 | 0.73s | 20.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Kickball。 代码标识：b。 | Projectiles\Boss\SupremeCataclysmFist.cs:283 (b) |
| 241 | `Custom\KingSlimeJewelSpawn.ogg` | 自定义/Boss/系统 | 2.07s | 62.0 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：King Slime Jewel Spawn。 代码标识：SpawnCrystalSound。 | NPCs\VanillaNPCAIOverrides\Bosses\KingSlimeAI.cs:15 (SpawnCrystalSound) |
| 242 | `Custom\LeviathanEmerge.ogg` | 自定义/Boss/系统 | 5.02s | 87.5 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Leviathan Emerge。 代码标识：EmergeSound。 | NPCs\Leviathan\Leviathan.cs:50 (EmergeSound) |
| 243 | `Custom\LeviathanRoarCharge.ogg` | 自定义/Boss/系统 | 3.92s | 55.4 | 自定义系统/敌怪/Boss/环境音效；动作：蓄力/充能/冷却；主题：Leviathan Roar Charge。 代码标识：RoarChargeSound。 | NPCs\Leviathan\Leviathan.cs:49 (RoarChargeSound) |
| 244 | `Custom\LeviathanRoarMeteor.ogg` | 自定义/Boss/系统 | 3.96s | 52.9 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Leviathan Roar Meteor。 代码标识：RoarMeteorSound。 | NPCs\Leviathan\Leviathan.cs:48 (RoarMeteorSound) |
| 245 | `Custom\LeviathanRumble.ogg` | 自定义/Boss/系统 | 3.97s | 48.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Leviathan Rumble。 代码标识：RumbleSound。 | Projectiles\Boss\LeviathanSpawner.cs:21 (RumbleSound) |
| 246 | `Custom\LightningStrike.ogg` | 自定义/Boss/系统 | 3.23s | 54.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Lightning Strike。 代码标识：LightningSound。 | Sounds\CommonCalamitySounds.cs:21 (LightningSound); Items\Weapons\Rogue\StormfrontRazor.cs:15 (LightningStrikeSound) |
| 247 | `Custom\LightningTelegraph.ogg` | 自定义/Boss/系统 | 2.30s | 34.0 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Lightning Telegraph。 代码标识：lightning。 | NPCs\StormWeaver\StormWeaverHead.cs:556 (lightning) |
| 248 | `Custom\LiliesOfFinalityTileHitSound.ogg` | 自定义/Boss/系统 | 3.60s | 49.2 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Lilies Of Finality Tile Hit Sound。 代码标识：HitSound。 | Tiles\Crags\LiliesOfFinalityTile.cs:28 (HitSound) |
| 249 | `Custom\LoudSwingWoosh.ogg` | 自定义/Boss/系统 | 0.35s | 11.0 | 自定义系统/敌怪/Boss/环境音效；动作：近战挥击/撞击；主题：Loud Swing Woosh。 首处代码上下文：RoxcaliburProj.cs。 | Projectiles\Melee\RoxcaliburProj.cs:114; Projectiles\Summon\MirrorofKalandraMinions\AtzirisDisfavor.cs:81 (swing); Items\Accessories\WulfrumAcrobaticsPack.cs:532 (swing) |
| 250 | `Custom\MagicalRockMine1.ogg` | 自定义/Boss/系统 | 0.52s | 26.0 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Magical Rock Mine。 代码标识：MineSound。 | Tiles\Ores\AerialiteOre.cs:14 (MineSound) |
| 251 | `Custom\MagicalRockMine2.ogg` | 自定义/Boss/系统 | 0.48s | 24.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Magical Rock Mine。 代码标识：MineSound。 | Tiles\Ores\AerialiteOre.cs:14 (MineSound) |
| 252 | `Custom\MagicalRockMine3.ogg` | 自定义/Boss/系统 | 0.52s | 24.9 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Magical Rock Mine。 代码标识：MineSound。 | Tiles\Ores\AerialiteOre.cs:14 (MineSound) |
| 253 | `Custom\MaulerRoar.ogg` | 自定义/Boss/系统 | 1.70s | 26.4 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Mauler Roar。 代码标识：RoarSound。 | NPCs\AcidRain\Mauler.cs:50 (RoarSound) |
| 254 | `Custom\MeatySlash.ogg` | 自定义/Boss/系统 | 0.94s | 13.6 | 自定义系统/敌怪/Boss/环境音效；动作：近战挥击/撞击；主题：Meaty Slash。 代码标识：MeatySlashSound。 | Sounds\CommonCalamitySounds.cs:24 (MeatySlashSound); Projectiles\Melee\LucreciaHoldout.cs:191 (swish); Projectiles\Ranged\SuperradiantSlaughtererHoldout.cs:82 |
| 255 | `Custom\MetalPipeFalling.ogg` | 自定义/Boss/系统 | 2.72s | 46.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Metal Pipe Falling。 代码标识：TileCollideGFB。 | Projectiles\Ranged\BuzzkillSaw.cs:18 (TileCollideGFB); Projectiles\Ranged\SuperradiantSaw.cs:20 (TileCollideGFB) |
| 256 | `Custom\MicrowaveBeep.ogg` | 自定义/Boss/系统 | 0.55s | 11.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Microwave Beep。 代码标识：BeepSound。 | Items\Weapons\Melee\TheMicrowave.cs:13 (BeepSound) |
| 257 | `Custom\MMMMMMMMMMMMM.ogg` | 自定义/Boss/系统 | 0.10s | 6.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：MMMMMMMMMMMMM。 代码标识：MMMSound。 | Items\Weapons\Melee\TheMicrowave.cs:14 (MMMSound) |
| 258 | `Custom\MoonLordLaserCharge.ogg` | 自定义/Boss/系统 | 2.29s | 25.6 | 自定义系统/敌怪/Boss/环境音效；动作：激光/光束；主题：Moon Lord Laser Charge。 首处代码上下文：RancorMagicCircle.cs。 | Projectiles\Magic\RancorMagicCircle.cs:160; NPCs\VanillaNPCAIOverrides\Bosses\MoonLordAI.cs:18 (DeathrayChargeSound) |
| 259 | `Custom\MossMine.ogg` | 自定义/Boss/系统 | 0.30s | 10.2 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Moss Mine。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 260 | `Custom\NuclearTerrorSpawn.ogg` | 自定义/Boss/系统 | 4.94s | 86.5 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Nuclear Terror Spawn。 代码标识：SpawnSound。 | NPCs\AcidRain\NuclearTerror.cs:83 (SpawnSound) |
| 261 | `Custom\OldDukeDash.ogg` | 自定义/Boss/系统 | 1.95s | 67.9 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Old Duke Dash。 代码标识：DashSound。 | NPCs\OldDuke\OldDuke.cs:61 (DashSound) |
| 262 | `Custom\OldDukeDashP3.ogg` | 自定义/Boss/系统 | 3.56s | 79.8 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Old Duke Dash P。 代码标识：DashSoundP3。 | NPCs\OldDuke\OldDuke.cs:62 (DashSoundP3) |
| 263 | `Custom\OldDukeHuff.ogg` | 自定义/Boss/系统 | 0.58s | 10.6 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Old Duke Huff。 代码标识：tired。 | Projectiles\Ranged\FetidEmesisHoldout.cs:144 (tired); Projectiles\Summon\MutatedTruffleMinion.cs:229; NPCs\OldDuke\OldDuke.cs:57 (HuffSound); 另 1 处 |
| 264 | `Custom\OldDukeRoar.ogg` | 自定义/Boss/系统 | 1.99s | 29.9 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Old Duke Roar。 首处代码上下文：MutatedTruffleMinion.cs。 | Projectiles\Summon\MutatedTruffleMinion.cs:233; NPCs\OldDuke\OldDuke.cs:58 (RoarSound) |
| 265 | `Custom\OldDukeVomit.ogg` | 自定义/Boss/系统 | 1.68s | 31.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Old Duke Vomit。 首处代码上下文：MutatedTruffleMinion.cs。 | Projectiles\Summon\MutatedTruffleMinion.cs:241; NPCs\OldDuke\OldDuke.cs:59 (VomitSound) |
| 266 | `Custom\OldDukeVortex.ogg` | 自定义/Boss/系统 | 2.89s | 45.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Old Duke Vortex。 代码标识：SpawnSound。 | Projectiles\Boss\OldDukeVortex.cs:20 (SpawnSound); Projectiles\Summon\MutatedTruffleVortex.cs:61 |
| 267 | `Custom\OldDukeVortexSpawn.ogg` | 自定义/Boss/系统 | 4.98s | 169.3 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Old Duke Vortex Spawn。 代码标识：VortexSpawnSound。 | NPCs\OldDuke\OldDuke.cs:60 (VortexSpawnSound) |
| 268 | `Custom\OrbHeal1.ogg` | 自定义/Boss/系统 | 1.22s | 17.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Orb Heal。 代码标识：Spawnsound。 | Projectiles\Healing\BlueJellyAura.cs:23 (Spawnsound); Projectiles\Healing\GladiatorHealOrb.cs:82; Projectiles\Healing\PinkJellyAura.cs:21 (Spawnsound) |
| 269 | `Custom\OrbHeal2.ogg` | 自定义/Boss/系统 | 1.18s | 17.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Orb Heal。 首处代码上下文：GladiatorHealOrb.cs。 | Projectiles\Healing\GladiatorHealOrb.cs:82 |
| 270 | `Custom\OrbHeal3.ogg` | 自定义/Boss/系统 | 1.23s | 18.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Orb Heal。 代码标识：Spawnsound。 | Projectiles\Healing\AbsorberAura.cs:26 (Spawnsound); Projectiles\Healing\GladiatorHealOrb.cs:82; Projectiles\Healing\GreenJellyAura.cs:24 (Spawnsound) |
| 271 | `Custom\OrbHeal4.ogg` | 自定义/Boss/系统 | 1.17s | 17.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Orb Heal。 首处代码上下文：GladiatorHealOrb.cs。 | Projectiles\Healing\GladiatorHealOrb.cs:82 |
| 272 | `Custom\OrbHeal5.ogg` | 自定义/Boss/系统 | 1.18s | 17.6 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Orb Heal。 首处代码上下文：GladiatorHealOrb.cs。 | Projectiles\Healing\GladiatorHealOrb.cs:82 |
| 273 | `Custom\ORDER.ogg` | 自定义/Boss/系统 | 39.71s | 635.5 | 自定义系统/敌怪/Boss/环境音效；动作：音乐/静音轨；主题：ORDER。 代码标识：ORDERTrack。 | Systems\Sound\ORDERSystem.cs:10 (ORDERTrack) |
| 280 | `Custom\PistolShrimpBubbleBurst.ogg` | 自定义/Boss/系统 | 0.42s | 10.9 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Pistol Shrimp Bubble Burst。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 291 | `Custom\PlagueUnleash.ogg` | 自定义/Boss/系统 | 4.05s | 68.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Plague Unleash。 代码标识：PlagueSound。 | NPCs\CalamityGlobalNPCLoot.cs:40 (PlagueSound) |
| 292 | `Custom\PlantyMushMine1.ogg` | 自定义/Boss/系统 | 0.30s | 9.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Planty Mush Mine。 代码标识：MineSound。 | Tiles\Abyss\PlantyMush.cs:17 (MineSound); Projectiles\Boss\HolyLight.cs:119 (fireHeal); Projectiles\Rogue\InkBombProjectile.cs:13 (Explode); 另 2 处 |
| 293 | `Custom\PlantyMushMine2.ogg` | 自定义/Boss/系统 | 0.33s | 9.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Planty Mush Mine。 代码标识：MineSound。 | Tiles\Abyss\PlantyMush.cs:17 (MineSound); Projectiles\Boss\HolyLight.cs:119 (fireHeal); Projectiles\Rogue\InkBombProjectile.cs:13 (Explode); 另 2 处 |
| 294 | `Custom\PlantyMushMine3.ogg` | 自定义/Boss/系统 | 0.32s | 10.6 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Planty Mush Mine。 代码标识：MineSound。 | Tiles\Abyss\PlantyMush.cs:17 (MineSound); Projectiles\Boss\HolyLight.cs:119 (fireHeal); Projectiles\Rogue\InkBombProjectile.cs:13 (Explode); 另 2 处 |
| 295 | `Custom\PlatingMine1.ogg` | 自定义/Boss/系统 | 0.33s | 11.0 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Plating Mine。 代码标识：PlatingMine。 | Sounds\CommonCalamitySounds.cs:28 (PlatingMine) |
| 296 | `Custom\PlatingMine2.ogg` | 自定义/Boss/系统 | 0.33s | 11.4 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Plating Mine。 代码标识：PlatingMine。 | Sounds\CommonCalamitySounds.cs:28 (PlatingMine) |
| 297 | `Custom\PlatingMine3.ogg` | 自定义/Boss/系统 | 0.34s | 11.3 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Plating Mine。 代码标识：PlatingMine。 | Sounds\CommonCalamitySounds.cs:28 (PlatingMine) |
| 302 | `Custom\PrimordialWyrmCharge.ogg` | 自定义/Boss/系统 | 2.48s | 19.7 | 自定义系统/敌怪/Boss/环境音效；动作：蓄力/充能/冷却；主题：Primordial Wyrm Charge。 代码标识：ChargeSound。 | NPCs\PrimordialWyrm\PrimordialWyrmHead.cs:85 (ChargeSound) |
| 317 | `Custom\PumpkinEmerge1.ogg` | 自定义/Boss/系统 | 0.64s | 16.5 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Pumpkin Emerge。 代码标识：GrowSound。 | Projectiles\Summon\HarvestStaffMinion.cs:157 (GrowSound) |
| 318 | `Custom\PumpkinEmerge2.ogg` | 自定义/Boss/系统 | 0.72s | 17.8 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Pumpkin Emerge。 代码标识：GrowSound。 | Projectiles\Summon\HarvestStaffMinion.cs:157 (GrowSound) |
| 319 | `Custom\PumpkinEmerge3.ogg` | 自定义/Boss/系统 | 0.62s | 14.7 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Pumpkin Emerge。 代码标识：GrowSound。 | Projectiles\Summon\HarvestStaffMinion.cs:157 (GrowSound) |
| 320 | `Custom\PumpkinExplode1.ogg` | 自定义/Boss/系统 | 0.57s | 14.9 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Pumpkin Explode。 首处代码上下文：PumpkaboomBig.cs。 | Projectiles\Rogue\PumpkaboomBig.cs:257; Projectiles\Rogue\PumpkaboomSmall.cs:227; Projectiles\Summon\HarvestStaffMinion.cs:167 (BoomSound) |
| 321 | `Custom\PumpkinExplode2.ogg` | 自定义/Boss/系统 | 0.65s | 16.7 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Pumpkin Explode。 代码标识：BoomSound。 | Projectiles\Summon\HarvestStaffMinion.cs:167 (BoomSound) |
| 322 | `Custom\PumpkinExplodeGFB1.ogg` | 自定义/Boss/系统 | 3.19s | 46.4 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Pumpkin Explode GFB。 代码标识：BoomSoundGFB。 | Projectiles\Summon\HarvestStaffMinion.cs:169 (BoomSoundGFB) |
| 323 | `Custom\PumpkinExplodeGFB2.ogg` | 自定义/Boss/系统 | 3.22s | 61.1 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Pumpkin Explode GFB。 代码标识：BoomSoundGFB。 | Projectiles\Summon\HarvestStaffMinion.cs:169 (BoomSoundGFB) |
| 324 | `Custom\PumpkinIdle1.ogg` | 自定义/Boss/系统 | 0.55s | 11.6 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Pumpkin Idle。 代码标识：IdleSound。 | Projectiles\Summon\HarvestStaffMinion.cs:159 (IdleSound) |
| 325 | `Custom\PumpkinIdle2.ogg` | 自定义/Boss/系统 | 0.54s | 11.5 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Pumpkin Idle。 代码标识：IdleSound。 | Projectiles\Summon\HarvestStaffMinion.cs:159 (IdleSound) |
| 326 | `Custom\PumpkinIdle3.ogg` | 自定义/Boss/系统 | 0.54s | 12.2 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Pumpkin Idle。 代码标识：IdleSound。 | Projectiles\Summon\HarvestStaffMinion.cs:159 (IdleSound) |
| 327 | `Custom\PumpkinIdle4.ogg` | 自定义/Boss/系统 | 0.58s | 12.8 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Pumpkin Idle。 代码标识：IdleSound。 | Projectiles\Summon\HarvestStaffMinion.cs:159 (IdleSound) |
| 328 | `Custom\PumpkinJump.ogg` | 自定义/Boss/系统 | 0.19s | 7.3 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Pumpkin Jump。 代码标识：JumpSound。 | Projectiles\Summon\HarvestStaffMinion.cs:165 (JumpSound) |
| 329 | `Custom\PumpkinRareIdle.ogg` | 自定义/Boss/系统 | 1.41s | 25.2 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Pumpkin Rare Idle。 代码标识：IdleRareSound。 | Projectiles\Summon\HarvestStaffMinion.cs:161 (IdleRareSound) |
| 330 | `Custom\PumpkinScream1.ogg` | 自定义/Boss/系统 | 0.89s | 15.6 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Pumpkin Scream。 代码标识：ScreamSound。 | Projectiles\Summon\HarvestStaffMinion.cs:163 (ScreamSound) |
| 331 | `Custom\PumpkinScream2.ogg` | 自定义/Boss/系统 | 0.97s | 16.2 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Pumpkin Scream。 代码标识：ScreamSound。 | Projectiles\Summon\HarvestStaffMinion.cs:163 (ScreamSound) |
| 332 | `Custom\RaidersTalismanStealthHit.ogg` | 自定义/Boss/系统 | 1.02s | 22.8 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Raiders Talisman Stealth Hit。 代码标识：StealthHitSound。 | Items\Accessories\RaidersTalisman.cs:14 (StealthHitSound) |
| 344 | `Custom\ReaperEnragedRoar.ogg` | 自定义/Boss/系统 | 2.55s | 30.2 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Reaper Enraged Roar。 代码标识：EnragedRoarSound。 | NPCs\Abyss\ReaperShark.cs:28 (EnragedRoarSound) |
| 345 | `Custom\ReaperSearchRoar.ogg` | 自定义/Boss/系统 | 3.10s | 26.7 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Reaper Search Roar。 代码标识：SearchRoarSound。 | NPCs\Abyss\ReaperShark.cs:27 (SearchRoarSound) |
| 346 | `Custom\RedJewelFire.ogg` | 自定义/Boss/系统 | 1.19s | 30.1 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Red Jewel Fire。 代码标识：ShootSound。 | NPCs\NormalNPCs\KingSlimeJewelRuby.cs:22 (ShootSound); NPCs\VanillaNPCAIOverrides\Bosses\KingSlimeAI.cs:16 (ShootSound) |
| 347 | `Custom\RedJewelModeShift.ogg` | 自定义/Boss/系统 | 1.37s | 27.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Red Jewel Mode Shift。 代码标识：ModeShiftSound。 | NPCs\NormalNPCs\KingSlimeJewelRuby.cs:23 (ModeShiftSound) |
| 348 | `Custom\RimehoundGrowl.ogg` | 自定义/Boss/系统 | 2.19s | 21.6 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Rimehound Growl。 代码标识：GrowlSound。 | NPCs\NormalNPCs\Rimehound.cs:18 (GrowlSound) |
| 349 | `Custom\RogueStealth.ogg` | 自定义/Boss/系统 | 0.85s | 10.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Rogue Stealth。 代码标识：RogueStealthSound。 | CalPlayer\CalamityPlayer.cs:478 (RogueStealthSound) |
| 350 | `Custom\RoverDriveActivate.ogg` | 自定义/Boss/系统 | 8.90s | 198.7 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Rover Drive Activate。 代码标识：ActivationSound。 | Items\Accessories\RoverDrive.cs:26 (ActivationSound); Items\Accessories\TheSponge.cs:34 (ActivationSound); Items\Armor\LunicCorps\LunicCorpsHelmet.cs:27 (ActivationSound) |
| 351 | `Custom\RoverDriveBreak.ogg` | 自定义/Boss/系统 | 1.93s | 39.5 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Rover Drive Break。 代码标识：BreakSound。 | Items\Accessories\RoverDrive.cs:27 (BreakSound); Items\Accessories\TheSponge.cs:35 (BreakSound); Items\Armor\LunicCorps\LunicCorpsHelmet.cs:28 (BreakSound) |
| 352 | `Custom\RoverDriveHit.ogg` | 自定义/Boss/系统 | 0.91s | 22.1 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Rover Drive Hit。 代码标识：ShieldHurtSound。 | Items\Accessories\RoverDrive.cs:25 (ShieldHurtSound); Items\Accessories\TheSponge.cs:33 (ShieldHurtSound); Items\Armor\LunicCorps\LunicCorpsHelmet.cs:26 (ShieldHurtSound) |
| 353 | `Custom\SCalAltarSummon.ogg` | 自定义/Boss/系统 | 8.48s | 185.9 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：S Cal Altar Summon。 代码标识：SummonSound。 | Tiles\Furniture\CraftingStations\SCalAltar.cs:24 (SummonSound); NPCs\SupremeCalamitas\SupremeCalamitas.cs:1395 |
| 372 | `Custom\Scare.ogg` | 自定义/Boss/系统 | 4.61s | 67.1 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Scare。 代码标识：SpawnSound。 | NPCs\PrimordialWyrm\PrimordialWyrmHead.cs:83 (SpawnSound) |
| 373 | `Custom\ScissorGuillotineSnap.ogg` | 自定义/Boss/系统 | 1.01s | 22.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Scissor Guillotine Snap。 代码标识：ScissorGuillotineSnapSound。 | Sounds\CommonCalamitySounds.cs:30 (ScissorGuillotineSnapSound) |
| 374 | `Custom\ScornJump.ogg` | 自定义/Boss/系统 | 0.68s | 12.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Scorn Jump。 代码标识：JumpSound。 | NPCs\NormalNPCs\ScornEater.cs:18 (JumpSound) |
| 375 | `Custom\SharkoonBoom.ogg` | 自定义/Boss/系统 | 0.67s | 20.7 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Sharkoon Boom。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 376 | `Custom\ShoreskipperGrunt1.ogg` | 自定义/Boss/系统 | 0.24s | 8.1 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Shoreskipper Grunt。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 377 | `Custom\ShoreskipperGrunt2.ogg` | 自定义/Boss/系统 | 0.38s | 9.5 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Shoreskipper Grunt。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 378 | `Custom\ShoreskipperSighting.ogg` | 自定义/Boss/系统 | 0.41s | 9.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Shoreskipper Sighting。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 379 | `Custom\SlimeGodBigShot1.ogg` | 自定义/Boss/系统 | 0.74s | 21.9 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Slime God Big Shot。 代码标识：BigShotSound。 | NPCs\SlimeGod\SlimeGodCore.cs:45 (BigShotSound) |
| 380 | `Custom\SlimeGodBigShot2.ogg` | 自定义/Boss/系统 | 0.66s | 18.5 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Slime God Big Shot。 代码标识：BigShotSound。 | NPCs\SlimeGod\SlimeGodCore.cs:45 (BigShotSound) |
| 381 | `Custom\SlimeGodExit.ogg` | 自定义/Boss/系统 | 1.05s | 22.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Slime God Exit。 代码标识：ExitSound。 | NPCs\SlimeGod\SlimeGodCore.cs:43 (ExitSound) |
| 382 | `Custom\SlimeGodPossession.ogg` | 自定义/Boss/系统 | 2.17s | 40.8 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Slime God Possession。 代码标识：PossessionSound。 | NPCs\SlimeGod\SlimeGodCore.cs:42 (PossessionSound) |
| 383 | `Custom\SlimeGodShot1.ogg` | 自定义/Boss/系统 | 0.46s | 14.3 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Slime God Shot。 代码标识：ShotSound。 | NPCs\SlimeGod\SlimeGodCore.cs:44 (ShotSound) |
| 384 | `Custom\SlimeGodShot2.ogg` | 自定义/Boss/系统 | 0.46s | 12.4 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Slime God Shot。 代码标识：ShotSound。 | NPCs\SlimeGod\SlimeGodCore.cs:44 (ShotSound) |
| 385 | `Custom\StormlionAltIdle1.ogg` | 自定义/Boss/系统 | 0.46s | 16.3 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Stormlion Alt Idle。 代码标识：Idle1。 | Effects\StormlionEffects.cs:21 (Idle1) |
| 386 | `Custom\StormlionAltIdle2.ogg` | 自定义/Boss/系统 | 0.33s | 14.6 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Stormlion Alt Idle。 代码标识：Idle2。 | Effects\StormlionEffects.cs:22 (Idle2) |
| 387 | `Custom\StormlionAltShoot.ogg` | 自定义/Boss/系统 | 0.47s | 16.8 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Stormlion Alt Shoot。 代码标识：Attack。 | Effects\StormlionEffects.cs:20 (Attack) |
| 388 | `Custom\StormlionIdle.ogg` | 自定义/Boss/系统 | 1.05s | 23.5 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Stormlion Idle。 代码标识：IdleSound。 | NPCs\NormalNPCs\Stormlion.cs:15 (IdleSound) |
| 389 | `Custom\Stylish.ogg` | 自定义/Boss/系统 | 1.07s | 18.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Stylish。 代码标识：StylishSound。 | Projectiles\Summon\Umbrella\MagicHammer.cs:23 (StylishSound) |
| 390 | `Custom\SubsumingVortexExplosion.ogg` | 自定义/Boss/系统 | 1.10s | 13.9 | 自定义系统/敌怪/Boss/环境音效；动作：爆炸/爆裂；主题：Subsuming Vortex Explosion。 代码标识：ExplosionSound。 | Items\Weapons\Magic\SubsumingVortex.cs:39 (ExplosionSound) |
| 391 | `Custom\SupremeCalamitasSpawn.ogg` | 自定义/Boss/系统 | 6.00s | 76.9 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Supreme Calamitas Spawn。 代码标识：SpawnSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:221 (SpawnSound) |
| 392 | `Custom\SwiftSlice.ogg` | 自定义/Boss/系统 | 0.67s | 12.2 | 自定义系统/敌怪/Boss/环境音效；动作：近战挥击/撞击；主题：Swift Slice。 代码标识：SwiftSliceSound。 | Sounds\CommonCalamitySounds.cs:31 (SwiftSliceSound); Projectiles\Melee\NeptunesBountyProjectile.cs:276; Projectiles\Melee\StreamGougePortal.cs:18 (SpawnSound); 另 7 处 |
| 393 | `Custom\TickingTimer.ogg` | 自定义/Boss/系统 | 0.60s | 7.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Ticking Timer。 代码标识：TimerSound。 | Items\VanillaArmorChanges\NecroArmorSetChange.cs:20 (TimerSound) |
| 394 | `Custom\TreeFalling.ogg` | 自定义/Boss/系统 | 1.90s | 43.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Tree Falling。 代码标识：TreeCrashSound。 | Projectiles\Summon\Umbrella\MagicTree.cs:29 (TreeCrashSound) |
| 395 | `Custom\Ultrabling.ogg` | 自定义/Boss/系统 | 1.02s | 14.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Ultrabling。 代码标识：BlingSound。 | Projectiles\Ranged\M1GarandEmptyClip.cs:15 (BlingSound); Projectiles\Ranged\RicoshotCoin.cs:16 (BlingSound) |
| 396 | `Custom\UltrablingHit.ogg` | 自定义/Boss/系统 | 1.02s | 15.4 | 自定义系统/敌怪/Boss/环境音效；动作：受击/命中/冲击；主题：Ultrabling Hit。 代码标识：BlingHitSound。 | Projectiles\Ranged\M1GarandEmptyClip.cs:16 (BlingHitSound); Projectiles\Ranged\RicoshotCoin.cs:17 (BlingHitSound); Items\Weapons\Melee\WulfrumScrewdriver.cs:26 (FunnyUltrablingSound) |
| 397 | `Custom\ur.ogg` | 自定义/Boss/系统 | 0.31s | 11.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：ur。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 398 | `Custom\VoidstoneMine1.ogg` | 自定义/Boss/系统 | 0.44s | 12.2 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Voidstone Mine。 代码标识：VoidstoneMine。 | Sounds\CommonCalamitySounds.cs:33 (VoidstoneMine) |
| 399 | `Custom\VoidstoneMine2.ogg` | 自定义/Boss/系统 | 0.36s | 10.3 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Voidstone Mine。 代码标识：VoidstoneMine。 | Sounds\CommonCalamitySounds.cs:33 (VoidstoneMine) |
| 400 | `Custom\VoidstoneMine3.ogg` | 自定义/Boss/系统 | 0.24s | 9.0 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Voidstone Mine。 代码标识：VoidstoneMine。 | Sounds\CommonCalamitySounds.cs:33 (VoidstoneMine) |
| 401 | `Custom\WeaponEnchant.ogg` | 自定义/Boss/系统 | 3.51s | 29.2 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Weapon Enchant。 首处代码上下文：CalamityPlayerMiscEffects.cs。 | CalPlayer\CalamityPlayerMiscEffects.cs:1082; UI\CalamitasEnchantments\CalamitasEnchantUI.cs:30 (EnchSound); Projectiles\Magic\IncineratingFireball.cs:134 |
| 402 | `Custom\WeaponExhume.ogg` | 自定义/Boss/系统 | 4.96s | 36.7 | 自定义系统/敌怪/Boss/环境音效；动作：激活/使用/UI；主题：Weapon Exhume。 代码标识：EXSound。 | UI\CalamitasEnchantments\CalamitasEnchantUI.cs:31 (EXSound) |
| 403 | `Custom\WeaverArmorShed.ogg` | 自定义/Boss/系统 | 1.72s | 21.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Weaver Armor Shed。 代码标识：ArmorShedSound。 | NPCs\StormWeaver\StormWeaverHead.cs:54 (ArmorShedSound) |
| 404 | `Custom\WetSlap1.ogg` | 自定义/Boss/系统 | 0.99s | 15.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wet Slap。 代码标识：SlapSound。 | Projectiles\Summon\CnidarianJellyfishOnTheString.cs:31 (SlapSound); Projectiles\Typeless\LeviAmberDash.cs:28 (Slap) |
| 405 | `Custom\WetSlap2.ogg` | 自定义/Boss/系统 | 0.99s | 15.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wet Slap。 代码标识：SlapSound。 | Projectiles\Summon\CnidarianJellyfishOnTheString.cs:31 (SlapSound); Projectiles\Typeless\LeviAmberDash.cs:28 (Slap) |
| 406 | `Custom\WetSlap3.ogg` | 自定义/Boss/系统 | 0.99s | 16.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wet Slap。 代码标识：SlapSound。 | Projectiles\Summon\CnidarianJellyfishOnTheString.cs:31 (SlapSound); Projectiles\Typeless\LeviAmberDash.cs:28 (Slap) |
| 407 | `Custom\WetSlap4.ogg` | 自定义/Boss/系统 | 0.99s | 16.7 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wet Slap。 代码标识：SlapSound。 | Projectiles\Summon\CnidarianJellyfishOnTheString.cs:31 (SlapSound); Projectiles\Typeless\LeviAmberDash.cs:28 (Slap) |
| 408 | `Custom\WulfrumDroidChirp1.ogg` | 自定义/Boss/系统 | 0.72s | 17.9 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Chirp。 代码标识：RandomChirpSound。 | Projectiles\Summon\WulfrumDroid.cs:26 (RandomChirpSound) |
| 409 | `Custom\WulfrumDroidChirp2.ogg` | 自定义/Boss/系统 | 0.48s | 14.7 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Chirp。 代码标识：RandomChirpSound。 | Projectiles\Summon\WulfrumDroid.cs:26 (RandomChirpSound) |
| 410 | `Custom\WulfrumDroidChirp3.ogg` | 自定义/Boss/系统 | 0.79s | 19.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Chirp。 代码标识：RandomChirpSound。 | Projectiles\Summon\WulfrumDroid.cs:26 (RandomChirpSound) |
| 411 | `Custom\WulfrumDroidChirp4.ogg` | 自定义/Boss/系统 | 0.91s | 20.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Chirp。 代码标识：RandomChirpSound。 | Projectiles\Summon\WulfrumDroid.cs:26 (RandomChirpSound) |
| 412 | `Custom\WulfrumDroidFire.ogg` | 自定义/Boss/系统 | 1.27s | 26.5 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Wulfrum Droid Fire。 代码标识：PewSound。 | Projectiles\Summon\WulfrumDroid.cs:25 (PewSound) |
| 413 | `Custom\WulfrumDroidHurry1.ogg` | 自定义/Boss/系统 | 1.71s | 34.8 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Hurry。 代码标识：HurrySound。 | Projectiles\Summon\WulfrumDroid.cs:27 (HurrySound); NPCs\DraedonLabThings\Androomba.cs:21 (HurrySound) |
| 414 | `Custom\WulfrumDroidHurry2.ogg` | 自定义/Boss/系统 | 0.91s | 22.2 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Hurry。 代码标识：HurrySound。 | Projectiles\Summon\WulfrumDroid.cs:27 (HurrySound); NPCs\DraedonLabThings\Androomba.cs:21 (HurrySound) |
| 415 | `Custom\WulfrumDroidRepair.ogg` | 自定义/Boss/系统 | 1.49s | 33.7 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Droid Repair。 代码标识：RepairSound。 | Projectiles\Summon\WulfrumDroid.cs:28 (RepairSound) |
| 416 | `Custom\WulfrumDroidSpawnBeep.ogg` | 自定义/Boss/系统 | 0.45s | 16.1 | 自定义系统/敌怪/Boss/环境音效；动作：移动/生成/阶段转换；主题：Wulfrum Droid Spawn Beep。 代码标识：HelloSound。 | Projectiles\Summon\WulfrumDroid.cs:24 (HelloSound) |
| 417 | `Custom\WulfrumExtraDrop.ogg` | 自定义/Boss/系统 | 0.89s | 26.4 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Extra Drop。 代码标识：ExtraDropSound。 | Items\Accessories\WulfrumBattery.cs:13 (ExtraDropSound) |
| 418 | `Custom\WulfrumHookDisengage.ogg` | 自定义/Boss/系统 | 0.62s | 16.5 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Hook Disengage。 代码标识：ReleaseSound。 | Items\Accessories\WulfrumAcrobaticsPack.cs:26 (ReleaseSound) |
| 419 | `Custom\WulfrumHookGrapple.ogg` | 自定义/Boss/系统 | 1.14s | 26.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Hook Grapple。 代码标识：GrabSound。 | Items\Accessories\WulfrumAcrobaticsPack.cs:25 (GrabSound) |
| 420 | `Custom\WulfrumHookShoot.ogg` | 自定义/Boss/系统 | 0.78s | 14.7 | 自定义系统/敌怪/Boss/环境音效；动作：射击/发射；主题：Wulfrum Hook Shoot。 代码标识：ShootSound。 | Items\Accessories\WulfrumAcrobaticsPack.cs:24 (ShootSound) |
| 421 | `Custom\WulfrumMachineBreak.ogg` | 自定义/Boss/系统 | 1.55s | 29.1 | 自定义系统/敌怪/Boss/环境音效；动作：采掘/破碎/物块碰撞；主题：Wulfrum Machine Break。 代码标识：BreakingSound。 | Projectiles\Typeless\WulfrumDiggingTurtleProjectile.cs:19 (BreakingSound) |
| 422 | `Custom\WulfrumSawCutting.ogg` | 自定义/Boss/系统 | 3.33s | 63.0 | 自定义系统/敌怪/Boss/环境音效；动作：通用/特殊；主题：Wulfrum Saw Cutting。 代码标识：CuttingSound。 | Projectiles\Typeless\WulfrumDiggingTurtleProjectile.cs:18 (CuttingSound) |
| 423 | `Custom\WulfrumSawIdle.ogg` | 自定义/Boss/系统 | 8.66s | 164.8 | 自定义系统/敌怪/Boss/环境音效；动作：循环/开始结束/预警；主题：Wulfrum Saw Idle。 代码标识：IdleSound。 | Projectiles\Typeless\WulfrumDiggingTurtleProjectile.cs:17 (IdleSound) |
| 424 | `Custom\WyrmScream.ogg` | 自定义/Boss/系统 | 3.32s | 44.6 | 自定义系统/敌怪/Boss/环境音效；动作：吼叫/语音；主题：Wyrm Scream。 代码标识：WyrmScreamSound。 | Sounds\CommonCalamitySounds.cs:36 (WyrmScreamSound) |

### Custom\AbilitySounds

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 1 | `Custom\AbilitySounds\AdrenalineActivate.ogg` | 玩家能力 | 5.50s | 99.6 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Adrenaline Activate。 代码标识：AdrenalineActivationSound。 | CalPlayer\CalamityPlayer.cs:473 (AdrenalineActivationSound) |
| 2 | `Custom\AbilitySounds\AngelicAllianceActivation.ogg` | 玩家能力 | 6.53s | 82.5 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Angelic Alliance Activation。 代码标识：ActivationSound。 | Items\Accessories\AngelicAlliance.cs:18 (ActivationSound) |
| 3 | `Custom\AbilitySounds\BloodflareRangerActivation.ogg` | 玩家能力 | 0.81s | 16.7 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Bloodflare Ranger Activation。 代码标识：ActivationSound。 | Items\Armor\Bloodflare\BloodflareHeadRanged.cs:16 (ActivationSound) |
| 4 | `Custom\AbilitySounds\BloodflareRangerRecharge.ogg` | 玩家能力 | 0.56s | 14.7 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Bloodflare Ranger Recharge。 代码标识：EndSound。 | Cooldowns\BloodflareRangedSet.cs:17 (EndSound) |
| 5 | `Custom\AbilitySounds\BrimflameAbility.ogg` | 玩家能力 | 1.62s | 32.3 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Brimflame Ability。 代码标识：ActivationSound。 | Items\Armor\Brimflame\BrimflameCowl.cs:18 (ActivationSound) |
| 6 | `Custom\AbilitySounds\BrimflameRecharge.ogg` | 玩家能力 | 0.97s | 16.2 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Brimflame Recharge。 代码标识：EndSound。 | Cooldowns\BrimflameFrenzy.cs:20 (EndSound); Projectiles\Ranged\CondemnationHoldout.cs:100 (fire) |
| 7 | `Custom\AbilitySounds\ChaosStateOver.ogg` | 玩家能力 | 1.27s | 24.6 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Chaos State Over。 代码标识：EndSound。 | Cooldowns\ChaosState.cs:17 (EndSound) |
| 8 | `Custom\AbilitySounds\DarklightEnergyCharged.ogg` | 玩家能力 | 0.75s | 20.2 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Darklight Energy Charged。 代码标识：maxEnergyReached。 | CalPlayer\CalamityPlayerMiscEffects.cs:415 (maxEnergyReached); CalPlayer\CalamityPlayerMiscEffects.cs:472 (maxEnergyReached) |
| 9 | `Custom\AbilitySounds\DemonshadeEnrage.ogg` | 玩家能力 | 3.19s | 53.6 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Demonshade Enrage。 代码标识：ActivationSound。 | Items\Armor\Demonshade\DemonshadeHelm.cs:19 (ActivationSound) |
| 10 | `Custom\AbilitySounds\DesertProwlerCDReset.ogg` | 玩家能力 | 1.50s | 28.4 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Desert Prowler CD Reset。 代码标识：CDResetSound。 | Items\Armor\DesertProwler\DesertProwlerSet.cs:23 (CDResetSound) |
| 11 | `Custom\AbilitySounds\DesertProwlerSmokeBomb.ogg` | 玩家能力 | 5.51s | 93.8 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Desert Prowler Smoke Bomb。 代码标识：SmokeBombSound。 | Items\Armor\DesertProwler\DesertProwlerSet.cs:21 (SmokeBombSound) |
| 12 | `Custom\AbilitySounds\DesertProwlerSmokeBombEnd.ogg` | 玩家能力 | 1.41s | 24.6 | 玩家能力/套装或冷却提示音效；动作：循环/开始结束/预警；主题：Desert Prowler Smoke Bomb End。 代码标识：SmokeBombEndSound。 | Items\Armor\DesertProwler\DesertProwlerSet.cs:22 (SmokeBombEndSound) |
| 13 | `Custom\AbilitySounds\DesertProwlerSmokeBombReload.ogg` | 玩家能力 | 1.86s | 40.0 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Desert Prowler Smoke Bomb Reload。 代码标识：EndSound。 | Cooldowns\SandsmokeBomb.cs:30 (EndSound) |
| 14 | `Custom\AbilitySounds\FullAdrenaline.ogg` | 玩家能力 | 0.65s | 10.8 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Full Adrenaline。 代码标识：AdrenalineFilledSound。 | CalPlayer\CalamityPlayer.cs:472 (AdrenalineFilledSound) |
| 15 | `Custom\AbilitySounds\FullRage.ogg` | 玩家能力 | 0.62s | 10.3 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Full Rage。 代码标识：RageFilledSound。 | CalPlayer\CalamityPlayer.cs:468 (RageFilledSound) |
| 16 | `Custom\AbilitySounds\NanomachinesActivate.ogg` | 玩家能力 | 3.64s | 68.6 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Nanomachines Activate。 代码标识：NanomachinesActivationSound。 | CalPlayer\CalamityPlayer.cs:476 (NanomachinesActivationSound) |
| 17 | `Custom\AbilitySounds\OmegaBlueAbility.ogg` | 玩家能力 | 1.93s | 39.0 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Omega Blue Ability。 代码标识：OverheatSound。 | Projectiles\Ranged\SpectralstormCannonHoldout.cs:28 (OverheatSound); Items\Armor\OmegaBlue\OmegaBlueHelmet.cs:19 (ActivationSound) |
| 18 | `Custom\AbilitySounds\OmegaBlueRecharge.ogg` | 玩家能力 | 0.86s | 17.4 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Omega Blue Recharge。 代码标识：EndSound。 | Cooldowns\OmegaBlue.cs:24 (EndSound) |
| 19 | `Custom\AbilitySounds\PlagueReaperAbility.ogg` | 玩家能力 | 1.30s | 27.2 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Plague Reaper Ability。 代码标识：ActivationSound。 | Items\Armor\PlagueReaper\PlagueReaperMask.cs:17 (ActivationSound) |
| 20 | `Custom\AbilitySounds\PlagueReaperRecharge.ogg` | 玩家能力 | 0.85s | 19.5 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Plague Reaper Recharge。 代码标识：EndSound。 | Cooldowns\PlagueBlackout.cs:22 (EndSound) |
| 21 | `Custom\AbilitySounds\PotionSicknessOver.ogg` | 玩家能力 | 1.50s | 34.2 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Potion Sickness Over。 代码标识：EndSound。 | Cooldowns\PanaceaCooldown.cs:16 (EndSound); Cooldowns\PotionSickness.cs:16 (EndSound) |
| 22 | `Custom\AbilitySounds\RageActivate.ogg` | 玩家能力 | 3.92s | 70.8 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Rage Activate。 代码标识：RageActivationSound。 | CalPlayer\CalamityPlayer.cs:469 (RageActivationSound) |
| 23 | `Custom\AbilitySounds\RageEnd.ogg` | 玩家能力 | 1.72s | 34.7 | 玩家能力/套装或冷却提示音效；动作：循环/开始结束/预警；主题：Rage End。 代码标识：RageEndSound。 | CalPlayer\CalamityPlayer.cs:470 (RageEndSound) |
| 24 | `Custom\AbilitySounds\SilvaActivation.ogg` | 玩家能力 | 2.41s | 36.4 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Silva Activation。 代码标识：AbsorberHit。 | CalPlayer\CalamityPlayer.cs:485 (AbsorberHit); Items\Armor\Silva\SilvaArmor.cs:16 (ActivationSound) |
| 25 | `Custom\AbilitySounds\SilvaDispel.ogg` | 玩家能力 | 2.16s | 30.8 | 玩家能力/套装或冷却提示音效；动作：通用/特殊；主题：Silva Dispel。 代码标识：DispelSound。 | Items\Armor\Silva\SilvaArmor.cs:17 (DispelSound) |
| 26 | `Custom\AbilitySounds\WulfrumBastionActivate.ogg` | 玩家能力 | 2.52s | 48.5 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Wulfrum Bastion Activate。 代码标识：SetActivationSound。 | Items\Armor\Wulfrum\WulfrumSet.cs:33 (SetActivationSound) |
| 27 | `Custom\AbilitySounds\WulfrumBastionBreak.ogg` | 玩家能力 | 2.40s | 49.5 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Wulfrum Bastion Break。 代码标识：SetBreakSound。 | Items\Armor\Wulfrum\WulfrumSet.cs:34 (SetBreakSound) |
| 28 | `Custom\AbilitySounds\WulfrumBastionBreakSafely.ogg` | 玩家能力 | 2.40s | 49.4 | 玩家能力/套装或冷却提示音效；动作：激活/使用/UI；主题：Wulfrum Bastion Break Safely。 代码标识：SetBreakSoundSafe。 | Items\Armor\Wulfrum\WulfrumSet.cs:35 (SetBreakSoundSafe) |
| 29 | `Custom\AbilitySounds\WulfrumBastionRecharge.ogg` | 玩家能力 | 1.46s | 26.2 | 玩家能力/套装或冷却提示音效；动作：蓄力/充能/冷却；主题：Wulfrum Bastion Recharge。 代码标识：EndSound。 | Cooldowns\WulfrumBastion.cs:29 (EndSound) |

### Custom\AstrumAureus

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 41 | `Custom\AstrumAureus\AstrumAureusSpawn.ogg` | 自定义/Boss/系统 | 2.38s | 34.7 | Custom\AstrumAureus 专用音效；动作：移动/生成/阶段转换；主题：Astrum Aureus Spawn。 代码标识：UseSound。 | Items\SummonItems\AstralChunk.cs:17 (UseSound) |
| 42 | `Custom\AstrumAureus\AureusJump.ogg` | 自定义/Boss/系统 | 1.48s | 25.3 | Custom\AstrumAureus 专用音效；动作：通用/特殊；主题：Aureus Jump。 代码标识：JumpSound。 | NPCs\AstrumAureus\AstrumAureus.cs:46 (JumpSound) |
| 43 | `Custom\AstrumAureus\AureusShoot.ogg` | 自定义/Boss/系统 | 0.44s | 13.8 | Custom\AstrumAureus 专用音效；动作：射击/发射；主题：Aureus Shoot。 代码标识：LaserSound。 | NPCs\AstrumAureus\AstrumAureus.cs:43 (LaserSound) |
| 44 | `Custom\AstrumAureus\AureusShootCrystal.ogg` | 自定义/Boss/系统 | 0.70s | 20.4 | Custom\AstrumAureus 专用音效；动作：射击/发射；主题：Aureus Shoot Crystal。 代码标识：FlameCrystalSound。 | NPCs\AstrumAureus\AstrumAureus.cs:44 (FlameCrystalSound) |
| 45 | `Custom\AstrumAureus\AureusTeleport.ogg` | 自定义/Boss/系统 | 1.21s | 32.0 | Custom\AstrumAureus 专用音效；动作：移动/生成/阶段转换；主题：Aureus Teleport。 代码标识：TeleportSound。 | NPCs\AstrumAureus\AstrumAureus.cs:47 (TeleportSound) |
| 46 | `Custom\AstrumAureus\LegStomp.ogg` | 自定义/Boss/系统 | 0.79s | 21.7 | Custom\AstrumAureus 专用音效；动作：近战挥击/撞击；主题：Leg Stomp。 代码标识：StompSound。 | NPCs\AstrumAureus\AstrumAureus.cs:45 (StompSound) |

### Custom\AstrumDeus

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 47 | `Custom\AstrumDeus\AstrumDeusGodRay.ogg` | 自定义/Boss/系统 | 0.48s | 11.3 | Custom\AstrumDeus 专用音效；动作：激光/光束；主题：Astrum Deus God Ray。 代码标识：GodRaySound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:43 (GodRaySound) |
| 48 | `Custom\AstrumDeus\AstrumDeusLaser.ogg` | 自定义/Boss/系统 | 0.42s | 10.9 | Custom\AstrumDeus 专用音效；动作：激光/光束；主题：Astrum Deus Laser。 代码标识：LaserSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:42 (LaserSound) |
| 49 | `Custom\AstrumDeus\AstrumDeusMine.ogg` | 自定义/Boss/系统 | 1.00s | 22.0 | Custom\AstrumDeus 专用音效；动作：采掘/破碎/物块碰撞；主题：Astrum Deus Mine。 代码标识：SmallBeamSound。 | Projectiles\Ranged\TauCannonHoldout.cs:73 (SmallBeamSound); NPCs\AstrumDeus\AstrumDeusHead.cs:44 (MineSound) |
| 50 | `Custom\AstrumDeus\AstrumDeusSpawn.ogg` | 自定义/Boss/系统 | 3.15s | 46.6 | Custom\AstrumDeus 专用音效；动作：移动/生成/阶段转换；主题：Astrum Deus Spawn。 代码标识：SpawnSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:41 (SpawnSound) |
| 51 | `Custom\AstrumDeus\AstrumDeusSplit.ogg` | 自定义/Boss/系统 | 0.93s | 15.6 | Custom\AstrumDeus 专用音效；动作：通用/特殊；主题：Astrum Deus Split。 代码标识：SplitSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:45 (SplitSound) |
| 52 | `Custom\AstrumDeus\DeusMineExplode.ogg` | 自定义/Boss/系统 | 0.45s | 11.8 | Custom\AstrumDeus 专用音效；动作：爆炸/爆裂；主题：Deus Mine Explode。 代码标识：ExplodeSound。 | Projectiles\Boss\DeusMine.cs:16 (ExplodeSound) |

### Custom\BEES

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 61 | `Custom\BEES\bees1.ogg` | 自定义/Boss/系统 | 0.44s | 13.4 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 62 | `Custom\BEES\bees10.ogg` | 自定义/Boss/系统 | 0.74s | 9.1 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 63 | `Custom\BEES\bees11.ogg` | 自定义/Boss/系统 | 0.91s | 15.1 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 64 | `Custom\BEES\bees12.ogg` | 自定义/Boss/系统 | 1.12s | 15.1 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 65 | `Custom\BEES\bees2.ogg` | 自定义/Boss/系统 | 0.73s | 8.2 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 66 | `Custom\BEES\bees3.ogg` | 自定义/Boss/系统 | 1.05s | 9.2 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 67 | `Custom\BEES\bees4.ogg` | 自定义/Boss/系统 | 0.75s | 9.2 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 68 | `Custom\BEES\bees5.ogg` | 自定义/Boss/系统 | 2.07s | 34.6 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 69 | `Custom\BEES\bees6.ogg` | 自定义/Boss/系统 | 1.23s | 10.4 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 70 | `Custom\BEES\bees7.ogg` | 自定义/Boss/系统 | 1.89s | 14.7 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 71 | `Custom\BEES\bees8.ogg` | 自定义/Boss/系统 | 1.30s | 10.8 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |
| 72 | `Custom\BEES\bees9.ogg` | 自定义/Boss/系统 | 1.00s | 11.1 | Custom\BEES 专用音效；动作：通用/特殊；主题：bees。 代码标识：bees。 | Projectiles\Ranged\HiveNuke.cs:182 (bees) |

### Custom\BossRush

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 75 | `Custom\BossRush\BossRushSummon1.ogg` | 自定义/Boss/系统 | 2.18s | 36.9 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Summon。 代码标识：BossSummonSound。 | Events\BossRushEvent.cs:119 (BossSummonSound) |
| 76 | `Custom\BossRush\BossRushSummon2.ogg` | 自定义/Boss/系统 | 2.31s | 37.0 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Summon。 代码标识：BossSummonSound。 | Events\BossRushEvent.cs:119 (BossSummonSound) |
| 77 | `Custom\BossRush\BossRushTeleport.ogg` | 自定义/Boss/系统 | 1.81s | 27.5 | Boss Rush 事件流程；动作：移动/生成/阶段转换；主题：Boss Rush Teleport。 代码标识：TeleportSound。 | Events\BossRushEvent.cs:121 (TeleportSound) |
| 78 | `Custom\BossRush\BossRushTerminusActivate.ogg` | 自定义/Boss/系统 | 5.81s | 81.9 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Terminus Activate。 代码标识：TerminusActivationSound。 | Events\BossRushEvent.cs:123 (TerminusActivationSound) |
| 79 | `Custom\BossRush\BossRushTerminusCharge.ogg` | 自定义/Boss/系统 | 5.52s | 78.0 | Boss Rush 事件流程；动作：蓄力/充能/冷却；主题：Boss Rush Terminus Charge。 代码标识：StartBuildupSound。 | Events\BossRushEvent.cs:125 (StartBuildupSound) |
| 80 | `Custom\BossRush\BossRushTerminusDeactivate.ogg` | 自定义/Boss/系统 | 3.81s | 54.2 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Terminus Deactivate。 代码标识：TerminusDeactivationSound。 | Events\BossRushEvent.cs:127 (TerminusDeactivationSound) |
| 81 | `Custom\BossRush\BossRushTier2Transition.ogg` | 自定义/Boss/系统 | 2.55s | 38.1 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Tier2 Transition。 代码标识：Tier2TransitionSound。 | Events\BossRushEvent.cs:129 (Tier2TransitionSound) |
| 82 | `Custom\BossRush\BossRushTier3Transition.ogg` | 自定义/Boss/系统 | 2.59s | 44.9 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Tier3 Transition。 代码标识：Tier3TransitionSound。 | Events\BossRushEvent.cs:131 (Tier3TransitionSound) |
| 83 | `Custom\BossRush\BossRushTier4Transition.ogg` | 自定义/Boss/系统 | 3.40s | 53.9 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Tier4 Transition。 代码标识：Tier4TransitionSound。 | Events\BossRushEvent.cs:133 (Tier4TransitionSound) |
| 84 | `Custom\BossRush\BossRushTier5Transition.ogg` | 自定义/Boss/系统 | 5.01s | 72.6 | Boss Rush 事件流程；动作：激活/使用/UI；主题：Boss Rush Tier5 Transition。 代码标识：Tier5TransitionSound。 | Events\BossRushEvent.cs:135 (Tier5TransitionSound) |
| 85 | `Custom\BossRush\BossRushVictory.ogg` | 自定义/Boss/系统 | 5.95s | 96.8 | Boss Rush 事件流程；动作：通用/特殊；主题：Boss Rush Victory。 代码标识：VictorySound。 | Events\BossRushEvent.cs:137 (VictorySound) |

### Custom\BrainOfCthulhu

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 86 | `Custom\BrainOfCthulhu\BoC_Rev_BloodBomb.ogg` | 自定义/Boss/系统 | 1.09s | 22.4 | Custom\BrainOfCthulhu 专用音效；动作：通用/特殊；主题：Bo C Rev Blood Bomb。 代码标识：BloodBomb。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:44 (BloodBomb) |
| 87 | `Custom\BrainOfCthulhu\BoC_Rev_BloodShot.ogg` | 自定义/Boss/系统 | 1.29s | 21.5 | Custom\BrainOfCthulhu 专用音效；动作：射击/发射；主题：Bo C Rev Blood Shot。 代码标识：BloodShot。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:43 (BloodShot) |
| 88 | `Custom\BrainOfCthulhu\BoC_Rev_Death_Roar.ogg` | 自定义/Boss/系统 | 4.37s | 66.9 | Custom\BrainOfCthulhu 专用音效；动作：死亡/击杀；主题：Bo C Rev Death Roar。 代码标识：Death。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:42 (Death) |
| 89 | `Custom\BrainOfCthulhu\BoC_Rev_Explosion1.ogg` | 自定义/Boss/系统 | 2.37s | 45.5 | Custom\BrainOfCthulhu 专用音效；动作：爆炸/爆裂；主题：Bo C Rev Explosion。 代码标识：BloodExplosion。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:45 (BloodExplosion) |
| 90 | `Custom\BrainOfCthulhu\BoC_Rev_Explosion2.ogg` | 自定义/Boss/系统 | 2.37s | 44.4 | Custom\BrainOfCthulhu 专用音效；动作：爆炸/爆裂；主题：Bo C Rev Explosion。 代码标识：BloodExplosion。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:45 (BloodExplosion) |
| 91 | `Custom\BrainOfCthulhu\BoC_Rev_Growl1.ogg` | 自定义/Boss/系统 | 3.80s | 53.0 | Custom\BrainOfCthulhu 专用音效；动作：吼叫/语音；主题：Bo C Rev Growl。 代码标识：Growl。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:41 (Growl) |
| 92 | `Custom\BrainOfCthulhu\BoC_Rev_Growl2.ogg` | 自定义/Boss/系统 | 3.24s | 44.7 | Custom\BrainOfCthulhu 专用音效；动作：吼叫/语音；主题：Bo C Rev Growl。 代码标识：Growl。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:41 (Growl) |
| 93 | `Custom\BrainOfCthulhu\BoC_Rev_Laugh.ogg` | 自定义/Boss/系统 | 5.47s | 87.7 | Custom\BrainOfCthulhu 专用音效；动作：吼叫/语音；主题：Bo C Rev Laugh。 代码标识：Laugh。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:40 (Laugh) |
| 94 | `Custom\BrainOfCthulhu\BoC_Rev_Roar.ogg` | 自定义/Boss/系统 | 4.25s | 85.8 | Custom\BrainOfCthulhu 专用音效；动作：吼叫/语音；主题：Bo C Rev Roar。 代码标识：IntroRoar。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:38 (IntroRoar) |
| 95 | `Custom\BrainOfCthulhu\BoC_Rev_Shield_Down.ogg` | 自定义/Boss/系统 | 2.00s | 30.7 | Custom\BrainOfCthulhu 专用音效；动作：通用/特殊；主题：Bo C Rev Shield Down。 代码标识：ShieldDown。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:36 (ShieldDown) |
| 96 | `Custom\BrainOfCthulhu\BoC_Rev_Shield_Up.ogg` | 自定义/Boss/系统 | 2.87s | 39.7 | Custom\BrainOfCthulhu 专用音效；动作：通用/特殊；主题：Bo C Rev Shield Up。 代码标识：ShieldUp。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:37 (ShieldUp) |
| 97 | `Custom\BrainOfCthulhu\BoC_Rev_Short_Roar.ogg` | 自定义/Boss/系统 | 3.40s | 66.7 | Custom\BrainOfCthulhu 专用音效；动作：吼叫/语音；主题：Bo C Rev Short Roar。 代码标识：Roar。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:39 (Roar) |
| 98 | `Custom\BrainOfCthulhu\BoC_Rev_Stun_Hit1.ogg` | 自定义/Boss/系统 | 1.00s | 17.6 | Custom\BrainOfCthulhu 专用音效；动作：受击/命中/冲击；主题：Bo C Rev Stun Hit。 代码标识：StunnedHit。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:35 (StunnedHit) |
| 99 | `Custom\BrainOfCthulhu\BoC_Rev_Stun_Hit2.ogg` | 自定义/Boss/系统 | 1.00s | 17.8 | Custom\BrainOfCthulhu 专用音效；动作：受击/命中/冲击；主题：Bo C Rev Stun Hit。 代码标识：StunnedHit。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:35 (StunnedHit) |
| 100 | `Custom\BrainOfCthulhu\BoC_Rev_Stun_Hit3.ogg` | 自定义/Boss/系统 | 1.00s | 18.3 | Custom\BrainOfCthulhu 专用音效；动作：受击/命中/冲击；主题：Bo C Rev Stun Hit。 代码标识：StunnedHit。 | NPCs\VanillaNPCAIOverrides\Bosses\BrainOfCthulhu\BrainOfCthulhuAI.cs:35 (StunnedHit) |

### Custom\BrimstoneElemental

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 101 | `Custom\BrimstoneElemental\BrimstoneDartRing1.ogg` | 自定义/Boss/系统 | 0.51s | 13.9 | Custom\BrimstoneElemental 专用音效；动作：激活/使用/UI；主题：Brimstone Dart Ring。 代码标识：DartSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:50 (DartSound) |
| 102 | `Custom\BrimstoneElemental\BrimstoneDartRing2.ogg` | 自定义/Boss/系统 | 0.51s | 12.5 | Custom\BrimstoneElemental 专用音效；动作：激活/使用/UI；主题：Brimstone Dart Ring。 代码标识：DartSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:50 (DartSound) |
| 103 | `Custom\BrimstoneElemental\BrimstoneDartRing3.ogg` | 自定义/Boss/系统 | 0.51s | 13.9 | Custom\BrimstoneElemental 专用音效；动作：激活/使用/UI；主题：Brimstone Dart Ring。 代码标识：DartSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:50 (DartSound) |
| 104 | `Custom\BrimstoneElemental\BrimstoneSpawn.ogg` | 自定义/Boss/系统 | 1.82s | 28.3 | Custom\BrimstoneElemental 专用音效；动作：激活/使用/UI；主题：Brimstone Spawn。 代码标识：UseSound。 | Items\SummonItems\CharredIdol.cs:15 (UseSound) |
| 105 | `Custom\BrimstoneElemental\Hellfireball1.ogg` | 自定义/Boss/系统 | 0.51s | 11.4 | Custom\BrimstoneElemental 专用音效；动作：射击/发射；主题：Hellfireball。 代码标识：HellfireballSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:49 (HellfireballSound) |
| 106 | `Custom\BrimstoneElemental\Hellfireball2.ogg` | 自定义/Boss/系统 | 0.48s | 10.8 | Custom\BrimstoneElemental 专用音效；动作：射击/发射；主题：Hellfireball。 代码标识：HellfireballSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:49 (HellfireballSound) |
| 107 | `Custom\BrimstoneElemental\Hellfireball3.ogg` | 自定义/Boss/系统 | 0.47s | 10.5 | Custom\BrimstoneElemental 专用音效；动作：射击/发射；主题：Hellfireball。 代码标识：HellfireballSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:49 (HellfireballSound) |
| 108 | `Custom\BrimstoneElemental\ShellProjectiles1.ogg` | 自定义/Boss/系统 | 0.89s | 12.1 | Custom\BrimstoneElemental 专用音效；动作：通用/特殊；主题：Shell Projectiles。 代码标识：ShellFireSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:52 (ShellFireSound) |
| 109 | `Custom\BrimstoneElemental\ShellProjectiles2.ogg` | 自定义/Boss/系统 | 0.89s | 12.4 | Custom\BrimstoneElemental 专用音效；动作：通用/特殊；主题：Shell Projectiles。 代码标识：ShellFireSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:52 (ShellFireSound) |
| 110 | `Custom\BrimstoneElemental\ShellProjectiles3.ogg` | 自定义/Boss/系统 | 0.89s | 11.6 | Custom\BrimstoneElemental 专用音效；动作：通用/特殊；主题：Shell Projectiles。 代码标识：ShellFireSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:52 (ShellFireSound) |
| 111 | `Custom\BrimstoneElemental\ShellTransform.ogg` | 自定义/Boss/系统 | 0.60s | 7.4 | Custom\BrimstoneElemental 专用音效；动作：通用/特殊；主题：Shell Transform。 代码标识：HideInShellSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:51 (HideInShellSound) |
| 112 | `Custom\BrimstoneElemental\Teleport.ogg` | 自定义/Boss/系统 | 0.71s | 8.9 | Custom\BrimstoneElemental 专用音效；动作：移动/生成/阶段转换；主题：Teleport。 代码标识：TeleportSound。 | NPCs\BrimstoneElemental\BrimstoneElemental.cs:48 (TeleportSound) |

### Custom\CalamitasClone

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 117 | `Custom\CalamitasClone\BrimstoneFlamethrowerCast.ogg` | 自定义/Boss/系统 | 0.88s | 17.7 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Brimstone Flamethrower Cast。 代码标识：FlamethrowerStart。 | NPCs\CalClone\Cataclysm.cs:30 (FlamethrowerStart) |
| 118 | `Custom\CalamitasClone\BrimstoneFlamethrowerLoop.ogg` | 自定义/Boss/系统 | 1.62s | 31.8 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Brimstone Flamethrower Loop。 代码标识：FlamethrowerLoop。 | NPCs\CalClone\Cataclysm.cs:31 (FlamethrowerLoop) |
| 119 | `Custom\CalamitasClone\BulletHellEnd.ogg` | 自定义/Boss/系统 | 2.43s | 30.9 | Custom\CalamitasClone 专用音效；动作：循环/开始结束/预警；主题：Bullet Hell End。 代码标识：BulletHellEnd。 | NPCs\CalClone\CalamitasClone.cs:43 (BulletHellEnd) |
| 120 | `Custom\CalamitasClone\BulletHellEnding.ogg` | 自定义/Boss/系统 | 6.20s | 69.5 | Custom\CalamitasClone 专用音效；动作：循环/开始结束/预警；主题：Bullet Hell Ending。 代码标识：BulletHellWarning。 | NPCs\CalClone\CalamitasClone.cs:42 (BulletHellWarning) |
| 121 | `Custom\CalamitasClone\CalClone_BigFireballBit1.ogg` | 自定义/Boss/系统 | 2.05s | 40.7 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Cal Clone Big Fireball Bit。 代码标识：CalamitousFireballSound。 | NPCs\CalClone\CalamitasClone.cs:45 (CalamitousFireballSound) |
| 122 | `Custom\CalamitasClone\CalClone_BigFireballBit2.ogg` | 自定义/Boss/系统 | 2.77s | 50.8 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Cal Clone Big Fireball Bit。 代码标识：CalamitousFireballSound。 | NPCs\CalClone\CalamitasClone.cs:45 (CalamitousFireballSound) |
| 123 | `Custom\CalamitasClone\CalClone_BigFireballBit3.ogg` | 自定义/Boss/系统 | 2.23s | 42.9 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Cal Clone Big Fireball Bit。 代码标识：CalamitousFireballSound。 | NPCs\CalClone\CalamitasClone.cs:45 (CalamitousFireballSound) |
| 124 | `Custom\CalamitasClone\CalClone_BigFireballBit4.ogg` | 自定义/Boss/系统 | 2.49s | 46.7 | Custom\CalamitasClone 专用音效；动作：射击/发射；主题：Cal Clone Big Fireball Bit。 代码标识：CalamitousFireballSound。 | NPCs\CalClone\CalamitasClone.cs:45 (CalamitousFireballSound) |
| 125 | `Custom\CalamitasClone\CalClone_Explosion1.ogg` | 自定义/Boss/系统 | 3.91s | 72.8 | Custom\CalamitasClone 专用音效；动作：爆炸/爆裂；主题：Cal Clone Explosion。 代码标识：CalamitousExplosionSound。 | NPCs\CalClone\CalamitasClone.cs:46 (CalamitousExplosionSound) |
| 126 | `Custom\CalamitasClone\CalClone_Explosion2.ogg` | 自定义/Boss/系统 | 4.14s | 75.5 | Custom\CalamitasClone 专用音效；动作：爆炸/爆裂；主题：Cal Clone Explosion。 代码标识：CalamitousExplosionSound。 | NPCs\CalClone\CalamitasClone.cs:46 (CalamitousExplosionSound) |
| 127 | `Custom\CalamitasClone\CalClone_Explosion3.ogg` | 自定义/Boss/系统 | 3.95s | 73.1 | Custom\CalamitasClone 专用音效；动作：爆炸/爆裂；主题：Cal Clone Explosion。 代码标识：CalamitousExplosionSound。 | NPCs\CalClone\CalamitasClone.cs:46 (CalamitousExplosionSound) |
| 128 | `Custom\CalamitasClone\CalCloneDash1.ogg` | 自定义/Boss/系统 | 1.32s | 18.9 | Custom\CalamitasClone 专用音效；动作：激活/使用/UI；主题：Cal Clone Dash。 代码标识：ChargeSound。 | NPCs\CalClone\CalamitasClone.cs:44 (ChargeSound) |
| 129 | `Custom\CalamitasClone\CalCloneDash2.ogg` | 自定义/Boss/系统 | 1.32s | 18.2 | Custom\CalamitasClone 专用音效；动作：激活/使用/UI；主题：Cal Clone Dash。 代码标识：ChargeSound。 | NPCs\CalClone\CalamitasClone.cs:44 (ChargeSound) |
| 130 | `Custom\CalamitasClone\CalCloneDash3.ogg` | 自定义/Boss/系统 | 1.32s | 17.9 | Custom\CalamitasClone 专用音效；动作：激活/使用/UI；主题：Cal Clone Dash。 代码标识：ChargeSound。 | NPCs\CalClone\CalamitasClone.cs:44 (ChargeSound) |
| 131 | `Custom\CalamitasClone\CataclysmDeath.ogg` | 自定义/Boss/系统 | 0.86s | 17.9 | Custom\CalamitasClone 专用音效；动作：死亡/击杀；主题：Cataclysm Death。 代码标识：DeathSound。 | NPCs\CalClone\Cataclysm.cs:29 (DeathSound) |
| 132 | `Custom\CalamitasClone\CataclysmHit1.ogg` | 自定义/Boss/系统 | 0.44s | 10.3 | Custom\CalamitasClone 专用音效；动作：受击/命中/冲击；主题：Cataclysm Hit。 代码标识：HitSound。 | NPCs\CalClone\Cataclysm.cs:28 (HitSound) |
| 133 | `Custom\CalamitasClone\CataclysmHit2.ogg` | 自定义/Boss/系统 | 0.46s | 10.8 | Custom\CalamitasClone 专用音效；动作：受击/命中/冲击；主题：Cataclysm Hit。 代码标识：HitSound。 | NPCs\CalClone\Cataclysm.cs:28 (HitSound) |
| 134 | `Custom\CalamitasClone\CataclysmHit3.ogg` | 自定义/Boss/系统 | 0.46s | 10.7 | Custom\CalamitasClone 专用音效；动作：受击/命中/冲击；主题：Cataclysm Hit。 代码标识：HitSound。 | NPCs\CalClone\Cataclysm.cs:28 (HitSound) |

### Custom\Codebreaker

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 144 | `Custom\Codebreaker\AdvancedDisplayInstall.ogg` | 自定义/Boss/系统 | 2.52s | 69.1 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Advanced Display Install。 代码标识：InstallSound。 | Items\DraedonMisc\AdvancedDisplay.cs:18 (InstallSound) |
| 145 | `Custom\Codebreaker\AresIconHover.ogg` | 自定义/Boss/系统 | 0.57s | 14.6 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Ares Icon Hover。 代码标识：AresHoverSound。 | UI\DraedonSummoning\ExoMechSelectionUI.cs:34 (AresHoverSound) |
| 146 | `Custom\Codebreaker\ArtemisApolloIconHover.ogg` | 自定义/Boss/系统 | 0.84s | 17.5 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Artemis Apollo Icon Hover。 代码标识：TwinsHoverSound。 | UI\DraedonSummoning\ExoMechSelectionUI.cs:36 (TwinsHoverSound) |
| 147 | `Custom\Codebreaker\AuricQuantumCoolingCellInstallNew.ogg` | 自定义/Boss/系统 | 5.03s | 106.9 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Auric Quantum Cooling Cell Install New。 代码标识：InstallSound。 | Items\DraedonMisc\AuricQuantumCoolingCell.cs:20 (InstallSound) |
| 148 | `Custom\Codebreaker\BloodForHekate.ogg` | 自定义/Boss/系统 | 1.64s | 47.9 | Codebreaker UI、Draedon 对话和部件安装；动作：通用/特殊；主题：Blood For Hekate。 代码标识：BloodSound。 | UI\DraedonSummoning\CodebreakerUI.cs:100 (BloodSound) |
| 149 | `Custom\Codebreaker\DecryptionComputerInstall.ogg` | 自定义/Boss/系统 | 2.01s | 52.9 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Decryption Computer Install。 代码标识：InstallSound。 | Items\DraedonMisc\DecryptionComputer.cs:18 (InstallSound) |
| 150 | `Custom\Codebreaker\DialogOptionHover.ogg` | 自定义/Boss/系统 | 0.01s | 5.0 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Dialog Option Hover。 代码标识：DialogOptionHoverSound。 | UI\DraedonSummoning\CodebreakerUI.Communication.cs:234 (DialogOptionHoverSound) |
| 151 | `Custom\Codebreaker\DraedonTalk1.ogg` | 自定义/Boss/系统 | 0.08s | 6.8 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Draedon Talk。 首处代码上下文：CodebreakerUI.Communication.cs。 | UI\DraedonSummoning\CodebreakerUI.Communication.cs:241 |
| 152 | `Custom\Codebreaker\DraedonTalk2.ogg` | 自定义/Boss/系统 | 0.07s | 6.3 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Draedon Talk。 首处代码上下文：CodebreakerUI.Communication.cs。 | UI\DraedonSummoning\CodebreakerUI.Communication.cs:242 |
| 153 | `Custom\Codebreaker\DraedonTalk3.ogg` | 自定义/Boss/系统 | 0.09s | 6.6 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Draedon Talk。 首处代码上下文：CodebreakerUI.Communication.cs。 | UI\DraedonSummoning\CodebreakerUI.Communication.cs:243 |
| 154 | `Custom\Codebreaker\ExoMechsIconSelect.ogg` | 自定义/Boss/系统 | 0.21s | 9.2 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Exo Mechs Icon Select。 代码标识：SelectionSound。 | NPCs\ExoMechs\Draedon.cs:84 (SelectionSound) |
| 155 | `Custom\Codebreaker\LongRangeSensorArrayInstall.ogg` | 自定义/Boss/系统 | 2.03s | 48.9 | Codebreaker UI、Draedon 对话和部件安装；动作：激光/光束；主题：Long Range Sensor Array Install。 代码标识：InstallSound。 | Items\DraedonMisc\LongRangedSensorArray.cs:18 (InstallSound) |
| 156 | `Custom\Codebreaker\ThanatosIconHover.ogg` | 自定义/Boss/系统 | 0.61s | 14.4 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Thanatos Icon Hover。 代码标识：ThanatosHoverSound。 | UI\DraedonSummoning\ExoMechSelectionUI.cs:32 (ThanatosHoverSound) |
| 157 | `Custom\Codebreaker\VoltageRegulationSystemInstall.ogg` | 自定义/Boss/系统 | 3.30s | 60.1 | Codebreaker UI、Draedon 对话和部件安装；动作：激活/使用/UI；主题：Voltage Regulation System Install。 代码标识：InstallSound。 | Items\DraedonMisc\VoltageRegulationSystem.cs:19 (InstallSound) |

### Custom\Crabulon

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 160 | `Custom\Crabulon\CrabJump.ogg` | 自定义/Boss/系统 | 0.69s | 12.4 | Custom\Crabulon 专用音效；动作：通用/特殊；主题：Crab Jump。 代码标识：JumpSound。 | NPCs\Crabulon\Crabulon.cs:45 (JumpSound) |
| 161 | `Custom\Crabulon\CrabSlam1.ogg` | 自定义/Boss/系统 | 0.94s | 15.4 | Custom\Crabulon 专用音效；动作：近战挥击/撞击；主题：Crab Slam。 代码标识：SlamSound。 | NPCs\Crabulon\Crabulon.cs:46 (SlamSound) |
| 162 | `Custom\Crabulon\CrabSlam2.ogg` | 自定义/Boss/系统 | 0.90s | 14.9 | Custom\Crabulon 专用音效；动作：近战挥击/撞击；主题：Crab Slam。 代码标识：SlamSound。 | NPCs\Crabulon\Crabulon.cs:46 (SlamSound) |

### Custom\DesertScourge

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 172 | `Custom\DesertScourge\DesertScourgeRoar.ogg` | 自定义/Boss/系统 | 2.53s | 36.5 | Custom\DesertScourge 专用音效；动作：吼叫/语音；主题：Desert Scourge Roar。 代码标识：RoarSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:65 (RoarSound) |
| 173 | `Custom\DesertScourge\DesertScourgeSandBlast.ogg` | 自定义/Boss/系统 | 2.86s | 52.0 | Custom\DesertScourge 专用音效；动作：爆炸/爆裂；主题：Desert Scourge Sand Blast。 代码标识：SandBlastSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:66 (SandBlastSound) |
| 174 | `Custom\DesertScourge\DesertScourgeSummon.ogg` | 自定义/Boss/系统 | 2.22s | 34.2 | Custom\DesertScourge 专用音效；动作：激活/使用/UI；主题：Desert Scourge Summon。 代码标识：SummonSound。 | Items\SummonItems\DesertMedallion.cs:15 (SummonSound) |

### Custom\DifficultySelection

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 180 | `Custom\DifficultySelection\Death_Mode_Select.ogg` | 自定义/Boss/系统 | 5.00s | 83.2 | 难度选择 UI 音效；动作：死亡/击杀；主题：Death Mode Select。 代码标识：ActivationSound。 | Systems\Mechanic\DifficultyModeSystem.cs:561 (ActivationSound) |
| 181 | `Custom\DifficultySelection\Legendary_Mode_Select.ogg` | 自定义/Boss/系统 | 1.19s | 21.6 | 难度选择 UI 音效；动作：激活/使用/UI；主题：Legendary Mode Select。 代码标识：ActivationSound。 | Systems\Mechanic\DifficultyModeSystem.cs:470 (ActivationSound) |
| 182 | `Custom\DifficultySelection\Malice_Mode_Select.ogg` | 自定义/Boss/系统 | 3.81s | 66.9 | 难度选择 UI 音效；动作：激活/使用/UI；主题：Malice Mode Select。 代码标识：ActivationSound。 | Systems\Mechanic\DifficultyModeSystem.cs:622 (ActivationSound) |
| 183 | `Custom\DifficultySelection\Revengeance_Mode_Select.ogg` | 自定义/Boss/系统 | 3.12s | 58.0 | 难度选择 UI 音效；动作：激活/使用/UI；主题：Revengeance Mode Select。 代码标识：ActivationSound。 | Systems\Mechanic\DifficultyModeSystem.cs:505 (ActivationSound) |

### Custom\ExoMechs

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 195 | `Custom\ExoMechs\ApolloArtemisTargetSelection.ogg` | 自定义/Boss/系统 | 0.15s | 8.2 | Exo Mechs / Draedon 机械 Boss 音效；动作：激活/使用/UI；主题：Apollo Artemis Target Selection。 代码标识：lockon。 | Projectiles\Rogue\SupernovaBomb.cs:125 (lockon); Projectiles\Rogue\SupernovaBomb.cs:132 (lockon); Projectiles\Rogue\SupernovaBomb.cs:168 (lockon); 另 1 处 |
| 196 | `Custom\ExoMechs\ApolloMissileLaunch.ogg` | 自定义/Boss/系统 | 1.84s | 35.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Apollo Missile Launch。 代码标识：MissileLaunchSound。 | NPCs\ExoMechs\Apollo\Apollo.cs:124 (MissileLaunchSound) |
| 197 | `Custom\ExoMechs\AresCircleLaserEnd.ogg` | 自定义/Boss/系统 | 3.87s | 59.0 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：Ares Circle Laser End。 代码标识：LaserEndSound。 | NPCs\ExoMechs\Ares\AresBody.cs:160 (LaserEndSound) |
| 198 | `Custom\ExoMechs\AresCircleLaserLoop.ogg` | 自定义/Boss/系统 | 3.04s | 57.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：Ares Circle Laser Loop。 代码标识：LaserLoopSound。 | NPCs\ExoMechs\Ares\AresBody.cs:158 (LaserLoopSound) |
| 199 | `Custom\ExoMechs\AresCircleLaserStart.ogg` | 自定义/Boss/系统 | 3.87s | 69.6 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：Ares Circle Laser Start。 代码标识：LaserStartSound。 | NPCs\ExoMechs\Ares\AresBody.cs:156 (LaserStartSound) |
| 200 | `Custom\ExoMechs\AresEnraged.ogg` | 自定义/Boss/系统 | 3.47s | 164.4 | Exo Mechs / Draedon 机械 Boss 音效；动作：通用/特殊；主题：Ares Enraged。 代码标识：EnragedSound。 | NPCs\ExoMechs\Ares\AresBody.cs:154 (EnragedSound) |
| 201 | `Custom\ExoMechs\AresGaussNukeArmCharge.ogg` | 自定义/Boss/系统 | 3.05s | 51.3 | Exo Mechs / Draedon 机械 Boss 音效；动作：爆炸/爆裂；主题：Ares Gauss Nuke Arm Charge。 代码标识：TelSound。 | NPCs\ExoMechs\Ares\AresGaussNuke.cs:71 (TelSound) |
| 202 | `Custom\ExoMechs\AresGaussNukeExplosion.ogg` | 自定义/Boss/系统 | 2.85s | 48.8 | Exo Mechs / Draedon 机械 Boss 音效；动作：爆炸/爆裂；主题：Ares Gauss Nuke Explosion。 代码标识：gfbExplosion。 | Projectiles\Rogue\DestructionStar.cs:176 (gfbExplosion); NPCs\ExoMechs\Ares\AresGaussNuke.cs:73 (NukeExplosionSound) |
| 203 | `Custom\ExoMechs\AresLaserArmCharge.ogg` | 自定义/Boss/系统 | 3.05s | 73.2 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：Ares Laser Arm Charge。 代码标识：laserCharge。 | Projectiles\Summon\StellarTorusSummon.cs:88 (laserCharge); NPCs\ExoMechs\Ares\AresLaserCannon.cs:73 (TelSound) |
| 204 | `Custom\ExoMechs\AresLaserArmShoot.ogg` | 自定义/Boss/系统 | 2.34s | 45.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Ares Laser Arm Shoot。 代码标识：LaserbeamShootSound。 | NPCs\ExoMechs\Ares\AresLaserCannon.cs:75 (LaserbeamShootSound) |
| 205 | `Custom\ExoMechs\AresPlasmaArmCharge.ogg` | 自定义/Boss/系统 | 3.05s | 45.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：蓄力/充能/冷却；主题：Ares Plasma Arm Charge。 代码标识：TelSound。 | NPCs\ExoMechs\Ares\AresPlasmaFlamethrower.cs:69 (TelSound) |
| 206 | `Custom\ExoMechs\AresTeslaArmCharge.ogg` | 自定义/Boss/系统 | 3.05s | 56.6 | Exo Mechs / Draedon 机械 Boss 音效；动作：蓄力/充能/冷却；主题：Ares Tesla Arm Charge。 代码标识：TelSound。 | NPCs\ExoMechs\Ares\AresTeslaCannon.cs:69 (TelSound) |
| 207 | `Custom\ExoMechs\ArtemisApolloDash.ogg` | 自定义/Boss/系统 | 1.05s | 21.9 | Exo Mechs / Draedon 机械 Boss 音效；动作：移动/生成/阶段转换；主题：Artemis Apollo Dash。 代码标识：fire。 | Projectiles\Magic\OmicronBeam.cs:155 (fire); NPCs\ExoMechs\Artemis\Artemis.cs:35 (ChargeSound); Items\Mounts\ExoTank.cs:27 (MissileLaunchSound) |
| 208 | `Custom\ExoMechs\ArtemisApolloDashTelegraph.ogg` | 自定义/Boss/系统 | 0.69s | 15.4 | Exo Mechs / Draedon 机械 Boss 音效；动作：移动/生成/阶段转换；主题：Artemis Apollo Dash Telegraph。 代码标识：ChargeTelegraphSound。 | NPCs\ExoMechs\Artemis\Artemis.cs:37 (ChargeTelegraphSound) |
| 209 | `Custom\ExoMechs\ArtemisShotgunLaser.ogg` | 自定义/Boss/系统 | 1.06s | 23.1 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Artemis Shotgun Laser。 代码标识：LaserShotgunSound。 | NPCs\ExoMechs\Artemis\Artemis.cs:41 (LaserShotgunSound) |
| 210 | `Custom\ExoMechs\ArtemisSpinLaserbeam.ogg` | 自定义/Boss/系统 | 4.10s | 105.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：Artemis Spin Laserbeam。 代码标识：SpinLaserbeamSound。 | NPCs\ExoMechs\Artemis\Artemis.cs:43 (SpinLaserbeamSound) |
| 211 | `Custom\ExoMechs\ExoLaserShoot.ogg` | 自定义/Boss/系统 | 0.66s | 17.2 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Exo Laser Shoot。 代码标识：ExoLaserShootSound。 | Sounds\CommonCalamitySounds.cs:13 (ExoLaserShootSound); Projectiles\Ranged\TauCannonHoldout.cs:72 (BoltShootSound) |
| 212 | `Custom\ExoMechs\ExoPlasmaExplosion1.ogg` | 自定义/Boss/系统 | 1.13s | 21.2 | Exo Mechs / Draedon 机械 Boss 音效；动作：爆炸/爆裂；主题：Exo Plasma Explosion。 代码标识：ExoPlasmaExplosionSound。 | Sounds\CommonCalamitySounds.cs:14 (ExoPlasmaExplosionSound) |
| 213 | `Custom\ExoMechs\ExoPlasmaExplosion2.ogg` | 自定义/Boss/系统 | 1.17s | 21.5 | Exo Mechs / Draedon 机械 Boss 音效；动作：爆炸/爆裂；主题：Exo Plasma Explosion。 代码标识：ExoPlasmaExplosionSound。 | Sounds\CommonCalamitySounds.cs:14 (ExoPlasmaExplosionSound) |
| 214 | `Custom\ExoMechs\ExoPlasmaShoot.ogg` | 自定义/Boss/系统 | 1.29s | 21.6 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Exo Plasma Shoot。 代码标识：ExoPlasmaShootSound。 | Sounds\CommonCalamitySounds.cs:15 (ExoPlasmaShootSound) |
| 215 | `Custom\ExoMechs\ExoTwinsEject.ogg` | 自定义/Boss/系统 | 3.10s | 48.7 | Exo Mechs / Draedon 机械 Boss 音效；动作：通用/特殊；主题：Exo Twins Eject。 代码标识：LensSound。 | NPCs\ExoMechs\Artemis\Artemis.cs:39 (LensSound) |
| 216 | `Custom\ExoMechs\TeslaShoot1.ogg` | 自定义/Boss/系统 | 0.88s | 20.1 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Tesla Shoot。 首处代码上下文：CalamityPlayerMiscEffects.cs。 | CalPlayer\CalamityPlayerMiscEffects.cs:1034; NPCs\CalamityGlobalNPC.cs:3433; Projectiles\CalamityGlobalProjectile.cs:4032; 另 3 处 |
| 217 | `Custom\ExoMechs\TeslaShoot2.ogg` | 自定义/Boss/系统 | 0.87s | 20.0 | Exo Mechs / Draedon 机械 Boss 音效；动作：射击/发射；主题：Tesla Shoot。 代码标识：TeslaOrbShootSound。 | NPCs\ExoMechs\Ares\AresTeslaCannon.cs:71 (TeslaOrbShootSound) |
| 218 | `Custom\ExoMechs\ThanatosVent.ogg` | 自定义/Boss/系统 | 3.15s | 44.6 | Exo Mechs / Draedon 机械 Boss 音效；动作：通用/特殊；主题：Thanatos Vent。 代码标识：CoolingDownSound。 | Projectiles\Ranged\TauCannonHoldout.cs:75 (CoolingDownSound); NPCs\ExoMechs\Thanatos\ThanatosHead.cs:32 (VentSound) |
| 219 | `Custom\ExoMechs\THanosGFBeam.ogg` | 自定义/Boss/系统 | 8.93s | 121.7 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：T Hanos GF Beam。 代码标识：GFBeam。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:36 (GFBeam) |
| 220 | `Custom\ExoMechs\THanosLaser.ogg` | 自定义/Boss/系统 | 6.80s | 91.6 | Exo Mechs / Draedon 机械 Boss 音效；动作：激光/光束；主题：T Hanos Laser。 代码标识：LaserSound。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:34 (LaserSound) |

### Custom\GFB

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 222 | `Custom\GFB\GrandDad.ogg` | 自定义/Boss/系统 | 1.24s | 20.4 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：Grand Dad。 代码标识：GrandDadEasterEggSound。 | Items\Weapons\Melee\GrandDad.cs:23 (GrandDadEasterEggSound) |
| 223 | `Custom\GFB\HeComes.ogg` | 自定义/Boss/系统 | 0.76s | 32.5 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：He Comes。 代码标识：h。 | Projectiles\Summon\OldDukeHeadCorpse.cs:61 (h) |
| 224 | `Custom\GFB\Jesus.ogg` | 自定义/Boss/系统 | 3.24s | 62.5 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：Jesus。 代码标识：gong。 | Projectiles\Rogue\ExorcismProj.cs:358 (gong) |
| 225 | `Custom\GFB\LeonDeathNoiseRE4.ogg` | 自定义/Boss/系统 | 2.13s | 78.8 | Get fixed boi / Zenith world 彩蛋音效；动作：死亡/击杀；主题：Leon Death Noise RE。 代码标识：LeonDeathNoiseRE4_ForGFB。 | CalPlayer\CalamityPlayer.cs:483 (LeonDeathNoiseRE4_ForGFB) |
| 226 | `Custom\GFB\SevenTrebleClefSouls.ogg` | 自定义/Boss/系统 | 40.30s | 606.4 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：Seven Treble Clef Souls。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:75; Projectiles\Magic\AnahitasArpeggioNote.cs:83 |
| 227 | `Custom\GFB\YouAreNotSafe.ogg` | 自定义/Boss/系统 | 9.06s | 424.5 | Get fixed boi / Zenith world 彩蛋音效；动作：通用/特殊；主题：You Are Not Safe。 代码标识：h。 | Projectiles\Summon\OldDukeHeadCorpse.cs:87 (h) |

### Custom\Perforator

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 274 | `Custom\Perforator\PerfHiveIchorShoot.ogg` | 自定义/Boss/系统 | 1.15s | 48.0 | Custom\Perforator 专用音效；动作：射击/发射；主题：Perf Hive Ichor Shoot。 代码标识：bigShot。 | Projectiles\Ranged\FetidEmesisHoldout.cs:98 (bigShot); Projectiles\Ranged\SepticSkewerHarpoon.cs:145 (pull); NPCs\Perforator\PerforatorHive.cs:38 (IchorShoot) |
| 275 | `Custom\Perforator\PerfHiveShoot1.ogg` | 自定义/Boss/系统 | 0.69s | 30.1 | Custom\Perforator 专用音效；动作：射击/发射；主题：Perf Hive Shoot。 代码标识：GeyserShoot。 | NPCs\Perforator\PerforatorHive.cs:37 (GeyserShoot) |
| 276 | `Custom\Perforator\PerfHiveShoot2.ogg` | 自定义/Boss/系统 | 0.69s | 31.4 | Custom\Perforator 专用音效；动作：射击/发射；主题：Perf Hive Shoot。 代码标识：GeyserShoot。 | NPCs\Perforator\PerforatorHive.cs:37 (GeyserShoot) |
| 277 | `Custom\Perforator\PerfHiveShoot3.ogg` | 自定义/Boss/系统 | 0.69s | 32.5 | Custom\Perforator 专用音效；动作：射击/发射；主题：Perf Hive Shoot。 代码标识：pull。 | Projectiles\Ranged\SepticSkewerHarpoon.cs:152 (pull); NPCs\Perforator\PerforatorHive.cs:37 (GeyserShoot) |
| 278 | `Custom\Perforator\PerfHiveSpawn.ogg` | 自定义/Boss/系统 | 2.31s | 77.8 | Custom\Perforator 专用音效；动作：移动/生成/阶段转换；主题：Perf Hive Spawn。 代码标识：HiveSpawn。 | NPCs\Perforator\PerforatorCyst.cs:15 (HiveSpawn) |
| 279 | `Custom\Perforator\PerfHiveWormSpawn.ogg` | 自定义/Boss/系统 | 1.15s | 45.3 | Custom\Perforator 专用音效；动作：移动/生成/阶段转换；主题：Perf Hive Worm Spawn。 代码标识：WormSpawn。 | NPCs\Perforator\PerforatorHive.cs:39 (WormSpawn) |

### Custom\PlagueSounds

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 281 | `Custom\PlagueSounds\PBGAttackSwitch1.ogg` | 自定义/Boss/系统 | 1.31s | 28.4 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：通用/特殊；主题：PBG Attack Switch。 代码标识：AttackSwitchSound。 | NPCs\PlaguebringerGoliath\PlaguebringerGoliath.cs:58 (AttackSwitchSound) |
| 282 | `Custom\PlagueSounds\PBGAttackSwitch2.ogg` | 自定义/Boss/系统 | 1.66s | 39.8 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：通用/特殊；主题：PBG Attack Switch。 代码标识：AttackSwitchSound。 | NPCs\PlaguebringerGoliath\PlaguebringerGoliath.cs:58 (AttackSwitchSound) |
| 283 | `Custom\PlagueSounds\PBGAttackSwitchShort.ogg` | 自定义/Boss/系统 | 0.23s | 6.1 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：通用/特殊；主题：PBG Attack Switch Short。 代码标识：Primed。 | Projectiles\Ranged\AcidRocket.cs:103 (Primed); Projectiles\Ranged\TheHiveHoldout.cs:95 (fullCharge); Projectiles\Rogue\DestructionStar.cs:95 (sound) |
| 284 | `Custom\PlagueSounds\PBGBarrageLaunch.ogg` | 自定义/Boss/系统 | 1.38s | 22.5 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：射击/发射；主题：PBG Barrage Launch。 代码标识：fire。 | Projectiles\Ranged\TheHiveHoldout.cs:140 (fire); Projectiles\Ranged\TheHiveHoldout.cs:150 (fire); NPCs\PlaguebringerGoliath\PlaguebringerGoliath.cs:60 (BarrageLaunchSound) |
| 285 | `Custom\PlagueSounds\PBGDash.ogg` | 自定义/Boss/系统 | 0.96s | 22.0 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：移动/生成/阶段转换；主题：PBG Dash。 代码标识：DashSound。 | NPCs\PlaguebringerGoliath\PlaguebringerGoliath.cs:59 (DashSound) |
| 286 | `Custom\PlagueSounds\PBGNukeWarning.ogg` | 自定义/Boss/系统 | 3.57s | 50.8 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：爆炸/爆裂；主题：PBG Nuke Warning。 代码标识：NukeWarningSound。 | NPCs\PlaguebringerGoliath\PlaguebringerGoliath.cs:57 (NukeWarningSound) |
| 287 | `Custom\PlagueSounds\PlagueBoom1.ogg` | 自定义/Boss/系统 | 1.06s | 17.4 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：爆炸/爆裂；主题：Plague Boom。 代码标识：PlagueBoomSound。 | Sounds\CommonCalamitySounds.cs:26 (PlagueBoomSound); Projectiles\Ranged\AcidRocket.cs:140 (fire); Projectiles\Ranged\HiveMissile.cs:130 (fire); 另 1 处 |
| 288 | `Custom\PlagueSounds\PlagueBoom2.ogg` | 自定义/Boss/系统 | 1.21s | 23.3 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：爆炸/爆裂；主题：Plague Boom。 代码标识：PlagueBoomSound。 | Sounds\CommonCalamitySounds.cs:26 (PlagueBoomSound); Projectiles\Ranged\AcidRocket.cs:140 (fire); Projectiles\Ranged\HiveMissile.cs:130 (fire); 另 1 处 |
| 289 | `Custom\PlagueSounds\PlagueBoom3.ogg` | 自定义/Boss/系统 | 1.38s | 21.6 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：爆炸/爆裂；主题：Plague Boom。 代码标识：PlagueBoomSound。 | Sounds\CommonCalamitySounds.cs:26 (PlagueBoomSound); Projectiles\Ranged\AcidRocket.cs:140 (fire); Projectiles\Ranged\HiveMissile.cs:130 (fire); 另 1 处 |
| 290 | `Custom\PlagueSounds\PlagueBoom4.ogg` | 自定义/Boss/系统 | 1.47s | 22.0 | 瘟疫主题攻击、爆炸和机械虫群音效；动作：爆炸/爆裂；主题：Plague Boom。 代码标识：PlagueBoomSound。 | Sounds\CommonCalamitySounds.cs:26 (PlagueBoomSound); Projectiles\Magic\WarpSigilShot.cs:96 (w); Projectiles\Ranged\AcidRocket.cs:140 (fire); 另 2 处 |

### Custom\Polterghast

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 298 | `Custom\Polterghast\PolterghastP2Transition.ogg` | 自定义/Boss/系统 | 1.50s | 76.6 | Custom\Polterghast 专用音效；动作：激活/使用/UI；主题：Polterghast P2 Transition。 代码标识：P2Sound。 | NPCs\Polterghast\Polterghast.cs:57 (P2Sound) |
| 299 | `Custom\Polterghast\PolterghastP3Transition.ogg` | 自定义/Boss/系统 | 2.54s | 114.2 | Custom\Polterghast 专用音效；动作：激活/使用/UI；主题：Polterghast P3 Transition。 代码标识：P3Sound。 | NPCs\Polterghast\Polterghast.cs:58 (P3Sound) |
| 300 | `Custom\Polterghast\PolterghastPhantomSpawn.ogg` | 自定义/Boss/系统 | 1.38s | 73.8 | Custom\Polterghast 专用音效；动作：移动/生成/阶段转换；主题：Polterghast Phantom Spawn。 代码标识：PhantomSound。 | NPCs\Polterghast\Polterghast.cs:60 (PhantomSound) |
| 301 | `Custom\Polterghast\PolterghastSpawn.ogg` | 自定义/Boss/系统 | 3.69s | 174.3 | Custom\Polterghast 专用音效；动作：移动/生成/阶段转换；主题：Polterghast Spawn。 代码标识：SpawnSound。 | NPCs\Polterghast\Polterghast.cs:59 (SpawnSound) |

### Custom\ProfanedGuardians

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 303 | `Custom\ProfanedGuardians\GuardianDash.ogg` | 自定义/Boss/系统 | 0.69s | 22.1 | Custom\ProfanedGuardians 专用音效；动作：移动/生成/阶段转换；主题：Guardian Dash。 代码标识：sound。 | CalPlayer\CalamityPlayerMiscEffects.cs:1753 (sound); Projectiles\Magic\FireImplosion.cs:121; Projectiles\Ranged\ElysianArrowRain.cs:158 (onKill); 另 6 处 |
| 304 | `Custom\ProfanedGuardians\GuardianHeal.ogg` | 自定义/Boss/系统 | 0.77s | 33.3 | Custom\ProfanedGuardians 专用音效；动作：通用/特殊；主题：Guardian Heal。 代码标识：heal。 | Projectiles\Typeless\RelicOfConvergenceCrystal.cs:107 (heal); Projectiles\Typeless\RelicOfDeliveranceSpear.cs:348 (sound) |
| 305 | `Custom\ProfanedGuardians\GuardianRay.ogg` | 自定义/Boss/系统 | 2.43s | 52.3 | Custom\ProfanedGuardians 专用音效；动作：激光/光束；主题：Guardian Ray。 代码标识：bigShot。 | Projectiles\Magic\PurgeGuzzlerHoldout.cs:88 (bigShot); Projectiles\Melee\Yoyos\BurningRevelationYoyo.cs:138 (fireHeal); NPCs\ProfanedGuardians\ProfanedGuardianCommander.cs:41 (HolyRaySound); 另 1 处 |
| 306 | `Custom\ProfanedGuardians\GuardianRockShieldActivate.ogg` | 自定义/Boss/系统 | 1.21s | 29.2 | Custom\ProfanedGuardians 专用音效；动作：激活/使用/UI；主题：Guardian Rock Shield Activate。 代码标识：youGotHit。 | CalPlayer\CalamityPlayerHitHurt.cs:1305 (youGotHit); CalPlayer\CalamityPlayerMiscEffects.cs:1723 (y); NPCs\ProfanedGuardians\ProfanedGuardianDefender.cs:31 (RockShieldSpawnSound) |
| 307 | `Custom\ProfanedGuardians\GuardianShieldDeactivate.ogg` | 自定义/Boss/系统 | 0.97s | 23.4 | Custom\ProfanedGuardians 专用音效；动作：激活/使用/UI；主题：Guardian Shield Deactivate。 代码标识：shot。 | Projectiles\Magic\PurgeGuzzlerHoldout.cs:57 (shot); Projectiles\Melee\HolyColliderHoldout.cs:247 (swing); Projectiles\Typeless\ArtifactOfResilienceShards.cs:144 (boom); 另 2 处 |
| 308 | `Custom\ProfanedGuardians\GuardianSpawn.ogg` | 自定义/Boss/系统 | 1.93s | 31.6 | Custom\ProfanedGuardians 专用音效；动作：移动/生成/阶段转换；主题：Guardian Spawn。 代码标识：UseSound。 | Items\SummonItems\ProfanedShard.cs:14 (UseSound) |

### Custom\Providence

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 309 | `Custom\Providence\ProvidenceBurn.ogg` | 自定义/Boss/系统 | 3.28s | 63.4 | Custom\Providence 专用音效；动作：通用/特殊；主题：Providence Burn。 代码标识：fullPower。 | NPCs\CalamityGlobalNPC.cs:3143 (fullPower); Projectiles\Melee\HolyColliderHoldout.cs:166 (fullCharge); Projectiles\Ranged\PristineFire.cs:66 (ignite); 另 1 处 |
| 310 | `Custom\Providence\ProvidenceBurnLoop.ogg` | 自定义/Boss/系统 | 3.54s | 101.5 | Custom\Providence 专用音效；动作：循环/开始结束/预警；主题：Providence Burn Loop。 代码标识：burn。 | Projectiles\Melee\HolyColliderHoldout.cs:158 (burn); NPCs\Providence\Providence.cs:110 (BurnLoopSound) |
| 311 | `Custom\Providence\ProvidenceDeathAnimation.ogg` | 自定义/Boss/系统 | 9.91s | 194.6 | Custom\Providence 专用音效；动作：死亡/击杀；主题：Providence Death Animation。 代码标识：DeathAnimationSound。 | NPCs\Providence\Providence.cs:106 (DeathAnimationSound) |
| 312 | `Custom\Providence\ProvidenceHolyBlastImpact.ogg` | 自定义/Boss/系统 | 1.00s | 20.7 | Custom\Providence 专用音效；动作：受击/命中/冲击；主题：Providence Holy Blast Impact。 代码标识：ImpactSound。 | Projectiles\Boss\HolyBlast.cs:27 (ImpactSound); Projectiles\Ranged\BlissfulBombardierSplitProjectile.cs:77 (fire); Projectiles\Ranged\PristineSecondary.cs:55 (ignite); 另 3 处 |
| 313 | `Custom\Providence\ProvidenceHolyBlastShoot.ogg` | 自定义/Boss/系统 | 0.71s | 16.2 | Custom\Providence 专用音效；动作：射击/发射；主题：Providence Holy Blast Shoot。 代码标识：ShootSound。 | Projectiles\Boss\HolyBlast.cs:26 (ShootSound); Projectiles\Magic\TerraSigil.cs:47; Projectiles\Melee\HolyColliderHoldout.cs:240 (swing); 另 4 处 |
| 314 | `Custom\Providence\ProvidenceHolyRay.ogg` | 自定义/Boss/系统 | 3.04s | 65.6 | Custom\Providence 专用音效；动作：激光/光束；主题：Providence Holy Ray。 代码标识：bigShot2。 | Projectiles\Magic\PurgeGuzzlerHoldout.cs:90 (bigShot2); NPCs\Providence\Providence.cs:104 (HolyRaySound) |
| 315 | `Custom\Providence\ProvidenceSizzle.ogg` | 自定义/Boss/系统 | 2.30s | 47.4 | Custom\Providence 专用音效；动作：通用/特殊；主题：Providence Sizzle。 代码标识：NearBurnSound。 | NPCs\Providence\Providence.cs:108 (NearBurnSound) |
| 316 | `Custom\Providence\ProvidenceSpawn.ogg` | 自定义/Boss/系统 | 3.15s | 56.5 | Custom\Providence 专用音效；动作：移动/生成/阶段转换；主题：Providence Spawn。 代码标识：SpawnSound。 | NPCs\Providence\Providence.cs:103 (SpawnSound) |

### Custom\Ravager

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 333 | `Custom\Ravager\GasterBlasterCharge.ogg` | 自定义/Boss/系统 | 1.32s | 15.8 | Custom\Ravager 专用音效；动作：爆炸/爆裂；主题：Gaster Blaster Charge。 代码标识：SANSCharge。 | Projectiles\Boss\PermafrostBlaster.cs:15 (SANSCharge); Projectiles\Boss\RavagerBlaster.cs:13 (SANSCharge) |
| 334 | `Custom\Ravager\GasterBlasterFire.ogg` | 自定义/Boss/系统 | 2.11s | 26.7 | Custom\Ravager 专用音效；动作：射击/发射；主题：Gaster Blaster Fire。 代码标识：SANSFire。 | Projectiles\Boss\PermafrostBlaster.cs:16 (SANSFire); Projectiles\Boss\RavagerBlaster.cs:14 (SANSFire) |
| 335 | `Custom\Ravager\RavagerJump1.ogg` | 自定义/Boss/系统 | 0.70s | 24.1 | Custom\Ravager 专用音效；动作：通用/特殊；主题：Ravager Jump。 代码标识：JumpSound。 | NPCs\Ravager\RavagerBody.cs:40 (JumpSound) |
| 336 | `Custom\Ravager\RavagerJump2.ogg` | 自定义/Boss/系统 | 0.95s | 26.8 | Custom\Ravager 专用音效；动作：通用/特殊；主题：Ravager Jump。 首处代码上下文：TerraSigilMediumRock.cs。 | Projectiles\Magic\TerraSigilMediumRock.cs:80; NPCs\Ravager\RavagerBody.cs:40 (JumpSound) |
| 337 | `Custom\Ravager\RavagerMissileExplosion.ogg` | 自定义/Boss/系统 | 1.27s | 28.9 | Custom\Ravager 专用音效；动作：爆炸/爆裂；主题：Ravager Missile Explosion。 代码标识：ExplosionSound。 | Projectiles\Boss\RavagerNuke.cs:18 (ExplosionSound) |
| 338 | `Custom\Ravager\RavagerMissileLaunch.ogg` | 自定义/Boss/系统 | 1.50s | 27.4 | Custom\Ravager 专用音效；动作：射击/发射；主题：Ravager Missile Launch。 代码标识：MissileSound。 | NPCs\Ravager\RavagerBody.cs:47 (MissileSound); NPCs\Ravager\RavagerHead.cs:16 (MissileSound) |
| 339 | `Custom\Ravager\RavagerPillarSummon.ogg` | 自定义/Boss/系统 | 2.51s | 66.3 | Custom\Ravager 专用音效；动作：激活/使用/UI；主题：Ravager Pillar Summon。 代码标识：buff。 | Projectiles\Magic\DeathValleyDusterProjectile.cs:76 (buff); Projectiles\Magic\PrimordialAncientProjectile.cs:91 (buff); Projectiles\Magic\PrimordialEarthProjectile.cs:85 (buff); 另 3 处 |
| 340 | `Custom\Ravager\RavagerPunch1.ogg` | 自定义/Boss/系统 | 0.85s | 21.2 | Custom\Ravager 专用音效；动作：通用/特殊；主题：Ravager Punch。 代码标识：FistSound。 | NPCs\Ravager\RavagerBody.cs:42 (FistSound) |
| 341 | `Custom\Ravager\RavagerPunch2.ogg` | 自定义/Boss/系统 | 0.97s | 26.8 | Custom\Ravager 专用音效；动作：通用/特殊；主题：Ravager Punch。 代码标识：FistSound。 | NPCs\Ravager\RavagerBody.cs:42 (FistSound) |
| 342 | `Custom\Ravager\RavagerStomp1.ogg` | 自定义/Boss/系统 | 0.59s | 20.6 | Custom\Ravager 专用音效；动作：近战挥击/撞击；主题：Ravager Stomp。 首处代码上下文：RemsRevengeExplosion.cs。 | Projectiles\Melee\RemsRevengeExplosion.cs:38; NPCs\Ravager\RavagerBody.cs:41 (StompSound) |
| 343 | `Custom\Ravager\RavagerStomp2.ogg` | 自定义/Boss/系统 | 0.72s | 24.7 | Custom\Ravager 专用音效；动作：近战挥击/撞击；主题：Ravager Stomp。 首处代码上下文：RemsRevengeExplosion.cs。 | Projectiles\Melee\RemsRevengeExplosion.cs:38; NPCs\Ravager\RavagerBody.cs:41 (StompSound) |

### Custom\SCalSounds

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 354 | `Custom\SCalSounds\BrimstoneBigShoot.ogg` | 自定义/Boss/系统 | 1.46s | 23.2 | Supreme Calamitas / SCal 战斗与状态音效；动作：射击/发射；主题：Brimstone Big Shoot。 代码标识：BrimstoneBigShotSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:227 (BrimstoneBigShotSound) |
| 355 | `Custom\SCalSounds\BrimstoneFireblastImpact.ogg` | 自定义/Boss/系统 | 1.07s | 22.1 | Supreme Calamitas / SCal 战斗与状态音效；动作：受击/命中/冲击；主题：Brimstone Fireblast Impact。 代码标识：ImpactSound。 | Projectiles\Boss\BurningFireblast.cs:20 (ImpactSound); Projectiles\Boss\SCalBrimstoneFireblast.cs:22 (ImpactSound) |
| 356 | `Custom\SCalSounds\BrimstoneGigablastImpact.ogg` | 自定义/Boss/系统 | 1.65s | 29.7 | Supreme Calamitas / SCal 战斗与状态音效；动作：受击/命中/冲击；主题：Brimstone Gigablast Impact。 代码标识：ImpactSound。 | Projectiles\Boss\BurningGigablast.cs:20 (ImpactSound); Projectiles\Boss\SCalBrimstoneGigablast.cs:21 (ImpactSound) |
| 357 | `Custom\SCalSounds\BrimstoneHellblastSound.ogg` | 自定义/Boss/系统 | 0.78s | 15.8 | Supreme Calamitas / SCal 战斗与状态音效；动作：爆炸/爆裂；主题：Brimstone Hellblast Sound。 首处代码上下文：LiliesOfFinalityAriane.cs。 | Projectiles\Summon\LiliesOfFinalityAriane.cs:336; NPCs\SupremeCalamitas\SupremeCalamitas.cs:229 (HellblastSound) |
| 358 | `Custom\SCalSounds\BrimstoneMonsterDrone.ogg` | 自定义/Boss/系统 | 4.47s | 71.1 | Supreme Calamitas / SCal 战斗与状态音效；动作：激活/使用/UI；主题：Brimstone Monster Drone。 代码标识：DroneSound。 | Projectiles\Boss\BrimstoneMonster.cs:31 (DroneSound) |
| 359 | `Custom\SCalSounds\BrimstoneMonsterSpawn.ogg` | 自定义/Boss/系统 | 3.50s | 50.3 | Supreme Calamitas / SCal 战斗与状态音效；动作：激活/使用/UI；主题：Brimstone Monster Spawn。 代码标识：SpawnSound。 | Projectiles\Boss\BrimstoneMonster.cs:30 (SpawnSound) |
| 360 | `Custom\SCalSounds\BrimstoneShoot.ogg` | 自定义/Boss/系统 | 0.56s | 12.7 | Supreme Calamitas / SCal 战斗与状态音效；动作：射击/发射；主题：Brimstone Shoot。 代码标识：BrimstoneShotSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:223 (BrimstoneShotSound) |
| 361 | `Custom\SCalSounds\BrothersDeath1.ogg` | 自定义/Boss/系统 | 1.35s | 23.8 | Supreme Calamitas / SCal 战斗与状态音效；动作：死亡/击杀；主题：Brothers Death。 代码标识：BrotherDeath。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:225 (BrotherDeath) |
| 362 | `Custom\SCalSounds\BrothersDeath2.ogg` | 自定义/Boss/系统 | 1.65s | 26.9 | Supreme Calamitas / SCal 战斗与状态音效；动作：死亡/击杀；主题：Brothers Death。 代码标识：BrotherDeath。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:225 (BrotherDeath) |
| 363 | `Custom\SCalSounds\BrothersHurt1.ogg` | 自定义/Boss/系统 | 0.58s | 13.0 | Supreme Calamitas / SCal 战斗与状态音效；动作：受击/命中/冲击；主题：Brothers Hurt。 代码标识：BrotherHit。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:224 (BrotherHit) |
| 364 | `Custom\SCalSounds\BrothersHurt2.ogg` | 自定义/Boss/系统 | 0.58s | 14.0 | Supreme Calamitas / SCal 战斗与状态音效；动作：受击/命中/冲击；主题：Brothers Hurt。 代码标识：BrotherHit。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:224 (BrotherHit) |
| 365 | `Custom\SCalSounds\CatastropheResonanceSlash.ogg` | 自定义/Boss/系统 | 0.78s | 18.2 | Supreme Calamitas / SCal 战斗与状态音效；动作：激活/使用/UI；主题：Catastrophe Resonance Slash。 代码标识：CatastropheSwing。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:226 (CatastropheSwing) |
| 366 | `Custom\SCalSounds\GFBDrone.ogg` | 自定义/Boss/系统 | 1.84s | 30.5 | Supreme Calamitas / SCal 战斗与状态音效；动作：激活/使用/UI；主题：GFB Drone。 首处代码上下文：BrimstoneMonster.cs。 | Projectiles\Boss\BrimstoneMonster.cs:136; Projectiles\Boss\BrimstoneMonster.cs:169 |
| 367 | `Custom\SCalSounds\SCalDash.ogg` | 自定义/Boss/系统 | 1.30s | 24.5 | Supreme Calamitas / SCal 战斗与状态音效；动作：移动/生成/阶段转换；主题：S Cal Dash。 代码标识：DashSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:228 (DashSound) |
| 368 | `Custom\SCalSounds\SCalEndBH.ogg` | 自定义/Boss/系统 | 2.48s | 43.2 | Supreme Calamitas / SCal 战斗与状态音效；动作：循环/开始结束/预警；主题：S Cal End BH。 代码标识：BulletHellEndSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:232 (BulletHellEndSound) |
| 369 | `Custom\SCalSounds\SCalRumble.ogg` | 自定义/Boss/系统 | 6.12s | 39.7 | Supreme Calamitas / SCal 战斗与状态音效；动作：通用/特殊；主题：S Cal Rumble。 代码标识：BulletHellSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:231 (BulletHellSound) |
| 370 | `Custom\SCalSounds\SepulcherSpawn.ogg` | 自定义/Boss/系统 | 4.14s | 72.5 | Supreme Calamitas / SCal 战斗与状态音效；动作：移动/生成/阶段转换；主题：Sepulcher Spawn。 代码标识：SepulcherSummonSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:222 (SepulcherSummonSound) |
| 371 | `Custom\SCalSounds\SupremeCalamitasGiveUp.ogg` | 自定义/Boss/系统 | 5.55s | 42.0 | Supreme Calamitas / SCal 战斗与状态音效；动作：通用/特殊；主题：Supreme Calamitas Give Up。 代码标识：GiveUpSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:233 (GiveUpSound) |

### Custom\Yharon

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 425 | `Custom\Yharon\YharonFire.ogg` | 自定义/Boss/系统 | 2.63s | 53.3 | Custom\Yharon 专用音效；动作：射击/发射；主题：Yharon Fire。 代码标识：FireSound。 | NPCs\Yharon\Yharon.cs:61 (FireSound) |
| 426 | `Custom\Yharon\YharonFireball1.ogg` | 自定义/Boss/系统 | 0.47s | 9.7 | Custom\Yharon 专用音效；动作：射击/发射；主题：Yharon Fireball。 代码标识：FireballSound。 | Projectiles\Boss\YharonFireball2.cs:17 (FireballSound); Items\Weapons\Ranged\DragonsBreath.cs:21 (FireballSound) |
| 427 | `Custom\Yharon\YharonFireball2.ogg` | 自定义/Boss/系统 | 0.48s | 9.9 | Custom\Yharon 专用音效；动作：射击/发射；主题：Yharon Fireball。 代码标识：FireballSound。 | Projectiles\Boss\YharonFireball2.cs:17 (FireballSound); Items\Weapons\Ranged\DragonsBreath.cs:21 (FireballSound) |
| 428 | `Custom\Yharon\YharonFireball3.ogg` | 自定义/Boss/系统 | 0.48s | 9.0 | Custom\Yharon 专用音效；动作：射击/发射；主题：Yharon Fireball。 代码标识：FireballSound。 | Projectiles\Boss\YharonFireball2.cs:17 (FireballSound); Items\Weapons\Ranged\DragonsBreath.cs:21 (FireballSound) |
| 429 | `Custom\Yharon\YharonFireOrb.ogg` | 自定义/Boss/系统 | 5.12s | 63.7 | Custom\Yharon 专用音效；动作：射击/发射；主题：Yharon Fire Orb。 代码标识：OrbSound。 | NPCs\Yharon\Yharon.cs:62 (OrbSound) |
| 430 | `Custom\Yharon\YharonInfernado.ogg` | 自定义/Boss/系统 | 3.24s | 35.7 | Custom\Yharon 专用音效；动作：激活/使用/UI；主题：Yharon Infernado。 代码标识：FlareSound。 | Projectiles\Boss\BigFlare.cs:15 (FlareSound); Projectiles\Boss\BigFlare2.cs:15 (FlareSound); Projectiles\Boss\Flare.cs:16 (FlareSound); 另 3 处 |
| 431 | `Custom\Yharon\YharonRoar.ogg` | 自定义/Boss/系统 | 5.30s | 45.9 | Custom\Yharon 专用音效；动作：激活/使用/UI；主题：Yharon Roar。 代码标识：RoarSound。 | NPCs\Yharon\Yharon.cs:59 (RoarSound) |
| 432 | `Custom\Yharon\YharonRoarShort.ogg` | 自定义/Boss/系统 | 1.62s | 16.5 | Custom\Yharon 专用音效；动作：激活/使用/UI；主题：Yharon Roar Short。 代码标识：ShortRoarSound。 | NPCs\Yharon\Yharon.cs:60 (ShortRoarSound) |

### Item

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 433 | `Item\AmidiasTrident_Raise.ogg` | 物品/武器 | 1.57s | 65.1 | 物品/武器音效；动作：通用/特殊；主题：Amidias Trident Raise。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 434 | `Item\AmidiasTrident_Spin.ogg` | 物品/武器 | 0.94s | 45.8 | 物品/武器音效；动作：近战挥击/撞击；主题：Amidias Trident Spin。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 435 | `Item\AmidiasTrident_Stab1.ogg` | 物品/武器 | 0.49s | 23.1 | 物品/武器音效；动作：近战挥击/撞击；主题：Amidias Trident Stab。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 436 | `Item\AmidiasTrident_Stab2.ogg` | 物品/武器 | 0.48s | 24.8 | 物品/武器音效；动作：近战挥击/撞击；主题：Amidias Trident Stab。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 437 | `Item\AmphibiansGuitarSummon.ogg` | 物品/武器 | 0.90s | 28.5 | 物品/武器音效；动作：激活/使用/UI；主题：Amphibians Guitar Summon。 代码标识：spawnSound。 | Projectiles\Summon\AmphibiansGuitarMinion.cs:55 (spawnSound) |
| 438 | `Item\AnomalysNanogunMPFBExplosion.ogg` | 物品/武器 | 1.17s | 12.9 | 物品/武器音效；动作：爆炸/爆裂；主题：Anomalys Nanogun MPFB Explosion。 代码标识：MPFBExplosion。 | Projectiles\DraedonsArsenal\AnomalysNanogunMPFBBoom.cs:12 (MPFBExplosion); Projectiles\Ranged\ScorchedEarthRocket.cs:19 (RocketExplosion) |
| 439 | `Item\AnomalysNanogunMPFBShot.ogg` | 物品/武器 | 0.71s | 9.4 | 物品/武器音效；动作：射击/发射；主题：Anomalys Nanogun MPFB Shot。 代码标识：MPFBShotSFX。 | Items\Weapons\DraedonsArsenal\TheAnomalysNanogun.cs:21 (MPFBShotSFX); Items\Weapons\Magic\SHPC.cs:21 (FireSound) |
| 440 | `Item\AnomalysNanogunPlasmaCharge.ogg` | 物品/武器 | 0.84s | 9.8 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Anomalys Nanogun Plasma Charge。 代码标识：PlasmaChargeSFX。 | Items\Weapons\DraedonsArsenal\TheAnomalysNanogun.cs:20 (PlasmaChargeSFX) |
| 441 | `Item\AnomalysNanogunPlasmaShot.ogg` | 物品/武器 | 1.38s | 9.9 | 物品/武器音效；动作：射击/发射；主题：Anomalys Nanogun Plasma Shot。 代码标识：PlasmaShotSFX。 | Items\Weapons\DraedonsArsenal\TheAnomalysNanogun.cs:22 (PlasmaShotSFX) |
| 442 | `Item\ApoctosisShoot.ogg` | 物品/武器 | 0.91s | 23.1 | 物品/武器音效；动作：射击/发射；主题：Apoctosis Shoot。 代码标识：shoot。 | Projectiles\Magic\ApoctosisArrayHoldout.cs:259 (shoot) |
| 443 | `Item\ArcFlash.ogg` | 物品/武器 | 0.77s | 27.8 | 物品/武器音效；动作：通用/特殊；主题：Arc Flash。 代码标识：fire。 | Projectiles\Typeless\FlashBolt.cs:53 (fire) |
| 444 | `Item\ArcNovaDiffuserBigShot.ogg` | 物品/武器 | 1.50s | 17.1 | 物品/武器音效；动作：射击/发射；主题：Arc Nova Diffuser Big Shot。 代码标识：fire。 | Projectiles\Magic\OmicronHoldout.cs:121 (fire); Items\Weapons\Ranged\ArcNovaDiffuser.cs:21 (BigShot); Items\Weapons\Ranged\OntologicalDespoiler.cs:23 (BigShot) |
| 445 | `Item\ArcNovaDiffuserChargeImpact.ogg` | 物品/武器 | 1.13s | 15.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Arc Nova Diffuser Charge Impact。 代码标识：fire。 | Projectiles\Magic\GenesisBeam.cs:124 (fire); Projectiles\Magic\VolterionOrb.cs:19 (ExplosionSound); Projectiles\Magic\WingmanGrenade.cs:83 (fire); 另 1 处 |
| 446 | `Item\ArcNovaDiffuserChargeLoop.ogg` | 物品/武器 | 2.53s | 26.0 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arc Nova Diffuser Charge Loop。 代码标识：OrbSound。 | Projectiles\Ranged\TauCannonHoldout.cs:71 (OrbSound); Items\Weapons\Ranged\ArcNovaDiffuser.cs:18 (ChargeLoop); Items\Weapons\Ranged\OntologicalDespoiler.cs:20 (ChargeLoop) |
| 447 | `Item\ArcNovaDiffuserChargeLV1.ogg` | 物品/武器 | 0.95s | 11.4 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arc Nova Diffuser Charge LV。 代码标识：ChargeLV1Sound。 | Projectiles\Ranged\TauCannonHoldout.cs:69 (ChargeLV1Sound); Items\Weapons\Ranged\ArcNovaDiffuser.cs:15 (ChargeLV1); Items\Weapons\Ranged\OntologicalDespoiler.cs:17 (ChargeLV1); 另 1 处 |
| 448 | `Item\ArcNovaDiffuserChargeLV2.ogg` | 物品/武器 | 0.95s | 12.4 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arc Nova Diffuser Charge LV。 代码标识：ChargeLV2Sound。 | Projectiles\Ranged\TauCannonHoldout.cs:70 (ChargeLV2Sound); Items\Weapons\Ranged\ArcNovaDiffuser.cs:16 (ChargeLV2); Items\Weapons\Ranged\OntologicalDespoiler.cs:18 (ChargeLV2); 另 1 处 |
| 449 | `Item\ArcNovaDiffuserChargeStart.ogg` | 物品/武器 | 2.44s | 25.3 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arc Nova Diffuser Charge Start。 代码标识：ChargeStart。 | Items\Weapons\Ranged\ArcNovaDiffuser.cs:17 (ChargeStart); Items\Weapons\Ranged\OntologicalDespoiler.cs:19 (ChargeStart) |
| 450 | `Item\ArcNovaDiffuserCompleteCharge.ogg` | 物品/武器 | 7.50s | 68.8 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arc Nova Diffuser Complete Charge。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 451 | `Item\ArcNovaDiffuserSmallShot.ogg` | 物品/武器 | 0.77s | 10.6 | 物品/武器音效；动作：射击/发射；主题：Arc Nova Diffuser Small Shot。 代码标识：SmallShot。 | Items\Weapons\Ranged\ArcNovaDiffuser.cs:20 (SmallShot); Items\Weapons\Ranged\OntologicalDespoiler.cs:22 (SmallShot) |
| 452 | `Item\ArsenalOffCooldown.ogg` | 物品/武器 | 1.32s | 30.3 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Arsenal Off Cooldown。 代码标识：EndSound。 | Cooldowns\ArsenalPower.cs:24 (EndSound) |
| 453 | `Item\ArtAttackCast.ogg` | 物品/武器 | 1.06s | 23.0 | 物品/武器音效；动作：通用/特殊；主题：Art Attack Cast。 代码标识：UseSound。 | Items\Weapons\Magic\ArtAttack.cs:14 (UseSound) |
| 454 | `Item\AscendantActivate.ogg` | 物品/武器 | 3.00s | 34.6 | 物品/武器音效；动作：激活/使用/UI；主题：Ascendant Activate。 首处代码上下文：CalamityPlayer.cs。 | CalPlayer\CalamityPlayer.cs:3489; Projectiles\Typeless\AscendantAura.cs:105 (s) |
| 455 | `Item\AscendantOff.ogg` | 物品/武器 | 2.57s | 48.6 | 物品/武器音效；动作：激活/使用/UI；主题：Ascendant Off。 代码标识：EndSound。 | Cooldowns\AscendEffect.cs:17 (EndSound) |
| 456 | `Item\AstralSlash1.ogg` | 物品/武器 | 1.55s | 37.5 | 物品/武器音效；动作：近战挥击/撞击；主题：Astral Slash。 代码标识：sound。 | Projectiles\Typeless\UrsaSlash.cs:89 (sound) |
| 457 | `Item\AstralSlash2.ogg` | 物品/武器 | 1.55s | 41.8 | 物品/武器音效；动作：近战挥击/撞击；主题：Astral Slash。 代码标识：sound。 | Projectiles\Typeless\UrsaSlash.cs:89 (sound) |
| 458 | `Item\AstralSlash3.ogg` | 物品/武器 | 1.55s | 42.1 | 物品/武器音效；动作：近战挥击/撞击；主题：Astral Slash。 代码标识：sound。 | Projectiles\Typeless\UrsaSlash.cs:89 (sound) |
| 459 | `Item\AugerBigSlash.ogg` | 物品/武器 | 1.35s | 35.6 | 物品/武器音效；动作：近战挥击/撞击；主题：Auger Big Slash。 代码标识：swing。 | Projectiles\DraedonsArsenal\AugerHoldout.cs:206 (swing) |
| 460 | `Item\AugerHit.ogg` | 物品/武器 | 0.70s | 14.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Auger Hit。 代码标识：hit。 | Projectiles\DraedonsArsenal\AugerSlash.cs:97 (hit) |
| 461 | `Item\AugerPull.ogg` | 物品/武器 | 0.54s | 15.7 | 物品/武器音效；动作：通用/特殊；主题：Auger Pull。 代码标识：pull。 | Projectiles\DraedonsArsenal\AugerHoldout.cs:108 (pull) |
| 462 | `Item\AugerSlash1.ogg` | 物品/武器 | 0.67s | 20.2 | 物品/武器音效；动作：近战挥击/撞击；主题：Auger Slash。 代码标识：swing。 | Projectiles\DraedonsArsenal\AugerHoldout.cs:206 (swing) |
| 463 | `Item\AugerSlash2.ogg` | 物品/武器 | 0.88s | 23.7 | 物品/武器音效；动作：近战挥击/撞击；主题：Auger Slash。 代码标识：swing。 | Projectiles\DraedonsArsenal\AugerHoldout.cs:206 (swing) |
| 464 | `Item\AugerWindup.ogg` | 物品/武器 | 0.22s | 9.8 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Auger Windup。 代码标识：windup。 | Projectiles\DraedonsArsenal\AugerHoldout.cs:165 (windup) |
| 465 | `Item\AuricBulletHit.ogg` | 物品/武器 | 1.39s | 15.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Auric Bullet Hit。 代码标识：fire。 | Projectiles\Magic\VoidVortexProj.cs:157 (fire); Projectiles\Melee\SkytideDragoonHoldout.cs:234 (fire2); Projectiles\Melee\SkytideDragoonHoldout.cs:295 (fire); 另 2 处 |
| 466 | `Item\BatholithBangleSound.ogg` | 物品/武器 | 5.19s | 135.4 | 物品/武器音效；动作：通用/特殊；主题：Batholith Bangle Sound。 代码标识：sound。 | Projectiles\Typeless\BatholithBangleProjectile.cs:52 (sound) |
| 467 | `Item\BlackGlassBandSound.ogg` | 物品/武器 | 0.80s | 23.0 | 物品/武器音效；动作：通用/特殊；主题：Black Glass Band Sound。 代码标识：sound。 | Projectiles\Typeless\BlackGlassBandProjectile.cs:36 (sound) |
| 468 | `Item\BlazingCoreParry.ogg` | 物品/武器 | 1.44s | 27.6 | 物品/武器音效；动作：通用/特殊；主题：Blazing Core Parry。 代码标识：fire2。 | Projectiles\Ranged\NukeOfBliss.cs:157 (fire2); Projectiles\Rogue\SpearofDestinyStealth.cs:15 (Hitsound); Items\Accessories\BlazingCore.cs:21 (ParrySuccessSound); 另 1 处 |
| 469 | `Item\BlazingCoreParryActivate.ogg` | 物品/武器 | 0.66s | 16.1 | 物品/武器音效；动作：激活/使用/UI；主题：Blazing Core Parry Activate。 代码标识：ParryActivateSound。 | Items\Accessories\BlazingCore.cs:20 (ParryActivateSound) |
| 470 | `Item\BloodOrangeConsume.ogg` | 物品/武器 | 1.84s | 38.9 | 物品/武器音效；动作：激活/使用/UI；主题：Blood Orange Consume。 代码标识：UseSound。 | Items\PermanentBoosters\SanguineTangerine.cs:21 (UseSound) |
| 471 | `Item\BreakAndReform.ogg` | 物品/武器 | 0.69s | 25.9 | 物品/武器音效；动作：采掘/破碎/物块碰撞；主题：Break And Reform。 代码标识：sound。 | Projectiles\Rogue\WhitewaterProj.cs:194 (sound) |
| 472 | `Item\CalamityBell.ogg` | 物品/武器 | 1.58s | 28.9 | 物品/武器音效；动作：通用/特殊；主题：Calamity Bell。 代码标识：UseSoundFunny。 | Projectiles\Melee\FallenPaladinsHammerProj.cs:19 (UseSoundFunny); Projectiles\Melee\GalaxySmasherHammer.cs:25 (UseSoundFunny); Projectiles\Melee\PwnagehammerProj.cs:15 (UseSoundFunny); 另 2 处 |
| 473 | `Item\CeaselessVoidSpawn.ogg` | 物品/武器 | 3.79s | 64.2 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Ceaseless Void Spawn。 代码标识：CVSound。 | Items\SummonItems\MarkofProvidence.cs:19 (CVSound) |
| 474 | `Item\ChainPull.ogg` | 物品/武器 | 0.93s | 33.2 | 物品/武器音效；动作：通用/特殊；主题：Chain Pull。 代码标识：pull3。 | Projectiles\Ranged\SepticSkewerHarpoon.cs:130 (pull3) |
| 475 | `Item\ClamImpact.ogg` | 物品/武器 | 0.49s | 10.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Clam Impact。 代码标识：SlamSound。 | NPCs\SunkenSea\GiantClam.cs:31 (SlamSound) |
| 476 | `Item\CometShardUse.ogg` | 物品/武器 | 2.79s | 20.3 | 物品/武器音效；动作：激活/使用/UI；主题：Comet Shard Use。 代码标识：UseSound。 | Items\PermanentBoosters\CometShard.cs:16 (UseSound) |
| 477 | `Item\CrackshotColtShot.ogg` | 物品/武器 | 1.59s | 34.8 | 物品/武器音效；动作：射击/发射；主题：Crackshot Colt Shot。 代码标识：ShootSound。 | Items\Weapons\Ranged\MidasPrime.cs:23 (ShootSound) |
| 478 | `Item\CrystylCharge.ogg` | 物品/武器 | 1.52s | 17.6 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Crystyl Charge。 代码标识：ChargeSound。 | Items\Tools\CrystylCrusher.cs:21 (ChargeSound) |
| 479 | `Item\CursedDaggerThrow.ogg` | 物品/武器 | 0.55s | 8.2 | 物品/武器音效；动作：射击/发射；主题：Cursed Dagger Throw。 代码标识：fire。 | Projectiles\Melee\CometQuasherHoldout.cs:202 (fire); Projectiles\Melee\MajesticGuardHoldout.cs:188 (fire); Projectiles\Melee\StellarStrikerHoldout.cs:199 (fire); 另 2 处 |
| 480 | `Item\DampExplosion.ogg` | 物品/武器 | 0.33s | 6.6 | 物品/武器音效；动作：爆炸/爆裂；主题：Damp Explosion。 代码标识：fire3。 | Projectiles\Melee\BasherHoldout.cs:197 (fire3) |
| 481 | `Item\DashSound.ogg` | 物品/武器 | 0.62s | 18.8 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Dash Sound。 代码标识：dash。 | CalPlayer\CalamityPlayerMiscEffects.cs:567 (dash) |
| 482 | `Item\DeadSunExplosion.ogg` | 物品/武器 | 1.45s | 16.6 | 物品/武器音效；动作：爆炸/爆裂；主题：Dead Sun Explosion。 代码标识：fire。 | Projectiles\Magic\OmicronWingman.cs:166 (fire); Projectiles\Magic\WingmanHoldout.cs:195 (fire); Items\Weapons\Ranged\DeadSunsWind.cs:17 (Explosion) |
| 483 | `Item\DeadSunRicochet.ogg` | 物品/武器 | 0.64s | 9.5 | 物品/武器音效；动作：通用/特殊；主题：Dead Sun Ricochet。 代码标识：explo2。 | Projectiles\Rogue\DestructionStar.cs:129 (explo2); NPCs\SupremeCalamitas\SepulcherHead.cs:358; Items\Weapons\Ranged\DeadSunsWind.cs:16 (Ricochet) |
| 484 | `Item\DeadSunShot.ogg` | 物品/武器 | 0.91s | 11.9 | 物品/武器音效；动作：射击/发射；主题：Dead Sun Shot。 代码标识：ShootSound。 | Items\Weapons\Ranged\DeadSunsWind.cs:15 (ShootSound) |
| 485 | `Item\DemonSwordFinalStrike.ogg` | 物品/武器 | 3.04s | 71.6 | 物品/武器音效；动作：激活/使用/UI；主题：Demon Sword Final Strike。 代码标识：dieSound。 | Projectiles\Melee\DevilsDevastationHoldout.cs:260 (dieSound) |
| 486 | `Item\DemonSwordImpact1.ogg` | 物品/武器 | 1.82s | 39.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Demon Sword Impact。 代码标识：stuck。 | Projectiles\Melee\BladecrestOathswordThrownBlade.cs:104 (stuck); Projectiles\Melee\BladecrestOathswordThrownBlade.cs:382 (stuck); Projectiles\Melee\DevilsDevastationThrownBlade.cs:320 (stuck2); 另 4 处 |
| 487 | `Item\DemonSwordImpact2.ogg` | 物品/武器 | 1.95s | 43.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Demon Sword Impact。 代码标识：stuck。 | Projectiles\Melee\BladecrestOathswordThrownBlade.cs:104 (stuck); Projectiles\Melee\BladecrestOathswordThrownBlade.cs:382 (stuck); Projectiles\Melee\DevilsDevastationThrownBlade.cs:320 (stuck2); 另 4 处 |
| 488 | `Item\DemonSwordInsaneImpact.ogg` | 物品/武器 | 2.41s | 38.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Demon Sword Insane Impact。 代码标识：swing。 | Projectiles\Melee\DevilsDevastationHoldout.cs:458 (swing); Projectiles\Melee\ExaltedOathbladeHoldout.cs:310 (swing) |
| 489 | `Item\DemonSwordKillMode.ogg` | 物品/武器 | 2.26s | 51.7 | 物品/武器音效；动作：激活/使用/UI；主题：Demon Sword Kill Mode。 代码标识：buff。 | Items\Weapons\Melee\DevilsDevastation.cs:53 (buff); Items\Weapons\Melee\ExaltedOathblade.cs:51 (buff); Items\Weapons\Melee\ForbiddenOathblade.cs:50 (buff) |
| 490 | `Item\DemonSwordKillModeOffCooldown.ogg` | 物品/武器 | 0.34s | 13.1 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Demon Sword Kill Mode Off Cooldown。 代码标识：EndSound。 | Cooldowns\KillMode.cs:25 (EndSound) |
| 491 | `Item\DemonSwordStrongImpact.ogg` | 物品/武器 | 2.17s | 31.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Demon Sword Strong Impact。 代码标识：swing。 | Projectiles\Melee\ForbiddenOathbladeHoldout.cs:357 (swing); Projectiles\Melee\OldLordClaymoreHoldout.cs:221 (swing) |
| 492 | `Item\DemonSwordSwing1.ogg` | 物品/武器 | 0.70s | 18.7 | 物品/武器音效；动作：激活/使用/UI；主题：Demon Sword Swing。 代码标识：swing。 | Projectiles\Melee\BladecrestOathswordThrownBlade.cs:280 (swing); Projectiles\Melee\DevilsDevastationHoldout.cs:319 (swing); Projectiles\Melee\DevilsDevastationThrownBlade.cs:257 (swing); 另 5 处 |
| 493 | `Item\DemonSwordSwing2.ogg` | 物品/武器 | 0.70s | 20.1 | 物品/武器音效；动作：激活/使用/UI；主题：Demon Sword Swing。 代码标识：swing。 | Projectiles\Melee\BladecrestOathswordThrownBlade.cs:280 (swing); Projectiles\Melee\DevilsDevastationHoldout.cs:319 (swing); Projectiles\Melee\DevilsDevastationThrownBlade.cs:257 (swing); 另 6 处 |
| 494 | `Item\DoomsdayDeviceImpact.ogg` | 物品/武器 | 0.75s | 21.8 | 物品/武器音效；动作：受击/命中/冲击；主题：Doomsday Device Impact。 代码标识：sound。 | Projectiles\Rogue\DoomsdayDeviceProjectile.cs:303 (sound); Projectiles\Rogue\DoomsdayDeviceProjectile.cs:372 (soundExtra); Projectiles\Rogue\DoomsdayDeviceProjectile.cs:380 (sound) |
| 495 | `Item\DragonfruitConsume.ogg` | 物品/武器 | 2.85s | 72.6 | 物品/武器音效；动作：激活/使用/UI；主题：Dragonfruit Consume。 代码标识：UseSound。 | Items\PermanentBoosters\SacredStrawberry.cs:23 (UseSound) |
| 496 | `Item\DragonsBreathStrongStart.ogg` | 物品/武器 | 3.10s | 185.1 | 物品/武器音效；动作：激活/使用/UI；主题：Dragons Breath Strong Start。 代码标识：WeldingStart。 | Items\Weapons\Ranged\DragonsBreath.cs:22 (WeldingStart) |
| 497 | `Item\DudFire.ogg` | 物品/武器 | 0.17s | 7.1 | 物品/武器音效；动作：射击/发射；主题：Dud Fire。 首处代码上下文：AuricLandMineTile.cs。 | Tiles\FurnitureAuric\AuricLandMineTile.cs:37; Projectiles\DraedonsArsenal\AqueousHunterDroneSummon.cs:212 (click); Projectiles\DraedonsArsenal\ShortCircuitHook.cs:151; 另 19 处 |
| 498 | `Item\EarthMeteor.ogg` | 物品/武器 | 0.97s | 29.5 | 物品/武器音效；动作：通用/特殊；主题：Earth Meteor。 代码标识：explo2。 | Projectiles\Magic\PrimordialAncientProjectile.cs:247 (explo2); Projectiles\Melee\EarthMeteor.cs:201 (fire2); Projectiles\Ranged\OntologicalDespoilerGrenade.cs:169 (fire); 另 1 处 |
| 499 | `Item\EffervescenceBurst.ogg` | 物品/武器 | 0.90s | 13.1 | 物品/武器音效；动作：爆炸/爆裂；主题：Effervescence Burst。 代码标识：BurstSound。 | Items\Weapons\Magic\Effervescence.cs:15 (BurstSound) |
| 500 | `Item\EffervescenceFire.ogg` | 物品/武器 | 0.99s | 16.4 | 物品/武器音效；动作：射击/发射；主题：Effervescence Fire。 代码标识：FireSound。 | Items\Weapons\Magic\Effervescence.cs:14 (FireSound) |
| 501 | `Item\EffervescencePop.ogg` | 物品/武器 | 0.40s | 9.1 | 物品/武器音效；动作：水体/气泡；主题：Effervescence Pop。 代码标识：PopSound。 | Items\Weapons\Magic\Effervescence.cs:16 (PopSound) |
| 502 | `Item\ElderberryConsume.ogg` | 物品/武器 | 2.34s | 51.1 | 物品/武器音效；动作：激活/使用/UI；主题：Elderberry Consume。 代码标识：UseSound。 | Items\PermanentBoosters\TaintedCloudberry.cs:22 (UseSound) |
| 503 | `Item\ElectricBurst.ogg` | 物品/武器 | 1.32s | 40.1 | 物品/武器音效；动作：爆炸/爆裂；主题：Electric Burst。 代码标识：Explode。 | Projectiles\DraedonsArsenal\ShortCircuitHook.cs:19 (Explode) |
| 504 | `Item\ElectricHit.ogg` | 物品/武器 | 0.27s | 5.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Electric Hit。 代码标识：hitSound。 | CalPlayer\CalamityPlayerOnHit.cs:524 (hitSound) |
| 505 | `Item\ELRFire.ogg` | 物品/武器 | 1.09s | 15.0 | 物品/武器音效；动作：射击/发射；主题：ELR Fire。 代码标识：ELRFireSound。 | Sounds\CommonCalamitySounds.cs:10 (ELRFireSound) |
| 506 | `Item\EtherealCoreUse.ogg` | 物品/武器 | 4.48s | 34.7 | 物品/武器音效；动作：激活/使用/UI；主题：Ethereal Core Use。 代码标识：UseSound。 | Items\PermanentBoosters\EtherealCore.cs:16 (UseSound) |
| 507 | `Item\Evernote.ogg` | 物品/武器 | 0.44s | 13.0 | 物品/武器音效；动作：通用/特殊；主题：Evernote。 代码标识：fire。 | Projectiles\Summon\AmphibiansGuitarMinion.cs:91 (fire) |
| 508 | `Item\ExobladeBeamSlash.ogg` | 物品/武器 | 1.76s | 37.5 | 物品/武器音效；动作：激光/光束；主题：Exoblade Beam Slash。 代码标识：charge。 | Projectiles\Boss\SupremeCatastropheSlash.cs:127 (charge); Projectiles\Boss\SupremeCatastropheSlash.cs:159 (charge); Projectiles\Melee\EarthHoldout.cs:285 (fire2); 另 5 处 |
| 509 | `Item\ExobladeBigHit.ogg` | 物品/武器 | 1.21s | 20.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Exoblade Big Hit。 代码标识：BigHitSound。 | Items\Weapons\Melee\Exoblade.cs:23 (BigHitSound) |
| 510 | `Item\ExobladeBigSwing.ogg` | 物品/武器 | 1.13s | 19.7 | 物品/武器音效；动作：近战挥击/撞击；主题：Exoblade Big Swing。 代码标识：BigSwingSound。 | Items\Weapons\Melee\Exoblade.cs:22 (BigSwingSound) |
| 511 | `Item\ExobladeDash.ogg` | 物品/武器 | 1.57s | 27.6 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Exoblade Dash。 代码标识：DashSound。 | Items\Weapons\Melee\Exoblade.cs:25 (DashSound) |
| 512 | `Item\ExobladeDashImpact.ogg` | 物品/武器 | 1.20s | 22.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Exoblade Dash Impact。 代码标识：HitSound。 | Projectiles\Melee\PrismaticRay.cs:32 (HitSound); Projectiles\Rogue\DestructionStar.cs:185 (explo3); Items\Weapons\Melee\Exoblade.cs:26 (DashHitSound) |
| 513 | `Item\ExobladeSwing.ogg` | 物品/武器 | 1.13s | 18.3 | 物品/武器音效；动作：近战挥击/撞击；主题：Exoblade Swing。 代码标识：SwingSound。 | Items\Weapons\Melee\Exoblade.cs:21 (SwingSound) |
| 514 | `Item\FallenPaladinsHammerBigImpact.ogg` | 物品/武器 | 3.01s | 42.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Fallen Paladins Hammer Big Impact。 代码标识：SlamHamSound。 | Projectiles\Melee\FallenPaladinsHammerEcho.cs:14 (SlamHamSound) |
| 515 | `Item\FallenPaladinsHammerClone.ogg` | 物品/武器 | 1.03s | 16.5 | 物品/武器音效；动作：激活/使用/UI；主题：Fallen Paladins Hammer Clone。 代码标识：RedHamSound。 | Projectiles\Melee\FallenPaladinsHammerProj.cs:20 (RedHamSound) |
| 516 | `Item\FestiveJingle.ogg` | 物品/武器 | 0.60s | 12.2 | 物品/武器音效；动作：通用/特殊；主题：Festive Jingle。 代码标识：JingleSound。 | Projectiles\Typeless\FestiveWingsOrnament.cs:15 (JingleSound) |
| 517 | `Item\FinalDawnSlash.ogg` | 物品/武器 | 0.82s | 45.1 | 物品/武器音效；动作：近战挥击/撞击；主题：Final Dawn Slash。 代码标识：hit2。 | Projectiles\Melee\EarthHoldout.cs:281 (hit2); Projectiles\Melee\GrandDadHoldout.cs:227 (fire2); Projectiles\Ranged\SepticSkewerHarpoon.cs:146 (pull2); 另 3 处 |
| 518 | `Item\FireImplosion.ogg` | 物品/武器 | 1.22s | 26.0 | 物品/武器音效；动作：射击/发射；主题：Fire Implosion。 首处代码上下文：FireImplosion.cs。 | Projectiles\Magic\FireImplosion.cs:44; Projectiles\Ranged\FirestormCannonHoldout.cs:26 (WarningSound) |
| 519 | `Item\FlakKrakenShoot.ogg` | 物品/武器 | 1.29s | 23.5 | 物品/武器音效；动作：射击/发射；主题：Flak Kraken Shoot。 代码标识：bigShotGun。 | Projectiles\Ranged\FetidEmesisHoldout.cs:99 (bigShotGun); Projectiles\Ranged\FlakKrakenHoldout.cs:149; Projectiles\Ranged\FlakToxicannonHoldout.cs:148; 另 4 处 |
| 520 | `Item\FlareSound.ogg` | 物品/武器 | 2.21s | 31.5 | 物品/武器音效；动作：通用/特殊；主题：Flare Sound。 代码标识：FlareSound。 | Sounds\CommonCalamitySounds.cs:17 (FlareSound) |
| 521 | `Item\FrigidflashCharge.ogg` | 物品/武器 | 1.38s | 46.9 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Frigidflash Charge。 代码标识：ChargeSound。 | Items\Weapons\Magic\FrigidflashBolt.cs:17 (ChargeSound) |
| 522 | `Item\FrigidflashDeath.ogg` | 物品/武器 | 0.81s | 29.5 | 物品/武器音效；动作：死亡/击杀；主题：Frigidflash Death。 代码标识：ProjDeathSound。 | Items\Weapons\Magic\FrigidflashBolt.cs:16 (ProjDeathSound) |
| 523 | `Item\FrigidflashUse.ogg` | 物品/武器 | 0.92s | 35.7 | 物品/武器音效；动作：激活/使用/UI；主题：Frigidflash Use。 代码标识：UseSound。 | Items\Weapons\Magic\FrigidflashBolt.cs:15 (UseSound) |
| 524 | `Item\FuguBlow.ogg` | 物品/武器 | 1.79s | 26.9 | 物品/武器音效；动作：通用/特殊；主题：Fugu Blow。 代码标识：BlowSound。 | Items\Weapons\Melee\BallOFugu.cs:12 (BlowSound) |
| 525 | `Item\GalaxySmasherClone.ogg` | 物品/武器 | 2.22s | 38.3 | 物品/武器音效；动作：激活/使用/UI；主题：Galaxy Smasher Clone。 代码标识：RedHamSound。 | Projectiles\Melee\GalaxySmasherHammer.cs:24 (RedHamSound); Projectiles\Melee\TriactisHammerProj.cs:19 (WindUpSound) |
| 526 | `Item\GalaxySmasherSmash.ogg` | 物品/武器 | 3.06s | 54.5 | 物品/武器音效；动作：通用/特殊；主题：Galaxy Smasher Smash。 代码标识：SlamHamSound。 | Projectiles\Melee\GalaxySmasherEcho.cs:19 (SlamHamSound); Projectiles\Melee\TriactisHammerProj.cs:20 (SmashSound) |
| 527 | `Item\GeliticBladeSwing1.ogg` | 物品/武器 | 0.97s | 18.9 | 物品/武器音效；动作：近战挥击/撞击；主题：Gelitic Blade Swing。 代码标识：UseSound。 | Items\Weapons\Melee\GeliticBlade.cs:15 (UseSound) |
| 528 | `Item\GeliticBladeSwing2.ogg` | 物品/武器 | 0.91s | 21.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Gelitic Blade Swing。 代码标识：UseSound。 | Items\Weapons\Melee\GeliticBlade.cs:15 (UseSound) |
| 537 | `Item\GunShotBig.ogg` | 物品/武器 | 1.93s | 52.7 | 物品/武器音效；动作：射击/发射；主题：Gun Shot Big。 代码标识：UseSound。 | Items\Weapons\Ranged\BarracudaGun.cs:32 (UseSound) |
| 538 | `Item\GunShotHeavy.ogg` | 物品/武器 | 1.26s | 21.7 | 物品/武器音效；动作：射击/发射；主题：Gun Shot Heavy。 代码标识：sound。 | Projectiles\DraedonsArsenal\VulcanHoldout.cs:182 (sound); Items\Weapons\Ranged\SepticSkewer.cs:65 (fire) |
| 539 | `Item\GunShotMid.ogg` | 物品/武器 | 0.42s | 16.5 | 物品/武器音效；动作：射击/发射；主题：Gun Shot Mid。 代码标识：fire。 | Projectiles\Ranged\KingsbaneHoldout.cs:135 (fire); Items\Weapons\Ranged\SepticSkewer.cs:58 (fire) |
| 540 | `Item\GunShotSmall.ogg` | 物品/武器 | 0.50s | 11.2 | 物品/武器音效；动作：射击/发射；主题：Gun Shot Small。 代码标识：fire。 | Projectiles\Ranged\KingsbaneHoldout.cs:102 (fire); Projectiles\Typeless\Luxor.cs:169 (blam); Items\Weapons\DraedonsArsenal\ShortCircuit.cs:69 (fire); 另 2 处 |
| 541 | `Item\GunShotSmallAlt.ogg` | 物品/武器 | 0.41s | 11.2 | 物品/武器音效；动作：射击/发射；主题：Gun Shot Small Alt。 代码标识：fire。 | Projectiles\Ranged\FetidEmesisHoldout.cs:54 (fire) |
| 542 | `Item\HadalUrnClose.ogg` | 物品/武器 | 0.52s | 8.2 | 物品/武器音效；动作：激活/使用/UI；主题：Hadal Urn Close。 代码标识：hitSound。 | CalPlayer\CalamityPlayerOnHit.cs:574 (hitSound); Projectiles\Magic\HadalUrnHoldout.cs:15 (UrnSound) |
| 543 | `Item\HadalUrnOpen.ogg` | 物品/武器 | 0.69s | 9.8 | 物品/武器音效；动作：激活/使用/UI；主题：Hadal Urn Open。 代码标识：ShootSound。 | Items\Weapons\Magic\HadalUrn.cs:16 (ShootSound) |
| 544 | `Item\HalleysInfernoHit.ogg` | 物品/武器 | 1.35s | 14.1 | 物品/武器音效；动作：受击/命中/冲击；主题：Halleys Inferno Hit。 代码标识：Hit。 | Items\Weapons\Ranged\HalleysInferno.cs:21 (Hit) |
| 545 | `Item\HalleysInfernoShoot.ogg` | 物品/武器 | 1.08s | 14.9 | 物品/武器音效；动作：射击/发射；主题：Halleys Inferno Shoot。 首处代码上下文：FlareBoltProjectile.cs。 | Projectiles\Magic\FlareBoltProjectile.cs:58; Projectiles\Magic\FrigidflashBoltProjectile.cs:81; Items\Weapons\Ranged\HalleysInferno.cs:20 (ShootSound) |
| 546 | `Item\HarpEnd.ogg` | 物品/武器 | 3.43s | 35.2 | 物品/武器音效；动作：循环/开始结束/预警；主题：Harp End。 代码标识：song。 | NPCs\Leviathan\Anahita.cs:764 (song); Items\Weapons\Magic\AnahitasArpeggio.cs:20 (EndSound) |
| 547 | `Item\HarpLV1.ogg` | 物品/武器 | 3.44s | 31.5 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78 |
| 548 | `Item\HarpLV2.ogg` | 物品/武器 | 3.43s | 34.7 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78 |
| 549 | `Item\HarpLV3.ogg` | 物品/武器 | 3.44s | 34.4 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78 |
| 550 | `Item\HarpLV4.ogg` | 物品/武器 | 3.43s | 34.4 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78 |
| 551 | `Item\HarpLV5.ogg` | 物品/武器 | 3.43s | 32.9 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78 |
| 552 | `Item\HarpLV6.ogg` | 物品/武器 | 3.44s | 33.2 | 物品/武器音效；动作：通用/特殊；主题：Harp LV。 首处代码上下文：AnahitasArpeggioNote.cs。 | Projectiles\Magic\AnahitasArpeggioNote.cs:78; Items\Weapons\Magic\AnahitasArpeggio.cs:19 (CapSound) |
| 553 | `Item\HarpNoteHit.ogg` | 物品/武器 | 3.43s | 32.3 | 物品/武器音效；动作：受击/命中/冲击；主题：Harp Note Hit。 代码标识：HitSound。 | Items\Weapons\Magic\AnahitasArpeggio.cs:21 (HitSound) |
| 554 | `Item\Heartbeat.ogg` | 物品/武器 | 0.36s | 7.2 | 物品/武器音效；动作：通用/特殊；主题：Heartbeat。 代码标识：heartbeat。 | CalPlayer\CalamityPlayerLifeRegen.cs:714 (heartbeat); Projectiles\Summon\OldDukeHeadCorpse.cs:53 (heartbeat); Projectiles\Summon\OldDukeHeadCorpse.cs:111 (heartbeat); 另 1 处 |
| 555 | `Item\HeavenlyGaleFire.ogg` | 物品/武器 | 2.23s | 40.0 | 物品/武器音效；动作：射击/发射；主题：Heavenly Gale Fire。 代码标识：FireSound。 | Items\Weapons\Ranged\HeavenlyGale.cs:34 (FireSound) |
| 556 | `Item\HeavyDig.ogg` | 物品/武器 | 0.43s | 11.7 | 物品/武器音效；动作：通用/特殊；主题：Heavy Dig。 代码标识：digSound。 | Projectiles\Typeless\RelicOfDeliveranceSpear.cs:305 (digSound) |
| 557 | `Item\HeavySwing.ogg` | 物品/武器 | 0.71s | 10.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Heavy Swing。 代码标识：swing2。 | Projectiles\Melee\DevilsDevastationHoldout.cs:321 (swing2); Projectiles\Melee\ExaltedOathbladeHoldout.cs:193 (swing2); Projectiles\Melee\ForbiddenOathbladeHoldout.cs:248 (swing2); 另 3 处 |
| 558 | `Item\HeliumFlashCharge.ogg` | 物品/武器 | 2.13s | 37.9 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Helium Flash Charge。 代码标识：Charge。 | Items\Weapons\Magic\HeliumFlash.cs:18 (Charge) |
| 559 | `Item\HeliumFlashCoreImpact.ogg` | 物品/武器 | 0.73s | 13.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Helium Flash Core Impact。 代码标识：fire。 | Projectiles\Magic\VolatileStarcore.cs:123 (fire); Projectiles\Melee\HolyColliderHolyFire.cs:67 (sound5); Projectiles\Ranged\SepticSkewerHarpoon.cs:377 (sound5); 另 2 处 |
| 560 | `Item\HeliumFlashDudFire.ogg` | 物品/武器 | 0.63s | 22.1 | 物品/武器音效；动作：射击/发射；主题：Helium Flash Dud Fire。 代码标识：fire。 | Projectiles\Magic\HeliumFlashHoldout.cs:108 (fire) |
| 561 | `Item\HeliumFlashExplodeNoMetal.ogg` | 物品/武器 | 2.07s | 35.0 | 物品/武器音效；动作：爆炸/爆裂；主题：Helium Flash Explode No Metal。 首处代码上下文：HeliumFlashBlast.cs。 | Projectiles\Magic\HeliumFlashBlast.cs:82 |
| 562 | `Item\HeliumFlashFire.ogg` | 物品/武器 | 1.39s | 27.2 | 物品/武器音效；动作：射击/发射；主题：Helium Flash Fire。 代码标识：ChargeFire。 | Items\Weapons\Magic\HeliumFlash.cs:21 (ChargeFire) |
| 563 | `Item\HeliumFlashFullChargeLoop.ogg` | 物品/武器 | 6.85s | 161.7 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Helium Flash Full Charge Loop。 代码标识：ChargeLoop。 | Items\Weapons\Magic\HeliumFlash.cs:19 (ChargeLoop) |
| 564 | `Item\HeliumFlashReady.ogg` | 物品/武器 | 1.12s | 38.1 | 物品/武器音效；动作：通用/特殊；主题：Helium Flash Ready。 代码标识：fire。 | Projectiles\Magic\HeliumFlashHoldout.cs:157 (fire); Projectiles\Melee\Yoyos\BurningRevelationYoyo.cs:209 (explode2) |
| 565 | `Item\HeliumFlashSteamRelease.ogg` | 物品/武器 | 1.42s | 27.5 | 物品/武器音效；动作：通用/特殊；主题：Helium Flash Steam Release。 代码标识：fire。 | Projectiles\Magic\HeliumFlashHoldout.cs:177 (fire); Projectiles\Ranged\FirestormCannonHoldout.cs:27 (OverheatSound); Projectiles\Ranged\TauCannonHoldout.cs:242 (steam) |
| 566 | `Item\HellbornImpact.ogg` | 物品/武器 | 1.01s | 25.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellborn Impact。 代码标识：FullChargeSound。 | Projectiles\Melee\DevilsSunriseProj.cs:19 (FullChargeSound); Projectiles\Ranged\HellbornProj.cs:131 (bigShot); Projectiles\Rogue\MeteorFistMeteorite.cs:56 |
| 567 | `Item\HellbornReload.ogg` | 物品/武器 | 0.57s | 19.5 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Hellborn Reload。 代码标识：fire。 | Projectiles\Ranged\HellbornHoldout.cs:198 (fire) |
| 568 | `Item\HellbornShoot.ogg` | 物品/武器 | 0.86s | 25.0 | 物品/武器音效；动作：射击/发射；主题：Hellborn Shoot。 代码标识：bigShot。 | Projectiles\Ranged\HellbornHoldout.cs:142 (bigShot) |
| 569 | `Item\HellkiteBigHit1.ogg` | 物品/武器 | 2.10s | 48.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellkite Big Hit。 代码标识：swing2。 | Projectiles\Melee\DevilsDevastationHoldout.cs:460 (swing2); Items\Weapons\Melee\Hellkite.cs:20 (HitSoundBig) |
| 570 | `Item\HellkiteBigHit2.ogg` | 物品/武器 | 2.10s | 46.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellkite Big Hit。 代码标识：HitSoundBig。 | Items\Weapons\Melee\Hellkite.cs:20 (HitSoundBig) |
| 571 | `Item\HellkiteCharge.ogg` | 物品/武器 | 6.41s | 191.1 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Hellkite Charge。 代码标识：ChargeSound。 | Items\Weapons\Melee\Hellkite.cs:21 (ChargeSound) |
| 572 | `Item\HellkiteFullCharge.ogg` | 物品/武器 | 2.41s | 59.1 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Hellkite Full Charge。 首处代码上下文：StarfleetHoldout.cs。 | Projectiles\Ranged\StarfleetHoldout.cs:175; Projectiles\Ranged\StarmadaHoldout.cs:230; Projectiles\Rogue\MoltenAmputatorProj.cs:291 (slice); 另 1 处 |
| 573 | `Item\HellkiteHeavySwing.ogg` | 物品/武器 | 1.93s | 51.6 | 物品/武器音效；动作：近战挥击/撞击；主题：Hellkite Heavy Swing。 代码标识：SwingSoundBig。 | Items\Weapons\Melee\Hellkite.cs:18 (SwingSoundBig) |
| 574 | `Item\HellkiteSmallHit1.ogg` | 物品/武器 | 0.64s | 20.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellkite Small Hit。 代码标识：stuck。 | Projectiles\Melee\DevilsDevastationThrownBlade.cs:318 (stuck); Projectiles\Melee\NeptunesBountyProjectile.cs:273 (HitSound2); Items\Weapons\Melee\Hellkite.cs:19 (HitSoundSmall) |
| 575 | `Item\HellkiteSmallHit2.ogg` | 物品/武器 | 0.56s | 17.0 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellkite Small Hit。 代码标识：stuck。 | Projectiles\Melee\DevilsDevastationThrownBlade.cs:318 (stuck); Projectiles\Melee\NeptunesBountyProjectile.cs:273 (HitSound2); Items\Weapons\Melee\Hellkite.cs:19 (HitSoundSmall) |
| 576 | `Item\HellkiteSmallHit3.ogg` | 物品/武器 | 0.61s | 16.8 | 物品/武器音效；动作：受击/命中/冲击；主题：Hellkite Small Hit。 代码标识：stuck。 | Projectiles\Melee\DevilsDevastationThrownBlade.cs:318 (stuck); Projectiles\Melee\NeptunesBountyProjectile.cs:273 (HitSound2); Items\Weapons\Melee\Hellkite.cs:19 (HitSoundSmall) |
| 577 | `Item\HellkiteSwing1.ogg` | 物品/武器 | 1.04s | 23.8 | 物品/武器音效；动作：近战挥击/撞击；主题：Hellkite Swing。 代码标识：swing2。 | Projectiles\Melee\EarthHoldout.cs:181 (swing2); Items\Weapons\Melee\Hellkite.cs:17 (SwingSound) |
| 578 | `Item\HellkiteSwing2.ogg` | 物品/武器 | 1.06s | 23.9 | 物品/武器音效；动作：近战挥击/撞击；主题：Hellkite Swing。 代码标识：swing2。 | Projectiles\Melee\EarthHoldout.cs:181 (swing2); Items\Weapons\Melee\Hellkite.cs:17 (SwingSound) |
| 579 | `Item\HolyBurst.ogg` | 物品/武器 | 0.87s | 21.9 | 物品/武器音效；动作：爆炸/爆裂；主题：Holy Burst。 代码标识：soundBurst。 | Projectiles\Rogue\ExorcismProj.cs:365 (soundBurst); Projectiles\Rogue\ExorcismProj.cs:400 (soundBurst) |
| 580 | `Item\HolyColliderBigHit.ogg` | 物品/武器 | 1.89s | 53.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Holy Collider Big Hit。 代码标识：hitSound。 | Projectiles\Melee\HolyColliderHoldout.cs:421 (hitSound); Projectiles\Typeless\RelicOfDeliveranceSpear.cs:484 (sound) |
| 581 | `Item\HolyColliderProjectileHit.ogg` | 物品/武器 | 1.89s | 48.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Holy Collider Projectile Hit。 代码标识：sound。 | Projectiles\Melee\HolyColliderHolyFire.cs:162 (sound); Projectiles\Rogue\ExorcismProj.cs:368 (soundExplosion); Projectiles\Typeless\PauldronDash.cs:117 (sound) |
| 582 | `Item\HolyColliderSmallHit.ogg` | 物品/武器 | 1.48s | 38.4 | 物品/武器音效；动作：受击/命中/冲击；主题：Holy Collider Small Hit。 代码标识：hitSound。 | Projectiles\Melee\HolyColliderHoldout.cs:406 (hitSound); Projectiles\Typeless\RelicOfDeliveranceSpear.cs:238 (sound) |
| 583 | `Item\HolyFireBulletExplosion.ogg` | 物品/武器 | 0.37s | 14.1 | 物品/武器音效；动作：射击/发射；主题：Holy Fire Bullet Explosion。 代码标识：Explosion。 | Items\Ammo\HolyFireBullet.cs:13 (Explosion) |
| 584 | `Item\HolyLoop.ogg` | 物品/武器 | 1.87s | 51.1 | 物品/武器音效；动作：循环/开始结束/预警；主题：Holy Loop。 代码标识：choir。 | Projectiles\Rogue\ExorcismProj.cs:299 (choir) |
| 585 | `Item\Hydra.ogg` | 物品/武器 | 1.70s | 20.3 | 物品/武器音效；动作：通用/特殊；主题：Hydra。 代码标识：FireSound。 | Items\Weapons\Ranged\Hydra.cs:103 (FireSound) |
| 586 | `Item\HyperiusOverflow.ogg` | 物品/武器 | 0.65s | 13.7 | 物品/武器音效；动作：通用/特殊；主题：Hyperius Overflow。 代码标识：hit。 | Items\Ammo\HyperiusBullet.cs:13 (hit) |
| 587 | `Item\IceBarrageCast.ogg` | 物品/武器 | 5.48s | 64.6 | 物品/武器音效；动作：通用/特殊；主题：Ice Barrage Cast。 代码标识：CastSound。 | Items\Weapons\Magic\IceBarrage.cs:18 (CastSound) |
| 588 | `Item\IchorSpearThrow.ogg` | 物品/武器 | 0.78s | 9.8 | 物品/武器音效；动作：射击/发射；主题：Ichor Spear Throw。 代码标识：ThrowSound。 | Items\Weapons\Rogue\IchorSpear.cs:13 (ThrowSound) |
| 589 | `Item\ImmolatorCharge.ogg` | 物品/武器 | 0.42s | 24.3 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Immolator Charge。 代码标识：sound。 | Projectiles\DraedonsArsenal\HolofibreImmolatorHoldout.cs:53 (sound) |
| 590 | `Item\ImmolatorChargeLoop.ogg` | 物品/武器 | 0.69s | 13.6 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Immolator Charge Loop。 代码标识：sound。 | Projectiles\DraedonsArsenal\HolofibreImmolatorHoldout.cs:150 (sound) |
| 591 | `Item\ImmolatorFire.ogg` | 物品/武器 | 0.54s | 15.3 | 物品/武器音效；动作：射击/发射；主题：Immolator Fire。 代码标识：sound。 | Projectiles\DraedonsArsenal\HolofibreImmolatorHoldout.cs:79 (sound); Projectiles\DraedonsArsenal\HolofibreImmolatorHoldout.cs:102 (sound) |
| 592 | `Item\ImmolatorPreExplode.ogg` | 物品/武器 | 0.79s | 21.2 | 物品/武器音效；动作：爆炸/爆裂；主题：Immolator Pre Explode。 代码标识：sound2。 | Projectiles\DraedonsArsenal\ImmolationArrow.cs:162 (sound2); Projectiles\DraedonsArsenal\ImmolationArrow.cs:197 (sound) |
| 593 | `Item\ImpalerLaunch.ogg` | 物品/武器 | 0.71s | 45.3 | 物品/武器音效；动作：射击/发射；主题：Impaler Launch。 代码标识：fire。 | Projectiles\Magic\ApoctosisArrayHoldout.cs:228 (fire); Projectiles\Magic\IonBlasterHoldout.cs:229 (fire); Projectiles\Melee\InsidiousHarpoon.cs:77 (fire) |
| 594 | `Item\Inkling1.ogg` | 物品/武器 | 1.10s | 17.1 | 物品/武器音效；动作：通用/特殊；主题：Inkling。 代码标识：GFB。 | Items\Weapons\Summon\CalamarisLament.cs:16 (GFB) |
| 595 | `Item\Inkling2.ogg` | 物品/武器 | 0.26s | 7.5 | 物品/武器音效；动作：通用/特殊；主题：Inkling。 代码标识：GFB。 | Items\Weapons\Summon\CalamarisLament.cs:16 (GFB) |
| 596 | `Item\Inkling3.ogg` | 物品/武器 | 1.28s | 18.4 | 物品/武器音效；动作：通用/特殊；主题：Inkling。 代码标识：GFB。 | Items\Weapons\Summon\CalamarisLament.cs:16 (GFB) |
| 597 | `Item\Inkling4.ogg` | 物品/武器 | 1.46s | 21.6 | 物品/武器音效；动作：通用/特殊；主题：Inkling。 代码标识：GFB。 | Items\Weapons\Summon\CalamarisLament.cs:16 (GFB) |
| 598 | `Item\Inkling5.ogg` | 物品/武器 | 0.70s | 12.5 | 物品/武器音效；动作：通用/特殊；主题：Inkling。 代码标识：GFB。 | Items\Weapons\Summon\CalamarisLament.cs:16 (GFB) |
| 599 | `Item\IonChargeLoop.ogg` | 物品/武器 | 2.20s | 37.3 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Ion Charge Loop。 代码标识：charge。 | Projectiles\Magic\ApoctosisArrayHoldout.cs:113 (charge); Projectiles\Magic\IonBlasterHoldout.cs:113 (charge) |
| 600 | `Item\KendraBark.ogg` | 物品/武器 | 0.27s | 5.7 | 物品/武器音效；动作：循环/开始结束/预警；主题：Kendra Bark。 代码标识：BarkSound。 | Projectiles\Pets\KendraPet.cs:15 (BarkSound) |
| 601 | `Item\LanceofDestiny.ogg` | 物品/武器 | 1.28s | 13.4 | 物品/武器音效；动作：通用/特殊；主题：Lanceof Destiny。 代码标识：ThrowSound2。 | Items\Weapons\Rogue\SpearofDestiny.cs:14 (ThrowSound2) |
| 602 | `Item\LanceofDestinyStrong.ogg` | 物品/武器 | 1.74s | 17.0 | 物品/武器音效；动作：激活/使用/UI；主题：Lanceof Destiny Strong。 代码标识：fire。 | Projectiles\Magic\GenesisHoldout.cs:96 (fire); Projectiles\Magic\IonBlasterHoldout.cs:232 (fire2); Projectiles\Melee\DevilsDevastationHoldout.cs:124 (dieSound); 另 2 处 |
| 603 | `Item\LargeWeaponFire.ogg` | 物品/武器 | 2.32s | 33.7 | 物品/武器音效；动作：射击/发射；主题：Large Weapon Fire。 代码标识：LargeWeaponFireSound。 | Sounds\CommonCalamitySounds.cs:19 (LargeWeaponFireSound) |
| 604 | `Item\LaserBurn.ogg` | 物品/武器 | 0.42s | 10.5 | 物品/武器音效；动作：激光/光束；主题：Laser Burn。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:3277 |
| 605 | `Item\LaserCannon.ogg` | 物品/武器 | 1.25s | 19.7 | 物品/武器音效；动作：激光/光束；主题：Laser Cannon。 代码标识：LaserCannonSound。 | Sounds\CommonCalamitySounds.cs:20 (LaserCannonSound) |
| 606 | `Item\LauncherHeavyShot.ogg` | 物品/武器 | 2.11s | 45.6 | 物品/武器音效；动作：射击/发射；主题：Launcher Heavy Shot。 代码标识：sound2。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:241 (sound2); Projectiles\Ranged\BlissfulBombardierHoldout.cs:181 (fire); Projectiles\Typeless\RelicOfDeliveranceSpear.cs:242 (sound2) |
| 607 | `Item\LeviathanHornSound.ogg` | 物品/武器 | 2.92s | 30.2 | 物品/武器音效；动作：通用/特殊；主题：Leviathan Horn Sound。 代码标识：HornSound。 | Items\SummonItems\NaiadsWarhorn.cs:18 (HornSound) |
| 608 | `Item\LightCatch.ogg` | 物品/武器 | 0.20s | 9.0 | 物品/武器音效；动作：通用/特殊；主题：Light Catch。 代码标识：grab。 | Projectiles\DraedonsArsenal\PulseGrenadeOrb.cs:89 (grab) |
| 609 | `Item\LightMetal.ogg` | 物品/武器 | 0.57s | 19.4 | 物品/武器音效；动作：通用/特殊；主题：Light Metal。 代码标识：pin。 | Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:180 (pin); Projectiles\Ranged\StarmadaHoldout.cs:220 (oops2) |
| 610 | `Item\LightningAura.ogg` | 物品/武器 | 3.34s | 172.0 | 物品/武器音效；动作：通用/特殊；主题：Lightning Aura。 首处代码上下文：AquasScepterCloud.cs。 | Projectiles\Summon\AquasScepterCloud.cs:81 |
| 611 | `Item\LightThrow.ogg` | 物品/武器 | 0.30s | 10.9 | 物品/武器音效；动作：射击/发射；主题：Light Throw。 代码标识：toss。 | Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:157 (toss) |
| 612 | `Item\LiliesOfFinalitySummonSpawn.ogg` | 物品/武器 | 1.99s | 21.0 | 物品/武器音效；动作：激活/使用/UI；主题：Lilies Of Finality Summon Spawn。 代码标识：UseSound。 | Items\Weapons\Summon\LiliesOfFinality.cs:56 (UseSound) |
| 613 | `Item\LouderPhantomPhoenix1.ogg` | 物品/武器 | 2.23s | 28.7 | 物品/武器音效；动作：通用/特殊；主题：Louder Phantom Phoenix。 代码标识：LouderPhantomPhoenix。 | Sounds\CommonCalamitySounds.cs:22 (LouderPhantomPhoenix) |
| 614 | `Item\LouderPhantomPhoenix2.ogg` | 物品/武器 | 2.14s | 32.0 | 物品/武器音效；动作：通用/特殊；主题：Louder Phantom Phoenix。 代码标识：LouderPhantomPhoenix。 | Sounds\CommonCalamitySounds.cs:22 (LouderPhantomPhoenix); Tiles\Furniture\BlueCandle.cs:16 (ActivationSound); Tiles\Furniture\PinkCandle.cs:16 (ActivationSound); 另 2 处 |
| 615 | `Item\LouderPhantomPhoenix3.ogg` | 物品/武器 | 2.03s | 25.0 | 物品/武器音效；动作：通用/特殊；主题：Louder Phantom Phoenix。 代码标识：LouderPhantomPhoenix。 | Sounds\CommonCalamitySounds.cs:22 (LouderPhantomPhoenix) |
| 616 | `Item\LowHum.ogg` | 物品/武器 | 3.67s | 39.9 | 物品/武器音效；动作：通用/特殊；主题：Low Hum。 代码标识：charge。 | Projectiles\DraedonsArsenal\ShortCircuitHook.cs:231 (charge); Projectiles\Ranged\TheHiveHoldout.cs:235 (charge) |
| 617 | `Item\LucreciaBoltFire.ogg` | 物品/武器 | 1.37s | 16.0 | 物品/武器音效；动作：射击/发射；主题：Lucrecia Bolt Fire。 代码标识：projectile。 | Projectiles\Melee\LucreciaHoldout.cs:252 (projectile) |
| 618 | `Item\LunicImpact.ogg` | 物品/武器 | 0.94s | 12.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Lunic Impact。 代码标识：ImpactSound。 | Items\Weapons\Typeless\LunicEye.cs:16 (ImpactSound) |
| 619 | `Item\LunicShot1.ogg` | 物品/武器 | 1.34s | 27.4 | 物品/武器音效；动作：射击/发射；主题：Lunic Shot。 代码标识：UseSound。 | Items\Weapons\Typeless\LunicEye.cs:15 (UseSound) |
| 620 | `Item\LunicShot2.ogg` | 物品/武器 | 1.22s | 19.9 | 物品/武器音效；动作：射击/发射；主题：Lunic Shot。 代码标识：UseSound。 | Items\Weapons\Typeless\LunicEye.cs:15 (UseSound) |
| 621 | `Item\M1GarandPing.ogg` | 物品/武器 | 0.72s | 13.9 | 物品/武器音效；动作：通用/特殊；主题：M1 Garand Ping。 代码标识：PingSound。 | Projectiles\Ranged\M1GarandHoldout.cs:47 (PingSound) |
| 622 | `Item\M1GarandReload.ogg` | 物品/武器 | 1.77s | 20.1 | 物品/武器音效；动作：蓄力/充能/冷却；主题：M1 Garand Reload。 代码标识：ReloadSound。 | Projectiles\Ranged\M1GarandHoldout.cs:48 (ReloadSound) |
| 623 | `Item\MagicRockImpact.ogg` | 物品/武器 | 1.50s | 21.0 | 物品/武器音效；动作：受击/命中/冲击；主题：Magic Rock Impact。 代码标识：explo。 | Projectiles\Magic\PrimordialAncientProjectile.cs:154 (explo); Projectiles\Magic\PrimordialEarthProjectile.cs:180 (explo); Projectiles\Melee\TrueBiomeBlade_EarthenTides.cs:35 (GroundImpact); 另 2 处 |
| 624 | `Item\MagicRockSound.ogg` | 物品/武器 | 0.76s | 12.0 | 物品/武器音效；动作：通用/特殊；主题：Magic Rock Sound。 首处代码上下文：UnstableCastersGauntletHoldout.cs。 | Projectiles\Magic\UnstableCastersGauntletHoldout.cs:174; Projectiles\Melee\CometQuasherHoldout.cs:204 (fire2); Projectiles\Melee\MendedBiomeBlade_TruePureClarity.cs:32 (FullChargeSound); 另 7 处 |
| 625 | `Item\MagnaCannonChargeFull.ogg` | 物品/武器 | 0.69s | 10.3 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Magna Cannon Charge Full。 代码标识：ChargeFull。 | Items\Weapons\Ranged\MagnaCannon.cs:13 (ChargeFull) |
| 626 | `Item\MagnaCannonChargeLoop.ogg` | 物品/武器 | 2.55s | 27.8 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Magna Cannon Charge Loop。 首处代码上下文：OntologicalDespoilerHoldout.cs。 | Projectiles\Ranged\OntologicalDespoilerHoldout.cs:343; Items\Weapons\Ranged\MagnaCannon.cs:15 (ChargeLoop) |
| 627 | `Item\MagnaCannonChargeStart.ogg` | 物品/武器 | 2.14s | 23.0 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Magna Cannon Charge Start。 代码标识：ChargeStart。 | Items\Weapons\Ranged\MagnaCannon.cs:17 (ChargeStart) |
| 628 | `Item\MagnaCannonShot.ogg` | 物品/武器 | 1.46s | 17.0 | 物品/武器音效；动作：射击/发射；主题：Magna Cannon Shot。 代码标识：fire。 | Projectiles\Magic\AetherfluxCannonHoldout.cs:82 (fire); Projectiles\Magic\GenesisHoldout.cs:112 (fire); Projectiles\Magic\OmicronWingman.cs:177 (fire); 另 3 处 |
| 629 | `Item\MagnusImpact.ogg` | 物品/武器 | 0.63s | 14.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Magnus Impact。 代码标识：StealthBoom。 | Items\Weapons\Rogue\Equanimity.cs:13 (StealthBoom); Items\Weapons\Typeless\EyeofMagnus.cs:13 (ImpactSound) |
| 630 | `Item\MantisSwipe1.ogg` | 物品/武器 | 0.48s | 16.6 | 物品/武器音效；动作：通用/特殊；主题：Mantis Swipe。 代码标识：HitSound。 | Projectiles\Melee\DevilsSunriseCyclone.cs:16 (HitSound); Projectiles\Melee\DevilsSunriseProj.cs:18 (HitSound); Projectiles\Melee\MantisClawHoldout.cs:129 (SlashStyle) |
| 631 | `Item\MantisSwipe2.ogg` | 物品/武器 | 0.48s | 17.2 | 物品/武器音效；动作：通用/特殊；主题：Mantis Swipe。 代码标识：HitSound。 | Projectiles\Melee\DevilsSunriseCyclone.cs:16 (HitSound); Projectiles\Melee\DevilsSunriseProj.cs:18 (HitSound); Projectiles\Melee\MantisClawHoldout.cs:129 (SlashStyle) |
| 632 | `Item\MarniteLiftHumm.ogg` | 物品/武器 | 12.03s | 197.3 | 物品/武器音效；动作：通用/特殊；主题：Marnite Lift Humm。 代码标识：LiftHummSound。 | Items\Armor\MarniteArchitect\MarniteArchitectSet.cs:26 (LiftHummSound) |
| 633 | `Item\MarniteLiftSummon.ogg` | 物品/武器 | 1.49s | 29.7 | 物品/武器音效；动作：激活/使用/UI；主题：Marnite Lift Summon。 代码标识：LiftSpawnSound。 | Items\Armor\MarniteArchitect\MarniteArchitectSet.cs:24 (LiftSpawnSound) |
| 634 | `Item\MarniteLiftUnsummon.ogg` | 物品/武器 | 1.49s | 26.9 | 物品/武器音效；动作：激活/使用/UI；主题：Marnite Lift Unsummon。 代码标识：LiftGoAwaySound。 | Items\Armor\MarniteArchitect\MarniteArchitectSet.cs:25 (LiftGoAwaySound) |
| 635 | `Item\MarniteObliteratorUse.ogg` | 物品/武器 | 1.91s | 39.5 | 物品/武器音效；动作：激活/使用/UI；主题：Marnite Obliterator Use。 代码标识：UseSound。 | Items\Tools\MarniteObliterator.cs:13 (UseSound) |
| 636 | `Item\MechGaussRifle.ogg` | 物品/武器 | 1.66s | 22.9 | 物品/武器音效；动作：通用/特殊；主题：Mech Gauss Rifle。 首处代码上下文：ApotheosisWorm.cs。 | Projectiles\Magic\ApotheosisWorm.cs:204; Projectiles\Summon\GiantIbanRobotOfDoom.cs:332; Projectiles\Summon\GiantIbanRobotOfDoom.cs:355; 另 2 处 |
| 637 | `Item\MeldBurn.ogg` | 物品/武器 | 0.65s | 21.9 | 物品/武器音效；动作：通用/特殊；主题：Meld Burn。 代码标识：sound。 | Projectiles\Melee\EntropicFlechette.cs:116 (sound); Items\Weapons\Magic\Apathanull.cs:32 (UseSound) |
| 638 | `Item\MeldExplosion.ogg` | 物品/武器 | 2.22s | 33.1 | 物品/武器音效；动作：爆炸/爆裂；主题：Meld Explosion。 代码标识：fire2。 | Projectiles\Ranged\OntologicalDespoilerBeam.cs:125 (fire2); Projectiles\Rogue\AntumbraShardProjectile.cs:192 (sound); Projectiles\Rogue\DestructionStar.cs:171 (explo) |
| 639 | `Item\MeldShoot.ogg` | 物品/武器 | 0.97s | 39.6 | 物品/武器音效；动作：射击/发射；主题：Meld Shoot。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeLance.cs:115 (sound); Projectiles\Magic\CosmicTentacle.cs:175 (fire); Projectiles\Magic\PhasedGodRay.cs:144 (hitSound); 另 1 处 |
| 640 | `Item\MeldSlice.ogg` | 物品/武器 | 0.66s | 15.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Meld Slice。 代码标识：sound。 | Projectiles\Rogue\AntumbraShardProjectile.cs:296 (sound); Projectiles\Rogue\DoomsdayDeviceProjectile.cs:176 (w); Projectiles\Rogue\DoomsdayDeviceProjectile.cs:190 (w) |
| 641 | `Item\MetalEcho.ogg` | 物品/武器 | 2.89s | 22.8 | 物品/武器音效；动作：通用/特殊；主题：Metal Echo。 代码标识：sound8。 | Projectiles\Ranged\SepticSkewerHarpoon.cs:381 (sound8) |
| 642 | `Item\MineralMortarExplode.ogg` | 物品/武器 | 2.32s | 50.5 | 物品/武器音效；动作：爆炸/爆裂；主题：Mineral Mortar Explode。 代码标识：explo。 | Projectiles\Magic\PrimordialAncientProjectile.cs:245 (explo); Projectiles\Ranged\LeviatitanMeteor.cs:64 (explo); Projectiles\Ranged\MineralMortarProjectile.cs:161; 另 2 处 |
| 643 | `Item\MiracleFruitConsume.ogg` | 物品/武器 | 1.95s | 38.8 | 物品/武器音效；动作：激活/使用/UI；主题：Miracle Fruit Consume。 代码标识：UseSound。 | Items\PermanentBoosters\MiracleFruit.cs:20 (UseSound) |
| 644 | `Item\MissileNearing.ogg` | 物品/武器 | 0.60s | 12.3 | 物品/武器音效；动作：通用/特殊；主题：Missile Nearing。 代码标识：sound。 | Projectiles\Ranged\NukeOfBliss.cs:89 (sound); Projectiles\Rogue\ExorcismProj.cs:102 (sound) |
| 645 | `Item\MittFail.ogg` | 物品/武器 | 0.60s | 12.7 | 物品/武器音效；动作：通用/特殊；主题：Mitt Fail。 代码标识：aud。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:472 (aud) |
| 646 | `Item\MittHit.ogg` | 物品/武器 | 1.02s | 19.4 | 物品/武器音效；动作：受击/命中/冲击；主题：Mitt Hit。 代码标识：aud。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:451 (aud) |
| 647 | `Item\MittReadyPalm.ogg` | 物品/武器 | 0.63s | 14.7 | 物品/武器音效；动作：通用/特殊；主题：Mitt Ready Palm。 代码标识：aud。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:330 (aud) |
| 648 | `Item\MittThrust.ogg` | 物品/武器 | 0.65s | 9.1 | 物品/武器音效；动作：近战挥击/撞击；主题：Mitt Thrust。 代码标识：aud。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:335 (aud) |
| 654 | `Item\MoltenAmputatorRecall.ogg` | 物品/武器 | 0.77s | 14.6 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Molten Amputator Recall。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 655 | `Item\MurasamaBigSwing.ogg` | 物品/武器 | 0.40s | 9.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Murasama Big Swing。 代码标识：slash。 | NPCs\SupremeCalamitas\SupremeCatastrophe.cs:304 (slash); NPCs\SupremeCalamitas\SupremeCatastrophe.cs:397 (slash); NPCs\SupremeCalamitas\SupremeCatastrophe.cs:458 (slash); 另 1 处 |
| 656 | `Item\MurasamaHitInorganic.ogg` | 物品/武器 | 1.17s | 19.4 | 物品/武器音效；动作：受击/命中/冲击；主题：Murasama Hit Inorganic。 代码标识：InorganicHit。 | Items\Weapons\Melee\Murasama.cs:21 (InorganicHit) |
| 657 | `Item\MurasamaHitOrganic.ogg` | 物品/武器 | 1.00s | 18.0 | 物品/武器音效；动作：受击/命中/冲击；主题：Murasama Hit Organic。 代码标识：OrganicHit。 | Items\Weapons\Melee\Murasama.cs:20 (OrganicHit) |
| 658 | `Item\MurasamaSwing.ogg` | 物品/武器 | 0.40s | 9.2 | 物品/武器音效；动作：近战挥击/撞击；主题：Murasama Swing。 代码标识：Swing。 | Items\Weapons\Melee\Murasama.cs:22 (Swing) |
| 666 | `Item\NanoSwarm.ogg` | 物品/武器 | 0.34s | 6.8 | 物品/武器音效；动作：通用/特殊；主题：Nano Swarm。 代码标识：Nanomachines。 | Items\Weapons\Ranged\BlightSpewer.cs:17 (Nanomachines) |
| 667 | `Item\NidhoggBigShot.ogg` | 物品/武器 | 1.41s | 43.6 | 物品/武器音效；动作：射击/发射；主题：Nidhogg Big Shot。 代码标识：sound。 | Projectiles\DraedonsArsenal\NidhoggHoldout.cs:156 (sound) |
| 668 | `Item\NidhoggCharge.ogg` | 物品/武器 | 3.78s | 72.6 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Nidhogg Charge。 代码标识：sound。 | Projectiles\DraedonsArsenal\NidhoggHoldout.cs:109 (sound) |
| 669 | `Item\NidhoggFire.ogg` | 物品/武器 | 0.95s | 23.8 | 物品/武器音效；动作：射击/发射；主题：Nidhogg Fire。 代码标识：sound。 | Projectiles\DraedonsArsenal\NidhoggHoldout.cs:180 (sound); Projectiles\DraedonsArsenal\VulcanSpear.cs:100 (sound2) |
| 670 | `Item\NitroExpressRifleFire.ogg` | 物品/武器 | 1.04s | 11.9 | 物品/武器音效；动作：射击/发射；主题：Nitro Express Rifle Fire。 代码标识：FireSound。 | Items\Weapons\Ranged\NitroExpressRifle.cs:15 (FireSound) |
| 671 | `Item\NorfleetFire.ogg` | 物品/武器 | 1.46s | 15.1 | 物品/武器音效；动作：射击/发射；主题：Norfleet Fire。 代码标识：fire。 | Projectiles\Ranged\NorfleetCannon.cs:189 (fire) |
| 672 | `Item\NorfleetRecharge.ogg` | 物品/武器 | 3.94s | 33.0 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Norfleet Recharge。 代码标识：charge。 | Projectiles\Ranged\NorfleetCannon.cs:255 (charge) |
| 673 | `Item\NormalityRelocator1.ogg` | 物品/武器 | 0.92s | 33.0 | 物品/武器音效；动作：通用/特殊；主题：Normality Relocator。 代码标识：TeleportSound。 | Items\Tools\NormalityRelocator.cs:16 (TeleportSound) |
| 674 | `Item\NormalityRelocator2.ogg` | 物品/武器 | 0.92s | 33.7 | 物品/武器音效；动作：通用/特殊；主题：Normality Relocator。 代码标识：TeleportSound。 | Items\Tools\NormalityRelocator.cs:16 (TeleportSound) |
| 675 | `Item\NormalityRelocator3.ogg` | 物品/武器 | 0.92s | 33.7 | 物品/武器音效；动作：通用/特殊；主题：Normality Relocator。 代码标识：TeleportSound。 | Items\Tools\NormalityRelocator.cs:16 (TeleportSound) |
| 676 | `Item\NuhUhUh.ogg` | 物品/武器 | 3.00s | 76.1 | 物品/武器音效；动作：通用/特殊；主题：Nuh Uh Uh。 首处代码上下文：NorfleetCannon.cs。 | Projectiles\Ranged\NorfleetCannon.cs:333 |
| 677 | `Item\NullHit.ogg` | 物品/武器 | 0.69s | 17.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Null Hit。 代码标识：h。 | Projectiles\Typeless\RelicOfConvergenceCrystal.cs:72 (h); Items\Weapons\Ranged\NullificationPistol.cs:19 (HitSound) |
| 678 | `Item\NullImpact.ogg` | 物品/武器 | 0.40s | 13.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Null Impact。 代码标识：transform。 | CalPlayer\CalamityPlayerMiscEffects.cs:1782 (transform); CalPlayer\CalamityPlayerMiscEffects.cs:1833 (transform); Projectiles\Ranged\NullFlash.cs:64 (fire); 另 1 处 |
| 679 | `Item\NullShot.ogg` | 物品/武器 | 0.73s | 19.3 | 物品/武器音效；动作：射击/发射；主题：Null Shot。 代码标识：activate。 | CalPlayer\CalamityPlayer.cs:3512 (activate); Items\Weapons\Ranged\NullificationPistol.cs:93 (fire) |
| 680 | `Item\OmicronBeam.ogg` | 物品/武器 | 1.45s | 13.9 | 物品/武器音效；动作：激光/光束；主题：Omicron Beam。 代码标识：fire。 | Projectiles\Magic\OmicronHoldout.cs:100 (fire); Projectiles\Melee\LightspeedHoldout.cs:344 (otherSound); Projectiles\Melee\LucreciaHoldout.cs:194 (projectile); 另 1 处 |
| 681 | `Item\OntologicalDespoilerLargeImpact.ogg` | 物品/武器 | 1.79s | 46.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Ontological Despoiler Large Impact。 代码标识：fire。 | Projectiles\Ranged\OntologicalDespoilerBeam.cs:122 (fire) |
| 682 | `Item\OntologicalDespoilerLargeShot.ogg` | 物品/武器 | 1.25s | 20.4 | 物品/武器音效；动作：射击/发射；主题：Ontological Despoiler Large Shot。 首处代码上下文：OrdoSigil.cs。 | Projectiles\Magic\OrdoSigil.cs:47; Items\Weapons\Ranged\OntologicalDespoiler.cs:24 (BigShot2) |
| 683 | `Item\OntologicalDespoilerSmallImpact.ogg` | 物品/武器 | 0.96s | 16.4 | 物品/武器音效；动作：受击/命中/冲击；主题：Ontological Despoiler Small Impact。 代码标识：fire。 | Projectiles\Ranged\OntologicalDespoilerShot.cs:192 (fire) |
| 684 | `Item\OpalCharge.ogg` | 物品/武器 | 1.37s | 13.1 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Opal Charge。 代码标识：Charge。 | Items\Weapons\Ranged\OpalStriker.cs:15 (Charge) |
| 685 | `Item\OpalChargedFire.ogg` | 物品/武器 | 1.82s | 20.6 | 物品/武器音效；动作：射击/发射；主题：Opal Charged Fire。 首处代码上下文：FireImplosion.cs。 | Projectiles\Magic\FireImplosion.cs:43; Projectiles\Typeless\RelicOfDeliveranceSpear.cs:247 (sound3); Items\Weapons\Ranged\OpalStriker.cs:19 (ChargedFire) |
| 686 | `Item\OpalChargeLoop.ogg` | 物品/武器 | 2.60s | 21.2 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Opal Charge Loop。 代码标识：ChargeLoop。 | Items\Weapons\Ranged\OpalStriker.cs:16 (ChargeLoop) |
| 687 | `Item\OpalFire.ogg` | 物品/武器 | 1.24s | 11.8 | 物品/武器音效；动作：射击/发射；主题：Opal Fire。 代码标识：Fire。 | Items\Weapons\Ranged\OpalStriker.cs:18 (Fire) |
| 688 | `Item\OracleHum.ogg` | 物品/武器 | 2.05s | 32.1 | 物品/武器音效；动作：通用/特殊；主题：Oracle Hum。 代码标识：charge。 | Projectiles\Melee\Yoyos\OracleYoyo.cs:169 (charge) |
| 689 | `Item\PBGSummon.ogg` | 物品/武器 | 3.61s | 79.0 | 物品/武器音效；动作：激活/使用/UI；主题：PBG Summon。 代码标识：UseSound。 | Items\SummonItems\Abombination.cs:18 (UseSound) |
| 690 | `Item\PhalanxSurgeCharge.ogg` | 物品/武器 | 1.08s | 28.4 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Phalanx Surge Charge。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:152 (sound) |
| 691 | `Item\PhalanxSurgeChargeLoop.ogg` | 物品/武器 | 2.73s | 57.9 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Phalanx Surge Charge Loop。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:147 (sound) |
| 692 | `Item\PhalanxSurgeChargeMax.ogg` | 物品/武器 | 0.28s | 11.6 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Phalanx Surge Charge Max。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:158 (sound) |
| 693 | `Item\PhalanxSurgeChargeShoot.ogg` | 物品/武器 | 1.88s | 45.8 | 物品/武器音效；动作：射击/发射；主题：Phalanx Surge Charge Shoot。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:260 (sound) |
| 694 | `Item\PhalanxSurgeShoot.ogg` | 物品/武器 | 1.07s | 27.4 | 物品/武器音效；动作：射击/发射；主题：Phalanx Surge Shoot。 代码标识：sound。 | Projectiles\DraedonsArsenal\PhalanxSurgeHoldout.cs:278 (sound) |
| 695 | `Item\PhantasmalFuryShoot.ogg` | 物品/武器 | 1.46s | 21.2 | 物品/武器音效；动作：射击/发射；主题：Phantasmal Fury Shoot。 代码标识：UseSound。 | Items\Weapons\Magic\PhantasmalFury.cs:35 (UseSound) |
| 696 | `Item\PhantomHeartUse.ogg` | 物品/武器 | 4.31s | 30.1 | 物品/武器音效；动作：激活/使用/UI；主题：Phantom Heart Use。 代码标识：UseSound。 | Items\PermanentBoosters\PhantomHeart.cs:17 (UseSound) |
| 697 | `Item\PhantomSpirit.ogg` | 物品/武器 | 1.27s | 12.0 | 物品/武器音效；动作：通用/特殊；主题：Phantom Spirit。 代码标识：HitSound。 | Projectiles\Rogue\PhantasmalSoulBlue.cs:11 (HitSound); Items\Weapons\Melee\VoidEdge.cs:88 (sound) |
| 698 | `Item\PhotoHitSound.ogg` | 物品/武器 | 0.31s | 7.0 | 物品/武器音效；动作：受击/命中/冲击；主题：Photo Hit Sound。 代码标识：HitSound。 | Items\Weapons\Ranged\Photoviscerator.cs:19 (HitSound) |
| 699 | `Item\PhotoUseSound.ogg` | 物品/武器 | 0.99s | 13.0 | 物品/武器音效；动作：激活/使用/UI；主题：Photo Use Sound。 代码标识：BigBeamSound。 | Projectiles\Ranged\TauCannonHoldout.cs:74 (BigBeamSound); Items\Weapons\Ranged\Photoviscerator.cs:18 (UseSound) |
| 700 | `Item\PlasmaBig.ogg` | 物品/武器 | 1.24s | 21.7 | 物品/武器音效；动作：通用/特殊；主题：Plasma Big。 代码标识：sound。 | Projectiles\DraedonsArsenal\ImmolationArrow.cs:225 (sound) |
| 701 | `Item\PlasmaBlast.ogg` | 物品/武器 | 2.00s | 15.2 | 物品/武器音效；动作：爆炸/爆裂；主题：Plasma Blast。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 702 | `Item\PlasmaBolt.ogg` | 物品/武器 | 1.29s | 10.2 | 物品/武器音效；动作：通用/特殊；主题：Plasma Bolt。 代码标识：PlasmaBoltSound。 | Sounds\CommonCalamitySounds.cs:27 (PlasmaBoltSound) |
| 703 | `Item\PlasmaCasterFire.ogg` | 物品/武器 | 1.31s | 20.4 | 物品/武器音效；动作：射击/发射；主题：Plasma Caster Fire。 代码标识：FireSound。 | Items\Weapons\DraedonsArsenal\PlasmaCaster.cs:20 (FireSound) |
| 704 | `Item\PlasmaGrenadeExplosion.ogg` | 物品/武器 | 1.50s | 16.7 | 物品/武器音效；动作：爆炸/爆裂；主题：Plasma Grenade Explosion。 代码标识：ExplosionSound。 | Items\Weapons\DraedonsArsenal\PlasmaGrenade.cs:21 (ExplosionSound) |
| 705 | `Item\PlasmaRifleAlt.ogg` | 物品/武器 | 0.76s | 16.4 | 物品/武器音效；动作：通用/特殊；主题：Plasma Rifle Alt。 代码标识：FastShotSound。 | Items\Weapons\Magic\PlasmaRifle.cs:16 (FastShotSound) |
| 706 | `Item\PlasmaRifleMain.ogg` | 物品/武器 | 1.27s | 26.1 | 物品/武器音效；动作：通用/特殊；主题：Plasma Rifle Main。 代码标识：HeavyShotSound。 | Items\Weapons\Magic\PlasmaRifle.cs:15 (HeavyShotSound); Items\Weapons\Ranged\Auralis.cs:17 (HeavyShotSound) |
| 707 | `Item\PlasmaSmall.ogg` | 物品/武器 | 0.22s | 8.1 | 物品/武器音效；动作：通用/特殊；主题：Plasma Small。 代码标识：PlasmaSound。 | Items\Weapons\DraedonsArsenal\HolofibreImmolator.cs:20 (PlasmaSound) |
| 708 | `Item\PolarisShot.ogg` | 物品/武器 | 0.27s | 6.4 | 物品/武器音效；动作：射击/发射；主题：Polaris Shot。 代码标识：Shot。 | Items\Weapons\Ranged\PolarisParrotfish.cs:21 (Shot) |
| 709 | `Item\ProtolithBangleSound.ogg` | 物品/武器 | 2.57s | 57.1 | 物品/武器音效；动作：通用/特殊；主题：Protolith Bangle Sound。 代码标识：sound。 | Projectiles\Typeless\ProtolithBangleProjectile.cs:80 (sound) |
| 710 | `Item\PulseRifleFire.ogg` | 物品/武器 | 1.51s | 42.5 | 物品/武器音效；动作：射击/发射；主题：Pulse Rifle Fire。 代码标识：fire2。 | Projectiles\Magic\ApoctosisArrayHoldout.cs:232 (fire2); Items\Weapons\DraedonsArsenal\PulseRifle.cs:20 (FireSound) |
| 711 | `Item\PulseSound.ogg` | 物品/武器 | 1.15s | 31.1 | 物品/武器音效；动作：通用/特殊；主题：Pulse Sound。 代码标识：pulse。 | Projectiles\DraedonsArsenal\PulseGrenadeOrb.cs:91 (pulse); Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:108 (pulse); Projectiles\DraedonsArsenal\PulsePistolShot.cs:126 (pulse); 另 1 处 |
| 712 | `Item\PulseSoundHeavy.ogg` | 物品/武器 | 0.94s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Pulse Sound Heavy。 代码标识：pulseHard。 | Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:313 (pulseHard); Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:316 (pulse) |
| 713 | `Item\PumpkaboomNormalTicking.ogg` | 物品/武器 | 1.88s | 16.6 | 物品/武器音效；动作：爆炸/爆裂；主题：Pumpkaboom Normal Ticking。 首处代码上下文：PumpkaboomSmall.cs。 | Projectiles\Rogue\PumpkaboomSmall.cs:94 |
| 714 | `Item\PumpkaboomStealthTicking.ogg` | 物品/武器 | 1.88s | 20.3 | 物品/武器音效；动作：爆炸/爆裂；主题：Pumpkaboom Stealth Ticking。 首处代码上下文：PumpkaboomBig.cs。 | Projectiles\Rogue\PumpkaboomBig.cs:97 |
| 715 | `Item\PwnagehammerBigImpact.ogg` | 物品/武器 | 2.86s | 29.1 | 物品/武器音效；动作：受击/命中/冲击；主题：Pwnagehammer Big Impact。 代码标识：BigSound。 | Projectiles\Melee\PwnagehammerEcho.cs:13 (BigSound) |
| 716 | `Item\PwnagehammerSound.ogg` | 物品/武器 | 1.85s | 22.5 | 物品/武器音效；动作：通用/特殊；主题：Pwnagehammer Sound。 代码标识：UseSound。 | Projectiles\Melee\FallenPaladinsHammerProj.cs:18 (UseSound); Projectiles\Melee\GalaxySmasherHammer.cs:23 (UseSound); Projectiles\Melee\PwnagehammerProj.cs:14 (UseSound); 另 2 处 |
| 717 | `Item\RadiationBurst.ogg` | 物品/武器 | 2.64s | 82.3 | 物品/武器音效；动作：爆炸/爆裂；主题：Radiation Burst。 代码标识：fire。 | Projectiles\Rogue\ReaperProjectile.cs:179 (fire); Projectiles\Rogue\ReaperProjectile.cs:252 (fire) |
| 718 | `Item\RadiationRain.ogg` | 物品/武器 | 4.60s | 164.4 | 物品/武器音效；动作：激活/使用/UI；主题：Radiation Rain。 代码标识：fire。 | Projectiles\Rogue\ReaperProjectile.cs:234 (fire) |
| 719 | `Item\RealityRupture.ogg` | 物品/武器 | 1.01s | 11.3 | 物品/武器音效；动作：通用/特殊；主题：Reality Rupture。 代码标识：ThrowSound。 | Items\Weapons\Rogue\RealityRupture.cs:16 (ThrowSound) |
| 720 | `Item\RealityRuptureStealth.ogg` | 物品/武器 | 1.86s | 17.5 | 物品/武器音效；动作：通用/特殊；主题：Reality Rupture Stealth。 代码标识：RightClickSound。 | Projectiles\Ranged\ScorpioHoldout.cs:165 (RightClickSound); Items\Weapons\Rogue\RealityRupture.cs:18 (ThrowSound3) |
| 721 | `Item\RealityRuptureStealthHit.ogg` | 物品/武器 | 1.54s | 15.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Reality Rupture Stealth Hit。 代码标识：Hitsound。 | Projectiles\Rogue\RealityRuptureStealth.cs:14 (Hitsound) |
| 722 | `Item\RubicoReload.ogg` | 物品/武器 | 0.57s | 22.8 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Rubico Reload。 代码标识：ReloadSound。 | Items\Weapons\Ranged\RubicoPrime.cs:17 (ReloadSound) |
| 723 | `Item\SanctifiedSparkHappy.ogg` | 物品/武器 | 0.72s | 29.2 | 物品/武器音效；动作：通用/特殊；主题：Sanctified Spark Happy。 代码标识：happy。 | Projectiles\Summon\ProfanedEnergy.cs:123 (happy) |
| 724 | `Item\SarosDiskThrow1.ogg` | 物品/武器 | 0.33s | 10.3 | 物品/武器音效；动作：射击/发射；主题：Saros Disk Throw。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 725 | `Item\SarosDiskThrow2.ogg` | 物品/武器 | 0.34s | 10.4 | 物品/武器音效；动作：射击/发射；主题：Saros Disk Throw。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 726 | `Item\SarosDiskThrow3.ogg` | 物品/武器 | 0.39s | 10.8 | 物品/武器音效；动作：射击/发射；主题：Saros Disk Throw。 未找到直接字符串引用，可能由资源约定、动态路径或遗留资源使用。 | 未直接匹配 |
| 727 | `Item\SawShot1.ogg` | 物品/武器 | 0.60s | 12.0 | 物品/武器音效；动作：射击/发射；主题：Saw Shot。 代码标识：ShootSound。 | Projectiles\Ranged\BuzzkillHoldout.cs:80 (ShootSound); Projectiles\Ranged\SuperradiantSlaughtererHoldout.cs:125 (ShootSound) |
| 728 | `Item\SawShot2.ogg` | 物品/武器 | 0.59s | 12.1 | 物品/武器音效；动作：射击/发射；主题：Saw Shot。 代码标识：ShootSound。 | Projectiles\Ranged\BuzzkillHoldout.cs:80 (ShootSound); Projectiles\Ranged\SuperradiantSlaughtererHoldout.cs:125 (ShootSound) |
| 735 | `Item\ScorchedEarthShot1.ogg` | 物品/武器 | 1.95s | 57.2 | 物品/武器音效；动作：射击/发射；主题：Scorched Earth Shot。 首处代码上下文：MineralMortarHoldout.cs。 | Projectiles\Ranged\MineralMortarHoldout.cs:63; Projectiles\Ranged\ScorchedEarthRocket.cs:65 (PrimeSound); NPCs\SupremeCalamitas\SupremeCataclysm.cs:423 (charge) |
| 736 | `Item\ScorchedEarthShot2.ogg` | 物品/武器 | 1.83s | 55.0 | 物品/武器音效；动作：射击/发射；主题：Scorched Earth Shot。 首处代码上下文：MineralMortarHoldout.cs。 | Projectiles\Ranged\MineralMortarHoldout.cs:63; Projectiles\Ranged\ScorchedEarthRocket.cs:65 (PrimeSound); NPCs\SupremeCalamitas\SupremeCataclysm.cs:423 (charge) |
| 737 | `Item\ScorchedEarthShot3.ogg` | 物品/武器 | 1.60s | 48.2 | 物品/武器音效；动作：射击/发射；主题：Scorched Earth Shot。 首处代码上下文：MineralMortarHoldout.cs。 | Projectiles\Ranged\MineralMortarHoldout.cs:63; Projectiles\Ranged\ScorchedEarthRocket.cs:65 (PrimeSound); NPCs\SupremeCalamitas\SupremeCataclysm.cs:423 (charge) |
| 738 | `Item\ScorpioHit.ogg` | 物品/武器 | 0.39s | 8.3 | 物品/武器音效；动作：受击/命中/冲击；主题：Scorpio Hit。 代码标识：onKill。 | Projectiles\Ranged\VanquisherArrowProj.cs:146 (onKill); Items\Weapons\Ranged\Scorpio.cs:37 (RocketHit) |
| 739 | `Item\ScorpioNukeHit.ogg` | 物品/武器 | 2.41s | 24.2 | 物品/武器音效；动作：受击/命中/冲击；主题：Scorpio Nuke Hit。 代码标识：fire。 | Projectiles\Ranged\NorfleetComet.cs:84 (fire); Projectiles\Summon\SiriusQuasar.cs:82 (fire); Items\Weapons\Ranged\Scorpio.cs:38 (NukeHit) |
| 740 | `Item\ScorpioShot.ogg` | 物品/武器 | 0.44s | 8.6 | 物品/武器音效；动作：射击/发射；主题：Scorpio Shot。 代码标识：RocketShoot。 | Items\Weapons\Ranged\ScorchedEarth.cs:19 (RocketShoot); Items\Weapons\Ranged\Scorpio.cs:36 (RocketShoot) |
| 741 | `Item\SevensStrikerBust.ogg` | 物品/武器 | 1.20s | 26.6 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Bust。 代码标识：BustSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:20 (BustSound) |
| 742 | `Item\SevensStrikerBustGFB.ogg` | 物品/武器 | 1.09s | 31.3 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Bust GFB。 代码标识：BustGFB。 | Items\Weapons\Ranged\TheSevensStriker.cs:21 (BustGFB) |
| 743 | `Item\SevensStrikerCoinShot.ogg` | 物品/武器 | 0.37s | 11.1 | 物品/武器音效；动作：射击/发射；主题：Sevens Striker Coin Shot。 代码标识：CoinSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:26 (CoinSound) |
| 744 | `Item\SevensStrikerDoubles.ogg` | 物品/武器 | 1.02s | 24.1 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Doubles。 代码标识：DoublesSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:22 (DoublesSound) |
| 745 | `Item\SevensStrikerJackpot.ogg` | 物品/武器 | 0.96s | 22.5 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Jackpot。 代码标识：JackpotSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:24 (JackpotSound) |
| 746 | `Item\SevensStrikerJackpotGFB.ogg` | 物品/武器 | 1.64s | 45.0 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Jackpot GFB。 代码标识：JackpotGFB。 | Items\Weapons\Ranged\TheSevensStriker.cs:25 (JackpotGFB) |
| 747 | `Item\SevensStrikerRoulette.ogg` | 物品/武器 | 2.77s | 57.6 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Roulette。 代码标识：RouletteSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:18 (RouletteSound) |
| 748 | `Item\SevensStrikerRouletteTick.ogg` | 物品/武器 | 0.16s | 7.0 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Roulette Tick。 代码标识：RouletteTickSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:19 (RouletteTickSound) |
| 749 | `Item\SevensStrikerTriples.ogg` | 物品/武器 | 1.11s | 25.5 | 物品/武器音效；动作：通用/特殊；主题：Sevens Striker Triples。 代码标识：TriplesSound。 | Items\Weapons\Ranged\TheSevensStriker.cs:23 (TriplesSound) |
| 750 | `Item\ShadowboltReflect.ogg` | 物品/武器 | 1.16s | 27.9 | 物品/武器音效；动作：通用/特殊；主题：Shadowbolt Reflect。 代码标识：bounce。 | Projectiles\Magic\Shadowbolt.cs:76 (bounce); Projectiles\Ranged\OntologicalDespoilerGrenade.cs:172 (fire2) |
| 751 | `Item\ShadowboltWallHit.ogg` | 物品/武器 | 0.19s | 10.1 | 物品/武器音效；动作：受击/命中/冲击；主题：Shadowbolt Wall Hit。 代码标识：wall。 | Projectiles\Magic\Shadowbolt.cs:122 (wall); Projectiles\Ranged\HyperiusBulletProj.cs:145 |
| 752 | `Item\SHPCVacuumEnd.ogg` | 物品/武器 | 0.96s | 25.6 | 物品/武器音效；动作：循环/开始结束/预警；主题：SHPC Vacuum End。 代码标识：VacuumEnd。 | Items\Weapons\Magic\SHPC.cs:24 (VacuumEnd) |
| 753 | `Item\SHPCVacuumLoop.ogg` | 物品/武器 | 2.24s | 57.3 | 物品/武器音效；动作：循环/开始结束/预警；主题：SHPC Vacuum Loop。 代码标识：VacuumLoop。 | Items\Weapons\Magic\SHPC.cs:23 (VacuumLoop) |
| 754 | `Item\SHPCVacuumStart.ogg` | 物品/武器 | 1.37s | 33.8 | 物品/武器音效；动作：循环/开始结束/预警；主题：SHPC Vacuum Start。 代码标识：VacuumStart。 | Items\Weapons\Magic\SHPC.cs:22 (VacuumStart) |
| 755 | `Item\ShrimpFire.ogg` | 物品/武器 | 0.58s | 23.2 | 物品/武器音效；动作：射击/发射；主题：Shrimp Fire。 代码标识：Fire。 | Items\Weapons\DraedonsArsenal\AqueousHunterDrone.cs:19 (Fire) |
| 756 | `Item\ShrimpMissileHit.ogg` | 物品/武器 | 0.35s | 12.3 | 物品/武器音效；动作：受击/命中/冲击；主题：Shrimp Missile Hit。 代码标识：Hit。 | Items\Weapons\DraedonsArsenal\AqueousHunterDrone.cs:20 (Hit) |
| 757 | `Item\ShrimpSound1.ogg` | 物品/武器 | 0.77s | 29.6 | 物品/武器音效；动作：通用/特殊；主题：Shrimp Sound。 代码标识：Sound1。 | Items\Weapons\DraedonsArsenal\AqueousHunterDrone.cs:21 (Sound1) |
| 758 | `Item\ShrimpSound2.ogg` | 物品/武器 | 0.81s | 33.8 | 物品/武器音效；动作：通用/特殊；主题：Shrimp Sound。 代码标识：Sound2。 | Items\Weapons\DraedonsArsenal\AqueousHunterDrone.cs:22 (Sound2) |
| 759 | `Item\ShrimpSurprise.ogg` | 物品/武器 | 0.43s | 18.5 | 物品/武器音效；动作：通用/特殊；主题：Shrimp Surprise。 代码标识：Surprise。 | Items\Weapons\DraedonsArsenal\AqueousHunterDrone.cs:23 (Surprise) |
| 760 | `Item\SignusSpawn.ogg` | 物品/武器 | 6.77s | 114.5 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Signus Spawn。 代码标识：SignutSound。 | Items\SummonItems\MarkofProvidence.cs:20 (SignutSound) |
| 761 | `Item\SkytideBolt.ogg` | 物品/武器 | 1.77s | 25.6 | 物品/武器音效；动作：通用/特殊；主题：Skytide Bolt。 代码标识：sound。 | Projectiles\Melee\SkytideDragoonHoldout.cs:222 (sound); Projectiles\Melee\SkytideDragoonHoldout.cs:232 (fire) |
| 762 | `Item\SkytideSwing.ogg` | 物品/武器 | 1.77s | 19.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Skytide Swing。 代码标识：fire。 | Projectiles\Melee\SkytideDragoonHoldout.cs:207 (fire) |
| 763 | `Item\SnootBooped.ogg` | 物品/武器 | 0.41s | 14.1 | 物品/武器音效；动作：通用/特殊；主题：Snoot Booped。 代码标识：boop。 | Projectiles\Melee\GrandDadHoldout.cs:242 (boop) |
| 764 | `Item\SoupConsumption.ogg` | 物品/武器 | 16.13s | 389.5 | 物品/武器音效；动作：激活/使用/UI；主题：Soup Consumption。 代码标识：UseSound。 | Items\Potions\Food\LavaChickenBroth.cs:16 (UseSound) |
| 765 | `Item\SpearofDestiny.ogg` | 物品/武器 | 0.62s | 7.7 | 物品/武器音效；动作：通用/特殊；主题：Spearof Destiny。 代码标识：fire。 | Items\Weapons\Rogue\MoltenAmputator.cs:76 (fire); Items\Weapons\Rogue\SpearofDestiny.cs:13 (ThrowSound) |
| 766 | `Item\SpinningWoosh.ogg` | 物品/武器 | 5.12s | 98.6 | 物品/武器音效；动作：近战挥击/撞击；主题：Spinning Woosh。 代码标识：spin。 | Projectiles\Ranged\HellbornHoldout.cs:98 (spin); Projectiles\Rogue\ReaperProjectile.cs:120; Items\Weapons\Melee\AbyssBlade.cs:13 (SpinSound); 另 1 处 |
| 767 | `Item\Splatshot.ogg` | 物品/武器 | 0.43s | 8.7 | 物品/武器音效；动作：射击/发射；主题：Splatshot。 代码标识：Shot。 | Items\Weapons\Ranged\SpeedBlaster.cs:18 (Shot) |
| 768 | `Item\SplatshotBig.ogg` | 物品/武器 | 0.67s | 12.0 | 物品/武器音效；动作：射击/发射；主题：Splatshot Big。 代码标识：ShotBig。 | Items\Weapons\Ranged\SpeedBlaster.cs:20 (ShotBig) |
| 769 | `Item\SplatshotBigImpact.ogg` | 物品/武器 | 0.65s | 11.3 | 物品/武器音效；动作：受击/命中/冲击；主题：Splatshot Big Impact。 代码标识：ShotImpactBig。 | Projectiles\Ranged\SpeedBlasterShot.cs:23 (ShotImpactBig) |
| 770 | `Item\SplatshotDash.ogg` | 物品/武器 | 0.53s | 10.0 | 物品/武器音效；动作：射击/发射；主题：Splatshot Dash。 代码标识：Dash。 | Items\Weapons\Ranged\SpeedBlaster.cs:19 (Dash) |
| 771 | `Item\SplatshotImpact.ogg` | 物品/武器 | 0.24s | 7.3 | 物品/武器音效；动作：受击/命中/冲击；主题：Splatshot Impact。 代码标识：ShotImpact。 | Projectiles\Ranged\SpeedBlasterShot.cs:22 (ShotImpact) |
| 772 | `Item\SporeKnifeChomp1.ogg` | 物品/武器 | 0.30s | 20.1 | 物品/武器音效；动作：通用/特殊；主题：Spore Knife Chomp。 代码标识：ChompSound。 | Items\Weapons\Rogue\SporeKnife.cs:15 (ChompSound) |
| 773 | `Item\SporeKnifeChomp2.ogg` | 物品/武器 | 0.33s | 21.9 | 物品/武器音效；动作：通用/特殊；主题：Spore Knife Chomp。 代码标识：ChompSound。 | Items\Weapons\Rogue\SporeKnife.cs:15 (ChompSound) |
| 774 | `Item\SporeKnifeChomp3.ogg` | 物品/武器 | 0.30s | 20.0 | 物品/武器音效；动作：通用/特殊；主题：Spore Knife Chomp。 代码标识：ChompSound。 | Items\Weapons\Rogue\SporeKnife.cs:15 (ChompSound) |
| 775 | `Item\SporeKnifeImpact.ogg` | 物品/武器 | 0.49s | 26.8 | 物品/武器音效；动作：受击/命中/冲击；主题：Spore Knife Impact。 代码标识：ImpactSound。 | Items\Weapons\Rogue\SporeKnife.cs:13 (ImpactSound) |
| 776 | `Item\SporeKnifeStealthImpact.ogg` | 物品/武器 | 1.08s | 50.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Spore Knife Stealth Impact。 代码标识：StealthImpactSound。 | Items\Weapons\Rogue\SporeKnife.cs:14 (StealthImpactSound) |
| 777 | `Item\SporeKnifeThrow1.ogg` | 物品/武器 | 0.44s | 25.2 | 物品/武器音效；动作：射击/发射；主题：Spore Knife Throw。 代码标识：ThrowSound。 | Items\Weapons\Rogue\SporeKnife.cs:12 (ThrowSound) |
| 778 | `Item\SporeKnifeThrow2.ogg` | 物品/武器 | 0.47s | 27.8 | 物品/武器音效；动作：射击/发射；主题：Spore Knife Throw。 代码标识：ThrowSound。 | Items\Weapons\Rogue\SporeKnife.cs:12 (ThrowSound) |
| 779 | `Item\StarfleetFire.ogg` | 物品/武器 | 1.03s | 31.3 | 物品/武器音效；动作：射击/发射；主题：Starfleet Fire。 代码标识：shotgunFire。 | Projectiles\Ranged\StarfleetHoldout.cs:171 (shotgunFire) |
| 780 | `Item\StarfleetStarburst.ogg` | 物品/武器 | 1.25s | 45.7 | 物品/武器音效；动作：爆炸/爆裂；主题：Starfleet Starburst。 代码标识：test。 | Projectiles\Ranged\StarfleetHoldout.cs:115 (test); Projectiles\Ranged\StarmadaHoldout.cs:129 (blast1); Projectiles\Ranged\StarmadaHoldout.cs:131 (blast2) |
| 781 | `Item\StarmadaFire.ogg` | 物品/武器 | 1.03s | 39.9 | 物品/武器音效；动作：射击/发射；主题：Starmada Fire。 代码标识：shotgunFire。 | Projectiles\Ranged\StarmadaHoldout.cs:226 (shotgunFire) |
| 782 | `Item\StellarContemptClone.ogg` | 物品/武器 | 1.17s | 24.4 | 物品/武器音效；动作：激活/使用/UI；主题：Stellar Contempt Clone。 代码标识：RedHamSound。 | Projectiles\Melee\StellarContemptHammer.cs:21 (RedHamSound) |
| 783 | `Item\StellarContemptImpact.ogg` | 物品/武器 | 3.00s | 43.5 | 物品/武器音效；动作：受击/命中/冲击；主题：Stellar Contempt Impact。 代码标识：SlamHamSound。 | Projectiles\Melee\StellarContemptEcho.cs:16 (SlamHamSound) |
| 784 | `Item\StormWeaverSpawn.ogg` | 物品/武器 | 4.85s | 91.5 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Storm Weaver Spawn。 代码标识：StormSound。 | Items\SummonItems\MarkofProvidence.cs:21 (StormSound) |
| 785 | `Item\StratusSphereCast.ogg` | 物品/武器 | 1.41s | 28.3 | 物品/武器音效；动作：通用/特殊；主题：Stratus Sphere Cast。 代码标识：CastSound。 | Items\Weapons\Typeless\StratusSphere.cs:27 (CastSound) |
| 786 | `Item\StygianBonk1.ogg` | 物品/武器 | 0.50s | 10.6 | 物品/武器音效；动作：激活/使用/UI；主题：Stygian Bonk。 代码标识：DashHitSound。 | Items\Weapons\Melee\StygianShield.cs:21 (DashHitSound) |
| 787 | `Item\StygianBonk2.ogg` | 物品/武器 | 0.55s | 10.5 | 物品/武器音效；动作：激活/使用/UI；主题：Stygian Bonk。 代码标识：DashHitSound。 | Items\Weapons\Melee\StygianShield.cs:21 (DashHitSound) |
| 788 | `Item\StygianBonk3.ogg` | 物品/武器 | 0.39s | 9.0 | 物品/武器音效；动作：激活/使用/UI；主题：Stygian Bonk。 代码标识：DashHitSound。 | Items\Weapons\Melee\StygianShield.cs:21 (DashHitSound) |
| 789 | `Item\StygianCatch.ogg` | 物品/武器 | 0.60s | 10.4 | 物品/武器音效；动作：通用/特殊；主题：Stygian Catch。 代码标识：ShieldCatchSound。 | Items\Weapons\Melee\StygianShield.cs:25 (ShieldCatchSound) |
| 790 | `Item\StygianDash.ogg` | 物品/武器 | 2.69s | 35.0 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Stygian Dash。 代码标识：DashSound。 | Items\Weapons\Melee\StygianShield.cs:20 (DashSound); Items\Weapons\Rogue\NanoblackReaper.cs:101 (flurryActivationSound1) |
| 791 | `Item\StygianDashCharge.ogg` | 物品/武器 | 1.07s | 17.2 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Stygian Dash Charge。 代码标识：DashChargeSound。 | Items\Weapons\Melee\StygianShield.cs:19 (DashChargeSound) |
| 792 | `Item\StygianThrow.ogg` | 物品/武器 | 0.47s | 9.2 | 物品/武器音效；动作：射击/发射；主题：Stygian Throw。 代码标识：ShieldThrowSound。 | Items\Weapons\Melee\StygianShield.cs:22 (ShieldThrowSound) |
| 793 | `Item\StygianThrowLoop.ogg` | 物品/武器 | 0.99s | 14.0 | 物品/武器音效；动作：射击/发射；主题：Stygian Throw Loop。 代码标识：ThrowLoopSound。 | Items\Weapons\Melee\StygianShield.cs:23 (ThrowLoopSound) |
| 796 | `Item\SupernovaBoom.ogg` | 物品/武器 | 0.81s | 10.6 | 物品/武器音效；动作：爆炸/爆裂；主题：Supernova Boom。 代码标识：ExplosionSound。 | Items\Weapons\Rogue\Supernova.cs:17 (ExplosionSound) |
| 797 | `Item\SupernovaStealthCharge.ogg` | 物品/武器 | 2.28s | 44.6 | 物品/武器音效；动作：蓄力/充能/冷却；主题：Supernova Stealth Charge。 代码标识：StealthChargeSound。 | Items\Weapons\Rogue\Supernova.cs:19 (StealthChargeSound) |
| 798 | `Item\SupernovaStealthExplode.ogg` | 物品/武器 | 2.89s | 45.8 | 物品/武器音效；动作：爆炸/爆裂；主题：Supernova Stealth Explode。 代码标识：StealthExplosionSound。 | Items\Weapons\Rogue\Supernova.cs:18 (StealthExplosionSound) |
| 799 | `Item\Swine1.ogg` | 物品/武器 | 0.44s | 19.3 | 物品/武器音效；动作：通用/特殊；主题：Swine。 代码标识：snort。 | Projectiles\Typeless\ScionsCurioMini.cs:148 (snort); Projectiles\Typeless\ScionsCurioMini.cs:167 (snort) |
| 800 | `Item\Swine2.ogg` | 物品/武器 | 0.32s | 16.3 | 物品/武器音效；动作：通用/特殊；主题：Swine。 代码标识：snort。 | Projectiles\Typeless\ScionsCurioMini.cs:148 (snort); Projectiles\Typeless\ScionsCurioMini.cs:167 (snort) |
| 801 | `Item\SwingMid.ogg` | 物品/武器 | 0.52s | 11.7 | 物品/武器音效；动作：近战挥击/撞击；主题：Swing Mid。 代码标识：fire2。 | Projectiles\Melee\BalefulHarvesterHoldout.cs:128 (fire2); Projectiles\Melee\CometQuasherHoldout.cs:139 (fire2); Projectiles\Melee\DevilsSunriseProj.cs:20 (ThrowSound); 另 8 处 |
| 802 | `Item\SwooshMid.ogg` | 物品/武器 | 0.15s | 6.7 | 物品/武器音效；动作：通用/特殊；主题：Swoosh Mid。 代码标识：hardThrow。 | Projectiles\DraedonsArsenal\PulseGrenadeProjectile.cs:130 (hardThrow); Projectiles\Melee\EarthHoldout.cs:188 (swoosh); Projectiles\Rogue\DoomsdayDeviceProjectile.cs:142 (w); 另 6 处 |
| 803 | `Item\SylvestaffFire1.ogg` | 物品/武器 | 0.89s | 21.4 | 物品/武器音效；动作：射击/发射；主题：Sylvestaff Fire。 代码标识：FireSound。 | Items\Weapons\Magic\Sylvestaff.cs:42 (FireSound) |
| 804 | `Item\SylvestaffFire2.ogg` | 物品/武器 | 1.24s | 24.6 | 物品/武器音效；动作：射击/发射；主题：Sylvestaff Fire。 代码标识：FireSound。 | Items\Weapons\Magic\Sylvestaff.cs:42 (FireSound) |
| 805 | `Item\SylvestaffFire3.ogg` | 物品/武器 | 1.10s | 23.0 | 物品/武器音效；动作：射击/发射；主题：Sylvestaff Fire。 代码标识：FireSound。 | Items\Weapons\Magic\Sylvestaff.cs:42 (FireSound) |
| 806 | `Item\SylvestaffProjectileBounce1.ogg` | 物品/武器 | 1.03s | 25.6 | 物品/武器音效；动作：通用/特殊；主题：Sylvestaff Projectile Bounce。 代码标识：BounceSound。 | Items\Weapons\Magic\Sylvestaff.cs:47 (BounceSound) |
| 807 | `Item\SylvestaffProjectileBounce2.ogg` | 物品/武器 | 1.10s | 24.4 | 物品/武器音效；动作：通用/特殊；主题：Sylvestaff Projectile Bounce。 代码标识：BounceSound。 | Items\Weapons\Magic\Sylvestaff.cs:47 (BounceSound) |
| 808 | `Item\SylvestaffProjectileBounce3.ogg` | 物品/武器 | 1.46s | 31.9 | 物品/武器音效；动作：通用/特殊；主题：Sylvestaff Projectile Bounce。 代码标识：BounceSound。 | Items\Weapons\Magic\Sylvestaff.cs:47 (BounceSound) |
| 809 | `Item\TankCannon.ogg` | 物品/武器 | 2.89s | 36.7 | 物品/武器音效；动作：激活/使用/UI；主题：Tank Cannon。 代码标识：UseSound。 | Items\Weapons\Ranged\HandheldTank.cs:15 (UseSound); Items\Weapons\Ranged\RubicoPrime.cs:16 (UseSound) |
| 810 | `Item\TaserLaunch.ogg` | 物品/武器 | 1.00s | 18.5 | 物品/武器音效；动作：射击/发射；主题：Taser Launch。 代码标识：oops。 | Projectiles\Ranged\StarmadaHoldout.cs:219 (oops); Items\Weapons\DraedonsArsenal\ShortCircuit.cs:22 (Fire) |
| 811 | `Item\TearsOfHeavenUse.ogg` | 物品/武器 | 0.92s | 34.9 | 物品/武器音效；动作：激活/使用/UI；主题：Tears Of Heaven Use。 代码标识：UseSound。 | Items\Weapons\Magic\TearsofHeaven.cs:14 (UseSound) |
| 812 | `Item\TerratomereSwing.ogg` | 物品/武器 | 0.69s | 13.5 | 物品/武器音效；动作：近战挥击/撞击；主题：Terratomere Swing。 代码标识：fire。 | Projectiles\Melee\BalefulHarvesterHoldout.cs:126 (fire); Projectiles\Melee\CometQuasherHoldout.cs:137 (fire); Projectiles\Melee\StellarStrikerHoldout.cs:139 (fire); 另 1 处 |
| 813 | `Item\TeslaCannonFire.ogg` | 物品/武器 | 2.40s | 35.6 | 物品/武器音效；动作：射击/发射；主题：Tesla Cannon Fire。 代码标识：FireSound。 | Items\Weapons\DraedonsArsenal\TeslaCannon.cs:20 (FireSound) |
| 814 | `Item\TF2PanHit.ogg` | 物品/武器 | 0.68s | 17.2 | 物品/武器音效；动作：受击/命中/冲击；主题：TF2 Pan Hit。 代码标识：Kunk。 | Projectiles\Melee\FallenPaladinsHammerEcho.cs:15 (Kunk); Projectiles\Melee\GalaxySmasherEcho.cs:20 (Kunk); Projectiles\Melee\PwnagehammerEcho.cs:14 (Kunk); 另 2 处 |
| 815 | `Item\TheHiveNuke.ogg` | 物品/武器 | 1.45s | 15.1 | 物品/武器音效；动作：爆炸/爆裂；主题：The Hive Nuke。 代码标识：fire。 | Projectiles\Ranged\HiveNuke.cs:184 (fire); Projectiles\Ranged\HiveNuke.cs:189 (fire); Projectiles\Typeless\AuricLandMineExplosion.cs:47 (explode2) |
| 823 | `Item\ViperSpit.ogg` | 物品/武器 | 1.04s | 25.8 | 物品/武器音效；动作：通用/特殊；主题：Viper Spit。 代码标识：fire。 | Projectiles\Magic\VitriolicViperHoldout.cs:75 (fire); Projectiles\Magic\VitriolicViperHoldout.cs:119 (fire); Projectiles\Rogue\ReaperProjectile.cs:236 (fire2) |
| 824 | `Item\VividClarityBeamAppear.ogg` | 物品/武器 | 0.53s | 13.8 | 物品/武器音效；动作：激光/光束；主题：Vivid Clarity Beam Appear。 首处代码上下文：AerSigil.cs。 | Projectiles\Magic\AerSigil.cs:46; Projectiles\Melee\NeptunesBountyProjectile.cs:134; Items\Weapons\Magic\Atlantis.cs:35 (UseSound); 另 1 处 |
| 825 | `Item\VividClarityShoot.ogg` | 物品/武器 | 0.77s | 18.5 | 物品/武器音效；动作：射击/发射；主题：Vivid Clarity Shoot。 代码标识：UseSound。 | Items\Weapons\Magic\VividClarity.cs:19 (UseSound) |
| 826 | `Item\VoidDash.ogg` | 物品/武器 | 1.39s | 28.9 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Void Dash。 代码标识：VoidDash。 | Items\Accessories\StatisVoidSash.cs:25 (VoidDash) |
| 827 | `Item\VolterionFire.ogg` | 物品/武器 | 4.65s | 72.0 | 物品/武器音效；动作：射击/发射；主题：Volterion Fire。 代码标识：FireSound。 | Projectiles\Magic\VolterionHoldout.cs:28 (FireSound) |
| 828 | `Item\VolterionOrbShot.ogg` | 物品/武器 | 1.24s | 18.4 | 物品/武器音效；动作：射击/发射；主题：Volterion Orb Shot。 代码标识：FireSound。 | Projectiles\Magic\VolterionOrb.cs:18 (FireSound) |
| 829 | `Item\VulcanRampDown.ogg` | 物品/武器 | 0.90s | 28.1 | 物品/武器音效；动作：通用/特殊；主题：Vulcan Ramp Down。 代码标识：sound。 | Projectiles\DraedonsArsenal\VulcanHoldout.cs:80 (sound) |
| 830 | `Item\VulcanRampUp.ogg` | 物品/武器 | 0.45s | 16.3 | 物品/武器音效；动作：通用/特殊；主题：Vulcan Ramp Up。 代码标识：sound2。 | Projectiles\DraedonsArsenal\VulcanHoldout.cs:87 (sound2); Projectiles\DraedonsArsenal\VulcanSpear.cs:91 (sound2) |
| 831 | `Item\VulcanShot.ogg` | 物品/武器 | 0.31s | 16.7 | 物品/武器音效；动作：射击/发射；主题：Vulcan Shot。 代码标识：sound。 | Projectiles\DraedonsArsenal\VulcanHoldout.cs:195 (sound) |
| 832 | `Item\WaterSplash1.ogg` | 物品/武器 | 1.21s | 17.3 | 物品/武器音效；动作：水体/气泡；主题：Water Splash。 代码标识：transform。 | CalPlayer\CalamityPlayerMiscEffects.cs:1899 (transform); CalPlayer\CalamityPlayerMiscEffects.cs:2013 (max); Projectiles\Typeless\LeviAmberDash.cs:122 (sound); 另 1 处 |
| 833 | `Item\WaterSplash2.ogg` | 物品/武器 | 0.90s | 14.7 | 物品/武器音效；动作：水体/气泡；主题：Water Splash。 代码标识：sound。 | Projectiles\Typeless\LeviAmberDash.cs:122 (sound); Projectiles\Typeless\PuddleSplash.cs:37 (waterSound) |
| 834 | `Item\WeldingBurn.ogg` | 物品/武器 | 0.25s | 6.2 | 物品/武器音效；动作：通用/特殊；主题：Welding Burn。 代码标识：burn。 | Projectiles\Boss\BrimstoneMonster.cs:306 (burn); Projectiles\Magic\IncineratingFireball.cs:158 (Burn); Items\Weapons\Ranged\DragonsBreath.cs:23 (WeldingBurn) |
| 835 | `Item\WeldingShoot.ogg` | 物品/武器 | 0.60s | 9.6 | 物品/武器音效；动作：射击/发射；主题：Welding Shoot。 代码标识：fire2。 | Projectiles\Melee\EarthMeteor.cs:76 (fire2); Items\Weapons\Ranged\DragonsBreath.cs:24 (WeldingShoot) |
| 836 | `Item\WulfrumBlunderbussFire.ogg` | 物品/武器 | 1.76s | 31.1 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Blunderbuss Fire。 代码标识：FireSound。 | Projectiles\Ranged\M1GarandHoldout.cs:46 (FireSound); Items\Weapons\Ranged\WulfrumBlunderbuss.cs:22 (ShootSound) |
| 837 | `Item\WulfrumBlunderbussFireAndReload.ogg` | 物品/武器 | 1.76s | 36.4 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Blunderbuss Fire And Reload。 代码标识：ShootAndReloadSound。 | Items\Weapons\Ranged\Animosity.cs:18 (ShootAndReloadSound); Items\Weapons\Ranged\WulfrumBlunderbuss.cs:23 (ShootAndReloadSound) |
| 838 | `Item\WulfrumKnifeThrowFull.ogg` | 物品/武器 | 0.77s | 14.9 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Knife Throw Full。 代码标识：Throw3Sound。 | Items\Weapons\Rogue\WulfrumKnife.cs:15 (Throw3Sound) |
| 839 | `Item\WulfrumKnifeThrowSingle.ogg` | 物品/武器 | 0.59s | 13.5 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Knife Throw Single。 代码标识：HitSound。 | Projectiles\Rogue\PhantasmalRuinProj.cs:13 (HitSound); Items\Weapons\Rogue\WulfrumKnife.cs:17 (Throw1Sound) |
| 840 | `Item\WulfrumKnifeThrowTwo.ogg` | 物品/武器 | 0.65s | 14.1 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Knife Throw Two。 代码标识：Throw2Sound。 | Items\Weapons\Rogue\WulfrumKnife.cs:16 (Throw2Sound) |
| 841 | `Item\WulfrumKnifeTileHit1.ogg` | 物品/武器 | 0.36s | 7.6 | 物品/武器音效；动作：受击/命中/冲击；主题：Wulfrum Knife Tile Hit。 代码标识：fire。 | Projectiles\Melee\PhotonRipperProjectile.cs:253 (fire); Projectiles\Melee\RespiteblockHoldout.cs:112 (fire); Items\Weapons\Rogue\WulfrumKnife.cs:18 (TileHitSound) |
| 842 | `Item\WulfrumKnifeTileHit2.ogg` | 物品/武器 | 0.38s | 8.0 | 物品/武器音效；动作：受击/命中/冲击；主题：Wulfrum Knife Tile Hit。 代码标识：fire。 | Projectiles\Melee\GrandGuardianHoldout.cs:205 (fire); Projectiles\Melee\PhotonRipperProjectile.cs:253 (fire); Projectiles\Melee\RespiteblockHoldout.cs:112 (fire); 另 7 处 |
| 843 | `Item\WulfrumPing.ogg` | 物品/武器 | 3.96s | 59.3 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Ping。 代码标识：ScanBeepSound。 | Items\Tools\WulfrumTreasurePinger.cs:20 (ScanBeepSound) |
| 844 | `Item\WulfrumPingBreak.ogg` | 物品/武器 | 2.86s | 49.4 | 物品/武器音效；动作：采掘/破碎/物块碰撞；主题：Wulfrum Ping Break。 代码标识：ScanBeepBreakSound。 | Items\Tools\WulfrumTreasurePinger.cs:21 (ScanBeepBreakSound) |
| 845 | `Item\WulfrumPingReady.ogg` | 物品/武器 | 0.39s | 7.1 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Ping Ready。 代码标识：RechargeBeepSound。 | Items\Tools\WulfrumTreasurePinger.cs:22 (RechargeBeepSound) |
| 846 | `Item\WulfrumProsthesisHit.ogg` | 物品/武器 | 1.01s | 17.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Wulfrum Prosthesis Hit。 代码标识：HitSound。 | Items\Weapons\Magic\WulfrumProsthesis.cs:21 (HitSound) |
| 847 | `Item\WulfrumProsthesisShoot.ogg` | 物品/武器 | 1.06s | 21.7 | 物品/武器音效；动作：射击/发射；主题：Wulfrum Prosthesis Shoot。 代码标识：fireSmall。 | Projectiles\Summon\AmphibiansGuitarMinion.cs:99 (fireSmall); Items\Weapons\Magic\WulfrumProsthesis.cs:20 (ShootSound); Items\Armor\Wulfrum\WulfrumFusionCannon.cs:22 (ShootSound) |
| 848 | `Item\WulfrumProsthesisSucc.ogg` | 物品/武器 | 2.27s | 31.4 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Prosthesis Succ。 代码标识：SuckSound。 | Items\Weapons\Magic\WulfrumProsthesis.cs:22 (SuckSound) |
| 849 | `Item\WulfrumProsthesisSuccStop.ogg` | 物品/武器 | 1.43s | 21.4 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Prosthesis Succ Stop。 代码标识：SuckStopSound。 | Items\Weapons\Magic\WulfrumProsthesis.cs:23 (SuckStopSound) |
| 850 | `Item\WulfrumScrewdriverScrewGet.ogg` | 物品/武器 | 1.81s | 37.7 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Screwdriver Screw Get。 代码标识：ScrewGetSound。 | Items\Weapons\Melee\WulfrumScrewdriver.cs:24 (ScrewGetSound) |
| 851 | `Item\WulfrumScrewdriverScrewHit.ogg` | 物品/武器 | 1.15s | 19.4 | 物品/武器音效；动作：受击/命中/冲击；主题：Wulfrum Screwdriver Screw Hit。 代码标识：ScrewHitSound。 | Items\Weapons\Melee\WulfrumScrewdriver.cs:25 (ScrewHitSound) |
| 852 | `Item\WulfrumScrewdriverThrust.ogg` | 物品/武器 | 0.78s | 15.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Wulfrum Screwdriver Thrust。 代码标识：ThrustSound。 | Items\Weapons\Melee\WulfrumScrewdriver.cs:22 (ThrustSound) |
| 853 | `Item\WulfrumScrewdriverThud.ogg` | 物品/武器 | 0.46s | 11.4 | 物品/武器音效；动作：通用/特殊；主题：Wulfrum Screwdriver Thud。 代码标识：w。 | Projectiles\Rogue\PumpkaboomBig.cs:209 (w); Projectiles\Rogue\PumpkaboomSmall.cs:178 (w); Items\Weapons\Melee\WulfrumScrewdriver.cs:23 (ThudSound) |
| 854 | `Item\XykDie.ogg` | 物品/武器 | 1.58s | 53.6 | 物品/武器音效；动作：死亡/击杀；主题：Xyk Die。 代码标识：die。 | Projectiles\Typeless\XykDeathAnim.cs:41 (die) |

### Item\GFBScreams

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 529 | `Item\GFBScreams\Scream1.ogg` | 物品/武器 | 0.39s | 8.0 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 530 | `Item\GFBScreams\Scream2.ogg` | 物品/武器 | 0.81s | 30.0 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 531 | `Item\GFBScreams\Scream3.ogg` | 物品/武器 | 0.55s | 21.7 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 532 | `Item\GFBScreams\Scream4.ogg` | 物品/武器 | 1.55s | 45.4 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 533 | `Item\GFBScreams\Scream5.ogg` | 物品/武器 | 1.45s | 51.6 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 534 | `Item\GFBScreams\Scream6.ogg` | 物品/武器 | 1.61s | 51.7 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 535 | `Item\GFBScreams\Scream7.ogg` | 物品/武器 | 0.76s | 22.8 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |
| 536 | `Item\GFBScreams\Scream8.ogg` | 物品/武器 | 0.61s | 17.8 | 物品/武器音效；动作：吼叫/语音；主题：Scream。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:1204; Projectiles\Melee\GrandDadHoldout.cs:219 (fire3) |

### Item\MittWelding

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 649 | `Item\MittWelding\Weld1.ogg` | 物品/武器 | 1.71s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Weld。 代码标识：beam。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:226 (beam) |
| 650 | `Item\MittWelding\Weld2.ogg` | 物品/武器 | 1.71s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Weld。 代码标识：beam。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:237 (beam) |
| 651 | `Item\MittWelding\Weld3.ogg` | 物品/武器 | 1.71s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Weld。 代码标识：beam。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:248 (beam) |
| 652 | `Item\MittWelding\Weld4.ogg` | 物品/武器 | 1.71s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Weld。 代码标识：beam。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:259 (beam) |
| 653 | `Item\MittWelding\Weld5.ogg` | 物品/武器 | 1.71s | 27.4 | 物品/武器音效；动作：通用/特殊；主题：Weld。 代码标识：beam。 | Projectiles\DraedonsArsenal\CountermeasureMittHoldout.cs:270 (beam) |

### Item\NanoblackReaper

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 659 | `Item\NanoblackReaper\NanoblackReaper_LightspeedMiss.ogg` | 物品/武器 | 2.02s | 24.6 | 物品/武器音效；动作：通用/特殊；主题：Nanoblack Reaper Lightspeed Miss。 代码标识：LightspeedMissSound。 | Projectiles\Rogue\NanoblackMain.cs:29 (LightspeedMissSound) |
| 660 | `Item\NanoblackReaper\NanoblackReaper_LightspeedMissPerfect.ogg` | 物品/武器 | 2.02s | 31.8 | 物品/武器音效；动作：通用/特殊；主题：Nanoblack Reaper Lightspeed Miss Perfect。 代码标识：LightspeedPerfectMissSound。 | Projectiles\Rogue\NanoblackMain.cs:36 (LightspeedPerfectMissSound) |
| 661 | `Item\NanoblackReaper\NanoblackReaper_LightspeedSlash.ogg` | 物品/武器 | 10.09s | 83.6 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Lightspeed Slash。 代码标识：LightspeedSlashBaseSound。 | Projectiles\Rogue\NanoblackMain.cs:43 (LightspeedSlashBaseSound); Projectiles\Rogue\NanoblackMain.cs:50 (LightspeedSlashVariantSound) |
| 662 | `Item\NanoblackReaper\NanoblackReaper_LightspeedSlash1.ogg` | 物品/武器 | 1.09s | 20.4 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Lightspeed Slash。 代码标识：LightspeedSlashBaseSound。 | Projectiles\Rogue\NanoblackMain.cs:43 (LightspeedSlashBaseSound); Projectiles\Rogue\NanoblackMain.cs:50 (LightspeedSlashVariantSound) |
| 663 | `Item\NanoblackReaper\NanoblackReaper_LightspeedSlash2.ogg` | 物品/武器 | 1.09s | 22.5 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Lightspeed Slash。 代码标识：LightspeedSlashBaseSound。 | Projectiles\Rogue\NanoblackMain.cs:43 (LightspeedSlashBaseSound); Projectiles\Rogue\NanoblackMain.cs:50 (LightspeedSlashVariantSound) |
| 664 | `Item\NanoblackReaper\NanoblackReaper_LightspeedSlash3.ogg` | 物品/武器 | 1.23s | 21.8 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Lightspeed Slash。 代码标识：LightspeedSlashBaseSound。 | Projectiles\Rogue\NanoblackMain.cs:43 (LightspeedSlashBaseSound); Projectiles\Rogue\NanoblackMain.cs:50 (LightspeedSlashVariantSound) |
| 665 | `Item\NanoblackReaper\NanoblackReaper_PerfectLightspeedSlash.ogg` | 物品/武器 | 1.50s | 29.1 | 物品/武器音效；动作：近战挥击/撞击；主题：Nanoblack Reaper Perfect Lightspeed Slash。 代码标识：LightspeedPerfectSlashSound。 | Projectiles\Rogue\NanoblackMain.cs:57 (LightspeedPerfectSlashSound) |

### Item\Saxophone

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 729 | `Item\Saxophone\Sax1.ogg` | 物品/武器 | 0.21s | 8.1 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |
| 730 | `Item\Saxophone\Sax2.ogg` | 物品/武器 | 0.21s | 8.2 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |
| 731 | `Item\Saxophone\Sax3.ogg` | 物品/武器 | 0.21s | 7.8 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |
| 732 | `Item\Saxophone\Sax4.ogg` | 物品/武器 | 0.31s | 8.9 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |
| 733 | `Item\Saxophone\Sax5.ogg` | 物品/武器 | 0.25s | 8.6 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |
| 734 | `Item\Saxophone\Sax6.ogg` | 物品/武器 | 0.35s | 10.5 | 物品/武器音效；动作：通用/特殊；主题：Sax。 代码标识：SaxSound。 | Projectiles\Magic\AcidicReed.cs:12 (SaxSound); NPCs\SulphurousSea\BelchingCoral.cs:20 (SAXOPHONE) |

### Item\Summon

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 794 | `Item\Summon\SarosFiring.ogg` | 物品/武器 | 1.10s | 16.4 | 物品/武器音效；动作：射击/发射；主题：Saros Firing。 代码标识：FiringSound。 | Items\Weapons\Summon\SarosPossession.cs:19 (FiringSound) |
| 795 | `Item\Summon\SarosSpawn.ogg` | 物品/武器 | 1.10s | 16.4 | 物品/武器音效；动作：移动/生成/阶段转换；主题：Saros Spawn。 代码标识：SpawnSound。 | Items\Weapons\Summon\SarosPossession.cs:20 (SpawnSound) |

### Item\UnstableCastersGauntlet

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 816 | `Item\UnstableCastersGauntlet\AerSigilGust.ogg` | 物品/武器 | 1.79s | 27.1 | 物品/武器音效；动作：通用/特殊；主题：Aer Sigil Gust。 首处代码上下文：AerSigilMissile.cs。 | Projectiles\Magic\AerSigilMissile.cs:98 |
| 817 | `Item\UnstableCastersGauntlet\AquaSigilExplosion.ogg` | 物品/武器 | 2.07s | 33.8 | 物品/武器音效；动作：爆炸/爆裂；主题：Aqua Sigil Explosion。 首处代码上下文：AquaSigilWaterball.cs。 | Projectiles\Magic\AquaSigilWaterball.cs:106 |
| 818 | `Item\UnstableCastersGauntlet\AquaSigilShot.ogg` | 物品/武器 | 0.77s | 17.1 | 物品/武器音效；动作：射击/发射；主题：Aqua Sigil Shot。 首处代码上下文：AquaSigil.cs。 | Projectiles\Magic\AquaSigil.cs:46 |
| 819 | `Item\UnstableCastersGauntlet\IgnisSigilHit.ogg` | 物品/武器 | 0.94s | 20.6 | 物品/武器音效；动作：受击/命中/冲击；主题：Ignis Sigil Hit。 首处代码上下文：IgnisSigilFireball.cs。 | Projectiles\Magic\IgnisSigilFireball.cs:128 |
| 820 | `Item\UnstableCastersGauntlet\PerditoSigilHit1.ogg` | 物品/武器 | 0.66s | 14.7 | 物品/武器音效；动作：受击/命中/冲击；主题：Perdito Sigil Hit。 首处代码上下文：PerditoSigilShotCreator.cs。 | Projectiles\Magic\PerditoSigilShotCreator.cs:48 |
| 821 | `Item\UnstableCastersGauntlet\PerditoSigilHit2.ogg` | 物品/武器 | 1.01s | 12.9 | 物品/武器音效；动作：受击/命中/冲击；主题：Perdito Sigil Hit。 首处代码上下文：PerditoSigilShotCreator.cs。 | Projectiles\Magic\PerditoSigilShotCreator.cs:45 |
| 822 | `Item\UnstableCastersGauntlet\VisNeedleFire.ogg` | 物品/武器 | 1.31s | 14.7 | 物品/武器音效；动作：射击/发射；主题：Vis Needle Fire。 首处代码上下文：UnstableCastersGauntletHoldout.cs。 | Projectiles\Magic\UnstableCastersGauntletHoldout.cs:145; Projectiles\Magic\WarpSigilShotCreator.cs:49 |

### Music

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 855 | `Music\DraedonExoSelect.ogg` | 音乐 | 1:09.8 | 1168.2 | 音乐或静音占位轨；主题：Draedon Exo Select。 首处代码上下文：DraedonExoSelectMusicScene.cs。 | Scenes\MusicScenes\DraedonExoSelectMusicScene.cs:12; Items\Placeables\MusicBoxes\DraedonExoSelectMusicBox.cs:15 |
| 856 | `Music\DraedonTalk.ogg` | 音乐 | 1:09.8 | 1151.4 | 音乐或静音占位轨；主题：Draedon Talk。 首处代码上下文：DraedonCommunicationMusicScene.cs。 | Scenes\MusicScenes\DraedonCommunicationMusicScene.cs:9; Items\Placeables\MusicBoxes\DraedonTalkMusicBox.cs:14 |
| 857 | `Music\MarniteOrgan.ogg` | 音乐 | 9.96s | 152.9 | 音乐或静音占位轨；主题：Marnite Organ。 代码标识：MarniteOrganSound。 | Tiles\FurnitureMarnite\MarniteOrgan.cs:12 (MarniteOrganSound) |
| 858 | `Music\Silence.ogg` | 音乐 | 2.50s | 3.8 | 音乐或静音占位轨；主题：Silence。 首处代码上下文：MusicEventSystem.cs。 | Systems\Sound\MusicEventSystem.cs:129; Systems\Sound\MusicEventSystem.cs:182; Systems\Sound\MusicEventSystem.cs:200; 另 3 处 |

### NPCHit

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 859 | `NPCHit\AnahitaHit1.ogg` | NPC受击 | 0.31s | 8.8 | NPC/敌怪受击音效；对象/主题：Anahita Hit。 代码标识：HitSound。 | NPCs\Leviathan\Anahita.cs:23 (HitSound) |
| 860 | `NPCHit\AnahitaHit2.ogg` | NPC受击 | 0.22s | 8.4 | NPC/敌怪受击音效；对象/主题：Anahita Hit。 代码标识：HitSound。 | NPCs\Leviathan\Anahita.cs:23 (HitSound) |
| 861 | `NPCHit\AnahitaHit3.ogg` | NPC受击 | 0.24s | 8.5 | NPC/敌怪受击音效；对象/主题：Anahita Hit。 代码标识：HitSound。 | NPCs\Leviathan\Anahita.cs:23 (HitSound) |
| 862 | `NPCHit\AstralEnemyHit1.ogg` | NPC受击 | 0.29s | 6.9 | NPC/敌怪受击音效；对象/主题：Astral Enemy Hit。 代码标识：AstralNPCHitSound。 | Sounds\CommonCalamitySounds.cs:8 (AstralNPCHitSound) |
| 863 | `NPCHit\AstralEnemyHit2.ogg` | NPC受击 | 0.22s | 6.2 | NPC/敌怪受击音效；对象/主题：Astral Enemy Hit。 代码标识：AstralNPCHitSound。 | Sounds\CommonCalamitySounds.cs:8 (AstralNPCHitSound) |
| 864 | `NPCHit\AstralEnemyHit3.ogg` | NPC受击 | 0.28s | 6.8 | NPC/敌怪受击音效；对象/主题：Astral Enemy Hit。 代码标识：AstralNPCHitSound。 | Sounds\CommonCalamitySounds.cs:8 (AstralNPCHitSound) |
| 865 | `NPCHit\AstrumDeusHit1.ogg` | NPC受击 | 0.35s | 9.0 | NPC/敌怪受击音效；对象/主题：Astrum Deus Hit。 代码标识：HitSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:46 (HitSound) |
| 866 | `NPCHit\AstrumDeusHit2.ogg` | NPC受击 | 0.33s | 8.7 | NPC/敌怪受击音效；对象/主题：Astrum Deus Hit。 代码标识：HitSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:46 (HitSound) |
| 867 | `NPCHit\AtlasHurt1.ogg` | NPC受击 | 0.33s | 8.5 | NPC/敌怪受击音效；对象/主题：Atlas Hurt。 代码标识：HurtSound。 | NPCs\Astral\Atlas.cs:51 (HurtSound) |
| 868 | `NPCHit\AtlasHurt2.ogg` | NPC受击 | 0.24s | 7.6 | NPC/敌怪受击音效；对象/主题：Atlas Hurt。 代码标识：HurtSound。 | NPCs\Astral\Atlas.cs:51 (HurtSound) |
| 869 | `NPCHit\AtlasHurt3.ogg` | NPC受击 | 0.27s | 8.5 | NPC/敌怪受击音效；对象/主题：Atlas Hurt。 代码标识：HurtSound。 | NPCs\Astral\Atlas.cs:51 (HurtSound) |
| 870 | `NPCHit\AureusHit1.ogg` | NPC受击 | 0.38s | 11.3 | NPC/敌怪受击音效；对象/主题：Aureus Hit。 代码标识：HitSound。 | NPCs\AstrumAureus\AstrumAureus.cs:41 (HitSound) |
| 871 | `NPCHit\AureusHit2.ogg` | NPC受击 | 0.37s | 10.6 | NPC/敌怪受击音效；对象/主题：Aureus Hit。 代码标识：HitSound。 | NPCs\AstrumAureus\AstrumAureus.cs:41 (HitSound) |
| 872 | `NPCHit\AureusHit3.ogg` | NPC受击 | 0.42s | 11.6 | NPC/敌怪受击音效；对象/主题：Aureus Hit。 代码标识：HitSound。 | NPCs\AstrumAureus\AstrumAureus.cs:41 (HitSound) |
| 873 | `NPCHit\AureusHit4.ogg` | NPC受击 | 0.37s | 10.8 | NPC/敌怪受击音效；对象/主题：Aureus Hit。 代码标识：HitSound。 | NPCs\AstrumAureus\AstrumAureus.cs:41 (HitSound) |
| 874 | `NPCHit\CrabulonHit1.ogg` | NPC受击 | 0.43s | 8.9 | NPC/敌怪受击音效；对象/主题：Crabulon Hit。 代码标识：HitSound。 | NPCs\Crabulon\Crabulon.cs:47 (HitSound) |
| 875 | `NPCHit\CrabulonHit2.ogg` | NPC受击 | 0.43s | 9.3 | NPC/敌怪受击音效；对象/主题：Crabulon Hit。 代码标识：HitSound。 | NPCs\Crabulon\Crabulon.cs:47 (HitSound) |
| 876 | `NPCHit\CrabulonHit3.ogg` | NPC受击 | 0.43s | 9.3 | NPC/敌怪受击音效；对象/主题：Crabulon Hit。 代码标识：HitSound。 | NPCs\Crabulon\Crabulon.cs:47 (HitSound) |
| 877 | `NPCHit\CryogenHit1.ogg` | NPC受击 | 0.58s | 32.5 | NPC/敌怪受击音效；对象/主题：Cryogen Hit。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:3194; Projectiles\Boss\DoGTeleportRift.cs:17 (CrackSound); Projectiles\Magic\FrostBoltProjectile.cs:49; 另 2 处 |
| 878 | `NPCHit\CryogenHit2.ogg` | NPC受击 | 0.58s | 32.9 | NPC/敌怪受击音效；对象/主题：Cryogen Hit。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:3194; Projectiles\Boss\DoGTeleportRift.cs:17 (CrackSound); Projectiles\Magic\FrostBoltProjectile.cs:49; 另 2 处 |
| 879 | `NPCHit\CryogenHit3.ogg` | NPC受击 | 0.58s | 32.8 | NPC/敌怪受击音效；对象/主题：Cryogen Hit。 首处代码上下文：CalamityGlobalNPC.cs。 | NPCs\CalamityGlobalNPC.cs:3194; Projectiles\Boss\DoGTeleportRift.cs:17 (CrackSound); Projectiles\Magic\FrostBoltProjectile.cs:49; 另 2 处 |
| 880 | `NPCHit\CryogenPhaseTransitionCrack.ogg` | NPC受击 | 1.38s | 73.4 | NPC/敌怪受击音效；对象/主题：Cryogen Phase Transition Crack。 代码标识：BreakSound。 | Projectiles\Boss\DoGTeleportRift.cs:19 (BreakSound); Projectiles\Ranged\HailstormBulletProj.cs:77 (crit); Projectiles\Rogue\ValariBoomerang.cs:171 (freeze); 另 1 处 |
| 881 | `NPCHit\DesertScourgeHit1.ogg` | NPC受击 | 0.53s | 13.2 | NPC/敌怪受击音效；对象/主题：Desert Scourge Hit。 代码标识：HitSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:63 (HitSound) |
| 882 | `NPCHit\DesertScourgeHit2.ogg` | NPC受击 | 0.44s | 12.6 | NPC/敌怪受击音效；对象/主题：Desert Scourge Hit。 代码标识：HitSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:63 (HitSound) |
| 883 | `NPCHit\DesertScourgeHit3.ogg` | NPC受击 | 0.41s | 11.8 | NPC/敌怪受击音效；对象/主题：Desert Scourge Hit。 代码标识：HitSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:63 (HitSound) |
| 884 | `NPCHit\ExoHit1.ogg` | NPC受击 | 0.83s | 15.0 | NPC/敌怪受击音效；对象/主题：Exo Hit。 代码标识：ExoHitSound。 | Sounds\CommonCalamitySounds.cs:12 (ExoHitSound); Projectiles\DraedonsArsenal\VulcanSpear.cs:148 (hitTile) |
| 885 | `NPCHit\ExoHit2.ogg` | NPC受击 | 0.79s | 15.7 | NPC/敌怪受击音效；对象/主题：Exo Hit。 代码标识：ExoHitSound。 | Sounds\CommonCalamitySounds.cs:12 (ExoHitSound); Projectiles\DraedonsArsenal\NidhoggHoldout.cs:69 (close); Projectiles\DraedonsArsenal\VulcanSpear.cs:98 (die); 另 1 处 |
| 886 | `NPCHit\ExoHit3.ogg` | NPC受击 | 0.78s | 16.5 | NPC/敌怪受击音效；对象/主题：Exo Hit。 代码标识：sound。 | CalPlayer\CalamityPlayerMiscEffects.cs:1955 (sound); Sounds\CommonCalamitySounds.cs:12 (ExoHitSound); Projectiles\DraedonsArsenal\VulcanSpear.cs:56 (hitTile); 另 2 处 |
| 887 | `NPCHit\ExoHit4.ogg` | NPC受击 | 0.88s | 17.2 | NPC/敌怪受击音效；对象/主题：Exo Hit。 代码标识：ExoHitSound。 | Sounds\CommonCalamitySounds.cs:12 (ExoHitSound); Projectiles\Rogue\ExorcismProj.cs:323 (sound) |
| 888 | `NPCHit\GreatSandSharkHit.ogg` | NPC受击 | 1.04s | 34.8 | NPC/敌怪受击音效；对象/主题：Great Sand Shark Hit。 代码标识：HurtSound。 | NPCs\GreatSandShark\GreatSandShark.cs:28 (HurtSound) |
| 889 | `NPCHit\NuclearTerrorHit.ogg` | NPC受击 | 0.24s | 10.4 | NPC/敌怪受击音效；对象/主题：Nuclear Terror Hit。 代码标识：fire。 | Projectiles\Magic\VitriolicViperHoldout.cs:177 (fire); Projectiles\Magic\VitriolicViperHoldout.cs:190 (fire); Projectiles\Magic\VitriolicViperSpit.cs:73 (fire); 另 2 处 |
| 890 | `NPCHit\OtherworldlyHit.ogg` | NPC受击 | 0.38s | 8.3 | NPC/敌怪受击音效；对象/主题：Otherworldly Hit。 首处代码上下文：ObliteratorYoyo.cs。 | Projectiles\Melee\Yoyos\ObliteratorYoyo.cs:201; NPCs\CeaselessVoid\CeaselessVoid.cs:809; NPCs\DevourerofGods\DevourerofGodsHead.cs:149 (HitSound) |
| 891 | `NPCHit\PerfHiveHit1.ogg` | NPC受击 | 0.37s | 23.8 | NPC/敌怪受击音效；对象/主题：Perf Hive Hit。 代码标识：sound。 | Projectiles\Typeless\RetaliationProjectile.cs:103 (sound); NPCs\Perforator\PerforatorHive.cs:40 (HitSound) |
| 892 | `NPCHit\PerfHiveHit2.ogg` | NPC受击 | 0.37s | 22.9 | NPC/敌怪受击音效；对象/主题：Perf Hive Hit。 代码标识：sound。 | Projectiles\Typeless\RetaliationProjectile.cs:103 (sound); NPCs\Perforator\PerforatorHive.cs:40 (HitSound) |
| 893 | `NPCHit\PerfHiveHit3.ogg` | NPC受击 | 0.37s | 22.6 | NPC/敌怪受击音效；对象/主题：Perf Hive Hit。 代码标识：sound。 | Projectiles\Typeless\RetaliationProjectile.cs:103 (sound); NPCs\Perforator\PerforatorHive.cs:40 (HitSound) |
| 894 | `NPCHit\PerfLargeHit1.ogg` | NPC受击 | 0.42s | 23.1 | NPC/敌怪受击音效；对象/主题：Perf Large Hit。 代码标识：hitSound。 | Projectiles\Magic\VisceraBeam.cs:89 (hitSound); Projectiles\Melee\BladecrestOathswordThrownBlade.cs:221 (unstuck); Projectiles\Melee\DevilsDevastationThrownBlade.cs:193 (unstuck); 另 6 处 |
| 895 | `NPCHit\PerfLargeHit2.ogg` | NPC受击 | 0.42s | 23.0 | NPC/敌怪受击音效；对象/主题：Perf Large Hit。 代码标识：hitSound。 | Projectiles\Magic\VisceraBeam.cs:89 (hitSound); Projectiles\Melee\BladecrestOathswordThrownBlade.cs:221 (unstuck); Projectiles\Melee\DevilsDevastationThrownBlade.cs:193 (unstuck); 另 6 处 |
| 896 | `NPCHit\PerfLargeHit3.ogg` | NPC受击 | 0.42s | 23.6 | NPC/敌怪受击音效；对象/主题：Perf Large Hit。 代码标识：hitSound。 | Projectiles\Magic\VisceraBeam.cs:89 (hitSound); Projectiles\Melee\BladecrestOathswordThrownBlade.cs:221 (unstuck); Projectiles\Melee\DevilsDevastationThrownBlade.cs:193 (unstuck); 另 6 处 |
| 897 | `NPCHit\PerfMediumHit1.ogg` | NPC受击 | 0.44s | 24.0 | NPC/敌怪受击音效；对象/主题：Perf Medium Hit。 代码标识：HitSound。 | NPCs\Perforator\PerforatorBodyMedium.cs:21 (HitSound); NPCs\Perforator\PerforatorHeadMedium.cs:23 (HitSound); NPCs\Perforator\PerforatorTailMedium.cs:21 (HitSound) |
| 898 | `NPCHit\PerfMediumHit2.ogg` | NPC受击 | 0.44s | 23.1 | NPC/敌怪受击音效；对象/主题：Perf Medium Hit。 代码标识：fir2e。 | Projectiles\Melee\InsidiousHarpoon.cs:154 (fir2e); NPCs\Perforator\PerforatorBodyMedium.cs:21 (HitSound); NPCs\Perforator\PerforatorHeadMedium.cs:23 (HitSound); 另 1 处 |
| 899 | `NPCHit\PerfMediumHit3.ogg` | NPC受击 | 0.44s | 24.1 | NPC/敌怪受击音效；对象/主题：Perf Medium Hit。 代码标识：HitSound。 | NPCs\Perforator\PerforatorBodyMedium.cs:21 (HitSound); NPCs\Perforator\PerforatorHeadMedium.cs:23 (HitSound); NPCs\Perforator\PerforatorTailMedium.cs:21 (HitSound) |
| 900 | `NPCHit\PerfSmallHit1.ogg` | NPC受击 | 0.43s | 23.4 | NPC/敌怪受击音效；对象/主题：Perf Small Hit。 代码标识：sound。 | Projectiles\Melee\GreenWater.cs:134 (sound); Projectiles\Rogue\LeviathanTooth.cs:123 (sound); NPCs\Perforator\PerforatorBodySmall.cs:20 (HitSound); 另 2 处 |
| 901 | `NPCHit\PerfSmallHit2.ogg` | NPC受击 | 0.42s | 22.1 | NPC/敌怪受击音效；对象/主题：Perf Small Hit。 代码标识：sound。 | Projectiles\Melee\GreenWater.cs:134 (sound); Projectiles\Rogue\LeviathanTooth.cs:123 (sound); NPCs\Perforator\PerforatorBodySmall.cs:20 (HitSound); 另 2 处 |
| 902 | `NPCHit\PerfSmallHit3.ogg` | NPC受击 | 0.42s | 21.6 | NPC/敌怪受击音效；对象/主题：Perf Small Hit。 代码标识：sound。 | Projectiles\Melee\GreenWater.cs:134 (sound); Projectiles\Rogue\LeviathanTooth.cs:123 (sound); NPCs\Perforator\PerforatorBodySmall.cs:20 (HitSound); 另 3 处 |
| 903 | `NPCHit\PolterghastHit.ogg` | NPC受击 | 0.35s | 21.1 | NPC/敌怪受击音效；对象/主题：Polterghast Hit。 代码标识：HitSound。 | NPCs\Polterghast\Polterghast.cs:56 (HitSound) |
| 904 | `NPCHit\ProvidenceHurt.ogg` | NPC受击 | 0.38s | 11.5 | NPC/敌怪受击音效；对象/主题：Providence Hurt。 代码标识：HurtSound。 | NPCs\Providence\Providence.cs:105 (HurtSound) |
| 905 | `NPCHit\RavagerHurt1.ogg` | NPC受击 | 0.28s | 10.9 | NPC/敌怪受击音效；对象/主题：Ravager Hurt。 代码标识：HitSound。 | NPCs\Ravager\RavagerBody.cs:44 (HitSound) |
| 906 | `NPCHit\RavagerHurt2.ogg` | NPC受击 | 0.24s | 10.6 | NPC/敌怪受击音效；对象/主题：Ravager Hurt。 代码标识：HitSound。 | NPCs\Ravager\RavagerBody.cs:44 (HitSound) |
| 907 | `NPCHit\RavagerHurt3.ogg` | NPC受击 | 0.28s | 12.4 | NPC/敌怪受击音效；对象/主题：Ravager Hurt。 代码标识：crunch2。 | Projectiles\Rogue\LeviathanToothStealth.cs:100 (crunch2); NPCs\Ravager\RavagerBody.cs:44 (HitSound) |
| 908 | `NPCHit\RavagerHurt4.ogg` | NPC受击 | 0.28s | 12.0 | NPC/敌怪受击音效；对象/主题：Ravager Hurt。 代码标识：HitSound。 | NPCs\Ravager\RavagerBody.cs:44 (HitSound) |
| 909 | `NPCHit\RavagerRockPillarHit1.ogg` | NPC受击 | 0.14s | 8.6 | NPC/敌怪受击音效；对象/主题：Ravager Rock Pillar Hit。 代码标识：sound。 | Projectiles\DraedonsArsenal\ImmolationArrow.cs:158 (sound); Projectiles\Melee\BasherHoldout.cs:183 (fire); Projectiles\Rogue\AntumbraShardProjectile.cs:243 (sound); 另 6 处 |
| 910 | `NPCHit\RavagerRockPillarHit2.ogg` | NPC受击 | 0.14s | 9.0 | NPC/敌怪受击音效；对象/主题：Ravager Rock Pillar Hit。 代码标识：fire。 | Projectiles\Melee\BasherHoldout.cs:183 (fire); Projectiles\Rogue\ExorcismProj.cs:119 (sound2); Projectiles\Rogue\ToxicantTwisterProj.cs:120 (fire); 另 3 处 |
| 911 | `NPCHit\RavagerRockPillarHit3.ogg` | NPC受击 | 0.13s | 8.6 | NPC/敌怪受击音效；对象/主题：Ravager Rock Pillar Hit。 代码标识：fire。 | Projectiles\Melee\BasherHoldout.cs:183 (fire); Projectiles\Rogue\ExorcismProj.cs:119 (sound2); Projectiles\Rogue\ToxicantTwisterProj.cs:120 (fire); 另 3 处 |
| 912 | `NPCHit\RimehoundHit.ogg` | NPC受击 | 0.40s | 6.7 | NPC/敌怪受击音效；对象/主题：Rimehound Hit。 代码标识：HitSound。 | NPCs\NormalNPCs\Rimehound.cs:19 (HitSound) |
| 913 | `NPCHit\ScornHurt.ogg` | NPC受击 | 0.33s | 8.7 | NPC/敌怪受击音效；对象/主题：Scorn Hurt。 代码标识：HitSound。 | NPCs\NormalNPCs\ScornEater.cs:19 (HitSound) |
| 914 | `NPCHit\ShieldHit1.ogg` | NPC受击 | 0.21s | 7.7 | NPC/敌怪受击音效；对象/主题：Shield Hit。 代码标识：HurtSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:230 (HurtSound) |
| 915 | `NPCHit\ShieldHit2.ogg` | NPC受击 | 0.20s | 8.0 | NPC/敌怪受击音效；对象/主题：Shield Hit。 代码标识：HurtSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:230 (HurtSound) |
| 916 | `NPCHit\ShieldHit3.ogg` | NPC受击 | 0.22s | 7.2 | NPC/敌怪受击音效；对象/主题：Shield Hit。 代码标识：HurtSound。 | NPCs\SupremeCalamitas\SupremeCalamitas.cs:230 (HurtSound) |
| 917 | `NPCHit\StormlionAltHit.ogg` | NPC受击 | 0.39s | 16.1 | NPC/敌怪受击音效；对象/主题：Stormlion Alt Hit。 代码标识：Hit。 | Effects\StormlionEffects.cs:18 (Hit) |
| 918 | `NPCHit\StormlionHit.ogg` | NPC受击 | 0.52s | 13.8 | NPC/敌怪受击音效；对象/主题：Stormlion Hit。 代码标识：HitSound。 | NPCs\NormalNPCs\Stormlion.cs:16 (HitSound) |
| 919 | `NPCHit\ThanatosHitClosed1.ogg` | NPC受击 | 0.39s | 11.4 | NPC/敌怪受击音效；对象/主题：Thanatos Hit Closed。 代码标识：ThanatosHitSoundClosed。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:41 (ThanatosHitSoundClosed) |
| 920 | `NPCHit\ThanatosHitClosed2.ogg` | NPC受击 | 0.41s | 11.7 | NPC/敌怪受击音效；对象/主题：Thanatos Hit Closed。 代码标识：ThanatosHitSoundClosed。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:41 (ThanatosHitSoundClosed) |
| 921 | `NPCHit\ThanatosHitClosed3.ogg` | NPC受击 | 0.68s | 17.2 | NPC/敌怪受击音效；对象/主题：Thanatos Hit Closed。 代码标识：ThanatosHitSoundClosed。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:41 (ThanatosHitSoundClosed) |
| 922 | `NPCHit\ThanatosHitOpen1.ogg` | NPC受击 | 0.50s | 12.9 | NPC/敌怪受击音效；对象/主题：Thanatos Hit Open。 代码标识：fire。 | Projectiles\Melee\EarthHoldout.cs:283 (fire); Projectiles\Melee\GrandDadHoldout.cs:225 (fire); NPCs\SupremeCalamitas\SupremeCataclysm.cs:413 (fire); 另 2 处 |
| 923 | `NPCHit\ThanatosHitOpen2.ogg` | NPC受击 | 0.51s | 13.0 | NPC/敌怪受击音效；对象/主题：Thanatos Hit Open。 代码标识：ThanatosHitSoundOpen。 | NPCs\ExoMechs\Thanatos\ThanatosHead.cs:39 (ThanatosHitSoundOpen) |
| 924 | `NPCHit\WulfrumHit1.ogg` | NPC受击 | 1.08s | 13.2 | NPC/敌怪受击音效；对象/主题：Wulfrum Hit。 代码标识：Hit。 | NPCs\NormalNPCs\WulfrumAmplifier.cs:20 (Hit) |
| 925 | `NPCHit\WulfrumHit2.ogg` | NPC受击 | 1.08s | 12.7 | NPC/敌怪受击音效；对象/主题：Wulfrum Hit。 代码标识：Hit。 | NPCs\NormalNPCs\WulfrumAmplifier.cs:20 (Hit) |
| 926 | `NPCHit\WulfrumHit3.ogg` | NPC受击 | 1.08s | 12.9 | NPC/敌怪受击音效；对象/主题：Wulfrum Hit。 代码标识：Hit。 | NPCs\NormalNPCs\WulfrumAmplifier.cs:20 (Hit) |
| 927 | `NPCHit\YharonHurt.ogg` | NPC受击 | 0.46s | 8.8 | NPC/敌怪受击音效；对象/主题：Yharon Hurt。 代码标识：HitSound。 | NPCs\Yharon\Yharon.cs:63 (HitSound) |

### NPCKilled

| # | 资源 | 分类 | 时长 | 大小 KB | 是什么 / 用途说明 | 代码引用 |
|---:|---|---|---:|---:|---|---|
| 928 | `NPCKilled\AnahitaDeath.ogg` | NPC死亡 | 1.93s | 33.8 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Anahita Death。 代码标识：DeathSound。 | NPCs\Leviathan\Anahita.cs:24 (DeathSound) |
| 929 | `NPCKilled\AstralEnemyDeath.ogg` | NPC死亡 | 0.62s | 10.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Astral Enemy Death。 代码标识：AstralNPCDeathSound。 | Sounds\CommonCalamitySounds.cs:7 (AstralNPCDeathSound) |
| 930 | `NPCKilled\AstrumDeusDeath.ogg` | NPC死亡 | 2.62s | 44.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Astrum Deus Death。 代码标识：DeathSound。 | NPCs\AstrumDeus\AstrumDeusHead.cs:47 (DeathSound) |
| 931 | `NPCKilled\AtlasDeath.ogg` | NPC死亡 | 1.00s | 17.7 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Atlas Death。 代码标识：DeathSound。 | NPCs\Astral\Atlas.cs:52 (DeathSound) |
| 932 | `NPCKilled\AureusDeath.ogg` | NPC死亡 | 1.93s | 29.2 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Aureus Death。 代码标识：DeathSound。 | NPCs\AstrumAureus\AstrumAureus.cs:42 (DeathSound) |
| 933 | `NPCKilled\CeaselessVoidDeath.ogg` | NPC死亡 | 4.38s | 75.2 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ceaseless Void Death。 代码标识：DeathSound。 | NPCs\CeaselessVoid\CeaselessVoid.cs:36 (DeathSound) |
| 934 | `NPCKilled\CrabulonDeath.ogg` | NPC死亡 | 0.43s | 9.3 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Crabulon Death。 代码标识：DeathSound。 | NPCs\Crabulon\Crabulon.cs:48 (DeathSound) |
| 935 | `NPCKilled\CrownJewelShatter.ogg` | NPC死亡 | 1.15s | 40.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Crown Jewel Shatter。 代码标识：ShatterSound。 | NPCs\NormalNPCs\KingSlimeJewelRuby.cs:21 (ShatterSound); Items\Weapons\Melee\SeekingScorcher.cs:17 (LightShatterSound) |
| 936 | `NPCKilled\CryogenDeath.ogg` | NPC死亡 | 3.35s | 190.3 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Cryogen Death。 代码标识：DeathSound。 | NPCs\Cryogen\Cryogen.cs:52 (DeathSound) |
| 937 | `NPCKilled\CryogenShieldBreak.ogg` | NPC死亡 | 1.38s | 79.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Cryogen Shield Break。 代码标识：BreakSound。 | NPCs\Cryogen\CryogenShield.cs:17 (BreakSound) |
| 938 | `NPCKilled\DesertScourgeDeath.ogg` | NPC死亡 | 2.24s | 39.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Desert Scourge Death。 代码标识：DeathSound。 | NPCs\DesertScourge\DesertScourgeHead.cs:64 (DeathSound) |
| 939 | `NPCKilled\DevourerDeath.ogg` | NPC死亡 | 4.86s | 59.9 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Death。 代码标识：DeathAnimationSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:150 (DeathAnimationSound) |
| 940 | `NPCKilled\DevourerDeathImpact.ogg` | NPC死亡 | 4.12s | 59.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Death Impact。 代码标识：DeathExplosionSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:151 (DeathExplosionSound); CalPlayer\Dashes\GodslayerArmorDash.cs:24 (Impact) |
| 941 | `NPCKilled\DevourerSegmentBreak1.ogg` | NPC死亡 | 0.53s | 13.6 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Segment Break。 首处代码上下文：MawOfInfinityJaws.cs。 | Projectiles\Melee\MawOfInfinityJaws.cs:69; Projectiles\Typeless\CosmicDashExplosion.cs:16 (Impact); NPCs\DevourerofGods\DevourerofGodsHead.cs:152 (DeathSegmentSound) |
| 942 | `NPCKilled\DevourerSegmentBreak2.ogg` | NPC死亡 | 0.37s | 12.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Segment Break。 代码标识：BaroclawHit。 | CalPlayer\CalamityPlayer.cs:484 (BaroclawHit); Projectiles\Rogue\LanceofDestiny.cs:12 (Hitsound); Projectiles\Rogue\RealityRuptureLance.cs:13 (Hitsound); 另 1 处 |
| 943 | `NPCKilled\DevourerSegmentBreak3.ogg` | NPC死亡 | 0.63s | 16.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Segment Break。 代码标识：DeathSegmentSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:152 (DeathSegmentSound) |
| 944 | `NPCKilled\DevourerSegmentBreak4.ogg` | NPC死亡 | 0.41s | 11.7 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Devourer Segment Break。 代码标识：DeathSegmentSound。 | NPCs\DevourerofGods\DevourerofGodsHead.cs:152 (DeathSegmentSound) |
| 945 | `NPCKilled\EidolistDeath.ogg` | NPC死亡 | 0.85s | 14.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Eidolist Death。 代码标识：DeathSound。 | NPCs\NormalNPCs\Eidolist.cs:22 (DeathSound) |
| 946 | `NPCKilled\ExoDeath.ogg` | NPC死亡 | 5.01s | 65.8 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Exo Death。 代码标识：ExoDeathSound。 | Sounds\CommonCalamitySounds.cs:11 (ExoDeathSound) |
| 947 | `NPCKilled\GreatSandSharkDeath.ogg` | NPC死亡 | 2.36s | 70.8 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Great Sand Shark Death。 代码标识：DeathSound。 | NPCs\GreatSandShark\GreatSandShark.cs:29 (DeathSound) |
| 948 | `NPCKilled\Lordeath.ogg` | NPC死亡 | 5.69s | 57.2 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Lordeath。 代码标识：DeathSound。 | NPCs\Other\THELORDE.cs:39 (DeathSound) |
| 949 | `NPCKilled\NuclearTerrorDeath.ogg` | NPC死亡 | 4.09s | 83.2 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Nuclear Terror Death。 代码标识：DeathSound。 | NPCs\AcidRain\NuclearTerror.cs:85 (DeathSound) |
| 950 | `NPCKilled\PerfHiveDeath.ogg` | NPC死亡 | 2.31s | 125.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Perf Hive Death。 代码标识：die2。 | Projectiles\Ranged\SepticSkewerHarpoon.cs:195 (die2); NPCs\Perforator\PerforatorHive.cs:41 (DeathSound) |
| 951 | `NPCKilled\PerfLargeDeath.ogg` | NPC死亡 | 1.38s | 46.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Perf Large Death。 代码标识：hitSound。 | Projectiles\Magic\SanguineFlareProj.cs:95 (hitSound); Projectiles\Magic\VisceraBoom.cs:33 (hitSound); Projectiles\Ranged\EmesisGore.cs:74 (splode); 另 7 处 |
| 952 | `NPCKilled\PerfMediumDeath.ogg` | NPC死亡 | 1.38s | 44.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Perf Medium Death。 代码标识：DeathSound。 | NPCs\Perforator\PerforatorBodyMedium.cs:22 (DeathSound); NPCs\Perforator\PerforatorHeadMedium.cs:24 (DeathSound); NPCs\Perforator\PerforatorTailMedium.cs:22 (DeathSound) |
| 953 | `NPCKilled\PerfSmallDeath.ogg` | NPC死亡 | 1.15s | 35.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Perf Small Death。 代码标识：crunch。 | Projectiles\Rogue\LeviathanToothStealth.cs:97 (crunch); NPCs\Perforator\PerforatorBodySmall.cs:21 (DeathSound); NPCs\Perforator\PerforatorHeadSmall.cs:24 (DeathSound); 另 1 处 |
| 954 | `NPCKilled\PrimordialWyrmDeath.ogg` | NPC死亡 | 6.77s | 45.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Primordial Wyrm Death。 代码标识：DeathSound。 | NPCs\PrimordialWyrm\PrimordialWyrmHead.cs:86 (DeathSound) |
| 955 | `NPCKilled\RavagerDeath1.ogg` | NPC死亡 | 4.41s | 79.8 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Death。 代码标识：DeathSound。 | NPCs\Ravager\RavagerBody.cs:45 (DeathSound); NPCs\SupremeCalamitas\SupremeCalamitas.cs:3438 |
| 956 | `NPCKilled\RavagerDeath2.ogg` | NPC死亡 | 4.06s | 69.6 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Death。 代码标识：DeathSound。 | NPCs\Ravager\RavagerBody.cs:45 (DeathSound); NPCs\SupremeCalamitas\SupremeCalamitas.cs:3439 |
| 957 | `NPCKilled\RavagerLimbLoss1.ogg` | NPC死亡 | 1.94s | 44.6 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Limb Loss。 代码标识：LimbLossSound。 | NPCs\Ravager\RavagerBody.cs:43 (LimbLossSound) |
| 958 | `NPCKilled\RavagerLimbLoss2.ogg` | NPC死亡 | 2.25s | 50.5 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Limb Loss。 代码标识：LimbLossSound。 | NPCs\Ravager\RavagerBody.cs:43 (LimbLossSound); NPCs\SupremeCalamitas\SupremeCataclysm.cs:217 (yell); NPCs\SupremeCalamitas\SupremeCatastrophe.cs:212 (yell) |
| 959 | `NPCKilled\RavagerLimbLoss3.ogg` | NPC死亡 | 2.26s | 58.1 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Limb Loss。 代码标识：LimbLossSound。 | NPCs\Ravager\RavagerBody.cs:43 (LimbLossSound); NPCs\SupremeCalamitas\SupremeCataclysm.cs:565 (respawn); NPCs\SupremeCalamitas\SupremeCatastrophe.cs:601 (respawn) |
| 960 | `NPCKilled\RavagerLimbLoss4.ogg` | NPC死亡 | 2.21s | 53.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Ravager Limb Loss。 代码标识：LimbLossSound。 | NPCs\Ravager\RavagerBody.cs:43 (LimbLossSound) |
| 961 | `NPCKilled\ScornDeath.ogg` | NPC死亡 | 0.80s | 16.0 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Scorn Death。 代码标识：DeathSound。 | NPCs\NormalNPCs\ScornEater.cs:20 (DeathSound) |
| 962 | `NPCKilled\SepulcherDeath.ogg` | NPC死亡 | 4.45s | 71.5 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Sepulcher Death。 代码标识：DeathSound。 | NPCs\SupremeCalamitas\SepulcherHead.cs:20 (DeathSound) |
| 963 | `NPCKilled\StormlionAltDeath.ogg` | NPC死亡 | 0.62s | 20.7 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Stormlion Alt Death。 代码标识：Killed。 | Effects\StormlionEffects.cs:19 (Killed) |
| 964 | `NPCKilled\StormlionDeath.ogg` | NPC死亡 | 1.24s | 24.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Stormlion Death。 代码标识：DeathSound。 | NPCs\NormalNPCs\Stormlion.cs:17 (DeathSound) |
| 965 | `NPCKilled\Sunskater.ogg` | NPC死亡 | 0.51s | 7.8 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Sunskater。 代码标识：DeathSound。 | NPCs\NormalNPCs\Sunskater.cs:19 (DeathSound) |
| 966 | `NPCKilled\WeaverDeath.ogg` | NPC死亡 | 1.18s | 19.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Weaver Death。 代码标识：DeathSound。 | NPCs\StormWeaver\StormWeaverHead.cs:55 (DeathSound) |
| 967 | `NPCKilled\WulfrumDeath.ogg` | NPC死亡 | 1.49s | 25.5 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Wulfrum Death。 代码标识：WulfrumNPCDeathSound。 | Sounds\CommonCalamitySounds.cs:35 (WulfrumNPCDeathSound) |
| 968 | `NPCKilled\YharonDeath.ogg` | NPC死亡 | 3.77s | 45.4 | NPC/敌怪死亡、破碎或部位损毁音效；对象/主题：Yharon Death。 代码标识：DeathSound。 | NPCs\Yharon\Yharon.cs:64 (DeathSound) |
