# 灾厄之影 (Calamitas Shadow) - Infernum Mode 深度分析报告

## 一、基本信息 (Basic Information)
- **模组内部ID**: `CalamitasShadow`
- **重写的NPC目标**: `ModContent.NPCType<SoulSeeker>()`, `ModContent.NPCType<Catastrophe>()`, `ModContent.NPCType<CalamitasShadowBoss>()`, `ModContent.NPCType<Cataclysm>()`
- **关联源文件**:
  - `AccentuationHexProj.cs`
  - `ArcingBrimstoneDart.cs`
  - `BaseHexProj.cs`
  - `BrimstoneBomb.cs`
  - `BrimstoneBoomExplosion.cs`
  - `BrimstoneLightning.cs`
  - `BrimstoneMeteor.cs`
  - `BrimstoneSlash.cs`
  - `CalamitasShadowBehaviorOverride.cs`
  - `CataclysmBehaviorOverride.cs`
  - `CatastropheBehaviorOverride.cs`
  - `CatharsisHexProj.cs`
  - `CatharsisSoul.cs`
  - `CharredWand.cs`
  - `ConvergingShadowSpark.cs`
  - `DarkMagicFlame.cs`
  - `EntropyBeam.cs`
  - `HauntingSoulSeeker.cs`
  - `HomingBrimstoneBurst.cs`
  - `IndignationHexProj.cs`
  - `LargeDarkFireOrb.cs`
  - `LingeringBrimstoneFlames.cs`
  - `RisingBrimstoneFireball.cs`
  - `ShadowBlob.cs`
  - `SoulSeeker2.cs`
  - `SoulSeekerBehaviorOverride.cs`
  - `SoulSeekerResurrectionBeam.cs`
  - `ThinBrimstoneSlash.cs`
  - `WeaknessHexProj.cs`
  - `ZealHexProj.cs`

## 二、血量阶段与触发阈值 (Life Phases & Thresholds)
- `Phase Ratio Array: Phase2LifeRatio, Phase3LifeRatio`
- `Phase2LifeRatio: 0.55f`
- `Phase3LifeRatio: 0.2f`

## 三、攻击模式与AI状态 (Attack Patterns & AI States)
### 状态机/枚举: `CalShadowAttackType`
- `SpawnAnimation`
- `WandFireballs`
- `SoulSeekerResurrection`
- `ShadowTeleports`
- `DarkOverheadFireball`
- `ConvergingBookEnergy`
- `// Nerd emoji.
            FireburstDashes`
- `BrothersPhase`
- `TransitionToFinalPhase`
- `BarrageOfArcingDarts`
- `FireSlashes`
- `RisingBrimstoneFireBursts`
- `DeathAnimation`
### 状态机/枚举: `SCalBrotherAttackType`
- `HorizontalCharges`
- `FireAndSwordSlashes`
- `BladeUppercutAndDashes`

### AI行为核心逻辑与注释摘录 (Core AI Logic & Comments)
- **源码注释**: *Check if the player is fighting in the underworld.*
- **源码注释**: *Disable extra damage from the brimstone flames debuff. The attacks themselves hit hard enough.*
- **源码注释**: *Create fire on the player.*
- **源码注释**: *Handle phase transitions.*
- **源码注释**: *Respond the gravity and natural tile collision for the duration of the attack.*
- **源码注释**: *Fly into the air and transition to the first attack after the background is fully dark.*
- **源码注释**: *Teleport above the player and make all seekers leave.*
- **源码注释**: *Play a charge telegraph sound.*
- **源码注释**: *Inititalize the teleport offset direction.*
- **源码注释**: *Jitter in place and become transluscent while casting a shadow void telegraph near the player.*
- **源码注释**: *Look in the direction of the player if not extremely close to them horizontally.*
- **源码注释**: *Make the player emit a lot of smoke if they're far away.*
- **源码注释**: *Give the player the madness effect if they leave the circle.*
- **源码注释**: *Hover to the side of the target in anticipation of the charge.*
- **源码注释**: *Charge at the target.*

## 四、Boss弹幕分析 (Projectiles)
- **弹幕类名/类型**: `RisingBrimstoneFireball`
  - *实现细节*: `RisingBrimstoneFireball.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ThinBrimstoneSlash`
  - *实现细节*: `ThinBrimstoneSlash.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LargeDarkFireOrb`
  - *实现细节*: `LargeDarkFireOrb.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BrimstoneMeteor`
  - *实现细节*: `BrimstoneMeteor.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BrimstoneLightning`
  - *实现细节*: `BrimstoneLightning.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BrimstoneBomb`
  - *实现细节*: `BrimstoneBomb.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `SoulSeekerResurrectionBeam`
  - *实现细节*: `SoulSeekerResurrectionBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `HomingBrimstoneBurst`
  - *实现细节*: `HomingBrimstoneBurst.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `CatharsisSoul`
  - *实现细节*: `CatharsisSoul.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `ArcingBrimstoneDart`
  - *实现细节*: `ArcingBrimstoneDart.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `DarkMagicFlame`
  - *实现细节*: `DarkMagicFlame.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `BrimstoneBoomExplosion`
  - *实现细节*: `BrimstoneBoomExplosion.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BrimstoneSlash`
  - *实现细节*: `BrimstoneSlash.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `HauntingSoulSeeker`
  - *实现细节*: `HauntingSoulSeeker.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `LingeringBrimstoneFlames`
  - *实现细节*: `LingeringBrimstoneFlames.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `BaseHexProj`
  - *实现细节*: `BaseHexProj.cs` (常规渲染)
- **弹幕类名/类型**: `ShadowBlob`
  - *实现细节*: `ShadowBlob.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `CharredWand`
  - *实现细节*: `CharredWand.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))
- **弹幕类名/类型**: `ConvergingShadowSpark`
  - *实现细节*: `ConvergingShadowSpark.cs` (支持自定义渲染 (PreDraw/PostDraw))
- **弹幕类名/类型**: `EntropyBeam`
  - *实现细节*: `EntropyBeam.cs` (支持自定义渲染 (PreDraw/PostDraw) ＋ 含有自定义Shader/拖尾特效 (Shader/Trail))

## 五、特色机制与专有系统 (Unique Mechanics & Systems)
- **特色系统**: 包含专属场地/竞技场锁定或地形修改逻辑 (Arena Lock/Terrain modification)
- **特色系统**: 支持宠物小红帽 (Hat Girl) 战斗提示或对话 (Pet Dialogue Support)
- **特色系统**: 有专属的背景音乐(BGM)或场景音效控制 (Custom Music / Scene Effect)

## 六、特效与着色器分析 (Visual Effects & Shaders)
### 着色器 (Shaders) & 绘制机制:
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.UseSaturation(Projectile.velocity.Length() / 13f);`
- Custom rendering found in ThinBrimstoneSlash.cs
- Custom rendering found in CharredWand.cs
- Custom rendering found in HomingBrimstoneBurst.cs
- Custom rendering found in SoulSeekerResurrectionBeam.cs
- Custom rendering found in DarkMagicFlame.cs
- 着色器引用: `Shader/Overlay reference in LargeDarkFireOrb.cs`
- Custom rendering found in ConvergingShadowSpark.cs
- Custom rendering found in BrimstoneBomb.cs
- 着色器引用: `var lightning = InfernumEffectsRegistry.GaleLightningShader;`
- Custom rendering found in SoulSeeker2.cs
- Custom rendering found in SoulSeekerBehaviorOverride.cs
- Custom rendering found in CataclysmBehaviorOverride.cs
- 着色器引用: `InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);`
- 着色器引用: `Effect fireballShader = InfernumEffectsRegistry.FireballShader.GetShader().Shader;`
- Custom rendering found in ShadowBlob.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakMagma);`
- 着色器引用: `FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader`
- Custom rendering found in BrimstoneLightning.cs
- 着色器引用: `Shader/Overlay reference in HomingBrimstoneBurst.cs`
- Custom rendering found in BrimstoneSlash.cs
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");`
- Custom rendering found in CalamitasShadowBehaviorOverride.cs
- 着色器引用: `var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Red);`
- Custom rendering found in BrimstoneMeteor.cs
- Custom rendering found in LargeDarkFireOrb.cs
- 着色器引用: `Shader/Overlay reference in CalamitasShadowBehaviorOverride.cs`
- 着色器引用: `BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVert`
- 着色器引用: `Shader/Overlay reference in BrimstoneLightning.cs`
- Custom rendering found in BrimstoneBoomExplosion.cs
- Custom rendering found in ArcingBrimstoneDart.cs
- Custom rendering found in EntropyBeam.cs
- Custom rendering found in CatastropheBehaviorOverride.cs
- Custom rendering found in LingeringBrimstoneFlames.cs
- Custom rendering found in RisingBrimstoneFireball.cs
- 着色器引用: `Shader/Overlay reference in CharredWand.cs`
- Custom rendering found in CatharsisSoul.cs
- 着色器引用: `Shader/Overlay reference in DarkMagicFlame.cs`
- 着色器引用: `Shader/Overlay reference in EntropyBeam.cs`
- Custom rendering found in HauntingSoulSeeker.cs
- 着色器引用: `Shader/Overlay reference in SoulSeekerResurrectionBeam.cs`
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) `
- 着色器引用: `InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) `
- 特效代码片段: `using InfernumMode.Common.Graphics.Metaballs;`
- 特效代码片段: `using InfernumMode.Common.Graphics.Primitives;`
- 特效代码片段: `using Terraria.Graphics.Shaders;`
- 特效代码片段: `public static Primitive3DStrip HexStripDrawer`
- 特效代码片段: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(target.Center + teleportOffsetAngle.ToRotationVector2() * telepo`
- 特效代码片段: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- 特效代码片段: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- 特效代码片段: `ModContent.GetInstance<ShadowMetaball>().CreateParticle(shadowTexture.CreateMetaballsFromTexture(npc.Center, npc.rotatio`
- 特效代码片段: `Main.spriteBatch.EnterShaderRegion();`
- 特效代码片段: `var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;`

### 屏幕特效 (Screen Effects):
- Screen shake/effects found in CalamitasShadowBehaviorOverride.cs
- Screen shake/effects found in CharredWand.cs
- Screen shake/effects found in CataclysmBehaviorOverride.cs
- Screen shake/effects found in BrimstoneLightning.cs
- 屏幕震动/音效触发: `using InfernumMode.Assets.Sounds;`
- 屏幕震动/音效触发: `npc.HitSound = SoundID.NPCHit49 with { Pitch = -0.56f, Volume = 1.3f };`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.NPCDeath39 with { Pitch = -0.8f, Volume = 0.15f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(feelingLikeABigShot ? InfernumSoundRegistry.GolemSpamtonSound : HeavenlyGale.LightningStrikeSound,`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.4f, Volume = 1.6f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SCalBoss.SpawnSound with { Pitch = -0.12f, Volume = 0.7f }, target.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(HeavenlyGale.LightningStrikeSound, npc.Center);`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item73, wandEnd);`
- 屏幕震动/音效触发: `Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 4f;`
- 屏幕震动/音效触发: `SoundEngine.PlaySound(SoundID.Item117 with { Pitch = 0.3f }, npc.Center);`