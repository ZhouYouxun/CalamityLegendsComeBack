using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public abstract class YC_LeftWarshipBase : ModProjectile, ILocalizedModType
    {
        private bool positionInitialized;

        protected virtual float PositionLerp => 0.22f;
        protected virtual float ScaleBase => 0.9f;
        protected virtual float ScaleAmplitude => 0.05f;
        protected virtual float LightStrength => 0.35f;
        protected virtual float IdleDustScale => 0.95f;
        protected virtual int IdleDustInterval => 9;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        protected Player Owner => Main.player[Projectile.owner];
        protected Vector2 CurrentLocalOffset { get; private set; }
        protected bool ManualAimActive { get; private set; }

        public int SlotIndex => (int)Projectile.ai[0];
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public Vector2 ForwardDirection { get; protected set; }
        public Vector2 DesiredAimDirection { get; protected set; }

        protected abstract Color AccentColor { get; }
        protected virtual Color OverlayColor => AccentColor * 0.72f;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = false;
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

            if (!YC_LeftSquadronHelper.TryGetHoldout(Projectile.owner, ParentHoldoutIndex, out Projectile holdoutProjectile, out YC_LeftHoldOut holdout))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            ForwardDirection = holdout.ForwardDirection;
            ManualAimActive = holdout.ManualAimMode;

            UpdateFormation(holdoutProjectile);
            DesiredAimDirection = CalculateDesiredAimDirection(holdout, holdoutProjectile).SafeNormalize(ForwardDirection);
            Projectile.rotation = DesiredAimDirection.ToRotation() + MathHelper.PiOver2;

            UpdateAttack(holdout, holdoutProjectile);
            EmitIdleFX();
            Lighting.AddLight(Projectile.Center, AccentColor.ToVector3() * LightStrength);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

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

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                OverlayColor,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.06f,
                SpriteEffects.None,
                0);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            KillPersistentBeam();
        }

        protected abstract Vector2 CalculateLocalOffset(float globalTime);

        protected abstract Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile);

        protected abstract void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile);

        protected Vector2 GetManualAimOrDefault(YC_LeftHoldOut holdout, Vector2 fallbackDirection)
        {
            if (holdout.ManualAimMode && Projectile.owner == Main.myPlayer)
                return (Main.MouseWorld - Projectile.Center).SafeNormalize(fallbackDirection);

            return fallbackDirection;
        }

        protected void EnsurePersistentBeam(
            int beamDamage,
            float beamWidth,
            float beamLength,
            Color outerColor,
            Color innerColor,
            float forwardOffset,
            float turnRateRadians,
            int hitCooldown,
            int damageDelayFrames)
        {
            if (Projectile.owner != Main.myPlayer || HasPersistentBeam())
                return;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                DesiredAimDirection,
                beamDamage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.LeftDrone,
                beamLength,
                beamWidth,
                0,
                true,
                false,
                outerColor,
                innerColor,
                forwardOffset,
                turnRateRadians,
                -1,
                hitCooldown,
                damageDelayFrames);
        }

        protected void KillPersistentBeam()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                if ((int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.LeftDrone)
                {
                    other.Kill();
                }
            }
        }

        protected bool HasPersistentBeam()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                if ((int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.LeftDrone)
                {
                    return true;
                }
            }

            return false;
        }

        protected void EmitMuzzleBurst(Vector2 direction, Color color, float speed, int dustCount)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(speed * 0.55f, speed);
                YC_LeftSquadronHelper.EmitTechDust(Projectile.Center + direction * 10f, velocity, color, Main.rand.NextFloat(0.85f, 1.2f));
            }
        }

        protected float GetManualAimBeamLength(float defaultLength, float forwardOffset)
        {
            if (!ManualAimActive || Projectile.owner != Main.myPlayer)
                return defaultLength;

            Vector2 beamStart = Projectile.Center + DesiredAimDirection * forwardOffset;
            float beamLength = Vector2.Dot(Main.MouseWorld - beamStart, DesiredAimDirection);
            return MathHelper.Clamp(beamLength, 6f, defaultLength);
        }

        private void UpdateFormation(Projectile holdoutProjectile)
        {
            CurrentLocalOffset = CalculateLocalOffset(Main.GlobalTimeWrappedHourly);

            Vector2 rightDirection = ForwardDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 desiredCenter = holdoutProjectile.Center + rightDirection * CurrentLocalOffset.X + ForwardDirection * CurrentLocalOffset.Y;

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, PositionLerp);
            }

            Projectile.velocity = (desiredCenter - holdoutProjectile.Center).SafeNormalize(ForwardDirection);
            Projectile.scale = ScaleBase + ScaleAmplitude * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex * 0.7f);
        }

        private void EmitIdleFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % IdleDustInterval != 0)
                return;

            YC_LeftSquadronHelper.EmitTechDust(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DesiredAimDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.45f, 1.1f),
                AccentColor,
                Main.rand.NextFloat(0.75f, IdleDustScale));
        }
    }
}
