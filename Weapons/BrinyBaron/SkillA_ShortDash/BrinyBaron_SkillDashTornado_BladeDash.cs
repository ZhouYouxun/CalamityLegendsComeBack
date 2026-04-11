using System;
using CalamityMod;
using CalamityMod.Enums;
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
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        private const int PrepareTime = 22;
        private const int DashTimeMax = 26;
        private const int ReboundTimeMax = 12;
        private const int DashHistoryLength = 8;
        private const float DashSpeed = 28f;
        private const float ReboundSpeed = 18f;
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
        private float oceanPhase;
        private float dashSpeedMultiplier;
        private float contactDamageMultiplier;
        private bool enemyReboundUnlocked;
        private readonly System.Collections.Generic.List<Vector2> dashDirectionHistory = new();

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
            lockedDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter + lockedDirection * 18f;
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            dashState = 0;
            stateTimer = 0;
            hasBounced = false;
            canceledCharge = false;
            oceanPhase = 0f;
            BB_Balance.ShortDashProfile growthProfile = ResolveDashGrowthProfile();
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
        }

        private void DoPreparePhase(Player owner)
        {
            if (!IsChargeHeld(owner))
            {
                canceledCharge = true;
                SpawnCanceledChargeBurst();
                Projectile.Kill();
                return;
            }

            stateTimer++;
            Projectile.velocity = Vector2.Zero;

            Vector2 aimDirection = (owner.Calamity().mouseWorld - owner.MountedCenter).SafeNormalize(lockedDirection);
            lockedDirection = Vector2.Lerp(lockedDirection, aimDirection, 0.18f).SafeNormalize(aimDirection);

            float chargeProgress = Utils.GetLerpValue(0f, PrepareTime, stateTimer, true);
            Projectile.Center = owner.MountedCenter + lockedDirection * MathHelper.Lerp(18f, ReadyBladeDistance, chargeProgress);
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
            Projectile.velocity = lockedDirection * (DashSpeed * dashSpeedMultiplier);
            SyncOwnerToProjectile(owner, DashBladeDistance);
            RecordDashDirection(Projectile.velocity.SafeNormalize(lockedDirection));
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item39 with
            {
                Volume = 0.85f,
                Pitch = -0.2f
            }, Projectile.Center);

            SpawnDashBurst();
        }

        private void DoDashPhase(Player owner)
        {
            stateTimer++;
            Projectile.velocity = lockedDirection * (DashSpeed * dashSpeedMultiplier);
            Projectile.Center += Projectile.velocity;
            SyncOwnerToProjectile(owner, DashBladeDistance);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            RecordDashDirection(Projectile.velocity.SafeNormalize(lockedDirection));
            SpawnOceanTrail();
            SpawnForwardWakeJets();

            if (stateTimer % 4 == 0)
                SpawnWaveRing(false);

            if (stateTimer >= DashTimeMax)
                Projectile.Kill();
        }

        private void DoReboundPhase(Player owner)
        {
            stateTimer++;

            float speedFactor = MathHelper.Lerp(1f, 0.55f, stateTimer / (float)ReboundTimeMax);
            Projectile.velocity = lockedDirection * ReboundSpeed * speedFactor;
            Projectile.Center += Projectile.velocity;
            SyncOwnerToProjectile(owner, ReboundBladeDistance);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            SpawnReboundTrail();

            if (stateTimer % 3 == 0)
                SpawnWaveRing(true);

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
            Vector2 mountedCenterOffset = owner.MountedCenter - owner.Center;
            Vector2 desiredMountedCenter = Projectile.Center - lockedDirection * bladeDistance;
            owner.Center = desiredMountedCenter - mountedCenterOffset;
            owner.velocity = Projectile.velocity;
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

            if (!enemyReboundUnlocked)
            {
                Projectile.Kill();
                return;
            }

            StartRebound(target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (dashState != 1 || hasBounced)
                return true;

            Vector2 collisionCenter = Projectile.Center - oldVelocity.SafeNormalize(lockedDirection) * 8f;
            StartRebound(collisionCenter);
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

        private BB_Balance.ShortDashProfile ResolveDashGrowthProfile()
        {
            return BB_Balance.GetShortDashProfile();
        }

        private void StartRebound(Vector2 impactCenter)
        {
            hasBounced = true;
            dashState = 2;
            stateTimer = 0;

            Vector2 reliableDashDirection = GetReliableDashDirection();
            float offsetAngle = MathHelper.ToRadians(Main.rand.NextFloat(-12f, 12f));
            lockedDirection = (-reliableDashDirection).RotatedBy(offsetAngle).SafeNormalize(-lockedDirection);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            Projectile.friendly = false;
            Projectile.velocity = lockedDirection * ReboundSpeed;
            Projectile.netUpdate = true;

            SpawnBounceBurst(impactCenter, reliableDashDirection);
            ApplyScreenShake(10f);

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 0.85f,
                Pitch = -0.1f
            }, impactCenter);
        }

        private void SpawnStartBurst()
        {
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 5.5f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel, 100, new Color(90, 180, 255), Main.rand.NextFloat(1f, 1.35f));
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.7f, 100, new Color(180, 240, 255), Main.rand.NextFloat(0.9f, 1.2f));
                frost.noGravity = true;
            }
        }

        private void SpawnChargeReadyBurst()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 24; i++)
            {
                float ratio = i / 23f;
                float angle = MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, ratio);
                Vector2 velocity = forward.RotatedBy(angle * 0.45f) * Main.rand.NextFloat(4.8f, 11f) + right * (float)Math.Sin(angle * 3f) * Main.rand.NextFloat(0.5f, 3.2f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, velocity, 100, new Color(70, 175, 255), Main.rand.NextFloat(1.1f, 1.45f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, velocity * 0.68f, 100, new Color(215, 248, 255), Main.rand.NextFloat(0.9f, 1.2f));
                    frost.noGravity = true;
                }
            }

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
        }

        private void SpawnCanceledChargeBurst()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2Circular(1.8f, 1.8f);
                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel, 100, new Color(95, 180, 255), Main.rand.NextFloat(0.8f, 1.05f));
                water.noGravity = true;
            }
        }

        private void SpawnDashBurst()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 20; i++)
            {
                float ratio = i / 19f;
                float spread = MathHelper.Lerp(-0.95f, 0.95f, ratio);
                Vector2 burstVel =
                    (forward * Main.rand.NextFloat(4f, 8.5f) + right * spread * Main.rand.NextFloat(2f, 6f))
                    .RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f));

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.1f, 1.45f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.75f, 100, new Color(210, 250, 255), Main.rand.NextFloat(0.95f, 1.25f));
                    frost.noGravity = true;
                }
            }
        }

        private void SpawnPrepareTrail()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 bladeTip = Projectile.Center + forward * 36f;
            float progress = Utils.GetLerpValue(0f, PrepareTime, stateTimer, true);

            for (int i = 0; i < 4; i++)
            {
                float side = MathHelper.Lerp(-1f, 1f, i / 3f);
                float lanePhase = oceanPhase * 1.3f + i * 0.85f;
                float lateralRadius = MathHelper.Lerp(10f, 24f, progress) * (0.7f + 0.3f * (float)Math.Sin(lanePhase));
                float backDistance = MathHelper.Lerp(20f, 58f, progress) + 8f * (float)Math.Cos(lanePhase * 0.8f);
                Vector2 spawnPos =
                    bladeTip -
                    forward * backDistance +
                    right * side * lateralRadius +
                    Main.rand.NextVector2Circular(3f, 3f);
                Vector2 toBlade = (bladeTip - spawnPos).SafeNormalize(forward);
                Vector2 curl = toBlade.RotatedBy(MathHelper.PiOver2 * Math.Sign(side == 0f ? 1f : side));
                Vector2 dustVel =
                    toBlade * Main.rand.NextFloat(2.6f, 5.4f) +
                    curl * Main.rand.NextFloat(0.8f, 2.2f) +
                    ownerVelocityInfluence();

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, dustVel, 100, new Color(80, 165, 255), Main.rand.NextFloat(0.95f, 1.2f));
                water.noGravity = true;
                water.fadeIn = 1.08f;

                Dust frost = Dust.NewDustPerfect(spawnPos - toBlade * 4f, DustID.Frost, dustVel * 0.65f, 100, new Color(185, 240, 255), Main.rand.NextFloat(0.85f, 1.05f));
                frost.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Dust gem = Dust.NewDustPerfect(spawnPos + curl * 4f, DustID.GemSapphire, dustVel * 0.42f, 100, new Color(120, 220, 255), Main.rand.NextFloat(0.85f, 1.1f));
                    gem.noGravity = true;
                }
            }

            Vector2 ownerVelocityInfluence()
            {
                return Main.player[Projectile.owner].velocity * 0.08f;
            }
        }

        private void SpawnOceanTrail()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 3; i++)
            {
                float backDistance = 18f + i * 14f;
                float waveWidth = 10f + i * 3f;
                float waveA = (float)Math.Sin(oceanPhase + i * 0.8f) * waveWidth;
                float waveB = (float)Math.Sin(oceanPhase + MathHelper.Pi + i * 0.8f) * waveWidth;

                Vector2 posA = Projectile.Center - forward * backDistance + right * waveA;
                Vector2 posB = Projectile.Center - forward * backDistance + right * waveB;

                Vector2 velA = (-forward * Main.rand.NextFloat(1.4f, 3.2f) + right * waveA * 0.08f).RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f));
                Vector2 velB = (-forward * Main.rand.NextFloat(1.4f, 3.2f) + right * waveB * 0.08f).RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f));

                Dust waterA = Dust.NewDustPerfect(posA, DustID.Water, velA, 100, new Color(70, 170, 255), Main.rand.NextFloat(1f, 1.35f));
                waterA.noGravity = true;

                Dust waterB = Dust.NewDustPerfect(posB, DustID.Water, velB, 100, new Color(70, 170, 255), Main.rand.NextFloat(1f, 1.35f));
                waterB.noGravity = true;

                if (i < 2)
                {
                    Dust frostA = Dust.NewDustPerfect(posA, DustID.Frost, velA * 0.6f, 100, new Color(200, 245, 255), Main.rand.NextFloat(0.85f, 1.1f));
                    frostA.noGravity = true;

                    Dust frostB = Dust.NewDustPerfect(posB, DustID.Frost, velB * 0.6f, 100, new Color(200, 245, 255), Main.rand.NextFloat(0.85f, 1.1f));
                    frostB.noGravity = true;
                }
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 corePos = Projectile.Center - forward * Main.rand.NextFloat(10f, 26f) + Main.rand.NextVector2Circular(3f, 3f);
                Vector2 coreVel = -forward * Main.rand.NextFloat(0.8f, 2.2f);

                Dust gem = Dust.NewDustPerfect(corePos, DustID.GemSapphire, coreVel, 100, new Color(120, 220, 255), Main.rand.NextFloat(0.95f, 1.2f));
                gem.noGravity = true;
            }
        }

        private void SpawnForwardWakeJets()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float leadDistance = 54f + 12f * (float)Math.Sin(oceanPhase * 0.9f);
            Vector2 leadPoint = Projectile.Center + forward * leadDistance;

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float phase = oceanPhase * 1.35f + i * MathHelper.Pi;
                float sideOffset = side * (16f + 7f * (float)Math.Sin(phase));
                float forwardOffset = 10f + 6f * (float)Math.Cos(phase * 0.85f);

                Vector2 spawnPos = leadPoint + right * sideOffset + forward * forwardOffset;
                Vector2 sprayDirection = (right * side * 0.88f + forward * 0.36f).SafeNormalize(right * side);
                Vector2 dustVelocity = sprayDirection * Main.rand.NextFloat(5f, 9.5f) + forward * Main.rand.NextFloat(1f, 3f);

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, dustVelocity, 100, new Color(90, 190, 255), Main.rand.NextFloat(1.05f, 1.45f));
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(spawnPos - forward * 5f, DustID.Frost, dustVelocity * 0.7f, 100, new Color(215, 250, 255), Main.rand.NextFloat(0.85f, 1.15f));
                frost.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 corePos = leadPoint + Main.rand.NextVector2Circular(5f, 5f);
                Vector2 coreVel = forward * Main.rand.NextFloat(1.5f, 3.4f);

                Dust gem = Dust.NewDustPerfect(corePos, DustID.GemSapphire, coreVel, 100, new Color(160, 235, 255), Main.rand.NextFloat(0.95f, 1.18f));
                gem.noGravity = true;
            }
        }

        private void SpawnReboundTrail()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 3; i++)
            {
                float backDistance = 10f + i * 10f;
                float sideOffset = Main.rand.NextFloat(-12f, 12f) + (float)Math.Sin(oceanPhase + i) * 6f;

                Vector2 spawnPos = Projectile.Center - forward * backDistance + right * sideOffset;
                Vector2 dustVel = -forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 4.5f);

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, dustVel, 100, new Color(80, 170, 255), Main.rand.NextFloat(1f, 1.3f));
                water.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Dust frost = Dust.NewDustPerfect(spawnPos, DustID.Frost, dustVel * 0.7f, 100, new Color(210, 250, 255), Main.rand.NextFloat(0.85f, 1.15f));
                    frost.noGravity = true;
                }
            }
        }

        private void SpawnWaveRing(bool stronger)
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            int count = stronger ? 16 : 12;
            float radiusX = stronger ? 24f : 18f;
            float radiusY = stronger ? 10f : 7f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi / count * i;
                Vector2 offset = new Vector2(radiusX, radiusY).RotatedBy(angle);
                Vector2 spawnPos = Projectile.Center - forward * 14f + right * offset.Y + forward * offset.X * 0.15f;
                Vector2 dustVel = (spawnPos - Projectile.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 3f);

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, dustVel, 100, new Color(80, 175, 255), Main.rand.NextFloat(0.95f, 1.2f));
                water.noGravity = true;
            }
        }

        private void SpawnBounceBurst(Vector2 center, Vector2 dashDirection)
        {
            Vector2 reboundDirection = (-dashDirection).SafeNormalize(-lockedDirection);
            Vector2 right = reboundDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 20; i++)
            {
                float factor = i / 19f;
                float angle = MathHelper.Lerp(-MathHelper.ToRadians(55f), MathHelper.ToRadians(55f), factor);
                Vector2 dir = reboundDirection.RotatedBy(angle);
                Vector2 burstVel = dir * MathHelper.Lerp(4.5f, 10f, factor);

                Dust water = Dust.NewDustPerfect(center, DustID.Water, burstVel, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.15f, 1.45f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(center, DustID.Frost, burstVel * 0.72f, 100, new Color(220, 250, 255), Main.rand.NextFloat(0.95f, 1.2f));
                    frost.noGravity = true;
                }
            }

            for (int i = 0; i < 16; i++)
            {
                float side = MathHelper.Lerp(-1f, 1f, i / 15f);
                Vector2 burstVel = right * side * Main.rand.NextFloat(2f, 7f) + reboundDirection * Main.rand.NextFloat(1.5f, 4.2f);

                Dust water = Dust.NewDustPerfect(center + right * side * 10f, DustID.Water, burstVel, 100, new Color(90, 185, 255), Main.rand.NextFloat(1f, 1.3f));
                water.noGravity = true;
            }
        }

        private void SpawnEndBurst()
        {
            int count = 16;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi / count * i;
                Vector2 burstVel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel, 100, new Color(70, 165, 255), Main.rand.NextFloat(1f, 1.35f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.7f, 100, new Color(210, 245, 255), Main.rand.NextFloat(0.9f, 1.15f));
                    frost.noGravity = true;
                }
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
            Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);

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

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                bladeRotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
