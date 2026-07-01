using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    public class DERule_ThermoclineBlaster : DEBulletRule
    {
        private static readonly Color IceColor = new(108, 210, 255);
        private static readonly Color FireColor = new(255, 112, 35);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.ThermoclineBlaster>();

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 1f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (projectile.ai[1] != 1f || Main.myPlayer != projectile.owner)
                return;

            Vector2 fireVelocity = projectile.velocity.RotatedBy(MathHelper.ToRadians(4f));
            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitX) * 2f,
                fireVelocity,
                projectile.type,
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                2f);

            projectile.velocity = projectile.velocity.RotatedBy(MathHelper.ToRadians(-4f));
        }

        public override void AI(Projectile projectile, Player owner)
        {
            bool ice = projectile.ai[1] != 2f;
            projectile.localAI[0]++;
            float side = ice ? -1f : 1f;
            projectile.velocity = projectile.velocity.RotatedBy(Math.Sin(projectile.localAI[0] * 0.13f) * 0.012f * side);

            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, ice ? DustID.IceTorch : DustID.Torch, ice ? IceColor : FireColor, 1f, 0.16f);
            DEBulletUtils.GlowTrail(projectile, ice ? IceColor : FireColor, 1.05f);
            Lighting.AddLight(projectile.Center, (ice ? IceColor : FireColor).ToVector3() * 0.5f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnZone(projectile, target.Center, hit.Damage);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnZone(projectile, projectile.Center, projectile.damage);
            return true;
        }

        private static void SpawnZone(Projectile projectile, Vector2 center, int damage)
        {
            bool ice = projectile.ai[1] != 2f;
            if (Main.myPlayer != projectile.owner)
                return;

            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<DEBullet_ThermalZone>(),
                Math.Max(1, (int)(damage * (ice ? 0.26f : 0.22f))),
                projectile.knockBack,
                projectile.owner,
                ice ? 0f : 1f,
                ice ? 92f : 84f);
        }

        public override string TooltipEffectEN => "Fires twin weaving ice/fire rounds; ice creates a freezing field, fire leaves lasting burn";
        public override string TooltipEffectZH => "一次射出冰火双弹交替运行；冰弹制造冻结区域，火弹留下持续燃烧";
    }
}
