# BrinyBaron 寮瑰箷鐗规晥绗旇

鑼冨洿锛氬綋鍓?`Weapons/BrinyBaron` 涓嬬殑寮瑰箷绫伙紱涓嶇粺璁?`Cooldown`銆乣Player`銆佺函杈呭姪 UI 绫伙紝涔熶笉缁熻 `SkillB_SpinDash宸插垹闄 鏃х洰褰曘€備笅闈㈡妸鐗规晥鍒嗘垚鈥滅粯鍒跺嚱鏁扮壒鏁堚€濆拰鈥淎I/鍛戒腑绮掑瓙鐗规晥鈥濄€?
## CommonAttack

### BrinyBaron_LeftClick_Swing
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鐢绘鍣ㄦ湰浣擄紱鍛戒腑绐楀彛鐢?`CalamityMod/Particles/VerticalSmearLarge` 姘磋摑鎸ョ爫 smear锛涘彔 18 灞?`NewLegendBrinyBaronGoest` 骞界伒鍒€褰卞舰鎴愬鍙戝厜銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氭尌鐮嶆椂鐢熸垚 `LineParticle` 鍜?`DustID.Water`锛涘彸閿搫鍔涙棆杞椂鐢熸垚 `DustID.Water`/`DustID.Frost` 鐜粫姘村皹锛屾弧钃勫姏鍚庣淮鎶や竴涓?`CircularSmearSmokeyVFX` 鍦嗗舰鐑熼浘鎷栧奖锛涢樁娈垫敾鍑讳細鐢熸垚 `BBSwing_Wave`銆乣BrinyBaron_RightClick_Shuriken`銆乣BrinyBaron_TornadoBolt`銆?
### BBSwing_Wave
- 缁樺埗鍑芥暟鐗规晥锛氭牳蹇冭瑙夋槸 `PrimitiveRenderer.RenderTrail`锛屼娇鐢?`GameShaders.Misc["CalamityMod:SideStreakTrail"]` 骞剁粦瀹?`Images/Misc/Perlin`锛涢殢鍚庣敾涓€涓笉鍙 projectile 璐村浘鍗犱綅銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚涓や晶 `GlowOrbParticle` 灏炬氮銆乣DustID.Water`/`DustID.Frost` 姘撮浘锛屼互鍙婇珮闃舵鐨?`GlowSparkParticle`锛涘噺閫熸椂棰濆鐢熸垚婕傜Щ `GlowOrbParticle`锛涘畾鏃堕噴鏀?`BrinyBaron_HomingBubble`锛屽懡涓椂鐖嗘按/闇?Dust銆?
### BBSwing_Slash
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `TextureAssets.Extra[ExtrasID.SharpTears]` 鐢讳袱灞傚皷閿?slash锛屼竴灞傛í鍚戞按钃濅富浣擄紝涓€灞傚瀭鐩寸櫧钃濇牳蹇冦€?- AI/鍛戒腑绮掑瓙鐗规晥锛氱敓鎴愬垵濮嬬垎鍙戯紝鍖呭惈 `DustID.Water`銆乣DustID.Frost` 鍜屼竴涓櫧鑹?`GlowSparkParticle`銆?
### BBSwing_INV
- 缁樺埗鍑芥暟鐗规晥锛氭棤鍙缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氳繖鏄殣褰㈠懡涓锛涘懡涓椂鐢熸垚 `DustID.Water`/`DustID.Frost`锛屽苟鍦ㄩ渶瑕佹椂缁?`BBEXPlayer` 鍔?Tide 鍜屽睆骞曢渿鍔ㄣ€?
### BrinyBaron_RightClick_Shuriken
- 缁樺埗鍑芥暟鐗规晥锛氭湰浣撶敤 `TornadoProj` 璐村浘锛沗BBShuriken_Initial_Effects.DrawOutlineAndBody` 鐢?8 鍚戣摑鑹叉弿杈瑰拰鏈綋锛涘洶闅炬ā寮忛樁娈?`DrawRotatingCopies` 鐢?4 涓棆杞壇鏈紱鐚波鍚庨樁娈?`PostDraw` 鐢?`CircularSmearSmokey`銆乣SemiCircularSmearSwipe` 鍜岄澶栧厜鐜敾鏃嬭浆鍒€鐩樸€?- AI/鍛戒腑绮掑瓙鐗规晥锛氬垵濮嬮琛岀敤 `DustID.Water`锛涘懡涓?姝讳骸鐢ㄦ按灏樺拰闇滃皹鐖嗗彂锛涘彲绮橀檮闃舵浼氭寔缁悙 `DustID.Frost`/`DustID.Water`/`DustID.GemSapphire` 鍒囧壊绮掑瓙锛涢奔榫欓樁娈靛姞 `GlowOrbParticle` 铻烘棆杞ㄨ抗锛岀尓椴ㄩ樁娈靛姞 `CustomSpark("CalamityMod/Particles/BloomCircle")` 鍙岀嚎鐏姳銆?
### BrinyBaron_HomingBubble
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚娉℃场 `Gore` 411/412锛涘懡涓垨娑堝け鏃剁敓鎴?`DustID.Water` 鐖嗘场銆?
### BrinyBaron_TornadoBolt
- 缁樺埗鍑芥暟鐗规晥锛氭病鏈夎嚜瀹氫箟 `PreDraw`锛屼娇鐢ㄩ粯璁?`Projectile_407` 鍔ㄧ敾甯с€?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚 `DustID.Water`锛涘懡涓€佹挒澧欐垨姝讳骸鏃剁敓鎴?`BrinyBaron_Tornado`銆?
### BrinyBaron_Tornado
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鐢讳袱灞?`TornadoProj`锛屼竴灞傛寜 `Projectile.rotation` 鏃嬭浆锛屼竴灞傚弽鍚戞棆杞殑闈掕摑閫忔槑鍙犲奖銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱幆缁曠敓鎴?`DustID.Water`/`DustID.Frost`锛屾ā鎷熼緳鍗峰唴閮ㄦ按闆俱€?
### BrinyBaron_WaterStream
- 缁樺埗鍑芥暟鐗规晥锛氭棤鍙缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氳拷韪琛屾椂鐢熸垚 `DustID.Water`/`DustID.Frost` 灏炬祦銆傚綋鍓嶆簮鐮侀噷鍙湅鍒扮被瀹氫箟锛屾湭鐪嬪埌涓诲姩鐢熸垚璋冪敤銆?
### BBShuriken_Light
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 浣跨敤 `TextureAssets.Extra[ExtrasID.ThePerfectGlow]` 鐢绘棫浣嶇疆娈嬪奖鍜屼腑蹇?4 灞傛棆杞厜鐜€?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鐢熸垚 `DustID.Water`/`DustID.Frost`銆乣GlowOrbParticle` 鍜?`GlowSparkParticle`锛涙浜℃椂鍐嶇垎涓€鍦堟按闇?Dust 鍜?`GlowSparkParticle`銆傚畠鐢遍珮鎴愰暱闃舵鎵嬮噷鍓戞浜℃椂鐢熸垚銆?
### BBShuriken_Lazer
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶殣褰㈤珮閫?hitbox锛涢琛屾椂鐢熸垚鍙岃灪鏃?`GlowOrbParticle`銆乣CustomSpark("CalamityMod/Particles/ThinEndedLine")` 鍜屾按/闇?Dust锛涘懡涓椂鐖?`GlowSparkParticle` 涓庢按/闇?Dust銆傚綋鍓嶆簮鐮侀噷鍙湅鍒扮被瀹氫箟锛屾湭鐪嬪埌涓诲姩鐢熸垚璋冪敤銆?
## SkillA_ShortDash

### BrinyBaron_SkillDashTornado_BladeDash
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鎵嬪姩鍒囨崲 Additive/AlphaBlend锛涗娇鐢?`CalamityMod/Particles/GlowBlade` 鐢诲啿鍒哄墠绔殑澶栧眰鍏夊垉銆佸３灞傚厜鍒冨拰鏍稿績鍏夊垉锛涙棫浣嶇疆鐢绘鍣ㄦ畫褰便€?- AI/鍛戒腑绮掑瓙鐗规晥锛氬紑濮?椋炶/鍥炲脊闃舵璋冪敤 `BrinyBaron_SkillDashTornado_FlightEffects`锛岀敓鎴?`DirectionalPulseRing`銆乣CustomSpark(GlowBlade)`銆佹按/闇?Dust 鍜屾场娉?Gore锛涘懡涓悗鐢熸垚 `BBASD_Lighting`锛屽苟鎸夋垚闀块樁娈靛柗鍑烘墜閲屽墤銆?
### BBASD_Lighting
- 缁樺埗鍑芥暟鐗规晥锛氭棤缁樺埗锛宍PreDraw` 杩斿洖 `false`锛岀鎾炰篃鍏抽棴锛屼富瑕佷綔涓鸿瑙夌數鐥曘€?- AI/鍛戒腑绮掑瓙鐗规晥锛氶珮閫?extraUpdates 鐨勬洸绾跨數寮э紝鍛ㄦ湡鎬х敓鎴?`CustomSpark("CalamityMod/Particles/BloomCircle")`锛屽苟鍙€掑綊鍒嗗弶鎴愭洿灏忕數寮с€?
## SkillD_SuperDash

### Z_BrinyBaron_SkillSuperCharge_SuperDash
- 缁樺埗鍑芥暟鐗规晥锛氶攣瀹氶樁娈佃皟鐢?`BBSD_Lock_Effects.DrawLockBeam`锛屼娇鐢?`ThinEndedLine` 鍜?`BloomCircle` 鐢婚攣瀹氱嚎锛涘厖鑳?閿佸畾闃舵璋冪敤 `DrawTargetingReticle`锛屼娇鐢?`BloomCircle`銆乣magic_03`銆乣magic_04` 鐢诲噯鏄燂紱鍐插埡闃舵鐢绘鍣ㄦ湰浣撳拰 4 鍚戦潚钃濇弿杈广€?- AI/鍛戒腑绮掑瓙鐗规晥锛氬厖鑳藉紑濮?鍏呰兘涓?鍏呰兘瀹屾垚鍒嗗埆璋冪敤 `BBSD_ChargeBegan_Effects`銆乣BBSD_Charge_Effects`銆乣BBSD_ChargeFiniah_Effects`锛屼富瑕佹槸 `DirectionalPulseRing`銆乣CustomSpark`銆乣GlowOrbParticle`銆乣LineParticle`銆乣HeavySmokeParticle` 鍜?`DustID.Frost`/`GemSapphire`/`GemTopaz`锛涢攣瀹氫娇鐢?`BBSD_Lock_Effects` 鐨?`DirectionalPulseRing`銆乣CustomSpark`銆乣LineParticle`銆乣GlowOrbParticle` 鍜岃摑瀹濈煶 Dust锛涚灛绉?鍐插埡浣跨敤 `BBSD_Teleport_Effects`銆乣BBSD_Strike_Effects` 鐨?`CustomSpark`銆乣CustomPulse`銆乣DirectionalPulseRing`銆乣GlowOrbParticle`銆佹按/闇?榛勭伀 Dust锛涚粓娈典細鐢熸垚 `BBSD_Final_INV`銆?
### BBSD_VirtualPROJ
- 缁樺埗鍑芥暟鐗规晥锛氶殢鏈轰娇鐢?`KsTexture/star_01` 鍒?`star_09`锛屽啀鍙?`BloomCircle`锛涜建杩圭敤 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + `Images/Misc/Perlin`銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氭部璐濆灏旀洸绾块鍥炵帺瀹讹紝椋炶鏃剁敓鎴?`GlowOrbParticle` 鍜?`LineParticle`锛涙姷杈炬椂鐢熸垚 `DirectionalPulseRing` 鍜屼竴鍦?`GlowOrbParticle`銆?
### BBSD_Star
- 缁樺埗鍑芥暟鐗规晥锛氱敤 `PrimitiveRenderer.RenderTrail` + `GameShaders.Misc["CalamityMod:TrailStreak"]` + Perlin 缁樺埗鏄熻建锛涙湰浣撶敤 `StarofJudgement`銆乣BloomCircle` 鍜?`ThePerfectGlow` 鍙犲姞鏄熻姃銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氶琛屾椂鍙岃灪鏃?`GlowOrbParticle`锛屽苟娣风敤 `DustID.Frost`銆乣DustID.YellowTorch`锛涘懡涓?娑堝け鏃剁垎 `DustID.Water`銆乣DustID.Frost`銆佹棆鑷?`GlowOrbParticle` 鍜?`DirectionalPulseRing`銆?
### BBSD_Final_INV
- 缁樺埗鍑芥暟鐗规晥锛歚PreDraw` 鍦ㄧ洰鏍囪韩涓婄敾涓ゅ眰 `BloomCircle` 缁堢粨鏍囪銆?- AI/鍛戒腑绮掑瓙鐗规晥锛氱洰鏍囧懆鍥村懆鏈熸€х敓鎴?`CustomSpark(".../SkillA_ShortDash/GlowBlade")`銆乣DirectionalPulseRing`銆乣DustID.Water`/`DustID.YellowTorch`锛涙瘡 5 甯ч噴鏀句竴涓斁澶х殑 `BBSwing_Slash`銆?

