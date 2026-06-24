using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public class Destroyer2041Summoner : Mech2041Summoner
    {
        protected override int VanillaItemType => ItemID.MechanicalWorm;
        protected override bool ExistingBossActive => NPC.AnyNPCs(ModContent.NPCType<Destroyer2041Head>());
        protected override void SpawnBoss(Player player, IEntitySource source)
        {
            Vector2 spawnPosition = player.Center + Vector2.UnitY * 640f;
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Destroyer2041Head>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;
        }

        protected override void AddVanillaRecipes()
        {
            RegisterRecipe(ItemID.RottenChunk, ItemID.IronBar);
            RegisterRecipe(ItemID.RottenChunk, ItemID.LeadBar);
            RegisterRecipe(ItemID.Vertebrae, ItemID.IronBar);
            RegisterRecipe(ItemID.Vertebrae, ItemID.LeadBar);
        }

        private void RegisterRecipe(int evilMaterialType, int barType)
        {
            Recipe.Create(Type)
                .AddIngredient(evilMaterialType, 6)
                .AddIngredient(barType, 5)
                .AddIngredient(ItemID.SoulofNight, 6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
