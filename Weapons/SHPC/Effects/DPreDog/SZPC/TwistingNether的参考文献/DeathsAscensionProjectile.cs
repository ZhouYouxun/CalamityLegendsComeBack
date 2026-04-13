using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC.TwistingNether的参考文献
{
    public class DeathsAscensionProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 102;
            Projectile.height = 82;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.alpha = 55;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0f / 255f, (255 - Projectile.alpha) * 0.65f / 255f);

            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }

            int startLife = Projectile.ai[2] > 0 ? DeathsAscension.RiftLifeTime : 180;
            // Player scythes just home
            if (Projectile.ai[2] <= 0)
            {
                CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 18f, 20f);
                Projectile.velocity *= 0.96f;
            }
            // Rift scythes have orbital motion
            else
            {
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(0.1f) * 20;
            }
            // Smoothly disappear
            if (Projectile.timeLeft < 60)
            {
                Projectile.alpha += 3;
            }
            // Rotation direction stuff from the vanilla Death Sickle
            if (Projectile.velocity.X < 0f)
            {
                Projectile.spriteDirection = -1;
            }
            Projectile.rotation += (float)Projectile.direction * 0.05f;
            Projectile.rotation += (float)Projectile.direction * 0.5f * ((float)Projectile.timeLeft / startLife);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor * Projectile.Opacity, 2);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(150, 0, 200, 0) * Projectile.Opacity;
        }
    }
}
