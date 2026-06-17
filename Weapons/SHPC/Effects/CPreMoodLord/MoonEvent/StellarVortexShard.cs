using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    // Pure visual sub-projectile spawned by FragmentVortexEffect during flight.
    // ai[0] = pixel size, ai[1] = colorFactor (0=deep teal, 1=bright turquoise-white), ai[2] = lifetime override
    public class StellarVortexShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.ai[2] > 0f)
                Projectile.timeLeft = (int)Projectile.ai[2];
            Projectile.localAI[0] = Projectile.timeLeft;
            Projectile.localAI[1] = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            Projectile.velocity *= 0.87f;

            float spinDir = Projectile.velocity.X >= 0f ? 1f : -1f;
            Projectile.rotation += spinDir * 0.22f;

            float age = Projectile.localAI[0] - Projectile.timeLeft;
            Projectile.velocity = Projectile.velocity.RotatedBy(
                (float)Math.Sin(Projectile.localAI[1] + age * 0.35f) * 0.04f);

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    0,
                    Color.Lerp(Color.Teal, Color.PaleTurquoise, Projectile.ai[1]),
                    Main.rand.NextFloat(0.35f, 0.6f));
                dust.noGravity = true;
            }

            float progress = 1f - Projectile.timeLeft / Math.Max(1f, Projectile.localAI[0]);
            Lighting.AddLight(Projectile.Center, new Color(0, 180, 170).ToVector3() * (1f - progress) * 0.12f);
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float lifetime = Math.Max(1f, Projectile.localAI[0]);
            float progress = 1f - Projectile.timeLeft / lifetime;

            float fadeIn = Utils.GetLerpValue(0f, 0.15f, progress, true);
            float fadeOut = (float)Math.Pow(1f - progress, 2.2f);
            float alpha = fadeIn * fadeOut;

            if (alpha <= 0f)
                return false;

            float colorFactor = MathHelper.Clamp(Projectile.ai[1], 0f, 1f);
            float flicker = 0.5f + 0.5f * (float)Math.Sin(Projectile.localAI[1] + progress * MathHelper.TwoPi * 3.5f);
            float size = Math.Max(2f, Projectile.ai[0]) * MathHelper.Lerp(1.2f, 0.3f, progress);
            int cellSize = Math.Max(1, (int)MathF.Round(size));

            Color coreColor = Color.Lerp(
                Color.PaleTurquoise,
                new Color(0, 120, 110),
                MathHelper.Clamp(colorFactor * 0.4f + progress * 0.6f + (1f - flicker) * 0.15f, 0f, 1f));

            if (colorFactor > 0.82f && flicker > 0.78f)
                coreColor = Color.Lerp(coreColor, Color.White, 0.35f);

            coreColor.A = 200;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Rectangle coreRect = new Rectangle(
                (int)drawPos.X - cellSize / 2,
                (int)drawPos.Y - cellSize / 2,
                cellSize, cellSize);
            Main.spriteBatch.Draw(pixel, coreRect, null, coreColor * alpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            int haloSize = Math.Max(3, cellSize * 2 + 1);
            Rectangle haloRect = new Rectangle(
                (int)drawPos.X - haloSize / 2,
                (int)drawPos.Y - haloSize / 2,
                haloSize, haloSize);
            Color haloColor = Color.Lerp(new Color(0, 220, 200), new Color(180, 255, 250), colorFactor);
            haloColor.A = 0;
            Main.spriteBatch.Draw(pixel, haloRect, null, haloColor * alpha * 0.28f, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            return false;
        }
    }
}
