using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal partial class SHPCRight_HoulOut
    {
        #region ===== 特效状态 =====

        private int stageOutlineTimer;
        private const int StageOutlineDuration = 24;

        private Vector2 normalShotFXLastCenter = Vector2.Zero;
        private readonly List<Particle> normalShotFXParticles = new();
        private int apoctosisCoreGlowTime;
        private float apoctosisCoreHeatRedInterpolant;

        #endregion

        #region ===== 音效 =====

        private void PlayStartupSound()
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Sound/SHPC/M14拉枪"
                : "CalamityLegendsComeBack/Sound/SHPC/双刃镰启动音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 0.804f : 0.67f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        private void PlayNormalFireSound()
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Sound/SHPC/M14开枪"
                : "CalamityLegendsComeBack/Sound/SHPC/双刃镰开火音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 0.804f : 0.67f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        private void PlayStageUpSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/迫击哨戒炮单次攻击")
                {
                    Volume = 5.2f,
                    Pitch = 0.2f
                },
                GunTipPosition
            );

            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Custom/ExoMechs/ApolloMissileLaunch")
                {
                    Volume = 0.55f,
                    Pitch = 0.35f,
                    MaxInstances = 6
                },
                GunTipPosition
            );
        }

        private void PlayManualCooldownSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/解放者机甲左手火箭弹")
                {
                    Volume = 2.7f,
                    Pitch = 0.2f
                },
                Projectile.Center
            );

            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Custom/ExoMechs/ApolloMissileLaunch")
                {
                    Volume = 1.1f,
                    Pitch = -0.12f,
                    MaxInstances = 4
                },
                Projectile.Center
            );
        }

        private void PlayRocketSalvoSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot")
                {
                    Volume = 1f,
                    Pitch = 0f
                },
                GunTipPosition
            );
        }

        private void PlayManaStarvedSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Custom/ExoMechs/ArtemisApolloDash")
                {
                    Volume = 0.42f,
                    Pitch = 0.25f,
                    MaxInstances = 2
                },
                Projectile.Center
            );
        }

        private void PlayMaxHeatEntrySound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Custom/ExoMechs/ArtemisApolloDash")
                {
                    Volume = 0.57f,
                    Pitch = -0.18f,
                    MaxInstances = 2
                },
                Projectile.Center
            );
        }

        private void PlayForcedShutdownSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Custom/ExoMechs/AresEnraged")
                {
                    Volume = 1.15f,
                    Pitch = -0.08f,
                    MaxInstances = 2
                },
                Projectile.Center
            );
        }

        #endregion

        #region ===== 特效：阶段与状态 =====

        private void TriggerStageOutlinePulse()
        {
            stageOutlineTimer = StageOutlineDuration;
            Owner.GetModPlayer<SHPCRight_Player>().TriggerHeatBarOutlinePulse(StageOutlineDuration);
        }

        private void SpawnStageUpEnergyBurst()
        {
            PlayStageUpSound();

            for (int i = 0; i < 12; i++)
            {
                Vector2 upward = -Vector2.UnitY.RotatedByRandom(0.4f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.RainbowMk2
                );

                dust.velocity = upward * Main.rand.NextFloat(3f, 7f);
                dust.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.4f, 1f));
                dust.scale = Main.rand.NextFloat(1.0f, 1.4f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity =
                    -Vector2.UnitY.RotatedByRandom(0.5f) *
                    Main.rand.NextFloat(1.5f, 3.5f);

                float scale = Main.rand.NextFloat(0.4f, 0.7f);
                Color color = Color.Lerp(Color.Orange, Color.White, Main.rand.NextFloat(0.3f, 0.8f));

                SquishyLightParticle particle = new(
                    GunTipPosition,
                    velocity,
                    scale,
                    color,
                    Main.rand.Next(16, 24)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity =
                    -Vector2.UnitY.RotatedByRandom(0.6f) *
                    Main.rand.NextFloat(1f, 2.5f);

                GlowOrbParticle glow = new GlowOrbParticle(
                    GunTipPosition + Main.rand.NextVector2Circular(4f, 4f),
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.7f, 1.0f),
                    Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.3f, 0.8f)),
                    true,
                    true
                );

                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        private void SpawnCoolingVentMist()
        {
            if (frameCounter % 3 != 0)
                return;

            Vector2 forward = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 back = -forward;
            float baseAngle = back.ToRotation();
            float angleOffset = MathHelper.Pi / 9f;
            float finalAngle = forward.X > 0f ? baseAngle - angleOffset : baseAngle + angleOffset;

            Vector2 direction = finalAngle.ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Vector2 spawnPos =
                GunBackPosition
                + forward * Main.rand.NextFloat(-10f, 4f)
                + right * Main.rand.NextFloat(-6f, 6f);

            direction = direction.RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f));

            Particle smoke = new MediumMistParticle(
                spawnPos,
                direction * Main.rand.NextFloat(5f, 11f),
                Color.White,
                Color.Transparent,
                Main.rand.NextFloat(0.7f, 1.2f),
                Main.rand.NextFloat(180f, 220f)
            );

            GeneralParticleHandler.SpawnParticle(smoke);
        }

        private void SpawnForcedShutdownGasBurst()
        {
            Vector2 forward = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 basePos = GunBackPosition - forward * 4f;
            Color toxicCore = new(178, 235, 92);
            Color toxicEdge = new(72, 112, 58);

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 spawnPos = basePos + direction * Main.rand.NextFloat(2f, 8f);
                Vector2 velocity = direction * Main.rand.NextFloat(2.6f, 5.4f);

                Particle gas = new MediumMistParticle(
                    spawnPos,
                    velocity,
                    Color.Lerp(toxicCore, Color.White, Main.rand.NextFloat(0.05f, 0.28f)),
                    toxicEdge * 0.12f,
                    Main.rand.NextFloat(0.72f, 1.05f),
                    Main.rand.NextFloat(110f, 150f)
                );

                GeneralParticleHandler.SpawnParticle(gas);
            }
        }

        private void SpawnTopHeatPlayerGlow(Player player, SHPCRight_Player heatPlayer)
        {
            if (Main.dedServ || MaxHeatStage <= 1)
                return;

            bool displayedTopHeat = heatPlayer.HasAnyHeat() &&
                heatPlayer.GetDisplayedHeatLevel() >= MaxHeatStage;
            if (!displayedTopHeat && stage < MaxHeatStage)
                return;
            if (!Main.rand.NextBool(3))
                return;

            Color brimstoneRed = new(135, 20, 36);
            Color brimstoneDark = new(72, 12, 22);
            Color demonicViolet = new(128, 55, 175);
            Color smokeGray = new(58, 50, 48);

            Vector2 ringPos = player.Center + new Vector2(
                Main.rand.NextFloat(-player.width * 0.46f, player.width * 0.46f),
                Main.rand.NextFloat(-player.height * 0.54f, player.height * 0.30f));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                ringPos,
                Vector2.Zero,
                Color.Lerp(brimstoneDark, brimstoneRed, Main.rand.NextFloat(0.25f, 0.75f)) * 0.48f,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.012f,
                Main.rand.NextFloat(0.06f, heatPlayer.IsForcedShutdownCooling() ? 0.14f : 0.11f),
                15));

            int dustCount = heatPlayer.IsForcedShutdownCooling() ? 4 : 3;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 bodyPoint = player.Center + new Vector2(
                    Main.rand.NextFloat(-player.width * 0.48f, player.width * 0.48f),
                    Main.rand.NextFloat(-player.height * 0.56f, player.height * 0.34f));

                Dust dust = Dust.NewDustPerfect(
                    bodyPoint,
                    Main.rand.NextBool(3) ? DustID.RuneWizard : (int)CalamityDusts.Brimstone,
                    player.velocity + new Vector2(Main.rand.NextFloat(-2.0f, 2.0f), Main.rand.NextFloat(-3.2f, -0.7f)),
                    110,
                    Main.rand.NextBool() ? brimstoneRed : brimstoneDark,
                    Main.rand.NextFloat(0.72f, 1.15f));
                dust.noGravity = true;
            }

            if (stage >= MaxHeatStage && Main.rand.NextBool(2))
            {
                Vector2 sparkPos = player.Center + new Vector2(
                    Main.rand.NextFloat(-player.width * 0.40f, player.width * 0.40f),
                    Main.rand.NextFloat(-player.height * 0.32f, player.height * 0.30f));
                Vector2 sparkVel = new(Main.rand.NextFloat(-player.width / 8f, player.width / 8f), Main.rand.NextFloat(-player.height / 18f, -player.height / 24f));
                Particle demonicSpark = new VelChangingSpark(
                    sparkPos,
                    sparkVel + player.velocity,
                    new Vector2(-sparkVel.X * 0.5f, sparkVel.Y * 2f) * 2.2f,
                    "CalamityMod/Particles/SmallBloom",
                    Main.rand.Next(13, 19),
                    Main.rand.NextFloat(0.045f, 0.08f),
                    Color.Lerp(demonicViolet, brimstoneRed, Main.rand.NextFloat(0.2f, 0.55f)) * 0.50f,
                    new Vector2(0.56f, 1.05f),
                    true,
                    false,
                    0,
                    false,
                    0.35f,
                    0.08f);
                GeneralParticleHandler.SpawnParticle(demonicSpark);
            }

            if (stage >= MaxHeatStage && Main.rand.NextBool(3))
            {
                Particle smoke = new SmallSmokeParticle(
                    player.Center + Main.rand.NextVector2Circular(player.width * 0.44f, player.height * 0.50f),
                    player.velocity * 0.18f - Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.1f, 3.0f),
                    smokeGray,
                    Color.Lerp(Color.Black, smokeGray, 0.35f),
                    Main.rand.NextFloat(0.35f, 0.72f),
                    0.40f,
                    Main.rand.NextFloat(-0.04f, 0.04f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        #endregion

        #region ===== 特效：普通开火 =====

        public override void PostDraw(Color lightColor)
        {
            DrawApoctosisCoreGlow();
        }

        private void DrawApoctosisCoreGlow()
        {
            if (spawnDelay > 0 || Main.dedServ)
                return;

            float manaPercent = Owner.statManaMax2 <= 0 ? 0f : Owner.statMana / (float)Owner.statManaMax2;
            float manaPower = MathHelper.Clamp(visualProgress, 0f, 1f);
            bool manaStarved = manaStarvedStopTimer > 0;
            bool cooling = fireStopTimer > 0 && !manaStarved;
            bool firing = !cooling && !manaStarved;
            float targetRedInterpolant = stage >= MaxHeatStage ? 1f : 0f;
            apoctosisCoreHeatRedInterpolant = MathHelper.Lerp(apoctosisCoreHeatRedInterpolant, targetRedInterpolant, 0.08f);

            Color techBlue = new(70, 190, 255);
            Color redHeat = new(255, 55, 38);
            Color manaStarvedRed = new(255, 34, 42);
            Color coolingYellow = new(255, 235, 80);
            Color effectsColor = manaStarved
                ? manaStarvedRed
                : cooling
                ? coolingYellow
                : Color.Lerp(techBlue, redHeat, apoctosisCoreHeatRedInterpolant);
            Color coreWhite = manaStarved
                ? new Color(255, 188, 188)
                : cooling
                ? new Color(255, 255, 205)
                : Color.Lerp(new Color(205, 245, 255), new Color(255, 188, 160), apoctosisCoreHeatRedInterpolant);
            Texture2D tex2 = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            SpriteEffects flipSprite = Projectile.spriteDirection * Owner.gravDir == -1f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Vector2 shake = Main.rand.NextVector2Circular(2f, 2f) * manaPower;
            float time = apoctosisCoreGlowTime;

            float reverseManaPower = MathHelper.Lerp(0.7f, 0.1f, manaPower > 0f ? 1f - manaPower : manaPercent);
            for (int i = 0; i < 5; i++)
            {
                float iMult = 1f - 0.1f * i;

                if (manaPower > 0f)
                {
                    Main.EntitySpriteDraw(
                        tex2,
                        EnergyCorePosition - Main.screenPosition + shake,
                        null,
                        Color.Lerp(effectsColor, coreWhite, i * 0.1f) with { A = 0 },
                        Main.rand.NextFloat(-5f, 5f),
                        tex2.Size() * 0.5f,
                        new Vector2(1f, 0.35f) * 0.75f * manaPower * Main.rand.NextFloat(0.7f, 1.3f) * iMult,
                        flipSprite
                    );
                }

                for (int b = -1; b <= 1; b += 2)
                {
                    float pulseRate = manaStarved ? 82f : (firing ? 20f : 35f) * (cooling ? 2f : 1f);
                    float sine = MathHelper.Lerp((float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseRate / MathHelper.Pi), reverseManaPower * b, 0.75f);
                    Vector2 scale = new Vector2(0.3f, 1f * sine * b) * (Main.rand.NextFloat(3f, 4.5f) * iMult + manaPower * 1.2f);
                    float starvedRotationBoost = manaStarved ? time * 0.68f : 0f;
                    float rotation = Projectile.rotation
                        + time * manaPower * Math.Max(i - 2, 0) * (manaStarved ? 0.55f : 0.2f)
                        + starvedRotationBoost
                        + MathHelper.PiOver4 * b;

                    Main.EntitySpriteDraw(
                        sparkle,
                        EnergyCorePosition - Main.screenPosition,
                        null,
                        Color.Lerp(effectsColor, coreWhite, i * 0.1f) with { A = 0 },
                        rotation,
                        sparkle.Size() * 0.5f,
                        scale,
                        flipSprite
                    );
                }
            }

            apoctosisCoreGlowTime++;
        }

        private void SpawnNormalShotMuzzleEffect(Player player, Vector2 direction)
        {
            Vector2 muzzlePos = GunTipPosition + direction * 4f;
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);

            int laserCount = Math.Max(1, Math.Min(LaserChainCount, 4));
            float heatInterpolant = MathHelper.Clamp(stage / Math.Max(1f, MaxHeatStage), 0f, 1f);

            Color techBlue = new Color(90, 190, 255);
            Color paleBlue = new Color(180, 235, 255);
            Color hotWhite = Color.Lerp(paleBlue, Color.White, 0.35f + heatInterpolant * 0.45f);

            float baseFanAngle = laserCount == 1
                ? 0f
                : MathHelper.Lerp(0.03f, 0.11f, (laserCount - 2f) / 2f);

            float fanAngle = baseFanAngle * 1.15f + MathHelper.Lerp(0f, 0.02f, heatInterpolant);

            float sideSpacing = MathHelper.Lerp(1.2f, 2.4f, heatInterpolant);

            int glowCount = 6 + laserCount * 2;
            for (int i = 0; i < glowCount; i++)
            {
                float t = glowCount == 1 ? 0.5f : i / (float)(glowCount - 1);
                float angleOffset = MathHelper.Lerp(-fanAngle * 0.8f, fanAngle * 0.8f, t);

                Vector2 glowDir = direction.RotatedBy(angleOffset);
                Vector2 glowSpawnPos =
                    muzzlePos +
                    glowDir * Main.rand.NextFloat(0.8f, 2.8f) +
                    right * Main.rand.NextFloat(-1.8f, 1.8f);

                Vector2 glowVelocity = glowDir * Main.rand.NextFloat(
                    MathHelper.Lerp(2.2f, 3.2f, heatInterpolant),
                    MathHelper.Lerp(4.2f, 6.8f, heatInterpolant));

                GlowOrbParticle glow = new GlowOrbParticle(
                    glowSpawnPos,
                    glowVelocity,
                    false,
                    16 + (int)(heatInterpolant * 8f),
                    Main.rand.NextFloat(
                        MathHelper.Lerp(0.55f, 0.72f, heatInterpolant),
                        MathHelper.Lerp(0.85f, 1.15f, heatInterpolant)),
                    Color.Lerp(techBlue, hotWhite, Main.rand.NextFloat(0.35f, 0.8f)),
                    true,
                    true
                );

                GeneralParticleHandler.SpawnParticle(glow);
                normalShotFXParticles.Add(glow);
            }

            int dustCount = 10 + laserCount * 4;
            for (int i = 0; i < dustCount; i++)
            {
                float t = dustCount == 1 ? 0.5f : i / (float)(dustCount - 1);
                float angleOffset = MathHelper.Lerp(-fanAngle * 1.25f, fanAngle * 1.25f, t);

                Vector2 dustDir = direction.RotatedBy(angleOffset);
                Vector2 dustRight = dustDir.RotatedBy(MathHelper.PiOver2);

                Vector2 dustSpawnPos =
                    muzzlePos +
                    dustDir * Main.rand.NextFloat(0.4f, 2.4f) +
                    dustRight * Main.rand.NextFloat(-1.4f, 1.4f);

                Vector2 dustVelocity =
                    dustDir * Main.rand.NextFloat(
                        MathHelper.Lerp(3.5f, 5.5f, heatInterpolant),
                        MathHelper.Lerp(6.5f, 10.5f, heatInterpolant)) +
                    dustRight * Main.rand.NextFloat(
                        -MathHelper.Lerp(0.6f, 1.4f, heatInterpolant),
                         MathHelper.Lerp(0.6f, 1.4f, heatInterpolant));

                Dust dust = Dust.NewDustPerfect(dustSpawnPos, DustID.RainbowMk2);
                dust.velocity = dustVelocity;
                dust.color = Color.Lerp(techBlue, hotWhite, Main.rand.NextFloat(0.2f, 0.75f));
                dust.scale = Main.rand.NextFloat(
                    MathHelper.Lerp(0.72f, 0.92f, heatInterpolant),
                    MathHelper.Lerp(1.05f, 1.35f, heatInterpolant));
                dust.noGravity = true;
            }

            for (int i = 0; i < laserCount; i++)
            {
                float laneT = laserCount == 1 ? 0.5f : i / (float)(laserCount - 1);
                float laneAngle = laserCount == 1 ? 0f : MathHelper.Lerp(-fanAngle, fanAngle, laneT);

                Vector2 laneDirection = direction.RotatedBy(laneAngle);
                Vector2 laneRight = laneDirection.RotatedBy(MathHelper.PiOver2);

                float centerWeight = laserCount == 1 ? 1f : 1f - Math.Abs(laneT - 0.5f) * 0.28f;
                Vector2 laneOrigin = muzzlePos + laneDirection * Main.rand.NextFloat(0.8f, 2f);

                Particle centerLine = new CustomSpark(
                    laneOrigin,
                    laneDirection * Main.rand.NextFloat(
                        MathHelper.Lerp(10.5f, 12.5f, heatInterpolant),
                        MathHelper.Lerp(14.5f, 18f, heatInterpolant)),
                    "CalamityLegendsComeBack/Texture/Shared/GlowBlade",
                    false,
                    8 + (int)(heatInterpolant * 3f),
                    MathHelper.Lerp(0.05f, 0.075f, heatInterpolant) * centerWeight,
                    Color.Lerp(techBlue, hotWhite, 0.28f + 0.18f * heatInterpolant) * 0.92f,
                    new Vector2(
                        MathHelper.Lerp(0.52f, 0.66f, heatInterpolant),
                        MathHelper.Lerp(1.35f, 1.9f, heatInterpolant)),
                    glowCenter: true,
                    shrinkSpeed: 0.8f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.72f
                );
                GeneralParticleHandler.SpawnParticle(centerLine);
                normalShotFXParticles.Add(centerLine);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 sideSpawnPos = laneOrigin + laneRight * side * sideSpacing;

                    Particle sideLine = new CustomSpark(
                        sideSpawnPos,
                        laneDirection * Main.rand.NextFloat(
                            MathHelper.Lerp(9.2f, 11.5f, heatInterpolant),
                            MathHelper.Lerp(13f, 16.5f, heatInterpolant))
                        + laneRight * side * 0.18f,
                        "CalamityLegendsComeBack/Texture/Shared/GlowBlade",
                        false,
                        7 + (int)(heatInterpolant * 2f),
                        MathHelper.Lerp(0.036f, 0.052f, heatInterpolant) * centerWeight,
                        Color.Lerp(techBlue, paleBlue, 0.42f) * 0.72f,
                        new Vector2(
                            MathHelper.Lerp(0.46f, 0.56f, heatInterpolant),
                            MathHelper.Lerp(1.05f, 1.45f, heatInterpolant)),
                        glowCenter: true,
                        shrinkSpeed: 0.9f,
                        glowCenterScale: 0.88f,
                        glowOpacity: 0.62f
                    );
                    GeneralParticleHandler.SpawnParticle(sideLine);
                    normalShotFXParticles.Add(sideLine);
                }
            }

            Particle coreFlash = new CustomSpark(
                muzzlePos + direction * Main.rand.NextFloat(1.2f, 3.5f),
                direction * Main.rand.NextFloat(
                    MathHelper.Lerp(4.5f, 5.8f, heatInterpolant),
                    MathHelper.Lerp(6.8f, 8.2f, heatInterpolant)),
                "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                false,
                10,
                MathHelper.Lerp(0.11f, 0.16f, heatInterpolant),
                Color.Lerp(techBlue, hotWhite, 0.55f) * 1.15f,
                new Vector2(0.58f, 1.75f),
                glowCenter: true,
                shrinkSpeed: 1.05f,
                glowCenterScale: 0.95f,
                glowOpacity: 0.78f
            );
            GeneralParticleHandler.SpawnParticle(coreFlash);
            normalShotFXParticles.Add(coreFlash);
        }

        #endregion

        #region ===== 特效：火箭齐射 =====

        private void FireCooldownRocketSalvo()
        {
            PlayRocketSalvoSound();

            Player player = Main.player[Projectile.owner];
            NewLegendSHPC weapon = player.HeldItem.ModItem as NewLegendSHPC;

            player.CheckMana(player.HeldItem, 150, true, false);

            if (weapon == null)
                return;

            int effectID = weapon.GetProjectileEffectIDForShot();
            int leftClickDamage = weapon.GetCurrentLeftClickDamage(player, effectID);

            weapon.ConsumeCurrentMagazineShots(1, player);

            Vector2 dir = Vector2.UnitX.RotatedBy(Projectile.rotation);

            float shakePower = 20f;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);

            player.velocity -= dir * 3.2f;

            SpawnNormalShotMuzzleEffect(player, dir);
            SpawnRocketSalvoMuzzleEffect(player, dir);

            var concentrationModule = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.SHPC.Skill.DiffuChip.DiffuChipPlayer>();
            int orbCount = 3;
            float maxAngle = 0.22f * concentrationModule.EmpoweredLeftClickSpreadMultiplier;
            for (int i = 0; i < orbCount; i++)
            {
                float t = orbCount == 1 ? 0.5f : i / (float)(orbCount - 1);
                float angle = MathHelper.Lerp(-maxAngle, maxAngle, t);
                float distFromCenter = Math.Abs(t - 0.5f) * 2f;
                float speedFactor = (float)Math.Pow(1f - distFromCenter, 1.5f);
                float speed = MathHelper.Lerp(10f, 18f, speedFactor);

                int orb = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    dir.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<NewLegendSHPB>(),
                    (int)(leftClickDamage * 0.8f),
                    Projectile.knockBack,
                    Projectile.owner,
                    effectID
                );

                if (Main.projectile.IndexInRange(orb))
                    Main.projectile[orb].netUpdate = true;
            }

            NewLegendSHPC.GainEXFromLeftShot(player, orbCount);
        }

        private void SpawnRocketSalvoMuzzleEffect(Player player, Vector2 baseDirection)
        {
            Vector2 muzzlePos = GetSafeFirePosition(player) + baseDirection * 6f;

            Color techBlue = new Color(90, 190, 255);
            Color paleBlue = new Color(180, 235, 255);

            for (int i = 0; i < 4; i++)
            {
                Color lineColor = Color.Lerp(techBlue, Color.White, Main.rand.NextFloat(0.3f, 0.65f));
                Vector2 lineVelocity = baseDirection.RotatedByRandom(0.16f) * Main.rand.NextFloat(13f, 19f);

                Particle line = new CustomSpark(
                    muzzlePos,
                    lineVelocity,
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    12,
                    Main.rand.NextFloat(0.04f, 0.055f),
                    lineColor,
                    new Vector2(1.25f, 0.8f),
                    shrinkSpeed: 0.72f
                );

                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 dustVelocity = baseDirection.RotatedByRandom(0.24f) * Main.rand.NextFloat(3.5f, 8f);

                Dust dust = Dust.NewDustPerfect(
                    muzzlePos + baseDirection * Main.rand.NextFloat(0f, 3f),
                    DustID.RainbowMk2
                );

                dust.velocity = dustVelocity;
                dust.color = Color.Lerp(techBlue, paleBlue, Main.rand.NextFloat(0.2f, 0.8f));
                dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 smokeVelocity = baseDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.4f, 5.4f);

                Particle smoke = new HeavySmokeParticle(
                    muzzlePos,
                    smokeVelocity,
                    Color.Lerp(Color.White, paleBlue, 0.35f),
                    18,
                    Main.rand.NextFloat(0.38f, 0.58f),
                    0.5f,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    Main.rand.NextBool()
                );

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                float angle = MathHelper.Lerp(-0.22f, 0.22f, t);

                Vector2 laneDirection = baseDirection.RotatedBy(angle);
                Vector2 lanePos = muzzlePos + laneDirection * Main.rand.NextFloat(2f, 5f);

                for (int j = 0; j < 2; j++)
                {
                    Color lineColor = Color.Lerp(techBlue, Color.White, Main.rand.NextFloat(0.25f, 0.55f));
                    Vector2 lineVelocity = laneDirection.RotatedByRandom(0.08f) * Main.rand.NextFloat(11f, 17f);

                    Particle laneLine = new CustomSpark(
                        lanePos,
                        lineVelocity,
                        "CalamityMod/Particles/ThinEndedLine",
                        false,
                        10,
                        Main.rand.NextFloat(0.03f, 0.045f),
                        lineColor,
                        new Vector2(1.05f, 0.72f),
                        shrinkSpeed: 0.75f
                    );

                    GeneralParticleHandler.SpawnParticle(laneLine);
                }

                for (int j = 0; j < 2; j++)
                {
                    Vector2 dustVelocity = laneDirection.RotatedByRandom(0.14f) * Main.rand.NextFloat(2.8f, 6.8f);

                    Dust dust = Dust.NewDustPerfect(
                        lanePos,
                        DustID.RainbowMk2
                    );

                    dust.velocity = dustVelocity;
                    dust.color = Color.Lerp(techBlue, paleBlue, Main.rand.NextFloat(0.2f, 0.75f));
                    dust.scale = Main.rand.NextFloat(0.7f, 0.95f);
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Vector2 smokeVelocity = laneDirection.RotatedByRandom(0.18f) * Main.rand.NextFloat(1.8f, 4.4f);

                    Particle smoke = new HeavySmokeParticle(
                        lanePos,
                        smokeVelocity,
                        Color.Lerp(Color.White, paleBlue, 0.35f),
                        16,
                        Main.rand.NextFloat(0.26f, 0.42f),
                        0.42f,
                        Main.rand.NextFloat(-0.08f, 0.08f),
                        Main.rand.NextBool()
                    );

                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }
        }

        #endregion
    }
}

