using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.Providence;
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
        ExoAres = 17,
        Ravager = 18
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
                return false;

            if (target.type == ModContent.NPCType<Providence>())
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

        internal static bool IsRavagerTarget(NPC target) =>
            target.type == ModContent.NPCType<RavagerBody>() ||
            target.type == ModContent.NPCType<RavagerHead>() ||
            target.type == ModContent.NPCType<RavagerHead2>() ||
            target.type == ModContent.NPCType<RavagerClawLeft>() ||
            target.type == ModContent.NPCType<RavagerClawRight>() ||
            target.type == ModContent.NPCType<RavagerLegLeft>() ||
            target.type == ModContent.NPCType<RavagerLegRight>();

        internal static bool IsProvidenceLockedRavager(NPC target) =>
            IsRavagerTarget(target) && !DownedBossSystem.downedProvidence;

        internal static string GetName(PristineFuryMark mark) =>
            Language.GetTextValue($"Mods.CalamityLegendsComeBack.PristineFury.Marks.{mark}");

        internal static Color GetColor(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.EvilT2 => WorldGen.crimson ? new Color(240, 40, 70) : new Color(160, 60, 240),
                PristineFuryMark.Plantera => new Color(0, 255, 180),
                PristineFuryMark.Aurora => new Color(60, 220, 240),
                PristineFuryMark.Goliath => new Color(130, 255, 60),
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
                PristineFuryMark.EvilT2 => WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead,
                PristineFuryMark.SlimeGod => ModContent.NPCType<SlimeGodCore>(),
                PristineFuryMark.HardMode => NPCID.WallofFlesh,
                PristineFuryMark.Prime => NPCID.SkeletronPrime,
                PristineFuryMark.BrimstoneElemental => ModContent.NPCType<BrimstoneElemental>(),
                PristineFuryMark.Plantera => NPCID.Plantera,
                PristineFuryMark.Aurora => ModContent.NPCType<AstrumAureus>(),
                PristineFuryMark.Goliath => ModContent.NPCType<PlaguebringerGoliath>(),
                PristineFuryMark.Moonlord => NPCID.MoonLordCore,
                PristineFuryMark.Providence => ModContent.NPCType<Providence>(),
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
