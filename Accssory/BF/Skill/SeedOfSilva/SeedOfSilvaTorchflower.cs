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
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/Torchflower";

        protected override void UpdateCommon(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            if (torchflowerCooldown > 0)
                torchflowerCooldown--;
        }

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.48f, 0.16f, 0.04f));

            if (Projectile.owner != Main.myPlayer || Main.GameUpdateCount % 58 != (uint)(Projectile.identity % 58))
                return;

            int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.22f));
            SpawnTorchflowerBlast(damage, 0.5f);
        }

        public override bool TryTriggerTorchflowerExplosion(Projectile triggeringProjectile)
        {
            if (!IsBlooming || torchflowerCooldown > 0)
                return false;

            torchflowerCooldown = TorchflowerExplosionCooldownFrames;

            if (Projectile.owner == Main.myPlayer)
            {
                int damage = System.Math.Max(1, (int)(triggeringProjectile.damage * 0.55f));
                SpawnTorchflowerBlast(damage, triggeringProjectile.knockBack * 0.4f);
            }

            EmitFlowerBurst(new Color(255, 138, 72), 8);
            return true;
        }

        private void SpawnTorchflowerBlast(int damage, float knockBack)
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFLeafBombExplosion>(),
                damage,
                knockBack,
                Projectile.owner,
                150f,
                1f);

            for (int i = 0; i < 3; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SeedOfSilvaTorchShockwave>(),
                    0,
                    0f,
                    Projectile.owner,
                    70f + i * 34f,
                    i * 6f);
            }
        }
    }
}
