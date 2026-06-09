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

        protected override int FlowerSlot => 2;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_CDetec;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/种子包/飞燕草";

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
            Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity.RotatedByRandom(0.22f),
                ModContent.ProjectileType<SeedOfSilvaDelphiniumArc>(),
                damage,
                0.5f,
                Projectile.owner,
                target.whoAmI);
        }
    }
}
