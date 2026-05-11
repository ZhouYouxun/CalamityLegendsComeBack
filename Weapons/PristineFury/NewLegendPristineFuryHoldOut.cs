using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class NewLegendPristineFuryHoldOut : ModProjectile, ILocalizedModType
    {
        private const int HookChargeMaxFrames = 180;
        private const int RightBurstCount = 6;
        private const int RightBurstInterval = 4;
        private const int RightBurstCooldown = 34;
        private const float HoldoutDistance = 34f;

        private int hookChargeTimer;
        private int hookCooldown;
        private bool hookFiredForThisHold;
        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;
        private int rightBurstTimer;
        private int rightBurstShotsLeft;
        private int rightCooldownTimer;
        private int muzzleFlashTimer;
        private int leftEffectResetKey = -1;
        private float recoilOffset;

        internal int LeftTimer;
        internal int LeftChargeTimer;
        internal int LeftAuxTimer;
        internal int LeftBurstIndex;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFuryHoldOut";

        internal Player Owner => Main.player[Projectile.owner];
        internal Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        internal Vector2 GunTipPosition => Projectile.Center + AimDirection * 54f;
        internal PristineFuryMark CurrentMark => Owner.GetModPlayer<PristineFuryPlayer>().CurrentMark;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 46;
            Projectile.friendly = false;
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
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<NewLegendPristineFury>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft = 2;

            UpdateAnimation();
            UpdatePose();
            UpdateTimers();

            if (Main.myPlayer == Projectile.owner)
                HandleInputs();
        }

        private void HandleInputs()
        {
            bool validMouse = !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface && !(Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type);
            bool leftHeld = validMouse && Main.mouseLeft;
            bool rightHeld = validMouse && (Main.mouseRight || Owner.Calamity().mouseRight);
            bool bothHeld = leftHeld && rightHeld;

            ResetLeftStateIfMarkChanged();

            if (bothHeld)
            {
                ResetRightBurst();
                HandleHookCharge();
                leftHeldLastFrame = leftHeld;
                rightHeldLastFrame = rightHeld;
                return;
            }

            DecayHookCharge();

            if (!leftHeld && !rightHeld)
                hookFiredForThisHold = false;

            PristineFuryLeftEffect effect = PristineFuryLeftEffectRegistry.Get(CurrentMark);
            effect.Update(this, leftHeld, leftHeld && !leftHeldLastFrame, !leftHeld && leftHeldLastFrame);

            HandleRightClick(rightHeld, rightHeld && !rightHeldLastFrame);

            leftHeldLastFrame = leftHeld;
            rightHeldLastFrame = rightHeld;
        }

        private void HandleHookCharge()
        {
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();

            if (hookCooldown > 0 || hookFiredForThisHold)
            {
                pristinePlayer.HookChargeOpacity = Math.Max(pristinePlayer.HookChargeOpacity, 0.35f);
                return;
            }

            hookChargeTimer++;
            pristinePlayer.HookChargeFrames = hookChargeTimer;
            pristinePlayer.HookChargeOpacity = MathHelper.Clamp(pristinePlayer.HookChargeOpacity + 0.08f, 0f, 1f);

            SpawnHookChargeEffects(hookChargeTimer / (float)HookChargeMaxFrames);

            if (hookChargeTimer < HookChargeMaxFrames)
                return;

            FireExtractionHook();
            hookChargeTimer = 0;
            hookCooldown = 42;
            hookFiredForThisHold = true;
        }

        private void DecayHookCharge()
        {
            if (hookChargeTimer <= 0)
                return;

            hookChargeTimer = Math.Max(0, hookChargeTimer - 5);
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            pristinePlayer.HookChargeFrames = hookChargeTimer;
            pristinePlayer.HookChargeOpacity = Math.Max(pristinePlayer.HookChargeOpacity, hookChargeTimer / (float)HookChargeMaxFrames);
        }

        private void FireExtractionHook()
        {
            Vector2 direction = (GetMouseWorld() - GunTipPosition).SafeNormalize(AimDirection);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 8f,
                direction * 23f,
                ModContent.ProjectileType<PristineFuryHook>(),
                GetScaledDamage(1.2f),
                Projectile.knockBack,
                Projectile.owner);

            ApplyRecoil(18f);
            TriggerMuzzleFlash(22);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.85f, Pitch = -0.18f }, GunTipPosition);
        }

        private void HandleRightClick(bool rightHeld, bool rightJustPressed)
        {
            if (rightCooldownTimer > 0)
                return;

            if (rightJustPressed && rightBurstShotsLeft <= 0)
            {
                rightBurstShotsLeft = RightBurstCount;
                rightBurstTimer = 0;
            }

            if (rightBurstShotsLeft <= 0)
                return;

            if (rightBurstTimer > 0)
            {
                rightBurstTimer--;
                return;
            }

            FireRightScatter();
            rightBurstShotsLeft--;
            rightBurstTimer = RightBurstInterval;

            if (rightBurstShotsLeft <= 0)
                rightCooldownTimer = RightBurstCooldown;
        }

        private void FireRightScatter()
        {
            Vector2 muzzleDirection = AimDirection;
            Vector2 muzzle = GunTipPosition + muzzleDirection * 8f;
            int pelletCount = Main.rand.Next(4, 7);
            int damage = GetScaledDamage(0.34f);
            float knockBack = Projectile.knockBack * 0.7f;
            float speed = 13.5f;

            if (Owner.PickAmmo(Owner.HeldItem, out _, out float pickedSpeed, out int pickedDamage, out float pickedKnockback, out _, dontConsume: Main.rand.NextBool(2)))
            {
                speed = Math.Max(10f, pickedSpeed);
                damage = Math.Max(1, (int)(pickedDamage * 0.34f));
                knockBack = pickedKnockback;
            }

            for (int i = 0; i < pelletCount; i++)
            {
                Vector2 velocity = muzzleDirection.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-11f, 11f))) * speed * Main.rand.NextFloat(0.86f, 1.18f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<PristineFuryRightPellet>(),
                    damage,
                    knockBack,
                    Projectile.owner);
            }

            ApplyRecoil(4f);
            TriggerMuzzleFlash(12);
            SpawnMuzzleBurst(new Color(255, 118, 57), 0.7f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FlakKrakenShoot") { Volume = 0.5f, Pitch = 0.45f }, muzzle);
        }

        private void ResetRightBurst()
        {
            rightBurstShotsLeft = 0;
            rightBurstTimer = 0;
        }

        private void ResetLeftStateIfMarkChanged()
        {
            int key = (int)CurrentMark;
            if (leftEffectResetKey == key)
                return;

            LeftTimer = 0;
            LeftChargeTimer = 0;
            LeftAuxTimer = 0;
            LeftBurstIndex = 0;
            leftEffectResetKey = key;
        }

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 aim = AimDirection;

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 desiredAim = (GetMouseWorld() - armPosition).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Vector2.Lerp(aim, desiredAim, 0.38f).SafeNormalize(desiredAim);
                aim = AimDirection;
                Projectile.netUpdate = true;
            }

            int direction = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = direction;
            Projectile.direction = direction;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPosition + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir);

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (aim * direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation - MathHelper.ToRadians(10f) * direction);

            if (recoilOffset > 0f)
                recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.22f);
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
        }

        private void UpdateTimers()
        {
            if (hookCooldown > 0)
                hookCooldown--;

            if (rightCooldownTimer > 0)
                rightCooldownTimer--;

            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;
        }

        internal void ApplyRecoil(float amount)
        {
            recoilOffset = Math.Max(recoilOffset, amount);
            Owner.velocity -= AimDirection * amount * 0.018f;
        }

        internal void TriggerMuzzleFlash(int frames = 10)
        {
            muzzleFlashTimer = Math.Max(muzzleFlashTimer, frames);
        }

        internal int GetScaledDamage(float multiplier) => Math.Max(1, (int)(Projectile.damage * multiplier));

        internal Vector2 GetMouseWorld()
        {
            Vector2 mouseWorld = Owner.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        internal NPC FindTarget(float range) => PristineFuryTargeting.FindTarget(GunTipPosition, range, Owner);

        internal void SpawnMuzzleBurst(Color color, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = AimDirection;
            Vector2 muzzle = GunTipPosition + direction * 8f;

            for (int i = 0; i < 9; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(4f, 4f), DustID.Torch, direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(2.6f, 8f), 90, color, Main.rand.NextFloat(0.8f, 1.35f) * scale);
                dust.noGravity = true;
            }

            Particle spark = new CustomSpark(
                muzzle,
                direction * 4f,
                "CalamityMod/Particles/BloomLineSoftEdge",
                false,
                12,
                0.055f * scale,
                Color.Lerp(color, Color.White, 0.42f),
                new Vector2(0.8f, 1.6f),
                glowCenter: true,
                shrinkSpeed: 0.78f);

            GeneralParticleHandler.SpawnParticle(spark);
        }

        private void SpawnHookChargeEffects(float charge)
        {
            if (Main.dedServ)
                return;

            Vector2 center = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.58f);
            Color color = Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, charge * 0.55f);
            Lighting.AddLight(center, color.ToVector3() * (0.25f + charge * 0.55f));

            if (Main.rand.NextFloat() < 0.35f + charge * 0.45f)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(18f + 34f * charge, 18f + 34f * charge);
                Dust dust = Dust.NewDustPerfect(center + offset, DustID.RainbowMk2, -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4.4f), 80, color, Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
            int frameHeight = texture.Height / 4;
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = new(texture.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = SpriteEffects.None;

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = frameHeight - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            float flash = muzzleFlashTimer / 22f;
            Color markColor = PristineFuryMarkHelper.GetColor(CurrentMark);
            if (flash > 0f)
            {
                Color outlineColor = (Color.Lerp(markColor, Color.White, 0.45f) with { A = 0 }) * (0.18f + flash * 0.42f);
                int drawCount = 16;
                float radius = 2.8f + flash * 5.4f;
                for (int i = 0; i < drawCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / drawCount + Main.GlobalTimeWrappedHourly * 2.4f;
                    Vector2 offset = angle.ToRotationVector2() * radius;
                    Main.EntitySpriteDraw(texture, drawPosition + offset, frame, outlineColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(glow, drawPosition, frame, (Color.White with { A = 0 }) * (0.45f + flash), Projectile.rotation, origin, Projectile.scale, effects, 0);
            DrawMuzzleGlow(flash);
            DrawHookChargeBar();
            return false;
        }

        private void DrawMuzzleGlow(float flash)
        {
            float charge = Owner.GetModPlayer<PristineFuryPlayer>().HookChargeOpacity;
            float power = Math.Max(flash, charge * 0.7f);
            if (power <= 0.02f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 muzzle = GunTipPosition + AimDirection * 8f - Main.screenPosition;
            Color color = (Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, 0.48f) with { A = 0 }) * power;

            Main.EntitySpriteDraw(bloom, muzzle, null, color * 0.55f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.34f + power * 0.24f, 0.18f + power * 0.12f), SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (1.1f + i * 0.15f);
                Main.EntitySpriteDraw(star, muzzle, null, color * 0.62f, rotation, star.Size() * 0.5f, new Vector2(0.24f + power * 0.18f, 1.1f + power * 1.4f), SpriteEffects.None, 0);
            }
        }

        private void DrawHookChargeBar()
        {
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            float opacity = pristinePlayer.HookChargeOpacity;
            if (opacity <= 0.02f || Main.dedServ)
                return;

            Texture2D back = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D front = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            float completion = MathHelper.Clamp(pristinePlayer.HookChargeFrames / (float)HookChargeMaxFrames, 0f, 1f);
            Vector2 drawPosition = Owner.Top + new Vector2(0f, -32f * Owner.gravDir) - Main.screenPosition;
            Vector2 origin = back.Size() * 0.5f;
            Rectangle frontFrame = new(0, 0, (int)(front.Width * completion), front.Height);
            Color color = PristineFuryMarkHelper.GetColor(CurrentMark);

            Main.EntitySpriteDraw(back, drawPosition, null, Color.Black * (0.55f * opacity), 0f, origin, 0.92f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(front, drawPosition - new Vector2((front.Width - frontFrame.Width) * 0.46f, 0f), frontFrame, Color.Lerp(color, Color.White, completion) * opacity, 0f, front.Size() * 0.5f, 0.92f, SpriteEffects.None, 0);
        }
    }
}
