using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaTorchShockwave : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float MaxRadius => ref Projectile.ai[0];
        private ref float Delay => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 28;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Delay > 0f)
            {
                Delay--;
                Projectile.timeLeft++;
                return;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 7f, 28f - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.12f, 0.02f) * Projectile.Opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Delay > 0f || Projectile.Opacity <= 0f)
                return false;

            Texture2D bloom = TextureAssets.Extra[98].Value;
            float progress = 1f - Projectile.timeLeft / 28f;
            float radius = MathHelper.Lerp(18f, MaxRadius, progress);
            Color color = Color.Lerp(new Color(255, 120, 50), Color.Gold, 0.32f) with { A = 0 };

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                color * Projectile.Opacity * 0.42f,
                0f,
                bloom.Size() * 0.5f,
                new Vector2(radius / bloom.Width, radius / bloom.Height) * 2f,
                SpriteEffects.None);

            return false;
        }
    }
}
