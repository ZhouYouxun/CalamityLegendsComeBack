using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingHoldout : ModProjectile, ILocalizedModType
    {
        private const int BurstShotCount = 3;
        private const int BurstShotSpacing = 5;
        private const int BurstLockoutFrames = 24;
        private const int RightChargeFrames = 34;
        private const int RightCooldownFrames = 52;
        private const float HoldoutDistance = 44f;

        private int burstShotsRemaining;
        private int burstShotTimer;
        private int burstLockoutTimer;
        private int rightChargeTimer;
        private int rightCooldownTimer;
        private int muzzleFlashTimer;
        private int emptyClickCooldown;
        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;
        private float recoilOffset;
        private float resonanceGlow;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SeasSearing/NewLegendSeasSearing";

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Max(Owner.direction, 1));
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 47f;

        public override void SetDefaults()
        {
            Projectile.width = 74;
            Projectile.height = 34;
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
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<SeasSearing>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft = 2;

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            UpdatePose();
            UpdateTimersAndVisuals();

            if (Main.myPlayer == Projectile.owner)
                HandleInputs();
        }

        private void HandleInputs()
        {
            bool validMouse = SeasSearing.CanUseWorldInput(Owner);
            bool leftHeld = validMouse && Main.mouseLeft;
            bool rightHeld = validMouse && (Main.mouseRight || Owner.Calamity().mouseRight);

            if (leftHeld && !leftHeldLastFrame && burstLockoutTimer <= 0 && rightChargeTimer <= 0)
                StartBurst();

            if (burstShotsRemaining > 0)
                HandleBurstShots();

            HandleRightClick(rightHeld);
            HandleUltimateInput(validMouse);

            leftHeldLastFrame = leftHeld;
            rightHeldLastFrame = rightHeld;
        }

        private void StartBurst()
        {
            burstShotsRemaining = BurstShotCount;
            burstShotTimer = 0;
            burstLockoutTimer = BurstLockoutFrames;
        }

        private void HandleBurstShots()
        {
            if (burstShotTimer > 0)
            {
                burstShotTimer--;
                return;
            }

            FirePollutionRound(BurstShotCount - burstShotsRemaining);
            burstShotsRemaining--;
            burstShotTimer = BurstShotSpacing;
        }

        private void HandleRightClick(bool rightHeld)
        {
            if (rightCooldownTimer > 0)
            {
                if (!rightHeld)
                    rightChargeTimer = 0;

                return;
            }

            if (rightHeld)
            {
                if (!rightHeldLastFrame)
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.48f, Pitch = -0.55f }, GunTipPosition);

                rightChargeTimer = Math.Min(RightChargeFrames, rightChargeTimer + 1);
                resonanceGlow = Math.Max(resonanceGlow, rightChargeTimer / (float)RightChargeFrames);
                SpawnRightChargeDust(rightChargeTimer / (float)RightChargeFrames);

                if (rightChargeTimer >= RightChargeFrames)
                {
                    FireResonancePulse();
                    rightChargeTimer = 0;
                    rightCooldownTimer = RightCooldownFrames;
                }

                return;
            }

            if (!rightHeld)
                rightChargeTimer = 0;
        }

        private void HandleUltimateInput(bool validMouse)
        {
            if (!validMouse || KeybindSystem.LegendarySkill?.JustPressed != true)
                return;

            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            if (!ssPlayer.CanUseUltimate)
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.32f, Pitch = -0.25f }, Owner.Center);
                return;
            }

            Vector2 direction = (GetMouseWorld() - GunTipPosition).SafeNormalize(AimDirection);
            int beamIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 16f,
                direction,
                ModContent.ProjectileType<SeasSearingDesignatorBeam>(),
                Math.Max(1, Projectile.damage / 8),
                0f,
                Projectile.owner,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(beamIndex))
                Main.projectile[beamIndex].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            ssPlayer.StartUltimateCooldown();
            ApplyRecoil(18f);
            TriggerMuzzleFlash(26);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.2f);
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.74f, Pitch = -0.42f }, GunTipPosition);
        }

        private void FirePollutionRound(int burstIndex)
        {
            if (!Owner.PickAmmo(Owner.HeldItem, out _, out float shootSpeed, out int damage, out float knockback, out _))
            {
                if (emptyClickCooldown <= 0)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = -0.35f }, Owner.Center);
                    emptyClickCooldown = 16;
                }

                burstShotsRemaining = 0;
                return;
            }

            Vector2 direction = AimDirection;
            float spread = MathHelper.ToRadians(0.65f + burstIndex * 0.22f);
            Vector2 velocity = direction.RotatedByRandom(spread) * Math.Max(34f, shootSpeed * 2.18f);
            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 8f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                velocity,
                ModContent.ProjectileType<SeasSearingPollutionRound>(),
                damage,
                knockback,
                Projectile.owner,
                burstIndex);

            if (Main.projectile.IndexInRange(projectileIndex))
            {
                Projectile shot = Main.projectile[projectileIndex];
                shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                shot.ArmorPenetration += 18;
            }

            ApplyRecoil(5.2f + burstIndex * 1.3f);
            TriggerMuzzleFlash(8);
            SpawnMuzzleBurst(direction, burstIndex);
            SeasSearingVisualUtility.PlayDeepShot(GunTipPosition, burstIndex * 0.06f);
        }

        private void FireResonancePulse()
        {
            Vector2 direction = AimDirection;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 20f,
                direction,
                ModContent.ProjectileType<SeasSearingResonancePulse>(),
                0,
                0f,
                Projectile.owner,
                Projectile.damage);

            int detonated = SeasSearingPollutionNPC.DetonateAll(Owner, Projectile.damage, GunTipPosition);
            float shake = detonated <= 0 ? 1.8f : MathHelper.Clamp(2f + detonated * 0.7f, 2.8f, 13f);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, shake);
            ApplyRecoil(12f + Math.Min(detonated, 8) * 1.4f);
            TriggerMuzzleFlash(24);
            resonanceGlow = 1.4f;

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.72f, Pitch = -0.55f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.58f, Pitch = -0.3f }, GunTipPosition);
        }

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 desiredAim = (GetMouseWorld() - armPosition).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Vector2.Lerp(AimDirection, desiredAim, 0.44f).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int direction = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = direction;
            Projectile.direction = direction;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPosition + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir);

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * direction).ToRotation();
            Owner.HeldItem.noUseGraphic = true;
            Owner.itemTime = Owner.itemAnimation = 2;

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation + MathHelper.ToRadians(5f) * direction);
        }

        private void UpdateTimersAndVisuals()
        {
            if (burstLockoutTimer > 0)
                burstLockoutTimer--;

            if (rightCooldownTimer > 0)
                rightCooldownTimer--;

            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;

            if (emptyClickCooldown > 0)
                emptyClickCooldown--;

            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.26f);
            resonanceGlow = MathHelper.Clamp(resonanceGlow - 0.035f, 0f, 1.5f);
        }

        private Vector2 GetMouseWorld() => SeasSearing.GetMouseWorld(Owner);

        private void ApplyRecoil(float amount)
        {
            recoilOffset = Math.Max(recoilOffset, amount);
            Owner.velocity -= AimDirection * amount * 0.014f;
        }

        private void TriggerMuzzleFlash(int frames)
        {
            muzzleFlashTimer = Math.Max(muzzleFlashTimer, frames);
        }

        private void SpawnMuzzleBurst(Vector2 direction, int burstIndex)
        {
            if (Main.dedServ)
                return;

            Color cyan = SeasSearingPalette.RadioactiveCyan;
            Color green = SeasSearingPalette.ToxicGreen;

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.4f, 4.8f) - direction * burstIndex * 0.2f;
                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool(2) ? DustID.Water : DustID.GemEmerald,
                    velocity,
                    100,
                    Color.Lerp(cyan, green, Main.rand.NextFloat(0.15f, 0.75f)),
                    Main.rand.NextFloat(0.7f, 1.25f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    GunTipPosition - direction * Main.rand.NextFloat(2f, 12f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.7f, 2.4f),
                    155,
                    Color.Lerp(SeasSearingPalette.AbyssBlack, SeasSearingPalette.DeepBlue, 0.45f),
                    Main.rand.NextFloat(0.55f, 0.9f));
                smoke.noGravity = true;
            }
        }

        private void SpawnRightChargeDust(float charge)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.35f + charge * 0.55f)
                return;

            Vector2 center = GunTipPosition + AimDirection * (8f + charge * 10f);
            Vector2 offset = Main.rand.NextVector2CircularEdge(16f + 28f * charge, 16f + 22f * charge);
            Dust dust = Dust.NewDustPerfect(
                center + offset,
                Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.Water,
                -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.2f, 4.2f),
                100,
                Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, charge),
                Main.rand.NextFloat(0.65f, 1.1f) * (0.7f + charge));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawPressureField();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            float flash = muzzleFlashTimer / 24f;
            if (resonanceGlow > 0.02f || flash > 0.02f)
                DrawWeaponGlow(texture, drawPosition, origin, effects, flash);

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
            DrawMuzzleGlow(flash);
            return false;
        }

        private void DrawWeaponGlow(Texture2D texture, Vector2 drawPosition, Vector2 origin, SpriteEffects effects, float flash)
        {
            Color color = (Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, 0.72f) with { A = 0 }) *
                MathHelper.Clamp(resonanceGlow * 0.45f + flash * 0.62f, 0f, 0.9f);
            int draws = 10;
            float radius = 2.2f + resonanceGlow * 4f + flash * 5f;
            for (int i = 0; i < draws; i++)
            {
                float angle = MathHelper.TwoPi * i / draws + Main.GlobalTimeWrappedHourly * 1.6f;
                Main.EntitySpriteDraw(texture, drawPosition + angle.ToRotationVector2() * radius, null, color, Projectile.rotation, origin, Projectile.scale, effects, 0);
            }
        }

        private void DrawMuzzleGlow(float flash)
        {
            float charge = rightChargeTimer / (float)RightChargeFrames;
            float power = Math.Max(flash, Math.Max(charge, resonanceGlow * 0.45f));
            if (power <= 0.02f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 muzzle = GunTipPosition - Main.screenPosition;
            Color color = (Color.Lerp(SeasSearingPalette.RadioactiveCyan, Color.White, 0.25f + power * 0.3f) with { A = 0 }) * power;

            Main.EntitySpriteDraw(bloom, muzzle, null, color * 0.68f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.22f + power * 0.3f, 0.11f + power * 0.18f), SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / 3f + Main.GlobalTimeWrappedHourly * 2f;
                Main.EntitySpriteDraw(star, muzzle, null, color * 0.46f, rotation, star.Size() * 0.5f, new Vector2(0.12f, 0.9f + power * 1.2f), SpriteEffects.None, 0);
            }
        }

        private void DrawPressureField()
        {
            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            float power = ssPlayer.PressureVisualPower;
            if (power <= 0.02f || Main.dedServ)
                return;

            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Owner.Center - Main.screenPosition;
            Color cyan = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * (0.12f + power * 0.2f);
            Color deep = (SeasSearingPalette.DeepBlue with { A = 0 }) * (0.1f + power * 0.18f);
            float pulse = 0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f);

            Main.EntitySpriteDraw(bloom, center, null, deep * 0.55f, 0f, bloom.Size() * 0.5f, new Vector2(1.9f, 1.25f) * power * 0.72f, SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float local = i / 3f;
                float rotation = Main.GlobalTimeWrappedHourly * (0.28f + local * 0.14f);
                float scale = (1.65f + local * 0.74f + power * 0.35f) * pulse;
                Main.EntitySpriteDraw(ring, center, null, (i == 0 ? cyan : deep) * (1f - local * 0.22f), rotation, ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
        }
    }
}
