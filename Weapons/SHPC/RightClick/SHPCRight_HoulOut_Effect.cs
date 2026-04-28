using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
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
        private int normalFireOutlineTimer;
        private const int NormalFireOutlineDuration = 10;

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
                Volume = zenith ? 1.2f : 1f,
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
                Volume = zenith ? 1.2f : 1f,
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

        #endregion

        #region ===== 特效：阶段与状态 =====

        private void TriggerStageOutlinePulse()
        {
            stageOutlineTimer = StageOutlineDuration;
        }

        private void TriggerNormalFireOutlinePulse()
        {
            normalFireOutlineTimer = NormalFireOutlineDuration;
        }

        private void SpawnStageUpEnergyBurst()
        {
            PlayStageUpSound();

            for (int i = 0; i < 12; i++)
            {
                Vector2 upward = -Vector2.UnitY.RotatedByRandom(0.4f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(6f, 6f),
                    267
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
            bool cooling = fireStopTimer > 0;
            bool firing = !cooling;
            float targetRedInterpolant = stage >= MaxHeatStage ? 1f : 0f;
            apoctosisCoreHeatRedInterpolant = MathHelper.Lerp(apoctosisCoreHeatRedInterpolant, targetRedInterpolant, 0.08f);

            Color techBlue = new(70, 190, 255);
            Color redHeat = new(255, 55, 38);
            Color coolingYellow = new(255, 235, 80);
            Color effectsColor = cooling
                ? coolingYellow
                : Color.Lerp(techBlue, redHeat, apoctosisCoreHeatRedInterpolant);
            Color coreWhite = cooling
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
                    float pulseRate = (firing ? 20f : 35f) * (cooling ? 2f : 1f);
                    float sine = MathHelper.Lerp((float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseRate / MathHelper.Pi), reverseManaPower * b, 0.75f);
                    Vector2 scale = new Vector2(0.3f, 1f * sine * b) * (Main.rand.NextFloat(3f, 4.5f) * iMult + manaPower * 1.2f);
                    float rotation = Projectile.rotation
                        + time * manaPower * Math.Max(i - 2, 0) * 0.2f
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
            float heatInterpolant = MathHelper.Clamp(stage / 7f, 0f, 1f);

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

                Dust dust = Dust.NewDustPerfect(dustSpawnPos, 267);
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
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
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
                        "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
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
            NewLegendSHPC testWeapon = player.HeldItem.ModItem as NewLegendSHPC;

            player.CheckMana(player.HeldItem, 150, true, false);

            if (weapon == null && testWeapon == null)
                return;

            if (weapon != null && weapon.storedEffectPower > 0)
            {
                weapon.storedEffectPower -= 5;

                if (weapon.storedEffectPower < 0)
                    weapon.storedEffectPower = 0;
            }

            if (testWeapon != null && testWeapon.storedEffectPower > 0)
            {
                testWeapon.storedEffectPower -= 5;

                if (testWeapon.storedEffectPower < 0)
                    testWeapon.storedEffectPower = 0;
            }

            int effectID = currentEffectID;
            Vector2 dir = Vector2.UnitX.RotatedBy(Projectile.rotation);

            float shakePower = 20f;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);

            player.velocity -= dir * 3.2f;

            SpawnNormalShotMuzzleEffect(player, dir);
            SpawnRocketSalvoMuzzleEffect(player, dir);

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float angle = MathHelper.Lerp(-0.3f, 0.3f, t);
                float distFromCenter = Math.Abs(t - 0.5f) * 2f;
                float speedFactor = (float)Math.Pow(1f - distFromCenter, 1.5f);
                float speed = MathHelper.Lerp(10f, 18f, speedFactor);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    dir.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<NewLegendSHPB>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    effectID
                );
            }
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
                    "CalamityMod/Particles/BloomLineSoftEdge",
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
                    267
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

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float angle = MathHelper.Lerp(-0.3f, 0.3f, t);

                Vector2 laneDirection = baseDirection.RotatedBy(angle);
                Vector2 lanePos = muzzlePos + laneDirection * Main.rand.NextFloat(2f, 5f);

                for (int j = 0; j < 2; j++)
                {
                    Color lineColor = Color.Lerp(techBlue, Color.White, Main.rand.NextFloat(0.25f, 0.55f));
                    Vector2 lineVelocity = laneDirection.RotatedByRandom(0.08f) * Main.rand.NextFloat(11f, 17f);

                    Particle laneLine = new CustomSpark(
                        lanePos,
                        lineVelocity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
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
                        267
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
