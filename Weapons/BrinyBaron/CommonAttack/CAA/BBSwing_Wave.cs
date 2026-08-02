using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
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
        private const float WaveSizeFactor = 0.58f;
        private const float BaseVelocityLoss = 0.012f;
        private const float JudgmentLikeInitialVelocityLoss = 0.052f;
        private const float JudgmentLikeSettledVelocityLoss = 0.018f;
        private const float HitVelocityLossMultiplier = 1.5f;
        private const int BubbleSpawnFrameInterval = 5;

        private int lifeTimer;
        private int bubbleTimer;
        private float initialSpeed;

        private int SpawnStage => Utils.Clamp((int)Projectile.ai[1], 0, 3);
        private bool IsEnhancedWave => SpawnStage == 3 && Projectile.ai[0] >= 2.35f;
        private bool IsAegisBlade => Main.player[Projectile.owner].HeldItem.type == ModContent.ItemType<global::CalamityLegendsComeBack.Weapons.AegisBlade.AegisBlade>();
        private float StageScale => Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultFinalWaveScale;
        private float StageIntensity => 1f + SpawnStage * 0.26f;
        private bool SlowdownBoostApplied
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
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
            Projectile.localNPCHitCooldown = BB_Balance.GetLeftProjectileHitCooldown(BBLeftProjectile.SwordWave);
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialSpeed = Projectile.velocity.Length();
            ApplyStageStats();
            
            // Energetic creation sound for any released wave
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot") with { Volume = 0.65f, Pitch = 0.05f }, Projectile.Center);
        }

        public override void AI()
        {
            lifeTimer++;
            float velocityLoss = GetVelocityLoss();
            Projectile.velocity *= 1f - velocityLoss;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.Opacity = Utils.GetLerpValue(0f, 16f, lifeTimer, true) * Utils.GetLerpValue(0f, 34f, Projectile.timeLeft, true);
            Vector3 lightColor = IsAegisBlade ? new Vector3(0.52f, 0.34f, 0.08f) : new Vector3(0.08f, 0.34f, 0.52f);
            Lighting.AddLight(Projectile.Center, lightColor * (1f + SpawnStage * 0.12f));

            SpawnFlightEffects(Projectile, lifeTimer, SpawnStage, StageIntensity, initialSpeed, IsAegisBlade);
            TrySpawnTrackingBubbles();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!SlowdownBoostApplied)
            {
                SlowdownBoostApplied = true;
                Projectile.netUpdate = true;
            }

            SpawnHitEffects(Projectile, target.Center, SpawnStage, StageIntensity, IsAegisBlade);

            if (IsEnhancedWave && Main.myPlayer == Projectile.owner)
                Main.player[Projectile.owner].GetModPlayer<BBTideValuePlayer>().RegisterEnhancedWaveHit();

            // Post-Plantera sword waves retain their hit effects, but no longer
            // create a persistent tornado or its attached explosion package.
        }

        public override void OnKill(int timeLeft)
        {
            SpawnDissolveEffects(Projectile, SpawnStage, StageIntensity, IsAegisBlade);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lifeTimer < 10)
                return true;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;

            Color[] palette = IsAegisBlade ? new Color[]
            {
                new Color(255, 242, 185),
                new Color(255, 205, 80),
                new Color(255, 145, 52),
                new Color(180, 60, 10)
            } : new Color[]
            {
                new Color(220, 250, 255),
                new Color(115, 215, 255),
                new Color(48, 146, 235),
                new Color(12, 54, 110),
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int trailLength = Projectile.oldPos.Length;
            float segmentLength = Projectile.width * 0.09f;
            Vector2[] shiftedOldPos = new Vector2[trailLength];

            for (int i = 0; i < trailLength; i++)
                shiftedOldPos[i] = Projectile.Center - forward * segmentLength * i;

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");
            float baseWidth = Projectile.width * 0.72f;

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
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * Projectile.Opacity,
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
            float sizeMult = 1f;
            if (Main.player.IndexInRange(Projectile.owner))
            {
                Player owner = Main.player[Projectile.owner];
                var bbAcc = owner.GetModPlayer<BBAccessoryPlayer>();
                var tidePlayer = owner.GetModPlayer<BBTideValuePlayer>();

                if ((bbAcc.BottledBlackPearlEquipped || bbAcc.BottledAircraftCarrierEquipped) && tidePlayer.TideFull)
                {
                    sizeMult = 1.5f;
                    Projectile.damage = (int)(Projectile.damage * 1.25f);
                }
            }

            int size = (int)(BaseSize * StageScale * WaveSizeFactor * sizeMult);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.scale = (0.96f + SpawnStage * 0.08f) * WaveSizeFactor * sizeMult;
            Projectile.Center = center;
        }

        private float GetVelocityLoss()
        {
            float baseLoss = BaseVelocityLoss;
            if (SpawnStage == 0)
            {
                float settle = Utils.GetLerpValue(0f, 52f, lifeTimer, true);
                settle = settle * settle * (3f - 2f * settle);
                baseLoss = MathHelper.Lerp(JudgmentLikeInitialVelocityLoss, JudgmentLikeSettledVelocityLoss, settle);
            }

            return baseLoss * (SlowdownBoostApplied ? HitVelocityLossMultiplier : 1f);
        }

        private static void SpawnDissolveEffects(Projectile projectile, int spawnStage, float stageIntensity, bool isAegisBlade)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color coreColor = isAegisBlade
                ? Color.Lerp(new Color(255, 205, 80), Color.White, 0.35f)
                : Color.Lerp(new Color(92, 210, 255), Color.White, 0.35f);
            int sparkCount = 8 + spawnStage * 2;
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 offset = right * Main.rand.NextFloat(-projectile.width * 0.26f, projectile.width * 0.26f);
                Vector2 velocity = -forward * Main.rand.NextFloat(0.8f, 2.8f) + right * Main.rand.NextFloat(-1.2f, 1.2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    projectile.Center + offset,
                    velocity,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.24f) * stageIntensity,
                    coreColor));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                projectile.Center,
                Vector2.Zero,
                coreColor * 0.48f,
                new Vector2(0.5f, 2.2f),
                forward.ToRotation(),
                0.12f,
                0.18f,
                14));
        }

        private void TrySpawnTrackingBubbles()
        {
            if (IsAegisBlade)
                return;

            if (Projectile.numUpdates != 0 || Main.myPlayer != Projectile.owner)
                return;

            if (Projectile.localAI[0] == -1f)
                return;

            bubbleTimer++;
            if (bubbleTimer < BubbleSpawnFrameInterval)
                return;

            bubbleTimer = 0;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Vector2 spawnPosition =
                Projectile.Center -
                forward * Main.rand.NextFloat(Projectile.width * 0.16f, Projectile.width * 0.38f) +
                right * Main.rand.NextFloat(-Projectile.width * 0.34f, Projectile.width * 0.34f) +
                Main.rand.NextVector2Circular(Projectile.width * 0.08f, Projectile.width * 0.08f);

            Vector2 baseDirection = (-forward).RotatedByRandom(Main.rand.NextFloat(0.35f, 0.95f));
            Vector2 velocity = (baseDirection + right * Main.rand.NextFloatDirection() * 0.18f)
                .SafeNormalize(-forward) * Main.rand.NextFloat(4.8f, 7.2f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<BrinyBaron_HomingLightOrb>(),
                Math.Max(1, (int)(Projectile.damage * 0.22f)),
                Projectile.knockBack * 0.35f,
                Projectile.owner);
        }

        private static void SpawnFlightEffects(Projectile projectile, int lifeTimer, int spawnStage, float stageIntensity, float initialSpeed, bool isAegisBlade)
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

                        //GlowOrbParticle wakeOrb = new GlowOrbParticle(
                        //    edgePos,
                        //    edgeVelocity,
                        //    false,
                        //    Main.rand.Next(9, 15),
                        //    MathHelper.Lerp(0.4f, 0.76f, speedRatio) * (1f + burst * 0.08f),
                        //    side < 0 ? new Color(70, 180, 255) : new Color(185, 245, 255),
                        //    true,
                        //    false,
                        //    true);
                        //GeneralParticleHandler.SpawnParticle(wakeOrb);
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
                        isAegisBlade ? (Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch) : (Main.rand.NextBool(4) ? DustID.Frost : DustID.Water),
                        dustVelocity,
                        0,
                        isAegisBlade
                            ? Color.Lerp(new Color(255, 180, 54), new Color(255, 248, 200), Main.rand.NextFloat(0.15f, 0.9f))
                            : Color.Lerp(new Color(105, 205, 255), new Color(215, 248, 255), Main.rand.NextFloat(0.15f, 0.9f)),
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
                    isAegisBlade
                        ? Color.Lerp(new Color(255, 150, 48), new Color(255, 245, 190), 1f - speedRatio)
                        : Color.Lerp(new Color(80, 170, 255), new Color(220, 250, 255), 1f - speedRatio),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(slowOrb);
            }

            if (spawnStage >= 1 && lifeTimer % 3 == 0)
            {
                const float goldenAngle = 2.3999631f;
                int streamCount = spawnStage >= 2 ? 2 : 1;

                for (int stream = 0; stream < streamCount; stream++)
                {
                    float side = stream == 0 ? -1f : 1f;
                    float phase = lifeTimer * 0.145f + projectile.identity * 0.37f + goldenAngle * (stream + 1);
                    float curve = (float)Math.Sin(phase);
                    float counterCurve = (float)Math.Cos(phase * 0.61803398875f);
                    float lateralDistance = visualRadius * side * MathHelper.Lerp(0.28f, 0.68f, Math.Abs(curve));
                    float axialDistance = visualRadius * MathHelper.Lerp(0.12f, 0.36f, 0.5f + counterCurve * 0.5f);

                    Vector2 sparkPos =
                        wakeAnchor -
                        forward * axialDistance +
                        right * lateralDistance +
                        right * sway * 0.15f;

                    Vector2 sparkVelocity =
                        -forward * MathHelper.Lerp(1.25f, 2.65f + spawnStage * 0.35f, speedRatio) +
                        right * side * MathHelper.Lerp(0.18f, 0.8f, Math.Abs(counterCurve));

                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        sparkPos,
                        sparkVelocity * stageIntensity,
                        false,
                        Main.rand.Next(9, 14),
                        Main.rand.NextFloat(0.22f, 0.42f) * (1f + spawnStage * 0.08f),
                        isAegisBlade
                            ? Color.Lerp(new Color(255, 190, 68), Color.White, 0.2f + Math.Abs(curve) * 0.25f)
                            : Color.Lerp(new Color(95, 205, 255), Color.White, 0.2f + Math.Abs(curve) * 0.25f)));
                }
            }
        }

        private static void SpawnHitEffects(Projectile projectile, Vector2 hitCenter, int spawnStage, float stageIntensity, bool isAegisBlade)
        {
            Vector2 pos = hitCenter;
            float radius = projectile.width * 0.4f;

            GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                pos,
                0.08f + spawnStage * 0.012f,
                18 + spawnStage * 2,
                0.9f + spawnStage * 0.08f,
                isAegisBlade
                    ? Color.Lerp(new Color(255, 205, 80), Color.White, 0.32f)
                    : Color.Lerp(new Color(115, 220, 255), Color.White, 0.32f)));

            for (int i = 0; i < 4 + spawnStage * 2; i++)
            {
                Vector2 spawnPos = pos + Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = -projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.42f) * Main.rand.NextFloat(2.4f, 5.6f);

                Dust impactDust = Dust.NewDustPerfect(
                    spawnPos,
                    isAegisBlade ? (Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch) : (Main.rand.NextBool() ? DustID.Frost : DustID.Water),
                    velocity,
                    0,
                    isAegisBlade
                        ? Color.Lerp(new Color(255, 175, 52), Color.White, Main.rand.NextFloat(0.25f, 0.8f))
                        : Color.Lerp(new Color(95, 195, 255), Color.White, Main.rand.NextFloat(0.25f, 0.8f)),
                    Main.rand.NextFloat(0.95f, 1.25f) * stageIntensity);
                impactDust.noGravity = true;
            }
        }
    }
}
