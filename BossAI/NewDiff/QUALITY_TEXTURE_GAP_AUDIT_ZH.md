# NewDiff vs 炼狱模式(Infernum)质感差距自检报告

生成日期:2026-07-14

## 实施记录(2026-07-14)

本报告第五节"建议优先级"里第 3、4、5 条已经动手实施,逐条编译验证通过(`dotnet build` 0 error):

- **框架层新增摄像机聚焦系统**:`Core/Systems/IUMWCameraFocusPlayer.cs`,提供 `player.IUMWCamera().RequestFocus(worldPos, strength, holdFrames)` API(rise/hold/fall 三段插值,与玩家视角混合而非硬切)。
- **`IUMWBossAI` 基类新增死亡演出公共骨架**:`InterceptLethalHit`(致命伤截断+锁1HP+回调)与 `TriggerDeathCinematic`(镜头拉近+震动),供各 boss 挂自己的主题化演出。
- **18 个 boss 的死亡演出补齐到全部完成**(原来只有 5 个):OldDuke、Polterghast、StormWeaver、CeaselessVoid、Signus、Providence、AstrumDeus、AstrumAureus、PlaguebringerGoliath、AquaticScourge、LeviathanAnahita(利维坦与阿纳西塔独立死亡,互不干扰对方状态)、Ravager、Dragonfolly 各自拿到 5 段式、贴合自身身份的死亡表演,而不是共用一套通用爆炸。
- **6 个"半成品级"boss 补强到 Cryogen 基线**:AquaticScourge(新增硫磺海激怒机制、专属音效字段、残影拖尾、脓疮破裂震动)、CeaselessVoid(残影拖尾、音效字段、护盾破碎震动)、StormWeaver(天空激怒机制、专属音效字段、残影拖尾)、AstrumDeus(每个头独立的残影拖尾、专属音效字段)、AstrumAureus/Ravager(音效 Pitch 变化密度提升)。

覆盖率缺口(第一节,28 个未触碰的 boss)和框架层更完整的过场/Cutscene 系统(第三节里的"完整版")本轮未动,仍是后续如果要继续追平炼狱模式的主要方向。

---
范围:仅基于代码结构与静态审计,未做游戏内实测(手感、节奏、难度曲线等仍需实机验证)。
方法:未反编译/照搬 Infernum 代码,只统计其公开源码中的架构模式(类名、调用方式、数量),不摘录具体弹幕数值或美术资源。

---

## 结论先行

朋友说不出具体问题,是因为差距**不在单点技术**上——你已经证明自己有能力做到、甚至超过炼狱水准(Cryogen 一个 boss 的代码量是炼狱本体 Cryogen 的 3 倍)。真正拉低整体观感的是三件事,按影响力排序:

1. **覆盖率**:炼狱对全游戏 45 个 boss 全部重写,NewDiff 目前只注册了 18 个,其中和炼狱重合的只有 17 个,占炼狱重写总数的 **38%**。玩家在一轮通关里,大部分时间打的其实是没改过的原版灾厄 boss。
2. **18 个已做的 boss 之间质感参差不齐**:存在明显的两级——以 Cryogen/Signus/Polterghast 为代表的"精工级",和以 AquaticScourge/CeaselessVoid/StormWeaver 为代表的"半成品级"。同一个玩家连续打两场,体验会突然掉线。
3. **项目级"运镜"能力完全缺失**:炼狱有屏幕震动之外的摄像机聚焦/推近/强制过场系统,NewDiff 目前全项目搜索为零(`ScreenFocus`/`CameraPan`/`ZoomSystem`/`CutsceneManager` 均无匹配)。这个是单个 boss 作者补不了的,得先在框架层补一次。

下面展开这三点,并给出一个不涉及抄袭、可执行的优先级建议。

---

## 一、覆盖率缺口(最大的量级问题)

`Common/IUMWBossAIRegistry.cs` 里注册的专属 AI 一共 18 个:

Yharon、OldDuke、Polterghast、StormWeaver、CeaselessVoid、Signus、Providence、AstrumDeus、PlaguebringerGoliath、AstrumAureus、Cryogen、AquaticScourge、HiveMind、Perforators、CalamitasClone、LeviathanAnahita、Ravager、Dragonfolly。

炼狱模式 `Content/BehaviorOverrides/BossAIs/` 下有 **45 个** boss 文件夹,逐一重写了从史莱姆王、克苏鲁之眼到灾厄艾比斯(SupremeCalamitas)、灾厄机神(Draedon)的几乎全部战斗。其中 Ravager 炼狱并未重写(NewDiff 做了自己的 Ravager,这点是净赚)。

两边取交集后,NewDiff 只覆盖了炼狱 45 个重写目标里的 **17 个(38%)**。以下 **28 个** boss 在 NewDiff 里目前打的仍是**原版灾厄 AI**,没有任何专属重制:

> AdultEidolonWyrm、Brain of Cthulhu、BrimstoneElemental、Crabulon、Cultist、Deerclops、DesertScourge、The Destroyer、Devourer of Gods、Draedon(及 Ares/Thanatos/Apollo&Artemis)、Dreadnautilus、DukeFishron、Empress of Light、Eater of Worlds、Eye of Cthulhu、Golem、GreatSandShark、King Slime、Moon Lord、Plantera、Skeletron Prime、Profaned Guardians、Queen Bee、Queen Slime、Skeletron、SlimeGod、Supreme Calamitas、The Twins、Wall of Flesh。

**为什么这一条最致命**:质感是连续体验的产物。哪怕 Cryogen 做到超越炼狱的精细度,只要下一场 Boss(比如 Golem 或者 Twins)还是原版 AI,玩家立刻会从"这是一个统一的高完成度硬核模式"跳回"这是灾厄本体",落差感会覆盖掉前一场战斗攒下的好印象。这也是"说不出具体哪里差,但整体就是感觉差很多"这种反馈的典型成因——单点拷打不出问题,因为问题出在**结构性的空白**上,不在某句代码。

这份差距也不是从头造轮子:`大计划/0628炼狱分析/` 目录下已经有全部 48 个 boss(含未被炼狱重写的)的深度分析报告,相当于设计蓝图已经打好了,缺的是把它们变成像 Cryogen 那样的实现。

---

## 二、18 个已覆盖 boss 内部的两级分化

对 18 个 boss 的 AI 主文件做了逐项特征扫描(变体系统、telegraph 传送、专属弹幕/持握武器文件、屏幕震动、地图/生物群系挂钩的激怒机制、死亡演出、音效多样性、残影拖尾、设计注释密度),结果非常清楚地分成两层:

### 精工级(达到或超过 Cryogen 标杆)
**Signus、Polterghast、CalamitasClone** —— 都有和 Cryogen 同款的可复用"传送预警"辅助方法(`blinkTimer`/`UpdateBlink`),不是每次攻击临时糊一段;音效变化(6~13 处 Pitch 变化)、残影拖尾、双语设计注释一应俱全。
**OldDuke、Providence** —— 音效打磨甚至是全组最重(19、14 处 Pitch 变化),中文设计注释密度也很高,只是传送预警是每招手写而非抽出公共方法,也都还没有死亡演出/绝境阶段。
**PlaguebringerGoliath、LeviathanAnahita** —— 独立实现了和 Cryogen 一模一样的"离开生物群系则激怒"套路(`outOfBiomeTimer` + Lerp 速度爬升),明显是照着同一套设计规范做的,只是换了丛林/海洋皮。

### 半成品级(明显偷工减料,建议优先补)
- **AquaticScourge**(605 行,全组最短):全文件零屏幕震动、零残影拖尾、音效单薄(没有专属 SoundStyle 字段)、注释密度全组最低。
- **CeaselessVoid**:注释密度全组最低(1.7%)、无残影、无死亡/绝境状态、音效几乎没有 Pitch 变化。
- **StormWeaver**:零专属音效设计(没有 SoundStyle 字段,没有任何 Pitch 变化)、无残影拖尾。
- **AstrumDeus**:和 StormWeaver 同样的音效空白,且无地图激怒、无死亡演出。
- **AstrumAureus、Ravager**:有变体系统和残影拖尾,但音效变化只有 1~2 处,注释密度不到 3%,没有设计动机说明。

**HiveMind、Perforators** 是特例:结构上明显是更早期的架构(唯一没有独立 Projectiles/HeldWeapons 文件、复用灾厄原版弹幕类型,而不是自制视觉),但战斗逻辑本身不算敷衍——它们其实有死亡演出/绝境状态,传送预警也是手写但确实存在。问题更像是"没升级到新架构",而不是"没做"。

**另一个附带发现**:18 个 boss 里,只有 **Yharon、Cryogen、HiveMind、Perforators、CalamitasClone**(5/18)做了死亡拦截+专属死亡演出(`InterceptLethalHit` + `DeathAnimation`)。炼狱几乎给每个 boss 都做了这个(至少 17 个文件遵循同一套"击杀那一下不直接杀死、而是切入演出状态"的命名规范)。这是一个成本不高、但很容易被玩家记住的"临门一脚"缺口。

---

## 三、框架层完全空缺的"运镜"能力

这条不是某个 boss 作者的锅,是项目公共层(`Core/Systems`、`Common/`)目前没有对应的挂钩:

- 全项目搜索 `ScreenFocus`、`CameraPan`、`ZoomSystem`、`CutsceneManager`、`BlockerSystem`,**零匹配**。
- 现有的屏幕震动是每个 boss 直接对 `player.Calamity().GeneralScreenShakePower` 赋值(灾厄本体自带的字段,不是自造系统),没有强弱叠加规则,纯粹"谁写的值大听谁的"。
- 炼狱这边(仅描述能力,不摘代码):除了同样基于这个字段做震动外,额外有一层独立的镜头系统——可以把屏幕临时"拉"向某个焦点并停留几帧,再插值拉回玩家;召唤仪式/阶段转换/击杀后可以触发真正的过场锁定(强制观战视角+黑边或类似遮罩),用在 Providence 的召唤祭坛、DoG 击败后接 Providence 的衔接过场等场合。

**这个缺口值得单独拎出来的原因**:即便某个 boss 的攻击设计已经做到 Cryogen 水准,没有镜头语言,"这一下很重要"这种信息也只能靠震动强度和音量堆,观感天花板比炼狱低一截。这是少数几个**值得投入做成公共框架**、而不是逐 boss 补的点——做一次,18 个甚至以后 45 个 boss 都能用。

---

## 四、值得肯定、不要被"全方位不如"带偏的地方

自检不是只找茬。几个 NewDiff 已经做对、甚至比炼狱思路更好的地方:

- **确定性变体轮换**(`UseVariantB`/`attackVariant[]`):同一招式在每次轮到时确定性地在 A/B 两种空间读法间切换,不用 RNG 就能保证花样,还天然不会联机同步出问题。这个思路在审计中没有在炼狱侧看到对应实现,是 NewDiff 自己的巧思。
- **中文设计动机注释**(Cryogen 11.1%、OldDuke/Signus/Polterghast 均有大段"为什么这么设计"的说明):炼狱源码里几乎没有这类注释,纯代码,不写"为什么"。这份文档化程度对后续维护和让协作者理解设计意图,反而是 NewDiff 更强的地方。
- Cryogen 单文件复杂度(2733 行主 AI + 独立弹幕/武器文件共 5431 行)已经是炼狱同名 boss(1794 行)的 3 倍,证明"做到那个质感"这件事本身不存在能力上限问题。

---

## 五、建议的优先级(不涉及照搬炼狱代码/资源)

1. **先做决策,不是先动手**:是把 NewDiff 的目标范围明确收窄到"这 18 个 boss 做到极致,其余保持原版",并在游戏内明确告知玩家范围;还是继续扩展覆盖率追平炼狱的 45 个。两条路径都合理,但目前处于"看起来想全做、实际只做了 38%"的中间状态,这本身就是玩家体感"什么都差一点"的来源之一。
2. 如果选择扩展覆盖率,**优先级应该看杀伤力而不是开发顺序**:从`大计划/0628炼狱分析/`里已有蓝图、且是主线关键节点的 boss 开始(比如 Plantera、Golem、Twins、Skeletron Prime、Moon Lord 这类几乎每个玩家都会打的),而不是从冷门收藏向 boss(比如 GreatSandShark、AdultEidolonWyrm)开始。
3. **把 6 个"半成品级"补到 Cryogen 基线**:AquaticScourge、CeaselessVoid、StormWeaver、AstrumDeus、AstrumAureus、Ravager。具体缺什么这份报告第二节已经列清楚(专属音效字段+Pitch 变化、残影拖尾数组、离开区域的激怒机制、死亡演出),工作量是"照抄自己已有的 Cryogen 套路搬过去改皮",不是从零设计。
4. **把死亡演出补齐到全部 18 个 boss**:`InterceptLethalHit` + `DeathAnimation` 这套模式已经在 Cryogen/HiveMind/Perforators/CalamitasClone/Yharon 跑通,复制这个模式本身的成本远低于原创设计。
5. **框架层建一次摄像机聚焦/短暂过场的公共系统**(哪怕只是"屏幕插值拉向某点再弹回"这种最基础版本),挂到 `IUMWGlobalNPC`/`Core/Systems` 里,做一次全部 boss 受益。不需要做到炼狱的完整 Cutscene 队列那么重,先解决"没有"的问题。
