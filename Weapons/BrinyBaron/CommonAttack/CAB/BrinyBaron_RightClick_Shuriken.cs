using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.EXSkill;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    public class BrinyBaron_RightClick_Shuriken : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/TornadoProj";

        private const int BaseSize = 50;
        private const int StickyLifetime = 72;
        private static readonly int[] ShurikenPenetrates = { -1, -1, -1, -1 };
        private static readonly int[] ShurikenStickySliceCounts = { 3, 4, 5, 6 };
        private static readonly bool[] ShurikenDeathSpawnsShpcExplosion = { false, false, false, false };
        private static readonly bool[] ShurikenDeathSpawnsCrossLights = { false, false, false, false };
        private static readonly float[] ShurikenTideHomingRanges = { 900f, 1040f, 1180f, 1320f };
        private static readonly float[] ShurikenRotationSpeeds = { 0.55f, 0.59f, 0.63f, 0.67f };
        private static readonly bool[] ShurikenStickySlashUnlocks = { false, false, true, true };
        private const float TideHomingBlendTargetWeight = 44f;
        private const float TideHomingBlendTotalWeight = 30f;
        private const float TideHomingFinalSpeed = 34f;
        private const float TideHomingFinalSpeedLerp = 0.2f;
        private const int TideHomingDelayFrames = 144;
        private const float TideHomingIdleAcceleration = 1.006f;
        private const float NonEmpoweredShurikenAcceleration = 1.0195f;
        private const float NonEmpoweredShurikenMaxSpeed = 48f;
        private const float StickySlashDamageFactor = 0.42f;
        private const float StickySlashBaseScale = 0.9f;
        private const float StickySlashScalePerTier = 0.08f;
        private static readonly bool StickySlashProjectilesEnabled = false;

        private float SizeScale => Projectile.width / (float)BaseSize;
        private float Radius => Projectile.width * 0.5f;
        private bool ForceHoming => Projectile.ai[0] >= 1f;
        private bool TideEmpowered => Main.player.IndexInRange(Projectile.owner) &&
                                      Main.player[Projectile.owner].active &&
                                      Main.player[Projectile.owner].GetModPlayer<BBEXPlayer>().TideFull;
        private ShurikenProfile shurikenProfile;

        private bool stuckInTarget;
        private int stuckTargetIndex = -1;
        private int stickTimer;
        private int sliceEffectTimer;
        private int slicesPerformed;
        private int soundTimer;
        private int homingTimer;
        private Vector2 stickOffsetFromTarget;
        private bool killedByTile;

        public override void SetDefaults()
        {
            Projectile.width = BaseSize;
            Projectile.height = BaseSize;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(IEntitySource source)
        {
            EnsureSquareHitbox();
            stuckInTarget = false;
            stuckTargetIndex = -1;
            stickTimer = 0;
            sliceEffectTimer = 0;
            slicesPerformed = 0;
            soundTimer = 0;
            homingTimer = 0;
            stickOffsetFromTarget = Vector2.Zero;
            killedByTile = false;
            shurikenProfile = CreateShurikenProfile();
            Projectile.penetrate = shurikenProfile.Penetrate;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            EnsureSquareHitbox();
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 22, 0, 255);
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.32f);

            if (soundTimer > 0)
                soundTimer--;

            if (!stuckInTarget)
            {
                HandleFlightMovement();
                SpawnUnlockedFlightEffects();
            }
            else
            {
                HandleStickyState();
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (stuckInTarget)
                return target.whoAmI == stuckTargetIndex;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);

            Vector2 hitForward = Projectile.velocity.SafeNormalize((target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX));

            BBShuriken_Initial_Effects.SpawnWaterFoamHit(target.Center, hitForward, SizeScale, shurikenProfile.GrowthTier);
            BBShuriken_Initial_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale, shurikenProfile.GrowthTier);

            if (shurikenProfile.GrowthTier >= 2)
                BBShuriken_Fishron_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale);

            if (shurikenProfile.GrowthTier >= 3)
                BBShuriken_BoomerDuke_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale);

            if (!shurikenProfile.CanStick)
            {
                Projectile.Kill();
                return;
            }

            if (!stuckInTarget)
            {
                stuckInTarget = true;
                stuckTargetIndex = target.whoAmI;
                stickOffsetFromTarget = Projectile.Center - target.Center;
                Projectile.tileCollide = false;
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = 90;
                stickTimer = 0;
                sliceEffectTimer = 0;
                slicesPerformed = 0;
                Projectile.direction = Projectile.direction == 0 ? (Main.rand.NextBool() ? 1 : -1) : Projectile.direction;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item39 with
                {
                    Volume = 0.7f,
                    Pitch = Main.rand.NextFloat(-0.1f, 0.15f)
                }, target.Center);
            }
            else
            {
                BBShuriken_Initial_Effects.SpawnStickySliceBurst(Projectile, SizeScale, shurikenProfile.GrowthTier);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            killedByTile = true;
            BBShuriken_Initial_Effects.SpawnTileDisappear(Projectile, oldVelocity, SizeScale);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item107 with
            {
                Volume = killedByTile ? 0.22f : 0.28f,
                Pitch = killedByTile ? 0.32f : 0.15f
            }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTexture = TextureAssets.Projectile[Type].Value;

            if (shurikenProfile.GrowthTier >= 1)
                BBShuriken_Hardmode_Effects.DrawRotatingCopies(Projectile, projectileTexture);

            BBShuriken_Initial_Effects.DrawOutlineAndBody(Projectile, projectileTexture, lightColor);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            if (shurikenProfile.GrowthTier >= 3)
                BBShuriken_BoomerDuke_Effects.DrawBladeDisc(Projectile);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(stuckInTarget);
            writer.Write(stuckTargetIndex);
            writer.Write(stickTimer);
            writer.Write(sliceEffectTimer);
            writer.WriteVector2(stickOffsetFromTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stuckInTarget = reader.ReadBoolean();
            stuckTargetIndex = reader.ReadInt32();
            stickTimer = reader.ReadInt32();
            sliceEffectTimer = reader.ReadInt32();
            stickOffsetFromTarget = reader.ReadVector2();
        }

        private void HandleFlightMovement()
        {
            bool homing = TideEmpowered;
            float homingRange = shurikenProfile.TideHomingRange;
            NPC target = homing ? FindNearestTarget(homingRange) : null;

            if (homing && target != null && target.active)
            {
                homingTimer++;
                if (homingTimer <= TideHomingDelayFrames)
                {
                    EnsureFallbackFlightVelocity();
                    Projectile.velocity *= TideHomingIdleAcceleration;
                    ClampFlightSpeed(shurikenProfile.TideHomingFinalSpeed);
                }
                else
                {
                Vector2 desiredDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float loosen = Utils.GetLerpValue(TideHomingDelayFrames, TideHomingDelayFrames + 36f, homingTimer, true);
                float closeTargetBoost = Utils.GetLerpValue(240f, 42f, Projectile.Distance(target.Center), true);
                float pullStrength = MathHelper.Max(loosen, closeTargetBoost);
                float targetSpeed = MathHelper.Lerp(shurikenProfile.TideHomingTotalWeight, shurikenProfile.TideHomingTargetWeight, pullStrength);
                float inertia = MathHelper.Lerp(7.5f, 1.35f, pullStrength);

                Projectile.velocity = (
                    Projectile.velocity * inertia +
                    desiredDir * targetSpeed
                ) / (inertia + 1f);

                float speed = Projectile.velocity.Length();
                float targetFinalSpeed = MathHelper.Lerp(24f, shurikenProfile.TideHomingFinalSpeed, pullStrength);
                float speedCorrection = MathHelper.Lerp(shurikenProfile.TideHomingFinalSpeedLerp, 0.55f, pullStrength);
                speed = MathHelper.Lerp(speed, targetFinalSpeed, speedCorrection);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
                }
            }
            else
            {
                homingTimer = 0;
                EnsureFallbackFlightVelocity();

                if (homing)
                {
                    Projectile.velocity *= TideHomingIdleAcceleration;
                    ClampFlightSpeed(shurikenProfile.TideHomingFinalSpeed);
                }
                else
                {
                    Projectile.velocity *= shurikenProfile.NonEmpoweredAcceleration;
                    ClampFlightSpeed(shurikenProfile.NonEmpoweredMaxSpeed);
                }
            }

            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

            Projectile.rotation += (Projectile.direction <= 0 ? -1f : 1f) * shurikenProfile.RotationSpeed;
        }

        private void EnsureFallbackFlightVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
                return;

            Projectile.velocity = Vector2.UnitX * (Projectile.direction == 0 ? 8f : 8f * Projectile.direction);
        }

        private void ClampFlightSpeed(float maxSpeed)
        {
            float speedSquared = Projectile.velocity.LengthSquared();
            if (speedSquared <= maxSpeed * maxSpeed)
                return;

            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * maxSpeed;
        }

        private void SpawnUnlockedFlightEffects()
        {
            BBShuriken_Initial_Effects.SpawnFlight(Projectile, SizeScale);

            if (shurikenProfile.GrowthTier >= 2)
                BBShuriken_Fishron_Effects.SpawnFlight(Projectile, SizeScale);

            if (shurikenProfile.GrowthTier >= 3)
                BBShuriken_BoomerDuke_Effects.SpawnFlight(Projectile, SizeScale);
        }

        private void HandleStickyState()
        {
            if (!Main.npc.IndexInRange(stuckTargetIndex))
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[stuckTargetIndex];
            if (!target.active || target.life <= 0 || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = target.Center + stickOffsetFromTarget;
            Projectile.rotation += Projectile.direction <= 0 ? -1.35f : 1.35f;

            stickTimer++;
            sliceEffectTimer++;

            BBShuriken_Initial_Effects.SpawnStickyAmbient(Projectile, target, SizeScale, shurikenProfile.GrowthTier);

            if (sliceEffectTimer >= 8)
            {
                sliceEffectTimer = 0;
                slicesPerformed++;
                BBShuriken_Initial_Effects.SpawnStickySliceBurst(Projectile, SizeScale, shurikenProfile.GrowthTier);
                TrySpawnStickySlash(target);

                if (soundTimer <= 0)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with
                    {
                        Volume = 0.45f,
                        Pitch = Main.rand.NextFloat(0.15f, 0.35f)
                    }, Projectile.Center);

                    soundTimer = 8;
                }

                if (slicesPerformed >= GetMaxStickySlices())
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (stickTimer >= StickyLifetime)
                Projectile.Kill();
        }

        private int GetMaxStickySlices()
        {
            return shurikenProfile.StickySliceCount;
        }

        private void TrySpawnStickySlash(NPC target)
        {
            if (!StickySlashProjectilesEnabled)
                return;

            if (!shurikenProfile.UnlocksStickySlash || Main.myPlayer != Projectile.owner)
                return;

            Vector2 slashDirection = target.velocity.LengthSquared() > 1f
                ? target.velocity.SafeNormalize(Vector2.UnitX)
                : (target.Center - Projectile.Center).SafeNormalize(Projectile.rotation.ToRotationVector2());

            slashDirection = slashDirection.RotatedByRandom(0.6f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center + Main.rand.NextVector2Circular(12f, 12f),
                slashDirection * Main.rand.NextFloat(5.2f, 6.8f),
                ModContent.ProjectileType<BBSwing_Slash>(),
                Math.Max(1, (int)(Projectile.damage * shurikenProfile.StickySlashDamageFactor)),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                shurikenProfile.StickySlashScale,
                Main.rand.NextFloat(-0.3f, 0.3f));
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        private void EnsureSquareHitbox()
        {
            int sideLength = Math.Max(1, Math.Max(Projectile.width, Projectile.height));
            if (Projectile.width == sideLength && Projectile.height == sideLength)
                return;

            Vector2 center = Projectile.Center;
            Projectile.width = sideLength;
            Projectile.height = sideLength;
            Projectile.Center = center;
        }

        private static ShurikenProfile CreateShurikenProfile()
        {
            int tier = GetShurikenGrowthTier();
            return new ShurikenProfile(
                tier,
                ShurikenPenetrates[tier],
                ShurikenStickySliceCounts[tier],
                ShurikenDeathSpawnsShpcExplosion[tier],
                ShurikenDeathSpawnsCrossLights[tier],
                ShurikenTideHomingRanges[tier],
                ShurikenRotationSpeeds[tier],
                ShurikenStickySlashUnlocks[tier],
                TideHomingBlendTargetWeight,
                TideHomingBlendTotalWeight,
                TideHomingFinalSpeed,
                TideHomingFinalSpeedLerp,
                NonEmpoweredShurikenAcceleration,
                NonEmpoweredShurikenMaxSpeed,
                StickySlashDamageFactor,
                StickySlashBaseScale + tier * StickySlashScalePerTier);
        }

        private static int GetShurikenGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 3;
            if (NPC.downedFishron)
                return 2;
            if (Main.hardMode)
                return 1;

            return 0;
        }

        private readonly struct ShurikenProfile
        {
            public readonly int GrowthTier;
            public readonly int Penetrate;
            public readonly int StickySliceCount;
            public readonly bool SpawnsShpcExplosionOnDeath;
            public readonly bool SpawnsCrossLightsOnDeath;
            public readonly float TideHomingRange;
            public readonly float RotationSpeed;
            public readonly bool UnlocksStickySlash;
            public readonly float TideHomingTargetWeight;
            public readonly float TideHomingTotalWeight;
            public readonly float TideHomingFinalSpeed;
            public readonly float TideHomingFinalSpeedLerp;
            public readonly float NonEmpoweredAcceleration;
            public readonly float NonEmpoweredMaxSpeed;
            public readonly float StickySlashDamageFactor;
            public readonly float StickySlashScale;

            public bool CanStick => StickySliceCount > 0;

            public ShurikenProfile(int growthTier, int penetrate, int stickySliceCount, bool spawnsShpcExplosionOnDeath, bool spawnsCrossLightsOnDeath, float tideHomingRange, float rotationSpeed, bool unlocksStickySlash, float tideHomingTargetWeight, float tideHomingTotalWeight, float tideHomingFinalSpeed, float tideHomingFinalSpeedLerp, float nonEmpoweredAcceleration, float nonEmpoweredMaxSpeed, float stickySlashDamageFactor, float stickySlashScale)
            {
                GrowthTier = growthTier;
                Penetrate = penetrate;
                StickySliceCount = stickySliceCount;
                SpawnsShpcExplosionOnDeath = spawnsShpcExplosionOnDeath;
                SpawnsCrossLightsOnDeath = spawnsCrossLightsOnDeath;
                TideHomingRange = tideHomingRange;
                RotationSpeed = rotationSpeed;
                UnlocksStickySlash = unlocksStickySlash;
                TideHomingTargetWeight = tideHomingTargetWeight;
                TideHomingTotalWeight = tideHomingTotalWeight;
                TideHomingFinalSpeed = tideHomingFinalSpeed;
                TideHomingFinalSpeedLerp = tideHomingFinalSpeedLerp;
                NonEmpoweredAcceleration = nonEmpoweredAcceleration;
                NonEmpoweredMaxSpeed = nonEmpoweredMaxSpeed;
                StickySlashDamageFactor = stickySlashDamageFactor;
                StickySlashScale = stickySlashScale;
            }
        }
    }

    internal static class BBShuriken_Initial_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            if (!Main.rand.NextBool(3))
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 10f + projectile.identity * 0.55f;
            float backDistance = projectile.width * Main.rand.NextFloat(0.22f, 0.38f);
            Vector2 spawnPos = projectile.Center - forward * backDistance + right * (float)Math.Sin(phase) * projectile.width * 0.1f;
            Vector2 dustVelocity = -forward * Main.rand.NextFloat(0.9f, 2.2f) + right * (float)Math.Cos(phase) * Main.rand.NextFloat(0.2f, 0.95f);

            Dust water = Dust.NewDustPerfect(
                spawnPos,
                DustID.Water,
                dustVelocity,
                100,
                new Color(70, 180, 255),
                Main.rand.NextFloat(0.8f, 1.1f) * sizeScale);
            water.noGravity = true;
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale, int highestUnlockedStage)
        {
            int hitBurstCount = 8 + highestUnlockedStage * 2;
            Vector2 hitRight = hitForward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < hitBurstCount; i++)
            {
                float t = i / (float)Math.Max(1, hitBurstCount - 1);
                float spread = MathHelper.Lerp(-0.55f, 0.55f, t);
                Vector2 burstVelocity = hitForward.RotatedBy(spread) * Main.rand.NextFloat(3.4f, 7.8f) + hitRight * spread * 2f;

                Dust water = Dust.NewDustPerfect(target.Center, DustID.Water, burstVelocity);
                water.noGravity = true;
                water.color = new Color(70, 180, 255);
                water.scale = Main.rand.NextFloat(0.85f, 1.3f) * sizeScale;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, burstVelocity * 0.72f);
                    frost.noGravity = true;
                    frost.color = new Color(210, 248, 255);
                    frost.scale = Main.rand.NextFloat(0.78f, 1.15f) * sizeScale;
                }
            }
        }

        public static void SpawnWaterFoamHit(Vector2 center, Vector2 hitForward, float sizeScale, int highestUnlockedStage)
        {
            if (Main.dedServ)
                return;

            int baseFoamCount = 6 + highestUnlockedStage * 2;
            int foamCount = Math.Max(1, (int)MathF.Ceiling(baseFoamCount * 0.34f));
            Vector2 hitRight = hitForward.RotatedBy(MathHelper.PiOver2);
            Color foamColor = Color.Lerp(new Color(150, 230, 255), Color.White, 0.46f);

            for (int i = 0; i < foamCount; i++)
            {
                float spread = Main.rand.NextFloat(-1.05f, 1.05f);
                Vector2 velocity =
                    hitForward.RotatedBy(spread) * Main.rand.NextFloat(2.2f, 7.2f + highestUnlockedStage * 0.9f) +
                    hitRight * Main.rand.NextFloatDirection() * 2.45f +
                    Main.rand.NextVector2Circular(1.6f, 1.6f);

                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    center + Main.rand.NextVector2Circular(14f, 14f) * sizeScale,
                    velocity,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.36f, 0.58f) * sizeScale,
                    foamColor));
            }
        }

        public static void SpawnStickyAmbient(Projectile projectile, NPC target, float sizeScale, int highestUnlockedStage)
        {
            int ambientCount = 1 + Math.Min(highestUnlockedStage, 1);
            for (int i = 0; i < ambientCount; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(3.2f, 3.2f) + target.velocity * 0.15f;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, dustVelocity);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.85f, 1.18f) * sizeScale;

                if (Main.rand.NextBool())
                {
                    Dust water = Dust.NewDustPerfect(projectile.Center, DustID.Water, dustVelocity * 0.8f);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(0.92f, 1.22f) * sizeScale;
                }
            }
        }

        public static void SpawnStickySliceBurst(Projectile projectile, float sizeScale, int highestUnlockedStage)
        {
            int burstCount = 4 + highestUnlockedStage;
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 6f);

                Dust gem = Dust.NewDustPerfect(projectile.Center, DustID.GemSapphire, burstVelocity);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(0.9f, 1.25f) * sizeScale;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, burstVelocity * 0.7f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.82f, 1.12f) * sizeScale;
            }
        }

        public static void SpawnDeathBurst(Projectile projectile, float sizeScale)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust water = Dust.NewDustPerfect(projectile.Center, DustID.Water, burstVelocity);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.05f, 1.45f) * sizeScale;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, burstVelocity * 0.85f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.2f) * sizeScale;
            }
        }

        public static void SpawnTileDisappear(Projectile projectile, Vector2 oldVelocity, float sizeScale)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = oldVelocity.SafeNormalize(projectile.rotation.ToRotationVector2());
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 10; i++)
            {
                float side = Main.rand.NextFloatDirection();
                Vector2 velocity =
                    -forward * Main.rand.NextFloat(1.2f, 4.4f) +
                    right * side * Main.rand.NextFloat(0.4f, 2.2f);

                Dust water = Dust.NewDustPerfect(
                    projectile.Center + right * side * Main.rand.NextFloat(2f, 10f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    velocity,
                    0,
                    Color.Lerp(new Color(90, 205, 255), Color.White, Main.rand.NextFloat(0.1f, 0.45f)),
                    Main.rand.NextFloat(0.72f, 1.08f) * sizeScale);
                water.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkVelocity =
                    -forward.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.4f, 4.8f) +
                    right * Main.rand.NextFloatDirection() * 0.8f;

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    sparkVelocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.22f, 0.42f) * sizeScale,
                    Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan));
            }
        }

        public static void DrawOutlineAndBody(Projectile projectile, Texture2D projectileTexture, Color lightColor)
        {
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float drawScale = projectile.width / (float)projectileTexture.Width;
            float outlineRadius = Math.Max(2f, projectile.width * 0.05f);
            Color outlineColor = new Color(90, 210, 255, 0) * 0.32f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * outlineRadius;
                Main.EntitySpriteDraw(projectileTexture, drawPos + offset, null, outlineColor, projectile.rotation, origin, drawScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(projectileTexture, drawPos, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, drawScale, SpriteEffects.None, 0);
        }
    }

    internal static class BBShuriken_Hardmode_Effects
    {
        public static void DrawRotatingCopies(Projectile projectile, Texture2D projectileTexture)
        {
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float drawScale = projectile.width / (float)projectileTexture.Width;
            float orbitRadius = projectile.width * 0.34f;
            float spin = Main.GlobalTimeWrappedHourly * 8.5f + projectile.identity * 0.37f;

            for (int i = 0; i < 4; i++)
            {
                float angle = spin + MathHelper.TwoPi * i / 4f;
                Vector2 offset = angle.ToRotationVector2() * orbitRadius;
                Color drawColor = Color.Lerp(new Color(15, 45, 85, 0), new Color(70, 170, 255, 0), 0.68f) * 0.45f;

                Main.EntitySpriteDraw(projectileTexture, drawPos + offset, null, drawColor, projectile.rotation - spin * 0.7f, origin, drawScale * 0.92f, SpriteEffects.None, 0);
            }
        }
    }

    internal static class BBShuriken_Fishron_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 16f + projectile.identity * 0.55f;
            float orbitRadius = projectile.width * 0.36f;
            float helixDepth = projectile.width * 0.12f;

            for (int arm = 0; arm < 4; arm++)
            {
                if (!Main.rand.NextBool(2))
                    continue;

                float armPhase = phase + MathHelper.TwoPi * arm / 4f;
                Vector2 orbitOffset = right * (float)Math.Cos(armPhase) * orbitRadius + forward * (float)Math.Sin(armPhase) * helixDepth;

                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        projectile.Center + orbitOffset,
                        right * (float)-Math.Sin(armPhase) * 0.2f + forward * (float)Math.Cos(armPhase) * 0.1f,
                        false,
                        8,
                        0.48f * sizeScale,
                        arm % 2 == 0 ? new Color(75, 190, 255) : Color.Cyan,
                        true,
                        false,
                        true));

                if (Main.rand.NextBool(3))
                {
                    Dust frost = Dust.NewDustPerfect(projectile.Center + orbitOffset * 0.92f, DustID.Frost, -forward * 0.4f);
                    frost.noGravity = true;
                    frost.color = new Color(220, 250, 255);
                    frost.scale = Main.rand.NextFloat(0.82f, 1.08f) * sizeScale;
                }
            }
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale)
        {
            Vector2 hitRight = hitForward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 4; i++)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    target.Center + Main.rand.NextVector2Circular(projectile.width * 0.18f, projectile.width * 0.18f),
                    hitForward * Main.rand.NextFloat(0.4f, 1.3f) + hitRight * Main.rand.NextFloat(-0.7f, 0.7f),
                    false,
                    8,
                    0.52f * sizeScale,
                    i % 2 == 0 ? Color.DeepSkyBlue : Color.Cyan,
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }

    internal static class BBShuriken_BoomerDuke_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 22f + projectile.identity * 0.63f;
            float sine = (float)Math.Sin(phase);
            Vector2 offset = right * sine * (projectile.width * 0.18f + projectile.width * 0.12f);
            Vector2 sparkVelocity = -forward * MathHelper.Lerp(1.6f, 3f, 0.5f + 0.5f * sine);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(projectile.Center + offset, sparkVelocity, "CalamityMod/Particles/BloomCircle", false, 10, 0.22f * sizeScale, new Color(90, 210, 255), new Vector2(0.5f, 1f), true, false));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(projectile.Center - offset, sparkVelocity, "CalamityMod/Particles/BloomCircle", false, 10, 0.22f * sizeScale, new Color(140, 235, 255), new Vector2(0.5f, 1f), true, false));
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale)
        {
            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        target.Center,
                        hitForward.RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 7f),
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        12,
                        0.28f * sizeScale,
                        i % 2 == 0 ? new Color(95, 210, 255) : Color.Cyan,
                        new Vector2(0.55f, 1.15f),
                        true,
                        false));
            }
        }

        public static void DrawBladeDisc(Projectile projectile)
        {
            Texture2D smearRound = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D smearHalf = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Texture2D projectileTexture = TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float baseDrawScale = projectile.width / (float)projectileTexture.Width;
            float discScale = baseDrawScale * 1.05f;
            float flash = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f + projectile.identity * 0.9f);

            Main.EntitySpriteDraw(smearHalf, drawPos, null, Color.Lerp(Color.DeepSkyBlue, Color.Cyan, flash) with { A = 0 } * 0.42f, projectile.rotation * 1.35f, smearHalf.Size() * 0.5f, discScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(smearRound, drawPos, null, Color.Lerp(Color.LightSeaGreen, Color.CornflowerBlue, flash) with { A = 0 } * 0.42f, projectile.rotation * 0.85f, smearRound.Size() * 0.5f, discScale, SpriteEffects.None, 0);

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * projectile.width * 0.08f;
                Color auraColor = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, i / 6f) * 0.2f;
                Main.EntitySpriteDraw(projectileTexture, drawPos + offset, null, auraColor, projectile.rotation, origin, discScale, SpriteEffects.None, 0);
            }
        }
    }
}
