using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_Left_Drone : ModProjectile, ILocalizedModType
    {
        private const float OrbitSpeed = 1.45f;
        private const float OrbitFrontBackRadius = 42f;
        private const float OrbitSideRadius = 82f;
        private const float PositionLerp = 0.3f;

        private bool positionInitialized;
        private bool laserSpawned;
        private float sideFactor;
        private Vector2 desiredAimDirection;
        private Vector2 forwardDirection;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_Drone";

        public int SlotIndex => (int)Projectile.ai[0];
        public int ColorIndex => (int)Projectile.ai[1];
        public Vector2 DesiredAimDirection => desiredAimDirection;
        public Vector2 ForwardDirection => forwardDirection;
        public Color DroneColor => GetDroneColor(ColorIndex);

        public static readonly Color[] DroneColors =
        {
            new(255, 105, 105),
            new(255, 162, 90),
            new(255, 226, 112),
            new(130, 235, 130),
            new(110, 196, 255),
            new(132, 146, 255),
            new(210, 128, 255)
        };

        public static Color GetDroneColor(int index) => DroneColors[Utils.Clamp(index, 0, DroneColors.Length - 1)];

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

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!TryGetLeftHoldout(out Projectile holdoutProjectile, out YC_LeftHoldOut holdout))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateOrbit(owner, holdoutProjectile);
            UpdateDesiredAim(owner, holdout);
            Projectile.rotation = desiredAimDirection.ToRotation() + MathHelper.PiOver2;
            EnsureLaserExists();
            EmitOrbitFX();

            Lighting.AddLight(Projectile.Center, DroneColor.ToVector3() * 0.45f);
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedLaser();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            bool beamActive = HasActiveBeam();

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

            if (beamActive)
            {
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    DroneColor * 0.72f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 1.12f,
                    SpriteEffects.None,
                    0);
            }

            return false;
        }

        private void UpdateOrbit(Player owner, Projectile holdoutProjectile)
        {
            forwardDirection = holdoutProjectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Vector2 rightDirection = forwardDirection.RotatedBy(MathHelper.PiOver2);

            float orbitPhase = Main.GlobalTimeWrappedHourly * OrbitSpeed + SlotIndex * MathHelper.TwoPi / YC_LeftHoldOut.LeftDroneCount;
            float horizontal = (float)System.Math.Cos(orbitPhase);
            float vertical = (float)System.Math.Sin(orbitPhase);
            sideFactor = horizontal;

            Vector2 desiredCenter =
                holdoutProjectile.Center +
                rightDirection * horizontal * OrbitSideRadius +
                forwardDirection * vertical * OrbitFrontBackRadius;

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, PositionLerp);
            }

            Projectile.scale = 0.9f + 0.06f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5.5f + SlotIndex * 0.7f);
            Projectile.velocity = (desiredCenter - holdoutProjectile.Center).SafeNormalize(forwardDirection);
        }

        private void UpdateDesiredAim(Player owner, YC_LeftHoldOut holdout)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            float maxOffsetRadians = MathHelper.ToRadians(YC_LeftHoldOut.MaxLaserOffsetDegrees);

            if (holdout.ManualAimMode && Projectile.owner == Main.myPlayer)
            {
                desiredAimDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(baseForward);
            }
            else
            {
                desiredAimDirection = baseForward.RotatedBy(maxOffsetRadians * sideFactor);
            }

            if (desiredAimDirection == Vector2.Zero || desiredAimDirection.HasNaNs())
                desiredAimDirection = baseForward;
        }

        private void EnsureLaserExists()
        {
            if (laserSpawned || Projectile.owner != Main.myPlayer)
                return;

            laserSpawned = true;
            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                desiredAimDirection,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.LeftDrone,
                1400f,
                10f,
                0,
                true,
                false,
                DroneColor,
                Color.White,
                22f,
                0.025f,
                -1,
                12,
                6);

            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.14f, Pitch = -0.2f + SlotIndex * 0.02f }, Projectile.Center);
        }

        private void KillOwnedLaser()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (other.type == ModContent.ProjectileType<YC_CBeam>() &&
                    (int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.LeftDrone)
                {
                    other.Kill();
                }
            }
        }

        private bool HasActiveBeam()
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

        private bool TryGetLeftHoldout(out Projectile holdoutProjectile, out YC_LeftHoldOut holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_LeftHoldOut>())
                    continue;

                if (other.ModProjectile is YC_LeftHoldOut holdoutMod)
                {
                    holdoutProjectile = other;
                    holdout = holdoutMod;
                    return true;
                }
            }

            return false;
        }

        private void EmitOrbitFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 8 != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                DustID.RainbowTorch,
                desiredAimDirection.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.6f, 1.5f),
                0,
                DroneColor,
                Main.rand.NextFloat(0.85f, 1.1f));
            dust.noGravity = true;
        }
    }
}
