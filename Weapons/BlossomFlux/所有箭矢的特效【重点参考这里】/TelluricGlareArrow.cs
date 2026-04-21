using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class TelluricGlareArrow : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 180;

        public int time = 0;
        public int fadeTime = 22;
        public bool colorAlt = false;

        public override void SetStaticDefaults()
        {
            // While this projectile doesn't have afterimages, it keeps track of old positions for its primitive drawcode.
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 21;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.arrow = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 3;
            Projectile.penetrate = -1; // Can hit many enemies. Will explode extremely soon after hitting the first, though.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => (Projectile.timeLeft < Lifetime - 4) ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3());
            if (time % 8 == 0 && time > 6)
            {
                bool isSpark = Main.rand.NextBool(3);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, isSpark ? 278 : ModContent.DustType<LightDust>(), (Projectile.velocity * 2).RotatedByRandom(0.2f) * Main.rand.NextFloat(0.2f, 1f));
                dust.noGravity = true;
                dust.velocity *= (isSpark ? 0.5f : 1);
                dust.scale = Main.rand.NextFloat(0.95f, 1.25f) * (isSpark ? 0.9f : 1);
                dust.color = Main.rand.NextBool(5) ? Color.Khaki : Color.Goldenrod;
                if (isSpark)
                    dust.noGravity = false;
                else
                    dust.noLightEmittence = true;
            }
            if (time == 4)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i < 3)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<LightDust>(), (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f));
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(0.85f, 1.15f);
                        dust.color = Main.rand.NextBool(5) ? Color.Khaki : Color.Goldenrod;
                        dust.noLightEmittence = true;
                    }
                    else
                    {
                        Particle spark = new CustomSpark(Projectile.Center, (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f), "CalamityMod/Particles/ProvidenceMarkParticle", false, 17, Main.rand.NextFloat(1.15f, 1.3f), Color.Lerp(Color.Orchid, Color.White, Main.rand.NextFloat(0, 0.7f)), new Vector2(1.3f, 0.5f), true, false, 0, false, false, Main.rand.NextFloat(0.3f, 0.4f));
                        GeneralParticleHandler.SpawnParticle(spark);
                    }

                    Particle spark2 = new GlowSparkParticle(Projectile.Center, (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f), false, 9, 0.017f, Color.Goldenrod, new Vector2(1.5f, 0.7f), true, false, 1.3f);
                    GeneralParticleHandler.SpawnParticle(spark2);
                }
            }
            if (time == 0)
                colorAlt = Main.rand.NextBool();
            if (Projectile.timeLeft < 8 && fadeTime > 0)
            {
                Projectile.velocity *= 0.92f;
                Projectile.scale -= 0.023f;
                fadeTime--;
                Projectile.timeLeft++;
                if (fadeTime == 6)
                {
                    Particle spark2 = new GlowSparkParticle(Projectile.Center, Projectile.velocity, false, 7, 0.012f, Color.Goldenrod, new Vector2(1.5f, 0.7f), true, false, 2);
                    GeneralParticleHandler.SpawnParticle(spark2);
                }
            }
            time++;
        }

        private void RestrictLifetime()
        {
            if (Projectile.timeLeft > 8)
                Projectile.timeLeft = 8;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0)
                return;

            RestrictLifetime();
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            RestrictLifetime();
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);

            // Explode into a bunch of holy fire.
            for (int i = 0; i < 3; i++)
            {
                LineParticle spark2 = new LineParticle(Projectile.Center + Main.rand.NextVector2Circular(13, 13), Projectile.velocity * Main.rand.NextFloat(0.5f, 2.1f), false, 12, 1.1f, colorAlt ? (Main.rand.NextBool(5) ? Color.Khaki : Color.Goldenrod) : (Main.rand.NextBool(5) ? Color.Goldenrod : Color.DarkGoldenrod));
                GeneralParticleHandler.SpawnParticle(spark2);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.8f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }
        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float arrowheadCutoff = 0.36f;
            float width = 29f * Projectile.scale;
            float minHeadWidth = 0.02f;
            float maxHeadWidth = width;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(minHeadWidth, maxHeadWidth, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));
            return width;
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float endFadeRatio = 0.41f;

            float completionRatioFactor = 2.7f;
            float globalTimeFactor = 5.3f;
            float endFadeFactor = 3.2f;
            float endFadeTerm = Utils.GetLerpValue(0f, endFadeRatio * 0.5f, completionRatio, true) * endFadeFactor;
            float cosArgument = completionRatio * completionRatioFactor - Main.GlobalTimeWrappedHourly * globalTimeFactor + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(cosArgument) * 0.5f + 0.5f;

            float colorLerpFactor = 0.8f;
            Color startingColor = Color.Lerp(colorAlt ? Color.DarkGoldenrod : Color.Goldenrod, Color.Khaki, startingInterpolant * colorLerpFactor);

            return Color.Lerp(startingColor, colorAlt ? (Color.DarkGoldenrod with { A = 0 } * 0.8f) : (Color.Goldenrod * 0.8f), MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 overallOffset = Projectile.Size * 0.5f;
            overallOffset += Projectile.velocity * 1.4f;
            int numPoints = 92;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(PrimitiveWidthFunction, PrimitiveColorFunction, (_,_) => overallOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), numPoints);
            return false;
        }
    }
}
