# Kevin 观察报告：持续放电法杖的绘制与命中逻辑

## 1. 总体判断

Kevin 是 InfernumMode 里一把非常典型的“持续引导型特效武器”。它的电流不是用普通 `Projectile`、`Dust`、`PrimitiveTrail` 逐段画出来的，而是用两个 1800x1800 的 `ManagedRenderTarget` 做反馈迭代，再通过 `KevinLightningShader.fx` 每帧把上一帧的电场衰减、旋转、加噪声并重新注入亮线。换句话说，它的主体视觉是一个持续自更新的屏幕纹理，而不是一条每帧重新生成的线。

这也是 Kevin 看起来“电流会活着抖动”的原因：旧电流不会瞬间消失，而是进入下一帧作为采样源，被 shader 稍微衰减、染色、旋转后继续存在；新电流则沿噪声曲线叠加上去。最终在弹幕 `PreDraw` 里把这张 render target 用 Additive 混合画到玩家手持方向上。

## 2. 主要源码位置

- `Content/Items/Weapons/Magic/Kevin.cs`
- `Content/Projectiles/Magic/KevinProjectile.cs`
- `Assets/Effects/Shapes/KevinLightningShader.fx`
- `Assets/Effects/InfernumEffectsRegistry.cs`
- `Assets/Sounds/InfernumSoundRegistry.cs`

## 3. 物品本体：只负责持续引导入口

`Kevin.cs` 的物品本体很薄，关键点是：

- `TargetingDistance = 884f`，这是自动索敌和命中判定的最大范围。
- `LightningArea = 1800`，这是电流 render target 的宽高。
- `Item.damage = 23000`，`DamageClass.Magic`，`Item.mana = 4`。
- `Item.useTime = Item.useAnimation = 21`，但是实际引导时弹幕会不断把玩家 `itemTime` / `itemAnimation` 压回 2，所以这个使用时间更像是初始发射节奏。
- `Item.noUseGraphic = true`，物品本体不画，视觉完全交给 `KevinProjectile`。
- `Item.channel = true`，玩家按住左键持续维持弹幕。
- `Item.shoot = ModContent.ProjectileType<KevinProjectile>()`。
- `CanUseItem` 限制 `player.ownedProjectileCounts[Item.shoot] <= 0`，也就是每个玩家同一时间只能存在一个 Kevin 引导弹幕。

这个结构说明 Kevin 的“武器”其实只是一个发射器，真正的武器逻辑、绘制、索敌、耗蓝、音效都在 `KevinProjectile`。

## 4. 弹幕生命周期

`KevinProjectile` 的 `SetDefaults` 设置为：

- `width = height = 38`
- `friendly = true`
- `DamageType = Magic`
- `tileCollide = false`
- `ignoreWater = true`
- `timeLeft = 7200`
- `penetrate = -1`
- `usesLocalNPCImmunity = true`
- `localNPCHitCooldown = 6`

虽然 `timeLeft` 初始很长，但在 `AdjustPlayerValues` 里每帧都会设为 2，所以它实际靠玩家持续引导续命。玩家松开、死亡、不能用物品、被控制时，AI 开头直接 `Projectile.Kill()`。

## 5. 两张 RenderTarget：电流特效的核心容器

`OnSpawn` 里创建两张同尺寸 render target：

- `LightningTarget`：保存“上一帧/当前稳定电场”。
- `TemporaryAuxillaryTarget`：作为本帧更新的临时目标。

两张都是 `Kevin.LightningArea x Kevin.LightningArea`，也就是 1800x1800。按 RGBA 32 位粗算，一张约 12.96 MB，两张约 25.92 MB，不算额外 GPU 资源开销。这是一个比较重的视觉做法，但它换来的好处是电流可以有连续的历史残影和反馈感。

创建之后，弹幕把 `UpdateLightningField` 订阅到：

```cs
RenderTargetManager.RenderTargetUpdateLoopEvent += UpdateLightningField;
```

这点很关键。它没有在普通 `AI` 或普通 `PreDraw` 里直接改 render target，而是挂到专门的 render target 更新事件上，避免在错误的绘制阶段操作 GPU 目标。

弹幕死亡时会：

- 取消订阅 `RenderTargetManager.RenderTargetUpdateLoopEvent -= UpdateLightningField`
- 用 `Main.QueueMainThreadAction` 延迟 dispose 两张 target
- 停止循环电流音效

这是复制这种做法时必须保留的清理步骤，否则容易留下 render target 泄漏或事件悬挂。

## 6. 索敌与电流方向

每帧 AI 的索敌流程是：

1. 弹幕中心固定到 `Owner.MountedCenter`。
2. `TargetIndex = -1`。
3. 调用 `Projectile.Center.ClosestNPCAt(Kevin.TargetingDistance)` 找最近 NPC。
4. 如果找到目标：
   - `TargetIndex = potentialTarget.whoAmI`
   - `Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget, 0.6f)`
   - `LightningDistance = Projectile.Distance(potentialTarget.Center)`
5. 如果找不到目标，并且当前玩家是本地 owner：
   - 朝 `Main.MouseWorld` 方向瞄准
   - `LightningDistance = distanceToMouse * Main.rand.NextFloat(0.9f, 1.1f)`
   - 同步 `Projectile.netUpdate = true`

这里的 `Projectile.velocity` 不是普通意义上的飞行速度，它是“枪口/电流朝向”。有目标时它用 0.6 的 Lerp 快速贴向目标方向；没目标时才用鼠标方向。

随后 `LightningDistance` 会被限制到：

```cs
Kevin.LightningArea * 0.5f - 8f
```

`LightningArea` 是 1800，所以最大显示距离是 892 像素。这个限制来自 render target 是以弹幕中心为原点绘制，半径不能超过 900，否则电流会越出贴图边界。

## 7. 命中逻辑：大范围碰撞，小范围实际命中目标

Kevin 的 `Colliding` 很粗暴：

```cs
projHitbox.Distance(targetHitbox.Center()) <= Kevin.TargetingDistance
```

也就是说，只要 NPC 在 884 像素范围内，碰撞层面就认为可能命中。但真正是否能打到由 `CanHitNPC` 限制：

```cs
target.whoAmI == TargetIndex ? null : false
```

所以它不是范围电击所有敌人，而是“大范围寻找碰撞候选 + 只允许当前锁定目标受伤”。这样能让电流视觉看起来很大，但伤害逻辑仍然是单目标引导。

命中冷却用的是 local NPC immunity，`localNPCHitCooldown = 6`，因此锁定同一个目标时可以非常高频地跳伤害。

## 8. 玩家手持表现

Kevin 的手持不是普通物品动画，而是弹幕自己控制玩家：

- `Projectile.timeLeft = 2`：持续续命。
- `Owner.heldProj = Projectile.whoAmI`：告诉游戏玩家正在持有这个弹幕。
- `Owner.itemTime = 2`
- `Owner.itemAnimation = 2`
- `Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation()`
- `Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt()`
- `Owner.ChangeDir(Projectile.spriteDirection)`

随后弹幕中心向当前电流方向前移 20 像素：

```cs
Projectile.Center += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 20f;
```

玩家前臂角度设置为：

```cs
float frontArmRotation = Projectile.rotation - PiOver2;
Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
```

然后 `Projectile.rotation += PiOver2`，让弹幕贴图绘制方向和逻辑方向对齐。这里的关键是：逻辑方向先用于玩家手臂，之后再加 `PiOver2` 变成贴图方向。

## 9. 电流更新流程：每帧把旧电场加工成新电场

`UpdateLightningField` 的流程如下：

1. 把 GPU render target 切到 `TemporaryAuxillaryTarget.Target`。
2. 清空为透明。
3. 开启 `SpriteSortMode.Immediate`，`BlendState.AlphaBlend`，`SamplerState.AnisotropicClamp`。
4. 绑定纹理：
   - `Textures[0] = LightningTarget.Target`，也就是上一帧电场。
   - `Textures[1] = InfernumTextureRegistry.WavyNoise.Value`，也就是噪声图。
5. 计算：
   - `angularOffset = Projectile.oldRot[0] - Projectile.oldRot[1]`
   - `lightningDirection = Projectile.velocity.SafeNormalize(Vector2.Zero)`
   - `LightningCoordinateOffset += lightningDirection * -0.003f`
6. 给 shader 塞参数。
7. 应用 `UpdatePass`。
8. 把 `LightningTarget.Target` 绘制到临时 target 上，这一步绘制会被 shader 接管。
9. 把临时 target 内容 copy 回 `LightningTarget.Target`。

这就是“双缓冲反馈”。`LightningTarget` 不是每帧从零生成，而是被上一帧递归加工。电流的连续感来自这里。

## 10. 传给 KevinLightningShader 的参数

`KevinProjectile` 传入的主要参数有：

- `uColor = LightningColor.ToVector3()`，`LightningColor` 是 `Color.Lerp(Color.Cyan, Color.DeepSkyBlue, 0.7f)`。
- `uTime = Main.GlobalTimeWrappedHourly`。
- `actualSize = LightningTarget.Target.Size()`。
- `screenMoveOffset = Main.screenPosition - Main.screenLastPosition`。
- `lightningDirection = Projectile.velocity.SafeNormalize(Vector2.Zero)`。
- `lightningAngle = angularOffset`。
- `noiseCoordsOffset = LightningCoordinateOffset`。
- `currentFrame = Main.GameUpdateCount`。
- `lightningLength = LightningDistance / LightningTarget.Target.Width + 0.5f`。
- `zoomFactor = 15f`。
- `bigArc = Main.rand.NextBool(5)`。

其中 `bigArc` 每帧有 1/5 概率为 true，用来让噪声幅度变大，偶尔出现更夸张的大弧线。

有一个细节：shader 文件里声明了 `uTime`、`screenMoveOffset`、`lightningDirection`，但当前 `UpdatePreviousState` 实际没有使用它们。真正参与视觉的核心参数是 `uColor`、`actualSize`、`lightningAngle`、`noiseCoordsOffset`、`currentFrame`、`lightningLength`、`zoomFactor`、`bigArc`，以及采样器 `uImage0` / `uImage1`。

## 11. Shader 内部到底怎么画电流

`KevinLightningShader.fx` 的 `UpdatePreviousState` 是关键。

### 11.1 像素化坐标

```hlsl
float2 pixelationZoom = 2 / actualSize;
float2 pixelatedCoords = floor(coords / pixelationZoom) * pixelationZoom;
```

`actualSize` 是 1800x1800，所以 `2 / actualSize` 大约是 0.001111。也就是说 shader 会把 UV 坐标按约 2 像素一格量化。这个量化让电流边缘带一点像素/栅格质感，不是完全平滑的曲线。

### 11.2 旋转上一帧电场

```hlsl
float2 rotatedCoords = RotatedBy(pixelatedCoords - 0.5, lightningAngle) + 0.5;
float4 color = tex2D(uImage0, rotatedCoords);
float4 result = color;
```

`uImage0` 是上一帧的 `LightningTarget`。shader 用 `lightningAngle` 把上一帧电场绕中心旋转后再采样。`lightningAngle` 来自 `Projectile.oldRot[0] - Projectile.oldRot[1]`，也就是本帧与上一帧旋转差。

这个设计非常巧：当玩家转动武器时，不是把旧电流粗暴清空，而是把旧电流跟着方向旋过去。于是电流会有一种“被枪口方向拖着走”的惯性残影。

### 11.3 衰减并染色

```hlsl
result *= float4(0.81 + uColor.r * 0.14, 0.81 + uColor.g * 0.14, 0.81 + uColor.b * 0.14, 1) * 0.88;
```

旧电场不会保留原亮度。它每帧乘上一个接近 0.88 的衰减系数，并根据 `uColor` 稍微向电流颜色偏移。这个操作让旧电弧逐渐淡掉，但淡掉过程中保持偏蓝的能量色。

如果没有这一步，render target 会越积越亮，最后变成一整块白蓝色噪声。

### 11.4 噪声曲线

```hlsl
float2 baseNoiseCoords = (pixelatedCoords + noiseCoordsOffset) * 0.9;
float2 noiseCoords = float2(baseNoiseCoords.x, currentFrame * floor(1 + abs(pixelatedCoords.y) * 3) * 0.02);
float noise = FractalNoise(noiseCoords) * 1.1;
if (bigArc)
    noise *= 1.5;
```

这里的噪声不是简单用 `coords.xy` 采样。它的 x 来自像素坐标和 `noiseCoordsOffset`，y 则主要来自 `currentFrame`，并乘上 `floor(1 + abs(pixelatedCoords.y) * 3)`。这会让不同 y 区域按不同节奏滚动，形成电流分层抖动。

`FractalNoise` 内部采样 `uImage1`，循环 5 次：

- 初始 `amplitude = 0.5`
- 每层 `coords *= 2`
- 每层 `amplitude *= 0.5`
- 最后 `result * 2 - 1`

这是一个很轻量的分形噪声，负责让电流主线弯曲。

### 11.5 明亮电弧的数学公式

核心亮线来自：

```hlsl
float4 brightness = 0.0156 / abs(pixelatedCoords.y * zoomFactor - noise - zoomFactor * 0.5);
result += brightness * direction.x * smoothstep(0.04, 0.12, coords.x) * smoothstep(lightningLength, lightningLength - 0.03, pixelatedCoords.x);
```

可以拆成几部分：

- `pixelatedCoords.y * zoomFactor - noise - zoomFactor * 0.5`：构造一条围绕贴图垂直中线的噪声曲线。
- `0.0156 / abs(...)`：距离曲线越近越亮，接近曲线时亮度爆发。
- `direction.x`：以贴图中心为基准，让正 x 方向成为主要放电方向。
- `smoothstep(0.04, 0.12, coords.x)`：从贴图左侧淡入，避免边缘突然出现硬线。
- `smoothstep(lightningLength, lightningLength - 0.03, pixelatedCoords.x)`：按照电流长度截断末端。

`lightningLength` 的计算是 `LightningDistance / 1800 + 0.5`。因为贴图中心是 0.5，所以距离越远，电流终点越接近贴图右边缘。最大距离 892 时，`lightningLength` 大约是 0.996。

这说明 Kevin 的电流本质上是在一张以中心为原点的大贴图上，沿正 x 方向生成噪声亮线，然后在最终绘制时用弹幕旋转把它转到实际瞄准方向。

## 12. 最终绘制：Additive 贴回世界

`PreDraw` 先画电流：

1. 结束当前 `spriteBatch`。
2. 用 `BlendState.Additive` 和 `SamplerState.PointClamp` 重新 Begin。
3. 把 `LightningTarget.Target` 画在 `Projectile.Center - Main.screenPosition`。
4. 原点是整张 target 的中心。
5. 旋转是 `Projectile.rotation - PiOver2`。
6. 画完后 `ResetBlendState`。

随后再画 Kevin 本体贴图：

- `Main.projFrames[Type] = 8`
- `Projectile.frame = Projectile.frameCounter / 3 % 8`
- 每 3 帧换一帧，8 帧循环

所以视觉层级是：先 additive 电流场，再普通混合画法杖本体。

有一个需要注意的代码细节：`PreDraw` 里的判定写的是：

```cs
if (LightningTarget != null || !LightningTarget.IsDisposed)
```

这按防御式写法应该更像 `&&`，因为如果 `LightningTarget` 为空，右侧会访问空对象。不过正常流程下 `OnSpawn` 一定会创建 target，所以实际游戏中一般不会撞到。若要复刻，建议改成更稳的 `LightningTarget != null && !LightningTarget.IsDisposed`。

## 13. 音效与命中特效

电流循环音效通过 `SlotId ElectricitySound` 保存。AI 每帧检查：

- 如果音效还在播放，就更新 `t.Position = Projectile.Center`。
- 否则重新播放 `InfernumSoundRegistry.KevinElectricitySound`，音量 0.6。

命中 NPC 时，每次循环 8 次：

- 生成一个 `SparkParticle`
  - 速度 2 到 8
  - 颜色在 `LightningColor` 和白色之间随机插值
  - 生命周期 45
  - scale 0.8
- 再生成一个 `ElectricArc`
  - 速度 2 到 23
  - 颜色在 `LightningColor` 和白色之间插值 0.1 到 0.65
  - scale 0.76
  - 生命周期 27

这部分是命中反馈，不是主电流本体。主电流仍然来自 render target。

## 14. 可复刻重点

如果以后想在 CLCB 里做类似“持续活电流”的武器，Kevin 最值得借的不是数值，而是这套结构：

1. 物品只负责发射一个 channel holdout。
2. 弹幕固定到玩家手上，并持续控制 `heldProj`、`itemTime`、`itemAnimation`、`SetCompositeArmFront`。
3. 主视觉使用两张 render target 反馈迭代。
4. shader 采样上一帧图像，先衰减，再按噪声公式注入新亮线。
5. 最终用 Additive 把整张电场贴回世界。
6. 命中逻辑和视觉范围分离：视觉可以很大，真正受伤目标由 `TargetIndex` 控制。
7. 一定要在死亡时取消 render target 更新事件并 dispose 资源。

Kevin 的核心价值是“反馈型 shader 武器”的范例。它的电流不是画一条线，而是维护一块活的电场。
