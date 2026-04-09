using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public abstract class YC_RightWarshipBase : ModProjectile, ILocalizedModType
    {
        private bool positionInitialized;

        protected virtual float PositionLerp => 0.3f;
        protected virtual float ScaleBase => 0.9f;
        protected virtual float ScaleAmplitude => 0.04f;
        protected virtual float LightStrength => 0.4f;
        protected virtual int IdleDustInterval => 10;

        public new string LocalizationCategory => "Projectiles";

        protected Player Owner => Main.player[Projectile.owner];

        public int SlotIndex => (int)Projectile.ai[0];
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public Vector2 CurrentForwardDirection { get; protected set; }

        protected abstract Color AccentColor { get; }
        protected virtual Color OverlayColor => AccentColor * 0.75f;

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

            if (!YC_RightHelper.TryGetHoldout(Projectile.owner, ParentHoldoutIndex, out Projectile holdoutProjectile, out YC_RightHoldOut holdout))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateStaticPosition(holdoutProjectile, holdout);
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
                Projectile.scale * 1.08f,
                SpriteEffects.None,
                0);

            return false;
        }

        protected abstract Vector2 GetLocalOffset();

        protected abstract float GetAngleOffsetDegrees();

        protected abstract void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile);

        protected NPC FindTargetAhead(float range, float coneDegrees, bool requireLineOfSight = true)
        {
            return YC_RightHelper.FindTargetAhead(Projectile, Projectile.Center, CurrentForwardDirection, range, coneDegrees, requireLineOfSight);
        }

        protected bool HasActiveBeam()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                if ((int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.RightDrone)
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
                YC_RightHelper.EmitRightDust(Projectile.Center + direction * 10f, velocity, color, Main.rand.NextFloat(0.8f, 1.1f));
            }
        }

        private void UpdateStaticPosition(Projectile holdoutProjectile, YC_RightHoldOut holdout)
        {
            Vector2 forward = holdout.ForwardDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 local = GetLocalOffset();

            Vector2 desiredCenter = holdoutProjectile.Center + right * local.X + forward * local.Y;
            CurrentForwardDirection = forward.RotatedBy(MathHelper.ToRadians(GetAngleOffsetDegrees()));

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, PositionLerp);
            }

            Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = ScaleBase + ScaleAmplitude * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f + SlotIndex * 0.7f);
        }

        private void EmitIdleFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % IdleDustInterval != 0)
                return;

            YC_RightHelper.EmitRightDust(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                CurrentForwardDirection.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.45f, 1.15f),
                AccentColor,
                Main.rand.NextFloat(0.8f, 1.05f));
        }
    }
}
