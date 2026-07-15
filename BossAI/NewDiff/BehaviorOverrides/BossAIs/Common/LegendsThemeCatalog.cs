using Microsoft.Xna.Framework;
using Terraria.ID;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    internal static class LegendsThemeCatalog
    {
        public static LegendsAttackProfile[][] For(string bossName) => bossName switch
        {
            "Aquatic Scourge" => AquaticScourge(),
            "Astrum Aureus" => AstrumAureus(),
            "Astrum Deus" => AstrumDeus(),
            "Ceaseless Void" => CeaselessVoid(),
            "Cryogen" => Cryogen(),
            "The Hive Mind" => HiveMind(),
            "The Old Duke" => OldDuke(),
            "The Perforators" => Perforators(),
            "The Plaguebringer Goliath" => PlaguebringerGoliath(),
            "Polterghast" => Polterghast(),
            "Providence, the Profaned Goddess" => Providence(),
            "Signus, Envoy of the Devourer" => Signus(),
            "Storm Weaver" => StormWeaver(),
            "Yharon, Dragon of Rebirth" => Yharon(),
            _ => Default()
        };

        private static LegendsAttackProfile[][] AquaticScourge() => Phases(
            Phase(
                P("Caustic Reef Orbit", LegendsPatternKind.OrbitingCrossfire, "SulphuricAcidBubble", new Color(80, 244, 170), DustID.Poisoned, "HomingGasBulb", 138, 26, 9.5f, 5),
                P("Tidal Spine Net", LegendsPatternKind.ConvergingFan, "MaulerAcidBubble", new Color(60, 210, 160), DustID.GreenTorch, "MaulerAcidDrop", 126, 24, 10.8f, 6),
                P("Sulphur Typhoon", LegendsPatternKind.VortexPressure, "OldDukeVortex", new Color(150, 255, 92), DustID.ToxicBubble, "SulphuricAcidMist", 150, 34, 8.6f, 4),
                P("Abyssal Leech Rain", LegendsPatternKind.FallingCurtain, "SulphuricAcidBubble", new Color(96, 236, 210), DustID.Water, "ToxicCloud", 132, 18, 11.2f, 7)),
            Phase(
                P("Ruptured Bubble Lattice", LegendsPatternKind.SpiralBloom, "SulphuricAcidBubble", new Color(76, 255, 190), DustID.GreenFairy, "HomingGasBulb", 126, 20, 10.4f, 7),
                P("Radline Wall Crash", LegendsPatternKind.LateralDashBarrage, "MaulerAcidDrop", new Color(174, 255, 94), DustID.PoisonStaff, "SandPoisonCloudOldDuke", 118, 22, 12.8f, 5),
                P("Acid Vortex Drag", LegendsPatternKind.MinefieldPulse, "OldDukeVortex", new Color(88, 220, 110), DustID.ToxicBubble, "SulphuricAcidMist", 144, 32, 7.5f, 6),
                P("Brine Needle Cross", LegendsPatternKind.SniperGrid, "MaulerAcidBubble", new Color(110, 255, 220), DustID.GreenTorch, "SulphuricAcidBubble", 128, 26, 15f, 4)),
            Phase(
                P("Dead Sea Monsoon", LegendsPatternKind.FallingCurtain, "MaulerAcidDrop", new Color(110, 255, 140), DustID.ToxicBubble, "SulphuricAcidBubble", 138, 13, 13.4f, 9),
                P("Toxic Maw Spiral", LegendsPatternKind.SpiralBloom, "HomingGasBulb", new Color(170, 255, 90), DustID.GreenFairy, "MaulerAcidBubble", 130, 15, 12.2f, 9),
                P("Corrosive Undertow", LegendsPatternKind.VortexPressure, "OldDukeVortex", new Color(74, 210, 168), DustID.Poisoned, "ToxicCloud", 150, 22, 10.4f, 7),
                P("Radiation Crown", LegendsPatternKind.OrbitingCrossfire, "SulphuricAcidMist", new Color(190, 255, 84), DustID.GreenTorch, "SulphuricAcidBubble", 136, 16, 13f, 8)));

        private static LegendsAttackProfile[][] AstrumAureus() => Phases(
            Phase(
                P("Aureus Railwalk", LegendsPatternKind.SniperGrid, "AstralLaser", new Color(255, 184, 64), DustID.OrangeTorch, "AstralShot2", 126, 26, 14.6f, 4),
                P("Meteor Hammer Leap", LegendsPatternKind.LateralDashBarrage, "AstralFlame", new Color(255, 110, 220), DustID.PinkTorch, "AstralShot2", 132, 24, 12.2f, 5),
                P("Astral Mine Dial", LegendsPatternKind.MinefieldPulse, "DeusMine", new Color(125, 190, 255), DustID.BlueTorch, "AstralShot2", 146, 34, 7f, 6),
                P("Star Furnace Recharge", LegendsPatternKind.SpiralBloom, "AstralShot2", new Color(255, 220, 104), DustID.GoldFlame, "AstralFlame", 118, 20, 11f, 7)),
            Phase(
                P("Nova Drill Telegraph", LegendsPatternKind.ConvergingFan, "AstralLaser", new Color(255, 148, 70), DustID.OrangeTorch, "AstralGodRay", 138, 26, 16f, 5),
                P("Comet Lattice", LegendsPatternKind.FallingCurtain, "AstralShot2", new Color(255, 96, 220), DustID.PinkTorch, "AstralFlame", 130, 16, 13f, 8),
                P("Tachyon Ricochet", LegendsPatternKind.OrbitingCrossfire, "AstralLaser", new Color(112, 220, 255), DustID.BlueTorch, "AstralShot2", 132, 20, 12.6f, 7),
                P("Aureus Spawn Echo", LegendsPatternKind.VortexPressure, "AstralFlame", new Color(255, 238, 120), DustID.GoldFlame, "DeusMine", 146, 28, 9.2f, 6)),
            Phase(
                P("Binary Star Crusher", LegendsPatternKind.ConvergingFan, "AstralGodRay", new Color(255, 188, 76), DustID.GoldFlame, "AstralLaser", 142, 20, 17f, 7),
                P("Cosmic Plating Burst", LegendsPatternKind.SpiralBloom, "AstralShot2", new Color(255, 90, 190), DustID.PinkTorch, "AstralFlame", 124, 13, 14f, 10),
                P("Orrery Missile Ring", LegendsPatternKind.OrbitingCrossfire, "DeusMine", new Color(118, 235, 255), DustID.BlueTorch, "AstralShot2", 150, 22, 11.4f, 9),
                P("Falling Constellation", LegendsPatternKind.FallingCurtain, "AstralLaser", new Color(255, 215, 92), DustID.OrangeTorch, "AstralGodRay", 136, 14, 16.6f, 8)));

        private static LegendsAttackProfile[][] AstrumDeus() => Phases(
            Phase(
                P("Warp Coil Charge", LegendsPatternKind.LateralDashBarrage, "AstralFlame", new Color(255, 90, 210), DustID.PinkTorch, "AstralShot2", 120, 22, 12.5f, 5),
                P("Meteor Wake", LegendsPatternKind.FallingCurtain, "AstralShot2", new Color(255, 165, 70), DustID.OrangeTorch, "DeusMine", 138, 17, 13f, 8),
                P("Crystal Segment Volley", LegendsPatternKind.SniperGrid, "AstralLaser", new Color(100, 230, 255), DustID.BlueTorch, "AstralShot2", 126, 25, 15.2f, 5),
                P("Infected Star Weave", LegendsPatternKind.SpiralBloom, "AstralFlame", new Color(210, 255, 110), DustID.GreenFairy, "AstralShot2", 142, 21, 11.8f, 8)),
            Phase(
                P("Constellation Collapse", LegendsPatternKind.ConvergingFan, "AstralGodRay", new Color(255, 120, 235), DustID.PinkTorch, "AstralLaser", 150, 23, 15.8f, 7),
                P("Vortex Lemniscate", LegendsPatternKind.VortexPressure, "DeusMine", new Color(160, 110, 255), DustID.PurpleTorch, "AstralFlame", 150, 30, 9f, 7),
                P("Solar System Release", LegendsPatternKind.OrbitingCrossfire, "AstralShot2", new Color(255, 210, 88), DustID.OrangeTorch, "AstralLaser", 150, 20, 12.6f, 8),
                P("Plasma Crystal Fork", LegendsPatternKind.SniperGrid, "AstralLaser", new Color(110, 255, 244), DustID.BlueTorch, "AstralFlame", 126, 20, 16.8f, 6)),
            Phase(
                P("Dark God Outburst", LegendsPatternKind.MinefieldPulse, "DarkEnergyBall", new Color(190, 90, 255), DustID.PurpleTorch, "AstralGodRay", 152, 26, 9.4f, 8),
                P("Astral Glob Rush", LegendsPatternKind.LateralDashBarrage, "AstralFlame", new Color(255, 110, 180), DustID.PinkTorch, "DarkEnergyBall2", 112, 18, 15f, 7),
                P("Twin Blackhole Shear", LegendsPatternKind.VortexPressure, "DarkOrb", new Color(130, 100, 255), DustID.PurpleTorch, "AstralShot2", 146, 22, 10.8f, 8),
                P("Terminal Star Mesh", LegendsPatternKind.ConvergingFan, "AstralLaser", new Color(255, 205, 120), DustID.GoldFlame, "AstralGodRay", 132, 16, 17.5f, 9)));

        private static LegendsAttackProfile[][] CeaselessVoid() => Phases(
            Phase(
                P("Chained Energy Swirl", LegendsPatternKind.OrbitingCrossfire, "DarkEnergyBall", new Color(160, 115, 255), DustID.PurpleTorch, "DarkOrb", 146, 24, 10f, 6),
                P("Mirror Bolt Diagonal", LegendsPatternKind.SniperGrid, "DarkEnergyBall2", new Color(110, 100, 255), DustID.ShadowbeamStaff, "DarkEnergyBall", 132, 26, 15f, 4),
                P("Void Tear Lattice", LegendsPatternKind.MinefieldPulse, "DarkOrb", new Color(120, 72, 210), DustID.PurpleCrystalShard, "DarkEnergyBall2", 150, 34, 7f, 7),
                P("Circular Vortex Spawn", LegendsPatternKind.VortexPressure, "DoGRiftCrack", new Color(190, 132, 255), DustID.PurpleTorch, "DarkEnergyBall", 150, 30, 8.5f, 6)),
            Phase(
                P("Shell Crack Torrent", LegendsPatternKind.SpiralBloom, "DarkEnergyBall2", new Color(180, 130, 255), DustID.ShadowbeamStaff, "DarkOrb", 132, 18, 12.2f, 9),
                P("Event Horizon Pull", LegendsPatternKind.VortexPressure, "DarkOrb", new Color(120, 88, 255), DustID.PurpleCrystalShard, "DarkEnergyBall", 158, 22, 9.8f, 8),
                P("Dungeon Rubble Inversion", LegendsPatternKind.FallingCurtain, "DarkEnergyBall", new Color(155, 135, 255), DustID.Stone, "DarkEnergyBall2", 136, 17, 13f, 8),
                P("Reverse Orbit Bolts", LegendsPatternKind.OrbitingCrossfire, "DarkEnergyBall2", new Color(210, 160, 255), DustID.PurpleTorch, "DarkOrb", 140, 18, 12.8f, 8)),
            Phase(
                P("Broken Chain Ambush", LegendsPatternKind.LateralDashBarrage, "DarkEnergyBall2", new Color(210, 110, 255), DustID.ShadowbeamStaff, "DarkOrb", 112, 18, 15.6f, 7),
                P("Jevil Burst Array", LegendsPatternKind.SpiralBloom, "DarkOrb", new Color(180, 74, 255), DustID.PurpleCrystalShard, "DarkEnergyBall", 126, 13, 14f, 10),
                P("Converging Archive", LegendsPatternKind.ConvergingFan, "DarkEnergyBall", new Color(130, 110, 255), DustID.PurpleTorch, "DarkEnergyBall2", 132, 15, 16.5f, 9),
                P("Absolute Singularity", LegendsPatternKind.MinefieldPulse, "DoGRiftCrack", new Color(230, 170, 255), DustID.ShadowbeamStaff, "DarkOrb", 154, 20, 10.8f, 9)));

        private static LegendsAttackProfile[][] Cryogen() => Phases(
            Phase(
                P("Icicle Circle Burst", LegendsPatternKind.SpiralBloom, "IceBlast", new Color(120, 230, 255), DustID.IceTorch, "IceRain", 126, 20, 10.8f, 7),
                P("Predictive Frost Ring", LegendsPatternKind.OrbitingCrossfire, "IceBomb", new Color(175, 250, 255), DustID.Frost, "IceBlast", 138, 26, 9.5f, 6),
                P("Aurora Shard Curtain", LegendsPatternKind.FallingCurtain, "IceRain", new Color(90, 210, 255), DustID.Ice, "FrostMist", 132, 15, 12.4f, 8),
                P("Permafrost Lattice", LegendsPatternKind.SniperGrid, "PermafrostColdheartIcicle", new Color(200, 255, 255), DustID.SnowflakeIce, "IceBlast", 128, 24, 15f, 5)),
            Phase(
                P("Cryonic Shell Break", LegendsPatternKind.MinefieldPulse, "IceBomb", new Color(150, 235, 255), DustID.IceTorch, "PermafrostBlaster", 146, 28, 8f, 7),
                P("Absolute Zero Sweep", LegendsPatternKind.ConvergingFan, "PermafrostAbsoluteZeroProjectile", new Color(210, 250, 255), DustID.Frost, "IceBlast", 146, 28, 13.6f, 6),
                P("Hail Prism Cross", LegendsPatternKind.SniperGrid, "IceRain", new Color(115, 225, 255), DustID.SnowflakeIce, "IceBlast", 124, 19, 16.2f, 6),
                P("Frozen Orbit Pulse", LegendsPatternKind.VortexPressure, "FrostMist", new Color(90, 240, 255), DustID.Ice, "IceBomb", 150, 24, 9.5f, 7)),
            Phase(
                P("Shattered Core Monsoon", LegendsPatternKind.FallingCurtain, "IceRain", new Color(140, 245, 255), DustID.SnowflakeIce, "IceBlast", 130, 11, 14.2f, 10),
                P("Whiteout Spear Grid", LegendsPatternKind.SniperGrid, "PermafrostColdheartIcicle", new Color(225, 255, 255), DustID.Frost, "PermafrostBlaster", 118, 17, 17.2f, 7),
                P("Rime Vortex Cage", LegendsPatternKind.VortexPressure, "IceBomb", new Color(108, 230, 255), DustID.IceTorch, "FrostMist", 150, 18, 10.8f, 9),
                P("Cryo Cathedral Bloom", LegendsPatternKind.SpiralBloom, "IceBlast", new Color(190, 250, 255), DustID.Ice, "PermafrostAbsoluteZeroProjectile", 132, 12, 15f, 11)));

        private static LegendsAttackProfile[][] HiveMind() => Phases(
            Phase(
                P("Cursed Spawn Arc", LegendsPatternKind.ConvergingFan, "CursedFire", new Color(120, 255, 105), DustID.CursedTorch, "VileClot", 132, 24, 10.6f, 6),
                P("Spin Lunge Spores", LegendsPatternKind.LateralDashBarrage, "MushBomb", new Color(95, 220, 105), DustID.GreenBlood, "ShadeNimbusHostile", 122, 24, 12.6f, 5),
                P("Cloud Dash Rain", LegendsPatternKind.FallingCurtain, "ShaderainHostile", new Color(140, 255, 150), DustID.CursedTorch, "MushBombFall", 132, 16, 11.8f, 8),
                P("Eater Wall Thought", LegendsPatternKind.SniperGrid, "UnstableEbonianGlob", new Color(88, 210, 130), DustID.CorruptGibs, "CursedFire", 136, 27, 14.8f, 4)),
            Phase(
                P("Underground Flame Dash", LegendsPatternKind.LateralDashBarrage, "CursedFire", new Color(155, 255, 90), DustID.CursedTorch, "MushBombGround", 118, 19, 14.2f, 6),
                P("Hive Blob Burst", LegendsPatternKind.SpiralBloom, "VileClot", new Color(120, 235, 110), DustID.GreenBlood, "UnstableEbonianGlob", 128, 16, 12.4f, 9),
                P("Cursed Rain Choir", LegendsPatternKind.FallingCurtain, "ShaderainHostile", new Color(105, 255, 160), DustID.CursedTorch, "CursedFire", 136, 13, 13.2f, 9),
                P("Neural Spore Ring", LegendsPatternKind.OrbitingCrossfire, "MushBomb", new Color(170, 255, 120), DustID.CorruptGibs, "VileClot", 150, 22, 10.8f, 8)),
            Phase(
                P("Final Colony Pulse", LegendsPatternKind.MinefieldPulse, "UnstableEbonianGlob", new Color(170, 255, 120), DustID.GreenBlood, "CursedFire", 150, 20, 9.8f, 10),
                P("Brainstem Crossfire", LegendsPatternKind.OrbitingCrossfire, "CursedFire", new Color(120, 255, 190), DustID.CursedTorch, "ShaderainHostile", 126, 13, 14f, 10),
                P("Putrid Sermon Grid", LegendsPatternKind.SniperGrid, "MushBombFall", new Color(95, 220, 130), DustID.CorruptGibs, "VileClot", 124, 16, 16f, 6),
                P("Hivemaw Bloom", LegendsPatternKind.SpiralBloom, "VileClot", new Color(180, 255, 140), DustID.CursedTorch, "UnstableEbonianGlob", 126, 12, 14.2f, 11)));

        private static LegendsAttackProfile[][] OldDuke() => Phases(
            Phase(
                P("Rotten Harpoon Rush", LegendsPatternKind.LateralDashBarrage, "OldDukeToothBallSpike", new Color(130, 255, 80), DustID.Poisoned, "HomingGasBulb", 112, 24, 13f, 5),
                P("Sulphur Tooth Wheel", LegendsPatternKind.OrbitingCrossfire, "OldDukeToothBallSpike", new Color(170, 255, 90), DustID.ToxicBubble, "OldDukeVortex", 138, 26, 10f, 7),
                P("Toxic Sand Undertow", LegendsPatternKind.FallingCurtain, "SandPoisonCloudOldDuke", new Color(110, 220, 80), DustID.GreenTorch, "ToxicCloud", 132, 18, 11.6f, 8),
                P("Sharkbone Crosscut", LegendsPatternKind.SniperGrid, "SandBlast", new Color(210, 255, 120), DustID.PoisonStaff, "OldDukeToothBallSpike", 124, 26, 15.4f, 4)),
            Phase(
                P("Bloodworm Afterbite", LegendsPatternKind.ConvergingFan, "HomingGasBulb", new Color(155, 255, 90), DustID.Poisoned, "OldDukeToothBallSpike", 128, 20, 13.2f, 7),
                P("Vortex Jaw Fakeout", LegendsPatternKind.VortexPressure, "OldDukeVortex", new Color(100, 255, 130), DustID.ToxicBubble, "SandPoisonCloudOldDuke", 150, 24, 10.5f, 8),
                P("Acid Carcass Rain", LegendsPatternKind.FallingCurtain, "MaulerAcidDrop", new Color(190, 255, 100), DustID.GreenTorch, "ToxicCloud", 134, 14, 13.4f, 9),
                P("Seven-Gill Dash Chain", LegendsPatternKind.LateralDashBarrage, "OldDukeToothBallSpike", new Color(140, 255, 70), DustID.PoisonStaff, "HomingGasBulb", 108, 18, 15f, 6)),
            Phase(
                P("Grand Sulphur Maw", LegendsPatternKind.SpiralBloom, "OldDukeToothBallSpike", new Color(190, 255, 80), DustID.ToxicBubble, "HomingGasBulb", 124, 12, 14.2f, 11),
                P("Cadaver Reef Collapse", LegendsPatternKind.ConvergingFan, "SandPoisonCloudOldDuke", new Color(120, 255, 115), DustID.Poisoned, "OldDukeVortex", 140, 15, 15.8f, 8),
                P("Mirewall Evisceration", LegendsPatternKind.SniperGrid, "MaulerAcidBubble", new Color(210, 255, 105), DustID.GreenTorch, "OldDukeToothBallSpike", 118, 15, 17.2f, 7),
                P("Last Rotten Current", LegendsPatternKind.VortexPressure, "OldDukeVortex", new Color(160, 255, 95), DustID.PoisonStaff, "ToxicCloud", 150, 18, 11.6f, 10)));

        private static LegendsAttackProfile[][] Perforators() => Phases(
            Phase(
                P("Diagonal Blood Charge", LegendsPatternKind.LateralDashBarrage, "BloodGeyser", new Color(255, 85, 95), DustID.Blood, "IchorShot", 118, 24, 12.6f, 5),
                P("Crimera Wall Signal", LegendsPatternKind.SniperGrid, "UnstableCrimulanGlob", new Color(255, 70, 105), DustID.CrimsonTorch, "IchorBlob", 132, 28, 14.8f, 4),
                P("Ichor Blast Choir", LegendsPatternKind.ConvergingFan, "IchorShot", new Color(255, 210, 80), DustID.Ichor, "IchorBlob", 124, 20, 12.8f, 7),
                P("Blood Spiral Dash", LegendsPatternKind.SpiralBloom, "VileClot", new Color(255, 95, 120), DustID.Blood, "IchorShot", 136, 20, 11.8f, 8)),
            Phase(
                P("Small Worm Vent", LegendsPatternKind.FallingCurtain, "IchorBlob", new Color(255, 195, 70), DustID.Ichor, "BloodGeyser", 136, 14, 12.6f, 9),
                P("Ichor Rain Replacement", LegendsPatternKind.FallingCurtain, "IchorShot", new Color(255, 225, 95), DustID.Ichor, "UnstableCrimulanGlob", 130, 12, 13.8f, 10),
                P("Medium Tooth Ball Echo", LegendsPatternKind.OrbitingCrossfire, "IchorBlob", new Color(255, 105, 105), DustID.Blood, "VileClot", 146, 20, 11.4f, 9),
                P("Crimson Fountain Charge", LegendsPatternKind.LateralDashBarrage, "BloodGeyser", new Color(255, 65, 85), DustID.CrimsonTorch, "IchorShot", 112, 18, 15.2f, 7)),
            Phase(
                P("Large Worm Rupture", LegendsPatternKind.MinefieldPulse, "UnstableCrimulanGlob", new Color(255, 85, 80), DustID.Blood, "IchorBlob", 150, 18, 10.4f, 10),
                P("Hemorrhage Sunflower", LegendsPatternKind.SpiralBloom, "BloodGeyser", new Color(255, 115, 120), DustID.Blood, "IchorShot", 124, 12, 14f, 11),
                P("Golden Clot Clamp", LegendsPatternKind.ConvergingFan, "IchorShot", new Color(255, 230, 88), DustID.Ichor, "VileClot", 124, 14, 16.2f, 9),
                P("Perforator Hive Finale", LegendsPatternKind.OrbitingCrossfire, "IchorBlob", new Color(255, 70, 95), DustID.CrimsonTorch, "UnstableCrimulanGlob", 132, 13, 14.8f, 11)));

        private static LegendsAttackProfile[][] PlaguebringerGoliath() => Phases(
            Phase(
                P("Plague Jet Charge", LegendsPatternKind.LateralDashBarrage, "PlagueStingerGoliath", new Color(185, 255, 70), DustID.GreenFairy, "PlagueStingerGoliathV2", 112, 22, 13.2f, 5),
                P("Stinger Missile Choir", LegendsPatternKind.ConvergingFan, "PlagueStingerGoliathV2", new Color(155, 255, 85), DustID.Poisoned, "HiveBombGoliath", 130, 21, 12.6f, 7),
                P("Carpet Bomb Survey", LegendsPatternKind.FallingCurtain, "HiveBombGoliath", new Color(210, 255, 75), DustID.PoisonStaff, "PlagueExplosion", 136, 18, 11.4f, 8),
                P("Viral Drone Orbit", LegendsPatternKind.OrbitingCrossfire, "BasicPlagueBee", new Color(145, 255, 100), DustID.GreenTorch, "PlagueStingerGoliath", 150, 24, 10.8f, 8)),
            Phase(
                P("Explosive Charger Line", LegendsPatternKind.SniperGrid, "PlagueExplosion", new Color(190, 255, 90), DustID.PoisonStaff, "PlagueStingerGoliath", 126, 25, 15.8f, 5),
                P("Nuclear Assembly Ring", LegendsPatternKind.MinefieldPulse, "HiveNuke", new Color(220, 255, 70), DustID.GreenFairy, "PlagueExplosion", 152, 28, 8f, 8),
                P("Virulence Spiral", LegendsPatternKind.SpiralBloom, "PlagueStingerGoliathV2", new Color(150, 255, 80), DustID.Poisoned, "PlaguePulse", 126, 15, 13f, 10),
                P("Builder Drone Crossfire", LegendsPatternKind.OrbitingCrossfire, "BasicPlagueBee", new Color(185, 255, 110), DustID.GreenTorch, "HiveBombGoliath", 142, 18, 12.2f, 9)),
            Phase(
                P("Nuke Bloom Detonation", LegendsPatternKind.MinefieldPulse, "HiveNuke", new Color(230, 255, 70), DustID.PoisonStaff, "PlagueExplosion", 150, 18, 10.6f, 10),
                P("Plague Queen Grid", LegendsPatternKind.SniperGrid, "PlagueStingerGoliath", new Color(160, 255, 80), DustID.GreenFairy, "PlagueStingerGoliathV2", 116, 15, 17.6f, 7),
                P("Toxic Hive Compression", LegendsPatternKind.VortexPressure, "PlaguePulse", new Color(140, 255, 95), DustID.Poisoned, "BasicPlagueBee", 150, 18, 11.2f, 10),
                P("Biohazard Crown", LegendsPatternKind.SpiralBloom, "HiveBombGoliath", new Color(210, 255, 95), DustID.GreenTorch, "PlagueStingerGoliathV2", 126, 12, 15f, 12)));

        private static LegendsAttackProfile[][] Polterghast() => Phases(
            Phase(
                P("Ectoplasm Uppercut", LegendsPatternKind.LateralDashBarrage, "PhantomBlast", new Color(105, 230, 255), DustID.BlueTorch, "PhantomShot", 118, 24, 12.8f, 5),
                P("Leg Swipe Vortex", LegendsPatternKind.VortexPressure, "PhantomMine", new Color(135, 210, 255), DustID.Ghost, "PhantomBlast2", 146, 28, 8.8f, 7),
                P("Wisp Circle Charge", LegendsPatternKind.OrbitingCrossfire, "PhantomGhostShot", new Color(165, 235, 255), DustID.BlueTorch, "PhantomShot2", 138, 21, 11.6f, 8),
                P("Spirit Petal", LegendsPatternKind.SpiralBloom, "GhastlySoulSmall", new Color(120, 255, 235), DustID.Ghost, "PhantomBlast", 132, 18, 12.2f, 9)),
            Phase(
                P("Asgore Soul Ring", LegendsPatternKind.ConvergingFan, "PhantomGhostShot", new Color(160, 230, 255), DustID.BlueTorch, "GhastlySoulMedium", 142, 19, 14.2f, 8),
                P("Arcing Soul Knives", LegendsPatternKind.SniperGrid, "SignusScythe", new Color(125, 210, 255), DustID.Ghost, "PhantomShot", 126, 22, 16.2f, 5),
                P("Vortex Charge Hymn", LegendsPatternKind.LateralDashBarrage, "PhantomBlast2", new Color(100, 245, 255), DustID.BlueTorch, "PhantomMine", 112, 18, 15f, 7),
                P("Nonreturning Soul Bloom", LegendsPatternKind.SpiralBloom, "GhastlySoulMedium", new Color(190, 255, 245), DustID.Ghost, "PhantomGhostShot", 130, 14, 14f, 10)),
            Phase(
                P("Clone Split Cross", LegendsPatternKind.SniperGrid, "PhantomBlast", new Color(95, 235, 255), DustID.BlueTorch, "SignusScythe", 112, 15, 17f, 8),
                P("Desperation Safety Ring", LegendsPatternKind.VortexPressure, "PhantomMine", new Color(150, 225, 255), DustID.Ghost, "GhastlySoulLarge", 150, 16, 11f, 10),
                P("Cathedral Soul Spiral", LegendsPatternKind.SpiralBloom, "GhastlySoulLarge", new Color(185, 255, 255), DustID.BlueTorch, "PhantomGhostShot", 128, 12, 15f, 12),
                P("Terminal Ectoplasm Grid", LegendsPatternKind.ConvergingFan, "PhantomBlast2", new Color(110, 230, 255), DustID.Ghost, "PhantomShot2", 128, 13, 16.5f, 10)));

        private static LegendsAttackProfile[][] Providence() => Phases(
            Phase(
                P("Fire Energy Charge", LegendsPatternKind.LateralDashBarrage, "HolyFire", new Color(255, 210, 100), DustID.GoldFlame, "HolyFlare", 126, 24, 12.2f, 5),
                P("Cinder Bomb Barrage", LegendsPatternKind.FallingCurtain, "HolyBomb", new Color(255, 170, 80), DustID.OrangeTorch, "HolyFire2", 138, 18, 11.8f, 8),
                P("Crystal Fan", LegendsPatternKind.ConvergingFan, "ProvidenceCrystalShard", new Color(255, 230, 160), DustID.GoldFlame, "ProvidenceCrystal", 128, 20, 13.6f, 7),
                P("Guardian Spear Order", LegendsPatternKind.SniperGrid, "ProfanedSpear", new Color(255, 245, 180), DustID.YellowTorch, "HolySpear", 132, 26, 15.5f, 4)),
            Phase(
                P("Cleansing Fire Bombardment", LegendsPatternKind.FallingCurtain, "HolyFire2", new Color(255, 190, 70), DustID.OrangeTorch, "HolyBomb", 132, 13, 13.2f, 10),
                P("Exploding Spear Chapel", LegendsPatternKind.SniperGrid, "HolySpear", new Color(255, 235, 140), DustID.GoldFlame, "ProfanedSpear", 126, 17, 16.6f, 7),
                P("Rock Magic Ritual", LegendsPatternKind.OrbitingCrossfire, "ProvidenceCrystal", new Color(255, 210, 150), DustID.YellowTorch, "ProvidenceCrystalShard", 150, 20, 11.4f, 9),
                P("Dogma Laser Omen", LegendsPatternKind.ConvergingFan, "ProvidenceHolyRay", new Color(255, 245, 190), DustID.GoldFlame, "HolyBlast", 142, 26, 15.8f, 6)),
            Phase(
                P("Radiance Burst", LegendsPatternKind.SpiralBloom, "HolyBlast", new Color(255, 240, 160), DustID.GoldFlame, "HolyFire", 124, 12, 15f, 12),
                P("Dying Sun Curtain", LegendsPatternKind.FallingCurtain, "DyingSun", new Color(255, 180, 95), DustID.OrangeTorch, "HolyBomb", 150, 24, 10.8f, 8),
                P("Profaned Core Cage", LegendsPatternKind.VortexPressure, "HolyProfanedCore", new Color(255, 225, 120), DustID.YellowTorch, "ProvidenceCrystalShard", 150, 18, 11.5f, 10),
                P("Final Liturgy Cross", LegendsPatternKind.SniperGrid, "ProvidenceHolyRay", new Color(255, 250, 205), DustID.GoldFlame, "HolySpear", 122, 13, 18f, 8)));

        private static LegendsAttackProfile[][] Signus() => Phases(
            Phase(
                P("Kunai Phase Dash", LegendsPatternKind.LateralDashBarrage, "DarkEnergyBall", new Color(205, 125, 255), DustID.PurpleTorch, "SignusScythe", 112, 22, 13.2f, 5),
                P("Scythe Teleport Throw", LegendsPatternKind.SniperGrid, "SignusScythe", new Color(180, 95, 255), DustID.ShadowbeamStaff, "DarkOrb", 126, 24, 15.2f, 4),
                P("Shadow Dash Mark", LegendsPatternKind.MinefieldPulse, "DarkOrb", new Color(150, 90, 255), DustID.PurpleCrystalShard, "DarkEnergyBall2", 142, 28, 8.8f, 7),
                P("Cosmic Flame Charge", LegendsPatternKind.ConvergingFan, "CosmicFire", new Color(215, 140, 255), DustID.PurpleTorch, "DarkEnergyBall", 130, 21, 12.8f, 7)),
            Phase(
                P("Eldritch Knife Wall", LegendsPatternKind.SniperGrid, "SignusScythe", new Color(215, 110, 255), DustID.ShadowbeamStaff, "DarkEnergyBall", 118, 17, 17f, 7),
                P("Wormhole Mine Dial", LegendsPatternKind.OrbitingCrossfire, "DarkOrb", new Color(155, 105, 255), DustID.PurpleCrystalShard, "DarkEnergyBall2", 142, 18, 12.8f, 9),
                P("Fast Horizontal Null", LegendsPatternKind.LateralDashBarrage, "DarkEnergyBall2", new Color(190, 125, 255), DustID.PurpleTorch, "SignusScythe", 106, 16, 16f, 7),
                P("Cosmic Bomb Descent", LegendsPatternKind.FallingCurtain, "DarkEnergyBall", new Color(230, 150, 255), DustID.ShadowbeamStaff, "CosmicFire", 130, 14, 13.8f, 9)),
            Phase(
                P("Eventide Shadow Grid", LegendsPatternKind.SniperGrid, "SignusScythe", new Color(230, 135, 255), DustID.PurpleTorch, "DarkEnergyBall2", 110, 13, 18.2f, 8),
                P("Envoy Spiral Bombs", LegendsPatternKind.SpiralBloom, "DarkOrb", new Color(180, 95, 255), DustID.ShadowbeamStaff, "CosmicFire", 124, 12, 15f, 11),
                P("Dimensional Collapse", LegendsPatternKind.VortexPressure, "DarkEnergyBall", new Color(205, 110, 255), DustID.PurpleCrystalShard, "DarkOrb", 150, 16, 11.8f, 10),
                P("Terminal Kunai Eclipse", LegendsPatternKind.ConvergingFan, "DarkEnergyBall2", new Color(245, 160, 255), DustID.PurpleTorch, "SignusScythe", 118, 12, 17f, 10)));

        private static LegendsAttackProfile[][] StormWeaver() => Phases(
            Phase(
                P("Aimed Lightning Bolts", LegendsPatternKind.SniperGrid, "RedLightning", new Color(145, 235, 255), DustID.Electric, "HomingLaserDart", 126, 26, 15.6f, 4),
                P("Ice Storm Channel", LegendsPatternKind.FallingCurtain, "IceRain", new Color(120, 230, 255), DustID.IceTorch, "StormWeaverFrostWaveTelegraph", 138, 18, 12.4f, 8),
                P("Fakeout Charge Spark", LegendsPatternKind.LateralDashBarrage, "DestroyerElectricLaser", new Color(185, 245, 255), DustID.Electric, "RedLightning", 112, 22, 14f, 5),
                P("Wind Gust Corridor", LegendsPatternKind.VortexPressure, "StormMarkHostile", new Color(180, 255, 230), DustID.Cloud, "HomingLaserDart", 150, 26, 8.8f, 7)),
            Phase(
                P("Fog Sneak Charge", LegendsPatternKind.LateralDashBarrage, "RedLightning", new Color(150, 240, 255), DustID.Electric, "StormMarkHostile", 106, 18, 15.8f, 7),
                P("Berdly Wind Shear", LegendsPatternKind.OrbitingCrossfire, "StormMarkHostile", new Color(190, 255, 240), DustID.Cloud, "DestroyerElectricLaser", 142, 18, 12f, 9),
                P("Spark Segment Burst", LegendsPatternKind.SpiralBloom, "HomingLaserDart", new Color(130, 235, 255), DustID.Electric, "RedLightning", 124, 14, 14.2f, 10),
                P("Thunderhead Crosscut", LegendsPatternKind.SniperGrid, "DestroyerElectricLaser", new Color(210, 250, 255), DustID.Electric, "IceBlast", 118, 15, 17f, 7)),
            Phase(
                P("White Squall Labyrinth", LegendsPatternKind.FallingCurtain, "IceRain", new Color(190, 250, 255), DustID.IceTorch, "RedLightning", 128, 11, 14.6f, 11),
                P("Arc Current Vortex", LegendsPatternKind.VortexPressure, "StormMarkHostile", new Color(150, 255, 230), DustID.Cloud, "HomingLaserDart", 150, 15, 11f, 10),
                P("Lightning Spine Net", LegendsPatternKind.ConvergingFan, "RedLightning", new Color(170, 240, 255), DustID.Electric, "DestroyerElectricLaser", 120, 12, 17.2f, 10),
                P("Skybreak Finale", LegendsPatternKind.SniperGrid, "HomingLaserDart", new Color(220, 255, 255), DustID.Electric, "RedLightning", 112, 11, 18.4f, 8)));

        private static LegendsAttackProfile[][] Yharon() => Phases(
            Phase(
                P("Rebirth Flame Rush", LegendsPatternKind.LateralDashBarrage, "YharonFireball", new Color(255, 160, 70), DustID.Torch, "BigFlare", 112, 22, 13.6f, 5),
                P("Dragon Blossom Ring", LegendsPatternKind.OrbitingCrossfire, "MajesticSparkle", new Color(255, 220, 90), DustID.GoldFlame, "YharonFireball2", 140, 22, 10.8f, 8),
                P("Infernado Gate", LegendsPatternKind.VortexPressure, "Infernado", new Color(255, 135, 55), DustID.Flare, "YharonFireball", 150, 34, 8.8f, 6),
                P("Solar Feather Grid", LegendsPatternKind.SniperGrid, "RedLightningFeather", new Color(255, 190, 90), DustID.OrangeTorch, "SkyFlareRevenge", 128, 26, 15.4f, 4)),
            Phase(
                P("Vortex Fireball Wreath", LegendsPatternKind.SpiralBloom, "YharonFireball2", new Color(255, 180, 70), DustID.Flare, "BigFlare2", 124, 16, 13.8f, 10),
                P("Majestic Spark Chorus", LegendsPatternKind.OrbitingCrossfire, "MajesticSparkle", new Color(255, 225, 110), DustID.GoldFlame, "YharonFireball", 132, 16, 13.2f, 10),
                P("Meteor Redirect Chain", LegendsPatternKind.ConvergingFan, "SkyFlareRevenge", new Color(255, 140, 65), DustID.OrangeTorch, "YharonFireball2", 126, 18, 16f, 8),
                P("Draconic Heat Flash", LegendsPatternKind.FallingCurtain, "BigFlare", new Color(255, 105, 50), DustID.Torch, "Infernado2", 136, 17, 13.6f, 9)),
            Phase(
                P("Grand Yharon Spiral", LegendsPatternKind.SpiralBloom, "YharonFireball2", new Color(255, 190, 85), DustID.GoldFlame, "MajesticSparkle", 118, 10, 15.6f, 13),
                P("Dragonfire Compression", LegendsPatternKind.VortexPressure, "Infernado2", new Color(255, 130, 55), DustID.Flare, "BigFlare2", 150, 14, 12f, 11),
                P("Phoenix Crown Barrage", LegendsPatternKind.FallingCurtain, "SkyFlareRevenge", new Color(255, 215, 100), DustID.OrangeTorch, "YharonFireball", 126, 10, 16f, 11),
                P("Rebirth Singularity", LegendsPatternKind.ConvergingFan, "YharonFireball2", new Color(255, 235, 120), DustID.GoldFlame, "BigFlare", 118, 10, 18f, 12)));

        private static LegendsAttackProfile[][] Default() => Phases(
            Phase(
                P("Matrix Hover", LegendsPatternKind.OrbitingCrossfire, "AstralShot2", new Color(88, 255, 211), DustID.Electric),
                P("Vector Dash", LegendsPatternKind.LateralDashBarrage, "AstralLaser", new Color(88, 255, 211), DustID.Electric),
                P("Orbit Lock", LegendsPatternKind.VortexPressure, "DarkEnergyBall", new Color(88, 255, 211), DustID.Electric),
                P("Phase Pressure", LegendsPatternKind.ConvergingFan, "AstralShot2", new Color(88, 255, 211), DustID.Electric)));

        private static LegendsAttackProfile[][] Phases(params LegendsAttackProfile[][] phases) => phases;

        private static LegendsAttackProfile[] Phase(params LegendsAttackProfile[] profiles) => profiles;

        private static LegendsAttackProfile P(
            string name,
            LegendsPatternKind kind,
            string primaryProjectile,
            Color color,
            int dustType,
            string secondaryProjectile = null,
            int duration = 150,
            int fireRate = 24,
            float speed = 10f,
            int count = 4,
            float spread = 0.58f)
        {
            return new LegendsAttackProfile(name, kind, primaryProjectile, color, dustType, secondaryProjectile, duration, fireRate, speed, count, spread);
        }
    }
}
