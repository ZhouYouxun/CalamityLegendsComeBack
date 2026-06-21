using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    internal static class Mechs2041Spawn
    {
        public static int SpawnDestroyer(Player player, IEntitySource source)
        {
            Vector2 spawnPosition = player.Center + Vector2.UnitY * 640f;
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Destroyer2041Head>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;
            return npc;
        }

        public static int SpawnRetinazer(Player player, IEntitySource source)
        {
            Vector2 spawnPosition = player.Center + new Vector2(-420f, -260f);
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Twins2041Retinazer>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;
            Twins2041State.retinazer = npc;
            return npc;
        }

        public static int SpawnSpazmatism(Player player, IEntitySource source)
        {
            Vector2 spawnPosition = player.Center + new Vector2(420f, -260f);
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Twins2041Spazmatism>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;
            Twins2041State.spazmatism = npc;
            return npc;
        }

        public static void SpawnTwins(Player player, IEntitySource source)
        {
            SpawnRetinazer(player, source);
            SpawnSpazmatism(player, source);
        }
    }

    internal static class Prime2041Compat
    {
        private static readonly int[] destroyerLaserColors = new int[Main.maxNPCs];

        static Prime2041Compat()
        {
            for (int i = 0; i < destroyerLaserColors.Length; i++)
                destroyerLaserColors[i] = -1;
        }

        public static double BossHealthBoost => 0D;
        public static bool EarlyHardmodeProgressionRework => false;
        public const double EarlyHardmodeProgressionReworkFirstMechStatMultiplierExpert = 1D;
        public const double EarlyHardmodeProgressionReworkSecondMechStatMultiplierExpert = 1D;

        public static int DestroyerLaserColor(this NPC npc)
        {
            return destroyerLaserColors[npc.whoAmI];
        }

        public static void SetDestroyerLaserColor(this NPC npc, int color)
        {
            destroyerLaserColors[npc.whoAmI] = color;
        }

        public static void SyncDestroyerLaserColor(this NPC npc)
        {
            npc.netUpdate = true;
        }

        public static int GetProjectileDamage(this NPC npc, int projType)
        {
            int damage = npc.defDamage > 0 ? npc.defDamage : npc.damage;
            if (Main.masterMode)
                damage = (int)(damage * 0.5f);
            else if (Main.expertMode)
                damage = (int)(damage * 0.67f);

            return damage > 0 ? damage : 1;
        }

        public static void GetNPCDamage(this NPC npc)
        {
            if (npc.damage <= 0 && npc.defDamage > 0)
                npc.damage = npc.defDamage;
        }

        public static double GetExpertDamageMultiplier(this NPC npc) => Main.masterMode ? 3D : Main.expertMode ? 2D : 1D;
    }

    public abstract class Mech2041NPC : ModNPC
    {
        protected void HideFromBestiary()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(NPC.localAI[3]);
            writer.Write(NPC.DestroyerLaserColor());
            for (int i = 0; i < 4; i++)
                writer.Write(NPC.Calamity().newAI[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            NPC.localAI[3] = reader.ReadSingle();
            NPC.SetDestroyerLaserColor(reader.ReadInt32());
            for (int i = 0; i < 4; i++)
                NPC.Calamity().newAI[i] = reader.ReadSingle();
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * NPC.GetExpertDamageMultiplier());
        }
    }

    public abstract class Mech2041Summoner : ModItem
    {
        public override string LocalizationCategory => "Items.Consumables";

        protected abstract int VanillaItemType { get; }
        protected abstract bool ExistingBossActive { get; }
        protected abstract void SpawnBoss(Player player, IEntitySource source);

        public override string Texture => $"Terraria/Images/Item_{VanillaItemType}";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 20;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 2);
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player) => !Main.dayTime && !ExistingBossActive;

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SpawnBoss(player, player.GetSource_ItemUse(Item));

            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            return true;
        }
    }
}
