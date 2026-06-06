using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal sealed class BrinyBaron_TornadoWaterExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 18;

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.timeLeft > Lifetime / 2;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SpawnBurstParticles();
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.25f, 0.32f) * Projectile.Opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = Utils.GetLerpValue(1f, 0.64f, progress, true);
            float pulse = 1f + (float)System.Math.Sin((Main.GlobalTimeWrappedHourly + Projectile.ai[0]) * 13f) * 0.08f;
            Color water = new Color(72, 205, 255, 0) * fade;
            Color deep = new Color(20, 86, 210, 0) * (fade * 0.75f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(bloom, drawPosition, null, deep * 0.58f, 0f, bloom.Size() * 0.5f, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, water * 0.44f, 0f, bloom.Size() * 0.5f, 0.25f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, water * 0.36f, Projectile.ai[0] + progress * MathHelper.TwoPi, ring.Size() * 0.5f, 0.18f + progress * 0.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White with { A = 0 } * (0.38f * fade), Projectile.ai[0], star.Size() * 0.5f, new Vector2(0.18f, 0.52f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.Cyan with { A = 0 } * (0.28f * fade), Projectile.ai[0] + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.14f, 0.4f) * pulse, SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void SpawnBurstParticles()
        {
            if (Main.dedServ)
                return;

            Color mainColor = new(82, 210, 255);
            Color accentColor = Color.Lerp(mainColor, Color.White, 0.28f);

            DirectionalPulseRing ring = new(
                Projectile.Center,
                Vector2.Zero,
                mainColor * 0.78f,
                Vector2.One,
                Projectile.ai[0],
                0.12f,
                0.02f,
                14);
            GeneralParticleHandler.SpawnParticle(ring);

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.4f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    velocity,
                    100,
                    Main.rand.NextBool() ? mainColor : accentColor,
                    Main.rand.NextFloat(0.85f, 1.28f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(0.9f, 0.9f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.12f, 0.34f)),
                    true,
                    false,
                    true));
            }
        }
    }
}
