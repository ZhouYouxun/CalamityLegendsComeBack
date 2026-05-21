using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.Yharon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal enum PristineFuryMark
    {
        Idle = 0,
        EvilT2 = 1,
        SlimeGod = 2,
        HardMode = 3,
        Prime = 4,
        BrimstoneElemental = 5,
        Plantera = 6,
        Aurora = 7,
        Goliath = 8,
        Moonlord = 9,
        Providence = 10,
        Polterghast = 11,
        Dog = 12,
        Dragon = 13,
        FakeCalamity = 14,
        ExoTwins = 15,
        ExoThanatos = 16,
        ExoAres = 17
    }

    internal static class PristineFuryMarkHelper
    {
        internal static bool TryGetMarkFromNPC(NPC target, out PristineFuryMark mark)
        {
            mark = PristineFuryMark.Idle;

            if (target.type == ModContent.NPCType<SlimeGodCore>())
            {
                mark = PristineFuryMark.SlimeGod;
                return true;
            }

            if (target.type == ModContent.NPCType<BrimstoneElemental>())
            {
                mark = PristineFuryMark.BrimstoneElemental;
                return true;
            }

            if (target.type == ModContent.NPCType<CalamitasClone>())
            {
                mark = PristineFuryMark.FakeCalamity;
                return true;
            }

            if (target.type == ModContent.NPCType<AstrumAureus>())
            {
                mark = PristineFuryMark.Aurora;
                return true;
            }

            if (target.type == ModContent.NPCType<PlaguebringerGoliath>())
            {
                mark = PristineFuryMark.Goliath;
                return true;
            }

            if (target.type == ModContent.NPCType<Providence>())
            {
                mark = PristineFuryMark.Providence;
                return true;
            }

            if (target.type == ModContent.NPCType<Polterghast>())
            {
                mark = PristineFuryMark.Polterghast;
                return true;
            }

            if (target.type == ModContent.NPCType<DevourerofGodsHead>() ||
                target.type == ModContent.NPCType<DevourerofGodsBody>() ||
                target.type == ModContent.NPCType<DevourerofGodsTail>())
            {
                mark = PristineFuryMark.Dog;
                return true;
            }

            if (target.type == ModContent.NPCType<Yharon>())
            {
                mark = PristineFuryMark.Dragon;
                return true;
            }

            if (target.type == ModContent.NPCType<Apollo>() ||
                target.type == ModContent.NPCType<Artemis>())
            {
                mark = PristineFuryMark.ExoTwins;
                return true;
            }

            if (target.type == ModContent.NPCType<ThanatosHead>() ||
                target.type == ModContent.NPCType<ThanatosBody1>() ||
                target.type == ModContent.NPCType<ThanatosBody2>() ||
                target.type == ModContent.NPCType<ThanatosTail>())
            {
                mark = PristineFuryMark.ExoThanatos;
                return true;
            }

            if (target.type == ModContent.NPCType<AresBody>() ||
                target.type == ModContent.NPCType<AresGaussNuke>() ||
                target.type == ModContent.NPCType<AresLaserCannon>() ||
                target.type == ModContent.NPCType<AresPlasmaFlamethrower>() ||
                target.type == ModContent.NPCType<AresTeslaCannon>())
            {
                mark = PristineFuryMark.ExoAres;
                return true;
            }

            if (target.type == NPCID.WallofFlesh || target.type == NPCID.WallofFleshEye)
            {
                mark = PristineFuryMark.HardMode;
                return true;
            }

            if (target.type == NPCID.Plantera)
            {
                mark = PristineFuryMark.Plantera;
                return true;
            }

            if (target.type == NPCID.MoonLordCore ||
                target.type == NPCID.MoonLordHand ||
                target.type == NPCID.MoonLordHead)
            {
                mark = PristineFuryMark.Moonlord;
                return true;
            }

            if (target.type == NPCID.SkeletronPrime)
            {
                mark = PristineFuryMark.Prime;
                return true;
            }

            if (target.type == NPCID.EaterofWorldsHead ||
                target.type == NPCID.EaterofWorldsBody ||
                target.type == NPCID.EaterofWorldsTail ||
                target.type == NPCID.BrainofCthulhu)
            {
                mark = PristineFuryMark.EvilT2;
                return true;
            }

            return false;
        }

        internal static string GetName(PristineFuryMark mark) =>
            Language.GetTextValue($"Mods.CalamityLegendsComeBack.PristineFury.Marks.{mark}");

        internal static Color GetColor(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.EvilT2 => new Color(210, 58, 235),
                PristineFuryMark.SlimeGod => new Color(188, 78, 255),
                PristineFuryMark.HardMode => new Color(255, 142, 66),
                PristineFuryMark.Prime => new Color(255, 206, 92),
                PristineFuryMark.BrimstoneElemental => new Color(246, 55, 64),
                PristineFuryMark.Plantera => new Color(94, 242, 108),
                PristineFuryMark.Aurora => new Color(126, 210, 255),
                PristineFuryMark.Goliath => new Color(139, 242, 73),
                PristineFuryMark.Moonlord => new Color(126, 156, 255),
                PristineFuryMark.Providence => new Color(255, 220, 118),
                PristineFuryMark.Polterghast => new Color(210, 136, 255),
                PristineFuryMark.Dog => new Color(96, 73, 214),
                PristineFuryMark.Dragon => new Color(255, 108, 50),
                PristineFuryMark.FakeCalamity => new Color(255, 66, 54),
                PristineFuryMark.ExoTwins => new Color(74, 255, 180),
                PristineFuryMark.ExoThanatos => new Color(102, 162, 255),
                PristineFuryMark.ExoAres => new Color(255, 76, 64),
                _ => new Color(255, 146, 62)
            };
        }

        internal static int GetDisplayedBaseDamage(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.SlimeGod => 95,
                PristineFuryMark.HardMode => 118,
                PristineFuryMark.Prime => 120,
                PristineFuryMark.BrimstoneElemental => 175,
                PristineFuryMark.Plantera => 210,
                PristineFuryMark.Aurora => 205,
                PristineFuryMark.Goliath => 230,
                PristineFuryMark.Moonlord => 270,
                PristineFuryMark.Providence => 420,
                PristineFuryMark.Polterghast => 285,
                PristineFuryMark.Dog => 330,
                PristineFuryMark.Dragon => 360,
                PristineFuryMark.ExoTwins => 390,
                PristineFuryMark.ExoThanatos => 460,
                PristineFuryMark.ExoAres => 430,
                PristineFuryMark.EvilT2 => 88,
                PristineFuryMark.FakeCalamity => 190,
                _ => 77
            };
        }
    }
}
