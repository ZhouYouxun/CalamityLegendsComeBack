# CalamityEntropy 特效审计与可迁移亮点

审计对象：本机 `CalamityEntropy` 源码；对照 `灾厄2.1武器重做列表.md`、`灾厄2.1特效拆解_总索引与进度.md` 及其第 0～5 篇拆解文档。

审计口径：只把“明确高于灾厄 2.1 已重做武器的高位表现”或“机制/呈现方式确实新颖”的内容列入推荐。普通的贴图拖尾、加法混合、随机 Dust、单纯换色和灾厄现成 Shader 的直接套用不算原创亮点。由于本次是源码审计，没有把运行时截图观感当作已证实事实。

## 结论先行

- CE 确实大量使用灾厄底层：`PrimitiveRenderer`、`Metaball`、`GeneralParticleHandler/PRTLoader`、灾厄 Misc Shader 和灾厄粒子贴图。
- CE 同时拥有自己的粒子系统与约 62 个 `Content/Particles/*.cs` 粒子/粒子相关类，以及 61 个本地 `.fx` 文件。因而不能把它归类为“灾厄特效搬运项目”。
- 可确认的 CE 原创技术量很大，但“原创”不等于“高于灾厄 2.1”。其中相当一部分只是常规透明变换、描边、扭曲、拖尾或 UI 后处理，建议筛掉。
- 最值得进入 CE 规划的不是单个 Shader 名称，而是以下几类组合：**自定义 Metaball 融合、完整屏幕后处理/天空场景、复杂剑刃拖尾 Shader 组、带状态/资源反馈的武器视觉、以及把灾厄 PrimitiveRenderer 与 CE 自有纹理/粒子组合成新的视觉语法**。

## 一、明确使用的灾厄特效底座

### 1. 灾厄 Misc Shader

源码直接调用的灾厄 Shader 包括：

| 灾厄 Shader | CE 用法 | 判断 |
|---|---|---|
| `CalamityMod:ArtAttack` | 多数 CE 武器、粒子和风/火/剑气拖尾；配 CE 自有 `Streak1/2/Goop/Solid` 等纹理 | 借用灾厄图元带管线；纹理和组合方式可原创，但 Shader 本体不是原创 |
| `CalamityMod:TrailStreak` | `Trail.cs`、`DivineRadienceBullet`、`Prophet/RuneSword`、`ZyphrosCrystal` 等 | 借用灾厄拖尾底座 |
| `CalamityMod:HeavenlyGaleLightningArc` | `Lightning.cs`、`SpiritLaser.cs`；使用 Perlin 噪声 | 借用灾厄闪电 Shader；闪电折线控制和颜色/时序可算 CE 组合设计 |
| `CalamityMod:ExobladePierce` | `ControlTerminal.cs` 的双层图元带 | 借用灾厄高端剑气 Shader，双层宽度/颜色组合是 CE 侧设计 |
| `ForceField` | Ratziel 与通用系统 | 原版/灾厄现成力场效果，不列为原创 |
| `CalamityMod` 的 `BasicTrail`、`SylvestaffStreak`、`ScarletDevilStreak` 等贴图 | 多个 CE 拖尾直接引用 | 资源复用，不列为原创 |

### 2. 灾厄粒子、粒子框架和 Metaball

- `PRTLoader` / `PRT_Spark`：用于 CE 挥砍命中反馈和光粒子；粒子类本体来自灾厄框架。
- `CalamityMod/Particles/HeavySmoke`：`EHeavySmoke` 直接请求灾厄粒子贴图。
- `CalamityMod.Graphics.Metaballs.Metaball`：`EclipseMetaball` 与 `ShadowMetaball` 继承灾厄 Metaball 基类；但具体粒子列表、更新和材质组合是 CE 自己实现的。
- 灾厄 `PrimitiveRenderer` 与 `PrimitiveSettings`：CE 大量用它绘制图元带。这是灾厄提供的绘制系统，不应在 CE 文案中称作原创引擎。

## 二、CE 自有粒子特效

`Content/Particles` 下有约 62 个 C# 类，至少包含以下明确的 CE 自有粒子/粒子组合：

- 亮点/能量：`GlowSpark`、`GlowLightParticle`、`EGlowOrb`、`ShineParticle`、`PRT_Light`、`LightAlt`、`ImpactParticle`、`PremultBurst`。
- 线/环/拖尾：`AbyssalLine`、`ELineParticle`、`LineParticle`、`HadLine`、`HadCircle`、`ERing`、`Trail`、`TrailSpark`、`StarTrail`、`ProminenceTrail`、`DashBeam`。
- 主题粒子：`RuneParticle`、`SakuraPetalsParticle`、`LifeLeaf`、`HeavenfallStar`、`CrystalGlow`、`PrismShardParticle`、`DarkBladeParticle`、`BlackKnifeSlash`。
- 烟尘/爆炸/碎片：`EHeavySmoke`、`MediumSmoke`、`Smoke`、`EXPLOSION`、`realisticexplosion`、`SnowPiece`、`ShellParticle`、`VoidImpactParticle`、`VoidParticles`。
- 状态/特殊：`ShadeDashParticle`、`ShadeCloakOrb`、`PortalParticle`、`WindParticle`、`HealingParticle`、`MCodeParticle`、`CruiserWarn`、`APRCAlarm`。

其中 `WindParticle` 仍调用灾厄 `ArtAttack`，`Trail` 调用灾厄 `TrailStreak`；所以“粒子类是 CE 自有”与“底层 Shader 是 CE 自有”必须分开写。

## 三、CE 自有着色器

`Assets/Effects` 下可确认有 61 个本地 `.fx` 文件。按用途筛选如下：

### 值得保留、可能形成 CE 差异化的组

- **剑刃/斩击组**：`KnifeRendering`、`SwordTrail`、`SwordTrail2`～`SwordTrail5`、`SlashTrans`、`SlashTrans2`。配合 `BaseSwing` 的历史角度/长度采样和 `TriangleStrip`，这是 CE 最完整的自有近战拖尾体系之一。能否高于灾厄 2.1，要看具体武器是否有更好的时序、宽度曲线和命中反馈；仅有 Shader 名称不足以判定。
- **Metaball/虚空融合组**：`ShadowMetaball`、`EclipseMetaball` 配合 `cvoid`、`cvoid2`、`cvoid3`、`cabyss`、`cblood`、`AntivoidTrail`。这类“融合成团、流动、吸积、侵蚀”的表现，比普通粒子堆叠更接近灾厄 2.1 的高端视觉语言，具备迁移价值。
- **屏幕后处理/场景组**：`fscreen`、`fscreenCr`、`kscreen`、`kscreen2`、`HeatDeath`、`AWSkyEffect`、`awsky2`、`ColorLerp2`、`PolarDistortShader`。如果配合完整状态机使用，可形成 CE 自有的场景级视觉；但必须避免整屏闪烁、过度色偏和遮挡战斗信息。
- **状态/材质变换组**：`ShadeDashParticle`、`Vortex`、`WarpShader`、`DisplacemenShaderP`、`RTShader`、`FinalFrac`。它们适合做位移、漩涡、实时纹理/扭曲和形态变化；其中 `FinalFrac`、`Vortex` 若与 Fractal/Oblivion 等武器状态绑定，创新度明显高于“单纯发光拖尾”。
- **特殊组合组**：`Prominence`、`fableeyelaser`、`Fire`、`Cylinder`、`NihShield`。它们分别对应高压拖尾、眼部激光、火焰材质、圆柱束/柱状特效、护盾；需要按实际武器表现筛选，不能一概推荐。

### 建议直接筛掉的普通项

`WhiteTrans`、`RedTrans`、`Trans`、`RedAdd`、`ColorLerp`、`ColorLerp2`、`Outline`、`blur`、`Pixel`、`Transform`～`Transform3`、`NameEffect`、多数染色 Shader 和普通 UI/物品描边 Shader，主要是通用材质变换或辅助层。它们有用，但没有达到“高于灾厄 2.1 高位特效”的独立推荐门槛。

## 四、目前最有资格称为“新颖/值得 CE 借鉴”的内容

### A. `BaseSwing` 的剑刃历史采样 + 自有拖尾 Shader

`Core/BaseSwing.cs` 不只是画一条 oldPos 拖尾：它保存历史旋转角、距离、长度，按剑刃上下边缘构造三角带，再以 `KnifeRendering` 绘制并叠加 Additive pass。这个结构适合实现“剑身真实扫过空间”的拖尾，而不是一条跟在弹体后的线。若武器同时拥有清楚的三段式动作、剑尖采样、边缘高光和命中反馈，可列为 CE 的高价值原创组合。

### B. CE 自有 Metaball 的虚空/暗影融合

`ShadowMetaball` 与 `EclipseMetaball` 继承灾厄 Metaball 框架，但自己维护粒子、衰减和绘制纹理。它能表达“多个粒子互相融合成一个有体积的暗团”，层级上高于普通 Dust/GlowSpark 堆叠；这类效果可与 2.1 拆解中血球、黑洞、吸积场的设计对标。

### C. 状态驱动的屏幕级反馈

`CrSky` 使用 RenderTarget 与本地天空/屏幕后处理 Shader，并混入灾厄 `ArtAttack` 图元带。若绑定到明确的阶段、资源或 Boss 状态，属于场景级反馈，创意和表现上有机会超过普通武器粒子；若只是常驻背景滤镜，则不应当当作武器特效移植。

### D. 自有 Shader 与灾厄 Shader 的混合，而不是盲目替换

例如 `ControlTerminal` 使用灾厄 `ExobladePierce`，但以两套宽度、颜色和轨迹绘制构成主/辅拖尾；`NightStar`、`NetherRiftBlade` 等也使用多条不同纹理拖尾。这种“灾厄成熟底座 + CE 自有纹理/时序/第二层结构”的组合是最现实、最容易达到高位质量的路线。

## 五、按灾厄 2.1 高位标准的筛选结果

### 纳入候选

1. `BaseSwing` + `KnifeRendering` / `SwordTrail*` 的完整近战扫击体系。
2. `ShadowMetaball` / `EclipseMetaball` 的融合型虚空、暗影和侵蚀表现。
3. `Vortex` / `WarpShader` / `PolarDistortShader` 与明确武器状态绑定的奇点、扭曲、吸积效果。
4. `CrSky` 的 RenderTarget + 天空/屏幕后处理组合，限于 Boss/阶段级视觉。
5. `ControlTerminal`、`NightStar`、`NetherRiftBlade` 一类“多层图元带 + 不同材质/颜色/宽度”的组合，前提是保留清晰的运动结构。

### 只作为素材或实现参考，不单独纳入

- 单个 `GlowSpark`、`LightAlt`、`Smoke`、`MediumSmoke`、`LineParticle`。
- 单纯的 Additive 双绘制、颜色渐变、透明度变化、描边、普通屏幕闪白。
- 直接调用灾厄 `ArtAttack` / `TrailStreak` / `HeavenlyGaleLightningArc` 的普通拖尾。
- 仅替换灾厄贴图、颜色或粒子数量，没有新的时序、空间结构或状态反馈的武器。

## 六、最终判断

CE 有原创粒子，有原创着色器，也有少量具备新颖性的组合；但它的“原创量”远大于“已证明高于灾厄 2.1 高位”的量。当前最可靠的 CE 差异化资产是：**剑刃历史几何拖尾、融合型 Metaball、状态绑定的扭曲/屏幕后处理，以及灾厄图元带与 CE 自有纹理的多层组合**。

后续若要把候选写进某把武器规划，必须再做一次逐武器核验：确认具体武器的释放时序、运动轨迹、粒子密度、屏幕遮挡和命中反馈；不能仅凭“用了本地 `.fx`”就判定它比灾厄 2.1 更好。

## 主要源码索引

- `CalamityEntropy/Core/BaseSwing.cs`
- `CalamityEntropy/Common/EffectLoader.cs`
- `CalamityEntropy/Content/Particles/`
- `CalamityEntropy/Content/ShadowMetaball.cs`
- `CalamityEntropy/Content/EclipseMetaball.cs`
- `CalamityEntropy/Content/Particles/WindParticle.cs`
- `CalamityEntropy/Content/Particles/Trail.cs`
- `CalamityEntropy/Content/Items/Books/ControlTerminal.cs`
- `CalamityEntropy/Content/Skies/CrSky.cs`
- `CalamityEntropy/Assets/Effects/`
