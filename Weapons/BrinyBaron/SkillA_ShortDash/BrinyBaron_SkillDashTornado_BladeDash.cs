using System;
using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    public class BrinyBaron_SkillDashTornado_BladeDash : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        private const int PrepareTime = 8;
        private const int DashTimeMax = 45;
        private const int ReboundTimeMax = 12;
        private const int DashHistoryLength = 8;
        private const float DashSpeed = 18f * 0.67f;
        private const float ReboundSpeed = 9f;
        private const float DefaultReboundDashSpeedMultiplier = 0.6f;
        private const float DashTurnRate = 0.01f; // 转向最大角度限
        private const float ReadyBladeDistance = 28f;
        private const float DashBladeDistance = 20f;
        private const float ReboundBladeDistance = 18f;

        private int dashState;
        private int stateTimer;
        private Vector2 lockedDirection = Vector2.UnitX;
        private float bladeRotation;
        private bool initialized;
        private bool hasBounced;
        private bool canceledCharge;
        private bool tileContactTideGranted;
        private float oceanPhase;
        private float dashSpeedMultiplier;
        private float contactDamageMultiplier;
        private bool enemyReboundUnlocked;
        private int dashShotTimer;
        private readonly System.Collections.Generic.List<Vector2> dashDirectionHistory = new();
        private static readonly float[] ShortDashSpeedMultipliers = { 2.05f, 2.28f, 2.5f, 2.72f, 3.25f };
        private static readonly float[] ShortDashContactDamageMultipliers = { 2.5f, 3.25f, 4f, 4.75f, 5.5f };
        private static readonly bool[] ShortDashEnemyReboundUnlocks = { false, true, true, true, true };
        private bool ReboundDashMode => Projectile.ai[0] == 2f;
        private float DashSpeedMultiplier => ReboundDashMode ? DefaultReboundDashSpeedMultiplier : 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PrepareTime + DashTimeMax + ReboundTimeMax + 40;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(IEntitySource source)
        {
            InitializeDash(Main.player[Projectile.owner]);
        }
        //speed
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
                InitializeDash(owner);

            owner.Calamity().mouseWorldListener = true;
            owner.Calamity().rightClickListener = true;

            MaintainOwnerState(owner);
            Projectile.rotation = bladeRotation;
            Lighting.AddLight(Projectile.Center, 0.04f, 0.2f, 0.28f);
            oceanPhase += 0.24f;

            switch (dashState)
            {
                case 0:
                    DoPreparePhase(owner);
                    break;
                case 1:
                    DoDashPhase(owner);
                    break;
                case 2:
                    DoReboundPhase(owner);
                    break;
            }
        }

        private void InitializeDash(Player owner)
        {
            lockedDirection = GetAimDirection(owner, Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction));

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter + lockedDirection * 18f;
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            dashState = 0;
            stateTimer = 0;
            hasBounced = false;
            canceledCharge = false;
            tileContactTideGranted = false;
            oceanPhase = 0f;
            dashShotTimer = 0;
            ShortDashProfile growthProfile = ResolveDashGrowthProfile();
            dashSpeedMultiplier = growthProfile.SpeedMultiplier;
            contactDamageMultiplier = growthProfile.ContactDamageMultiplier;
            enemyReboundUnlocked = growthProfile.EnemyReboundUnlocked;
            Projectile.damage = Math.Max(1, (int)Math.Round(Projectile.damage * contactDamageMultiplier));
            dashDirectionHistory.Clear();
            initialized = true;

            SoundEngine.PlaySound(SoundID.Item73 with
            {
                Volume = 0.65f,
                Pitch = -0.05f
            }, Projectile.Center);

            SpawnStartBurst();
            SpawnChargeReadyBurst();
        }

        private void DoPreparePhase(Player owner)
        {
            stateTimer++;
            Projectile.velocity = Vector2.Zero;

            Vector2 aimDirection = (owner.Calamity().mouseWorld - owner.MountedCenter).SafeNormalize(lockedDirection);
            lockedDirection = Vector2.Lerp(lockedDirection, aimDirection, 0.18f).SafeNormalize(aimDirection);

            float chargeProgress = Utils.GetLerpValue(0f, PrepareTime, stateTimer, true);
            float eased = MathHelper.SmoothStep(0f, 1f, chargeProgress);
            Projectile.Center = owner.MountedCenter + lockedDirection * MathHelper.Lerp(-16f, ReadyBladeDistance, eased);
            Projectile.scale = MathHelper.Lerp(0.88f, 1.04f, eased);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            if (stateTimer % 2 == 0)
                SpawnPrepareTrail();

            if (stateTimer >= PrepareTime)
            {
                SpawnChargeReadyBurst();
                StartDash(owner);
            }
        }

        private void StartDash(Player owner)
        {
            dashState = 1;
            stateTimer = 0;
            hasBounced = false;

            Projectile.friendly = true;
            Projectile.Center = owner.MountedCenter + lockedDirection * DashBladeDistance;
            Projectile.velocity = lockedDirection * (DashSpeed * dashSpeedMultiplier * DashSpeedMultiplier);
            SyncOwnerToProjectile(owner, DashBladeDistance);
            RecordDashDirection(Projectile.velocity.SafeNormalize(lockedDirection));
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item39 with
            {
                Volume = 0.85f,
                Pitch = -0.2f
            }, Projectile.Center);

            BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects(Projectile, lockedDirection);
        }

        private void DoDashPhase(Player owner)
        {
            stateTimer++;
            Vector2 aimDirection = GetAimDirection(owner, lockedDirection);
            float turnedRotation = lockedDirection.ToRotation().AngleTowards(aimDirection.ToRotation(), DashTurnRate);
            lockedDirection = turnedRotation.ToRotationVector2();

            Vector2 desiredVelocity = lockedDirection * (DashSpeed * dashSpeedMultiplier * DashSpeedMultiplier);
            Vector2 actualVelocity = ResolveSlidingVelocity(owner, desiredVelocity);

            Projectile.velocity = actualVelocity;
            if (actualVelocity.LengthSquared() <= 0.01f)
            {
                Projectile.Kill();
                return;
            }

            SyncOwnerToProjectile(owner, DashBladeDistance);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            RecordDashDirection(actualVelocity.SafeNormalize(lockedDirection));
            BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects(Projectile, lockedDirection, bladeRotation, oceanPhase, stateTimer);
            TryFireDashProjectile(owner, actualVelocity.SafeNormalize(lockedDirection));

            if (stateTimer >= DashTimeMax)
                Projectile.Kill();
        }

        private void DoReboundPhase(Player owner)
        {
            stateTimer++;

            float speedFactor = MathHelper.Lerp(1f, 0.55f, stateTimer / (float)ReboundTimeMax);
            Projectile.velocity = lockedDirection * ReboundSpeed * DashSpeedMultiplier * speedFactor;
            SyncOwnerToProjectile(owner, ReboundBladeDistance);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            BrinyBaron_SkillDashTornado_FlightEffects.SpawnReboundFlightEffects(Projectile, lockedDirection, bladeRotation, oceanPhase, stateTimer);

            if (stateTimer >= ReboundTimeMax)
                Projectile.Kill();
        }

        private void MaintainOwnerState(Player owner)
        {
            owner.ChangeDir(lockedDirection.X >= 0f ? 1 : -1);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;

            float armRotation = lockedDirection.ToRotation() - MathHelper.PiOver2;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        private void SyncOwnerToProjectile(Player owner, float bladeDistance)
        {
            owner.velocity = Projectile.velocity;
            Projectile.Center = owner.MountedCenter + lockedDirection * bladeDistance;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (dashState != 1 || hasBounced)
                return false;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (dashState != 1 || hasBounced)
                return;

            target.AddBuff(BuffID.Frostburn, 180);
            SpawnWaterPillarBurst(target.Center, GetReliableDashDirection());

            Player owner = Main.player[Projectile.owner];
            if (Main.myPlayer == Projectile.owner)
                owner.GetModPlayer<BBTideValuePlayer>().TryAddTideFromBlade();

            if (ReboundDashMode)
            {
                SpawnCeruleanShieldExplosion(target.Center, GetReliableDashDirection());
                StartRebound(owner, target.Center);
                if (Main.myPlayer == Projectile.owner)
                {
                    var dashCooldown = owner.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
                    if (owner.GetModPlayer<BBAccessoryPlayer>().ImpactRestarterEquipped)
                        dashCooldown.ClearCooldown();
                    else
                        dashCooldown.ReduceCooldownTo(60);
                }
                Projectile.netUpdate = true;
                return;
            }

            if (owner.GetModPlayer<BBAccessoryPlayer>().ImpactRestarterEquipped && Main.myPlayer == Projectile.owner)
                owner.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().ClearCooldown();

            // LostGarment mode: pass through enemies, keep dashing
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (canceledCharge)
                return;

            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead && dashState != 0)
                owner.velocity *= 0.85f;

            SpawnEndBurst();

            SoundEngine.PlaySound(SoundID.Item107 with
            {
                Volume = 0.45f,
                Pitch = -0.15f
            }, Projectile.Center);
        }

        private bool IsChargeHeld(Player owner)
        {
            if (owner.whoAmI != Main.myPlayer)
                return true;

            return owner.Calamity().mouseRight &&
                   !owner.noItems &&
                   !owner.CCed &&
                   !owner.mouseInterface &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private void RecordDashDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return;

            dashDirectionHistory.Add(direction);
            if (dashDirectionHistory.Count > DashHistoryLength)
                dashDirectionHistory.RemoveAt(0);
        }

        private Vector2 GetReliableDashDirection()
        {
            if (dashDirectionHistory.Count == 0)
                return lockedDirection;

            Vector2 sum = Vector2.Zero;
            foreach (Vector2 direction in dashDirectionHistory)
                sum += direction;

            return sum.SafeNormalize(lockedDirection);
        }

        private static ShortDashProfile ResolveDashGrowthProfile()
        {
            int tier = GetShortDashGrowthTier();
            return new ShortDashProfile(
                ShortDashSpeedMultipliers[tier],
                ShortDashContactDamageMultipliers[tier],
                ShortDashEnemyReboundUnlocks[tier]);
        }

        private static int GetShortDashGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 4;
            if (NPC.downedFishron)
                return 3;
            if (CalamityMod.DownedBossSystem.downedCalamitasClone || NPC.downedPlantBoss)
                return 2;
            if (Main.hardMode)
                return 1;

            return 0;
        }

        private Vector2 GetAimDirection(Player owner, Vector2 fallbackDirection)
        {
            return (owner.Calamity().mouseWorld - owner.MountedCenter).SafeNormalize(fallbackDirection);
        }

        private Vector2 ResolveSlidingVelocity(Player owner, Vector2 desiredVelocity)
        {
            Vector2 adjustedVelocity = Collision.TileCollision(owner.position, desiredVelocity, owner.width, owner.height, false, false, (int)owner.gravDir);

            if (adjustedVelocity.X != desiredVelocity.X || adjustedVelocity.Y != desiredVelocity.Y)
            {
                GrantTideFromTileContact(owner);

                if (adjustedVelocity.LengthSquared() > 0.01f)
                    return adjustedVelocity;

                Vector2 horizontalSlide = new Vector2(desiredVelocity.X, 0f);
                Vector2 verticalSlide = new Vector2(0f, desiredVelocity.Y);

                Vector2 horizontalAdjusted = Collision.TileCollision(owner.position, horizontalSlide, owner.width, owner.height, false, false, (int)owner.gravDir);
                if (horizontalAdjusted.LengthSquared() > 0.01f)
                    return horizontalAdjusted;

                Vector2 verticalAdjusted = Collision.TileCollision(owner.position, verticalSlide, owner.width, owner.height, false, false, (int)owner.gravDir);
                if (verticalAdjusted.LengthSquared() > 0.01f)
                    return verticalAdjusted;
            }

            return adjustedVelocity;
        }

        private void GrantTideFromTileContact(Player owner)
        {
            if (tileContactTideGranted || Main.myPlayer != Projectile.owner)
                return;

            tileContactTideGranted = true;
            owner.GetModPlayer<BBTideValuePlayer>().AddTide();
            Projectile.netUpdate = true;
        }

        private void StartRebound(Player owner, Vector2 impactCenter)
        {
            hasBounced = true;
            dashState = 2;
            stateTimer = 0;

            Vector2 reliableDashDirection = GetReliableDashDirection();
            float offsetAngle = MathHelper.ToRadians(Main.rand.NextFloat(-12f, 12f));
            lockedDirection = (-reliableDashDirection).RotatedBy(offsetAngle).SafeNormalize(-lockedDirection);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            Projectile.friendly = false;
            Projectile.velocity = lockedDirection * ReboundSpeed * DashSpeedMultiplier;
            SyncOwnerToProjectile(owner, ReboundBladeDistance);
            Projectile.netUpdate = true;

            SpawnBounceBurst(impactCenter, reliableDashDirection);
            ApplyScreenShake(10f);

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 0.85f,
                Pitch = -0.1f
            }, impactCenter);
        }

        private void SpawnWaterPillarBurst(Vector2 impactCenter, Vector2 dashDirection)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 baseDirection = dashDirection.SafeNormalize(lockedDirection);
            Vector2 sideDirection = baseDirection.RotatedBy(MathHelper.PiOver2);
            int pillarDamage = 0;

            for (int i = 0; i < 5; i++)
            {
                float laneOffset = i == 2 ? 0f : MathHelper.Lerp(-76f, 76f, i / 4f) + Main.rand.NextFloat(-12f, 12f);
                Vector2 spawnPosition = impactCenter + sideDirection * laneOffset + baseDirection * Main.rand.NextFloat(-18f, 22f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    -Vector2.UnitY * Main.rand.NextFloat(3.2f, 5.4f),
                    ModContent.ProjectileType<BrinyBaron_DashWaterPillar>(),
                    pillarDamage,
                    0f,
                Projectile.owner,
                Main.rand.NextFloat(0.86f, 1.22f),
                i == 2 ? 1f : 0f);
            }
        }

        private void SpawnCeruleanShieldExplosion(Vector2 impactCenter, Vector2 dashDirection)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 baseDirection = dashDirection.SafeNormalize(lockedDirection);
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = baseDirection.RotatedByRandom(0.9f) * Main.rand.NextFloat(2.4f, 6.2f);
                Dust foam = Dust.NewDustPerfect(
                    impactCenter + Main.rand.NextVector2Circular(18f, 18f),
                    DustID.Water,
                    velocity,
                    100,
                    new Color(90, 210, 255),
                    Main.rand.NextFloat(0.85f, 1.3f));
                foam.noGravity = true;
            }
        }

        private void TryFireDashProjectile(Player owner, Vector2 dashDirection)
        {
            if (ReboundDashMode)
                return;

            if (Main.myPlayer != Projectile.owner)
                return;

            dashShotTimer++;
            if (dashShotTimer < 7)
                return;

            dashShotTimer = 0;
            Vector2 forward = dashDirection.SafeNormalize(lockedDirection);
            Vector2 spawnPosition = owner.MountedCenter + forward * 34f + forward.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-18f, 18f);
            Vector2 velocity = forward.RotatedByRandom(0.18f) * Main.rand.NextFloat(12f, 15.5f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                Math.Max(1, (int)(Projectile.damage * 0.32f)),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                0.25f);
        }

        private void SpawnStartBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = lockedDirection.SafeNormalize(Vector2.UnitX);

            // 剑刃凝现：轻微水光散逸，像刀身从海水中被抽出
            for (int i = 0; i < 6; i++)
            {
                Vector2 dustVel = forward.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.2f, 3.5f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    dustVel, 120,
                    new Color(100, 215, 255),
                    Main.rand.NextFloat(0.55f, 0.82f));
                d.noGravity = true;
            }

            // GlowOrbParticle 凝聚光点
            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    forward * Main.rand.NextFloat(0.4f, 1.2f) + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.05f, 0.10f),
                    Color.Lerp(new Color(80, 190, 255), new Color(195, 245, 255), Main.rand.NextFloat())));
            }
        }

        private void SpawnChargeReadyBurst()
        {
            ApplyScreenShake(7f);

            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 0.85f,
                Pitch = -0.22f
            }, Projectile.Center);

            SoundEngine.PlaySound(SoundID.Splash with
            {
                Volume = 0.65f,
                Pitch = -0.15f
            }, Projectile.Center);

            if (Main.dedServ)
                return;

            Vector2 forward = lockedDirection.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = Projectile.Center + forward * ReadyBladeDistance;

            // 三层 DirectionalPulseRing 向前爆破（借鉴 Xyk 多层前冲脉冲技术，水蓝色调）
            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    tip - forward * (4f + i * 3.5f),
                    forward * (0.10f + i * 0.025f),
                    Color.Lerp(new Color(55, 175, 255), Color.White, 0.18f + i * 0.09f),
                    new Vector2(0.88f, 2.4f),
                    forward.ToRotation(),
                    0.25f + i * 0.04f,
                    0.05f,
                    16 - i * 2));
            }

            // 圆形冲击波（近圆 DirectionalPulseRing 模拟爆破环，Xyk BloomRing 思路）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                tip, Vector2.Zero,
                new Color(65, 195, 255) with { A = 0 },
                new Vector2(1.05f, 1.05f),
                0f, 0.22f, 0.04f, 24));

            // GlowBlade 刀气火花（已有 FlightEffects 技术直接复用）
            const string GlowBladeTexture = "CalamityLegendsComeBack/Texture/Shared/GlowBlade";
            for (int i = 0; i < 4; i++)
            {
                float sideOff = Main.rand.NextFloatDirection() * 5f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    tip - forward * (5f + i * 2.5f) + right * sideOff,
                    forward * 0.03f + right * sideOff * 0.008f,
                    GlowBladeTexture,
                    false, 7, 0.17f,
                    new Color(145, 235, 255) * 0.92f,
                    new Vector2(0.55f, 1.85f),
                    glowCenter: true, shrinkSpeed: 0.95f, glowCenterScale: 0.88f, glowOpacity: 0.70f));
            }

            // 水/霜尘粒子喷溅（已有水系特效，保留）
            for (int i = 0; i < 10; i++)
            {
                Vector2 dustVel = forward.RotatedByRandom(0.85f) * Main.rand.NextFloat(2.2f, 6.5f);
                Dust d = Dust.NewDustPerfect(
                    tip + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    dustVel, 100,
                    new Color(120, 222, 255),
                    Main.rand.NextFloat(0.72f, 1.02f));
                d.noGravity = true;
            }
        }

        private void SpawnCanceledChargeBurst()
        {
        }

        private void SpawnPrepareTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = lockedDirection.SafeNormalize(Vector2.UnitX);
            float chargeProgress = Utils.GetLerpValue(0f, PrepareTime, stateTimer, true);

            // 蓄力轨迹：GlowOrbParticle 随充能进度从剑身向后飘散，颜色由深蓝渐白
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                -forward * Main.rand.NextFloat(0.5f, 1.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                false,
                Main.rand.Next(12, 20),
                MathHelper.Lerp(0.05f, 0.13f, chargeProgress),
                Color.Lerp(new Color(80, 185, 255), new Color(200, 248, 255), chargeProgress)));

            // 50% 概率伴随水尘
            if (Main.rand.NextBool(2))
            {
                Vector2 dustVel = forward.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.8f, 2.5f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Water, dustVel, 130,
                    new Color(100, 210, 255),
                    Main.rand.NextFloat(0.48f, 0.72f));
                d.noGravity = true;
            }
        }

        private void SpawnBounceBurst(Vector2 center, Vector2 dashDirection)
        {
            // 注意：SpawnWaterPillarBurst 和 SpawnCeruleanShieldExplosion 已在 OnHitNPC 中单独调用，此处不重复
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(lockedDirection);

            // 四方向 DirectionalPulseRing 爆开（击中反弹冲击感）
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4f + forward.ToRotation() * 0.5f;
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    center,
                    Vector2.UnitX.RotatedBy(angle) * 0.05f,
                    Color.Lerp(new Color(60, 185, 255), Color.White, 0.14f + i * 0.04f),
                    new Vector2(0.75f, 1.95f),
                    angle, 0.20f + i * 0.025f, 0.045f, 20));
            }

            // 大圆形冲击波（Xyk BloomRing 思路，水蓝近圆 DirectionalPulseRing）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                new Color(55, 195, 255) with { A = 0 },
                new Vector2(1.1f, 1.1f),
                0f, 0.28f, 0.04f, 30));

            // GlowBlade 刀气溅射（已有 FlightEffects 技术，保留）
            const string GlowBladeTexture = "CalamityLegendsComeBack/Texture/Shared/GlowBlade";
            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkDir = forward.RotatedByRandom(MathHelper.Pi * 0.65f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    sparkDir * Main.rand.NextFloat(0.06f, 0.20f),
                    GlowBladeTexture,
                    false, 9, 0.22f,
                    new Color(160, 242, 255) * 0.9f,
                    new Vector2(0.50f, 2.2f),
                    glowCenter: true, shrinkSpeed: 0.88f, glowCenterScale: 0.90f, glowOpacity: 0.75f));
            }

            // WaterFoamParticle 水沫（已有被动冲刺特效中的技术，保留结合）
            for (int i = 0; i < 5; i++)
            {
                Vector2 foamVel = forward.RotatedByRandom(1.0f) * Main.rand.NextFloat(1.5f, 4.5f);
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    center + Main.rand.NextVector2Circular(14f, 14f),
                    foamVel,
                    Main.rand.Next(18, 34),
                    Main.rand.NextFloat(0.52f, 0.90f),
                    Color.Lerp(new Color(140, 225, 255), Color.White, Main.rand.NextFloat(0.15f, 0.42f))));
            }

            // 水/霜尘爆炸（已有水系特效，保留）
            for (int i = 0; i < 16; i++)
            {
                Vector2 dustVel = (i % 2 == 0
                    ? forward.RotatedByRandom(MathHelper.Pi)
                    : Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 16f)) * Main.rand.NextFloat(3f, 8.5f);
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    dustVel, 100,
                    new Color(110, 215, 255),
                    Main.rand.NextFloat(0.78f, 1.22f));
                d.noGravity = true;
            }

            // 泡泡 Gore（已有 FlightEffects SpawnOuterWake 技术，保留结合）
            for (int i = 0; i < 4; i++)
            {
                Gore bubble = Gore.NewGorePerfect(
                    Projectile.GetSource_FromAI(),
                    center + Main.rand.NextVector2Circular(20f, 20f),
                    forward.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.8f, 4.5f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    Main.rand.NextBool(3) ? 412 : 411);
                bubble.timeLeft = 8 + Main.rand.Next(7);
                bubble.scale = Main.rand.NextFloat(0.55f, 0.95f);
            }
        }

        private void SpawnEndBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = lockedDirection.SafeNormalize(Vector2.UnitX);

            // 五方向 DirectionalPulseRing 散开（水面破碎感）
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f + forward.ToRotation();
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center,
                    Vector2.UnitX.RotatedBy(angle) * 0.04f,
                    Color.Lerp(new Color(55, 175, 255), Color.White, 0.12f + i * 0.05f),
                    new Vector2(0.65f, 1.65f),
                    angle, 0.14f + i * 0.022f, 0.04f, 22));
            }

            // 结束圆形冲击波（Xyk BloomRing 思路，消散感）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero,
                new Color(55, 175, 255) with { A = 0 },
                new Vector2(0.95f, 0.95f),
                0f, 0.15f, 0.038f, 26));

            // GlowOrbParticle 七方向均匀散射
            for (int i = 0; i < 7; i++)
            {
                Vector2 orbDir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 7f + forward.ToRotation() * 0.3f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    orbDir * Main.rand.NextFloat(1.2f, 3.8f),
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.07f, 0.16f),
                    Color.Lerp(new Color(75, 195, 255), new Color(195, 245, 255), Main.rand.NextFloat())));
            }

            // 水/霜尘喷射（已有水系特效，保留）
            for (int i = 0; i < 14; i++)
            {
                Vector2 dustVel = forward.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(2.2f, 7f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                    Main.rand.NextBool(4) ? DustID.Frost : DustID.Water,
                    dustVel, 100,
                    new Color(100, 210, 255),
                    Main.rand.NextFloat(0.72f, 1.08f));
                d.noGravity = true;
            }
        }

        private void ApplyScreenShake(float power)
        {
            float distanceFactor = Utils.GetLerpValue(1200f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                power * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowBlade = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowBlade").Value;
            Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 glowOrigin = new(glowBlade.Width * 0.5f, glowBlade.Height);
            Vector2 forward = Projectile.velocity.SafeNormalize(lockedDirection).SafeNormalize(Vector2.UnitX);
            Vector2 screenOffset = new(0f, Projectile.gfxOffY);
            Vector2 drawCenter = Projectile.Center + screenOffset - Main.screenPosition;
            Vector2 glowAnchor = BrinyBaron_SkillDashTornado_FlightEffects.GetFrontAnchor(Projectile, lockedDirection) + screenOffset - Main.screenPosition;
            float glowProgress = dashState == 2
                ? Utils.GetLerpValue(0f, ReboundTimeMax, stateTimer, true)
                : Utils.GetLerpValue(0f, DashTimeMax, stateTimer, true);
            float glowStrength = dashState == 2 ? 0.72f : 1f;
            float glowPulse = 1f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.41f);
            float outerLength = dashState == 2
                ? MathHelper.Lerp(0.95f, 1.8f, glowProgress)
                : MathHelper.Lerp(1.2f, 2.55f, glowProgress);
            float coreLength = dashState == 2
                ? MathHelper.Lerp(0.7f, 1.3f, glowProgress)
                : MathHelper.Lerp(0.82f, 1.95f, glowProgress);
            float glowRotation = forward.ToRotation() + MathHelper.PiOver2;
            Vector2 haloGlowScale = new Vector2(1.42f, outerLength * 1.08f) * Projectile.scale * 0.05f * glowStrength * glowPulse;
            Vector2 shellGlowScale = new Vector2(1.02f, outerLength * 0.86f) * Projectile.scale * 0.045f * glowStrength * glowPulse;
            Vector2 coreGlowScale = new Vector2(0.68f, coreLength) * Projectile.scale * 0.043f * glowStrength * glowPulse;
            Color haloGlowColor = new Color(45, 205, 255, 0) * 1.1f * glowStrength;
            Color shellGlowColor = new Color(135, 238, 255, 0) * 0.92f * glowStrength;
            Color coreGlowColor = new Color(245, 255, 255, 0) * 0.88f * glowStrength;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                glowBlade,
                glowAnchor - forward * 9f,
                null,
                haloGlowColor,
                glowRotation,
                glowOrigin,
                haloGlowScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                glowBlade,
                glowAnchor - forward * 4.5f,
                null,
                shellGlowColor,
                glowRotation,
                glowOrigin,
                shellGlowScale,
                SpriteEffects.None,
                0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPos = Projectile.oldPos[i];
                if (oldPos == Vector2.Zero)
                    continue;

                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(40, 90, 140, 0), new Color(120, 220, 255, 0), factor) * factor * 0.6f;

                Main.EntitySpriteDraw(
                    texture,
                    oldPos + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    trailColor,
                    bladeRotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawCenter,
                null,
                lightColor,
                bladeRotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                glowBlade,
                glowAnchor - forward * 1.5f,
                null,
                coreGlowColor,
                glowRotation,
                glowOrigin,
                coreGlowScale,
                SpriteEffects.None,
                0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private readonly struct ShortDashProfile
        {
            public readonly float SpeedMultiplier;
            public readonly float ContactDamageMultiplier;
            public readonly bool EnemyReboundUnlocked;

            public ShortDashProfile(float speedMultiplier, float contactDamageMultiplier, bool enemyReboundUnlocked)
            {
                SpeedMultiplier = speedMultiplier;
                ContactDamageMultiplier = contactDamageMultiplier;
                EnemyReboundUnlocked = enemyReboundUnlocked;
            }
        }
    }
}
