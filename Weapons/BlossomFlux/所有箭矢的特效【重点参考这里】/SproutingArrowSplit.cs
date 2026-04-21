using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class SproutingArrowSplit : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Items/Ammo/SproutingArrow";
        public bool expanded = false;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.alpha = 255;
            Projectile.timeLeft = 450;
            Projectile.ArmorPenetration = 8;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            // If you get the direct arrowhead hit, make sure those arrows hit by making them large
            if (Projectile.ai[1] == 1)
            {
                if (!expanded)
                {
                    Projectile.ExpandHitboxBy(100);
                    expanded = true;
                }
            }
            else
                Projectile.tileCollide = true;
            Lighting.AddLight(Projectile.Center, Color.LimeGreen.ToVector3() * 0.25f);
            if (Projectile.alpha > 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PortalBoltTrail, Projectile.velocity.RotatedByRandom(0.6) * Main.rand.NextFloat(0.05f, 1.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.3f);
                dust.color = Main.rand.NextBool(3) ? Color.MediumAquamarine : Color.Lime;
                Projectile.alpha -= 20;
                Projectile.velocity *= 1.09f;
            }
            else if (Main.rand.NextBool())
            {
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.PortalBoltTrail, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.3f));
                dust2.noGravity = true;
                dust2.scale = Main.rand.NextFloat(0.35f, 0.65f);
                dust2.color = Main.rand.NextBool(3) ? Color.MediumAquamarine : Color.Lime;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void OnKill(int timeLeft)
        {
            int Dusts = 6;
            float radians = MathHelper.TwoPi / Dusts;
            Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
            float rotRando = Main.rand.NextFloat(0.1f, 2.5f);
            for (int i = 0; i < Dusts; i++)
            {
                Vector2 dustVelocity = spinningPoint.RotatedBy(radians * i).RotatedBy(0.5f * rotRando) * 3f;
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.PortalBoltTrail, dustVelocity);
                dust2.noGravity = true;
                dust2.scale = Main.rand.NextFloat(0.65f, 0.95f);
                dust2.color = Main.rand.NextBool(3) ? Color.MediumAquamarine : Color.Lime;
            }
            SoundEngine.PlaySound(SoundID.Item118 with { Pitch = 0.5f }, Projectile.Center);
        }
    }
}
