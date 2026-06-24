using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public class Twins2041Summoner : Mech2041Summoner
    {
        protected override int VanillaItemType => ItemID.MechanicalEye;
        protected override bool ExistingBossActive =>
            NPC.AnyNPCs(ModContent.NPCType<Twins2041Retinazer>()) ||
            NPC.AnyNPCs(ModContent.NPCType<Twins2041Spazmatism>());

        protected override void SpawnBoss(Player player, IEntitySource source)
        {
            Vector2 spawnPosition1 = player.Center + new Vector2(-420f, -260f);
            int npc1 = NPC.NewNPC(source, (int)spawnPosition1.X, (int)spawnPosition1.Y, ModContent.NPCType<Twins2041Retinazer>());
            Main.npc[npc1].target = player.whoAmI;
            Main.npc[npc1].netUpdate = true;
            Twins2041State.retinazer = npc1;

            Vector2 spawnPosition2 = player.Center + new Vector2(420f, -260f);
            int npc2 = NPC.NewNPC(source, (int)spawnPosition2.X, (int)spawnPosition2.Y, ModContent.NPCType<Twins2041Spazmatism>());
            Main.npc[npc2].target = player.whoAmI;
            Main.npc[npc2].netUpdate = true;
            Twins2041State.spazmatism = npc2;
        }

        protected override void AddVanillaRecipes()
        {
            RegisterRecipe(ItemID.IronBar);
            RegisterRecipe(ItemID.LeadBar);
        }

        private void RegisterRecipe(int barType)
        {
            Recipe.Create(Type)
                .AddIngredient(ItemID.Lens, 3)
                .AddIngredient(barType, 5)
                .AddIngredient(ItemID.SoulofLight, 7)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
