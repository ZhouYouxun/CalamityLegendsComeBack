using System;
using CalamityLegendsComeBack.Accssory.BB;
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
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        private const int PrepareTime = 0;
        private const int DashTimeMax = 45;
        private const int ReboundTimeMax = 12;
        private const int DashHistoryLength = 8;
        private const float DashSpeed = 14f;
        private const float ReboundSpeed = 9f;
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
            lockedDirection = GetAimDirection(owner, Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction));

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter + lockedDirection * 18f;
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            dashState = 1;
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
            SpawnChargeReadyBurst();
            StartDash(owner);
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

            BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects(Projectile, lockedDirection);
        }

        private void DoDashPhase(Player owner)
        {
            if (!IsChargeHeld(owner))
            {
                Projectile.Kill();
                return;
            }

            stateTimer++;
            Vector2 aimDirection = GetAimDirection(owner, lockedDirection);
            float turnedRotation = lockedDirection.ToRotation().AngleTowards(aimDirection.ToRotation(), DashTurnRate);
            lockedDirection = turnedRotation.ToRotationVector2();

            Vector2 desiredVelocity = lockedDirection * (DashSpeed * dashSpeedMultiplier);
            Vector2 actualVelocity = ResolveSlidingVelocity(owner, desiredVelocity);

            Projectile.velocity = actualVelocity;
            if (actualVelocity.LengthSquared() <= 0.01f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center += actualVelocity;
            SyncOwnerToProjectile(owner, DashBladeDistance);
            bladeRotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            RecordDashDirection(actualVelocity.SafeNormalize(lockedDirection));
            BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects(Projectile, lockedDirection, bladeRotation, oceanPhase, stateTimer);

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
            SpawnLightningBurst(target.Center, GetReliableDashDirection());

            Player owner = Main.player[Projectile.owner];
            if (owner.GetModPlayer<BBAccessoryPlayer>().ImpactRestarterEquipped)
                owner.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().ClearCooldown();

            if (!enemyReboundUnlocked)
            {
                Projectile.Kill();
                return;
            }

            StartRebound(target.Center);
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

        private BB_Balance.ShortDashProfile ResolveDashGrowthProfile()
        {
            return BB_Balance.GetShortDashProfile();
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

        private void SpawnLightningBurst(Vector2 impactCenter, Vector2 dashDirection)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 baseDirection = dashDirection.SafeNormalize(lockedDirection);
            float boltSpeed = 6.5f;
            int boltDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));

            for (int i = 0; i < 3; i++)
            {
                float laneOffset = MathHelper.Lerp(-0.22f, 0.22f, i / 2f);
                float randomOffset = Main.rand.NextFloat(-0.055f, 0.055f);
                Vector2 boltVelocity = baseDirection.RotatedBy(laneOffset + randomOffset) * boltSpeed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    impactCenter + baseDirection * 14f + baseDirection.RotatedBy(MathHelper.PiOver2) * laneOffset * 28f,
                    boltVelocity,
                    ModContent.ProjectileType<BBASD_Lighting>(),
                    boltDamage,
                    0f,
                    Projectile.owner,
                    0.75f,
                    1f);
            }
        }

        private void SpawnStartBurst()
        {
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
        }

        private void SpawnCanceledChargeBurst()
        {
        }

        private void SpawnPrepareTrail()
        {
        }

        private void SpawnBounceBurst(Vector2 center, Vector2 dashDirection)
        {
        }

        private void SpawnEndBurst()
        {
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
    }
}
