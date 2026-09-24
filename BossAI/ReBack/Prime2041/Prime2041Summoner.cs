using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public class Prime2041Summoner : Mech2041Summoner
    {
        protected override int VanillaItemType => ItemID.MechanicalSkull;
        protected override bool ExistingBossActive => NPC.AnyNPCs(ModContent.NPCType<Prime2041>());

        protected override void SpawnBoss(Player player, IEntitySource source)
        {
            Vector2 spawnPosition = player.Center - Vector2.UnitY * 360f;
            int npc = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Prime2041>());
            Main.npc[npc].target = player.whoAmI;
            Main.npc[npc].netUpdate = true;
        }

    }
}
