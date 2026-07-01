using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    public class DERule_AcesHigh : DEBulletRule
    {
        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.AcesHigh>();

        public override float DamageMultiplier => 0.62f;

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
                int[] cards =
                {
                    ModContent.ProjectileType<DEBullet_CardHeart>(),
                    ModContent.ProjectileType<DEBullet_CardClub>(),
                    ModContent.ProjectileType<DEBullet_CardDiamond>(),
                    ModContent.ProjectileType<DEBullet_CardSpade>()
                };

                Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                for (int i = 0; i < cards.Length; i++)
                {
                    float spread = MathHelper.Lerp(-0.18f, 0.18f, i / 3f);
                    Projectile.NewProjectile(
                        projectile.GetSource_FromAI(),
                        projectile.Center + forward * 8f,
                        projectile.velocity.RotatedBy(spread) * Main.rand.NextFloat(0.96f, 1.05f),
                        cards[i],
                        projectile.damage,
                        projectile.knockBack,
                        projectile.owner);
                }
            }

            projectile.Kill();
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor) => false;

        public override string TooltipEffectEN => "Fires four scattered playing cards at once";
        public override string TooltipEffectZH => "一次射出4张散射的扑克牌";
    }
}
