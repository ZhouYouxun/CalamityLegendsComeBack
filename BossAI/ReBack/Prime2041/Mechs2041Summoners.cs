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

    public class Destroyer2041Summoner : Mech2041Summoner
    {
        protected override int VanillaItemType => ItemID.MechanicalWorm;
        protected override bool ExistingBossActive => NPC.AnyNPCs(ModContent.NPCType<Destroyer2041Head>());
        protected override void SpawnBoss(Player player, IEntitySource source) => Mechs2041Spawn.SpawnDestroyer(player, source);
    }

    public class Twins2041Summoner : Mech2041Summoner
    {
        protected override int VanillaItemType => ItemID.MechanicalEye;
        protected override bool ExistingBossActive =>
            NPC.AnyNPCs(ModContent.NPCType<Twins2041Retinazer>()) ||
            NPC.AnyNPCs(ModContent.NPCType<Twins2041Spazmatism>());

        protected override void SpawnBoss(Player player, IEntitySource source) => Mechs2041Spawn.SpawnTwins(player, source);
    }
}
