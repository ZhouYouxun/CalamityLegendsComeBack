# 世纪之花 (Plantera) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `Plantera`
- **重写的NPC目标**: `ModContent.NPCType<PlanterasFreeTentacle>()`, `NPCID.PlanterasTentacle`, `NPCID.Plantera`
- **关联源文件**:
  - `BouncingPetal.cs`
  - `ExplodingFlower.cs`
  - `NettlevineArenaSeparator.cs`
  - `Petal.cs`
  - `PlanteraBehaviorOverride.cs`
  - `PlanteraFreeTentacleBehaviorOverride.cs`
  - `PlanteraPinkTentacle.cs`
  - `PlanteraTentacleBehaviorOverride.cs`
  - `SporeFlower.cs`
  - `SporeGas.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.35f`
- `Phase2LifeRatio: 0.8f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `PlanteraAttackState`
- `UnripeFakeout`
- `RedBlossom`
- `PetalBurst`
- `PoisonousGasRelease`
- `TentacleSnap`
- `NettleBorders`
- `RoseGrowth`
- `Charge`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Reset damage.*
- **源码注释**: *Summon weird leg tentacle hook things.*
- **源码注释**: *Perform the transition to Phase 2. This involves the usage of camera effects and the removal of Plantera's bulb.*
- **源码注释**: *Disable extra damage from the poisoned debuff. The attacks themselves hit hard enough.*
- **源码注释**: *The constitutes the first phase.*
- **源码注释**: *Even if the player is dead it is still a valid index.*
- **源码注释**: *Go to the next attack after a burst of 8 petals has been shot.*
- **源码注释**: *Ignore blocked directions if possible, to prevent the player from getting unfairly hit.*
- **源码注释**: *Release a burst of nettle vines. These do not do damage for a moment and linger, splitting the arena*
- **源码注释**: *Do the charge.*
- **源码注释**: *Roar right and turn into a trap plant thing before transitioning back to attacking.*
- **源码注释**: *Ensure that Plantera starts phase 2 off with the poisonous gas release attack.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `ExplodingFlower`
  - *实现细节*: `ExplodingFlower.cs` (常规渲染)
- **弹幕类名/类型**: `BouncingPetal`
  - *实现细节*: `BouncingPetal.cs` (常规渲染)
- **弹幕类名/类型**: `SporeFlower`
  - *实现细节*: `SporeFlower.cs` (常规渲染)
- **弹幕类名/类型**: `SporeGas`
  - *实现细节*: `SporeGas.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `NettlevineArenaSeparator`
  - *实现细节*: `NettlevineArenaSeparator.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `Petal`
  - *实现细节*: `Petal.cs` (常规渲染)

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in PlanteraTentacleBehaviorOverride.cs
- Custom rendering found in NettlevineArenaSeparator.cs
- Custom rendering found in PlanteraBehaviorOverride.cs
- Custom rendering found in SporeGas.cs
- Custom rendering found in PlanteraPinkTentacle.cs

### 屏幕特效 (Screen Effects):
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item17, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item73, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Roar, npc.Center);`