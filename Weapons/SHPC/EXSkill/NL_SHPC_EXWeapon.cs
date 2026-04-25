using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class NL_SHPC_EXWeapon : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        #region Defaults And Simple Members

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 GunTip => Projectile.Center + Projectile.velocity * 56f;
        private bool IsDischarging => state == 2;
        private bool IsAimLocked => state >= 2;
        private float HoldoutRecoilOffset => MathHelper.Clamp(24f - timer * 1.2f, 0f, 47f);

        private int state;
        // 0 = 蓄力
        // 1 = 蓄满待机
        // 2 = 激光
        // 3 = 过热
        private int timer;

        private const int ChargeTime = 180;
        private const int LaserTime = 60;
        private const int OverheatTime = 30;

        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";

        public override void SetDefaults()
        {
            Projectile.width = 112;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => false;

        #endregion

        #region AI

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            UpdateProjectileHeldVariables(armPosition);
            ManipulatePlayerVariables();

            switch (state)
            {
                case 0:
                    ChargePhase();
                    break;
                case 1:
                    ReadyPhase();
                    break;
                case 2:
                    LaserPhase();
                    break;
                case 3:
                    OverheatPhase();
                    break;
            }
        }

        #endregion

        #region Phase 1 Charge

        // 播音
        private SlotId ChargeSoundSlot;
        private int chargeSoundTimer;
        private void ChargePhase()
        {


            // ================= 蓄力循环音 =================
            timer++; // ← 提前！！！

            float chargeFactor = Utils.GetLerpValue(0f, ChargeTime, timer, true);

            // 只在第一次创建
            if (ChargeSoundSlot == default)
            {
                ChargeSoundSlot = SoundEngine.PlaySound(
                    new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/蜃景大招正在蓄力")
                    {
                        Volume = 1.0f,
                        IsLooped = true
                    },
                    Projectile.Center
                );
            }

            // 每2帧更新一次（关键）
            if (timer % 2 == 0 && SoundEngine.TryGetActiveSound(ChargeSoundSlot, out var sound))
            {
                sound.Position = Projectile.Center;

                // 音量：1 → 5
                sound.Volume = 1.0f + chargeFactor * 4.0f;

                // 音调：稍微收一点，不要太抖
                sound.Pitch = -0.2f + chargeFactor * 0.6f;
            }


            SpawnBurstEffect(timer / (float)ChargeTime);

            if (timer < ChargeTime)
                return;

            state = 1;
            timer = 0;

            // 停止蓄力循环音
            if (SoundEngine.TryGetActiveSound(ChargeSoundSlot, out var s))
                s?.Stop();

            // 播放蓄力完成音
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/蜃景大招准备就绪")
                {
                    Volume = 1.2f,
                    Pitch = 0f
                },
                Projectile.Center
            );
        }

        #endregion

        #region Phase 2 Ready

        private void ReadyPhase()
        {
            SpawnReadyEffects();

            if (!Main.mouseLeft || !Main.mouseLeftRelease)
                return;

            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.netUpdate = true;
            state = 2;
            timer = 0;

            float shakePower = 20f;
            float distanceInterpolant = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceInterpolant);
        }

        #endregion

        #region Phase 3 Laser

        private SlotId LaserSoundSlot;
        private int laserSoundTimer;
        private const int SpiralInvBurstCount = 8;
        private const float SpiralInvMinSpeed = 72f;
        private const float SpiralInvMaxSpeed = 118f;

        private void LaserPhase()
        {
            timer++;

            // ================= 激光循环音效（武器级） =================
            laserSoundTimer++;

            float lifeFactor = Utils.GetLerpValue(0f, LaserTime, laserSoundTimer, true);

            // 初始化（只播一次）
            if (LaserSoundSlot == default)
            {
                LaserSoundSlot = SoundEngine.PlaySound(
                    new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/蜃景大招发射")
                    {
                        Volume = 9.0f,
                        IsLooped = true
                    },
                    Projectile.Center
                );
            }

            // 每2帧更新（防抖）
            if (laserSoundTimer % 2 == 0 && SoundEngine.TryGetActiveSound(LaserSoundSlot, out var sound))
            {
                sound.Position = Projectile.Center;

                // 音量：1 → 4
                sound.Volume = 1.0f + lifeFactor * 3.0f;

                // 音调
                sound.Pitch = -0.4f * lifeFactor;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                int laser = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip,
                    Projectile.velocity,
                    ModContent.ProjectileType<SHPC_SuperLazer>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);

                Main.projectile[laser].ai[0] = Projectile.whoAmI;

                SpawnSpiralInvs();
            }

            SpawnLaserMuzzleStorm(lifeFactor);

            Owner.velocity -= Projectile.velocity * 0.15f;

            if (timer < LaserTime)
                return;

            // 停止激光音
            if (SoundEngine.TryGetActiveSound(LaserSoundSlot, out var s))
                s?.Stop();

            LaserSoundSlot = default;
            laserSoundTimer = 0;

            state = 3;
            timer = 0;
        }

        private void SpawnSpiralInvs()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < SpiralInvBurstCount; i++)
            {
                float spawnOffset = Main.rand.NextFloat(0f, 26f);
                float speed = Main.rand.NextFloat(SpiralInvMinSpeed, SpiralInvMaxSpeed);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip,
                    forward * speed,
                    ModContent.ProjectileType<SHPC_SLINV>(),
                    Projectile.damage,
                    0f,
                    Projectile.owner,
                    Projectile.whoAmI,
                    spawnOffset);
            }
        }

        #endregion

        #region Phase 4 Overheat

        private void OverheatPhase()
        {
            timer++;

            if (!Main.dedServ)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
                Vector2 stockCenter = Projectile.Center - forward * 28f;
                float spinPhase = timer * 0.24f;

                for (int i = 0; i < 2; i++)
                {
                    float localAngle = spinPhase + i * MathHelper.Pi + Projectile.identity * 0.13f;
                    float spiralRadius = 4f + 2.5f * (0.5f + 0.5f * (float)Math.Sin(localAngle * 1.7f));
                    Vector2 swirlOffset =
                        right * ((float)Math.Cos(localAngle) * spiralRadius) +
                        forward * ((float)Math.Sin(localAngle) * 2.5f);

                    Vector2 smokePosition = stockCenter + swirlOffset + Main.rand.NextVector2Circular(1.5f, 1.5f);
                    Vector2 smokeVelocity =
                        -Vector2.UnitY * Main.rand.NextFloat(1.6f, 3.6f) +
                        right * ((float)Math.Sin(localAngle * 1.35f) * Main.rand.NextFloat(0.55f, 1.25f)) +
                        forward * Main.rand.NextFloat(-0.18f, 0.18f);

                    Particle smoke = new MediumMistParticle(
                        smokePosition,
                        smokeVelocity,
                        Color.White,
                        Color.Transparent,
                        Main.rand.NextFloat(0.62f, 0.94f),
                        Main.rand.NextFloat(90f, 132f));

                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

            if (timer >= OverheatTime)
                Projectile.Kill();
        }

        #endregion

        #region Held Projectile Logic

        private void UpdateProjectileHeldVariables(Vector2 armPosition)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 oldVelocity = Projectile.velocity;

                if (!IsAimLocked)
                {
                    Vector2 mouseWorld = Owner.Calamity().mouseWorld;
                    Vector2 targetDirection = Projectile.SafeDirectionTo(mouseWorld);
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity,
                        targetDirection,
                        0.45f).SafeNormalize(Vector2.UnitX);
                }

                if (Projectile.velocity != oldVelocity)
                    Projectile.netUpdate = true;
            }

            Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;
            float recoil = IsAimLocked ? 0f : HoldoutRecoilOffset;

            Projectile.Center = armPosition + Projectile.velocity * recoil + new Vector2(0f, 5f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.spriteDirection = Projectile.direction;

            if (Owner.CantUseHoldout() || IsDischarging)
                Projectile.Center += Main.rand.NextVector2Circular(4.5f, 4.5f);
        }

        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }

        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            bool facingLeft = Projectile.velocity.X < 0f;

            SpriteEffects effects = facingLeft ? SpriteEffects.FlipVertically : SpriteEffects.None;

            float rotation = Projectile.rotation;


            // ===== 白色叠加（蓄力阶段）=====
            if (state == 0)
            {
                float chargeFactor = Utils.GetLerpValue(0f, ChargeTime, timer, true);

                // 强度：越蓄越猛
                float intensity = chargeFactor;

                // 偏移范围：越蓄越大
                float offset = 4f + chargeFactor * 10f;

                int drawCount = 1 + (int)(chargeFactor * 3f); // 最多叠4层

                for (int i = 0; i < drawCount; i++)
                {
                    Vector2 randOffset = Main.rand.NextVector2Circular(offset, offset) * intensity;

                    Main.EntitySpriteDraw(
                        texture,
                        drawPos + randOffset,
                        null,
                        Color.White * intensity * 0.7f,
                        rotation,
                        origin,
                        Projectile.scale,
                        effects,
                        0
                    );
                }
            }

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                Projectile.GetAlpha(lightColor),
                rotation,
                origin,
                Projectile.scale,
                effects,
                0);

            return false;
        }

        #region Visual Effects

        private void SpawnLaserMuzzleStorm(float lifeFactor)
        {
            if (Main.dedServ)
                return;

            Vector2 center = GunTip;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float fanHalfAngle = MathHelper.ToRadians(60f);
            float muzzleRadius = 32f;

            Color lineColorA = Color.Lerp(new Color(90, 200, 255), Color.White, 0.32f + lifeFactor * 0.18f);
            Color lineColorB = Color.Lerp(new Color(120, 235, 255), Color.White, 0.44f + lifeFactor * 0.22f);
            Color pointColor = Color.Lerp(new Color(135, 228, 255), Color.White, 0.2f + lifeFactor * 0.18f);

            int lineCount = 16;
            int glowCount = 10;
            int pointCount = 8;

            for (int i = 0; i < lineCount; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float angle = fanHalfAngle * fanT;
                float radial = muzzleRadius * (float)Math.Sqrt(Main.rand.NextFloat());
                Vector2 lanePos =
                    center +
                    forward * Main.rand.NextFloat(2f, muzzleRadius * 0.65f) +
                    right * fanT * radial +
                    Main.rand.NextVector2Circular(1.5f, 1.5f);

                Vector2 lineVelocity =
                    forward.RotatedBy(angle) * Main.rand.NextFloat(14f, 27f) +
                    right * fanT * Main.rand.NextFloat(0.8f, 4.4f);

                Color lineColor = Main.rand.NextBool() ? lineColorA : lineColorB;
                Particle line = new CustomSpark(
                    lanePos,
                    lineVelocity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.032f, 0.048f),
                    lineColor,
                    new Vector2(Main.rand.NextFloat(1.1f, 1.55f), Main.rand.NextFloat(0.62f, 0.86f)),
                    shrinkSpeed: 0.75f
                );

                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < glowCount; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float angle = fanHalfAngle * fanT * Main.rand.NextFloat(0.75f, 1f);
                float radial = muzzleRadius * Main.rand.NextFloat(0.18f, 0.95f);
                Vector2 glowPos =
                    center +
                    forward * Main.rand.NextFloat(0f, muzzleRadius * 0.45f) +
                    right * fanT * radial;

                Vector2 glowVelocity =
                    forward.RotatedBy(angle) * Main.rand.NextFloat(10f, 21f) +
                    right * fanT * Main.rand.NextFloat(0.3f, 2.6f);

                GlowSparkParticle spark = new GlowSparkParticle(
                    glowPos,
                    glowVelocity,
                    false,
                    Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.08f, 0.14f),
                    Main.rand.NextBool() ? Color.White : lineColorB,
                    new Vector2(Main.rand.NextFloat(2.4f, 3.6f), Main.rand.NextFloat(0.46f, 0.72f)),
                    true);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < pointCount; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float angle = fanHalfAngle * fanT * Main.rand.NextFloat(0.65f, 1f);
                Vector2 pointPos =
                    center +
                    Main.rand.NextVector2Circular(muzzleRadius * 0.45f, muzzleRadius * 0.45f) +
                    forward * Main.rand.NextFloat(2f, 12f);

                Vector2 pointVelocity =
                    forward.RotatedBy(angle) * Main.rand.NextFloat(12f, 24f) +
                    right * fanT * Main.rand.NextFloat(0.5f, 3.5f);

                PointParticle point = new PointParticle(
                    pointPos,
                    pointVelocity,
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.85f, 1.25f),
                    pointColor);
                GeneralParticleHandler.SpawnParticle(point);
            }
        }

        private void SpawnBurstEffect(float progress)
        {
            Vector2 center = GunTip;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (Main.myPlayer == Projectile.owner)
            {
                int newShpsType = ModContent.ProjectileType<NewSHPS>();
                int activeSoulCount = 0;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (!other.active || other.owner != Projectile.owner || other.type != newShpsType)
                        continue;

                    if ((int)other.ai[0] == 4 && (int)other.ai[1] == Projectile.whoAmI)
                        activeSoulCount++;
                }

                float spawnCycle = 0.5f + 0.5f * (float)Math.Sin(timer * 0.28f + progress * 7f);
                int maxSoulCount = 10 + (int)MathHelper.Lerp(6f, 20f, progress);
                int spawnAttempts = 1 + (progress > 0.55f ? 1 : 0) + (progress > 0.83f ? 1 : 0);

                for (int i = 0; i < spawnAttempts && activeSoulCount < maxSoulCount; i++)
                {
                    float spawnChance = 0.24f + progress * 0.5f + spawnCycle * 0.15f - i * 0.12f;
                    if (Main.rand.NextFloat() > spawnChance)
                        continue;

                    float angle = timer * 0.19f + i * MathHelper.TwoPi / Math.Max(1, spawnAttempts) + Projectile.identity * 0.37f;
                    float radialOffset = 60f + 55f * (0.5f + 0.5f * (float)Math.Sin(angle * 1.37f + progress * 5.8f));
                    float forwardOffset = 28f + 42f * (0.5f + 0.5f * (float)Math.Cos(angle * 0.91f - progress * 3.4f));

                    Vector2 spawnPosition =
                        Owner.Center +
                        right * ((float)Math.Sin(angle) * radialOffset) +
                        forward * ((float)Math.Cos(angle * 1.21f + 0.6f) * forwardOffset - 18f) +
                        Main.rand.NextVector2Circular(10f, 10f);

                    Vector2 toGunTip = center - spawnPosition;
                    Vector2 inward = toGunTip.SafeNormalize(forward);
                    Vector2 tangent = inward.RotatedBy(MathHelper.PiOver2) * (Main.rand.NextBool() ? 1f : -1f);
                    Vector2 soulVelocity =
                        inward * MathHelper.Lerp(4.5f, 11.5f, progress) +
                        tangent * MathHelper.Lerp(1.8f, 4.5f, progress) * Main.rand.NextFloat(0.75f, 1.15f) +
                        forward * Main.rand.NextFloat(-1.2f, 1.2f);

                    int soul = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        soulVelocity,
                        newShpsType,
                        0,
                        0f,
                        Projectile.owner,
                        4f,
                        Projectile.whoAmI);

                    if (Main.projectile.IndexInRange(soul))
                    {
                        Main.projectile[soul].timeLeft = Main.rand.Next(80, 108);
                        Main.projectile[soul].netUpdate = true;
                    }

                    activeSoulCount++;
                }
            }

            if (Main.dedServ)
                return;

            float pulseA = 0.5f + 0.5f * (float)Math.Sin(timer * 0.18f + progress * 4.4f);
            float pulseB = 0.5f + 0.5f * (float)Math.Cos(timer * 0.41f - progress * 8.2f);
            float pulseC = 0.5f + 0.5f * (float)Math.Sin(timer * 0.09f + pulseB * MathHelper.TwoPi);
            float energy = MathHelper.Clamp(progress * progress * (1.1f + pulseA * 0.45f + pulseB * 0.3f), 0f, 2.2f);

            Color outerColor = Color.Lerp(new Color(16, 92, 255), new Color(55, 210, 255), 0.35f + 0.65f * pulseA);
            Color ringColor = Color.Lerp(new Color(50, 170, 255), new Color(145, 255, 255), 0.3f + 0.7f * pulseB);
            Color coreColor = Color.Lerp(new Color(140, 245, 255), Color.White, 0.4f + 0.45f * pulseC);

            int lightLanes = 3 + (progress > 0.62f ? 1 : 0);
            for (int lane = 0; lane < lightLanes; lane++)
            {
                float laneAngle = timer * (0.14f + lane * 0.026f) + lane * MathHelper.TwoPi / lightLanes + Projectile.identity * 0.19f;
                float funnelRadius = MathHelper.Lerp(24f, 110f, 0.25f + 0.75f * (0.5f + 0.5f * (float)Math.Sin(laneAngle * 0.93f + pulseA * 2.1f)));

                Vector2 spawnPosition =
                    center +
                    right * ((float)Math.Sin(laneAngle * 1.3f) * funnelRadius) +
                    forward * (-(38f + funnelRadius * 0.58f) + (float)Math.Cos(laneAngle * 0.6f) * 16f);

                Vector2 toCenter = center - spawnPosition;
                Vector2 inward = toCenter.SafeNormalize(forward);
                Vector2 curl = inward.RotatedBy(MathHelper.PiOver2 * (lane % 2 == 0 ? 1f : -1f));
                Vector2 velocity =
                    inward * MathHelper.Lerp(2.6f, 7.4f, progress) +
                    curl * (6f + funnelRadius * 0.035f) * (0.55f + 0.45f * pulseB) +
                    forward * (1.5f + 2.5f * pulseC);

                SquishyLightParticle particle = new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    Main.rand.NextFloat(0.42f, 0.72f) * energy,
                    Color.Lerp(outerColor, coreColor, Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.Next(20, 34));

                GeneralParticleHandler.SpawnParticle(particle);
            }

            int sparkCount = 2 + (int)(progress * 3f + pulseA * 2f);
            for (int i = 0; i < sparkCount; i++)
            {
                float offsetAngle = timer * 0.22f + i * MathHelper.TwoPi / sparkCount + pulseB * 0.7f;
                float spawnRadius = MathHelper.Lerp(20f, 72f, 0.3f + 0.7f * (0.5f + 0.5f * (float)Math.Cos(offsetAngle * 1.17f - progress * 6.1f)));

                Vector2 spawnPosition =
                    center +
                    right * ((float)Math.Cos(offsetAngle) * spawnRadius) +
                    forward * (-(18f + spawnRadius * 0.42f)) +
                    Main.rand.NextVector2Circular(3f, 3f);

                Vector2 toCenter = center - spawnPosition;
                Vector2 inward = toCenter.SafeNormalize(forward);
                Vector2 sparkVelocity =
                    inward * Main.rand.NextFloat(6.5f, 12f) * (0.65f + 0.55f * progress) +
                    inward.RotatedBy(MathHelper.PiOver2 * (i % 2 == 0 ? 1f : -1f)) * Main.rand.NextFloat(0.8f, 3.6f) * (0.45f + pulseA * 0.7f);

                GlowSparkParticle spark = new GlowSparkParticle(
                    spawnPosition,
                    sparkVelocity,
                    false,
                    Main.rand.Next(10, 14),
                    Main.rand.NextFloat(0.022f, 0.038f),
                    Color.Lerp(ringColor, Color.White, Main.rand.NextFloat(0.22f, 0.6f)),
                    new Vector2(Main.rand.NextFloat(1.8f, 2.7f), Main.rand.NextFloat(0.28f, 0.52f)),
                    true,
                    false,
                    1.1f);

                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (timer % 2 == 0)
            {
                int markCount = 1 + (progress > 0.5f ? 1 : 0);
                for (int i = 0; i < markCount; i++)
                {
                    float markAngle = -timer * 0.16f + i * MathHelper.Pi + pulseC * 1.7f;
                    float markRadius = MathHelper.Lerp(12f, 46f, 0.5f + 0.5f * (float)Math.Sin(markAngle * 1.4f + progress * 8f));

                    Vector2 markPosition =
                        center +
                        right * ((float)Math.Cos(markAngle) * markRadius) +
                        forward * ((float)Math.Sin(markAngle * 1.2f) * 10f - 6f);

                    Vector2 markVelocity =
                        (center - markPosition).SafeNormalize(forward) * Main.rand.NextFloat(0.9f, 2.4f) +
                        right * (float)Math.Sin(markAngle * 2f) * 0.8f;

                    Particle ring = new CustomSpark(
                        markPosition,
                        markVelocity,
                        "CalamityMod/Particles/ProvidenceMarkParticle",
                        false,
                        Main.rand.Next(20, 30),
                        Main.rand.NextFloat(0.75f, 1.12f) * (0.85f + progress * 0.4f),
                        Color.Lerp(new Color(80, 200, 255), coreColor, Main.rand.NextFloat(0.35f, 0.8f)),
                        new Vector2(Main.rand.NextFloat(1.1f, 1.5f), Main.rand.NextFloat(0.32f, 0.58f)),
                        true,
                        false,
                        Main.rand.NextFloat(-0.12f, 0.12f),
                        false,
                        false,
                        Main.rand.NextFloat(0.08f, 0.16f));

                    GeneralParticleHandler.SpawnParticle(ring);
                }
            }

            if (Main.rand.NextFloat() < 0.85f)
            {
                SquishyLightParticle corePulse = new SquishyLightParticle(
                    center + Main.rand.NextVector2Circular(4f + progress * 6f, 4f + progress * 6f),
                    Main.rand.NextVector2Circular(0.7f, 0.7f) - forward * Main.rand.NextFloat(0.2f, 1.3f),
                    Main.rand.NextFloat(0.35f, 0.6f) * (1f + progress),
                    Color.Lerp(coreColor, Color.White, 0.35f + 0.35f * pulseA),
                    Main.rand.Next(16, 24));

                GeneralParticleHandler.SpawnParticle(corePulse);
            }
        }

        private void SpawnReadyEffects()
        {
            Vector2 center = GunTip;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (Main.dedServ)
                return;

            float pulseA = 0.5f + 0.5f * (float)Math.Sin(timer * 0.46f);
            float pulseB = 0.5f + 0.5f * (float)Math.Cos(timer * 0.23f + 1.4f);
            float pulseC = 0.5f + 0.5f * (float)Math.Sin(timer * 0.71f + pulseB * 2.4f);

            Color coldBlue = Color.Lerp(new Color(65, 170, 255), new Color(110, 245, 255), pulseA);
            Color brightBlue = Color.Lerp(new Color(150, 240, 255), Color.White, 0.45f + 0.4f * pulseC);

            int glowPointCount = 2 + (timer % 3 == 0 ? 1 : 0);
            for (int i = 0; i < glowPointCount; i++)
            {
                float laneAngle = timer * 0.34f + i * MathHelper.TwoPi / glowPointCount + Projectile.identity * 0.11f;
                float laneRadius = 4f + 7f * (0.5f + 0.5f * (float)Math.Sin(laneAngle * 1.6f + pulseB * 1.9f));

                Vector2 spawnPosition =
                    center +
                    right * ((float)Math.Cos(laneAngle) * laneRadius) +
                    forward * ((float)Math.Sin(laneAngle * 1.2f) * 3.2f - 1.5f);

                Vector2 inward = (center - spawnPosition).SafeNormalize(forward);
                Vector2 velocity =
                    inward * Main.rand.NextFloat(0.9f, 2.4f) +
                    inward.RotatedBy(MathHelper.PiOver2 * (i % 2 == 0 ? 1f : -1f)) * Main.rand.NextFloat(0.2f, 0.9f);

                SquishyLightParticle particle = new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    Main.rand.NextFloat(0.18f, 0.3f),
                    Color.Lerp(coldBlue, brightBlue, Main.rand.NextFloat(0.3f, 0.75f)),
                    Main.rand.Next(8, 14));

                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(2.2f, 2.2f) - forward * Main.rand.NextFloat(0f, 4f),
                    forward * Main.rand.NextFloat(0.15f, 0.85f) + Main.rand.NextVector2Circular(0.2f, 0.2f),
                    false,
                    Main.rand.Next(5, 8),
                    Main.rand.NextFloat(0.28f, 0.44f),
                    Color.Lerp(coldBlue, Color.White, 0.35f + 0.4f * pulseB),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (timer % 3 == 0)
            {
                Vector2 frontPulsePosition = center + forward * (5f + 3f * pulseA);
                Vector2 backPulsePosition = center - forward * (8f + 4f * pulseB);

                Particle frontPulse = new DirectionalPulseRing(
                    frontPulsePosition,
                    forward * (0.5f + 0.4f * pulseA),
                    Color.Lerp(coldBlue, brightBlue, 0.5f + 0.35f * pulseC),
                    new Vector2(0.45f + 0.08f * pulseB, 1.15f + 0.28f * pulseA),
                    Projectile.rotation,
                    0.51f + pulseB * 0.02f,
                    0.035f,
                    12);

                Particle backPulse = new DirectionalPulseRing(
                    backPulsePosition,
                    -forward * (0.28f + 0.22f * pulseC),
                    Color.Lerp(coldBlue, new Color(90, 210, 255), 0.45f + 0.35f * pulseA) * 0.9f,
                    new Vector2(0.4f + 0.06f * pulseC, 0.95f + 0.24f * pulseB),
                    Projectile.rotation,
                    0.19f + pulseA * 0.018f,
                    0.013f,
                    10);

                GeneralParticleHandler.SpawnParticle(frontPulse);
                GeneralParticleHandler.SpawnParticle(backPulse);
            }

            int dustLines = 3;
            for (int line = 0; line < dustLines; line++)
            {
                float spinAngle = timer * 0.43f + line * MathHelper.TwoPi / dustLines + Projectile.identity * 0.17f;
                float radius = 7f + 3.5f * (float)Math.Sin(spinAngle * 1.7f + pulseA);
                Vector2 lineNormal = new Vector2(radius, 0f).RotatedBy(spinAngle);
                Vector2 lineTangent = lineNormal.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                Vector2 anchor = center + lineNormal * 0.85f + forward * ((float)Math.Cos(spinAngle * 1.3f) * 2.2f);

                for (int segment = 0; segment < 4; segment++)
                {
                    float segmentOffset = segment * 2.9f;
                    Vector2 dustPosition = anchor - lineTangent * segmentOffset;
                    Dust dust = Dust.NewDustPerfect(dustPosition, DustID.IceTorch);
                    dust.velocity =
                        lineTangent * (2.8f + segment * 0.45f) +
                        forward * ((segment - 1.5f) * 0.18f) -
                        lineNormal.SafeNormalize(Vector2.Zero) * 0.35f;
                    dust.noGravity = true;
                    dust.scale = 0.82f - segment * 0.11f + pulseC * 0.08f;
                    dust.color = Color.Lerp(new Color(120, 210, 255), Color.White, 0.3f + 0.15f * segment + 0.2f * pulseB);
                }
            }
        }

        #endregion
    }
}
