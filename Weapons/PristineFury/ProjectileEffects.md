# PristineFury 寮瑰箷鐗规晥绗旇

鑼冨洿锛氬綋鍓?`Weapons/PristineFury` 涓嬬殑寮瑰箷绫伙紱涓嶇粺璁?`Player`銆丮ark/瑙勫垯 helper銆佺函瑙﹀彂绫汇€備笅闈㈡妸鐗规晥鍒嗘垚鈥滅粯鍒跺嚱鏁扮壒鏁堚€濆拰鈥淎I/鍛戒腑绮掑瓙鐗规晥鈥濄€?
## 鏈綋銆佸彸閿€侀挬鐖笌琚姩

### NewLegendPristineFuryHoldOut
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢绘鍣ㄦ湰浣撳拰 glow 璐村浘锛涢緳鐪?榫欏彛鐑熼浘銆丗akeCalamity 鍏呰兘銆佸彸閿?Arc Nova 鍏呰兘銆佹灙鍙ｅ厜鍜岄挬鐖搫鍔涙潯鍒嗗埆鐢?`BloomCircle`銆乣HalfStar`銆乣magic_03`銆乣smoke_04`銆乣ForwardSmear`銆乣BloomRing`銆乣GenericBarBack/Front`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氭寔鏈夊拰宸?鍙抽敭鍑嗗闃舵鐢熸垚 `GlowOrbParticle`銆乣PointParticle` 绛夊皬鍏夌偣锛涘叿浣撴敾鍑荤矑瀛愪富瑕佺敱鍚勯樁娈?effect projectile 鎵挎媴銆?
### PristineFuryPassiveTentacle
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氳鍔ㄨЕ鎵嬬殑鍙閮ㄥ垎闈犵矑瀛愶紝涓昏鏄?`CustomSpark("CalamityMod/Particles/BloomCircle")`銆乣DustID.Shadowflame` 鍜?`DustID.Torch`銆?
### PristineFuryHook
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `CalamityMod/Particles/ThinEndedLine` 閫愭鐢婚摼绾匡紝鍐嶇敾閽╃埅鏈綋銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氬懡涓?閲婃斁鏃剁敓鎴?`SparkParticle` 鍜?`DirectionalPulseRing`銆?
### PristineFuryRightPellet
- 缁樺埗鍑芥暟鐗规晥锛氭病鏈夎嚜瀹氫箟缁樺埗锛屼娇鐢ㄩ粯璁ゅ脊骞曠粯鍒跺拰 trail cache銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚 `DustID.Torch`锛岃惤鍦扮敓鎴?`PristineFuryGroundFlame`銆?
### PristineFuryGroundFlame
- 缁樺埗鍑芥暟鐗规晥锛氭病鏈夎嚜瀹氫箟缁樺埗銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敤 `MediumMistParticle` 鍋氬湴闈㈢伀闆撅紝骞剁敤 `SparkParticle` 鐐圭紑鐏槦銆?
### PristineFuryImpactExplosion
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢荤垎鐐歌创鍥句袱灞傦紝涓€灞備富棰樿壊锛屼竴灞傜櫧鑹叉牳蹇冦€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱垎鐐稿惎鍔ㄦ椂鐢熸垚 `SparkParticle`銆乣DirectionalPulseRing`銆乣CustomPulse("SoftRoundExplosion")`銆乣CustomPulse("FlameExplosion")`銆?
### PristineFuryRightNovaChargeOrb
- 缁樺埗鍑芥暟鐗规晥锛氳皟鐢?`PristineFuryRightNovaVisuals.DrawArcNovaOrb`锛屼娇鐢?`BloomCircle`銆乣ForwardSmear`銆乣BloomRing`銆乣FullStar` 鐢诲彸閿搫鍔涚悆銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氬厖鑳芥椂鐢熸垚 `SparkParticle` 鎴?`PointParticle`锛屾弧钃勫姏/鑴夊啿鏃剁敓鎴?`DirectionalPulseRing`銆乣CustomPulse("BloomCircle")` 鍜?`DustID.Torch`銆?
### PristineFuryRightNovaFireball
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢绘棫浣嶇疆 `BloomCircle` 娈嬪奖銆佺伀鐞冩湰浣擄紝骞惰皟鐢?`DrawArcNovaOrb` 鍙?Arc Nova 鏍稿績銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚 `MediumMistParticle`銆乣CustomSpark("SmallBloom")`锛涚垎鐐告椂鐢熸垚 `PristineFuryImpactExplosion`锛屽苟鐢熸垚 `DirectionalPulseRing`銆乣CustomPulse("SoftRoundExplosion")`銆乣SparkParticle`銆?
### PristineFuryRightNovaPseudoLaser
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢?`Utils.DrawLaser` 缁樺埗 `CalamityMod/Projectiles/LaserProj` 涓ゅ眰婵€鍏夛紝骞跺湪璧风偣/缁堢偣鍙?`BloomCircle`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氭部婵€鍏夌敓鎴?`PointParticle` 鍜?`DustID.Torch`銆?
## APreHardMode

### PFIdle_Flame
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氫娇鐢?`SparkParticle` 鍜?`ModContent.DustType<FinalFlame>()`銆?
### PFEvilT2_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle`銆乣ThinEndedLine`銆乣HalfStar`銆乣magic_03`銆乣magic_04` 鐢绘硶闃?鍑嗘槦寮忕伀鐒般€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`/`DustID.YellowTorch` 鍜?`GlowOrbParticle`銆?
### PFSlimeGod_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle` 鐢婚粡娑茬涓婚鐏洟銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`/`DustID.YellowTorch` 鍜?`GlowOrbParticle`銆?
## BPrePlantera

### PFBrimstoneElemental_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `CalamityMod/Particles/MediumMist` 鐢荤～鐏浘銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`ModContent.DustType<BrimstoneFlame>()` 鍜?`MediumMistParticle`銆?
### PFBrimstoneElemental_Barrage
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `CalamityUtils.DrawAfterimagesCentered` 鍜?`BloomCircle` 鐢?Hellborn 寮逛綋/鍏夋檿銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DiamondDust`銆乣SquashDust`锛岀垎鍙戞椂鐢熸垚 `CustomPulse("BloomCircle")`銆?
### PFBrimstoneElemental_Laser
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鍒嗘缁樺埗 `BrimstoneRayMid` 鍜?`BrimstoneRayEnd`锛屼笉鏄?`Utils.DrawLaser`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氬厜鏉熸湯绔敓鎴?`CalamityDusts.Brimstone`銆乣GlowOrbParticle` 鍜?`DirectionalPulseRing`銆?
### PFFakeCalamity_ChargeOrb
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle`銆乣ForwardSmear`銆乣BloomRing` 鐢昏搫鍔涚悆銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氳搫鍔涘畬鎴愭椂鐢熸垚 `DirectionalPulseRing`锛屽苟鐢熸垚 `DustID.GoldFlame`/`DustID.YellowTorch`銆?
### PFFakeCalamity_NovaOrb
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤鏃т綅缃?`BloomCircle` 娈嬪奖鍜?`HalfStar` 鏄熻姃銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`SquashDust`銆乣GlowOrbParticle`銆乣CustomSpark("BloomCircle")`銆乣CustomSpark("SmallBloom")`銆?
### PFFakeCalamity_NovaExplosion
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DirectionalPulseRing`銆乣CustomPulse("SoftRoundExplosion")`銆乣CustomPulse("BloomCircle")`銆乣SparkParticle`銆乣PointParticle` 鍜岄噾鐏?Dust銆?
### PFHardMode_TotalityFire
- 缁樺埗鍑芥暟鐗规晥锛氫娇鐢?`CalamityUtils.DrawAfterimagesCentered`锛屾暣浣?`PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`/`DustID.YellowTorch`銆乣DirectionalPulseRing`銆乣CustomPulse`銆乣SparkParticle`銆乣PointParticle`銆乣MediumMistParticle`銆?
### PFHardMode_GroundFire
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氫娇鐢?`MediumMistParticle` 鍋氬湴闈㈢伀銆?
### PFHardMode_HeavyFireball
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢荤伀鐞冩湰浣撳拰鍏夋檿銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`/`DustID.YellowTorch`銆乣GlowOrbParticle`銆乣HeavySmokeParticle`銆乣MediumMistParticle`銆乣SparkParticle`銆?
### PFPlantera_PseudoLaser
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢讳吉婵€鍏夌嚎娈点€?- AI/鍛戒腑绮掑瓙鐗规晥锛氭病鏈夐澶栫矑瀛愶紝涓昏鏄吉婵€鍏?hitbox銆?
### PFPlantera_Flame
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸閫昏緫/浼ゅ寮瑰箷锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
### PFPrime_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `MediumMist` 鐢荤伀闆俱€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.Torch`/`DustID.SolarFlare`銆乣CustomSpark`銆乣SparkParticle`銆?
### PFPrime_BounceExplosion
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴愭墿鏁ｇ垎鍙戠矑瀛愶紝涓昏鏈嶅姟 `PFPrime_Flame` 鐨勫脊璺崇垎鐐搞€?
## CPreMoodLord

### PFAurora_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle` 鍜?`ThinEndedLine` 鐢绘瀬鍏夊紡鍏夋潫銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`SparkParticle` 鍜?`CustomSpark("ThinEndedLine")`銆?
### PFAurora_MuzzleOrb
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle` 涓?`HalfStar` 鐢绘灙鍙ｇ悆銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`GlowOrbParticle`銆?
### PFGoliath_ReaperDrone
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 drone 鏈綋銆乣BloomCircle`銆乣XykWingOrange1`銆乣XykWingOrange2` 鐢荤繀鑶€鍜屽彂鍏夊眰銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`/`DustID.GreenTorch`銆乣GlowOrbParticle`銆乣HeavySmokeParticle`銆?
### PFGoliath_MouseCrosshair
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氭病鏈夌矑瀛愶紝涓昏鏄紶鏍囧噯鏄?瀹氫綅鎺у埗寮瑰箷銆?
### PFGoliath_HiveNukeMissile
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `CalamityUtils.DrawAfterimagesCentered`锛屽苟鍙?`StarProj` 鍜?`FlameExplosion` 鍏夋晥銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GreenTorch`銆乣GlowOrbParticle`銆乣HeavySmokeParticle`銆?
### PFGoliath_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢荤伀鐒版湰浣撱€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GemDiamond` 鍜?`MediumMistParticle`銆?
## DPreDog

### PFDog_ChargeOrb
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle`銆乣BloomRing`銆乣CircularSmear` 鐢诲悶鍣€呰搫鍔涚悆銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`SparkParticle`銆乣GlowOrbParticle`銆乣SquishyLightParticle`銆乣HeavySmokeParticle`銆乣DirectionalPulseRing`銆乣CustomPulse`銆乣DustID.GoldFlame`/`SquashDust`銆?
### PFDog_Flame
- 缁樺埗鍑芥暟鐗规晥锛氬疄鐜?`IPixelatedPrimitiveRenderer`锛涗娇鐢?`PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]`锛屽垎鍒粦瀹?`ScarletDevilStreak` 鍜?`SylvestaffStreak`锛涘父瑙?`PreDraw` 杩樼敤 `BloomCircle`銆乣BloomRing`銆乣CircularSmear` 鐢绘牳蹇冦€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`GlowOrbParticle`銆乣SquishyLightParticle`銆乣HeavySmokeParticle`銆乣SparkParticle`銆乣DirectionalPulseRing`銆乣CustomPulse` 鍜?`SquashDust`銆?
### PFMoonlord_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `ThinEndedLine` 鐢绘湀鎬讳富棰樼伀绾裤€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`GlowOrbParticle`銆?
### PFMoonlord_VortexScorpioRocket
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢?`ScorpioRocket_Glow`锛屽苟閫氳繃 `IPixelatedPrimitiveRenderer` 浣跨敤 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + `SylvestaffStreak`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.Vortex`銆乣NanoParticle` 鍜?`DirectionalPulseRing`銆?
### PFMoonlord_SolarLaser
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `Utils.DrawLaser` 缁樺埗澶槼婵€鍏夈€?- AI/鍛戒腑绮掑瓙鐗规晥锛氭部鏉熺敓鎴?`BloomLineVFX`銆乣GlowOrbParticle`銆乣GlowSparkParticle`銆乣SparkParticle`锛屾湯绔敓鎴?`PFMoonlord_SolarExplosion`銆?
### PFMoonlord_SolarExplosion
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`CustomPulse("SoftRoundExplosion")`銆乣DirectionalPulseRing` 鍜?`CustomSpark("SmallBloom")`銆?
### PFPolterghast_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle` 鐢婚濡栨潙姝ｄ富棰樼伀鍥€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`SparkParticle`銆乣DirectionalPulseRing`銆乣DustID.GoldFlame`/`DustID.YellowTorch`銆?
### PFProvidence_Flame
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `ProvidenceMarkParticle` 鍜?`SmallBloom`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`CustomPulse("ProvidenceMarkParticle")`銆乣CustomSpark("ProvidenceMarkParticle")`銆乣CustomSpark("SmallBloom")`銆?
### PFProvidence_NukeOfBliss
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `SoftRoundExplosion` 鐢诲ぇ鑼冨洿鏍哥垎瑙嗚銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`LightDust`銆乣GlowOrbParticle`銆乣HeavySmokeParticle`銆乣SquishyLightParticle`銆?
### PFProvidence_HolyShrapnel
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle` 鍜?`HalfStar` 鐢诲湥鐏鐗囥€?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸杩愬姩閫昏緫锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
### PFProvidence_HolyFireField
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `ProvidenceMarkParticle` 鍜?`SoftRoundExplosion` 鐢诲湥鐏満銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸鍖哄煙鎸佺画浼ゅ锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
### PFProvidence_HolyRainOrb
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢婚洦浜?鍏夌悆銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`GlowOrbParticle` 鍜?`SparkParticle`銆?
### PFProvidence_MoltenRainBlob
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢荤啍闆ㄥ脊浣撱€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`GlowOrbParticle`銆乣HeavySmokeParticle`銆乣MediumMistParticle`銆?
### PFRavager_BloodBoilerOrb
- 缁樺埗鍑芥暟鐗规晥锛氭病鏈夎嚜瀹氫箟缁樺埗锛屼富瑕侀潬绮掑瓙琛ㄧ幇銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`MediumMistParticle`銆乣SparkParticle`锛屽苟浣跨敤 `DetailedExplosion`銆乣DustyCircleHardEdge` 璐村浘鐩稿叧鐖嗗彂銆?
### PFRavager_Laser
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `BloomCircle`銆乣ThinEndedLine`銆乣PearlParticleGlow`銆乣WaterFoam` 鐢昏娌告縺鍏夈€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.GoldFlame`銆乣DustID.LifeDrain`銆?
## EAfterDog

### PFDragon_Flame
- 缁樺埗鍑芥暟鐗规晥锛氭棤浼犵粺 projectile 缁樺埗锛宍PreDraw` 杩斿洖 `false`锛岃瑙変富瑕侀潬绮掑瓙銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`DustID.Torch`/`DustID.GoldFlame`銆乣SmallSmokeParticle`銆乣CustomSpark("SmallBloom")`銆乣CustomPulse("BloomRing")`銆乣SparkParticle`銆?
### PFDragon_Burst
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?`SparkParticle`銆乣SmallSmokeParticle`銆乣CustomPulse("BloomRing")`銆?
### PFExoTwins_ArtemisLaser
- 缁樺埗鍑芥暟鐗规晥锛氫娇鐢?`CalamityUtils.DrawAfterimagesCentered` 鍜?`LaserWallTelegraphBeam` 缁樺埗 Artemis 婵€鍏?棰勮绾裤€?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸婵€鍏夐€昏緫锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
### PFExoTwins_ApolloRocket
- 缁樺埗鍑芥暟鐗规晥锛氫娇鐢?`CalamityUtils.DrawAfterimagesCentered`锛屽苟鍙?`ApolloRocketGlow`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴?Exo 涓婚灏樺焹锛屼娇鐢?ExoMechEffects 鍐呴儴闅忔満 dust type銆?
### PFExoAresLaserBeamStart
- 缁樺埗鍑芥暟鐗规晥锛氭棤甯歌 `PreDraw`锛涘唴閮ㄧ粯鍒朵娇鐢?`AresLaserBeamMiddle` 涓?`AresLaserBeamEnd` 鍒嗘婵€鍏夎创鍥俱€?- AI/鍛戒腑绮掑瓙鐗规晥锛氭部鍏夋潫鐢熸垚 Exo 涓婚 dust銆?
### PFExoThanatosBeamTelegraph
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `LaserWallTelegraphBeam` 鐢?Thanatos 棰勮绾裤€?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸棰勮鎺у埗锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
### PFExoThanatosBeamStart
- 缁樺埗鍑芥暟鐗规晥锛氭棤甯歌 `PreDraw`锛涘唴閮ㄧ粯鍒朵娇鐢?`ThanatosBeamMiddle` 涓?`ThanatosBeamEnd` 鍒嗘婵€鍏夎创鍥俱€?- AI/鍛戒腑绮掑瓙鐗规晥锛氬厜鏉熸湯绔敓鎴?`BrimstoneFlame` Dust銆?
### PFExoThanatosSideLaser
- 缁樺埗鍑芥暟鐗规晥锛氫娇鐢?`CalamityUtils.DrawAfterimagesCentered` 鍜?`LaserWallTelegraphBeam` 缁樺埗渚у悜婵€鍏夈€?- AI/鍛戒腑绮掑瓙鐗规晥锛氫富瑕佹槸婵€鍏夐€昏緫锛屾簮鐮佷腑娌℃湁棰濆绮掑瓙璋冪敤銆?
