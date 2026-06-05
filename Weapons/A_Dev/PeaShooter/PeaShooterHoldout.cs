using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterHoldout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutLength = 30f;
        private const float MuzzleLength = 36f;
        private const int MuzzleFlashFrames = 8;

        private readonly BalancePeaShooter balance = new();
        private int autoFireTimer;
        private int muzzleFlashTimer;
        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;
        private float recoilOffset;

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => NewLegendPeaShooter.TextureAssetPath;

        private Player Owner => Main.player[Projectile.owner];
        private PeaShooterPlayer PeaPlayer => Owner.GetModPlayer<PeaShooterPlayer>();
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 MuzzlePosition => Owner.RotatedRelativePoint(Owner.MountedCenter, true) + AimDirection * MuzzleLength;

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<NewLegendPeaShooter>())
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            Owner.HeldItem.noUseGraphic = true;
            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft = 2;

            PeaPlayer.SetHoldingPeaShooter();
            UpdatePose();

            if (Main.myPlayer == Projectile.owner)
                HandleOwnerInput();

            UpdateTimersAndLight();
        }

        private void HandleOwnerInput()
        {
            bool validInput = NewLegendPeaShooter.CanUseWorldInput(Owner);
            bool rightHeld = validInput && (Main.mouseRight || Owner.Calamity().mouseRight);
            bool leftHeld = validInput && Main.mouseLeft && !rightHeld;

            if (rightHeld && !rightHeldLastFrame)
                ToggleMode();

            if (PeaPlayer.AutomaticFire)
                HandleAutomaticFire(leftHeld);
            else
                HandleSemiAutomaticFire(leftHeld);

            leftHeldLastFrame = leftHeld;
            rightHeldLastFrame = rightHeld;
        }

        private void HandleAutomaticFire(bool leftHeld)
        {
            if (!leftHeld)
            {
                autoFireTimer = 0;
                return;
            }

            KeepWeaponUseAnimation();

            if (autoFireTimer > 0)
            {
                autoFireTimer--;
                return;
            }

            FireRandomPea();
            autoFireTimer = BalancePeaShooter.AutoFireInterval - 1;
        }

        private void HandleSemiAutomaticFire(bool leftHeld)
        {
            if (!leftHeld)
                return;

            KeepWeaponUseAnimation();

            if (!leftHeldLastFrame)
                FireRandomPea();
        }

        private void ToggleMode()
        {
            PeaPlayer.ToggleFireMode();
            autoFireTimer = 0;

            string textKey = PeaPlayer.AutomaticFire
                ? "Mods.CalamityLegendsComeBack.PeaShooter.ModeAuto"
                : "Mods.CalamityLegendsComeBack.PeaShooter.ModeSemi";
            Color textColor = PeaPlayer.AutomaticFire ? new Color(122, 255, 138) : new Color(255, 214, 104);
            CombatText.NewText(Owner.Hitbox, textColor, Language.GetTextValue(textKey), dramatic: false, dot: false);
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.65f, Pitch = PeaPlayer.AutomaticFire ? 0.18f : -0.12f }, Owner.Center);
        }

        private void FireRandomPea()
        {
            int stageIndex = balance.GetCompletedStageIndex();
            PeaShooterPeaType peaType = (PeaShooterPeaType)Main.rand.Next((int)PeaShooterPeaType.Rock + 1);
            Vector2 aim = AimDirection;
            Vector2 velocity = aim.RotatedByRandom(0.055f) * balance.GetShootSpeed();
            Vector2 muzzle = MuzzlePosition + Main.rand.NextVector2Circular(1.2f, 1.2f);
            float knockback = Projectile.knockBack * BalancePeaShooter.GetKnockbackMultiplier(peaType);

            int peaIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                velocity,
                ModContent.ProjectileType<PeaShooterPea>(),
                Projectile.damage,
                knockback,
                Projectile.owner,
                (float)peaType,
                stageIndex);

            if (Main.projectile.IndexInRange(peaIndex))
            {
                Projectile pea = Main.projectile[peaIndex];
                pea.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                pea.originalDamage = pea.damage;
                pea.netUpdate = true;
            }

            recoilOffset = MathHelper.Clamp(recoilOffset + (peaType == PeaShooterPeaType.Rock ? 5.4f : 2.5f), 0f, 12f);
            muzzleFlashTimer = MuzzleFlashFrames;
            Owner.velocity -= aim * (peaType == PeaShooterPeaType.Rock ? 0.24f : 0.08f);
            SpawnMuzzleDust(peaType, muzzle, aim);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.32f, Pitch = Main.rand.NextFloat(0.18f, 0.42f), MaxInstances = 8 }, muzzle);
        }

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 desiredAim = (NewLegendPeaShooter.GetMouseWorld(Owner) - armPosition).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Projectile.velocity == Vector2.Zero
                    ? desiredAim
                    : Vector2.Lerp(Projectile.velocity, desiredAim, 0.42f).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int direction = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = direction;
            Projectile.direction = direction;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPosition + aim * (HoldoutLength - recoilOffset) + new Vector2(0f, -4f * Owner.gravDir);

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation - MathHelper.ToRadians(8f) * direction);
        }

        private void UpdateTimersAndLight()
        {
            if (recoilOffset > 0f)
                recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.34f);

            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;

            Lighting.AddLight(MuzzlePosition, new Vector3(0.08f, 0.28f, 0.08f) * (0.65f + muzzleFlashTimer / (float)MuzzleFlashFrames));
        }

        private void KeepWeaponUseAnimation()
        {
            Owner.itemTime = Owner.itemAnimation = 2;
        }

        private static void SpawnMuzzleDust(PeaShooterPeaType peaType, Vector2 muzzle, Vector2 aim)
        {
            Color color = PeaShooterPea.GetPeaColor(peaType);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(3f, 3f),
                    PeaShooterPea.GetDustType(peaType),
                    aim.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.8f, 2.6f),
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.28f)),
                    Main.rand.NextFloat(0.7f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
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

            if (muzzleFlashTimer > 0)
            {
                float flash = muzzleFlashTimer / (float)MuzzleFlashFrames;
                Color glow = (new Color(144, 255, 128) with { A = 0 }) * (0.24f + flash * 0.32f);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1.4f + flash * 2.2f);
                    Main.EntitySpriteDraw(texture, drawPosition + offset, null, glow, Projectile.rotation, origin, Projectile.scale, effects, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
