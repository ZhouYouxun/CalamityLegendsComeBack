using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.Miao
{
    public class MiaoGunHoldout : ModProjectile
    {
        private const float GunScale = 0.55f;
        private const float CenterOffsetFromArm = 36f;
        private const float RecoilCenterOffsetFromArm = 29f;
        private const float GripNormalOffset = 4f;
        private const float GunTipOffset = 57f;
        private const float SpreadRadians = 0.07f;

        private int shootTimer;
        private float centerOffsetFromArm = CenterOffsetFromArm;

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GripNormal => AimDirection.RotatedBy(MathHelper.PiOver2) * GripNormalOffset;
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * GunTipOffset + GripNormal;

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/Miao/MiaoGun";

        public override void SetDefaults()
        {
            Projectile.width = 224;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.scale = GunScale;
            shootTimer = Main.player[Projectile.owner].HeldItem.useTime - 1;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<MiaoGun>() || Owner.noItems || Owner.CCed || !Owner.channel)
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
                UpdateAim(armPosition);
                HandleFiring();
            }

            UpdatePose();
            UpdatePlayerVisuals();
        }

        private void UpdateAim(Vector2 armPosition)
        {
            Vector2 aimTarget = Owner.Calamity().mouseWorld;
            if (aimTarget == Vector2.Zero)
                aimTarget = Main.MouseWorld;

            Vector2 aimDirection = aimTarget - armPosition;
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            Vector2 desiredVelocity = aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 oldVelocity = Projectile.velocity;
            Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.35f);

            if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                Projectile.netUpdate = true;
        }

        private void HandleFiring()
        {
            shootTimer++;
            if (shootTimer < Owner.HeldItem.useTime)
                return;

            shootTimer = 0;

            if (!MiaoGunProjectileCatalog.TryGetRandomProjectileType(out int projectileType))
                return;

            Vector2 shotDirection = AimDirection.RotatedBy(Main.rand.NextFloat(-SpreadRadians, SpreadRadians));
            Vector2 shotVelocity = shotDirection * Owner.HeldItem.shootSpeed;

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                shotVelocity,
                projectileType,
                Projectile.damage,
                Projectile.knockBack,
                Owner.whoAmI);

            if (Main.projectile.IndexInRange(projectileIndex))
            {
                Projectile firedProjectile = Main.projectile[projectileIndex];
                firedProjectile.friendly = true;
                firedProjectile.hostile = false;
                firedProjectile.DamageType = DamageClass.Ranged;
            }

            centerOffsetFromArm = RecoilCenterOffsetFromArm;

            SoundEngine.PlaySound(
                SoundID.Item11 with
                {
                    Volume = 0.5f,
                    Pitch = Main.rand.NextFloat(-0.15f, 0.15f)
                },
                GunTipPosition);
        }

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            centerOffsetFromArm = MathHelper.Lerp(centerOffsetFromArm, CenterOffsetFromArm, 0.22f);
            Projectile.Center = armPosition + AimDirection * centerOffsetFromArm + GripNormal;
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }

        private void UpdatePlayerVisuals()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;

            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation - 0.08f * Projectile.direction);
        }
    }
}
