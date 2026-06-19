using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.Yharon;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal enum PristineFuryMark
    {
        Idle = 0,
        DesertScourge = 1,
        EyeOfCthulhu = 2,
        Skeletron = 3,
        EvilT2 = 4,
        SlimeGod = 5,
        HardMode = 6,
        BrimstoneElemental = 7,
        Prime = 8,
        FakeCalamity = 9,
        Plantera = 10,
        Golem = 11,
        Goliath = 12,
        Empress = 13,
        Moonlord = 14,
        Providence = 15,
        Polterghast = 16,
        Dog = 17,
        Dragon = 18,
        ExoTwins = 19,
        ExoThanatos = 20,
        ExoAres = 21,
        Ravager = 22
    }

    internal static class PristineFuryMarkHelper
    {
        internal static bool TryGetMarkFromNPC(NPC target, out PristineFuryMark mark)
        {
            mark = PristineFuryMark.Idle;

            if (target.type == ModContent.NPCType<DesertScourgeHead>() ||
                target.type == ModContent.NPCType<DesertScourgeBody>() ||
                target.type == ModContent.NPCType<DesertScourgeTail>())
            {
                mark = PristineFuryMark.DesertScourge;
                return true;
            }

            if (target.type == NPCID.EyeofCthulhu)
            {
                mark = PristineFuryMark.EyeOfCthulhu;
                return true;
            }

            if (target.type == NPCID.SkeletronHead || target.type == NPCID.SkeletronHand)
            {
                mark = PristineFuryMark.Skeletron;
                return true;
            }

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

            if (IsGolemTarget(target))
            {
                mark = PristineFuryMark.Golem;
                return true;
            }

            if (target.type == ModContent.NPCType<PlaguebringerGoliath>())
                return false;

            if (IsProfanedGuardianTarget(target))
            {
                mark = PristineFuryMark.Providence;
                return true;
            }

            if (IsRavagerTarget(target))
            {
                if (!DownedBossSystem.downedProvidence)
                    return false;

                mark = PristineFuryMark.Ravager;
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

            if (target.type == ModContent.NPCType<ThanatosHead>() ||
                target.type == ModContent.NPCType<ThanatosBody1>() ||
                target.type == ModContent.NPCType<ThanatosBody2>() ||
                target.type == ModContent.NPCType<ThanatosTail>())
            {
                return false;
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

            if (target.type == NPCID.HallowBoss)
            {
                mark = PristineFuryMark.Empress;
                return true;
            }

            if (target.type == NPCID.MoonLordCore ||
                target.type == NPCID.MoonLordHand ||
                target.type == NPCID.MoonLordHead)
            {
                mark = PristineFuryMark.Moonlord;
                return true;
            }

            if (IsMechanicalBossTarget(target))
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

        internal static bool IsRavagerTarget(NPC target) =>
            target.type == ModContent.NPCType<RavagerBody>() ||
            target.type == ModContent.NPCType<RavagerHead>() ||
            target.type == ModContent.NPCType<RavagerHead2>() ||
            target.type == ModContent.NPCType<RavagerClawLeft>() ||
            target.type == ModContent.NPCType<RavagerClawRight>() ||
            target.type == ModContent.NPCType<RavagerLegLeft>() ||
            target.type == ModContent.NPCType<RavagerLegRight>();

        private static bool IsMechanicalBossTarget(NPC target) =>
            target.type == NPCID.SkeletronPrime ||
            target.type == NPCID.PrimeCannon ||
            target.type == NPCID.PrimeLaser ||
            target.type == NPCID.PrimeSaw ||
            target.type == NPCID.PrimeVice ||
            target.type == NPCID.Retinazer ||
            target.type == NPCID.Spazmatism ||
            target.type == NPCID.TheDestroyer ||
            target.type == NPCID.TheDestroyerBody ||
            target.type == NPCID.TheDestroyerTail;

        private static bool IsGolemTarget(NPC target) =>
            target.type == NPCID.Golem ||
            target.type == NPCID.GolemHead ||
            target.type == NPCID.GolemHeadFree ||
            target.type == NPCID.GolemFistLeft ||
            target.type == NPCID.GolemFistRight;

        private static bool IsProfanedGuardianTarget(NPC target) =>
            target.type == ModContent.NPCType<ProfanedGuardianCommander>() ||
            target.type == ModContent.NPCType<ProfanedGuardianDefender>() ||
            target.type == ModContent.NPCType<ProfanedGuardianHealer>();

        internal static bool IsProvidenceLockedRavager(NPC target) =>
            IsRavagerTarget(target) && !DownedBossSystem.downedProvidence;

        internal static string GetName(PristineFuryMark mark) =>
            Language.GetTextValue($"Mods.CalamityLegendsComeBack.PristineFury.Marks.{mark}");

        internal static Color GetColor(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.DesertScourge => new Color(240, 214, 145),
                PristineFuryMark.EyeOfCthulhu => new Color(228, 72, 72),
                PristineFuryMark.Skeletron => new Color(220, 218, 196),
                PristineFuryMark.EvilT2 => WorldGen.crimson ? new Color(240, 40, 70) : new Color(160, 60, 240),
                PristineFuryMark.Plantera => new Color(0, 255, 180),
                PristineFuryMark.Golem => new Color(255, 190, 54),
                PristineFuryMark.Goliath => new Color(130, 255, 60),
                PristineFuryMark.Empress => Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.24f) % 1f, 0.88f, 0.58f),
                PristineFuryMark.Moonlord => new Color(100, 240, 220),
                PristineFuryMark.Providence => !Main.dayTime ? new Color(230, 80, 240) : new Color(255, 175, 50),
                PristineFuryMark.Polterghast => new Color(115, 232, 255),
                PristineFuryMark.Dog => new Color(170, 70, 255),
                PristineFuryMark.FakeCalamity => new Color(255, 40, 40),
                PristineFuryMark.ExoTwins => new Color(255, 80, 80),
                PristineFuryMark.ExoThanatos => new Color(50, 255, 100),
                PristineFuryMark.ExoAres => new Color(255, 140, 50),
                _ => new Color(255, 224, 92)
            };
        }

        // Returns the representative NPC type for icon display. -1 if none.
        internal static int GetMarkRepresentativeNPCType(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.DesertScourge => ModContent.NPCType<DesertScourgeHead>(),
                PristineFuryMark.EyeOfCthulhu => NPCID.EyeofCthulhu,
                PristineFuryMark.Skeletron => NPCID.SkeletronHead,
                PristineFuryMark.EvilT2 => WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead,
                PristineFuryMark.SlimeGod => ModContent.NPCType<SlimeGodCore>(),
                PristineFuryMark.HardMode => NPCID.WallofFlesh,
                PristineFuryMark.Prime => NPCID.SkeletronPrime,
                PristineFuryMark.BrimstoneElemental => ModContent.NPCType<BrimstoneElemental>(),
                PristineFuryMark.Plantera => NPCID.Plantera,
                PristineFuryMark.Golem => NPCID.Golem,
                PristineFuryMark.Goliath => ModContent.NPCType<PlaguebringerGoliath>(),
                PristineFuryMark.Empress => NPCID.HallowBoss,
                PristineFuryMark.Moonlord => NPCID.MoonLordCore,
                PristineFuryMark.Providence => ModContent.NPCType<ProfanedGuardianCommander>(),
                PristineFuryMark.Polterghast => ModContent.NPCType<Polterghast>(),
                PristineFuryMark.Dog => ModContent.NPCType<DevourerofGodsHead>(),
                PristineFuryMark.Dragon => ModContent.NPCType<Yharon>(),
                PristineFuryMark.FakeCalamity => ModContent.NPCType<CalamitasClone>(),
                PristineFuryMark.ExoTwins => ModContent.NPCType<Artemis>(),
                PristineFuryMark.ExoThanatos => ModContent.NPCType<ThanatosHead>(),
                PristineFuryMark.ExoAres => ModContent.NPCType<AresBody>(),
                PristineFuryMark.Ravager => ModContent.NPCType<RavagerBody>(),
                _ => -1
            };
        }

        // Returns the boss head icon texture for this mark, or null if unavailable.
        // Most CalamityMod bosses use [AutoloadBossHead] which populates NPCID.Sets.BossHeadTextures.
        // A few (Polterghast, DoG, Artemis, Thanatos) use AddBossHeadTexture(path, -1) and must be
        // looked up by path via NPCHeadLoader.GetBossHeadSlot.
        internal static Texture2D TryGetMarkBossHeadTexture(PristineFuryMark mark)
        {
            int npcType = GetMarkRepresentativeNPCType(mark);
            if (npcType >= 0 && npcType < NPCID.Sets.BossHeadTextures.Length)
            {
                int headSlot = NPCID.Sets.BossHeadTextures[npcType];
                if (headSlot >= 0 && headSlot < TextureAssets.NpcHeadBoss.Length)
                {
                    var asset = TextureAssets.NpcHeadBoss[headSlot];
                    if (asset?.IsLoaded == true)
                        return asset.Value;
                }
            }

            string calPath = GetMarkCalamityBossHeadPath(mark);
            if (calPath != null)
            {
                int headSlot = NPCHeadLoader.GetBossHeadSlot(calPath);
                if (headSlot >= 0 && headSlot < TextureAssets.NpcHeadBoss.Length)
                {
                    var asset = TextureAssets.NpcHeadBoss[headSlot];
                    if (asset?.IsLoaded == true)
                        return asset.Value;
                }
            }

            return null;
        }

        // Texture paths for CalamityMod bosses registered with AddBossHeadTexture(path, -1).
        private static string GetMarkCalamityBossHeadPath(PristineFuryMark mark) => mark switch
        {
            PristineFuryMark.Dog        => "CalamityMod/NPCs/DevourerofGods/DevourerofGodsHead_Head_Boss",
            PristineFuryMark.Polterghast => "CalamityMod/NPCs/Polterghast/Polterghast_Head_Boss",
            PristineFuryMark.ExoTwins   => "CalamityMod/NPCs/ExoMechs/Artemis/ArtemisHead",
            PristineFuryMark.ExoThanatos => "CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosNormalHead",
            _ => null
        };

        internal static int GetDisplayedBaseDamage(PristineFuryMark mark) =>
            PF_Balance.GetBaseDamage();
    }
}
