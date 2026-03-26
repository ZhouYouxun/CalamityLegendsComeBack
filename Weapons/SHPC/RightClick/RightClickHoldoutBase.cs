using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal abstract class RightClickHoldoutBase : ModProjectile
    {
        // ===== 必须提供 =====
        public abstract int AssociatedItemID { get; }

        public virtual Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * Projectile.width * 0.5f;

        public virtual float WeaponTurnSpeed => 0.2f;
        public virtual float RecoilResolveSpeed => 0.3f;
        public virtual float MaxOffsetLengthFromArm { get; }

        public virtual float OffsetXUpwards { get; }
        public virtual float OffsetXDownwards { get; }
        public virtual float BaseOffsetY { get; }
        public virtual float OffsetYUpwards { get; }
        public virtual float OffsetYDownwards { get; }

        // ===== 运行时数据 =====
        public Player Owner { get; private set; }
        public Item HeldItem { get; private set; }

        public float OffsetLengthFromArm { get; set; }

        public float ExtraFrontArmRotation { get; set; }
        public float ExtraBackArmRotation { get; set; }

        public Player.CompositeArmStretchAmount FrontArmStretch { get; set; } = Player.CompositeArmStretchAmount.Full;
        public Player.CompositeArmStretchAmount BackArmStretch { get; set; } = Player.CompositeArmStretchAmount.Full;

        private Asset<Texture2D> ItemTexture => TextureAssets.Item[AssociatedItemID];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = ItemTexture is null ? 1 : ItemTexture.Width();
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            OffsetLengthFromArm = MaxOffsetLengthFromArm;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Owner ??= Main.player[Projectile.owner];
            HeldItem ??= Owner.HeldItem;

            KillHoldoutLogic();
            ManageHoldout();
            HoldoutAI();
        }

        // ===== 改成右键控制 =====
        public virtual void KillHoldoutLogic()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Owner.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            if (!Main.mouseRight)
            {
                Projectile.Kill();
                return;
            }
        }

        public virtual void ManageHoldout()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            float holdoutDirection = Projectile.velocity.ToRotation();

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 ownerToMouse = Main.MouseWorld - armPosition;
                float proximityLookingUpwards = Vector2.Dot(ownerToMouse.SafeNormalize(Vector2.Zero), -Vector2.UnitY * Owner.gravDir);
                int direction = MathF.Sign(ownerToMouse.X);

                Vector2 lengthOffset = Projectile.rotation.ToRotationVector2() * OffsetLengthFromArm;
                Vector2 armOffset = new Vector2(
                    Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f,
                    proximityLookingUpwards > 0f ? OffsetXUpwards : OffsetXDownwards) * direction,
                    BaseOffsetY * Owner.gravDir +
                    Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f,
                    proximityLookingUpwards > 0f ? OffsetYUpwards : OffsetYDownwards) * Owner.gravDir
                );

                Projectile.Center = armPosition + lengthOffset + armOffset;
                Projectile.velocity = holdoutDirection.AngleTowards(ownerToMouse.ToRotation(), WeaponTurnSpeed).ToRotationVector2();
                Projectile.rotation = holdoutDirection;

                Projectile.spriteDirection = direction;
                Owner.ChangeDir(direction);
            }
            else
            {
                Vector2 lengthOffset = Projectile.rotation.ToRotationVector2() * OffsetLengthFromArm;
                float proximityLookingUpwards = Vector2.Dot(Projectile.velocity.SafeNormalize(Vector2.Zero), -Vector2.UnitY * Owner.gravDir);
                int direction = Projectile.spriteDirection;

                Vector2 armOffset = new Vector2(
                    Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f,
                    proximityLookingUpwards > 0f ? OffsetXUpwards : OffsetXDownwards) * direction,
                    BaseOffsetY * Owner.gravDir +
                    Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f,
                    proximityLookingUpwards > 0f ? OffsetYUpwards : OffsetYDownwards) * Owner.gravDir
                );

                Projectile.Center = armPosition + lengthOffset + armOffset;
                Projectile.velocity = Projectile.rotation.ToRotationVector2();
                Owner.ChangeDir(direction);
            }

            int currentDirection = Projectile.spriteDirection;

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir +
                                (Owner.gravDir == -1 ? MathHelper.Pi : 0f);

            Owner.SetCompositeArmFront(true, FrontArmStretch, armRotation + ExtraFrontArmRotation * currentDirection);
            Owner.SetCompositeArmBack(true, BackArmStretch, armRotation + ExtraBackArmRotation * currentDirection);

            Projectile.timeLeft = 2;

            if (OffsetLengthFromArm != MaxOffsetLengthFromArm)
                OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, MaxOffsetLengthFromArm, RecoilResolveSpeed);

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        // ===== 子类写这里 =====
        public abstract void HoldoutAI();

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = texture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}