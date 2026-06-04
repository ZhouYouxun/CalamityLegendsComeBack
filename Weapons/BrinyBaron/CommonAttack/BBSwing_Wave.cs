using CalamityLegendsComeBack.Weapons.BrinyBaron.EXSkill;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_Wave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "Terraria/Images/Projectile_0";

        private const int BaseSize = 200;
        private const float DefaultFinalWaveScale = 2.35f;
        private const float WaveSizeFactor = 0.7f;

        private int lifeTimer;
        private float initialSpeed;

        private int SpawnStage => Utils.Clamp((int)Projectile.ai[1], 0, 3);
        private float StageScale => Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultFinalWaveScale;
        private float StageIntensity => 1f + SpawnStage * 0.26f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = BaseSize;
            Projectile.height = BaseSize;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.light = 0.45f;
            Projectile.scale = 0.9f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 90 * Projectile.extraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialSpeed = Projectile.velocity.Length();
            ApplyStageStats();
        }

        public override void AI()
        {
            lifeTimer++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.52f) * (1f + SpawnStage * 0.12f));

            SpawnFlightEffects(Projectile, lifeTimer, SpawnStage, StageIntensity, initialSpeed);
            TrySpawnTrackingBubbles();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnHitEffects(Projectile, SpawnStage, StageIntensity);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Main.player[Projectile.owner].GetModPlayer<BBEXPlayer>().AddTide();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lifeTimer < 10)
                return true;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;

            Color[] palette =
            {
                new Color(220, 250, 255),
                new Color(115, 215, 255),
                new Color(48, 146, 235),
                new Color(12, 54, 110),
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int trailLength = Projectile.oldPos.Length;
            float segmentLength = Projectile.width * 0.09f;
            Vector2[] shiftedOldPos = new Vector2[trailLength];

            for (int i = 0; i < trailLength; i++)
                shiftedOldPos[i] = Projectile.Center - forward * segmentLength * i;

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");
            float baseWidth = Projectile.width;

            float WidthFunc(float t, Vector2 v)
            {
                float shape = (float)Math.Sin(t * MathHelper.Pi);
                shape = (float)Math.Pow(shape, 0.6f);
                shape = MathHelper.Lerp(0.25f, 1f, shape);
                return baseWidth * shape;
            }

            Color ColorFunc(float t, Vector2 v)
            {
                int index = Utils.Clamp((int)(t * (palette.Length - 1)), 0, palette.Length - 1);
                Color color = palette[index];
                color *= (1f - t) * Projectile.Opacity * 1.2f;
                color.A = 0;
                return color;
            }

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    (_, _) => Vector2.Zero,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]),
                60);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Main.spriteBatch.Draw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }

        private void ApplyStageStats()
        {
            Vector2 center = Projectile.Center;
            int size = (int)(BaseSize * StageScale * WaveSizeFactor);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.scale = (0.96f + SpawnStage * 0.08f) * WaveSizeFactor;
            Projectile.Center = center;
        }

        private void TrySpawnTrackingBubbles()
        {
            if (Projectile.numUpdates != 0 || Main.myPlayer != Projectile.owner || lifeTimer % 8 != 0)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            int count = SpawnStage >= 3 && Main.rand.NextBool(2) ? 2 : 1;

            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPosition =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(Projectile.width * 0.04f, Projectile.width * 0.22f) +
                    right * Main.rand.NextFloat(-Projectile.width * 0.35f, Projectile.width * 0.35f);

                Vector2 velocity = (-forward * Main.rand.NextFloat(0.6f, 1.8f) + right * Main.rand.NextFloatDirection() * 1.2f)
                    .SafeNormalize(-forward) * Main.rand.NextFloat(4.5f, 7f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_HomingBubble>(),
                    Math.Max(1, (int)(Projectile.damage * 0.22f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        private static void SpawnFlightEffects(Projectile projectile, int lifeTimer, int spawnStage, float stageIntensity, float initialSpeed)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float currentSpeed = projectile.velocity.Length();
            float speedRatio = initialSpeed <= 0.001f ? 0f : MathHelper.Clamp(currentSpeed / initialSpeed, 0f, 1f);

            float visualRadius = projectile.width * 0.5f;
            float t = Main.GameUpdateCount * 0.2f;
            float sway = (float)Math.Sin(t * (2.4f + spawnStage * 0.2f)) * MathHelper.Lerp(3f, 9f + spawnStage * 2f, 1f - speedRatio);

            Vector2 wakeAnchor = projectile.Center - forward * MathHelper.Lerp(visualRadius * 0.08f, visualRadius * 0.22f, speedRatio);
            float edgeDistance = visualRadius * MathHelper.Lerp(0.74f, 0.94f, speedRatio);
            float fillDistance = visualRadius * 0.88f;

            int edgeInterval = Math.Max(1, 3 - spawnStage);
            if (lifeTimer % edgeInterval == 0)
            {
                int edgeBursts = spawnStage >= 2 ? 2 : 1;
                for (int burst = 0; burst < edgeBursts; burst++)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 edgePos = wakeAnchor + right * side * (edgeDistance + sway * 0.3f) - forward * burst * visualRadius * 0.08f;
                        Vector2 edgeVelocity =
                            -forward * MathHelper.Lerp(1.8f, 4.8f + spawnStage * 0.8f, speedRatio) +
                            right * side * MathHelper.Lerp(0.55f, 1.85f + spawnStage * 0.2f, speedRatio);

                        GlowOrbParticle wakeOrb = new GlowOrbParticle(
                            edgePos,
                            edgeVelocity,
                            false,
                            Main.rand.Next(9, 15),
                            MathHelper.Lerp(0.4f, 0.76f, speedRatio) * (1f + burst * 0.08f),
                            side < 0 ? new Color(70, 180, 255) : new Color(185, 245, 255),
                            true,
                            false,
                            true);
                        GeneralParticleHandler.SpawnParticle(wakeOrb);
                    }
                }
            }

            if (lifeTimer % 2 == 0)
            {
                int dustCount = 2 + spawnStage;
                for (int i = 0; i < dustCount; i++)
                {
                    float band = (float)Math.Sqrt(Main.rand.NextFloat());
                    float sideBias = Main.rand.NextFloatDirection();
                    Vector2 dustPos =
                        wakeAnchor +
                        right * sideBias * fillDistance * MathHelper.Lerp(0.18f, 1f, band) -
                        forward * Main.rand.NextFloat(visualRadius * 0.04f, visualRadius * 0.24f);

                    Vector2 dustVelocity =
                        -forward * Main.rand.NextFloat(1f, MathHelper.Lerp(2.4f, 4.8f + spawnStage * 0.7f, speedRatio)) +
                        right * sideBias * Main.rand.NextFloat(0.1f, 1.25f + spawnStage * 0.12f);

                    Dust wakeDust = Dust.NewDustPerfect(
                        dustPos,
                        Main.rand.NextBool(4) ? DustID.Frost : DustID.Water,
                        dustVelocity,
                        0,
                        Color.Lerp(new Color(105, 205, 255), new Color(215, 248, 255), Main.rand.NextFloat(0.15f, 0.9f)),
                        MathHelper.Lerp(0.88f, 1.25f, speedRatio) * Main.rand.NextFloat(0.92f, 1.12f));
                    wakeDust.noGravity = true;
                }
            }

            int driftInterval = Math.Max(2, 4 - spawnStage);
            if (speedRatio < 0.78f && lifeTimer % driftInterval == 0)
            {
                float driftBand = Main.rand.NextFloatDirection();
                Vector2 driftPos =
                    wakeAnchor +
                    right * driftBand * visualRadius * Main.rand.NextFloat(0.25f, 0.84f) -
                    forward * Main.rand.NextFloat(visualRadius * 0.12f, visualRadius * 0.34f) +
                    right * sway * 0.4f;

                Vector2 driftVelocity =
                    -forward * MathHelper.Lerp(0.5f, 1.55f + spawnStage * 0.2f, speedRatio) +
                    right * driftBand * Main.rand.NextFloat(0.08f, 0.5f);

                GlowOrbParticle slowOrb = new GlowOrbParticle(
                    driftPos,
                    driftVelocity,
                    false,
                    Main.rand.Next(10, 16),
                    MathHelper.Lerp(0.35f, 0.62f, 1f - speedRatio),
                    Color.Lerp(new Color(80, 170, 255), new Color(220, 250, 255), 1f - speedRatio),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(slowOrb);
            }

            if (spawnStage >= 1 && lifeTimer % 3 == 0)
            {
                Vector2 sparkPos =
                    projectile.Center -
                    forward * visualRadius * 0.18f +
                    right * Main.rand.NextFloatDirection() * visualRadius * Main.rand.NextFloat(0.35f, 0.7f);

                Vector2 sparkVelocity =
                    -forward * Main.rand.NextFloat(2.0f, 3.6f) * stageIntensity +
                    right * Main.rand.NextFloatDirection() * Main.rand.NextFloat(0.2f, 0.7f);

                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        sparkPos,
                        sparkVelocity,
                        false,
                        Main.rand.Next(6, 9),
                        0.055f * stageIntensity,
                        Color.Lerp(new Color(120, 220, 255), Color.White, 0.22f),
                        new Vector2(3.6f, 0.28f),
                        true));
            }
        }

        private static void SpawnHitEffects(Projectile projectile, int spawnStage, float stageIntensity)
        {
            Vector2 pos = projectile.Center;
            float radius = projectile.width * 0.4f;

            for (int i = 0; i < 4 + spawnStage * 2; i++)
            {
                Vector2 spawnPos = pos + Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = -projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.42f) * Main.rand.NextFloat(2.4f, 5.6f);

                Dust impactDust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool() ? DustID.Frost : DustID.Water,
                    velocity,
                    0,
                    Color.Lerp(new Color(95, 195, 255), Color.White, Main.rand.NextFloat(0.25f, 0.8f)),
                    Main.rand.NextFloat(0.95f, 1.25f) * stageIntensity);
                impactDust.noGravity = true;
            }
        }
    }
}
