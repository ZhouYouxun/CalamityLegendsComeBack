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
        private const int RightChargeMaxFrames = 120;
        private const int RightChargeCooldown = 42;
        private const float RightFireballSpeed = 15.5f;
        private const float RightFireballDamageMultiplier = 2.15f;
        private const float HoldoutDistance = 34f;

        private static readonly PristineFuryMark[] TemporaryDebugMarkCycle =
        {
            PristineFuryMark.Idle,
            PristineFuryMark.EvilT2,
            PristineFuryMark.SlimeGod,
            PristineFuryMark.HardMode,
            PristineFuryMark.Prime,
            PristineFuryMark.BrimstoneElemental,
            PristineFuryMark.FakeCalamity,
            PristineFuryMark.Plantera,
            PristineFuryMark.Aurora,
            PristineFuryMark.Goliath,
            PristineFuryMark.Moonlord,
            PristineFuryMark.Providence,
            PristineFuryMark.Ravager,
            PristineFuryMark.Polterghast,
            PristineFuryMark.Dog,
            PristineFuryMark.Dragon,
            PristineFuryMark.ExoThanatos,
        };

        private int hookChargeTimer;
        private int hookCooldown;
        private bool hookChargeReady;
        private bool hookFiredForThisHold;
        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;
        private int rightChargeTimer;
        private bool rightChargeReady;
        private int rightCooldownTimer;
        private int muzzleFlashTimer;
        private int leftEffectResetKey = -1;
        private float recoilOffset;
        private float leftVisualPower;
        private int dragonEyeTimer;

        internal int LeftTimer;
        internal int LeftChargeTimer;
        internal int LeftAuxTimer;
        internal int LeftBurstIndex;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFuryHoldOut";

        internal Player Owner => Main.player[Projectile.owner];
        internal Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        internal Vector2 GunTipPosition => Projectile.Center + AimDirection * 54f;
        internal Vector2 DragonMouthPosition => GunTipPosition + AimDirection * 8f;
        internal Vector2 DragonEyePosition
        {
            get
            {
                Vector2 right = AimDirection.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
                return Projectile.Center + AimDirection * 16f - right * 12f;
            }
        }
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
            bool debugCycleEquipped = Owner.GetModPlayer<PristineFuryPlayer>().DebugCycleEquipped;

            ResetLeftStateIfMarkChanged();
            UpdateLeftVisualState(leftHeld);

            if (debugCycleEquipped && rightHeld && !bothHeld)
            {
                ResetRightCharge();
                CancelHookChargeForTemporaryDebug();

                if (!rightHeldLastFrame)
                    CycleMarkForTemporaryDebug();

                PristineFuryLeftEffectRegistry.Update(CurrentMark, this, leftHeld, leftHeld && !leftHeldLastFrame, !leftHeld && leftHeldLastFrame);
                leftHeldLastFrame = leftHeld;
                rightHeldLastFrame = rightHeld;
                return;
            }

            if (bothHeld)
            {
                ResetRightCharge();
                HandleHookCharge();
                leftHeldLastFrame = leftHeld;
                rightHeldLastFrame = rightHeld;
                return;
            }

            if (hookChargeReady && leftHeldLastFrame && rightHeldLastFrame)
            {
                FireExtractionHook();
                ResetHookChargeAfterFire();
                leftHeldLastFrame = leftHeld;
                rightHeldLastFrame = rightHeld;
                return;
            }

            DecayHookCharge();

            if (!leftHeld && !rightHeld)
            {
                hookFiredForThisHold = false;
                hookChargeReady = false;
            }

            PristineFuryLeftEffectRegistry.Update(CurrentMark, this, leftHeld, leftHeld && !leftHeldLastFrame, !leftHeld && leftHeldLastFrame);
            HandleRightClick(rightHeld);

            leftHeldLastFrame = leftHeld;
            rightHeldLastFrame = rightHeld;
        }

        private void UpdateLeftVisualState(bool leftHeld)
        {
            leftVisualPower = MathHelper.Lerp(leftVisualPower, leftHeld ? 1f : 0f, leftHeld ? 0.28f : 0.12f);
            if (leftHeld)
                dragonEyeTimer++;
        }

        private void CycleMarkForTemporaryDebug()
        {
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            int currentIndex = Array.IndexOf(TemporaryDebugMarkCycle, pristinePlayer.CurrentMark);
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % TemporaryDebugMarkCycle.Length;
            PristineFuryMark nextMark = TemporaryDebugMarkCycle[nextIndex];

            pristinePlayer.ExtractMark(nextMark, temporaryDebugSwitch: true);
            ResetLeftEffectStateForMark(nextMark);
            ApplyRecoil(2.4f);
            TriggerMuzzleFlash(8);
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.7f, Pitch = 0.08f }, Owner.Center);
        }

        private void CancelHookChargeForTemporaryDebug()
        {
            hookChargeTimer = 0;
            hookChargeReady = false;
            hookFiredForThisHold = false;

            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            pristinePlayer.HookChargeFrames = 0;
            pristinePlayer.HookChargeOpacity = 0f;
        }

        private void HandleHookCharge()
        {
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();

            if (hookCooldown > 0 || hookFiredForThisHold)
            {
                pristinePlayer.HookChargeOpacity = Math.Max(pristinePlayer.HookChargeOpacity, 0.35f);
                return;
            }

            if (!hookChargeReady)
                hookChargeTimer++;

            if (hookChargeTimer >= HookChargeMaxFrames)
            {
                hookChargeTimer = HookChargeMaxFrames;

                if (!hookChargeReady)
                {
                    hookChargeReady = true;
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.72f, Pitch = 0.35f }, GunTipPosition);
                }
            }

            pristinePlayer.HookChargeFrames = hookChargeTimer;
            pristinePlayer.HookChargeOpacity = MathHelper.Clamp(pristinePlayer.HookChargeOpacity + 0.08f, 0f, 1f);

            SpawnHookChargeEffects(hookChargeTimer / (float)HookChargeMaxFrames);
        }

        private void DecayHookCharge()
        {
            if (hookChargeTimer <= 0 || hookChargeReady)
                return;

            hookChargeTimer = Math.Max(0, hookChargeTimer - 5);
            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            pristinePlayer.HookChargeFrames = hookChargeTimer;
            pristinePlayer.HookChargeOpacity = Math.Max(pristinePlayer.HookChargeOpacity, hookChargeTimer / (float)HookChargeMaxFrames);
        }

        private void ResetHookChargeAfterFire()
        {
            hookChargeTimer = 0;
            hookChargeReady = false;
            hookCooldown = 42;
            hookFiredForThisHold = true;

            PristineFuryPlayer pristinePlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            pristinePlayer.HookChargeFrames = 0;
            pristinePlayer.HookChargeOpacity = Math.Max(pristinePlayer.HookChargeOpacity, 0.35f);
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

        private void HandleRightClick(bool rightHeld)
        {
            if (rightCooldownTimer > 0)
            {
                if (!rightHeld)
                    ResetRightCharge();
                return;
            }

            if (rightHeld)
            {
                if (rightChargeTimer == 0)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeStart") { Volume = 0.55f }, GunTipPosition);

                rightChargeTimer = Math.Min(RightChargeMaxFrames, rightChargeTimer + 1);
                float chargeCompletion = rightChargeTimer / (float)RightChargeMaxFrames;
                EnsureRightChargeOrb(chargeCompletion);

                if (rightChargeTimer == RightChargeMaxFrames / 2)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLV1") { Volume = 0.62f, Pitch = -0.08f }, GunTipPosition);

                if (!rightChargeReady && rightChargeTimer >= RightChargeMaxFrames)
                {
                    rightChargeReady = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLV2") { Volume = 0.72f, Pitch = -0.1f }, GunTipPosition);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserCompleteCharge") { Volume = 0.5f, Pitch = 0.08f }, GunTipPosition);
                }
                else if (rightChargeTimer > 10 && rightChargeTimer < RightChargeMaxFrames && rightChargeTimer % 40 == 0)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLoop") { Volume = 0.36f, Pitch = 0.18f }, GunTipPosition);

                return;
            }

            if (rightHeldLastFrame && rightChargeTimer >= RightChargeMaxFrames)
                FireRightNovaFireball();

            ResetRightCharge();
        }

        private void EnsureRightChargeOrb(float chargeCompletion)
        {
            int chargeType = ModContent.ProjectileType<PristineFuryRightNovaChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == chargeType && (int)projectile.ai[0] == Projectile.whoAmI)
                {
                    projectile.ai[1] = chargeCompletion;
                    projectile.netUpdate = true;
                    return;
                }
            }

            int orbIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + AimDirection * 8f,
                AimDirection,
                chargeType,
                0,
                0f,
                Projectile.owner,
                Projectile.whoAmI,
                chargeCompletion);
            PFLeftEffectRules.ApplyTheme(orbIndex, CurrentMark);
        }

        private void FireRightNovaFireball()
        {
            Vector2 direction = AimDirection;
            Vector2 muzzle = GunTipPosition + direction * 12f;
            int fireballIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                direction * RightFireballSpeed,
                ModContent.ProjectileType<PristineFuryRightNovaFireball>(),
                GetScaledDamage(RightFireballDamageMultiplier),
                Projectile.knockBack * 1.4f,
                Projectile.owner);
            PFLeftEffectRules.ApplyTheme(fireballIndex, CurrentMark);

            ApplyRecoil(20f);
            TriggerMuzzleFlash(24);
            SpawnMuzzleBurst(new Color(255, 54, 42), 1.35f);
            rightCooldownTimer = RightChargeCooldown;
            Owner.SetScreenshake(6f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserBigShot") { PitchVariance = 0.22f, Volume = 0.82f }, muzzle);
        }

        private void ResetRightCharge()
        {
            rightChargeTimer = 0;
            rightChargeReady = false;

            int chargeType = ModContent.ProjectileType<PristineFuryRightNovaChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == chargeType && (int)projectile.ai[0] == Projectile.whoAmI)
                    projectile.Kill();
            }
        }

        private void ResetLeftStateIfMarkChanged()
        {
            int key = (int)CurrentMark;
            if (leftEffectResetKey == key)
                return;

            ResetLeftEffectStateForMark(CurrentMark);
        }

        private void ResetLeftEffectStateForMark(PristineFuryMark mark)
        {
            LeftTimer = 0;
            LeftChargeTimer = 0;
            LeftAuxTimer = 0;
            LeftBurstIndex = 0;
            leftEffectResetKey = (int)mark;
        }

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 aim = AimDirection;

            if (ShouldUseProvidenceMortarPose())
            {
                UpdateProvidenceMortarPose(armPosition);
                return;
            }

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
            Owner.itemRotation = (aim * direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation - MathHelper.ToRadians(10f) * direction);

            if (recoilOffset > 0f)
                recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.22f);
        }

        private bool ShouldUseProvidenceMortarPose()
        {
            if (CurrentMark != PristineFuryMark.Providence)
                return false;

            if (Projectile.owner != Main.myPlayer)
                return leftHeldLastFrame;

            bool validMouse = !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface && !(Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type);
            return validMouse && Main.mouseLeft;
        }

        private void UpdateProvidenceMortarPose(Vector2 armPosition)
        {
            Vector2 mouseWorld = GetMouseWorld();
            Vector2 skyAnchor = new(MathHelper.Lerp(mouseWorld.X, Owner.Center.X, 0.55f), Owner.Center.Y - 500f * Owner.gravDir);
            Vector2 desiredAim = (skyAnchor - Owner.Center).SafeNormalize(-Vector2.UnitY * Owner.gravDir);
            Vector2 aim = AimDirection;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.velocity = Vector2.Lerp(aim, desiredAim, 0.42f).SafeNormalize(desiredAim);
                aim = AimDirection;
                Projectile.netUpdate = true;
            }

            float upDot = Vector2.Dot(aim, -Vector2.UnitY * Owner.gravDir);
            int direction = Math.Sign(aim.X);
            if (direction == 0)
                direction = Owner.direction;

            Vector2 armOffset = new(
                Utils.Remap(MathF.Abs(upDot), 0f, 1f, 0f, -14f, true) * direction,
                -6f * Owner.gravDir + Utils.Remap(MathF.Abs(upDot), 0f, 1f, 0f, 24f, true) * Owner.gravDir);

            Projectile.spriteDirection = direction;
            Projectile.direction = direction;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPosition + aim * (HoldoutDistance + 8f - recoilOffset) + armOffset;

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + MathHelper.ToRadians(12f) * direction);

            if (recoilOffset > 0f)
                recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.12f);
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
            Vector2 muzzle = GunTipPosition - direction * 14f;

            Particle glow = new GlowOrbParticle(
                muzzle + direction * 2f,
                direction * 1.8f,
                false,
                16,
                0.46f * scale,
                Color.Lerp(color, Color.White, 0.42f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.6f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    muzzle + Main.rand.NextVector2Circular(3f, 3f),
                    velocity,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.62f, 1f) * scale,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.18f, 0.5f))));
            }
        }

        private void SpawnHookChargeEffects(float charge)
        {
            if (Main.dedServ)
                return;

            Vector2 center = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.48f);
            Color color = Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, charge * 0.55f);
            Lighting.AddLight(center, color.ToVector3() * (0.25f + charge * 0.55f));

            if (Main.rand.NextFloat() < 0.35f + charge * 0.45f)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(18f + 34f * charge, 18f + 34f * charge);
                Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4.4f);
                Particle mote = new PointParticle(
                    center + offset,
                    velocity,
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.72f, 1.15f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.12f, 0.35f)));
                GeneralParticleHandler.SpawnParticle(mote);

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
            DrawDragonEyeGlow(leftVisualPower);
            DrawDragonMouthSmoke(leftVisualPower);
            DrawFakeCalamityArcNovaCharge();
            DrawRightArcNovaCharge();
            DrawMuzzleGlow(flash);
            DrawHookChargeBar();
            return false;
        }

        private void DrawDragonEyeGlow(float power)
        {
            if (power <= 0.025f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 eye = DragonEyePosition - Main.screenPosition;
            Color theme = PristineFuryMarkHelper.GetColor(CurrentMark);
            Color color = (Color.Lerp(theme, Color.White, 0.28f) with { A = 0 }) * power;
            float pulse = 0.86f + 0.14f * (float)Math.Sin(dragonEyeTimer * 0.22f);

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, eye, null, color * 0.72f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.18f, 0.12f) * pulse, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / 3f + dragonEyeTimer * 0.035f;
                Main.EntitySpriteDraw(star, eye, null, color * 0.58f, rotation, star.Size() * 0.5f, new Vector2(0.12f, 0.72f + power * 0.38f), SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
        }

        private void DrawDragonMouthSmoke(float power)
        {
            if (power <= 0.025f || Main.dedServ)
                return;

            Texture2D magic = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/smoke_04").Value;
            Vector2 mouth = DragonMouthPosition - Main.screenPosition;
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color theme = PristineFuryMarkHelper.GetColor(CurrentMark) with { A = 0 };
            Color white = Color.White with { A = 0 };
            float time = Main.GlobalTimeWrappedHourly * 4.8f;

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 5; i++)
            {
                float local = i / 5f;
                float swirl = time + local * MathHelper.TwoPi;
                Vector2 offset = forward * (2f + i * 3.2f) + right * (float)Math.Sin(swirl) * (1.1f + i * 0.35f);
                float opacity = power * (1f - local * 0.12f);
                float scale = 0.035f + local * 0.012f;
                Main.EntitySpriteDraw(smoke, mouth + offset, null, theme * opacity * 0.26f, Projectile.rotation + swirl * 0.18f, smoke.Size() * 0.5f, scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(magic, mouth + offset * 0.7f, null, Color.Lerp(theme, white, 0.25f) * opacity * 0.18f, -Projectile.rotation + swirl * 0.12f, magic.Size() * 0.5f, scale * 0.72f, SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
        }

        private void DrawFakeCalamityArcNovaCharge()
        {
            if (CurrentMark != PristineFuryMark.FakeCalamity || LeftChargeTimer <= 0 || Main.dedServ)
                return;

            const float chargeFrames = 108f;
            float charge = MathHelper.Clamp(LeftChargeTimer / chargeFrames, 0f, 1f);
            if (charge <= 0.02f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = AimDirection;
            Vector2 tip = GunTipPosition + direction * (8f + charge * 6f) - Main.screenPosition;
            Color theme = (Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, charge * 0.32f) with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float chargeScale = charge * 2f;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 6; i++)
            {
                Vector2 place = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 7f) * charge;
                Vector2 drawPosition = tip + place - Vector2.Lerp(place, -direction, 0.9f) * Main.rand.NextFloat(18f, 42f) + direction * -8f * (6f - chargeScale * 2f);
                Vector2 smearScale = new(0.28f * chargeScale, (1.8f + (Main.rand.NextBool(4) ? 1.9f : 0f)) * 0.055f * chargeScale);
                Main.EntitySpriteDraw(smear, drawPosition, null, theme * 0.86f, direction.RotatedByRandom(0.26f).ToRotation() - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), smearScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 3; i++)
            {
                Color layerColor = Color.Lerp(theme, white, i * 0.22f);
                Vector2 layerScale = new Vector2(1.42f, 1.02f) * chargeScale * (1f - i * 0.24f) * 0.25f * pulse;
                Main.EntitySpriteDraw(bloom, tip, null, layerColor * (0.88f - i * 0.13f), Projectile.rotation, bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(ring, tip, null, theme * (0.36f + charge * 0.22f), Projectile.rotation + Main.GlobalTimeWrappedHourly * 0.85f, ring.Size() * 0.5f, (0.16f + charge * 0.5f) * pulse, SpriteEffects.None, 0f);

            if (charge >= 0.98f)
            {
                for (int satellite = 0; satellite < 3; satellite++)
                {
                    Vector2 orbit = (MathHelper.TwoPi * satellite / 3f + Main.GlobalTimeWrappedHourly * 7.2f).ToRotationVector2();
                    Vector2 offset = new Vector2(orbit.X * 0.72f, orbit.Y * 1.18f).RotatedBy(Projectile.rotation) * chargeScale * 13f;
                    Main.EntitySpriteDraw(bloom, tip + offset, null, Color.Lerp(theme, white, 0.45f) * 0.82f, Projectile.rotation, bloom.Size() * 0.5f, chargeScale * 0.082f, SpriteEffects.None, 0f);
                }
            }

            PFLeftEffectRules.EndAdditive();
        }

        private void DrawRightArcNovaCharge()
        {
            if (rightChargeTimer <= 0 || Main.dedServ)
                return;

            float charge = MathHelper.Clamp(rightChargeTimer / (float)RightChargeMaxFrames, 0f, 1f);
            if (charge <= 0.02f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = AimDirection;
            Vector2 tip = GunTipPosition + direction * (8f + charge * 5f) - Main.screenPosition;
            Color fire = (Color.Lerp(new Color(255, 54, 42), new Color(255, 126, 42), 0.42f) with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float chargeScale = Math.Min(charge * 2f, 2f);
            float pulse = 0.9f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 13f + Projectile.identity);

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < 6; i++)
            {
                Vector2 place = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 6.5f) * charge;
                Vector2 drawPosition = tip + place - Vector2.Lerp(place, -direction, 0.9f) * Main.rand.NextFloat(16f, 40f) + direction * -8f * (6f - chargeScale * 2f);
                Vector2 smearScale = new(0.25f * chargeScale, (1.7f + (Main.rand.NextBool(4) ? 1.8f : 0f)) * 0.05f * chargeScale);
                Color smearColor = Main.rand.NextBool(4) ? white : fire;
                Main.EntitySpriteDraw(smear, drawPosition, null, smearColor * 0.82f, direction.RotatedByRandom(0.3f).ToRotation() - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), smearScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 3; i++)
            {
                Color layerColor = Color.Lerp(fire, white, i * 0.25f);
                Vector2 layerScale = new Vector2(1.35f, 1f) * chargeScale * (1f - 0.27f * i) * 0.23f * pulse;
                Main.EntitySpriteDraw(bloom, tip, null, layerColor * 0.8f, Projectile.rotation + Main.rand.NextFloat(-5f, 5f), bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(ring, tip, null, fire * (0.26f + charge * 0.38f), Projectile.rotation + Main.GlobalTimeWrappedHourly * 0.95f, ring.Size() * 0.5f, (0.13f + charge * 0.42f) * pulse, SpriteEffects.None, 0f);

            if (rightChargeReady)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 angle = (MathHelper.TwoPi * i / 3f).ToRotationVector2().RotatedBy(Main.GlobalTimeWrappedHourly * 7.2f);
                    Vector2 offset = new Vector2(angle.X * 0.7f, angle.Y * 1.2f).RotatedBy(Projectile.rotation) * chargeScale * 12f;
                    for (int layer = 0; layer < 2; layer++)
                    {
                        Color layerColor = Color.Lerp(fire, white, layer) * 0.8f;
                        float layerScale = chargeScale * (1f - 0.25f * layer) * 0.085f;
                        Main.EntitySpriteDraw(bloom, tip + offset, null, layerColor, Main.rand.NextFloat(-5f, 5f), bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
                    }
                }
            }

            PFLeftEffectRules.EndAdditive();
        }

        private void DrawMuzzleGlow(float flash)
        {
            float charge = Owner.GetModPlayer<PristineFuryPlayer>().HookChargeOpacity;
            float power = Math.Max(flash, charge * 0.7f);
            if (power <= 0.02f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 muzzle = GunTipPosition - AimDirection * 21f - Main.screenPosition;
            Color color = (Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, 0.48f) with { A = 0 }) * power;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, muzzle, null, color * 0.55f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.34f + power * 0.24f, 0.18f + power * 0.12f), SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (1.1f + i * 0.15f);
                Main.EntitySpriteDraw(star, muzzle, null, color * 0.62f, rotation, star.Size() * 0.5f, new Vector2(0.24f + power * 0.18f, 1.1f + power * 1.4f), SpriteEffects.None, 0);
            }
            PFLeftEffectRules.EndAdditive();
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
            float scale = 0.92f;
            Vector2 drawPosition = Owner.Top + new Vector2(0f, Owner.gfxOffY - 38f) - Main.screenPosition;
            Vector2 backOrigin = back.Size() * 0.5f;
            Vector2 frontOrigin = new(0f, front.Height * 0.5f);
            Vector2 frontPosition = drawPosition - new Vector2(front.Width * scale * 0.5f, 0f);
            Rectangle frontFrame = new(0, 0, (int)(front.Width * completion), front.Height);
            Color color = PristineFuryMarkHelper.GetColor(CurrentMark);

            Main.EntitySpriteDraw(back, drawPosition, null, Color.Black * (0.55f * opacity), 0f, backOrigin, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(front, frontPosition, frontFrame, Color.Lerp(color, Color.White, completion) * opacity, 0f, frontOrigin, scale, SpriteEffects.None, 0);
        }
    }
}
