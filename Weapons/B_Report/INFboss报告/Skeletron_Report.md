# 骷髅王 (Skeletron) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Skeletron`
- **重写的NPC目标**: `NPCID.SkeletronHead`, `NPCID.SkeletronHand`
- **关联源文件**:
  - `AcceleratingSkull.cs`
  - `NonHomingSkull.cs`
  - `ShadowflameFireball.cs`
  - `SkeletronHandBehaviorOverride.cs`
  - `SkeletronHeadBehaviorOverride.cs`
  - `SpinningFireball.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.85f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.475f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `SkeletronAttackType`
- `Phase1Fakeout`
- `HoverSkulls`
- `SpinCharge`
- `HandWaves`
- `HandShadowflameBurst`
- `HandShadowflameWaves`
- `DownwardAcceleratingSkulls`
- `DeathAnimation`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `SpinningFireball`
  - *实现细节*: `SpinningFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `AcceleratingSkull`
  - *实现细节*: `AcceleratingSkull.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `NonHomingSkull`
  - *实现细节*: `NonHomingSkull.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ShadowflameFireball`
  - *实现细节*: `ShadowflameFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in NonHomingSkull.cs
- Custom rendering found in SkeletronHeadBehaviorOverride.cs
- Custom rendering found in AcceleratingSkull.cs
- Custom rendering found in ShadowflameFireball.cs
- Custom rendering found in SpinningFireball.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);`