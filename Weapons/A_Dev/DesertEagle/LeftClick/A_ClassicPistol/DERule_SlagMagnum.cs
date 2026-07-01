using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    public class DERule_SlagMagnum : DEBulletRule
    {
        private static readonly Color Slag = new(218, 126, 49);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.SlagMagnum>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.light = 0.45f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.Sand, Slag, 1f, 0.18f);
            Lighting.AddLight(projectile.Center, Slag.ToVector3() * 0.35f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == projectile.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    projectile.GetSource_FromAI(),
                    target.Center,
                    Math.Max(1, (int)(hit.Damage * 0.48f)),
                    projectile.knockBack,
                    projectile.owner,
                    DEBurstStyle.Slag,
                    68f);
            }
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            Vector2 normal = oldVelocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = (-normal).RotatedBy(MathHelper.Lerp(-0.95f, 0.95f, i / 5f)) * Main.rand.NextFloat(7f, 12f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    projectile.Center - normal * 4f,
                    velocity,
                    ModContent.ProjectileType<DEBullet_ShrapnelShard>(),
                    Math.Max(1, (int)(projectile.damage * 0.24f)),
                    projectile.knockBack * 0.35f,
                    projectile.owner);
            }

            DEBulletUtils.BurstDust(projectile.Center, Slag, DustID.Sand, 24, 7f, 1.1f);
            DEBulletUtils.ParticleBurst(projectile.Center, Slag, 0.9f);
            return true;
        }

        public override string TooltipEffectEN => "Enemy hits trigger only a small slag explosion; tile impacts scatter slag shrapnel";
        public override string TooltipEffectZH => "命中敌人只触发小范围熔渣爆炸；击中方块时散射熔渣破片";
    }
}
