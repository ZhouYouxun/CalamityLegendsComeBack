using System;
using System.Linq;
using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    public class BrinyBaron_SkillDashTornado_BladeDash : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        private const int DashHistoryLength = 8;

        private int dashState;
        private int stateTimer;
        private Vector2 lockedDirection = Vector2.UnitX;
        private float bladeRotation;
        private bool initialized;
        private bool hasBounced;
        private bool enemyImpactRebound;
        private bool canceledCharge;
        private bool tileContactTideGranted;
        private float oceanPhase;
        private float dashSpeedMultiplier;
        private float contactDamageMultiplier;
        private bool enemyReboundUnlocked;
        private int dashShotTimer;
        private readonly System.Collections.Generic.List<Vector2> dashDirectionHistory = new();
        private bool ReboundDashMode => Projectile.ai[0] == 2f;
        private float DashSpeedMultiplier => ReboundDashMode ? BB_Balance.DefaultReboundDashSpeedMultiplier : 1f;
        private float EnemyImpactReboundSpeedMultiplier => ReboundDashMode && enemyImpactRebound ? BB_Balance.RightClickEnemyReboundSpeedMultiplier : 1f;
        private bool AbyssalBastionEquipped => Main.player.IndexInRange(Projectile.owner) && Main.player[Projectile.owner].GetModPlayer<BBAccessoryPlayer>().AbyssalBastionEquipped;
        private float AbyssalDashMultiplier => AbyssalBastionEquipped ? BB_Balance.AbyssalBastionDashSpeedMultiplier : 1f;
        private int DashTimeLimit => AbyssalBastionEquipped ? BB_Balance.AbyssalBastionDashFrames : BB_Balance.RightClickDashFrames;

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
            Projectile.timeLeft = BB_Balance.RightClickPrepareFrames + BB_Balance.RightClickDashFrames + BB_Balance.RightClickReboundFrames + 40;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BB_Balance.RightClickDashLocalHitCooldown;
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
            lockedDirection = GetAimDirection(owner, Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction));

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter + lockedDirection * 18f;
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            dashState = 0;
            stateTimer = 0;
            hasBounced = false;
            enemyImpactRebound = false;
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
        }

        private void DoPreparePhase(Player owner)
        {
            stateTimer++;
            Projectile.velocity = Vector2.Zero;

            Vector2 aimDirection = (owner.Calamity().mouseWorld - owner.MountedCenter).SafeNormalize(lockedDirection);
            lockedDirection = Vector2.Lerp(lockedDirection, aimDirection, 0.18f).SafeNormalize(aimDirection);

            float chargeProgress = Utils.GetLerpValue(0f, BB_Balance.RightClickPrepareFrames, stateTimer, true);
            float eased = MathHelper.SmoothStep(0f, 1f, chargeProgress);
            // Exoblade-style pullback on charge, then thrust forward right before dash
            float offsetDist = MathHelper.Lerp(-24f, BB_Balance.RightClickReadyBladeDistance, MathF.Pow(eased, 0.6f));
            Projectile.Center = owner.MountedCenter + lockedDirection * offsetDist;
            Projectile.scale = MathHelper.Lerp(0.08f, 1.12f, MathF.Pow(eased, 0.4f));
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            if (stateTimer % 2 == 0)
                SpawnPrepareTrail();

            if (stateTimer >= BB_Balance.RightClickPrepareFrames)
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
            enemyImpactRebound = false;

            Projectile.friendly = true;
            Projectile.Center = owner.MountedCenter + lockedDirection * BB_Balance.RightClickDashBladeDistance;
            Projectile.velocity = lockedDirection * (BB_Balance.RightClickDashSpeed * dashSpeedMultiplier * DashSpeedMultiplier * AbyssalDashMultiplier);
            SyncOwnerToProjectile(owner, BB_Balance.RightClickDashBladeDistance);
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
            float turnedRotation = lockedDirection.ToRotation().AngleTowards(aimDirection.ToRotation(), BB_Balance.RightClickDashTurnRate);
            lockedDirection = turnedRotation.ToRotationVector2();

            Vector2 desiredVelocity = lockedDirection * (BB_Balance.RightClickDashSpeed * dashSpeedMultiplier * DashSpeedMultiplier * AbyssalDashMultiplier);
            Vector2 actualVelocity = ResolveSlidingVelocity(owner, desiredVelocity);

            Projectile.velocity = actualVelocity;
            if (actualVelocity.LengthSquared() <= 0.01f)
            {
                StartRebound(owner, Projectile.Center);
                return;
            }

            float dashProgress = stateTimer / (float)DashTimeLimit;
            // Exoblade-style lunge retraction: smoothly retract position and shrink scale as dash completes
            float bladeDist = dashProgress > 0.72f
                ? MathHelper.Lerp(BB_Balance.RightClickDashBladeDistance, 8f, (dashProgress - 0.72f) / 0.28f)
                : BB_Balance.RightClickDashBladeDistance;

            Projectile.scale = dashProgress > 0.72f
                ? MathHelper.Lerp(1.12f, 0.4f, MathF.Pow((dashProgress - 0.72f) / 0.28f, 2f))
                : 1.12f;

            SyncOwnerToProjectile(owner, bladeDist);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            RecordDashDirection(actualVelocity.SafeNormalize(lockedDirection));
            BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects(Projectile, lockedDirection, bladeRotation, oceanPhase, stateTimer);
            TryFireDashProjectile(owner, actualVelocity.SafeNormalize(lockedDirection));

            if (stateTimer >= DashTimeLimit)
            {
                // Transition into smooth retraction phase instead of abrupt deletion
                dashState = 2;
                stateTimer = 0;
                Projectile.friendly = false;
                Projectile.velocity *= 0.15f;
                if (owner.velocity.Y > owner.maxFallSpeed)
                    owner.velocity.Y = owner.maxFallSpeed;
                Projectile.netUpdate = true;
            }
        }

        private void DoReboundPhase(Player owner)
        {
            stateTimer++;

            float reboundProgress = stateTimer / (float)BB_Balance.RightClickReboundFrames;
            float speedFactor = MathHelper.Lerp(1f, 0.2f, reboundProgress);
            Projectile.velocity = lockedDirection * BB_Balance.RightClickReboundSpeed * DashSpeedMultiplier * EnemyImpactReboundSpeedMultiplier * speedFactor;

            // Exoblade-style post-dash stasis: smoothly retract weapon back to player's hand and shrink to zero
            float bladeDist = MathHelper.Lerp(BB_Balance.RightClickReboundBladeDistance, 2f, MathF.Pow(reboundProgress, 0.6f));
            Projectile.scale = MathHelper.Lerp(hasBounced ? 1f : 0.4f, 0f, MathF.Pow(reboundProgress, 0.7f));

            SyncOwnerToProjectile(owner, bladeDist);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            BrinyBaron_SkillDashTornado_FlightEffects.SpawnReboundFlightEffects(Projectile, lockedDirection, bladeRotation, oceanPhase, stateTimer);

            if (stateTimer >= BB_Balance.RightClickReboundFrames)
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

            if (dashState == 1)
            {
                owner.immune = true;
                owner.immuneTime = Math.Max(owner.immuneTime, 2);
                if (lockedDirection.Y > 0f)
                    owner.controlDown = true;

                float speedY = Math.Abs(Projectile.velocity.Y);
                if (speedY > owner.maxFallSpeed)
                    owner.maxFallSpeed = speedY;
            }
            else if (dashState == 2)
            {
                owner.immune = true;
                owner.immuneTime = Math.Max(owner.immuneTime, 2);
            }
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

            Player owner = Main.player[Projectile.owner];
            target.AddBuff(BuffID.Frostburn, 180);
            // 水龙卷（含水柱/闪电/冲击）仅在肉后（血肉墙后=进入困难模式）解锁；肉前只造成一次伤害
            bool hardmodeOrLater = Main.hardMode;
            if (Main.myPlayer == Projectile.owner)
            {
                owner.GetModPlayer<BBTideValuePlayer>().AddTide(2);

                if (hardmodeOrLater)
                {
                    SpawnWaterPillarBurst(target.Center, GetReliableDashDirection());
                    SpawnJazzTyphoon(owner, target);
                }
            }

            if (hardmodeOrLater)
                SpawnDashImpactExplosion(target.Center, GetReliableDashDirection());
            // Match Myrindael's post-impact protection: a real hit grants a
            // standalone i-frame window that remains after the rebound begins.
            owner.GiveIFrames(-1, BB_Balance.RightClickEnemyHitIFrameDuration);
            StartRebound(owner, target.Center, fromEnemyImpact: true);

            owner.GetModPlayer<BBAccessoryPlayer>().GrantBubbleShield();

            if (Main.myPlayer == Projectile.owner)
            {
                var dashCooldown = owner.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
                dashCooldown.ReduceCooldownTo(BB_Balance.RightClickHitCooldownAfterEnemyHit);
            }
            Projectile.netUpdate = true;
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
            {
                owner.velocity *= 0.2f;
                if (owner.velocity.Y > owner.maxFallSpeed)
                    owner.velocity.Y = owner.maxFallSpeed;
            }

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
                BB_Balance.RightClickGrowthSpeedMultipliers[tier],
                BB_Balance.RightClickGrowthContactDamageMultipliers[tier],
                BB_Balance.RightClickGrowthEnemyReboundUnlocks[tier]);
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
            bool passThroughPlatforms = desiredVelocity.Y > 0f || (dashState == 1 && lockedDirection.Y > 0f);
            Vector2 adjustedVelocity = Collision.TileCollision(owner.position, desiredVelocity, owner.width, owner.height, passThroughPlatforms, false, (int)owner.gravDir);
            if (adjustedVelocity != desiredVelocity)
                GrantTideFromTileContact(owner);

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

        private void StartRebound(Player owner, Vector2 impactCenter, bool fromEnemyImpact = false)
        {
            hasBounced = true;
            enemyImpactRebound = fromEnemyImpact;
            dashState = 2;
            stateTimer = 0;

            if (owner.velocity.Y > owner.maxFallSpeed)
                owner.velocity.Y = owner.maxFallSpeed;

            Vector2 reliableDashDirection = GetReliableDashDirection();
            float offsetAngle = MathHelper.ToRadians(Main.rand.NextFloat(-12f, 12f));
            lockedDirection = (-reliableDashDirection).RotatedBy(offsetAngle).SafeNormalize(-lockedDirection);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            Projectile.friendly = false;
            Projectile.velocity = lockedDirection * BB_Balance.RightClickReboundSpeed * DashSpeedMultiplier * EnemyImpactReboundSpeedMultiplier;
            SyncOwnerToProjectile(owner, BB_Balance.RightClickReboundBladeDistance);
            owner.itemAnimation = 0;
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

            // One-shot WaterFoamParticle burst: 5 -> 2 particles (rounded down).
            for (int i = 0; i < 2; i++)
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

        private void SpawnJazzTyphoon(Player owner, NPC target)
        {
            bool helixVariant = owner.GetModPlayer<BBAccessoryPlayer>().BaronHelixEquipped;
            if (helixVariant)
            {
                int damage = Math.Max(1, (int)(Projectile.damage * BB_Balance.BaronHelixJazzTyphoonDamageMultiplier));
                for (int i = 0; i < 3; i++)
                    BrinyBaron_JazzTyphoon.Spawn(Projectile, target, damage, Projectile.knockBack, true, i);
                return;
            }

            int normalDamage = Math.Max(1, (int)(Projectile.damage * BB_Balance.JazzTyphoonDamageMultiplier));
            BrinyBaron_JazzTyphoon.Spawn(Projectile, target, normalDamage, Projectile.knockBack, false);
        }

        private void SpawnDashImpactExplosion(Vector2 impactCenter, Vector2 dashDirection)
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
            ApplyScreenShake(3f);

            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 0.68f,
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
            Vector2 tip = Projectile.Center + forward * BB_Balance.RightClickReadyBladeDistance;

            for (int i = 0; i < 5; i++)
            {
                float sideOffset = Main.rand.NextFloatDirection() * 12f;
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    tip - forward * Main.rand.NextFloat(2f, 13f) + right * sideOffset,
                    forward * Main.rand.NextFloat(0.55f, 1.35f) + right * sideOffset * 0.055f,
                    Main.rand.Next(16, 25),
                    Main.rand.NextFloat(0.36f, 0.58f),
                    Color.Lerp(new Color(85, 195, 255), Color.White, Main.rand.NextFloat(0.12f, 0.36f))));
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    tip - forward * Main.rand.NextFloat(2f, 11f) + Main.rand.NextVector2Circular(9f, 9f),
                    forward * Main.rand.NextFloat(0.35f, 0.9f) + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    false, Main.rand.Next(10, 17), Main.rand.NextFloat(0.055f, 0.1f),
                    Color.Lerp(new Color(95, 205, 255), Color.White, Main.rand.NextFloat(0.1f, 0.3f))));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 dustVel = forward.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.2f, 3.4f);
                Dust d = Dust.NewDustPerfect(
                    tip + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    dustVel, 100,
                    new Color(120, 222, 255),
                    Main.rand.NextFloat(0.72f, 1.02f));
                d.noGravity = true;
            }
        }

        private void SpawnPrepareTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = lockedDirection.SafeNormalize(Vector2.UnitX);
            float chargeProgress = Utils.GetLerpValue(0f, BB_Balance.RightClickPrepareFrames, stateTimer, true);

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                -forward * Main.rand.NextFloat(0.5f, 1.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                false,
                Main.rand.Next(12, 20),
                MathHelper.Lerp(0.05f, 0.13f, chargeProgress),
                Color.Lerp(new Color(80, 185, 255), new Color(200, 248, 255), chargeProgress)));

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
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(lockedDirection);

            for (int i = 0; i < 6; i++)
            {
                Vector2 mistVelocity = forward.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(1.5f, 6.2f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    center + Main.rand.NextVector2Circular(16f, 16f),
                    mistVelocity,
                    Color.Lerp(new Color(60, 185, 255), Color.White, Main.rand.NextFloat(0.1f, 0.45f)) * 0.85f,
                    new Color(40, 140, 220) * 0.4f,
                    Main.rand.NextFloat(1.1f, 1.8f),
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    Main.rand.Next(20, 32)));
            }

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

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                new Color(55, 195, 255) with { A = 0 },
                new Vector2(1.1f, 1.1f),
                0f, 0.28f, 0.04f, 30));

            // One-shot WaterFoamParticle burst: 8 -> 4 particles.
            for (int i = 0; i < 4; i++)
            {
                Vector2 foamVel = forward.RotatedByRandom(1.2f) * Main.rand.NextFloat(2f, 6.5f);
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    center + Main.rand.NextVector2Circular(16f, 16f),
                    foamVel,
                    Main.rand.Next(18, 34),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Color.Lerp(new Color(140, 225, 255), Color.White, Main.rand.NextFloat(0.15f, 0.42f))));
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 dustVel = (i % 2 == 0
                    ? forward.RotatedByRandom(MathHelper.Pi)
                    : Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 18f)) * Main.rand.NextFloat(3f, 9.5f);
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    dustVel, 100,
                    new Color(110, 215, 255),
                    Main.rand.NextFloat(0.78f, 1.22f));
                d.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
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

            // One-shot WaterFoamParticle burst: 6 -> 3 particles.
            for (int i = 0; i < 3; i++)
            {
                Vector2 foamVelocity = -forward * Main.rand.NextFloat(0.35f, 1.2f) + Main.rand.NextVector2Circular(1.15f, 1.15f);
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(15f, 15f),
                    foamVelocity,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.38f, 0.65f),
                    Color.Lerp(new Color(90, 200, 255), Color.White, Main.rand.NextFloat(0.08f, 0.32f))));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 orbDir = -forward.RotatedByRandom(0.9f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    orbDir * Main.rand.NextFloat(0.7f, 2.1f),
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.07f, 0.16f),
                    Color.Lerp(new Color(75, 195, 255), new Color(195, 245, 255), Main.rand.NextFloat())));
            }

            for (int i = 0; i < 7; i++)
            {
                Vector2 dustVel = -forward.RotatedByRandom(1.1f) * Main.rand.NextFloat(1.2f, 3.6f);
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

        private void DrawPierceShaderTrail()
        {
            if (dashState != 1 || Projectile.oldPos == null || Projectile.oldPos.Length == 0)
                return;

            try
            {
                Main.spriteBatch.EnterShaderRegion();

                float progress = stateTimer / (float)BB_Balance.RightClickDashFrames;
                Color mainColor = Color.Lerp(new Color(60, 200, 255), Color.Cyan, (float)Math.Sin(oceanPhase * 0.5f) * 0.5f + 0.5f);
                Color secondaryColor = Color.Lerp(new Color(180, 245, 255), Color.White, (float)Math.Cos(oceanPhase * 0.5f) * 0.5f + 0.5f);

                mainColor = Color.Lerp(mainColor, Color.White, 0.35f);
                secondaryColor = Color.Lerp(secondaryColor, Color.White, 0.35f);

                Vector2 forward = Projectile.velocity.SafeNormalize(lockedDirection).SafeNormalize(Vector2.UnitX);
                Vector2 trailOffset = forward * 48f + Projectile.Size * 0.5f;

                GameShaders.Misc["CalamityMod:ExobladePierce"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/EternityStreak"));
                GameShaders.Misc["CalamityMod:ExobladePierce"].UseImage2("Images/Extra_189");
                GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(mainColor);
                GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(secondaryColor);
                GameShaders.Misc["CalamityMod:ExobladePierce"].Apply();

                int numPointsRendered = 25;
                int numPointsProvided = 50;
                Vector2[] positionsToUse = Projectile.oldPos.Where(p => p != Vector2.Zero).Take(numPointsProvided).ToArray();
                if (positionsToUse.Length < 2)
                    return;

                PrimitiveRenderer.RenderTrail(positionsToUse, new PrimitiveSettings(
                    (completionRatio, vertexPos) =>
                    {
                        float width = Utils.GetLerpValue(0f, 0.25f, completionRatio, true) * Projectile.scale * 46f;
                        width *= (1f - (float)Math.Pow(progress, 4));
                        return width;
                    },
                    (completionRatio, vertexPos) => Color.Cyan * (1f - completionRatio) * Projectile.Opacity,
                    (_, _) => trailOffset,
                    shader: GameShaders.Misc["CalamityMod:ExobladePierce"]), numPointsRendered);

                Main.spriteBatch.ExitShaderRegion();
            }
            catch
            {
                // Fallback gracefully if shader region is inactive or fails
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawPierceShaderTrail();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowBlade = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowBlade").Value;
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaronGoest").Value;
            Texture2D smearTex = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D flareTex = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 glowOrigin = new(glowBlade.Width * 0.5f, glowBlade.Height);
            Vector2 forward = Projectile.velocity.SafeNormalize(lockedDirection).SafeNormalize(Vector2.UnitX);
            Vector2 screenOffset = new(0f, Projectile.gfxOffY);
            Vector2 drawCenter = Projectile.Center + screenOffset - Main.screenPosition;
            Vector2 glowAnchor = BrinyBaron_SkillDashTornado_FlightEffects.GetFrontAnchor(Projectile, lockedDirection) + screenOffset - Main.screenPosition;
            float glowProgress = dashState == 2
                ? Utils.GetLerpValue(0f, BB_Balance.RightClickReboundFrames, stateTimer, true)
                : Utils.GetLerpValue(0f, BB_Balance.RightClickDashFrames, stateTimer, true);
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

            // 1. Lucrecia-style blade head smear flare
            if (dashState == 1)
            {
                Main.EntitySpriteDraw(
                    smearTex,
                    glowAnchor - forward * 12f,
                    null,
                    Color.DeepSkyBlue with { A = 0 } * 0.65f * glowStrength,
                    forward.ToRotation() + MathHelper.PiOver2,
                    smearTex.Size() * 0.5f,
                    new Vector2(0.8f, 1.6f) * Projectile.scale,
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    flareTex,
                    glowAnchor,
                    null,
                    Color.White with { A = 0 } * 0.75f * glowStrength,
                    MathHelper.PiOver2,
                    flareTex.Size() * 0.5f,
                    new Vector2(0.8f, 2.2f) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

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

            // 2. Motion afterimages with ghost texture
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPos = Projectile.oldPos[i];
                if (oldPos == Vector2.Zero)
                    continue;

                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(40, 190, 255, 0), new Color(180, 245, 255, 0), factor) * factor * 0.65f;

                Main.EntitySpriteDraw(
                    ghost,
                    oldPos + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    trailColor,
                    bladeRotation,
                    origin,
                    Projectile.scale * (0.85f + factor * 0.15f),
                    SpriteEffects.None,
                    0
                );
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 3. Draw fullbright main blade body (Lucrecia style)
            Color fullbrightBlade = Color.Lerp(lightColor, Color.White, 0.85f);
            Main.EntitySpriteDraw(
                texture,
                drawCenter,
                null,
                fullbrightBlade,
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
