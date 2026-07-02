using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryCrystalShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceCrystalShard";

        private const float Gravity = 0.16f;
        private const float MaxFallSpeed = 30f;

        private ref float Hue => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float HasBurst => ref Projectile.localAI[1];

        private static readonly Color ShardGold = new(255, 224, 72);
        private static readonly Color ShardWhite = new(255, 248, 198);
        private Color ShardColor => Color.Lerp(ShardGold, ShardWhite, MathHelper.Clamp(Hue, 0f, 1f) * 0.22f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            if (Timer == 0f)
            {
                Projectile.scale = Main.rand.NextFloat(0.92f, 1.16f);
                SpawnArrivalSpark();
            }

            Timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 38);
            Projectile.velocity.Y = Math.Min(MaxFallSpeed, Projectile.velocity.Y + Gravity);

            if (Projectile.velocity.LengthSquared() > MaxFallSpeed * MaxFallSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MaxFallSpeed;

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            if (!Main.dedServ)
            {
                if (Timer % 2 == 0)
                    SpawnTrailParticle();
                if (Main.rand.NextBool(5))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        DustID.GoldFlame,
                        -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.2f, 0.8f),
                        100, Main.rand.NextBool(3) ? ShardWhite : ShardGold, Main.rand.NextFloat(0.65f, 0.95f));
                    d.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, ShardColor.ToVector3() * 0.4f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Burst();

        public override void OnKill(int timeLeft) => Burst();

        private void SpawnArrivalSpark()
        {
            if (Main.dedServ)
                return;

            Color color = ShardColor;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, color with { A = 0 },
                new Vector2(0.75f, 0.75f), Projectile.velocity.ToRotation(), 0.22f, 0.08f, 10));
        }

        private void SpawnTrailParticle()
        {
            Color color = ShardColor;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 pos = Projectile.Center - forward * Main.rand.NextFloat(4f, 10f) + right * Main.rand.NextFloat(-4f, 4f);
            Vector2 vel = -forward * Main.rand.NextFloat(0.5f, 1.2f) + right * Main.rand.NextFloat(-0.2f, 0.2f);

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                pos, vel, false, Main.rand.Next(8, 14),
                Main.rand.NextFloat(0.18f, 0.32f) * Projectile.scale,
                color with { A = 0 }, false, false, false));

            if (Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos, vel * 1.4f, false, Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.1f, 0.18f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.1f, 0.4f)) with { A = 0 }));
            }
        }

        private void Burst()
        {
            if (Main.dedServ || HasBurst == 1f)
                return;

            HasBurst = 1f;
            Color color = ShardColor;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, color with { A = 0 },
                Vector2.One, 0f, 0.08f, 0.28f, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, ShardWhite with { A = 0 },
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, 0f, 0.36f, 0.05f, 16));

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.1f, 3.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), velocity, false,
                    Main.rand.Next(10, 17), Main.rand.NextFloat(0.28f, 0.5f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.15f, 0.45f)) with { A = 0 },
                    false, false, false));
            }

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.38f, Pitch = 0.35f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            float opacity = 1f - Projectile.alpha / 255f;
            Color shardColor = ShardColor;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = (float)i / Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(shardColor with { A = 0 }, new Color(255, 174, 42) with { A = 0 }, t)
                    * ((1f - t) * 0.55f * opacity);
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float bloomScale = Projectile.scale * MathHelper.Lerp(0.08f, 0.22f, 1f - t);
                float starScale = Projectile.scale * MathHelper.Lerp(0.04f, 0.14f, 1f - t);

                Main.EntitySpriteDraw(bloom, trailPos, null, trailColor * 0.42f,
                    Projectile.oldRot[i], bloom.Size() * 0.5f, bloomScale, SpriteEffects.None);
                Main.EntitySpriteDraw(star, trailPos, null, trailColor * 0.28f,
                    Projectile.oldRot[i] + t * 0.6f, star.Size() * 0.5f,
                    new Vector2(starScale * 0.9f, starScale * 0.32f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPos, null,
                shardColor with { A = 0 } * (0.42f * opacity),
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.24f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPos, null,
                Color.White with { A = 0 } * (0.34f * opacity),
                Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.18f, Projectile.scale * 0.06f), SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Color bodyColor = new Color(
                (int)(shardColor.R * opacity),
                (int)(shardColor.G * opacity),
                (int)(shardColor.B * opacity),
                (int)(200 * opacity));
            Main.EntitySpriteDraw(texture, drawPos, texture.Frame(), bodyColor, Projectile.rotation,
                texture.Frame().Center(), Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
