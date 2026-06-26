# 腐巢意志 (The Hive Mind) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `HiveMind`
- **重写的NPC目标**: `ModContent.NPCType<HiveBlob>()`, `ModContent.NPCType<HiveMindBoss>()`, `ModContent.NPCType<DarkHeart>()`
- **关联源文件**:
  - `BlobProjectile.cs`
  - `DarkHeartBehaviorOverride.cs`
  - `EaterOfSouls.cs`
  - `HiveBlobBehaviorOverride.cs`
  - `HiveMindBehaviorOverride.cs`
  - `HiveMindWave.cs`
  - `ShadeFire.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `FinalPhaseLifeRatio: 0.2f`
- `Phase Ratio Array: FinalPhaseLifeRatio`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `HiveMindAttackState`
- `SuspensionStateDrift`
- `Reset`
- `NPCSpawnArc`
- `SpinLunge`
- `CloudDash`
- `EaterOfSoulsWall`
- `UndergroundFlameDash`
- `CursedRain`
- `SlowDown`
- `BlobBurst`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `BlobProjectile`
  - *实现细节*: `BlobProjectile.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ShadeFire`
  - *实现细节*: `ShadeFire.cs` (常规渲染)
- **弹幕类名/类型**: `EaterOfSouls`
  - *实现细节*: `EaterOfSouls.cs` (支持自定义渲染 (PreDraw/PostDraw))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `Shader/Overlay reference in HiveMindBehaviorOverride.cs`
- Custom rendering found in EaterOfSouls.cs
- 着色器引用: `Shader/Overlay reference in DarkHeartBehaviorOverride.cs`
- Custom rendering found in BlobProjectile.cs
- Custom rendering found in HiveMindBehaviorOverride.cs
- 特效代码片段: `int type = ModContent.ProjectileType<ShaderainHostile>();`
- 特效代码片段: `int damage = CalamityMod.NPCs.HiveMind.HiveMind.ShaderainDamage;`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in HiveMindWave.cs