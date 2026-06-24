# 灾厄（Calamity Mod）召唤物 AI 使用说明书

> 本文档基于对 CalamityMod 全部召唤武器及其弹幕源码的系统性阅读，整理出各类召唤物 AI 的核心机制、代码模式和设计思路，供本项目（CalamityLegendsComeBack）开发参考。字数约 14000 字，覆盖早期到末期全段位召唤武器。

---

## 一、前言：灾厄召唤物的规模与体系

在 Calamity Mod 中，召唤武器是数量最多、机制最复杂的武器类别之一。从最早期的 FrostBlossomStaff（霜冻花朵法杖）到末期的 UniverseSplitter（宇宙分裂器），全 Mod 共有约 **98 件** 召唤武器（含哨兵、鞭子类），对应超过 **200 个** 召唤物弹幕文件。

这些召唤物在行为模式上跨度极大：
- 最简单的只是普通追击并接触伤害的浮游生物；
- 复杂的拥有多个攻击阶段、自定义 UI 条、父子联动节段、甚至完整的轨道运动体系。

灾厄的召唤物体系同时经历了从「传统 tML AI 风格」到「现代 BaseMinionProjectile 基类风格」的演变，理解这两套体系对于自制召唤武器开发至关重要。

---

## 二、基础框架体系

### 2.1 召唤物槽位系统（Minion Slots）

每个召唤物在武器的 `SetDefaults()` 中通过 `Item.minionSlots` 设定占用的召唤槽数量。Calamity 中的槽位分布如下：

| 阶段 | 典型武器 | 槽位数 |
|------|----------|--------|
| 早期 | FrostBlossomStaff、CausticStaff | 1 |
| 困难前期 | BrittleStarStaff（含附加盾甲加成）| 1 |
| 困难中期 | TundraFlameBlossomsStaff | 1（上限3只）|
| 困难后期 | DragonbloodDisgorger | 6 |
| 月亮领主后 | Endogenesis | 10 |
| 开发者专属 | CosmicImmaterializer、UniverseSplitter | 10 |

注意：**槽位数并不等于只能召唤一只**。大多数多只上限通过在 AI 中对 `player.ownedProjectileCounts[type]` 计数来控制，或通过 Buff 中的 `ai[0]++` 系统递增。

### 2.2 Buff 持续机制

召唤物通过 Buff 与玩家保持绑定。标准写法：

```csharp
// 在武器 SetDefaults() 中：
Item.buffType = ModContent.BuffType<SomeMinionBuff>();

// 在 UseItem() 或 ShootProjectile 生成后：
player.AddBuff(Item.buffType, 2); // 2帧是常见设置

// 在召唤物弹幕的 AI() 中：
if (player.dead)
    modPlayer.minionBool = false;
if (modPlayer.minionBool)
    Projectile.timeLeft = 2; // 每帧重置存活时间
```

这套机制使得召唤物在玩家存活、Buff 激活时永远不会超时死亡。一旦 Buff 消失（玩家死亡、主动取消），召唤物在 2 帧后即消失。

### 2.3 BrittleStarStaff 的特殊右键移除机制

BrittleStaff（脆星法杖）还实现了**右键单独移除召唤物**的机制：玩家每次使用武器时，若已有召唤物存在，则触发右键模式逐一移除，同时提供每只召唤物 +3 防御的叠加加成。这是 Calamity 中少见的「召唤物可被玩家主动逐一撤回」的设计。

### 2.4 BaseMinionProjectile 基类

较新版本的 Calamity 为召唤物弹幕引入了 `BaseMinionProjectile` 抽象基类，统一了以下功能：
- 自动处理 `Projectile.tileCollide = false`
- 自动处理 `Projectile.minion = true` 等属性
- 提供 `MinionHoming()` 扩展方法进行目标查找
- 自动处理 Boss 优先级与普通敌人的靶选切换

凡是继承 `BaseMinionProjectile` 的召唤物（如 CalamarisLamentMinion、FlyingOrthocera、DeathstareEyeball），其代码量相对简洁，目标获取由基类统一管理。

---

## 三、AI 状态变量体系

### 3.1 主状态数组 ai[]

`Projectile.ai[]` 是 tModLoader 提供的 4 个可网络同步的浮点数状态变量（索引 0-3）。Calamity 的大多数召唤物只用到 ai[0] 和 ai[1]，高复杂度的召唤物会用到 ai[2]、ai[3]。

**ai[0] 的典型用法：**

| 值 | 代表含义 | 示例武器 |
|----|----------|----------|
| `0f` | 追击/攻击状态 | ApexShark、CosmicViperSummon |
| `1f` | 返回玩家状态 | ApexShark、大多数双态召唤物 |
| 枚举值 | 复杂状态索引 | CalamarisLamentMinion（枚举 AIState）|
| 计数器 | 轨道角度存储 | GlacialEmbracePointyThing、FlowersOfMortality |
| UUID引用 | 父体弹幕标识 | EndoHydra头部存储身体实体的 whoAmI |

**ai[1] 的典型用法：**

| 值 | 代表含义 | 示例武器 |
|----|----------|----------|
| 冷却计时 | 攻击间隔计数（如累加到60f触发攻击）| ApexShark（加随机1-3） |
| 状态变量 | 副状态（如 CalamarisLament 的枚举 AIState）|  
| 暂停计数 | 冲刺后的静止帧数 | BlackDragonHead |
| 角度数据 | 轨道偏移存储（2π*i/总数）| TundraFlameBlossom、FlowersOfMortality |

**ai[2] 的典型用法（较少见）：**

| 值 | 代表含义 | 示例武器 |
|----|----------|----------|
| 防死循环计数 | 持续绕圈超过90帧时强制向目标冲刺 | BlackDragonHead |
| 旋转角度 | 物体自身旋转角度追踪 | 部分粒子型召唤物 |

### 3.2 本地状态数组 localAI[]

`Projectile.localAI[]` 同样有 4 个槽位，但**不通过网络同步**，仅在本地客户端生效，因此只用于视觉效果和一次性初始化标记。

常见用法：
```csharp
// 单机初始化标记（防止重复生成初始粒子）
if (Projectile.localAI[0] == 0f)
{
    Projectile.localAI[0] = 1f;
    // 生成初始化粒子...
}

// 本地攻击类型轮换（不影响命中计算）
int attackType = (int)(Projectile.localAI[1] % 3);
```

### 3.3 典型 AI 字段分配对照表

| 召唤物 | ai[0] | ai[1] | ai[2] | localAI[0] |
|--------|-------|-------|-------|------------|
| ApexShark | 状态(0追/1回) | 攻击冷却 | - | 初始化标记 |
| CalamarisLamentMinion | 状态枚举 | 攻击计时 | - | - |
| GlacialEmbracePointyThing | 轨道角度 | 是否绕圈 | - | - |
| BlackDragonHead | 绕圈角度 | 暂停计时 | 防死循环计数 | - |
| EndoHydraHead | 身体whoAmI | - | - | 初始化标记 |
| SunSpiritMinion | 多只计数(++) | - | - | - |

---

## 四、目标获取系统

### 4.1 玩家指定目标优先机制

Calamity 中所有的召唤物均遵循「玩家指定目标 > 自动扫描」的优先级。标准实现：

```csharp
float maxTargetDist = detectionRange; // 如 1200f 像素
Vector2 targetCenter = Vector2.Zero;
bool canAttack = false;

// 第一优先：玩家光标指定的 NPC
if (player.HasMinionAttackTargetNPC)
{
    NPC npc = Main.npc[player.MinionAttackTargetNPC];
    if (npc.CanBeChasedBy(Projectile, false))
    {
        float dist = Vector2.Distance(npc.Center, Projectile.Center);
        if (dist < maxTargetDist)
        {
            maxTargetDist = dist;
            targetCenter = npc.Center;
            canAttack = true;
        }
    }
}

// 第二优先：自动扫描所有活跃 NPC
if (!canAttack)
{
    foreach (var npc in Main.ActiveNPCs)
    {
        if (npc.CanBeChasedBy(Projectile, false))
        {
            float dist = Vector2.Distance(npc.Center, Projectile.Center);
            if (dist < maxTargetDist)
            {
                maxTargetDist = dist;
                targetCenter = npc.Center;
                canAttack = true;
            }
        }
    }
}
```

`CanBeChasedBy()` 是 tML 提供的 NPC 方法，会自动排除无敌、不可受召唤物攻击的 NPC（如友好 NPC、特定 Boss 阶段）。

### 4.2 检测范围的差异化设计

Calamity 在不同等级的召唤物上对检测范围有明显梯度：

| 召唤物 | 检测范围（像素） | 特点 |
|--------|-----------------|------|
| BelladonnaSpiritStaff | 1200 px | 早期，中等范围 |
| AncientIceChunk | 400 px（冲刺触发） | 距离短，倾向近战 |
| CalamarisLamentMinion | 8000 px | 末期，几乎全屏 |
| MutatedTruffleMinion | 8000 px | 月领后，超远程感知 |
| AtlasMunitionsBeacon（哨兵）| 2400 px（普通）/ 720 px（超载）| 超载时反而缩小 |

### 4.3 MinionHoming() 现代化接口

较新的召唤物直接调用扩展方法，简化代码：

```csharp
// 在 BaseMinionProjectile 子类中
NPC target = Projectile.Center.MinionHoming(detectionRange, owner);
if (target != null)
{
    // 朝 target.Center 运动
}
```

这个方法内部自动处理了 Boss 优先目标、基础可追击性检测，并返回距离最近的有效目标。

### 4.4 视线检测（LoS Check）

部分高级召唤物在攻击前会检查是否能看到目标，防止穿墙攻击（主要用于非穿墙投射物）：

```csharp
if (Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                          npc.position, npc.width, npc.height))
{
    // 有视线才攻击
    canShoot = true;
}
```

---

## 五、移动模式详解

### 5.1 惯性平滑追击（最常用）

这是 Calamity 召唤物中使用频率最高的移动公式：

```csharp
// 通用惯性公式：(当前速度 * 惯性值 + 目标方向 * 速度) / (惯性值 + 1)
Vector2 toTarget = targetCenter - Projectile.Center;
toTarget.Normalize();
toTarget *= chaseSpeed; // 追击速度，如 12f
Projectile.velocity = (Projectile.velocity * 40f + toTarget) / 41f;
```

惯性值越大，加速/转向越慢但越平滑。典型配置：
- **惯性 40**：大多数标准召唤物（顺滑，有「水感」）
- **惯性 15**：较为灵敏的中型召唤物
- **惯性 9**：高灵敏快速反应召唤物
- **惯性 1**：几乎瞬间转向（通常用于激光或粒子类）

### 5.2 距离自适应速度

ApexShark、BlackDragonHead 等使用基于距离的速度变化：

```csharp
float speed;
if (distanceToTarget > 200f)
    speed = 13f; // 远距离时快速接近
else
    speed = -6f; // 近距离时反向减速（防止穿越目标）
```

### 5.3 轨道/环绕运动

GlacialEmbracePointyThing、FlowersOfMortality、TundraFlameBlossom 使用轨道 AI：

```csharp
// 以玩家为中心，按角度分布（ai[0] 存储当前角度）
float angle = Projectile.ai[0];
angle += rotationSpeed; // 每帧累加角度
Projectile.ai[0] = angle;

Vector2 orbitCenter = player.Center;
Vector2 destination = orbitCenter + angle.ToRotationVector2() * orbitRadius;
Projectile.Center = Vector2.Lerp(Projectile.Center, destination, 0.25f);
```

对于多只召唤物（如 TundraFlameBlossom 最多3只），初始角度通过生成序号计算：

```csharp
// 每只花朵的初始角度 = 2π/3 * 召唤序号
float initialAngle = MathHelper.TwoPi / 3f * minionIndex;
Projectile.ai[1] = initialAngle; // 存入 ai[1]
```

FlowersOfMortality（5片花瓣）则用 `TwoPi * i / 5f` 间隔，确保均匀360°分布。

### 5.4 Lerp 平滑跟随

对于大型视觉召唤物（如 CosmicEnergySpiral），通常使用插值而非物理速度：

```csharp
Projectile.Center = Vector2.Lerp(Projectile.Center, destination, 0.25f);
// 每帧移动25%的距离差，自动呈现指数衰减的跟随效果
```

### 5.5 返回玩家逻辑

所有召唤物在目标超出范围或需要回归玩家时，都有标准的「返回」行为：

```csharp
Vector2 toPlayer = player.Center - Projectile.Center + new Vector2(0f, -60f); 
// 注：-60f 是为了让召唤物停留在玩家头顶而非脚底
float playerDist = toPlayer.Length();

if (playerDist > 70f) // 超过70像素才主动靠近
{
    toPlayer.Normalize();
    toPlayer *= returnSpeed; // 通常 10~21 f/帧
    Projectile.velocity = (Projectile.velocity * 40f + toPlayer) / 41f;
}
else if (Projectile.velocity == Vector2.Zero)
{
    // 防止速度为零导致静止不动（有时会触发物理BUG）
    Projectile.velocity = new Vector2(-0.15f, -0.05f);
}
```

### 5.6 防聚团机制（Anti-Clump）

当多只召唤物同时存在时，tML 提供了标准防聚团方法：

```csharp
Projectile.MinionAntiClump();       // 默认排斥力
Projectile.MinionAntiClump(0.15f);  // 自定义排斥强度
```

这个方法会自动扫描同类召唤物并施加排斥力，防止多只叠在同一位置。早期灾厄召唤物有时没有调用此方法，导致多只召唤物重叠的视觉问题。

---

## 六、状态机系统详解

### 6.1 双状态基础模式（最通用）

这是 Calamity 全 Mod 使用最广泛的 AI 架构：

```csharp
// 状态 0：攻击/追击状态
if (Projectile.ai[0] == 0f)
{
    if (canAttack)
    {
        // 朝目标运动
        Vector2 direction = (targetCenter - Projectile.Center).SafeNormalize(Vector2.Zero);
        Projectile.velocity = (Projectile.velocity * 40f + direction * chaseSpeed) / 41f;
        
        // 攻击冷却计时
        if (Projectile.ai[1] > 0f)
        {
            Projectile.ai[1] += Main.rand.Next(1, 4); // 随机化冷却速度
        }
        if (Projectile.ai[1] > shootCooldown)
        {
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
            // 发射投射物...
        }
    }
    else
    {
        // 无目标时返回玩家
        Projectile.ai[0] = 1f;
    }
    
    // 距离玩家过远时强制返回
    if (Vector2.Distance(player.Center, Projectile.Center) > maxTetherDist)
    {
        Projectile.ai[0] = 1f;
        Projectile.netUpdate = true;
    }
}

// 状态 1：返回玩家状态
else if (Projectile.ai[0] == 1f)
{
    // 靠近玩家时重新进入攻击状态
    if (Vector2.Distance(player.Center, Projectile.Center) < 100f)
        Projectile.ai[0] = 0f;
    
    // 朝玩家运动...
}
```

使用此模式的典型武器：ApexShark（鲨鱼法杖）、StormjawBaby（雷颌幼崽）、EtherealSubjugator（幽灵征服者）

### 6.2 三态及多态复杂模式

CalamarisLamentMinion 是 Calamity 中使用枚举型状态机的典型代表：

```csharp
public enum AIState
{
    Idle = 0,       // 游荡/返回玩家
    Shooting = 1,   // 远程射击态
    Latching = 2    // 近身附着态
}

// AI() 中的调度逻辑
private AIState CurrentState => (AIState)Projectile.ai[0]; // ai[0] 存储枚举值

private void HandleIdle() { /* 返回玩家逻辑 */ }
private void HandleShooting()
{
    // 距离 < 320px 时进入射击
    // 发射投射物，ai[1] 计时30帧一次
}
private void HandleLatching()
{
    // 距离 < 400px 时直接附着
    // 速度 *= 0.2f（附着后减速）
    // 1.25x 伤害倍率，30帧无敌帧冷却
    // 依附状态下发射速度 20，检测距离 8000
}
```

转换条件：
- Idle → Shooting：发现目标且距离 ≤ 射击范围
- Shooting → Latching：靠近到 400px 以内
- Latching → Idle：目标死亡或离开过远

### 6.3 动作型状态机（DaedalusGolem）

DaedalusGolem（代达罗斯魔像）作为一个落地型召唤物，实现了接近 Boss 的状态机：

```csharp
int AttackTimer = 0;
bool UsingChargedLaserAttack = false;

// AttackTimer == 1 时随机决定本轮攻击类型
if (AttackTimer == 1)
{
    UsingChargedLaserAttack = Main.rand.NextBool(7); // 1/8概率用蓄力激光
}

// 普通攻击：每16帧发射一次闪电弹
if (!UsingChargedLaserAttack && AttackTimer % 16 == 15)
{
    Projectile.NewProjectileDirect(...lightningBolt...);
}

// 蓄力激光攻击：在后半段持续发射激光
if (UsingChargedLaserAttack && AttackTimer >= ChargedLaserAttackTime / 2)
{
    if (AttackTimer % 16 == 15)
        Projectile.NewProjectileDirect(...chargedLaser...);
}

// 帧图切换（基于 AttackTimer 和攻击类型）
Projectile.frame = /* 插值计算 */;
```

此外，DaedalusGolem 还实现了完整的地面行走系统（重力、跳跃、障碍物检测）。

---

## 七、攻击模式详解

### 7.1 冷却计时攻击（最通用）

```csharp
// 在攻击状态下：
if (Projectile.ai[1] > 0f)
    Projectile.ai[1] += Main.rand.Next(1, 4); // 随机加速冷却，使多只不同步
    
if (Projectile.ai[1] > shootInterval) // 如 60f
{
    Projectile.ai[1] = 0f;
    Projectile.netUpdate = true;
    // 实际发射
    if (Main.myClient == Projectile.owner)
    {
        Vector2 shootDir = (targetCenter - Projectile.Center).SafeNormalize(Vector2.Zero);
        Projectile.NewProjectileDirect(
            Projectile.GetSource_FromAI(),
            Projectile.Center,
            shootDir * shootSpeed,
            ModContent.ProjectileType<SomeBullet>(),
            Projectile.damage,
            Projectile.knockBack,
            Projectile.owner
        );
    }
}
```

注意：**只有 `Main.myClient == Projectile.owner` 的客户端才生成投射物**，避免多端重复生成。

### 7.2 ChargingMinionAI 辅助函数

灾厄封装了一个常用的冲刺 AI 辅助函数：

```csharp
Projectile.ChargingMinionAI(
    targetDist: 1200f,       // 开始追击的距离
    returnDist1: 1500f,      // 开始返回的距离阈值1
    returnDist2: 2200f,      // 强制瞬移的距离阈值2
    chargeDist: 150f,        // 开始冲刺的距离
    state: 0,                // 使用的 ai 状态索引
    chargeCooldown: 24f,     // 冲刺冷却帧数
    chargeSpeed: 15f,        // 冲刺速度
    normalSpeed: 4f,         // 普通追击速度
    offset: new Vector2(0f, -60f), // 悬停偏移（头顶60像素）
    ...
);
```

这个辅助函数封装了「正常追击 → 高速冲刺 → 冷却 → 返回玩家」的完整循环。

### 7.3 爆发/连射攻击（CosmicViperSummon）

```csharp
// localAI[1] 轮换攻击类型
int attackType = (int)(Projectile.localAI[1] % 3);

// ai[1] 计时
Projectile.ai[1]++;
if (Projectile.ai[1] >= 60)
{
    Projectile.ai[1] = 0;
    Projectile.localAI[1]++;
    
    switch (attackType)
    {
        case 0: // 子弹连射
            for (int i = 0; i < 5; i++)
                Projectile.NewProjectileDirect(...bullet...);
            break;
        case 1: // 导弹攻击（带随机偏角）
            Vector2 rocketDir = shootDir.RotatedByRandom(0.1f);
            Projectile.NewProjectileDirect(...rocket...);
            break;
        case 2: // 等待（空转一轮）
            break;
    }
}
```

### 7.4 SarosPossession 蓄力通道攻击

SarosPossession（占据萨洛斯）是一个使用 `Item.channel = true` 的通道型召唤物，每次按住使用键可以生成多只召唤物，并在蓄力满时释放「日食光束」：

```csharp
// 每次使用时：
if (player.channel && modPlayer.sarosCount < maxMinions)
{
    Projectile.ai[0]++; // 递增召唤物数量
    Projectile.NewProjectileDirect(...SarosAura...);
    SoundEngine.PlaySound(SarosSpawn);
}

// 冷却满（300帧）时：
if (modPlayer.sarosCooldown <= 0 && Input.RightClick)
{
    // 发射日食光束
    SoundEngine.PlaySound(SarosFiring);
    Projectile.NewProjectileDirect(...EclipseBeam...);
    modPlayer.sarosCooldown = 300;
}
```

这种设计配合专用 UI 进度条，实现了「边召唤边蓄力」的独特游玩体验。

### 7.5 附着/潜入攻击（CalamarisLamentMinion）

CalamarisLamentMinion（卡拉玛悲叹召唤物）的附着态是灾厄召唤物中最独特的攻击形式之一：

```csharp
// 进入附着状态
private void HandleLatching()
{
    if (!Projectile.getRect().Intersects(Target.getRect()))
    {
        // 尚未接触目标——高速追近（比普通追击更快）
        Vector2 toTarget = (Target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
        Projectile.velocity = (Projectile.velocity * inertia + toTarget * extraSpeed) / (inertia + 1);
    }
    else
    {
        // 已附着在目标上——减速停留
        Projectile.velocity *= 0.2f;
        // 附着状态下伤害 * 1.25
        // 每30帧重置无敌帧，实现持续伤害
    }
}
```

附着状态的检测范围高达 8000 像素，这意味着在末期阶段召唤物几乎随时都能「跨屏感知」目标并发动附着。

---

## 八、哨兵（Sentry）系统详解

### 8.1 哨兵与普通召唤物的根本区别

| 特性 | 普通召唤物 | 哨兵 |
|------|----------|------|
| 跟随玩家 | 是 | 否（固定位置） |
| 移动能力 | 自由移动 | 无（或极有限）|
| 槽位类型 | 召唤物槽位 | 哨兵槽位（单独计数）|
| 激活方式 | `Projectile.minion = true` | `Projectile.sentry = true` |
| 管理函数 | `UpdateMaxMinions()` | `UpdateMaxTurrets()` |
| 放置位置 | 玩家周围自动 | 光标指向位置（Clamp限制） |

### 8.2 哨兵武器的标准放置代码

```csharp
// 在武器 Shoot() 方法中：
Vector2 spawnPos = Main.MouseWorld;
// Clamp 确保放置位置不超出当前屏幕范围
spawnPos = Vector2.Clamp(spawnPos, 
    new Vector2(Main.screenPosition.X, Main.screenPosition.Y),
    new Vector2(Main.screenPosition.X + Main.screenWidth, 
                Main.screenPosition.Y + Main.screenHeight));

Projectile.NewProjectileDirect(
    player.GetProjectileSource_Item(Item),
    spawnPos,
    Vector2.Zero,
    ModContent.ProjectileType<SentrySummon>(),
    Item.damage,
    Item.knockBack,
    player.whoAmI
);
player.UpdateMaxTurrets();
```

### 8.3 典型哨兵武器分析

**RustyBeaconPrototype（生锈信标原型）**
- 弹幕：RustyDrone
- 特点：ai[0] = 16f，用于控制脉冲模式的节奏（每16帧发射一次）
- 用途：早期哨兵，适合作为教学式哨兵实现参考

**SpikecragStaff（尖峰石棱法杖）**
- 弹幕：Spikecrag
- 特点：120帧脉冲速率，生成时向下偏移 6 像素以贴地；天顶世界中伤害×3
- 附加 debuff：HeavyBleeding（重度出血）

**CausticCroakerStaff（腐蚀蟾蜍法杖）**
- 弹幕：EXPLODINGFROG
- 特点：落地点向右偏移 -13 格（即在地表而非悬空），附着敌人后爆炸
- 附加 debuff：Irradiated（辐射）

**AtlasMunitionsBeacon（阿特拉斯军火信标）**
- 弹幕：AtlasMunitionsDropPod / Autocannon / AutocannonHeld
- 特点：灾厄最复杂的哨兵之一，含「过热系统」
  - 正常射速：9帧/发
  - 过热射速：23帧/发（伤害×1.18）
  - 蓄热上限：100发后达到最高温
  - 冷却时长：180帧完全冷却
  - 目标范围：2400px（过热时缩至720px）
  - 炮台存活时间：720帧后自动消失
  - 炮管可被玩家「捡起」后重新放置（ActiveCannonHeld 状态）

**Perdition（哨兵型，月领后）**
- 弹幕：PerditionBeacon
- 特点：唯一只允许存在一个的哨兵，被摧毁后自动重新生成（特殊存活逻辑）
- 位置：光标点，强制 UpdateMaxTurrets()

**MidnightSunBeacon（午夜太阳信标）**
- 特点：机枪型哨兵（18帧射速），追踪目标时对原始伤害值进行独立计算
- 附加 debuff：AuricRebuke（金光斥责）

---

## 九、多段体/联动系统

### 9.1 龙型节段召唤物

KingofConstellationsTenryu（星座之王天龙）同时生成**两条完整的蜈蚣型龙**（黑龙+白龙），每条龙包含：头部 × 1 + 身体节段 × 20 + 尾部 × 1 = **44个弹幕实体**。

```csharp
// 生成头部
int headID = Projectile.NewProjectile(...BlackDragonHead...);
// 为每个身体节段传递头部 ID（通过 ai[0]）
for (int i = 0; i < 20; i++)
{
    int bodyID = Projectile.NewProjectile(...BlackDragonBody...);
    Main.projectile[bodyID].ai[0] = headID; // 关联头部
}
// 尾部同样关联
int tailID = Projectile.NewProjectile(...BlackDragonTail...);
Main.projectile[tailID].ai[0] = headID;
```

BlackDragonHead 的 AI 逻辑：
1. 通过轨道运动（ai[0]存角度）绕圈接近目标
2. ai[2] 防死循环计数器：连续绕圈超过90帧后强制向目标直线冲刺
3. 身体/尾部通过「弹性绳」算法跟随头部

### 9.2 EndoHydra 多头九头蛇

EndoHydraStaff 生成一个「身体」实体（EndoHydraBody）作为控制核心，以及多个「头部」（EndoHydraHead）：

```csharp
// 头部读取目标信息
NPC target = Main.npc[(int)body.ai[0]]; // 目标 whoAmI 存在身体的 ai[0]
Vector2 targetPos = target.Center;

// 身体同步所有头部的目标选取
// 头部各自独立运动，共享同一目标
```

### 9.3 Endogenesis（末期联动召唤物）

Endogenesis 是灾厄最复杂的联动召唤物，由以下弹幕协同工作：
- EndoCooperBody（主体）
- EndoCooperLimbs（四肢，通过 ai[1] 存储 ID 链接）
- EndoBeam（激光指向系统）

攻击阶段轮换（伤害倍率）：
- 激光攻击：0.65× 伤害
- 冰柱攻击：1.0× 伤害
- 近战冲撞：0.95× 伤害
- 火焰喷射：0.9× 伤害

四肢通过各自的 ai[1] 存储与主体的绑定关系，实现完全同步的动画和攻击。

---

## 十、武器分类大全（按游戏进度）

### 10.1 早期游戏（Pre-Hardmode）

**FrostBlossomStaff（霜冻花法杖）**
- 召唤物：FrostBlossom
- 伤害：10，稀有度：蓝
- AI 类型：单体（每次只能有1只），生成于玩家中心，造成冻伤（Frostburn）

**CinderBlossomStaff（烬花法杖）**
- 召唤物：CinderBlossom
- 伤害：16，稀有度：橙
- AI 类型：单体限制，debuff：OnFire

**StormjawStaff（雷颌法杖）**
- 召唤物：StormjawBaby
- 伤害：11，稀有度：绿
- AI 类型：多只，每次只召唤一只，追击并施加静电放电（StaticDischarge）debuff

**WulfrumController（沃尔弗鲁姆控制器）**
- 召唤物：WulfrumDroid
- 伤害：19，稀有度：蓝
- AI 类型：实用型无人机，提供每只 +0.5 生命再生 + 2 防御的叠加增益

**BelladonnaSpiritStaff（颠茄精灵法杖）**
- 召唤物：BelladonnaSpirit
- 伤害：22，稀有度：橙
- AI 特色：**60帧延迟启动**——召唤后花瓣会等待60帧才开始追击（有「醒来」动画），重力系统（0.2强度），射速20像素/帧，检测范围1200像素

**CausticStaff（腐蚀法杖）**
- 召唤物：CausticStaffSummon
- 伤害：15，稀有度：淡红
- 特色 debuff 组合：OnFire3 + Venom + Ichor + CursedInferno + MarkedForDeath（5种同时施加）

### 10.2 困难模式前期

**AncientIceChunk（远古冰块）**
- 召唤物：IceClasperMinion
- 详细 AI 参数（硬编码常量）：
  - 无敌帧：20
  - 最大跟随距离：400px
  - 冲刺触发距离：250px
  - 停止冲刺距离：800px
  - 最小速度：12
  - 射击帧数：80帧
  - 投射物伤害倍率：1.5×
- AI 模式：双态（空闲绕行 / 主动冲刺追击远距目标）

**CausticCroakerStaff（腐蚀蟾蜍法杖）**
- 哨兵型，弹幕：EXPLODINGFROG（全大写，灾厄早期命名风格）
- 落地位置偏右 -13 格，接触爆炸

**RustyBeaconPrototype（生锈信标原型）**
- 哨兵型，16帧脉冲模式（ai[0] = 16f）

**DankStaff（浓臭法杖）**
- 召唤物：DankCreeperMinion
- 伤害：14，稀有度：橙，施加 BrainRot（脑腐）debuff

### 10.3 困难模式中期

**GlacialEmbracePointyThing（冰川拥抱尖刺）**
- 召唤物：GlacialEmbracePointyThing
- 伤害：48，稀有度：粉
- AI 类型：环绕型，ai[0]存储当前角度，ai[1]标记是否在绕圈
- 多只时360°均匀分布，附加 Frostburn2（极寒）

**TundraFlameBlossomsStaff（苔原焰花法杖）**
- 召唤物：TundraFlameBlossom
- 伤害：56，最多3只，轨道 AI
- 角度分布：`TwoPi/3 * i * 32f` 确保三朵花均匀分布
- 双重 debuff：OnFire3 + Frostburn2

**IgneousExaltation（炽焰升华）— 刀刃型召唤物**
- 召唤物：IgneousBlade
- 伤害：30，稀有度：粉
- 刀刃状态机：CircleOwner（绕玩家旋转）→ TransitionToLaunch（发射准备）→ 高速冲刺
- 多刀时有蓄力冷却追踪（25帧蓄力，120帧冷却），扇形展开角度随刀数变化

**DaedalusGolemStaff（代达罗斯魔像法杖）**
- 召唤物：DaedalusGolem
- 伤害：70，稀有度：粉，仅在实心地块上生成
- 完整地面行走AI（重力+跳跃+障碍检测）
- 攻击类型：1/8概率发动蓄力激光，其余时候发射闪电弹

**ForgottenApexWand（遗忘顶点魔杖）**
- 召唤物：ApexShark
- 伤害：46，双 debuff：HeavyBleeding + ArmorCrunch

### 10.4 困难模式后期

**DazzlingStabberStaff（炫目刺客法杖）**
- 单次召唤3只 DazzlingStabber
- AI特色：多只时角度分布算法——按已存在总数计算间隔角，超过30只时角度超过360°后封顶
- 附加 HolyFlames（圣焰）

**FlowersOfMortality（死亡之花）**
- 5片花瓣形成轨道体系
- 伤害：72，3槽位，附加 ElementalMix（元素混合）debuff
- 每片花瓣的 `rotation = ai[0]`，即旋转角度与轨道位置同步，视觉效果出色

**ViridVanguard（翠绿先锋）— 高级刀刃型**
- 召唤物：ViridVanguardBlade
- 伤害：60，槽位可变
- 攻击状态机（Shift+右键可切换旋转方向）：
  - 启动：90帧
  - 收尾：30帧
  - 冷却：300帧
  - 旋转速度：普通0.0375，攻击时×10倍
  - 三种攻击：横斩×7 / 纵刺×5 / 戳刺×6
  - 攻击伤害倍率：5×

**DragonbloodDisgorger（龙血吞噬者）**
- 召唤物：SkeletalDragonMother
- 伤害：215，6槽位，只能存在一只
- 附加 BurningBlood + Laceration 双 debuff

**KingofConstellationsTenryu（星座之王天龙，捐赠）**
- 同时生成黑龙+白龙两条完整蜈蚣龙
- 伤害：187，4槽位，附加 Shadowflame + Frostburn2
- 总弹幕数：44个（2条龙 × 22节段）

### 10.5 月亮领主后期

**PlantationStaff（园林法杖）**
- 召唤物：PlantationStaffSummon
- 伤害：58，3槽位，只能存在一只
- 完整多态攻击：
  - 荆棘球：同时2发，90帧射速，速度20
  - 种子爆发：30帧延迟，每10帧一发，速度25，3发/次 × 3次
  - 孢子：速度3，前置动作
  - 冲刺：15帧启动，持续240帧，速度35
  - 触须：速度25
- 敌人检测范围：1600像素

**MutatedTruffleMinion（变异松露召唤物）**
- 伤害：250，3槽位，只能存在一只
- 攻击系统：
  - 牙球攻击：60帧射速，每次5发连射
  - 冲刺：速度50，持续240帧
  - 漩涡形态：300帧过渡时间
- 检测范围：8000像素（几乎全屏感知）
- 生成时有微小圆形速度扰动（防完全静止）

**CalamarisLament（卡拉玛悲叹）**
- 召唤物：CalamarisLamentMinion
- 伤害：110，稀有度：PureGreen
- 三态状态机（Idle/Shooting/Latching），附加 HadopelagicPressure（深渊压力）
- 附着伤害倍率：1.25×，30帧无敌帧重置

**Vigilance（警戒）**
- 召唤物：SeekerSummonProj
- 伤害：115，多只系统（ai[0]存总数索引）
- 附加 BrimstoneFlames（硫磺火焰）

**YharonsKindleStaff（雅哈龙点火法杖）**
- 召唤物：FieryDraconid
- 伤害：325，5槽位
- 冲刺伤害倍率：2×，附加 Dragonfire（龙焰）

### 10.6 开发者/捐赠者专属

**FlamsteedRing（弗拉姆斯泰德之环）**
- 召唤物：GiantIbanRobotOfDoom
- 伤害：1999，200魔力
- 8格宽的巨型机器人，含完整状态机（TopIcon/BottomIcon模式）
- 近战技能：Regislash（雷霆斩）
- 远程技能：AndromedaDeathRay（仙女座死亡射线）
- Boss战期间使用时附加360帧受限惩罚

**UniverseSplitter（宇宙分裂器）**
- 召唤物：UniverseSplitterField
- 伤害：9000，300魔力，只能存在一只
- 30秒（1800帧）独立冷却系统（Cooldown.UniverseSplitter）
- 冷却中使用时生成废料投射物作为反馈
- 生成时触发尘埃漩涡视觉效果

**Endogenesis**
- 伤害：1300，10槽位，HotPink（Dev）稀有度
- 最高技术复杂度的召唤物之一
- 双 debuff：Voidfrost + Frozen

**TemporalUmbrella（时间雨伞）**
- 伤害：193，5槽位（Dev 专属）
- 同时生成5种子召唤物：MagicArrow / MagicHammer / MagicAxe / MagicUmbrella / MagicRifle

**AbandonedSlimeStaff（废弃史莱姆法杖）**
- 特殊槽位系统：每次使用槽位消耗量按「log₈(已用数)」指数增长
- 同时伤害倍率随槽位数增加而提升，创造了「积累式」的召唤物设计

**MirrorofKalandra（卡兰德拉之镜，捐赠）**
- 依次生成：AtzirisDisfavor → HopeShredder → WindRipper → Paradoxica → Starforge
- 5种弹幕交替出现，各有独立运动轨迹和攻击模式
- 振荡追击，距离触发切换（2500像素），多攻击态

---

## 十一、VoidConcentrationStaff——轨道标记系统

VoidConcentrationStaff（虚空专注法杖）代表了 Calamity 召唤物设计的另一个高度——**「标记+轨道叠加」系统**：

```
召唤物组成：
- VoidConcentrationMinion（主体，轨道控制）
- VoidConcentrationDarkEnergy × 7 × 3 圈 = 21 个轨道粒子
- VoidConcentrationMark（对目标的「标记」）
```

轨道粒子的脉冲运动：
```csharp
// 每帧的轨道半径在 10~400 像素之间脉冲振荡
float orbitRadius = 10f + 390f * (0.5f + 0.5f * Math.Sin(frameCount * pulseFrq));
Vector2 orbitPos = mainMinion.Center + angleVec * orbitRadius;
darkEnergy.Center = orbitPos;
```

标记（Mark）系统：
- 当目标被标记后，每10帧从召唤物释放一次「虚空弹」
- 弹幕伤害 = 储存的敌人总受击伤害 × 0.3（对召唤物伤害翻倍）
- 实际上是一种「伤害积分释放」机制，攻击越密集则后续弹幕越强

---

## 十二、AresExoskeleton——模块化武器系统

AresExoskeleton（阿瑞斯外骨骼）是 Calamity 中最复杂的召唤系统，实质上是一套**可配置的多炮台装甲**：

```
炮台模块：
- ExoskeletonPlasmaCannon（等离子炮）: 30帧射速，0.9× 伤害倍率
- ExoskeletonTeslaCannon（特斯拉炮）: 36帧射速，1.0× 倍率，放电弧（1500px分叉距离）
- ExoskeletonLaserCannon（激光炮）: 15帧射速，1.1× 倍率
- ExoskeletonGaussNukeCannon（高斯核弹炮）: 240帧射速，720px爆炸半径
```

ExoskeletonPanel 面板系统允许玩家远程添加/移除各炮台，实现完全可定制的召唤物配置。目标检测范围：1020像素。

---

## 十三、网络同步机制

在多人游戏中，召唤物 AI 的网络同步至关重要：

### 13.1 基础同步标记

```csharp
Projectile.netUpdate = true;  // 在下一个网络帧同步
Projectile.ForceNetUpdate();  // 立即强制同步
```

只要 ai[] 数组内容变化（如状态切换），就应标记 `netUpdate`，否则客户端会看到召唤物卡在旧状态。

### 13.2 复杂状态序列化

对于超过 ai[] 四个槽位的状态数据，使用二进制序列化：

```csharp
public override void SendExtraAI(BinaryWriter writer)
{
    writer.Write(AttackTimer);
    writer.Write(UsingChargedLaserAttack);
    writer.Write(SegmentBodyID);
}

public override void ReceiveExtraAI(BinaryReader reader)
{
    AttackTimer = reader.ReadInt32();
    UsingChargedLaserAttack = reader.ReadBoolean();
    SegmentBodyID = reader.ReadInt32();
}
```

DaedalusGolem 和 EndoHydra 等使用 SendExtraAI/ReceiveExtraAI 来同步超出 ai[] 范围的状态。

### 13.3 投射物生成的单端控制

```csharp
// 只有拥有者客户端生成投射物（避免多端重复）
if (Main.myClient == Projectile.owner)
{
    Projectile.NewProjectileDirect(...);
}
```

这是 Calamity 中所有召唤物生成子弹的标准写法，务必遵守。

### 13.4 瞬移后强制同步

```csharp
// 执行瞬移
Projectile.position = newPosition;
Projectile.velocity = Vector2.Zero;
Projectile.netUpdate = true; // 瞬移后必须同步
```

---

## 十四、实用代码模式总结

### 14.1 强制距离瞬移（防止召唤物丢失）

```csharp
if (Vector2.Distance(player.Center, Projectile.Center) > 2000f)
{
    Projectile.Center = player.Center;
    Projectile.velocity = Vector2.Zero;
    Projectile.netUpdate = true;
}
```

几乎所有末期召唤物都有这个保险机制，防止召唤物在高速传送玩家后「飘走」。

### 14.2 多只召唤物的序号获取

```csharp
// 遍历同类召唤物，计算自己的序号
int minionIndex = 0;
for (int i = 0; i < Main.maxProjectiles; i++)
{
    Projectile p = Main.projectile[i];
    if (p.active && p.owner == Projectile.owner 
        && p.type == Projectile.type
        && p.whoAmI != Projectile.whoAmI)
    {
        if (p.timeLeft < Projectile.timeLeft)
            minionIndex++;
    }
}
// minionIndex 即为本召唤物的序号（从0开始）
```

这个序号可用于计算轨道角度、决定特殊分工等。

### 14.3 冲刺/停滞速度切换

```csharp
const float IDLE_SPEED = 4f;
const float CHASE_SPEED = 12f;
const float DASH_SPEED = 28f;

float currentSpeed;
if (isIdle) currentSpeed = IDLE_SPEED;
else if (isDashing) currentSpeed = DASH_SPEED;
else currentSpeed = CHASE_SPEED;
```

三档速度是大多数「有攻击行为」召唤物的标准配置。

### 14.4 角度限制旋转追踪

```csharp
// 召唤物旋转角度向目标方向追踪（限制每帧旋转量）
float targetAngle = Projectile.DirectionTo(targetCenter).ToRotation();
Projectile.rotation = Projectile.rotation.AngleTowards(targetAngle, MathHelper.ToRadians(8f));
// 每帧最多旋转8度，使朝向过渡更平滑
```

### 14.5 落地型召唤物的地面检测

```csharp
// 获取召唤物脚底的瓦片位置
Point tilePos = new Point(
    (int)(Projectile.Bottom.X / 16f),
    (int)(Projectile.Bottom.Y / 16f)
);
Tile groundTile = Main.tile[tilePos.X, tilePos.Y];
bool onGround = groundTile.HasUnactuatedTile && 
                Main.tileSolid[groundTile.TileType];
```

---

## 十五、设计规律总结与建议

通过对全部 98 件召唤武器的分析，可以归纳出以下设计规律：

### 15.1 伤害进程曲线

| 阶段 | 典型伤害值 | 代表武器 |
|------|-----------|----------|
| 早期 Pre-HM | 10~25 | FrostBlossom、Belladonna |
| 困难前期 | 25~70 | GlacialEmbrace、DaedalusGolem |
| 困难中期 | 70~150 | CalamarisLament、TacticalPlagueJet |
| 月领前 | 150~400 | Endogenesis四肢、YharonsKindle |
| 月领后 | 200~500 | CosmicImmaterializer、Metastasis |
| Dev 专属 | 600~9000 | UniverseSplitter |

### 15.2 AI 复杂度与阶段对应关系

早期召唤物（Pre-HM）基本使用「一追一回」双态机，攻击方式简单（接触或单一投射物）。困难模式中期开始出现多态状态机、轨道系统、冲刺机制。月领后及捐赠者武器则是完整的多状态+多阶段攻击+联动体系，部分甚至有独立 UI、可配置炮台、全屏感知等特性。

### 15.3 槽位经济平衡原则

灾厄对槽位经济有明确思路：高 DPS 的单体召唤物倾向于高槽位（减少堆叠），低 DPS 但有实用价值（如 WulfrumDroid 的Buff型召唤物）使用1槽位支持多叠加。特殊召唤物（如 BrittleStarStaff）通过「召唤数量提供叠加Buff」的机制，使多槽叠加在机制上变得有意义而非单纯堆叠伤害。

### 15.4 对本项目的建议

1. **基础召唤物**：继承 BaseMinionProjectile，使用标准双态机 + MinionHoming()，复用灾厄已成熟的代码路径。
2. **轨道型召唤物**：用 ai[0] 存角度、TwoPi 均分、每帧累加角速度，这套模板是最稳定的轨道实现。
3. **多只召唤物**：务必调用 MinionAntiClump()，并通过时间戳比较获取序号以实现差异化行为。
4. **哨兵类**：生成位置用 Clamp 限制在屏幕内，调用 UpdateMaxTurrets()，不要忘记 Projectile.sentry = true。
5. **网络安全**：所有弹幕生成加 `if (Main.myClient == Projectile.owner)` 守卫，状态变化后标记 netUpdate。
6. **复杂联动**：父子弹幕通过 ai[0]/whoAmI 互相引用，子弹幕读取父弹幕数据时先检查 `Main.projectile[id].active`。

---

---

## 十六、特殊机制扩展专题

### 16.1 Cosmilamp——多灯笼编队重排系统

Cosmilamp（宇宙油灯）在每次使用时会触发全部已存在灯笼的编队重排：

```csharp
// 每次召唤新灯笼时，重置所有灯笼的 Timer
foreach (var proj in Main.ActiveProjectiles)
{
    if (proj.type == lampType && proj.owner == player.whoAmI)
    {
        proj.localAI[0] = 0; // 重置重排计时器
    }
}
```

这使得不管先后召唤顺序如何，每次添加新灯笼后整个编队都会重新排列成均匀圆形，视觉上始终保持对称。发射光束的冷却为 105 帧，归巢速度 17，最大锁定范围 1360 像素。每只占用 2 槽位，通过多叠加来增加输出密度。

### 16.2 EntropysVigil——三位一体同步生成

EntropysVigil 每次使用同时生成三只不同类型的小召唤物（Calamitamini、Catastromini、Cataclymini），并以 `TwoPi/3` 的角度间隔分布，形成三角形阵列。每次重新使用时，新的三角阵都有随机化的起始偏转角，防止所有三角阵完全重叠，确保视觉上的丰富感。槽位占用 2，鼓励玩家多次叠加以获得多个三角阵同时旋转的密集覆盖效果。

### 16.3 FuelCellBundle——双模式混合武器

FuelCellBundle（燃料电池束）是一件兼具召唤物和投掷物特性的混合武器：
- **左键**：召唤 PlaguebringerMK2 作为常规跟随召唤物（普通召唤物槽位）
- **右键**：将 MK2FlaskSummon 作为抛掷物投出（有弧线弹道）

这种设计使一件武器同时服务于「固定召唤」和「快速扔出」两种场景，是灾厄少见的双模式召唤器，为自制武器提供了「右键替代行为」的参考思路。

### 16.4 AmphibiansGuitar——通道持握型召唤物

AmphibiansGuitar（两栖吉他）使用 `Item.channel = true` 与 `Item.noUseGraphic = true`，将召唤物弹幕设计为一个「持握的演奏者」：玩家按住使用键时，弹幕跟随鼠标旋转，类似持握近战武器的效果，但本质上是召唤物弹幕。这种设计模式适合需要玩家持续「瞄准」方向来施放效果的召唤物。

### 16.5 EnchantedConch 与 DeepseaStaff——水域专属召唤物

部分召唤物设计有环境感知逻辑，只在水中或海洋生物群落中才能发挥全部效果。Calamity 通过检测 `player.ZoneSulphurSea`、`player.ZoneAstral` 等自定义生物群落标记来实现环境加成，使召唤物在特定区域具有额外的伤害倍率或攻击模式。这是一种将「环境探索」与「召唤物成长」挂钩的设计思路，让不同区域的召唤流玩家有不同的策略考量。

### 16.6 CorvidHarbringerStaff——群攻型禽类召唤物

禽类/蜂群系召唤物（包括 HivePod、CorvidHarbringerStaff 等）的核心设计思路是「数量覆盖」：单只伤害低，但生成速度快、槽位消耗极低，鼓励堆叠到十几只甚至数十只来制造「铺屏」效果。AI 上通常是最简单的双态追击，牺牲机制复杂度换取数量密度带来的稳定输出压制。这类召唤物设计的关键在于防聚团（MinionAntiClump）的参数调优，以及确保多只不会因攻击同步而导致帧率下降。

---

## 十七、Calamity 召唤物 AI 框架演进简史

通过对不同时期召唤物源码的对比，可以清晰看到 Calamity 的 AI 设计思路演变：

**第一阶段（早期武器，Pre-HM 到 HM 前期）**
代码风格偏向原版 Terraria，使用大量 if/else 嵌套，直接操作 `Projectile.velocity`，NPC 扫描使用裸 `for` 循环。命名不规范（如全大写的 `EXPLODINGFROG`）。

**第二阶段（HM 中后期武器）**
引入了 ChargingMinionAI 等辅助函数封装重复逻辑；开始使用枚举型状态机；多只召唤物开始有序号系统和角度分布模板；AttackTimer/AI 字段分工更加清晰。

**第三阶段（月领后及现代武器）**
全面引入 `BaseMinionProjectile` 基类；`MinionHoming()` 统一目标获取；SendExtraAI/ReceiveExtraAI 处理复杂同步；更多工具类方法（MinionAntiClump、DirectionTo、SafeNormalize）替代手动计算。代码量大幅减少，可读性显著提升。

这一演进轨迹对本项目有直接指导意义：**新写的召唤物应尽量遵循第三阶段风格**，继承 BaseMinionProjectile，使用工具方法，避免重复造轮子。只有在需要特殊行为（自定义 UI、联动节段、环境感知）时才在基类之上扩展。

---

*本文档由 CalamityLegendsComeBack 开发组基于 CalamityMod 源码分析整理，仅供内部开发参考。*
*撰写日期：2026-06-20*
