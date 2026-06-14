using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPlantera_Flame : ModProjectile, ILocalizedModType
    {
        private static readonly Color ThemeColor = new Color(255, 150, 20); // Warm reddish-yellow theme

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int time = 0;

        public override void SetStaticDefaults()
        {
            // Enable trail caching for primitive rendering
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2; // 3 updates per tick
            Projectile.penetrate = -1; // Infinite pierce
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12; // Cooldown for hitting the same target
        }

        public override void AI()
        {
            // Face the direction of travel
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.45f);

            // Replicating Telluric Glare's signature muzzle flash particle release when passing the muzzle
            if (time == 4)
            {
                if (!Main.dedServ)
                {
                    Color orangeRed = new Color(255, 69, 0);
                    Color gold = new Color(255, 180, 20);

                    for (int i = 0; i < 5; i++)
                    {
                        if (i < 3)
                        {
                            Dust dust = Dust.NewDustPerfect(
                                Projectile.Center,
                                ModContent.DustType<CalamityMod.Dusts.LightDust>(),
                                (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f)
                            );
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(0.85f, 1.15f);
                            dust.color = Main.rand.NextBool(5) ? Color.Orange : gold;
                            dust.noLightEmittence = true;
                        }
                        else
                        {
                            Particle spark = new CustomSpark(
                                Projectile.Center,
                                (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f),
                                "CalamityMod/Particles/ProvidenceMarkParticle",
                                false,
                                17,
                                Main.rand.NextFloat(1.15f, 1.3f),
                                Color.Lerp(orangeRed, Color.White, Main.rand.NextFloat(0f, 0.7f)),
                                new Vector2(1.3f, 0.5f),
                                true,
                                false,
                                0,
                                false,
                                false,
                                Main.rand.NextFloat(0.3f, 0.4f)
                            );
                            GeneralParticleHandler.SpawnParticle(spark);
                        }

                        Particle spark2 = new GlowSparkParticle(
                            Projectile.Center,
                            (Projectile.velocity).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1f),
                            false,
                            9,
                            0.017f,
                            gold,
                            new Vector2(1.5f, 0.7f),
                            true,
                            false,
                            1.3f
                        );
                        GeneralParticleHandler.SpawnParticle(spark2);
                    }
                }
            }

            // Periodically emit particles and sparks along the trail
            if (time % 6 == 0 && time > 4)
            {
                if (!Main.dedServ)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        ModContent.DustType<CalamityMod.Dusts.LightDust>(),
                        (Projectile.velocity * 0.35f).RotatedByRandom(0.15f) * Main.rand.NextFloat(0.3f, 0.8f)
                    );
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.85f, 1.15f);
                    dust.color = Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.1f, 0.4f));
                    dust.noLightEmittence = true;

                    if (Main.rand.NextBool(2))
                    {
                        GeneralParticleHandler.SpawnParticle(new CustomSpark(
                            Projectile.Center,
                            (Projectile.velocity * 0.2f).RotatedByRandom(0.25f) * Main.rand.NextFloat(0.4f, 1.2f),
                            "CalamityMod/Particles/ThinEndedLine",
                            false,
                            10,
                            Main.rand.NextFloat(0.08f, 0.13f),
                            Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.2f, 0.5f)),
                            new Vector2(0.3f, 1.1f),
                            true,
                            false
                        ));
                    }
                }
            }

            time++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.HolyFlames>(), 180);

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LunicImpact") { Volume = 0.16f, Pitch = Main.rand.NextFloat(0.1f, 0.3f) }, target.Center);

            // Spawn impact particles
            for (int i = 0; i < 3; i++)
            {
                LineParticle spark = new LineParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8, 8),
                    Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f) + Main.rand.NextVector2Circular(2f, 2f),
                    false,
                    12,
                    0.95f,
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.1f, 0.4f))
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.4f,
                ThemeColor * 0.55f,
                new Vector2(0.3f, 0.9f),
                direction.ToRotation(),
                0.08f,
                0.015f,
                10
            ));
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float arrowheadCutoff = 0.36f;
            float width = 24f * Projectile.scale;
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

            Color startingColor = Color.Lerp(ThemeColor, Color.White, startingInterpolant * 0.7f);

            return Color.Lerp(startingColor, ThemeColor * 0.8f, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            Vector2 overallOffset = Projectile.Size * 0.5f;
            overallOffset += Projectile.velocity * 1.4f;
            int numPoints = 32;

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    PrimitiveWidthFunction,
                    PrimitiveColorFunction,
                    (_, _) => overallOffset,
                    pixelate: false,
                    shader: GameShaders.Misc["CalamityMod:TrailStreak"]
                ),
                numPoints
            );

            return false;
        }
    }
}
