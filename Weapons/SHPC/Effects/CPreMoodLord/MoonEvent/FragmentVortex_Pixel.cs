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
            Lighting.AddLight(Projectile.Center, new Color(55, 255, 235).ToVector3() * 0.18f);
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

            pixelColor.A = 255;

            Color deepColor = Color.Lerp(
                new Color(0, 18, 34),
                new Color(0, 92, 126),
                colorFactor);

            deepColor.A = 255;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // ====================
            // 外层深色方块
            // ====================

            Rectangle outerRect = new Rectangle(
                (int)drawPos.X - 16,
                (int)drawPos.Y - 16,
                32,
                32);

            Main.spriteBatch.Draw(
                pixel,
                outerRect,
                null,
                deepColor * alpha * 0.42f,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            // ====================
            // 中层青色方块
            // ====================

            Rectangle midRect = new Rectangle(
                (int)drawPos.X - 12,
                (int)drawPos.Y - 12,
                24,
                24);

            Main.spriteBatch.Draw(
                pixel,
                midRect,
                null,
                pixelColor * alpha * 0.55f,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            // ====================
            // 主核心
            // ====================

            Rectangle coreRect = new Rectangle(
                (int)drawPos.X - 8,
                (int)drawPos.Y - 8,
                16,
                16);

            Main.spriteBatch.Draw(
                pixel,
                coreRect,
                null,
                pixelColor * alpha,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            // ====================
            // 白色中心高光
            // ====================

            Rectangle whiteRect = new Rectangle(
                (int)drawPos.X - 4,
                (int)drawPos.Y - 4,
                8,
                8);

            Main.spriteBatch.Draw(
                pixel,
                whiteRect,
                null,
                Color.White * alpha * 0.72f,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            return false;
        }



    }
}
