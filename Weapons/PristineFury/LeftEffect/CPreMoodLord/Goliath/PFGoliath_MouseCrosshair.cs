using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_MouseCrosshair : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.timeLeft = 42;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73)).ToVector3() * 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73)) with { A = 0 };
            Vector2 center = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true) * Utils.GetLerpValue(42f, 32f, Projectile.timeLeft, true);
            float radius = 18f + (42 - Projectile.timeLeft) * 0.16f;

            Main.EntitySpriteDraw(bloom, center, null, theme * opacity * 0.16f, 0f, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None, 0);
            DrawLine(pixel, center + new Vector2(-radius, 0f), center + new Vector2(-6f, 0f), theme * opacity);
            DrawLine(pixel, center + new Vector2(6f, 0f), center + new Vector2(radius, 0f), theme * opacity);
            DrawLine(pixel, center + new Vector2(0f, -radius), center + new Vector2(0f, -6f), theme * opacity);
            DrawLine(pixel, center + new Vector2(0f, 6f), center + new Vector2(0f, radius), theme * opacity);
            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            Main.EntitySpriteDraw(pixel, start, null, color, edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), 2f), SpriteEffects.None, 0);
        }
    }
}
