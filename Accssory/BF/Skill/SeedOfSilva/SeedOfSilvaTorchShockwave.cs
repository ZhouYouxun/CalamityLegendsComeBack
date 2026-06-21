using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaTorchShockwave : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float MaxRadius => ref Projectile.ai[0];
        private ref float Delay => ref Projectile.ai[1];
        private bool burstSpawned;

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

            if (!burstSpawned && !Main.dedServ)
            {
                burstSpawned = true;
                SpawnBurstFX();
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 7f, 28f - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.12f, 0.02f) * Projectile.Opacity);
        }

        private void SpawnBurstFX()
        {
            Color fireColor = Color.Lerp(new Color(255, 148, 60), Color.Gold, 0.28f);

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + angle.ToRotationVector2() * MaxRadius * 0.45f,
                    DustID.Torch,
                    angle.ToRotationVector2() * Main.rand.NextFloat(1.8f, 4f),
                    0, fireColor, Main.rand.NextFloat(0.7f, 1.05f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2CircularEdge(MaxRadius * 0.35f, MaxRadius * 0.35f),
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.28f, 0.44f),
                    Color.Lerp(fireColor, Color.White, Main.rand.NextFloat(0.2f, 0.5f)),
                    true, false, true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Delay > 0f || Projectile.Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            float progress = 1f - Projectile.timeLeft / 28f;
            float radius = MathHelper.Lerp(12f, MaxRadius, progress);
            float opacity = Projectile.Opacity;

            Color ringOuter = new Color(255, 140, 50, 0);
            Color ringCore = new Color(255, 210, 80, 0);

            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            int segments = 20;
            float outerSeg = radius * MathHelper.TwoPi / (segments * bloom.Width) * 2.2f;
            float innerSeg = outerSeg * 0.6f;

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments + progress * 0.25f;
                Vector2 pos = center + angle.ToRotationVector2() * radius;
                Main.EntitySpriteDraw(bloom, pos, null, ringOuter * (opacity * 0.48f), 0f, bloom.Size() * 0.5f, outerSeg, SpriteEffects.None, 0);
            }

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 pos = center + angle.ToRotationVector2() * (radius * 0.95f);
                Main.EntitySpriteDraw(bloom, pos, null, ringCore * (opacity * 0.28f), 0f, bloom.Size() * 0.5f, innerSeg, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, center, null, ringCore * (opacity * 0.07f), 0f, bloom.Size() * 0.5f, radius / bloom.Width, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
