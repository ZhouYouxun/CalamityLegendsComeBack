using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaMandrake : SeedOfSilvaFlowerProjectile
    {
        private int mandrakeSporeCooldown;
        private int mandrakeDartCooldown;

        protected override int FlowerSlot => 4;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_EPlague;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/种子包/曼陀罗";

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(npc.Center, Projectile.Center) > 230f * 230f)
                    continue;

                npc.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 12);
            }

            if (mandrakeSporeCooldown > 0)
                mandrakeSporeCooldown--;

            if (mandrakeDartCooldown > 0)
                mandrakeDartCooldown--;

            NPC target = FindTarget(620f);
            if (target is null || Projectile.owner != Main.myPlayer)
                return;

            if (mandrakeSporeCooldown <= 0)
            {
                mandrakeSporeCooldown = 42;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.16f));
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 4.2f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<SeedOfSilvaMandrakeSpore>(), damage, 0.2f, Projectile.owner, target.whoAmI);
            }

            if (mandrakeDartCooldown <= 0)
            {
                mandrakeDartCooldown = 128;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.34f));
                Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.28f) * 9.5f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<SeedOfSilvaMandrakeDart>(), damage, 0.6f, Projectile.owner, target.whoAmI);
            }
        }
    }
}
