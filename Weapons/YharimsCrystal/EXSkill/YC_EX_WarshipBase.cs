using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public abstract class YC_EX_WarshipBase : ModProjectile, ILocalizedModType, IYCEXBeamSource
    {
        private bool positionInitialized;
        private int previousVipState = -1;

        protected virtual float PositionLerp => 0.26f;
        protected virtual float ScaleBase => 0.92f;
        protected virtual float ScaleAmplitude => 0.04f;
        protected virtual float FormationAngleOffsetRadians => -MathHelper.PiOver2;
        protected virtual float TargetRange => 1600f;
        protected virtual float LightStrength => 0.45f;
        protected virtual int IdleDustInterval => 10;

        public new string LocalizationCategory => "Projectiles";

        protected Player Owner => Main.player[Projectile.owner];
        public int SlotIndex => (int)Projectile.ai[0];
        public Vector2 CurrentForwardDirection { get; protected set; } = -Vector2.UnitY;

        protected abstract float FormationRadius { get; }
        protected abstract int FormationCount { get; }
        protected abstract Color AccentColor { get; }
        protected virtual Color OutlineColor => AccentColor;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public sealed override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!YC_EXHelper.TryGetActiveVip(Projectile.owner, out _, out YC_EX_VIP vip))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateFormation(vip);
            HandleVipState(vip);
            Lighting.AddLight(Projectile.Center, AccentColor.ToVector3() * LightStrength);
        }

        public override void OnKill(int timeLeft)
        {
            KillAnchoredBeams();
            KillOwnedAttackProjectiles();
            EmitDismissBurst();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i).ToRotationVector2() * 1.2f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    OutlineColor * 0.38f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        protected virtual Vector2 GetIdleFacingDirection(Vector2 radialDirection) => radialDirection;

        protected virtual Vector2 GetFiringAimDirection(Vector2 defaultDirection)
        {
            NPC target = YC_EXHelper.FindNearestTarget(Projectile, Projectile.Center, TargetRange);
            return target != null
                ? (target.Center - Projectile.Center).SafeNormalize(defaultDirection)
                : defaultDirection;
        }

        protected NPC FindTarget(float maxDistance, bool requireLineOfSight = false)
        {
            return YC_EXHelper.FindNearestTarget(Projectile, Projectile.Center, maxDistance, requireLineOfSight);
        }

        protected void EmitMuzzleBurst(Vector2 direction, int dustCount, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < dustCount; i++)
            {
                YC_EXHelper.EmitExDust(
                    Projectile.Center + direction * 10f,
                    direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(speed * 0.55f, speed),
                    Color.Lerp(AccentColor, Color.White, Main.rand.NextFloat(0.2f, 0.55f)),
                    Main.rand.NextFloat(0.85f, 1.15f));
            }
        }

        protected abstract void HandleFiringState(YC_EX_VIP vip, int timer);

        protected virtual void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
        }

        protected virtual void KillOwnedAttackProjectiles()
        {
        }

        protected void KillAnchoredBeams()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                if ((int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.ExDrone)
                {
                    other.Kill();
                }
            }
        }

        private void UpdateFormation(YC_EX_VIP vip)
        {
            float slotAngle = MathHelper.TwoPi * Utils.Clamp(SlotIndex, 0, FormationCount - 1) / FormationCount + FormationAngleOffsetRadians;
            Vector2 radialDirection = slotAngle.ToRotationVector2();
            float radius = FormationRadius;

            if (vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge || vip.CurrentState == YC_EX_VIP.EXVipState.AwaitingFireCommand)
                radius += (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + SlotIndex * 0.65f) * 2.5f;

            Vector2 desiredCenter = Owner.Center + radialDirection * radius;
            Vector2 defaultDirection = GetIdleFacingDirection(radialDirection);
            Vector2 desiredDirection = vip.CurrentState == YC_EX_VIP.EXVipState.Firing
                ? GetFiringAimDirection(defaultDirection)
                : defaultDirection;

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, PositionLerp);
            }

            CurrentForwardDirection = desiredDirection.SafeNormalize(defaultDirection);
            Projectile.velocity = CurrentForwardDirection;
            Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = ScaleBase + ScaleAmplitude * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4f + SlotIndex * 0.75f);
        }

        private void HandleVipState(YC_EX_VIP vip)
        {
            int currentState = (int)vip.CurrentState;
            if (currentState != previousVipState)
            {
                previousVipState = currentState;
                OnStateChanged(vip.CurrentState);

                if (vip.CurrentState == YC_EX_VIP.EXVipState.AwaitingFireCommand)
                    EmitReadyBurst();
                else if (vip.CurrentState == YC_EX_VIP.EXVipState.Firing)
                    EmitFiringBurst();
            }

            switch (vip.CurrentState)
            {
                case YC_EX_VIP.EXVipState.Summoning:
                    EmitIdleFX();
                    break;
                case YC_EX_VIP.EXVipState.DroneCharge:
                    EmitChargeFX(vip.CurrentStateTimer / (float)YC_EX_VIP.DroneChargeTime);
                    break;
                case YC_EX_VIP.EXVipState.AwaitingFireCommand:
                    EmitReadyFX();
                    break;
                case YC_EX_VIP.EXVipState.Firing:
                    HandleFiringState(vip, vip.CurrentStateTimer);
                    EmitBeamAnchorFX(vip.CurrentStateTimer);
                    break;
                case YC_EX_VIP.EXVipState.Cleanup:
                    EmitCleanupFX();
                    break;
            }
        }

        private void EmitIdleFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % IdleDustInterval != 0)
                return;

            YC_EXHelper.EmitExDust(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                CurrentForwardDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.35f, 0.95f),
                AccentColor,
                Main.rand.NextFloat(0.75f, 1f));
        }

        private void EmitChargeFX(float progress)
        {
            if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
                return;

            Vector2 inward = -CurrentForwardDirection.SafeNormalize(-Vector2.UnitY);
            Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(14f + progress * 8f, 14f + progress * 8f);

            GlowOrbParticle glow = new GlowOrbParticle(
                spawnPosition,
                inward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.1f, 2.3f),
                false,
                Main.rand.Next(10, 16),
                MathHelper.Lerp(0.24f, 0.42f, progress),
                Color.Lerp(AccentColor, Color.White, 0.25f + progress * 0.35f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitReadyFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 16 != 0)
                return;

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                CurrentForwardDirection,
                AccentColor * 0.8f,
                new Vector2(1f, 1.55f),
                CurrentForwardDirection.ToRotation(),
                0.08f,
                0.03f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        private void EmitReadyBurst()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.16f, Pitch = -0.24f + SlotIndex * 0.02f }, Projectile.Center);

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                CurrentForwardDirection,
                AccentColor,
                new Vector2(1f, 1.8f),
                CurrentForwardDirection.ToRotation(),
                0.12f,
                0.04f,
                18);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = CurrentForwardDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.8f, 3.4f);
                SquareParticle square = new SquareParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(14, 20),
                    Main.rand.NextFloat(0.8f, 1.05f),
                    Color.Lerp(AccentColor, Color.White, Main.rand.NextFloat(0.2f, 0.55f)));
                GeneralParticleHandler.SpawnParticle(square);
            }
        }

        private void EmitFiringBurst()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = CurrentForwardDirection.RotatedByRandom(0.32f) * Main.rand.NextFloat(2.2f, 4.6f);
                SquishyLightParticle spark = new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    velocity,
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(AccentColor, Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
                    Main.rand.Next(12, 18));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void EmitBeamAnchorFX(int timer)
        {
            if (Main.dedServ || timer % 8 != 0)
                return;

            GlowOrbParticle glow = new GlowOrbParticle(
                Projectile.Center + CurrentForwardDirection * 12f,
                CurrentForwardDirection * Main.rand.NextFloat(0.6f, 1.3f),
                false,
                8,
                0.28f,
                Color.Lerp(AccentColor, Color.White, 0.4f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitCleanupFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 12 != 0)
                return;

            GlowOrbParticle glow = new GlowOrbParticle(
                Projectile.Center,
                Main.rand.NextVector2Circular(0.35f, 0.35f),
                false,
                8,
                0.22f,
                AccentColor * 0.75f,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitDismissBurst()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 5; i++)
            {
                YC_EXHelper.EmitExDust(
                    Projectile.Center,
                    Main.rand.NextVector2Circular(2.1f, 2.1f),
                    Color.Lerp(AccentColor, Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.NextFloat(0.85f, 1.15f));
            }
        }
    }
}
