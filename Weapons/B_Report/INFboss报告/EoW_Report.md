# 世界吞噬者 (Eater of Worlds) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `EoW`
- **重写的NPC目标**: `NPCID.EaterofWorldsBody`, `NPCID.EaterofWorldsHead`
- **关联源文件**:
  - `CorruptThorn.cs`
  - `CursedBullet.cs`
  - `CursedFlameBomb.cs`
  - `EoWHeadBehaviorOverride.cs`
  - `EoWSegmentBehaviorOverride.cs`
  - `ShadowOrb.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- 无硬编码的血量比例常量，可能使用默认的阶段转换或动态AI。

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `EoWAttackState`
- `CursedBombBurst`
- `VineCharge`
- `ShadowOrbSummon`
- `RainHover`
- `DownwardSlam`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Charge and do a roar sound.*
- **源码注释**: *Spawn a shadow orb that'll summon an enemy near the target.*
- **源码注释**: *The spawned enemies may interfere with later attacks if not killed in time.*
- **源码注释**: *Hover above the player.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `CursedBullet`
  - *实现细节*: `CursedBullet.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CursedFlameBomb`
  - *实现细节*: `CursedFlameBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CorruptThorn`
  - *实现细节*: `CorruptThorn.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ShadowOrb`
  - *实现细节*: `ShadowOrb.cs` (常规渲染)

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in CorruptThorn.cs
- Custom rendering found in CursedBullet.cs
- Custom rendering found in CursedFlameBomb.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item20, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item62, npc.Center);`