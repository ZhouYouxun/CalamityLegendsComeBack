using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill.SpecialEffects;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera;
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

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXWeapon : ModProjectile, ILocalizedModType
    {
        private const int ChargeFrames = 180;
        private const int BarrageFrames = 180;
        private const float MinChargeScreenshake = 3f;
        private const float MaxChargeScreenshake = 30f;
        private const float IdleHoldOffset = 22f;
        private const float ChargedHoldOffset = 14f;
        private const int BarrageVolleyCount = 5;
        private const float BarrageSpawnBackDistance = 280f;
        private const float BarrageSpawnDepth = 130f;
        private const float BarrageSpawnHalfHeight = 360f;
        private const float BarrageCenterShotSpeed = 31f;
        private const float BarrageSideShotSpeed = 19.5f;
        private const float BarrageShotSpeedMultiplier = 1.2f;
        private const float BarrageTargetRange = 2200f;
        private const float BarrageTargetHalfWidth = 420f;
        private const float ChargeScreenEdgeMargin = 96f;

        private int timer;
        private int fireSoundTimer;
        private bool leftHeldLastFrame;
        private float holdOffsetFromArm = IdleHoldOffset;
        private float extraFrontArmRotation;
        private float extraBackArmRotation;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";

        private Player Owner => Main.player[Projectile.owner];
        private int State
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Projectile.Center + AimDirection * 44f;
        private Color MainColor => new Color(86, 255, 148);
        private Color AccentColor => new Color(204, 255, 220);

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            CloseSelectionPanel();
            KillNormalHoldout();
            SoundEngine.PlaySound(SoundID.Item164 with { Volume = 0.8f, Pitch = -0.35f }, Projectile.Center);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            UpdateHeldProjectileVariables(State != 2);
            ManipulatePlayerVariables();

            switch (State)
            {
                case 0:
                    ChargePhase();
                    break;

                case 1:
                    ReadyPhase();
                    break;

                default:
                    BarragePhase();
                    break;
            }
        }

        private void ChargePhase()
        {
            timer++;
            float chargeCompletion = Utils.GetLerpValue(0f, ChargeFrames, timer, true);
            holdOffsetFromArm = MathHelper.Lerp(IdleHoldOffset, ChargedHoldOffset, chargeCompletion);
            extraFrontArmRotation = -0.08f * chargeCompletion;
            extraBackArmRotation = 0.05f * chargeCompletion;

            SpawnAbsorbEffects(strong: true);
            SpawnChargeConvergenceEffects(chargeCompletion);
            ApplyChargeScreenshake(chargeCompletion);

            if (timer >= ChargeFrames)
                EnterReadyPhase();
        }

        private void ApplyChargeScreenshake(float chargeCompletion)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            float screenshake = MathHelper.Lerp(MinChargeScreenshake, MaxChargeScreenshake, chargeCompletion);
            Owner.SetScreenshake(screenshake);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, screenshake);
        }

        private void ReadyPhase()
        {
            timer++;
            holdOffsetFromArm = ChargedHoldOffset;
            extraFrontArmRotation = -0.08f;
            extraBackArmRotation = 0.05f;

            SpawnAbsorbEffects(strong: false);

            bool validLeftClick =
                Main.myPlayer == Projectile.owner &&
                Main.mouseLeft &&
                !Owner.mouseInterface &&
                !Main.blockMouse &&
                !Main.mapFullscreen;

            if (validLeftClick && !leftHeldLastFrame)
                EnterBarragePhase();

            leftHeldLastFrame = validLeftClick;
        }

        private void BarragePhase()
        {
            timer++;
            fireSoundTimer++;

            float barrageCompletion = Utils.GetLerpValue(0f, BarrageFrames, timer, true);
            holdOffsetFromArm = MathHelper.Lerp(ChargedHoldOffset - 2f, IdleHoldOffset - 1f, barrageCompletion);
            extraFrontArmRotation = -0.1f;
            extraBackArmRotation = 0.06f;

            SpawnAbsorbEffects(strong: false, subduedFactor: 0.35f);
            SpawnMuzzleEffects();

            if (Main.myPlayer == Projectile.owner && timer % 2 == 0)
                FireBarrageVolley();

            if (fireSoundTimer >= 4)
            {
                fireSoundTimer = 0;
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.58f, Pitch = 0.2f + Main.rand.NextFloat(-0.08f, 0.08f) }, GunTip);
            }

            if (timer >= BarrageFrames)
                Projectile.Kill();
        }

        private void EnterReadyPhase()
        {
            State = 1;
            timer = 0;
            Projectile.netUpdate = true;

            FadeChargeConvergenceProjectiles();
            SpawnChargeBurst(1.1f);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.78f, Pitch = -0.08f }, GunTip);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.42f, Pitch = -0.2f }, GunTip);
        }

        private void EnterBarragePhase()
        {
            State = 2;
            timer = 0;
            fireSoundTimer = 0;
            leftHeldLastFrame = false;
            Projectile.velocity = AimDirection;
            Projectile.netUpdate = true;

            FadeChargeConvergenceProjectiles();
            SpawnChargeBurst(1.35f);
            SpawnReleaseShockwave();

            if (Main.myPlayer == Projectile.owner)
            {
                Owner.SetScreenshake(18f);
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 14f);
                FireBarrageVolley();
            }

            SoundEngine.PlaySound(SoundID.Item163 with { Volume = 1.05f, Pitch = -0.22f }, GunTip);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.88f, Pitch = -0.34f, PitchVariance = 0.08f }, Owner.Center);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.45f }, Owner.Center);
        }

        private void FireBarrageVolley()
        {
            Vector2 forward = GetCurrentMouseAimDirection();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 backfieldCenter = Owner.Center - forward * BarrageSpawnBackDistance + Owner.velocity * 0.35f;
            int shotDamage = (int)(Projectile.damage * 0.72f);
            NPC lockedTarget = FindBarrageTarget(forward);

            for (int i = 0; i < BarrageVolleyCount; i++)
            {
                float laneRatio = BarrageVolleyCount <= 1
                    ? 0f
                    : (i - (BarrageVolleyCount - 1f) * 0.5f) / ((BarrageVolleyCount - 1f) * 0.5f);

                float sideOffset = laneRatio * BarrageSpawnHalfHeight + Main.rand.NextFloat(-28f, 28f);
                float sideCompletion = MathHelper.Clamp(Math.Abs(sideOffset) / BarrageSpawnHalfHeight, 0f, 1f);
                float depthOffset = Main.rand.NextFloat(-BarrageSpawnDepth * 0.5f, BarrageSpawnDepth * 0.5f);
                float shotSpeed = MathHelper.Lerp(BarrageCenterShotSpeed, BarrageSideShotSpeed, MathHelper.SmoothStep(0f, 1f, sideCompletion)) * BarrageShotSpeedMultiplier;
                Vector2 spawnPosition =
                    backfieldCenter +
                    right * sideOffset +
                    forward * depthOffset +
                    Main.rand.NextVector2Circular(10f, 10f);

                Vector2 aimPoint = lockedTarget is not null
                    ? lockedTarget.Center + lockedTarget.velocity * 5f
                    : Owner.Center + forward * 980f + right * sideOffset * 0.08f;

                aimPoint += Main.rand.NextVector2Circular(lockedTarget is not null ? 18f : 32f, lockedTarget is not null ? 18f : 32f);

                Vector2 shotVelocity = (aimPoint - spawnPosition).SafeNormalize(forward) * (shotSpeed + Main.rand.NextFloat(-0.7f, 0.7f));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    shotVelocity,
                    ModContent.ProjectileType<BFEXVernalShot>(),
                    shotDamage,
                    Projectile.knockBack * 0.8f,
                    Projectile.owner,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }
        }

        private Vector2 GetCurrentMouseAimDirection()
        {
            Vector2 aimTarget = Owner.Calamity().mouseWorld;
            if (aimTarget == Vector2.Zero)
                aimTarget = Main.MouseWorld;

            return (aimTarget - Owner.Center).SafeNormalize(AimDirection);
        }

        private NPC FindBarrageTarget(Vector2 forward)
        {
            NPC bestTarget = null;
            float bestScore = float.MaxValue;
            Vector2 origin = Owner.Center;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                Vector2 offset = npc.Center - origin;
                float along = Vector2.Dot(offset, forward);
                if (along <= 0f || along > BarrageTargetRange)
                    continue;

                float lateral = Math.Abs(Vector2.Dot(offset, forward.RotatedBy(MathHelper.PiOver2)));
                float allowedWidth = BarrageTargetHalfWidth + npc.width * 0.5f;
                if (lateral > allowedWidth)
                    continue;

                float score = lateral * 2.8f + along * 0.18f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void SpawnChargeConvergenceEffects(float chargeCompletion)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            if (timer % 2 == 0)
                SpawnChargeConvergenceProjectile(ModContent.ProjectileType<BFEXConvergingLeaf>(), 12.5f, 17.5f);

            if (timer % 3 == 0)
                SpawnChargeConvergenceProjectile(ModContent.ProjectileType<BFEXConvergingWisp>(), 8.5f, 14.5f);

            if (chargeCompletion > 0.55f && timer % 5 == 0)
            {
                int extraType = Main.rand.NextBool()
                    ? ModContent.ProjectileType<BFEXConvergingLeaf>()
                    : ModContent.ProjectileType<BFEXConvergingWisp>();
                SpawnChargeConvergenceProjectile(extraType, 11f, 18f);
            }

            SpawnEdgeGlowSparks(chargeCompletion);
        }

        private void SpawnChargeConvergenceProjectile(int projectileType, float minSpeed, float maxSpeed)
        {
            Vector2 spawnPosition = GetScreenEdgeSpawnPosition(ChargeScreenEdgeMargin);
            Vector2 targetPosition = Owner.Center + Main.rand.NextVector2Circular(54f, 44f);
            Vector2 velocity = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(minSpeed, maxSpeed);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                projectileType,
                0,
                0f,
                Projectile.owner,
                0f,
                Main.rand.NextFloat(MathHelper.TwoPi));
        }

        private void SpawnEdgeGlowSparks(float chargeCompletion)
        {
            if (Main.dedServ)
                return;

            int sparkCount = 2 + (int)(chargeCompletion * 2.4f);
            Color edgeColor = Color.Lerp(MainColor, AccentColor, 0.28f);
            Color whiteColor = Color.Lerp(AccentColor, Color.White, 0.42f);

            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 spawnPosition = GetScreenEdgeSpawnPosition(38f);
                Vector2 targetPosition = Owner.Center + Main.rand.NextVector2Circular(82f, 56f);
                Vector2 direction = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY);
                Vector2 velocity = direction * Main.rand.NextFloat(18f, 28f);
                Color sparkColor = Main.rand.NextBool(3) ? whiteColor : edgeColor;

                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    spawnPosition,
                    velocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.045f, 0.075f),
                    sparkColor,
                    new Vector2(Main.rand.NextFloat(1.75f, 2.45f), Main.rand.NextFloat(0.32f, 0.52f)),
                    true,
                    false));
            }
        }

        private static Vector2 GetScreenEdgeSpawnPosition(float margin)
        {
            float left = Main.screenPosition.X - margin;
            float right = Main.screenPosition.X + Main.screenWidth + margin;
            float top = Main.screenPosition.Y - margin;
            float bottom = Main.screenPosition.Y + Main.screenHeight + margin;

            return Main.rand.Next(4) switch
            {
                0 => new Vector2(left, Main.rand.NextFloat(top, bottom)),
                1 => new Vector2(right, Main.rand.NextFloat(top, bottom)),
                2 => new Vector2(Main.rand.NextFloat(left, right), top),
                _ => new Vector2(Main.rand.NextFloat(left, right), bottom)
            };
        }

        private void FadeChargeConvergenceProjectiles()
        {
            int leafType = ModContent.ProjectileType<BFEXConvergingLeaf>();
            int wispType = ModContent.ProjectileType<BFEXConvergingWisp>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner)
                    continue;

                if (projectile.type != leafType && projectile.type != wispType)
                    continue;

                projectile.ai[2] = 1f;
                projectile.timeLeft = Math.Min(projectile.timeLeft, 28);
                projectile.netUpdate = true;
            }
        }

        private void SpawnReleaseShockwave()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 center = Owner.Center - forward * 42f;
            Color flashColor = Color.Lerp(MainColor, Color.White, 0.48f);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, flashColor, 1.45f, 24));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, flashColor * 0.86f, Vector2.One, 0f, 0f, 0.32f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, MainColor * 0.9f, new Vector2(1.35f, 4.4f), forward.ToRotation(), 0.26f, 0.045f, 24));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, AccentColor * 0.8f, new Vector2(1.1f, 6.2f), forward.ToRotation() + MathHelper.PiOver2, 0.22f, 0.038f, 22));

            for (int i = 0; i < 22; i++)
            {
                Vector2 sparkVelocity =
                    forward.RotatedByRandom(0.32f) * Main.rand.NextFloat(6f, 18f) +
                    right * Main.rand.NextFloat(-2.4f, 2.4f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    sparkVelocity,
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.04f, 0.07f),
                    Main.rand.NextBool(3) ? Color.White : Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.2f, 0.7f)),
                    new Vector2(Main.rand.NextFloat(1.2f, 2f), Main.rand.NextFloat(0.55f, 0.9f)),
                    glowCenter: true,
                    shrinkSpeed: 0.72f,
                    glowOpacity: 0.62f));
            }
        }

        private void SpawnAbsorbEffects(bool strong, float subduedFactor = 1f)
        {
            if (Main.dedServ)
                return;

            float strength = (strong ? 1f : 0.42f) * subduedFactor;
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 center = GunTip;

            int ellipticalCount = strong ? 4 : 2;
            for (int i = 0; i < ellipticalCount; i++)
            {
                float ellipseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float shortAxis = Main.rand.NextFloat(38f, 58f);
                float longAxis = Main.rand.NextFloat(118f, 156f);
                Vector2 spawnPosition =
                    center +
                    forward * ((float)Math.Sin(ellipseAngle) * shortAxis) +
                    right * ((float)Math.Cos(ellipseAngle) * longAxis) +
                    Main.rand.NextVector2Circular(6f, 6f);

                Vector2 toCenter = (center - spawnPosition).SafeNormalize(forward);
                Vector2 swirl = toCenter.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
                Vector2 velocity =
                    toCenter * Main.rand.NextFloat(6f, 13f) * strength +
                    swirl * Main.rand.NextFloat(1.8f, 5.5f) * strength +
                    forward * Main.rand.NextFloat(0.2f, 1.6f);

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    Main.rand.NextFloat(0.3f, 0.65f) * (0.9f + strength * 0.65f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.15f, 0.5f)),
                    Main.rand.Next(16, 28)));

                if (strong && Main.myPlayer == Projectile.owner && Main.rand.NextBool(3))
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        velocity * 0.45f,
                        ModContent.ProjectileType<NewSHPS>(),
                        0,
                        0f,
                        Projectile.owner,
                        4f,
                        Projectile.whoAmI,
                        2f);
                }
            }

            int forwardCount = strong ? 8 : 4;
            for (int i = 0; i < forwardCount; i++)
            {
                Vector2 spawnPosition =
                    center -
                    forward * Main.rand.NextFloat(6f, 34f) +
                    right * Main.rand.NextFloat(-24f, 24f);

                Vector2 velocity = forward * Main.rand.NextFloat(10f, 27f) * MathHelper.Lerp(0.7f, 1f, strength);
                float scale = Main.rand.NextFloat(0.18f, 0.42f) * (0.85f + strength * 0.75f);
                int life = strong ? Main.rand.Next(12, 20) : Main.rand.Next(8, 14);
                Color color = Color.Lerp(MainColor, Color.White, Main.rand.NextFloat(0.18f, 0.52f));

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    scale,
                    color,
                    life,
                    1f,
                    1.55f));
            }

            Lighting.AddLight(center, MainColor.ToVector3() * (0.34f + 0.24f * strength));
        }

        private void SpawnChargeBurst(float scaleFactor)
        {
            if (Main.dedServ)
                return;

            Color flashColor = Color.Lerp(MainColor, Color.White, 0.28f);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(GunTip, Vector2.Zero, flashColor, 0.85f * scaleFactor, 22));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(GunTip, Vector2.Zero, flashColor * 0.75f, Vector2.One, Main.rand.NextFloat(-0.2f, 0.2f), 0f, 0.24f * scaleFactor, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Vector2.Zero, MainColor * 0.8f, new Vector2(0.58f, 5.8f) * scaleFactor, Projectile.rotation, 0.2f, 0.035f, 24));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Vector2.Zero, MainColor * 0.7f, new Vector2(0.52f, 4.4f) * scaleFactor, Projectile.rotation + MathHelper.PiOver2, 0.16f, 0.03f, 22));
        }

        private void SpawnMuzzleEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (timer % 2 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    GunTip + right * Main.rand.NextFloat(-12f, 12f),
                    DustID.GemEmerald,
                    forward * Main.rand.NextFloat(3.5f, 8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    150,
                    Color.Lerp(MainColor, Color.White, 0.2f),
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                GunTip + right * Main.rand.NextFloat(-6f, 6f),
                forward * Main.rand.NextFloat(3f, 6.5f),
                Main.rand.NextFloat(0.2f, 0.36f),
                Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.2f, 0.6f)),
                Main.rand.Next(10, 16)));
        }

        private void UpdateHeldProjectileVariables(bool allowTurning)
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner && allowTurning)
            {
                Vector2 aimTarget = Owner.Calamity().mouseWorld;
                if (aimTarget == Vector2.Zero)
                    aimTarget = Main.MouseWorld;

                Vector2 aimDirection = aimTarget - armPosition;
                if (aimDirection == Vector2.Zero)
                    aimDirection = Vector2.UnitX * Owner.direction;

                Vector2 desiredVelocity = aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.38f);
                if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
            }

            Projectile.Center = armPosition + AimDirection * holdOffsetFromArm;
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }

        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemAnimation = 2;
            Owner.itemTime = 2;

            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + extraFrontArmRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + extraBackArmRotation);
        }

        private void KillNormalHoldout()
        {
            int holdoutType = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == holdoutType)
                    projectile.Kill();
            }
        }

        private void CloseSelectionPanel()
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != selectionPanelType)
                    continue;

                projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 鍩虹璐村浘
            Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // 鍩虹缁樺埗鍙傛暟
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;

            // 澶栨弿杈逛笌鏈綋鍙戝厜鑴夊啿
            float outlinePulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.8f + Projectile.identity * 0.43f);
            float outlineDistance = 1.6f + 1f * outlinePulse;
            Color outlineColor = Color.Lerp(MainColor, AccentColor, 0.34f) * (0.24f + 0.16f * outlinePulse);
            Color glowColor = Color.Lerp(MainColor, Color.White, 0.24f) * (0.1f + 0.08f * outlinePulse);

            // 閲嶅姏缈昏浆鏃朵慨姝ｅ師鐐逛笌缈昏浆鏂瑰悜
            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = weaponTexture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            // 缁樺埗澶栨弿杈?
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, outlineColor, rotation, origin, Projectile.scale, effects, 0);
            }

            // 缁樺埗鏈綋鍙戝厜灞備笌鏈綋
            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, glowColor, rotation, origin, Projectile.scale * (1.015f + 0.025f * outlinePulse), effects, 0);
            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            // 鏋彛 BloomCircle锛氳繖閲屽繀椤讳娇鐢?Additive锛屽惁鍒欓€忔槑杈圭紭瀹规槗鍑虹幇榛戝潡/鑹插潡
            Vector2 gunTipDrawPosition = GunTip - Main.screenPosition;
            float bloomScale = State == 0 ? 0.42f + Utils.GetLerpValue(0f, ChargeFrames, timer, true) * 0.34f : (State == 1 ? 0.78f : 0.96f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloomTexture,
                gunTipDrawPosition,
                null,
                Color.Lerp(MainColor, Color.White, 0.25f) * 0.7f,
                0f,
                bloomTexture.Size() * 0.5f,
                bloomScale,
                SpriteEffects.None,
                0
            );

            float chargeForGlow = State == 0 ? Utils.GetLerpValue(0f, ChargeFrames, timer, true) : 1f;
            float flashWave = 0.88f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f + Projectile.identity * 0.27f);
            for (int i = 0; i < 4; i++)
            {
                float starRotation = AimDirection.ToRotation() + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (1.25f + i * 0.14f);
                Main.EntitySpriteDraw(
                    starTexture,
                    gunTipDrawPosition,
                    null,
                    Color.Lerp(AccentColor, Color.White, 0.52f) * (0.22f + chargeForGlow * 0.38f),
                    starRotation,
                    starTexture.Size() * 0.5f,
                    new Vector2(0.22f + chargeForGlow * 0.18f, 1.2f + chargeForGlow * 1.25f) * flashWave,
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // 杩涘叆鍚庣画鐘舵€佸悗锛岀粯鍒跺墠鏂瑰弻绾挎寚绀?
            if (State >= 1)
            {
                Vector2 forward = AimDirection;
                Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
                float lineLength = State == 1 ? 520f : 620f;
                float spread = State == 1 ? 16f : 10f;
                Color lineColor = Color.Lerp(MainColor, AccentColor, 0.35f) * (State == 1 ? 0.58f : 0.4f);

                //DrawLine(pixel, gunTipDrawPosition + right * spread, gunTipDrawPosition + right * spread + forward * lineLength, lineColor, State == 1 ? 2.4f : 1.5f);
                //DrawLine(pixel, gunTipDrawPosition - right * spread, gunTipDrawPosition - right * spread + forward * lineLength, lineColor, State == 1 ? 2.4f : 1.5f);
            }

            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start,
                null,
                color,
                direction.ToRotation(),
                Vector2.Zero,
                new Vector2(length, width),
                SpriteEffects.None,
                0f);
        }








    }
}

