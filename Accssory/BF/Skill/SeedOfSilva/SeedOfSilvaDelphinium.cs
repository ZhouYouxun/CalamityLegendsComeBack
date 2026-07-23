using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaDelphinium : SeedOfSilvaFlowerProjectile
    {
        private int delphiniumCooldown;

        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_CDetec;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/Delphinium";

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            if (delphiniumCooldown > 0)
            {
                delphiniumCooldown--;
                return;
            }

            NPC target = FindTarget(680f);
            if (target is null)
                return;

            delphiniumCooldown = 90;
            if (Projectile.owner != Main.myPlayer)
                return;

            int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.28f));
            Vector2 baseDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 3; i++)
            {
                // ai[0] = target index, ai[1] = arc index (0=centre, 1=left, 2=right)
                // The arc rotates its own divergence in OnSpawn based on ai[1].
                Vector2 velocity = baseDir * 9f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<SeedOfSilvaDelphiniumArc>(),
                    damage,
                    0.5f,
                    Projectile.owner,
                    target.whoAmI,
                    i);
            }
        }
    }
}
