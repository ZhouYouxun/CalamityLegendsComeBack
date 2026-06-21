using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public class Prime2041Summoner : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.MechanicalSkull}";
        public override string LocalizationCategory => "Items.Consumables";

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

        public override bool CanUseItem(Player player)
        {
            return !Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<Prime2041>());
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return true;

            IEntitySource source = player.GetSource_ItemUse(Item);
            Vector2 spawnPosition = player.Center - Vector2.UnitY * 360f;
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Prime2041>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;

            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            return true;
        }
    }
}
