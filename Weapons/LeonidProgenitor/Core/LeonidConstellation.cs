using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    /// <summary>
    /// The fourteen principal stars used by the Leonid constellation matrix. Regulus is the
    /// always-lit heart; the remaining thirteen stars follow prerequisite paths and together cost
    /// 32 of the 44 boss stars, so the whole sky can be completed by Moon Lord if every
    /// available boss is defeated. New entries are appended (never inserted) to keep the byte ids
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
        public const int TotalMajorBosses = 44;

        // A compact but recognisable Leo: the sickle (head) curls on the upper left, the body sits in the
        // middle, and the hindquarters + tail run rightward. The extra stars fill the emptier regions so the
        // expanded chart still reads as the lion rather than a random scatter of points.
        private static readonly LeonidConstellationNode[] nodes =
        {
            new(LeonidStar.Regulus, 0, new Vector2(-30f, 45f), LeonidStar.Algieba, LeonidStar.Zosma, LeonidStar.Subra),
            new(LeonidStar.Algieba, 1, new Vector2(-112f, -34f), LeonidStar.Adhafera, LeonidStar.Rasalas),
            new(LeonidStar.Adhafera, 2, new Vector2(-205f, -118f), LeonidStar.Algenubi),
            new(LeonidStar.Rasalas, 4, new Vector2(-98f, -190f)),
            new(LeonidStar.Zosma, 1, new Vector2(92f, -18f), LeonidStar.Chertan, LeonidStar.RLeonis),
            new(LeonidStar.Chertan, 2, new Vector2(205f, 36f), LeonidStar.Denebola, LeonidStar.Dingolay),
            new(LeonidStar.Denebola, 2, new Vector2(326f, 120f), LeonidStar.Wolf359),
            new(LeonidStar.Alterf, 2, new Vector2(74f, 126f), LeonidStar.AlMinliar),
            new(LeonidStar.Subra, 1, new Vector2(-70f, 164f), LeonidStar.Alterf),
            new(LeonidStar.Algenubi, 4, new Vector2(-288f, -180f)),
            new(LeonidStar.AlMinliar, 4, new Vector2(160f, 158f)),
            new(LeonidStar.RLeonis, 4, new Vector2(0f, -60f)),
            new(LeonidStar.Wolf359, 4, new Vector2(250f, 132f)),
            new(LeonidStar.Dingolay, 1, new Vector2(200f, -110f))
        };

        // The whole sky costs the sum of every choosable star, derived so the value never drifts from the table.
        public static int TotalCost => nodes.Sum(node => node.Cost);

        private static readonly IReadOnlyDictionary<LeonidStar, LeonidConstellationNode> nodesByStar = nodes.ToDictionary(node => node.Star);

        public static IReadOnlyList<LeonidConstellationNode> Nodes => nodes;
        public static LeonidConstellationNode GetNode(LeonidStar star) => nodesByStar[star];
        public static LeonidStar Prerequisite(LeonidStar star) => star switch
        {
            LeonidStar.Adhafera or LeonidStar.Rasalas => LeonidStar.Algieba,
            LeonidStar.Algenubi => LeonidStar.Adhafera,
            LeonidStar.Alterf => LeonidStar.Subra,
            LeonidStar.AlMinliar => LeonidStar.Alterf,
            LeonidStar.Chertan or LeonidStar.RLeonis => LeonidStar.Zosma,
            LeonidStar.Denebola or LeonidStar.Dingolay => LeonidStar.Chertan,
            LeonidStar.Wolf359 => LeonidStar.Denebola,
            _ => LeonidStar.Regulus
        };

        public static string LocalizationKey(LeonidStar star) => star.ToString();
    }

    /// <summary>
    /// Player-owned constellation progress. Boss stars are derived from the shared world progression flags,
    /// which gives every active player one credit per unique major boss and handles existing completed worlds.
    /// </summary>
    public sealed class LeonidConstellationPlayer : ModPlayer
    {
        private readonly HashSet<LeonidStar> unlockedStars = new();
        private ushort loadedMask;
        private bool receivedLoadedMask;

        // Boss stars belong to the current world, never to a character's history.
        public int EarnedPoints => GetMajorBossProgress().Count(progress => progress.Downed);
        public int SpentPoints => unlockedStars.Sum(star => LeonidConstellation.GetNode(star).Cost);
        public int AvailablePoints => Math.Max(0, EarnedPoints - SpentPoints);
        public IReadOnlyCollection<LeonidStar> UnlockedStars => unlockedStars;

        public bool IsUnlocked(LeonidStar star) => star == LeonidStar.Regulus || unlockedStars.Contains(star);

        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                LeonidConstellationPackets.SendLoadedStars(receivedLoadedMask ? loadedMask : GetUnlockedMask());
            else if (Main.netMode == NetmodeID.SinglePlayer && ReconcileToWorld())
                NotifyWorldAdjustment();
        }

        public override void PostUpdate()
        {
            // In multiplayer, the server reconciles only after receiving the character's
            // saved chart. Its result is then synchronized to every client.
            if (Main.netMode == NetmodeID.MultiplayerClient ||
                (Main.netMode == NetmodeID.Server && !receivedLoadedMask))
                return;

            if (ReconcileToWorld())
            {
                if (Main.netMode == NetmodeID.Server)
                    LeonidConstellationPackets.SendState(Player, adjustedForWorld: true);
                else if (Player.whoAmI == Main.myPlayer)
                    NotifyWorldAdjustment();
            }
        }

        private static void NotifyWorldAdjustment() => Main.NewText(
            "狮源流星：检测到世界变动，星图已被改变", new Color(47, 99, 202));

        // Keep only stars the player had already selected. Traverse the former chart from
        // the center outward, spreading each layer across west, south and east first.
        internal bool ReconcileToWorld()
        {
            ValidatePaths();
            int budget = EarnedPoints;
            if (SpentPoints <= budget)
                return false;

            ushort oldMask = GetUnlockedMask();
            HashSet<LeonidStar> formerStars = new(unlockedStars);
            unlockedStars.Clear();

            while (true)
            {
                LeonidStar? next = formerStars
                    .Where(star => !IsUnlocked(star)
                        && IsUnlocked(LeonidConstellation.Prerequisite(star))
                        && LeonidConstellation.GetNode(star).Cost <= budget - SpentPoints)
                    .OrderBy(Depth)
                    .ThenBy(BranchSpent)
                    .ThenBy(star => LeonidConstellation.GetNode(star).Cost)
                    .ThenBy(Branch)
                    .ThenBy(star => (int)star)
                    .Cast<LeonidStar?>()
                    .FirstOrDefault();

                if (next is null)
                    break;
                unlockedStars.Add(next.Value);
            }

            return GetUnlockedMask() != oldMask;
        }

        private int BranchSpent(LeonidStar star) => unlockedStars
            .Where(existing => Branch(existing) == Branch(star))
            .Sum(existing => LeonidConstellation.GetNode(existing).Cost);

        private static int Branch(LeonidStar star) => star switch
        {
            LeonidStar.Algieba or LeonidStar.Adhafera or LeonidStar.Rasalas or LeonidStar.Algenubi => 0,
            LeonidStar.Subra or LeonidStar.Alterf or LeonidStar.AlMinliar => 1,
            _ => 2
        };

        private static int Depth(LeonidStar star)
        {
            int depth = 0;
            while (star != LeonidStar.Regulus)
            {
                depth++;
                star = LeonidConstellation.Prerequisite(star);
            }
            return depth;
        }

        public bool CanUnlock(LeonidStar star) => Enum.IsDefined(typeof(LeonidStar), star)
            && !IsUnlocked(star) && IsUnlocked(LeonidConstellation.Prerequisite(star))
            && AvailablePoints >= LeonidConstellation.GetNode(star).Cost;

        public bool TryUnlock(LeonidStar star)
        {
            if (!CanUnlock(star))
                return false;
            unlockedStars.Add(star);
            return true;
        }

        // Old saves may contain isolated outer nodes. Removing them refunds their full cost.
        private void ValidatePaths()
        {
            while (unlockedStars.RemoveWhere(star => !IsUnlocked(LeonidConstellation.Prerequisite(star))) > 0) { }
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
            tag["LeonidUnlockedStars"] = unlockedStars.Select(star => (byte)star).ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            unlockedStars.Clear();

            foreach (byte starID in tag.GetList<byte>("LeonidUnlockedStars"))
            {
                if (Enum.IsDefined(typeof(LeonidStar), starID) && starID != (byte)LeonidStar.Regulus)
                    unlockedStars.Add((LeonidStar)starID);
            }
            ValidatePaths();
            loadedMask = GetUnlockedMask();
            receivedLoadedMask = false;
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
            ValidatePaths();
        }

        internal void ReceiveLoadedMask(ushort mask)
        {
            ReceiveUnlockedMask(mask);
            receivedLoadedMask = true;
        }

        private static IEnumerable<(string Key, bool Downed)> GetMajorBossProgress()
        {
            // Keep this list aligned with the existing full Calamity + vanilla major-boss progression table.
            yield return ("KingSlime", NPC.downedSlimeKing);
            yield return ("DesertScourge", DownedBossSystem.downedDesertScourge);
            yield return ("EyeOfCthulhu", NPC.downedBoss1);
            yield return ("Crabulon", DownedBossSystem.downedCrabulon);
            yield return ("EvilBoss", NPC.downedBoss2);
            yield return ("OtherEvilBoss", LeonidBossCreditWorld.DownedBothEvilBosses);
            yield return ("HiveMind", DownedBossSystem.downedHiveMind);
            yield return ("Perforators", DownedBossSystem.downedPerforator);
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
