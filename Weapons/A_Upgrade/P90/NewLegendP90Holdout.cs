using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal enum P90RangeMode
    {
        Neutral,
        Close,
        Far
    }

    internal sealed class NewLegendP90Holdout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutDistance = 34f;
        private const float CloseRange = 10f * 16f;
        private const float FarRange = 15f * 16f;

        private static readonly SoundStyle FireSound = SoundID.Item11 with { Volume = 0.48f, Pitch = 0.35f, MaxInstances = 8 };
        private int shotTimer;
        private int emptyClickTimer;
        private int muzzleFlashTimer;
        private int useAnimationTimer;
        private float recoilOffset;
        private bool rightHeldLastFrame;
        private P90RangeMode currentMode;

        public new string LocalizationCategory => "Projectiles.P90";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/P90/NewLegendP90";

        private Player Owner => Main.player[Projectile.owner];
        private NewLegendP90Player P90Player => Owner.GetModPlayer<NewLegendP90Player>();
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 33f;

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 28;
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
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed ||
                Owner.HeldItem.type != ModContent.ItemType<NewLegendP90>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft = 2;
            P90Player.SetHoldingP90();

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            UpdateTimers();
            UpdatePose();
            EmitModeParticles();

            if (Main.myPlayer == Projectile.owner)
                HandleInputs();
        }

        private void UpdateTimers()
        {
            if (shotTimer > 0)
                shotTimer--;
            if (emptyClickTimer > 0)
                emptyClickTimer--;
            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;
            if (useAnimationTimer > 0)
                useAnimationTimer--;

            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.28f);
        }

        private void HandleInputs()
        {
            bool valid = NewLegendP90.CanUseWorldInput(Owner);
            bool leftHeld = valid && Main.mouseLeft;
            bool rightHeld = valid && (Main.mouseRight || Owner.Calamity().mouseRight);
            bool rightJustPressed = rightHeld && !rightHeldLastFrame;

            if (rightJustPressed)
                HandleRightClick();

            if (leftHeld && !rightHeld)
                HandleLeftClick();

            rightHeldLastFrame = rightHeld;
        }

        private void HandleLeftClick()
        {
            if (P90Player.IsRolling)
                return;

            if (P90Player.IsReloading)
            {
                useAnimationTimer = 2;
                return;
            }

            if (P90Player.Magazine <= 0)
            {
                TryReloadOrClick();
                return;
            }

            if (shotTimer > 0)
                return;

            if (!FireOnce())
                return;

            shotTimer = 2;
            useAnimationTimer = 4;

            if (P90Player.Magazine <= 0)
                P90Player.TryStartReload(Owner, Owner.HeldItem);
        }

        private void HandleRightClick()
        {
            int rollDirection = ResolveRollDirection();
            if (P90Player.DashCooldownTimer <= 0 && !P90Player.IsReloading)
            {
                P90Player.TryStartRoll(Owner.HeldItem, rollDirection);
                return;
            }

            P90Player.TryThrowShockGrenade(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                AimDirection,
                Projectile.damage,
                Projectile.knockBack);
        }

        private bool FireOnce()
        {
            currentMode = ResolveRangeMode(Owner);
            Vector2 direction = AimDirection;
            float speed = MathHelper.Max(4f, P90Player.LoadedShootSpeed);
            Vector2 velocity = direction.RotatedByRandom(0.026f) * speed;
            int damage = P90Player.GetLoadedShotDamage(Owner, Owner.HeldItem);
            float knockback = P90Player.LoadedKnockback;

            int shotIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 5f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                velocity,
                P90Player.LoadedProjectileType,
                damage,
                knockback,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(shotIndex))
                return false;

            Projectile shot = Main.projectile[shotIndex];
            shot.DamageType = DamageClass.Ranged;
            shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            shot.GetGlobalProjectile<P90ProjectileGlobal>().Configure(
                homing: currentMode == P90RangeMode.Far,
                strongKnockback: currentMode == P90RangeMode.Close);
            shot.netUpdate = true;

            P90Player.ConsumeMagazineShot();
            recoilOffset = 5f;
            muzzleFlashTimer = 7;
            Owner.velocity -= direction * 0.09f;
            SpawnMuzzleBurst(direction);
            SoundEngine.PlaySound(FireSound, GunTipPosition);
            return true;
        }

        private void TryReloadOrClick()
        {
            if (P90Player.TryStartReload(Owner, Owner.HeldItem))
                return;

            if (emptyClickTimer > 0)
                return;

            emptyClickTimer = 18;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.28f, Pitch = -0.35f }, Owner.Center);
        }

        private void UpdatePose()
        {
            currentMode = ResolveRangeMode(Owner);
            Vector2 aim = P90Player.IsReloading
                ? Vector2.UnitX * Owner.direction
                : (NewLegendP90.GetMouseWorld(Owner) - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.velocity = Vector2.Lerp(AimDirection, aim, P90Player.IsReloading ? 0.7f : 0.44f).SafeNormalize(aim);
                Projectile.netUpdate = true;
            }

            aim = AimDirection;
            int dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = dir;
            Projectile.direction = dir;
            Projectile.rotation = P90Player.IsReloading ? (dir == 1 ? 0f : MathHelper.Pi) : aim.ToRotation();

            Projectile.Center = Owner.MountedCenter + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -5f * Owner.gravDir);

            Owner.ChangeDir(dir);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * dir).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            if (useAnimationTimer > 0 || P90Player.IsReloading || P90Player.IsRolling)
            {
                Owner.itemTime = Math.Max(Owner.itemTime, 2);
                Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            }

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            if (P90Player.IsReloading)
            {
                float wave = MathF.Sin(P90Player.ReloadCompletion * MathHelper.TwoPi * 2f) * 0.18f;
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + wave * dir);
                Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation - 0.28f * dir - wave * dir);
            }
            else
            {
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
                Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation + MathHelper.ToRadians(7f) * dir);
            }
        }

        private int ResolveRollDirection()
        {
            if (Owner.controlLeft && !Owner.controlRight)
                return -1;
            if (Owner.controlRight && !Owner.controlLeft)
                return 1;

            return Owner.direction == 0 ? 1 : Owner.direction;
        }

        internal static P90RangeMode ResolveRangeMode(Player player)
        {
            float nearest = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(player.Center, npc.Center);
                if (distance < nearest)
                    nearest = distance;
            }

            if (nearest <= CloseRange)
                return P90RangeMode.Close;
            if (nearest > FarRange)
                return P90RangeMode.Far;

            return P90RangeMode.Neutral;
        }

        private void EmitModeParticles()
        {
            Color lightColor = currentMode == P90RangeMode.Close
                ? new Color(255, 58, 58)
                : currentMode == P90RangeMode.Far
                    ? new Color(60, 255, 126)
                    : Color.Gold;

            if (currentMode != P90RangeMode.Neutral)
                Lighting.AddLight(GunTipPosition, lightColor.ToVector3() * 0.35f);

            if (currentMode == P90RangeMode.Neutral || Main.dedServ || !Main.rand.NextBool(currentMode == P90RangeMode.Close ? 2 : 3))
                return;

            Dust dust = Dust.NewDustPerfect(
                GunTipPosition + Main.rand.NextVector2Circular(5f, 5f),
                currentMode == P90RangeMode.Close ? DustID.RedTorch : DustID.GreenTorch,
                -AimDirection.RotatedByRandom(0.34f) * Main.rand.NextFloat(0.4f, 1.8f),
                90,
                lightColor,
                Main.rand.NextFloat(0.6f, 1.05f));
            dust.noGravity = true;
        }

        private void SpawnMuzzleBurst(Vector2 direction)
        {
            Color modeColor = currentMode == P90RangeMode.Close
                ? new Color(255, 58, 58)
                : currentMode == P90RangeMode.Far
                    ? new Color(60, 255, 126)
                    : Color.Gold;

            for (int i = 0; i < 4; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(2.5f, 2.5f),
                    Main.rand.NextBool() ? DustID.GoldFlame : DustID.Torch,
                    direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(1.2f, 4.2f),
                    80,
                    modeColor,
                    Main.rand.NextFloat(0.7f, 1.15f));
                spark.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    GunTipPosition - direction * Main.rand.NextFloat(4f, 12f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.4f, 1.8f),
                    150,
                    Color.Gray,
                    Main.rand.NextFloat(0.45f, 0.8f));
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            DrawModeGlow(texture, drawPosition, origin, effects);
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0f);
            DrawMuzzleGlow();
            return false;
        }

        private void DrawModeGlow(Texture2D texture, Vector2 drawPosition, Vector2 origin, SpriteEffects effects)
        {
            float power = currentMode == P90RangeMode.Neutral ? muzzleFlashTimer / 8f : 0.3f + muzzleFlashTimer / 9f;
            if (power <= 0.02f)
                return;

            Color color = currentMode == P90RangeMode.Close
                ? new Color(255, 58, 58, 0)
                : currentMode == P90RangeMode.Far
                    ? new Color(60, 255, 126, 0)
                    : new Color(255, 210, 80, 0);

            int draws = currentMode == P90RangeMode.Neutral ? 4 : 8;
            float radius = currentMode == P90RangeMode.Neutral ? 1.8f : 3.2f + power * 2.4f;
            for (int i = 0; i < draws; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / draws + Main.GlobalTimeWrappedHourly * 2f).ToRotationVector2() * radius;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, color * (0.12f + power * 0.28f), Projectile.rotation, origin, Projectile.scale, effects, 0f);
            }
        }

        private void DrawMuzzleGlow()
        {
            float flash = muzzleFlashTimer / 8f;
            float modePower = currentMode == P90RangeMode.Neutral ? 0f : 0.45f;
            float power = MathHelper.Clamp(Math.Max(flash, modePower), 0f, 1f);
            if (power <= 0.02f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = currentMode == P90RangeMode.Close
                ? new Color(255, 58, 58, 0)
                : currentMode == P90RangeMode.Far
                    ? new Color(60, 255, 126, 0)
                    : new Color(255, 218, 90, 0);

            Main.EntitySpriteDraw(
                bloom,
                GunTipPosition - Main.screenPosition,
                null,
                color * power * 0.55f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.18f + power * 0.18f, 0.08f + power * 0.12f),
                SpriteEffects.None,
                0f);
        }
    }
}
