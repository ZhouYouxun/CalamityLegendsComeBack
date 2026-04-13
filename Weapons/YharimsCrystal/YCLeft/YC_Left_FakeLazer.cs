using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_FakeLazer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 12;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.008f;
            Projectile.scale = MathHelper.Lerp(0.35f, 1f, Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true));

            Lighting.AddLight(Projectile.Center, new Color(160, 245, 255).ToVector3() * 0.42f);

            if (Main.dedServ || !Main.rand.NextBool(4))
                return;

            Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            YC_LeftSquadronHelper.EmitTechDust(
                Projectile.Center + side * Main.rand.NextFloatDirection() * Main.rand.NextFloat(2f, 6f),
                (-Projectile.velocity * 0.04f).RotatedByRandom(0.28f),
                new Color(200, 250, 255),
                Main.rand.NextFloat(0.65f, 0.95f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 previous = Projectile.Center;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 segment = previous - current;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                float opacity = completion * 0.45f;
                float length = segment.Length();

                if (length > 0.01f)
                {
                    Main.EntitySpriteDraw(
                        trail,
                        previous - Main.screenPosition - segment * 0.5f,
                        null,
                        new Color(120, 240, 255, 0) * opacity,
                        segment.ToRotation() + MathHelper.PiOver2,
                        trail.Size() * 0.5f,
                        new Vector2(0.25f, length / trail.Height) * Projectile.scale,
                        SpriteEffects.None,
                        0);
                }

                previous = current;
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(225, 255, 255, 0) * 0.8f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.16f,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
