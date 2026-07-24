using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    internal sealed class BrinyBaron_DashOceanSwirl : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color DeepOceanColor = new(45, 175, 255);
        private static readonly Color FoamBlueColor = new(135, 235, 255);
        private static readonly Color BloomWhite = new(240, 255, 255);

        private ref float Timer => ref Projectile.ai[0];
        private ref float Seed => ref Projectile.ai[1];

        private const int TotalLifeTime = 34;

        private float Opacity =>
            Utils.GetLerpValue(0f, 6f, Timer, true) *
            Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLifeTime;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Seed == 0f)
                Seed = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity = Vector2.Zero;

            float progress = Timer / (float)TotalLifeTime;
            Lighting.AddLight(Projectile.Center, DeepOceanColor.ToVector3() * (0.32f * Opacity));

            if (!Main.dedServ && Timer % 3 == 0 && Main.rand.NextBool(2))
            {
                float angle = Timer * 0.15f + Seed;
                Vector2 swirlPos = Projectile.Center + angle.ToRotationVector2() * MathHelper.Lerp(6f, 26f, progress);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    swirlPos,
                    Main.rand.NextVector2Circular(0.3f, 0.3f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.12f, 0.24f) * Opacity,
                    Color.Lerp(DeepOceanColor, BloomWhite, Main.rand.NextFloat(0.2f, 0.7f))));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/PulseStar").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Seed);
            float orbitRadius = MathHelper.Lerp(8f, 28f, Timer / (float)TotalLifeTime);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                DeepOceanColor with { A = 0 } * (0.35f * Opacity),
                0f,
                bloom.Size() * 0.5f,
                0.22f * pulse * Opacity,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.TwoPi * i / 3f + Timer * 0.14f + Seed;
                Vector2 wispOffset = new Vector2((float)Math.Cos(angle) * orbitRadius, (float)Math.Sin(angle * 1.2f) * (orbitRadius * 0.7f));
                Vector2 wispDrawPos = drawPosition + wispOffset;

                Main.EntitySpriteDraw(
                    soft,
                    wispDrawPos,
                    null,
                    FoamBlueColor with { A = 0 } * (0.55f * Opacity),
                    angle * 0.5f,
                    soft.Size() * 0.5f,
                    0.16f * pulse * Opacity,
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    star,
                    wispDrawPos,
                    null,
                    BloomWhite with { A = 0 } * (0.42f * Opacity),
                    angle,
                    star.Size() * 0.5f,
                    new Vector2(0.1f, 0.22f) * pulse * Opacity,
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
