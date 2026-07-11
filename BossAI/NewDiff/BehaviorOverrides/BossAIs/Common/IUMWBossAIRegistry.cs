using System.Collections.Generic;
using System;
using Terraria.ModLoader;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumAureus;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumDeus;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.CeaselessVoid;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.Cryogen;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage1.HiveMind;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.OldDuke;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage1.Perforators;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.PlaguebringerGoliath;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Polterghast;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Providence;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Signus;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.StormWeaver;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage5.Yharon;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.CalamitasClone;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.LeviathanAnahita;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.Ravager;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Dragonfolly;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    internal static class IUMWBossAIRegistry
    {
        private static Dictionary<int, IUMWBossAI> aiByNPCType = new();

        public static void Load()
        {
            aiByNPCType = new Dictionary<int, IUMWBossAI>();

            Register(new YharonIUMWAI());
            Register(new OldDukeAI());
            Register(new PolterghastAI());
            var stormWeaver = new StormWeaverAI();
            Register(stormWeaver);
            if (ModContent.TryFind("CalamityMod/StormWeaverBody", out ModNPC stormWeaverBody))
                aiByNPCType[stormWeaverBody.Type] = stormWeaver;
            if (ModContent.TryFind("CalamityMod/StormWeaverTail", out ModNPC stormWeaverTail))
                aiByNPCType[stormWeaverTail.Type] = stormWeaver;
            Register(new CeaselessVoidAI());
            Register(new SignusAI());
            Register(new ProvidenceAI());
            var astrumDeus = new AstrumDeusAI();
            Register(astrumDeus);
            aiByNPCType[ModContent.NPCType<CalamityMod.NPCs.AstrumDeus.AstrumDeusBody>()] = astrumDeus;
            aiByNPCType[ModContent.NPCType<CalamityMod.NPCs.AstrumDeus.AstrumDeusTail>()] = astrumDeus;
            Register(new PlaguebringerGoliathAI());
            Register(new AstrumAureusAI());
            Register(new CryogenIUMWAI());
            var aquaticScourge = new AquaticScourgeAI();
            Register(aquaticScourge);
            if (ModContent.TryFind("CalamityMod/AquaticScourgeBody", out ModNPC scourgeBody))
                aiByNPCType[scourgeBody.Type] = aquaticScourge;
            if (ModContent.TryFind("CalamityMod/AquaticScourgeBodyAlt", out ModNPC scourgeBodyAlt))
                aiByNPCType[scourgeBodyAlt.Type] = aquaticScourge;
            if (ModContent.TryFind("CalamityMod/AquaticScourgeTail", out ModNPC scourgeTail))
                aiByNPCType[scourgeTail.Type] = aquaticScourge;
            Register(new HiveMindIUMWAI());
            Register(new PerforatorsIUMWAI());
            Register(new CalamitasCloneAI());
            var leviathanAnahita = new LeviathanAnahitaAI();
            Register(leviathanAnahita);
            if (ModContent.TryFind("CalamityMod/Anahita", out ModNPC anahita))
                aiByNPCType[anahita.Type] = leviathanAnahita;
            Register(new RavagerAI());
            Register(new DragonfollyAI());

            IUMWWeaponBossRegistry.Load();
        }

        public static void Unload()
        {
            IUMWWeaponBossRegistry.Unload();
            aiByNPCType = null;
        }

        public static bool TryGetAI(int npcType, out IUMWBossAI ai)
        {
            ai = null;
            return aiByNPCType?.TryGetValue(npcType, out ai) == true;
        }

        private static void Register(IUMWBossAI ai)
        {
            try
            {
                aiByNPCType[ai.NPCType] = ai;
            }
            catch (Exception)
            {
            }
        }
    }
}
