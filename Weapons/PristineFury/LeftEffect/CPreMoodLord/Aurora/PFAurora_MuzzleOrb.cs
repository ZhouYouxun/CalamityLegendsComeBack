using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFAurora_MuzzleOrb : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            int holdoutIndex = (int)Projectile.ai[0];
            if (!Main.projectile.IndexInRange(holdoutIndex) || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout || holdout.CurrentMark != PristineFuryMark.Aurora)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = holdout.GunTipPosition + holdout.AimDirection * 12f;
            Projectile.rotation += 0.1f;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.3f + Projectile.ai[1] * 0.8f));

            if (!Main.dedServ && Main.rand.NextFloat() < Projectile.ai[1] * 0.48f)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(26f, 26f) * Projectile.ai[1];
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center + offset, -offset * 0.05f, false, 12, 0.45f, ThemeColor, true, false, true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Color theme = ThemeColor with { A = 0 };
            float charge = MathHelper.Clamp(Projectile.ai[1], 0f, 1f);
            Vector2 center = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, center, null, theme * (0.32f + charge * 0.42f), Projectile.rotation, bloom.Size() * 0.5f, 0.14f + charge * 0.32f, SpriteEffects.None, 0);
            for (int i = 0; i < 4; i++)
                Main.EntitySpriteDraw(star, center, null, theme * charge * 0.58f, Projectile.rotation + MathHelper.PiOver4 * i, star.Size() * 0.5f, new Vector2(0.16f, 0.7f + charge * 1.5f), SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
