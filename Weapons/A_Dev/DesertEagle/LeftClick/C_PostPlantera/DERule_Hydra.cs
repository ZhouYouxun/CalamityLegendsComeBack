using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    public class DERule_Hydra : DEBulletRule
    {
        private static readonly Color HydraGreen = new(94, 255, 83);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Hydra>();

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (Main.myPlayer != projectile.owner)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    projectile.Center + side * sign * 24f,
                    projectile.velocity.RotatedBy(sign * 0.28f) * 0.92f,
                    ModContent.ProjectileType<DEBullet_HydraSnake>(),
                    Math.Max(1, (int)(projectile.damage * 0.33f)),
                    projectile.knockBack * 0.4f,
                    projectile.owner,
                    sign);
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.Venom, HydraGreen, 0.9f, 0.15f);
            DEBulletUtils.GlowTrail(projectile, HydraGreen, 1f);
            Lighting.AddLight(projectile.Center, HydraGreen.ToVector3() * 0.38f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 300);
        }

        public override string TooltipEffectEN => "Each shot creates two side snake rounds; the snakes deal 33% damage";
        public override string TooltipEffectZH => "每次攻击在周围生成两条额外弹幕蛇，额外弹幕造成33%伤害";
    }
}
