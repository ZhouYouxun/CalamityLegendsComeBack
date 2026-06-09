using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaTorchflower : SeedOfSilvaFlowerProjectile
    {
        private const int TorchflowerExplosionCooldownFrames = 15;

        private int torchflowerCooldown;

        protected override int FlowerSlot => 3;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_DBomb;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/种子包/火炬花";

        protected override void UpdateCommon(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            if (torchflowerCooldown > 0)
                torchflowerCooldown--;
        }

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.48f, 0.16f, 0.04f));

            NPC target = FindTarget(270f);
            if (target is null || Projectile.owner != Main.myPlayer || Main.GameUpdateCount % 24 != (uint)(Projectile.identity % 24))
                return;

            int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.18f));
            Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.22f) * 6f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                ModContent.ProjectileType<SeedOfSilvaTorchFlame>(),
                damage,
                0.2f,
                Projectile.owner);
        }

        public override bool TryTriggerTorchflowerExplosion(Projectile triggeringProjectile)
        {
            if (!IsBlooming || torchflowerCooldown > 0)
                return false;

            torchflowerCooldown = TorchflowerExplosionCooldownFrames;

            if (Projectile.owner == Main.myPlayer)
            {
                int damage = System.Math.Max(1, (int)(triggeringProjectile.damage * 0.55f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFLeafBombExplosion>(),
                    damage,
                    triggeringProjectile.knockBack * 0.4f,
                    Projectile.owner,
                    116f,
                    1f);
            }

            EmitFlowerBurst(new Color(255, 138, 72), 8);
            return true;
        }
    }
}
