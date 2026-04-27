using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.Miao
{
    public class MiaoGunHoldout : BaseGunHoldoutProjectile
    {
        private const float SpreadRadians = 0.07f;

        private ref float ShootTimer => ref Projectile.ai[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/Miao/MiaoGun";
        public override int AssociatedItemID => ModContent.ItemType<MiaoGun>();
        public override float MaxOffsetLengthFromArm => 36f;
        public override float OffsetXUpwards => -6f;
        public override float OffsetXDownwards => 4f;
        public override float BaseOffsetY => 4f;
        public override float OffsetYUpwards => -2f;
        public override float OffsetYDownwards => 8f;
        public override float RecoilResolveSpeed => 0.22f;

        private new Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 112;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override void HoldoutAI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != AssociatedItemID || !Owner.channel)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 aimDirection = (Owner.Calamity().mouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = aimDirection;
                Projectile.netUpdate = true;
            }

            ShootTimer++;
            if (ShootTimer < Owner.HeldItem.useTime)
                return;

            ShootTimer = 0f;

            if (!MiaoGunProjectileCatalog.TryGetRandomProjectileType(out int projectileType))
                return;

            Vector2 shotDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(Main.rand.NextFloat(-SpreadRadians, SpreadRadians));
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

                firedProjectile.netUpdate = true;
            }

            OffsetLengthFromArm -= 7f;
            Owner.SetScreenshake(0.75f);

            SoundEngine.PlaySound(
                SoundID.Item11 with
                {
                    Volume = 0.5f,
                    Pitch = Main.rand.NextFloat(-0.15f, 0.15f)
                },
                GunTipPosition);
        }

        public override void KillHoldoutLogic()
        {
            if (!Owner.active || Owner.dead || Owner.CantUseHoldout())
                Projectile.Kill();
        }
    }
}





