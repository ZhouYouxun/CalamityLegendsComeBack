# 史莱姆王 (King Slime) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `KingSlime`
- **重写的NPC目标**: `NPCID.KingSlime`, `ModContent.NPCType<KingSlimeJewelRuby>()`
- **关联源文件**:
  - `DeathSlash.cs`
  - `JewelBeam.cs`
  - `JewelBehaviorOverride.cs`
  - `KingSlimeBehaviorOverride.cs`
  - `Ninja.cs`
  - `Shuriken.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase2LifeRatio: 0.75f`
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase3LifeRatio: 0.3f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `KingSlimeAttackType`
- `SmallJump`
- `LargeJump`
- `SlamJump`
- `Teleport`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- 暂无特定提取的源码AI逻辑注释。

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `Shuriken`
  - *实现细节*: `Shuriken.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `JewelBeam`
  - *实现细节*: `JewelBeam.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DeathSlash`
  - *实现细节*: `DeathSlash.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有特殊的死亡动画或谢幕仪式 (Special Death Animation / Outro)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- Custom rendering found in KingSlimeBehaviorOverride.cs
- 着色器引用: `Shader/Overlay reference in DeathSlash.cs`
- Custom rendering found in JewelBeam.cs
- 着色器引用: `var tear = InfernumEffectsRegistry.RealityTearVertexShader;`
- Custom rendering found in JewelBehaviorOverride.cs
- Custom rendering found in Shuriken.cs
- Custom rendering found in Ninja.cs
- Custom rendering found in DeathSlash.cs

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in KingSlimeBehaviorOverride.cs
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item28, target.Center);`