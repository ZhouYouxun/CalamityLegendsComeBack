using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    internal static class HyperdimensionalMatrixCoreRuntime
    {
        public const int BaseDamage = 33;

        public static void RemoveOtherSlotConsumingMinions(Player player, int coreType)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI ||
                    projectile.type == coreType ||
                    !projectile.minion ||
                    projectile.minionSlots <= 0f)
                {
                    continue;
                }

                projectile.Kill();
            }
        }
    }

    public sealed class HyperdimensionalMatrixCoreBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/HyperdimensionalMatrixCore/矩阵BUFF";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>()] <= 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = 18000;
            player.statDefense += 15;
            player.endurance += 0.15f;
            player.lifeRegen += 16;
            player.moveSpeed += 0.08f;
            player.luck += 0.05f;
            player.aggro -= 400;
            Lighting.AddLight(player.Center, new Microsoft.Xna.Framework.Vector3(0.08f, 0.3f, 0.4f));
        }
    }
}
