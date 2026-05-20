using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentVortex_Pixel : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[2] > 0f)
                Projectile.timeLeft = (int)Projectile.ai[2];

            Projectile.localAI[0] = Projectile.timeLeft;
            Projectile.localAI[1] = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            if (Projectile.localAI[0] <= 0f)
                Projectile.localAI[0] = Projectile.timeLeft;

            Projectile.velocity *= 0.92f;
            Projectile.rotation += Projectile.ai[1] > 0.5f ? 0.045f : -0.045f;
            Lighting.AddLight(Projectile.Center, new Color(55, 255, 235).ToVector3() * 0.08f);
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float lifetime = System.Math.Max(1f, Projectile.localAI[0]);
            float progress = 1f - Projectile.timeLeft / lifetime;

            float fadeIn = Utils.GetLerpValue(0f, 0.12f, progress, true);
            float fadeOut = (float)System.Math.Pow(1f - progress, 1.75f);
            float alpha = fadeIn * fadeOut;

            if (alpha <= 0f)
                return false;

            float colorFactor = MathHelper.Clamp(Projectile.ai[1], 0f, 1f);
            float flicker = 0.5f + 0.5f * (float)System.Math.Sin(Projectile.localAI[1] + progress * MathHelper.TwoPi * 2.5f);
            float size = System.Math.Max(2f, Projectile.ai[0]) * MathHelper.Lerp(1.08f, 0.42f, progress);
            int cellSize = System.Math.Max(1, (int)System.MathF.Round(size));

            Color pixelColor = Color.Lerp(
                new Color(0, 70, 96),
                new Color(66, 255, 238),
                MathHelper.Clamp(colorFactor * 0.78f + flicker * 0.22f, 0f, 1f));

            if (colorFactor > 0.9f)
                pixelColor = Color.Lerp(pixelColor, new Color(184, 255, 252), 0.25f);

            pixelColor.A = 255;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Rectangle pixelRect = new Rectangle(
                (int)drawPos.X - cellSize / 2,
                (int)drawPos.Y - cellSize / 2,
                cellSize,
                cellSize);

            Main.spriteBatch.Draw(
                pixel,
                pixelRect,
                null,
                pixelColor * alpha,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
