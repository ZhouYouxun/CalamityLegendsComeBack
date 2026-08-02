using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    /// <summary>
    /// The fourteen principal stars used by the Leonid constellation matrix. Regulus is the
    /// always-lit heart; the remaining thirteen stars are freely choosable and together cost
    /// 38 of the 42 boss stars, so the whole sky can still be completed by the very end while
    /// leaving a small buffer. New entries are appended (never inserted) to keep the byte ids
    /// stored in existing save files stable.
    /// </summary>
    public enum LeonidStar : byte
    {
        Regulus,
        Algieba,
        Adhafera,
        Rasalas,
        Zosma,
        Chertan,
        Denebola,
        Alterf,
        Subra,
        Algenubi,
        AlMinliar,
        RLeonis,
        Wolf359,
        Dingolay
    }

    public readonly struct LeonidConstellationNode
    {
        public LeonidConstellationNode(LeonidStar star, int cost, Vector2 position, params LeonidStar[] connections)
        {
            Star = star;
            Cost = cost;
            Position = position;
            Connections = connections ?? Array.Empty<LeonidStar>();
        }

        public LeonidStar Star { get; }
        public int Cost { get; }
        public Vector2 Position { get; }
        public LeonidStar[] Connections { get; }
    }

    public static class LeonidConstellation
    {
        public const int TotalMajorBosses = 42;

        // A compact but recognisable Leo: the sickle (head) curls on the upper left, the body sits in the
        // middle, and the hindquarters + tail run rightward. The extra stars fill the emptier regions so the
        // expanded chart still reads as the lion rather than a random scatter of points.
        private static readonly LeonidConstellationNode[] nodes =
        {
            new(LeonidStar.Regulus, 0, new Vector2(-30f, 45f), LeonidStar.Algieba, LeonidStar.Zosma, LeonidStar.Subra, LeonidStar.RLeonis),
            new(LeonidStar.Algieba, 2, new Vector2(-112f, -34f), LeonidStar.Adhafera, LeonidStar.Rasalas),
            new(LeonidStar.Adhafera, 3, new Vector2(-205f, -118f), LeonidStar.Rasalas, LeonidStar.Algenubi),
            new(LeonidStar.Rasalas, 3, new Vector2(-98f, -190f)),
            new(LeonidStar.Zosma, 3, new Vector2(92f, -18f), LeonidStar.Chertan, LeonidStar.Dingolay),
            new(LeonidStar.Chertan, 2, new Vector2(205f, 36f), LeonidStar.Denebola),
            new(LeonidStar.Denebola, 3, new Vector2(326f, 120f), LeonidStar.Wolf359),
            new(LeonidStar.Alterf, 4, new Vector2(74f, 126f), LeonidStar.Subra, LeonidStar.AlMinliar),
            new(LeonidStar.Subra, 3, new Vector2(-70f, 164f)),
            new(LeonidStar.Algenubi, 3, new Vector2(-288f, -180f)),
            new(LeonidStar.AlMinliar, 3, new Vector2(160f, 158f)),
            new(LeonidStar.RLeonis, 3, new Vector2(0f, -60f), LeonidStar.Zosma),
            new(LeonidStar.Wolf359, 3, new Vector2(250f, 132f)),
            new(LeonidStar.Dingolay, 3, new Vector2(200f, -110f))
        };

        // The whole sky costs the sum of every choosable star, derived so the value never drifts from the table.
        public static int TotalCost => nodes.Sum(node => node.Cost);

        private static readonly IReadOnlyDictionary<LeonidStar, LeonidConstellationNode> nodesByStar = nodes.ToDictionary(node => node.Star);

        public static IReadOnlyList<LeonidConstellationNode> Nodes => nodes;
        public static LeonidConstellationNode GetNode(LeonidStar star) => nodesByStar[star];
        public static string LocalizationKey(LeonidStar star) => star.ToString();
    }

    /// <summary>
    /// Player-owned constellation progress. Boss stars are derived from the shared world progression flags,
    /// which gives every active player one credit per unique major boss and handles existing completed worlds.
    /// </summary>
    public sealed class LeonidConstellationPlayer : ModPlayer
    {
        private readonly HashSet<string> claimedBosses = new(StringComparer.Ordinal);
        private readonly HashSet<LeonidStar> unlockedStars = new();

        public int EarnedPoints => claimedBosses.Count;
        public int SpentPoints => unlockedStars.Sum(star => LeonidConstellation.GetNode(star).Cost);
        public int AvailablePoints => Math.Max(0, EarnedPoints - SpentPoints);
        public IReadOnlyCollection<LeonidStar> UnlockedStars => unlockedStars;

        public bool IsUnlocked(LeonidStar star) => star == LeonidStar.Regulus || unlockedStars.Contains(star);

        public override void PostUpdate()
        {
            // World progression is already synchronized by Terraria/Calamity. Recording it locally prevents
            // duplicate rewards while allowing a character entering an advanced world to receive its due stars.
            foreach ((string key, bool downed) in GetMajorBossProgress())
            {
                if (downed)
                    claimedBosses.Add(key);
            }
        }

        public bool TryUnlock(LeonidStar star)
        {
            if (star == LeonidStar.Regulus || IsUnlocked(star))
                return false;

            int cost = LeonidConstellation.GetNode(star).Cost;
            if (AvailablePoints < cost)
                return false;

            unlockedStars.Add(star);
            return true;
        }

        public bool ResetStars()
        {
            if (unlockedStars.Count == 0)
                return false;

            unlockedStars.Clear();
            return true;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["LeonidClaimedBosses"] = claimedBosses.ToList();
            tag["LeonidUnlockedStars"] = unlockedStars.Select(star => (byte)star).ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            claimedBosses.Clear();
            unlockedStars.Clear();

            foreach (string key in tag.GetList<string>("LeonidClaimedBosses"))
                claimedBosses.Add(key);

            foreach (byte starID in tag.GetList<byte>("LeonidUnlockedStars"))
            {
                if (Enum.IsDefined(typeof(LeonidStar), starID) && starID != (byte)LeonidStar.Regulus)
                    unlockedStars.Add((LeonidStar)starID);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => LeonidConstellationPackets.SendState(Player, toWho, fromWho);

        internal ushort GetUnlockedMask()
        {
            ushort mask = 0;
            foreach (LeonidStar star in unlockedStars)
                mask |= (ushort)(1 << (int)star);
            return mask;
        }

        internal void ReceiveUnlockedMask(ushort mask)
        {
            unlockedStars.Clear();
            foreach (LeonidStar star in Enum.GetValues(typeof(LeonidStar)))
            {
                if (star != LeonidStar.Regulus && (mask & (1 << (int)star)) != 0)
                    unlockedStars.Add(star);
            }
        }

        private static IEnumerable<(string Key, bool Downed)> GetMajorBossProgress()
        {
            // Keep this list aligned with the existing full Calamity + vanilla major-boss progression table.
            yield return ("KingSlime", NPC.downedSlimeKing);
            yield return ("DesertScourge", DownedBossSystem.downedDesertScourge);
            yield return ("EyeOfCthulhu", NPC.downedBoss1);
            yield return ("Crabulon", DownedBossSystem.downedCrabulon);
            yield return ("EvilBoss", NPC.downedBoss2);
            yield return ("HiveMindOrPerforator", DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator);
            yield return ("QueenBee", NPC.downedQueenBee);
            yield return ("Skeletron", NPC.downedBoss3);
            yield return ("Deerclops", NPC.downedDeerclops);
            yield return ("SlimeGod", DownedBossSystem.downedSlimeGod);
            yield return ("WallOfFlesh", Main.hardMode);

            yield return ("QueenSlime", NPC.downedQueenSlime);
            yield return ("AquaticScourge", DownedBossSystem.downedAquaticScourge);
            yield return ("BrimstoneElemental", DownedBossSystem.downedBrimstoneElemental);
            yield return ("Cryogen", DownedBossSystem.downedCryogen);
            yield return ("Destroyer", NPC.downedMechBoss1);
            yield return ("Twins", NPC.downedMechBoss2);
            yield return ("SkeletronPrime", NPC.downedMechBoss3);
            yield return ("CalamitasClone", DownedBossSystem.downedCalamitasClone);
            yield return ("Plantera", NPC.downedPlantBoss);
            yield return ("LeviathanAndAnahita", DownedBossSystem.downedLeviathan);
            yield return ("AstrumAureus", DownedBossSystem.downedAstrumAureus);
            yield return ("Golem", NPC.downedGolemBoss);
            yield return ("PlaguebringerGoliath", DownedBossSystem.downedPlaguebringer);
            yield return ("DukeFishron", NPC.downedFishron);
            yield return ("EmpressOfLight", NPC.downedEmpressOfLight);
            yield return ("Ravager", DownedBossSystem.downedRavager);
            yield return ("LunaticCultist", NPC.downedAncientCultist);
            yield return ("AstrumDeus", DownedBossSystem.downedAstrumDeus);
            yield return ("MoonLord", NPC.downedMoonlord);

            yield return ("ProfanedGuardians", DownedBossSystem.downedGuardians);
            yield return ("Dragonfolly", DownedBossSystem.downedDragonfolly);
            yield return ("Providence", DownedBossSystem.downedProvidence);
            yield return ("StormWeaver", DownedBossSystem.downedStormWeaver);
            yield return ("CeaselessVoid", DownedBossSystem.downedCeaselessVoid);
            yield return ("Signus", DownedBossSystem.downedSignus);
            yield return ("Polterghast", DownedBossSystem.downedPolterghast);
            yield return ("OldDuke", DownedBossSystem.downedBoomerDuke);
            yield return ("DevourerOfGods", DownedBossSystem.downedDoG);
            yield return ("Yharon", DownedBossSystem.downedYharon);
            yield return ("ExoMechs", DownedBossSystem.downedExoMechs);
            yield return ("SupremeCalamitas", DownedBossSystem.downedCalamitas);
        }
    }
}
