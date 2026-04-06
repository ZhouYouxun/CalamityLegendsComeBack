using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.World;
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
        public new string LocalizationCategory => "Projectiles";

        private const int BaseSize = 200;

        private int lifeTimer;
        private float initialSpeed;

        private int HighestUnlockedStage =>
            DownedBossSystem.downedBoomerDuke ? 3 :
            NPC.downedFishron ? 2 :
            Main.hardMode ? 1 : 0;

        private int SpawnStage => (int)Projectile.localAI[0];
        private float StageScale => 1f + SpawnStage * 0.16f;
        private float StageIntensity => 1f + SpawnStage * 0.26f;

        public override string Texture => "Terraria/Images/Projectile_0";

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
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 90 * Projectile.extraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lifeTimer < 10)
                return true;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            Color[] palette = new Color[]
            {
                new Color(220, 250, 255),
                new Color(115, 215, 255),
                new Color(48, 146, 235),
                new Color(12, 54, 110),
            };

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);

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
                int idx = (int)(t * (palette.Length - 1));
                idx = Utils.Clamp(idx, 0, palette.Length - 1);

                Color c = palette[idx];
                c *= (1f - t) * Projectile.Opacity * 1.2f;
                c.A = 0;
                return c;
            }

            Vector2 offsetFunc(float t, Vector2 v) => Vector2.Zero;

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    offsetFunc,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]),
                60);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Main.spriteBatch.Draw(
                tex,
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

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialSpeed = Projectile.velocity.Length();
            Projectile.localAI[0] = HighestUnlockedStage;
            ApplyStageStats();
        }

        public override void AI()
        {
            lifeTimer++;

            float drag = SpawnStage >= 3 ? 0.994f : SpawnStage >= 2 ? 0.993f : SpawnStage >= 1 ? 0.992f : 0.99f;
            Projectile.velocity *= drag;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float currentSpeed = Projectile.velocity.Length();
            float speedRatio = initialSpeed <= 0.001f ? 0f : MathHelper.Clamp(currentSpeed / initialSpeed, 0f, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.52f) * (1f + SpawnStage * 0.12f));

            float visualRadius = Projectile.width * 0.5f;
            float t = Main.GameUpdateCount * 0.2f;
            float sway = (float)Math.Sin(t * (2.4f + SpawnStage * 0.2f)) * MathHelper.Lerp(3f, 9f + SpawnStage * 2f, 1f - speedRatio);

            Vector2 wakeAnchor = Projectile.Center - forward * MathHelper.Lerp(visualRadius * 0.08f, visualRadius * 0.22f, speedRatio);
            float edgeDistance = visualRadius * MathHelper.Lerp(0.74f, 0.94f, speedRatio);
            float fillDistance = visualRadius * 0.88f;

            int edgeInterval = Math.Max(1, 3 - SpawnStage);
            if (lifeTimer % edgeInterval == 0)
            {
                int edgeBursts = SpawnStage >= 2 ? 2 : 1;
                for (int burst = 0; burst < edgeBursts; burst++)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 edgePos = wakeAnchor + right * side * (edgeDistance + sway * 0.3f) - forward * burst * visualRadius * 0.08f;
                        Vector2 edgeVelocity =
                            -forward * MathHelper.Lerp(1.8f, 4.8f + SpawnStage * 0.8f, speedRatio) +
                            right * side * MathHelper.Lerp(0.55f, 1.85f + SpawnStage * 0.2f, speedRatio);

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
                int dustCount = 2 + SpawnStage;
                for (int i = 0; i < dustCount; i++)
                {
                    float band = (float)Math.Sqrt(Main.rand.NextFloat());
                    float sideBias = Main.rand.NextFloatDirection();
                    Vector2 dustPos =
                        wakeAnchor +
                        right * sideBias * fillDistance * MathHelper.Lerp(0.18f, 1f, band) -
                        forward * Main.rand.NextFloat(visualRadius * 0.04f, visualRadius * 0.24f);

                    Vector2 dustVelocity =
                        -forward * Main.rand.NextFloat(1f, MathHelper.Lerp(2.4f, 4.8f + SpawnStage * 0.7f, speedRatio)) +
                        right * sideBias * Main.rand.NextFloat(0.1f, 1.25f + SpawnStage * 0.12f);

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

            int driftInterval = Math.Max(2, 4 - SpawnStage);
            if (speedRatio < 0.78f && lifeTimer % driftInterval == 0)
            {
                float driftBand = Main.rand.NextFloatDirection();
                Vector2 driftPos =
                    wakeAnchor +
                    right * driftBand * visualRadius * Main.rand.NextFloat(0.25f, 0.84f) -
                    forward * Main.rand.NextFloat(visualRadius * 0.12f, visualRadius * 0.34f) +
                    right * sway * 0.4f;

                Vector2 driftVelocity =
                    -forward * MathHelper.Lerp(0.5f, 1.55f + SpawnStage * 0.2f, speedRatio) +
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

                Dust slowDust = Dust.NewDustPerfect(
                    driftPos + Main.rand.NextVector2Circular(visualRadius * 0.08f, visualRadius * 0.08f),
                    DustID.Water,
                    driftVelocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.8f, 1.15f),
                    0,
                    new Color(195, 245, 255),
                    MathHelper.Lerp(0.85f, 1.16f, 1f - speedRatio));
                slowDust.noGravity = true;
            }

            if (SpawnStage >= 1 && lifeTimer % 3 == 0)
            {
                Vector2 sparkPos = Projectile.Center + right * Main.rand.NextFloatDirection() * visualRadius * Main.rand.NextFloat(0.45f, 0.82f);
                Vector2 sparkVelocity =
                    -forward * Main.rand.NextFloat(3.2f, 5.4f) * StageIntensity +
                    right * Main.rand.NextFloatDirection() * Main.rand.NextFloat(0.4f, 1.2f);

                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        sparkPos,
                        sparkVelocity,
                        false,
                        Main.rand.Next(8, 12),
                        0.09f * StageIntensity,
                        Color.Lerp(new Color(120, 220, 255), Color.White, 0.35f),
                        new Vector2(2.8f, 0.42f),
                        true));
            }

            TrySpawnTrailingStars(forward, right, visualRadius);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = Projectile.Center;
            float radius = Projectile.width * 0.4f;

            for (int i = 0; i < 4 + SpawnStage * 2; i++)
            {
                Vector2 spawnPos = pos + Main.rand.NextVector2Circular(radius, radius);
                Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.42f) * Main.rand.NextFloat(2.4f, 5.6f);

                Dust impactDust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool() ? DustID.Frost : DustID.Water,
                    vel,
                    0,
                    Color.Lerp(new Color(95, 195, 255), Color.White, Main.rand.NextFloat(0.25f, 0.8f)),
                    Main.rand.NextFloat(0.95f, 1.25f) * StageIntensity);
                impactDust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
        }

        private void ApplyStageStats()
        {
            Vector2 center = Projectile.Center;
            int size = (int)(BaseSize * StageScale);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.scale = 0.9f + SpawnStage * 0.06f;
            Projectile.tileCollide = SpawnStage < 1;
            Projectile.Center = center;
        }

        private void TrySpawnTrailingStars(Vector2 forward, Vector2 right, float visualRadius)
        {
            if (SpawnStage < 2 || Projectile.numUpdates != 0 || Main.myPlayer != Projectile.owner)
                return;

            int starInterval = SpawnStage >= 3 ? 7 : 10;
            if (lifeTimer % starInterval != 0)
                return;

            int spawnCount = SpawnStage >= 3 && Main.rand.NextBool(3) ? 2 : 1;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 spawnPos =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(visualRadius * 0.18f, visualRadius * 0.42f) +
                    right * Main.rand.NextFloat(-visualRadius * 0.38f, visualRadius * 0.38f);

                Vector2 launchVelocity =
                    (-forward * Main.rand.NextFloat(6.8f, 10.2f) +
                    right * Main.rand.NextFloat(-1.4f, 1.4f) -
                    Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.1f)).SafeNormalize(-forward) *
                    Main.rand.NextFloat(6.2f, 9.4f);

                int starDamage = Math.Max(1, (int)(Projectile.damage * (SpawnStage >= 3 ? 0.24f : 0.18f)));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    launchVelocity,
                    ModContent.ProjectileType<BBSD_Star>(),
                    starDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }
    }
}
