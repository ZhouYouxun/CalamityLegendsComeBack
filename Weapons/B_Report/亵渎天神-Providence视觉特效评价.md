# 普罗维登斯（Providence）重做特效观察与技术总结报告

普罗维登斯（Providence, 亵渎天神）最近在灾厄模组（Calamity Mod）中迎来了视觉与战斗机制的重做。本报告针对其特效机制进行系统整理，主要分为两大类：**飞行/战斗期间释放的自定义粒子特效** 与 **绘制函数（`PreDraw`/`PostDraw`）中的高级像素/着色器绘制**。

---

## 核心颜色与设计系统 (Color & Palette Design)
在介绍具体特效前，必须明确普罗维登斯重做后的**昼夜与特殊种子色彩变换机制**（由 `ProvUtils` 静态辅助类控制）：
*   **白天模式 (Day Mode / Normal)**：经典的黄、橙、红暖色调，使用 `CalamityDusts.ProfanedFire` 粒子，赋予天神圣火的威严感。
*   **夜晚模式 (Night Mode / Enraged)**：转为粉、紫、青冷色调，使用 `CalamityDusts.Nightwither` 粒子，弹幕使用专属的 `*Night.png` 贴图，充斥着幽冥与狂暴感。
*   **彩虹模式 (Rainbow Mode / Zenith/GFB 种子)**：弹幕和羽翼颜色随时间（`Main.GlobalTimeWrappedHourly % 6f`）在**红、橙、黄、绿、蓝、紫**六种色彩之间循环渐变，并附加反向治愈（负治疗）等抽象机制。

---

## 一、 Boss 刚出场时的灯光与生成特效 (Spawn Effects)

### 1. 核心能量汇聚状态 (Convergence State)
*   **特效名字/类名**：`Phase.HolyBlast` 阶段中的 `spawnAnimation` 生成动画逻辑。
*   **粒子与资源路径**：
    *   `GlowOrbParticle` (发光法阵/球体粒子)
    *   `CustomPulse`（使用 `"CalamityMod/Particles/SoftRoundExplosion"` 纹理）
    *   `SparkParticle` (火花粒子)
    *   `BloomCircle`（使用 `"CalamityMod/Particles/BloomCircle"` 纹理）
*   **调用/实现方法**：
    *   生成动画持续 180 帧（3秒）。在此期间，Boss 处于无敌状态（`NPC.dontTakeDamage = true`）。
    *   **屏幕震动**：每帧调用 `CalamityUtils.AddScreenshakeAt(NPC.Center, MathHelper.Lerp(0f, 0.25f, calamityGlobalNPC.newAI[3] / spawnAnimationTime), 2000)`，震动幅度随时间线性增强。
    *   **汇聚火花**：每帧在 Boss 周围 `80f` 到 `300f` 像素范围内生成大量的 `SparkParticle`，其初速度方向指向 Boss 中心核心（`destination`），产生一种周围圣光能量不断被吸入天神体内的汇聚视觉效果。
    *   **核心脉冲**：每 10 或 15 帧在 Boss 核心处生成带有缩放渐变的 `CustomPulse` 脉冲，并伴随着上升音调的 `SoundID.Item74`（圣光蓄力音效）。
    *   **蓄力光球 (Draw)**：在 `PreDraw` 中，如果处于蓄力阶段，会调用 `Main.EntitySpriteDraw` 在 Boss 核心位置重叠绘制 3 个 `BloomCircle` 纹理。通过叠加混合模式（Additive Blend）产生一个极其耀眼的光斑，其大小和不透明度根据 `CircInEasing` 缓动函数呼吸颤动。

### 2. 天神降临爆破 (Arrival Explosion Burst)
*   **特效名字/类名**：生成动画第 179 帧（`calamityGlobalNPC.newAI[3] == spawnAnimationTime - 1`）的爆发冲击波。
*   **音效路径**：`"CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay"` (亵渎死光轰鸣声)
*   **调用/实现方法**：
    *   当计数器结束，无敌解除，立刻触发一次强烈的屏幕震动（幅度为 10）。
    *   在中心瞬间发射 **40个** 高速向外扩散的 `FlameParticle` 与 **40个** 带有扰动物理的 `HeavySmokeParticle` 圣火烟雾。
    *   同时，生成多个不同初始半径的 `CustomPulse` 脉冲，分别调用 `"CalamityMod/Particles/SoftRoundExplosion"` 和 `"CalamityMod/Particles/ShatteredExplosion"` 纹理，以波纹形式快速向四周炸开。
*   **个人评价**：
    蓄力阶段的“光子向中心汇聚”到最后的“圣光大爆炸”形成了极强的视觉张力。屏幕震动的缓动递增和蓄力音效的音调渐高完美契合。天神庞大的身躯最终从炫目的白光爆破中现身，极具仪式感和压迫感。

---

## 二、 Boss 发射的所有弹幕类型及其特效列表 (Projectiles & Danmaku)

| 弹幕英文名 | 中文常用名 | 对应C#类名 | 调用与发射时机 | 专属特效与调用方法 |
| :--- | :--- | :--- | :--- | :--- |
| **Holy Blast** | 圣光爆弹 | `HolyBlast` | 核心战斗阶段，基础自导或直道射击。 | **特效描述**：尾部拖曳明亮的 `GlowOrbParticle` 与 `MediumMistParticle` 圣灰烟雾，夜间使用 `HolyBlastNight.png`。在消亡时（`OnKill`），不仅产生 30 个 `FlameParticle` 的爆炸，还会分裂成 6（昼）/ 8（夜）个呈环状发射的 `HolyFire2` 火球。<br>**调用方法**：`AI()` 中每帧在后方随机生成粒子；`OnKill()` 中使用 `CustomPulse` 环状脉冲。 |
| **Holy Bomb** | 圣光炸弹 | `HolyBomb` | 飘浮于空中的减速重力弹，起区域封锁作用。 | **特效描述**：利用 `CalamityUtils.SineBumpEasing` 算法配合计时器，在飞行中对自身贴图进行**横向与纵向的挤压变形**（`SquishAnimation`）。每隔 120 帧，炸弹产生一次剧烈跳动，向上弹射一枚自导的 `HolyFlare`，并炸出 10 个 `GlowOrbParticle`。<br>**调用方法**：在 `PreDraw` 中根据弹性系数动态调整绘制的 `scale` 向量：`new Vector2(scale + squish, scale - squish)`。 |
| **Holy Flare** | 圣光耀斑 | `HolyFlare` | 由圣光炸弹产生，或由 Boss 直接射出，缓慢追踪玩家。 | **特效描述**：高亮度自导火球，在 `AI()` 中产生发光球粒子（`GlowOrbParticle`）和厚重的灰色灰烬轨迹（`MediumMistParticle`）。消亡时播放爆裂冲击音效并伴有火花四射。<br>**调用方法**：`AI()` 中调用 `GeneralParticleHandler.SpawnParticle` 进行尾迹渲染。 |
| **Holy Fire / Holy Fire 2** | 神圣之火 | `HolyFire` / `HolyFire2` | 散播型弹幕，或由爆弹分裂产生。 | **特效描述**：天神的羽翼或爆弹散落的小圣火。夜间自动切换为偏蓝紫色的 `HolyFireNight.png`，在空中留下零星的小火星（由 `ProvUtils.GetDustID()` 获取的尘埃）。<br>**调用方法**：在 `PreDraw` 中调用 `Projectile.DrawBackglow` 绘制光晕。 |
| **Holy Spear** | 神圣之枪 | `HolySpear` | 茧化阶段（Cocoon）向四周散射的弹幕，战斗核心难点。 | **特效描述**：拥有极长拖尾的圣枪。`TrailCacheLength` 设置为 **15**，在 `PreDraw` 中利用历史位置数组 `oldPos` 渐变缩放和透明度，绘制出 15 个残影拖尾。结合移动速度，残影会产生“拉伸伸长”的透视特效。<br>**调用方法**：`PreDraw` 中通过速度向量的长度计算拉伸系数 `squish = MathHelper.Clamp(velocity.Length() / 20, -0.3f, 0.3f)`，并传递给 `EntitySpriteDraw`。 |
| **Molten Blast** | 熔岩爆弹 | `MoltenBlast` | 狂暴或夜晚阶段发射的重型岩浆弹。 | **特效描述**：与圣光爆弹类似，但性质偏向重元素岩浆。拖曳厚重的岩浆和灰色烟雾粒子。消亡时（`OnKill`）会朝随机角度抛射 6 到 9 个受重力影响的 `MoltenBlob`（熔岩滴），在地表留下大范围的熔岩封锁。<br>**调用方法**：在消亡时循环发射 `MoltenBlob` 弹幕，并播放 `SoundID.Item74`。 |
| **Molten Blob** | 熔岩滴 | `MoltenBlob` | 熔岩爆弹炸裂产生的小型岩浆滴。 | **特效描述**：具有重力加速度的红色/紫色岩浆滴，在空中下落时会持续向下漏出细小的熔岩尘埃（`CalamityDusts.ProfanedFire`）。<br>**调用方法**：利用重力公式改变 `velocity.Y`，每帧添加重力尘埃。 |
| **Providence Crystal** | 普罗维登斯水晶 | `ProvidenceCrystal` | 在玩家头顶正上方召唤的巨型亵渎晶体。 | **特效描述**：**双半晶体合并动画**。生成时，左右两个半晶体（`ProvidenceCrystal_Halves.png`）旋转并合并为一体。合并瞬间播放清脆的晶体撞击音效，并向四周喷射大量紫色的 `BlastCone` 冲击波。在 `PreDraw` 中通过 8 个方向的偏置向量重叠绘制本体，产生绚丽的晶体发光层。<br>**调用方法**：`PreDraw` 中使用计时器正弦波控制发光层偏置距离：`scaleFactor = Math.Cos(TwoPi * AI_Timer / 60) + 6f`。 |
| **Providence Holy Ray** | 亵渎死光（激光） | `ProvidenceHolyRay` | 经典的横扫死光攻击，伤害极高。 | **特效描述**：天神最震撼的死亡射线。激光中间段（`texture2D20`）并不是静态贴图，而是通过每几帧截取纹理不同帧区域（`36 * (timeLeft / 3 % 4)`）来实现**射线内部能量流动动画**。激光始端绘制巨大的 `BloomCircle` 与 `BloomRing` 发光环。<br>**调用方法**：在 `PreDraw` 中通过 `Collision.LaserScan` 动态计算激光长度，利用 `while` 循环拼接流动段纹理。如果玩家躲在掩体后，射线将无视地形障碍强制穿墙。 |
| **Holy Burn Orb** | 圣火燃球 | `HolyBurnOrb` | 高速自导星状火焰弹。 | **特效描述**：外观为带有尖锐光芒的十字星。在 `PreDraw` 中以不同角度和缩放重叠绘制多层星光纹理。弹幕运动时，会在反方向生成圆锥形的圣光冲击波（`BlastCone`）作为尾焰，表现极速移动。<br>**调用方法**：`AI()` 中调用 `GeneralParticleHandler.SpawnParticle(new CustomPulse(..., "CalamityMod/Particles/BlastCone", ...))`。 |
| **Holy Light** | 神圣之光 | `HolyLight` | 仅在特定阶段或机制下发射的绿色治愈光弹。 | **特效描述**：**唯一的绿色治疗弹幕**（`new Color(54, 209, 54)`）。天神为玩家提供的补给，拖曳着亮绿色的 `GlowSparkParticle` 和闪烁粒子。玩家触碰时会直接治疗生命值（`player.HealPlayer`），消亡时会爆发出华丽的绿色光环脉冲。<br>**调用方法**：`AI()` 中使用 `player.HealPlayer` 触发治愈数字，调用 `SpiritHeal` 网包进行多端同步。 |

*   **个人评价**：
    弹幕设计非常有层次感。激光的动态纹理剪裁让能量流看起来无比丝滑，而“神圣之枪”的运动状态（静止 -> 高速拉伸残影）与“圣光炸弹”的节奏感呼吸捏合得恰到好处。绿色治疗弹幕在黄/紫色的 Bullet Hell 中非常醒目，为高难战斗提供了优秀的视觉反馈。

---

## 三、 玩家屏幕边缘的神圣炼狱力场着色器 (Screen Shader & Forcefield)

当玩家距离普罗维登斯太远时，屏幕边缘会产生灼烧的火海特效，逼迫玩家留在 Boss 身边决战。

### 1. 技术实现 (Implementation)
*   **着色器名称**：`CalamityMod:HolyInfernoShader`
*   **注册与渲染挂载点**：
    在 `Providence.cs` 的 `Load()` 中，将绘制函数注册至 Tile 渲染前图层：
    ```csharp
    GeneralDrawLayerSystem.OnBeforeAllTiles += DrawHolyInferno;
    ```
    这保证了灼烧效果会作为背景天空的滤镜，渲染在所有物块和角色的后方，不会遮挡前景战斗的视线。
*   **传递给着色器的核心参数**：
    *   `time`: 传入全局运行时间 `Main.GlobalTimeWrappedHourly`，控制火浪边缘的流动和起伏。
    *   `radius`: 力场的安全半径（`borderRadius`）。此值并非固定值，在天神处于**火茧（Flame Cocoon）**和**枪茧（Spear Cocoon）**时，安全半径会大幅缩小（最高在 Death 模式下缩小 1000 像素），将玩家强行压缩在 Boss 身边。
    *   `burnIntensity`: 当前玩家的灼烧强度（`holyInfernoFadeIntensity`）。根据玩家到 Boss 的真实距离与安全半径的差值进行 `GetLerpValue` 插值计算。
    *   `day`: 白天/夜晚状态。白天传入 `true`，着色器渲染黄红色的烈焰；夜晚传入 `false`，着色器渲染深紫粉色的冥火。

### 2. 纹理映射 (Texture Mapping)
着色器不仅通过算法计算波形，还绑定了三个灰度噪声纹理来增强细节：
1.  **Texture 1 (对角噪声)**: `CalamityMod/ExtraTextures/GreyscaleGradients/HarshNoise`
2.  **Texture 2 (流态熔岩)**: `CalamityMod/ExtraTextures/GreyscaleGradients/MeltyNoise`
3.  **Texture 3 (柏林噪声)**: `CalamityMod/ExtraTextures/GreyscaleGradients/Perlin`
这三个噪声以不同速度进行偏移合成，最终生成宛如流动岩浆和翻滚热浪的边缘。

### 3. 伴随的声音与粒子特效
*   **临界预警（强度 > 0.45）**：播放警告音效 `ProvidenceSizzle`（滋滋的火烤预警声）。
*   **正式灼烧（强度 >= 1.0）**：玩家身上染上 `HolyInferno` 状态，播放起燃音效 `ProvidenceBurn`，并循环播放高频火烧声 `ProvidenceBurnLoop`（该声效带有 `IsLooped = true` 属性）。同时，玩家身体喷发出极高密度的 `ProfanedFire` 粒子，生命值迅速崩解。

*   **个人评价**：
    这是一个工业级品质的屏幕着色器。它不仅起到了机制惩罚（防风筝）的作用，更在视觉上极大地增强了战斗的氛围感。热浪边缘的热畸变和空气折射感极其逼真。昼夜颜色的动态切换使得不管是烈阳还是幽冥的主题都展现得淋漓尽致。

---

## 四、 Boss 死亡时的灯光与消亡特效 (Death Animation)

普罗维登斯的击败动画是一个宏大的分步消亡过程，持续 345 帧（约 5.75 秒）。

### 1. 圣体融化与空间撕裂 (Dissolving Body Drawing)
*   **特效名字**：`DoDeathAnimation` 死亡绘制覆写。
*   **调用/实现方法**：
    *   **热畸变分裂（PreDraw）**：
        根据死亡动画计时器计算当前升华强度：`burnIntensity = Utils.GetLerpValue(0f, 45f, DeathAnimationTimer, true)`。
        在 `PreDraw` 中，Boss 的本体不再是单一实例，而是根据 `burnIntensity` 线性插值分裂绘制最多 **30 个** 呈环状排开的镜像幻影。
        每个幻影的位置带有由正弦波和时间控制的辐射状偏移：
        ```csharp
        float offsetAngle = MathHelper.TwoPi * i * 2f / totalProvidencesToDraw;
        float drawOffsetFactor = (float)Math.Sin(offsetAngle * 6f + Main.GlobalTimeWrappedHourly * MathHelper.Pi);
        drawOffsetFactor *= (float)Math.Pow(burnIntensity, 3f) * 50f;
        ```
        这会让 Boss 的实体看起来在刺眼的高温中开始沸腾、融化并向四周汽化扩散。

### 2. 残影像素抖动与解体 (Silhouette Jitter PostDraw)
*   **特效名字**：`PostDraw` 中的 `Providence_DeathSilhouette`（死亡剪影）绘制。
*   **调用/实现方法**：
    *   当死亡计时器超过 91 帧，在 `PostDraw` 中会停止绘制常规本体贴图，改用绘制纯白/暗灰的死亡剪影纹理。
    *   为了表现天神身躯解体崩塌，系统采用了一种**横向切片抖动算法**：
        将死亡剪影纹理沿着纵向每 2 个像素切割成一条水平切片，每一条切片根据当前的解体进度 `progress`，在横向上施加一个随机偏置：
        ```csharp
        int outset = ((i - NPC.frame.Height / 4) * 2);
        int pr = (int)(progress * 30f);
        // 在横向上施加随机偏移 Main.rand.Next(-pr, pr) * 2 
        Main.EntitySpriteDraw(tex, NPC.Center + vec + new Vector2(0, 310) - Main.screenPosition + new Vector2(Main.rand.Next(-pr, pr) * 2, outset), ...);
        ```
        随着时间推移，水平抖动幅度从 0 扩大到 60 像素，导致整个晶体身躯像是被高频声波震碎，像素一条一条地散落至虚无中。

### 3. 能量浪潮与余烬爆裂 (Aura & Embers Explosion)
*   **调用/实现方法**：
    *   **终结震击 (Frame 92)**：在剪影解体的起点，播放庞大的水晶破碎巨响，瞬间生成 30 对 `FlameParticle` / `HeavySmokeParticle` 向外激射，并炸出多层同心扩展的 `SoftRoundExplosion` 与 `ShatteredExplosion` 脉冲。
    *   **余烬狂舞 (Frame 92 - 345)**：天神残骸不断疯狂发射无伤害的 `SwirlingFire`（回旋火花）弹幕，发射频率从 12 帧一次加速到 5 帧一次，火花速度逐渐变快，模拟能量外泄。同时屏幕震动达到峰值。
    *   **星光谢幕 (Frame 310)**：天神完全消散的瞬间，在核心点产生一次最终闪光，向全屏幕抛射 **80 个** 具有拖尾物理的 `MajesticSparkle`（庄严火花）粒子，呈球状散开。

*   **个人评价**：
    这是一个具有史诗感的 Boss 死亡特效。它摒弃了 Terraria 传统的“Boss 变成几个碎肢掉落”的做法，而是利用**数学切片抖动**和**镜像热流偏移**，在视觉上表现了一个高次元神明因为体内能量失控而“升华、气化、最终融于天际”的宏大场景。30个沸腾重影与最后的 80 颗庄严火花爆散将击败 Boss 的成就感推向了极致。

---
**报告编写完成。观察报告已保存于模组对应的武器目录下。**
