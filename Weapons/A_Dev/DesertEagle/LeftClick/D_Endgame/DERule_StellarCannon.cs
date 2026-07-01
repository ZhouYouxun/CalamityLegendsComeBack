using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    public class DERule_StellarCannon : DEBulletRule
    {
        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.StellarCannon>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.friendly = false;
            projectile.hide = true;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (Main.myPlayer == projectile.owner)
            {
                int starCount = Main.rand.Next(2, 5);
                Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                for (int i = 0; i < starCount; i++)
                {
                    float spread = starCount == 1 ? 0f : MathHelper.Lerp(-0.35f, 0.35f, i / (float)(starCount - 1));
                    Vector2 velocity = forward.RotatedBy(spread) * Main.rand.NextFloat(4.8f, 7.2f);
                    Projectile.NewProjectile(
                        projectile.GetSource_FromAI(),
                        projectile.Center + forward * 14f + Main.rand.NextVector2Circular(10f, 10f),
                        velocity,
                        ModContent.ProjectileType<DEBullet_StellarStar>(),
                        Math.Max(1, (int)(projectile.damage * 0.64f)),
                        projectile.knockBack,
                        projectile.owner);
                }
            }

            projectile.Kill();
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor) => false;

        public override string TooltipEffectEN => "Fires 2-4 astral stars that slow to a halt, then acquire targets";
        public override string TooltipEffectZH => "一次射出2到4颗星星，慢慢停在空中，随后追踪敌人";
    }
}
