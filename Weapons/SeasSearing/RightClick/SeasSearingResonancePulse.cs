using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingResonancePulse : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 34;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width      = 10;
            Projectile.height     = 10;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity *= 0.98f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.RadioactiveCyan.ToVector3() * 0.3f);

            if (Projectile.timeLeft % 4 == 0)
            {
                float completion = 1f - Projectile.timeLeft / (float)Lifetime;
                SeasSearingVisualUtility.SpawnPressureRing(
                    Projectile.Center,
                    3.4f + completion * 3.8f,
                    12f + completion * 75f,
                    28,
                    Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, completion));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            float opacity    = (float)Math.Sin(completion * MathHelper.Pi);
            Vector2 center   = Projectile.Center - Main.screenPosition;
            Color color = (Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, completion) with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(bloom, center, null, color * 0.28f, 0f, bloom.Size() * 0.5f, new Vector2(0.35f + completion * 1.2f, 0.18f + completion * 0.35f), SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float scale = 0.22f + completion * (0.72f + i * 0.32f);
                Main.EntitySpriteDraw(ring, center, null, color * (0.72f - i * 0.16f), Projectile.rotation + Main.GlobalTimeWrappedHourly * (0.6f + i * 0.2f), ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
