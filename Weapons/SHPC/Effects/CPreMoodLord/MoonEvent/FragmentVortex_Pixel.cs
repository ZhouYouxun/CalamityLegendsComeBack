using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentVortex_Pixel : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[2] > 0f)
                Projectile.timeLeft = (int)Projectile.ai[2];

            Projectile.localAI[0] = Projectile.timeLeft;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            if (Projectile.localAI[0] <= 0f)
                Projectile.localAI[0] = Projectile.timeLeft;

            Projectile.velocity *= 0.92f;
            Projectile.rotation += Projectile.ai[1] > 0.5f ? 0.03f : -0.03f;
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
            Color pixelColor = Color.Lerp(
                new Color(0, 58, 82),
                new Color(68, 255, 238),
                colorFactor);

            if (colorFactor > 0.88f)
                pixelColor = Color.Lerp(pixelColor, Color.White, 0.32f);

            pixelColor.A = 0;
            float size = System.Math.Max(1f, Projectile.ai[0]) * MathHelper.Lerp(1f, 0.42f, progress);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Rectangle drawRect = new(
                (int)(drawPos.X - size * 0.5f),
                (int)(drawPos.Y - size * 0.5f),
                System.Math.Max(1, (int)size),
                System.Math.Max(1, (int)size));

            Main.spriteBatch.Draw(
                pixel,
                drawRect,
                null,
                pixelColor * alpha,
                Projectile.rotation,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
