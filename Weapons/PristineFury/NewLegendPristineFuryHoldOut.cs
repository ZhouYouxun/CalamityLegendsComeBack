using CalamityLegendsComeBack.Accssory.PF;
using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityLegendsComeBack.Weapons.PristineFury.UI;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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
        private const float HoldoutDistance = 34f;

        // Debug 模式下的完整印记顺序（按难度顺序，包含 Idle 作为默认）。
        private static readonly PristineFuryMark[] TemporaryDebugMarkCycle =
        {
            PristineFuryMark.Idle,
            PristineFuryMark.DesertScourge,
            PristineFuryMark.EyeOfCthulhu,
            PristineFuryMark.Skeletron,
            PristineFuryMark.EvilT2,
            PristineFuryMark.SlimeGod,
            PristineFuryMark.HardMode,
            PristineFuryMark.BrimstoneElemental,
            PristineFuryMark.Prime,
            PristineFuryMark.FakeCalamity,
            PristineFuryMark.Plantera,
            PristineFuryMark.Golem,
            PristineFuryMark.Goliath,
            PristineFuryMark.Empress,
            PristineFuryMark.Moonlord,
            PristineFuryMark.Providence,
            PristineFuryMark.Polterghast,
            PristineFuryMark.Dog,
            PristineFuryMark.Dragon,
        };

        private int hookChargeTimer;
        private int hookCooldown;
        private bool hookChargeReady;
        private bool hookFiredForThisHold;
        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;
        private int rightChargeTimer;
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

        private int fomoEchoTimer;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFuryHoldOut";

        internal Player Owner => Main.player[Projectile.owner];
        internal Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        internal Vector2 GunTipPosition => Projectile.Center + AimDirection * 2f;
        internal Vector2 DragonMouthPosition => NewLegendPristineFuryHoldOut_DragonDrawData.GetDragonMouthPosition(Projectile.Center, AimDirection);
        internal Vector2 DragonEyePosition => NewLegendPristineFuryHoldOut_DragonDrawData.GetDragonEyePosition(Projectile.Center, AimDirection, Owner.gravDir, Projectile.spriteDirection);
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

            // 弧形 HUD：debug 模式下关闭弹夹显示，正常模式下保证存在。
            UpdateMarkArcVisibility(debugCycleEquipped);

            // 轮盘键：debug 模式下禁用。
            if (!debugCycleEquipped && KeybindSystem.LegendaryWeaponFormSwitch?.JustPressed == true)
                SpawnMarkSelectionWheel();

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
            if (leftHeld)
                HandleFOMOEchoes();

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

            if (!Main.dedServ)
            {
                Color markColor = PristineFuryMarkHelper.GetColor(nextMark);

                // 爆发粒子环（12 个辐射 + 8 个随机）
                for (int i = 0; i < 12; i++)
                {
                    float angle = MathHelper.TwoPi * i / 12f;
                    Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(3.5f, 7.8f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        GunTipPosition,
                        vel + AimDirection * 1.2f,
                        false,
                        Main.rand.Next(20, 32),
                        Main.rand.NextFloat(0.20f, 0.38f),
                        Color.Lerp(markColor, Color.White, 0.35f),
                        true, false, true));
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = AimDirection.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 5.5f);
                    GeneralParticleHandler.SpawnParticle(new PointParticle(
                        GunTipPosition + Main.rand.NextVector2Circular(5f, 5f),
                        vel,
                        false,
                        Main.rand.Next(14, 22),
                        Main.rand.NextFloat(0.55f, 0.95f),
                        Color.Lerp(markColor, Color.White, Main.rand.NextFloat(0.2f, 0.65f))));
                }

                SpawnMuzzleBurst(markColor, 2.2f);
            }

            ApplyRecoil(10f);
            TriggerMuzzleFlash(28);
            Owner.SetScreenshake(3.5f);

            // 根据印记在列表中的位置微调音调，营造"频率计"感觉
            float pitchStep = nextIndex / (float)(TemporaryDebugMarkCycle.Length - 1);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.80f, Pitch = MathHelper.Lerp(-0.12f, 0.42f, pitchStep) }, GunTipPosition);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserCompleteCharge") { Volume = 0.45f, Pitch = 0.12f + pitchStep * 0.28f }, GunTipPosition);
        }

        // debug 模式下关闭弧形弹夹 HUD，普通模式下保证它存在。
        private void UpdateMarkArcVisibility(bool debugEquipped)
        {
            int arcType = ModContent.ProjectileType<PFMarkStatusArc>();
            if (debugEquipped)
            {
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.active && proj.owner == Projectile.owner && proj.type == arcType)
                    {
                        proj.Kill();
                        break;
                    }
                }
                return;
            }
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.active && proj.owner == Projectile.owner && proj.type == arcType)
                    return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center, Vector2.Zero,
                arcType, 0, 0f, Projectile.owner);
        }

        private void SpawnMarkSelectionWheel()
        {
            int wheelType = ModContent.ProjectileType<PFMarkSelectionWheel>();
            // Kill any existing wheel first so only one appears at a time.
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.active && proj.owner == Projectile.owner && proj.type == wheelType)
                {
                    proj.Kill();
                    break;
                }
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center, Vector2.Zero,
                wheelType, 0, 0f, Projectile.owner);
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

        private int GetMaxRightChargeFrames()
        {
            if (!Main.hardMode)
                return 119;
            if (!NPC.downedPlantBoss)
                return 179;
            if (!NPC.downedMoonlord)
                return 299;
            return 600;
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

                int maxFrames = GetMaxRightChargeFrames();
                rightChargeTimer = Math.Min(maxFrames, rightChargeTimer + 1);

                if (rightChargeTimer == 60)
                {
                    if (Owner.whoAmI == Main.myPlayer)
                        CombatText.NewText(Owner.getRect(), new Color(255, 160, 60), "圣焰临界", true);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLV1") { Volume = 0.62f, Pitch = -0.08f }, GunTipPosition);
                    
                    if (!Main.dedServ)
                    {
                        Color themeColor = PristineFuryMarkHelper.GetColor(CurrentMark);
                        GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                            GunTipPosition, Vector2.Zero, themeColor * 0.9f,
                            new Vector2(3f, 3f), 0f, 0.9f, 0.08f, 20
                        ));
                    }
                }
                else if (rightChargeTimer == 120)
                {
                    if (Owner.whoAmI == Main.myPlayer)
                        CombatText.NewText(Owner.getRect(), new Color(255, 100, 30), "净火压缩", true);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLV2") { Volume = 0.72f, Pitch = -0.1f }, GunTipPosition);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.45f }, GunTipPosition);
                    
                    if (!Main.dedServ)
                    {
                        Color themeColor = PristineFuryMarkHelper.GetColor(CurrentMark);
                        GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                            GunTipPosition, Vector2.Zero, themeColor * 0.95f,
                            new Vector2(4.5f, 4.5f), 0f, 0.9f, 0.08f, 20
                        ));
                    }
                }
                else if (rightChargeTimer == 180)
                {
                    if (Owner.whoAmI == Main.myPlayer)
                        CombatText.NewText(Owner.getRect(), new Color(255, 50, 20), "圣火过压", true);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot") { Volume = 0.75f, Pitch = -0.1f }, GunTipPosition);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserCompleteCharge") { Volume = 0.5f, Pitch = 0.08f }, GunTipPosition);
                    
                    if (!Main.dedServ)
                    {
                        Color themeColor = PristineFuryMarkHelper.GetColor(CurrentMark);
                        GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                            GunTipPosition, Vector2.Zero, themeColor,
                            new Vector2(6f, 6f), 0f, 0.9f, 0.08f, 20
                        ));
                    }
                }
                else if (rightChargeTimer == 300)
                {
                    if (Owner.whoAmI == Main.myPlayer)
                        CombatText.NewText(Owner.getRect(), new Color(255, 0, 0), "过载自燃", true);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.85f, Pitch = -0.2f }, GunTipPosition);
                }

                EnsureRightChargeOrb(rightChargeTimer);

                if (rightChargeTimer > 300)
                {
                    if (rightChargeTimer % 20 == 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurnLoop") { Volume = 0.35f, MaxInstances = 1 }, GunTipPosition);
                    }

                    if (rightChargeTimer % 3 == 0 && Owner.whoAmI == Main.myPlayer)
                    {
                        Vector2 direction = Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi).ToRotationVector2();
                        int starProj = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            GunTipPosition,
                            direction * 11.5f,
                            ModContent.ProjectileType<PristineFuryHomingStar>(),
                            GetRightScaledDamage(PF_Balance.GetRightOverheatStarDamageMultiplier()),
                            Projectile.knockBack * 0.3f,
                            Projectile.owner
                        );
                        PFLeftEffectRules.ApplyTheme(starProj, CurrentMark);
                    }
                }

                return;
            }

            if (rightHeldLastFrame)
            {
                if (rightChargeTimer >= 180)
                {
                    FireRightNovaFireball(3f);
                }
                else if (rightChargeTimer >= 120)
                {
                    FireRightNovaFireball(2f);
                }
                else if (rightChargeTimer >= 60)
                {
                    FireRightNovaFireball(1f);
                }
            }

            ResetRightCharge();
        }

        private void EnsureRightChargeOrb(float chargeTimer)
        {
            int chargeType = ModContent.ProjectileType<PristineFuryRightNovaChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == chargeType && (int)projectile.ai[0] == Projectile.whoAmI)
                {
                    projectile.ai[1] = chargeTimer;
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
                chargeTimer);
            PFLeftEffectRules.ApplyTheme(orbIndex, CurrentMark);
        }

        private void FireRightNovaFireball(float chargeLevel)
        {
            Vector2 direction = AimDirection;
            Vector2 muzzle = GunTipPosition + direction * 12f;
            float damageMult = PF_Balance.GetRightFireballDamageMultiplier(chargeLevel);

            int fireballIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                direction * RightFireballSpeed,
                ModContent.ProjectileType<PristineFuryRightNovaFireball>(),
                GetScaledDamage(damageMult),
                Projectile.knockBack * (chargeLevel == 3f ? 1.6f : (chargeLevel == 2f ? 1.2f : 0.8f)),
                Projectile.owner,
                0f,
                chargeLevel,
                (float)CurrentMark);
            PFLeftEffectRules.ApplyTheme(fireballIndex, CurrentMark);

            ApplyRecoil(chargeLevel == 3f ? 24f : (chargeLevel == 2f ? 16f : 8f));
            TriggerMuzzleFlash(chargeLevel == 3f ? 28 : (chargeLevel == 2f ? 18 : 10));

            Color burstColor = PristineFuryMarkHelper.GetColor(CurrentMark);
            SpawnMuzzleBurst(burstColor, chargeLevel == 3f ? 1.8f : (chargeLevel == 2f ? 1.2f : 0.7f));
            rightCooldownTimer = RightChargeCooldown;
            Owner.SetScreenshake(chargeLevel == 3f ? 8f : (chargeLevel == 2f ? 4f : 2f));

            if (chargeLevel == 3f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot") { PitchVariance = 0.15f, Volume = 0.85f }, muzzle);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserBigShot") { PitchVariance = 0.15f, Volume = 0.7f }, muzzle);
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserBigShot") { PitchVariance = 0.22f, Volume = 0.82f }, muzzle);
            }
        }

        private void ResetRightCharge()
        {
            rightChargeTimer = 0;

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

            Vector2 vibrationOffset = Vector2.Zero;
            if (rightChargeTimer > 300)
            {
                float vibProgress = Math.Min(1f, (rightChargeTimer - 300f) / 60f);
                float maxVib = 6f;
                vibrationOffset = Main.rand.NextVector2Circular(maxVib * vibProgress, maxVib * vibProgress);
            }

            Projectile.Center = armPosition + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir) + vibrationOffset;

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

            Vector2 vibrationOffset = Vector2.Zero;
            if (rightChargeTimer > 300)
            {
                float vibProgress = Math.Min(1f, (rightChargeTimer - 300f) / 60f);
                float maxVib = 6f;
                vibrationOffset = Main.rand.NextVector2Circular(maxVib * vibProgress, maxVib * vibProgress);
            }

            Projectile.Center = armPosition + aim * (HoldoutDistance + 8f - recoilOffset) + armOffset + vibrationOffset;

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

        internal int GetScaledDamage(float multiplier) => GetScaledDamage(multiplier, CurrentMark);

        internal int GetScaledDamage(float multiplier, PristineFuryMark mark) =>
            Math.Max(1, (int)(Projectile.damage * PF_Balance.GetLeftClickMarkDamageMultiplier(mark) * multiplier));

        internal int GetRightScaledDamage(float multiplier) => Math.Max(1, (int)(Projectile.damage * multiplier));

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

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                muzzle + direction * 2f,
                direction * 1.8f,
                false,
                16,
                0.46f * scale,
                Color.Lerp(color, Color.White, 0.42f),
                true,
                false,
                true));
        }

        private void SpawnHookChargeEffects(float charge)
        {
            if (Main.dedServ)
                return;

            Vector2 center = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.48f);
            Color color = Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, charge * 0.55f);
            Lighting.AddLight(center, color.ToVector3() * (0.25f + charge * 0.55f));

            if (charge >= 0.98f && Main.rand.NextBool(18))
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.24f, Pitch = 0.55f, MaxInstances = 2 }, center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            bool hasGlow = ModContent.RequestIfExists(Texture + "_Glow", out ReLogic.Content.Asset<Texture2D> glowAsset);
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
            if (hasGlow)
                Main.EntitySpriteDraw(glowAsset.Value, drawPosition, frame, (Color.White with { A = 0 }) * (0.45f + flash), Projectile.rotation, origin, Projectile.scale, effects, 0);
            DrawDragonEyeGlow(leftVisualPower);
            DrawDragonMouthSmoke(leftVisualPower);
            DrawFakeCalamityArcNovaCharge();
            DrawRightArcNovaCharge();
            DrawHookChargeBar();
            return false;
        }

        private void DrawDragonMouthSmoke(float power)
        {
            if (power <= 0.025f || Main.dedServ)
                return;

            Texture2D magic = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthMagicTexturePath()).Value;
            Texture2D smoke = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthSmokeTexturePath()).Value;
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

            Texture2D bloom = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeBloomTexturePath()).Value;
            Texture2D smear = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeSmearTexturePath()).Value;
            Texture2D ring = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeRingTexturePath()).Value;
            Vector2 direction = AimDirection;
            Vector2 tip = DragonMouthPosition + direction * charge * NewLegendPristineFuryHoldOut_DragonDrawData.FakeCalamityChargeForwardTravelFromMouth - Main.screenPosition;
            Color theme = (Color.Lerp(PristineFuryMarkHelper.GetColor(CurrentMark), Color.White, charge * 0.32f) with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float chargeScale = charge * 1.3f;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 6; i++)
            {
                Vector2 place = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 7f) * charge;
                Vector2 drawPosition = tip + place - Vector2.Lerp(place, -direction, 0.9f) * Main.rand.NextFloat(18f, 42f) + direction * -8f * (6f - chargeScale * 2f);
                Vector2 smearScale = new(0.18f * chargeScale, (1.2f + (Main.rand.NextBool(4) ? 1.3f : 0f)) * 0.055f * chargeScale);
                Main.EntitySpriteDraw(smear, drawPosition, null, theme * 0.86f, direction.RotatedByRandom(0.26f).ToRotation() - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), smearScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 3; i++)
            {
                Color layerColor = Color.Lerp(theme, white, i * 0.22f);
                Vector2 layerScale = new Vector2(1.42f, 1.02f) * chargeScale * (1f - i * 0.24f) * 0.16f * pulse;
                Main.EntitySpriteDraw(bloom, tip, null, layerColor * (0.88f - i * 0.13f), Projectile.rotation, bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(ring, tip, null, theme * (0.24f + charge * 0.15f), Projectile.rotation + Main.GlobalTimeWrappedHourly * 0.85f, ring.Size() * 0.5f, (0.1f + charge * 0.32f) * pulse, SpriteEffects.None, 0f);

            if (charge >= 0.98f)
            {
                for (int satellite = 0; satellite < 3; satellite++)
                {
                    Vector2 orbit = (MathHelper.TwoPi * satellite / 3f + Main.GlobalTimeWrappedHourly * 7.2f).ToRotationVector2();
                    Vector2 offset = new Vector2(orbit.X * 0.72f, orbit.Y * 1.18f).RotatedBy(Projectile.rotation) * chargeScale * 9f;
                    Main.EntitySpriteDraw(bloom, tip + offset, null, Color.Lerp(theme, white, 0.45f) * 0.82f, Projectile.rotation, bloom.Size() * 0.5f, chargeScale * 0.055f, SpriteEffects.None, 0f);
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

            Texture2D bloom = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeBloomTexturePath()).Value;
            Texture2D smear = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeSmearTexturePath()).Value;
            Texture2D ring = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonMouthChargeRingTexturePath()).Value;
            Vector2 direction = AimDirection;
            Vector2 tip = DragonMouthPosition + direction * charge * NewLegendPristineFuryHoldOut_DragonDrawData.RightArcNovaChargeForwardTravelFromMouth - Main.screenPosition;
            Color themeColor = PristineFuryMarkHelper.GetColor(CurrentMark);
            Color fire = (Color.Lerp(themeColor, Color.White, 0.15f) with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float chargeScale = Math.Min(charge * 1.35f, 1.35f);
            float pulse = 0.9f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 13f + Projectile.identity);

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < 10; i++)
            {
                Vector2 place = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.5f, 5f) * charge;
                Vector2 drawPosition = tip + place - Vector2.Lerp(place, -direction, 0.9f) * Main.rand.NextFloat(12f, 31f) + direction * -4f * (6f - chargeScale * 2f);
                Vector2 smearScale = new(0.11f * chargeScale, (1.5f + (Main.rand.NextBool(3) ? 1.6f : 0f)) * 0.0275f * chargeScale);
                Color smearColor = Main.rand.NextBool(4) ? white : fire;
                Main.EntitySpriteDraw(smear, drawPosition, null, smearColor * 0.94f, direction.RotatedByRandom(0.168f).ToRotation() - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), smearScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 4; i++)
            {
                Color layerColor = Color.Lerp(fire, white, i * 0.25f);
                Vector2 layerScale = new Vector2(1.58f, 1.08f) * chargeScale * (1f - 0.21f * i) * 0.08f * pulse;
                Main.EntitySpriteDraw(bloom, tip, null, layerColor * 0.9f, Projectile.rotation + Main.rand.NextFloat(-5f, 5f), bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(ring, tip, null, fire * (0.26f + charge * 0.32f), Projectile.rotation + Main.GlobalTimeWrappedHourly * 1.25f, ring.Size() * 0.5f, (0.06f + charge * 0.2f) * pulse, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(
                smear,
                tip + direction * (12f + charge * 10f),
                null,
                Color.Lerp(fire, white, 0.36f) * (0.52f + charge * 0.36f),
                direction.ToRotation() - MathHelper.PiOver2,
                new Vector2(smear.Width * 0.5f, smear.Height),
                new Vector2(0.175f + charge * 0.14f, 0.06f + charge * 0.04f),
                SpriteEffects.None,
                0f);

            if (rightChargeTimer >= RightChargeMaxFrames)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 angle = (MathHelper.TwoPi * i / 3f).ToRotationVector2().RotatedBy(Main.GlobalTimeWrappedHourly * 7.2f);
                    Vector2 offset = new Vector2(angle.X * 0.7f, angle.Y * 1.2f).RotatedBy(Projectile.rotation) * chargeScale * 4f;
                    for (int layer = 0; layer < 2; layer++)
                    {
                        Color layerColor = Color.Lerp(fire, white, layer) * 0.8f;
                        float layerScale = chargeScale * (1f - 0.25f * layer) * 0.0275f;
                        Main.EntitySpriteDraw(bloom, tip + offset, null, layerColor, Main.rand.NextFloat(-5f, 5f), bloom.Size() * 0.5f, layerScale, SpriteEffects.None, 0f);
                    }
                }
            }

            PFLeftEffectRules.EndAdditive();
        }

        private void DrawDragonEyeGlow(float power)
        {
            if (power <= 0.025f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeBloomTexturePath()).Value;
            Texture2D halfStar = ModContent.Request<Texture2D>(NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeHalfStarTexturePath()).Value;
            Vector2 eye = DragonEyePosition - Main.screenPosition;
            Color theme = PristineFuryMarkHelper.GetColor(CurrentMark);
            Color coreWhite = Color.Lerp(theme, Color.White, 0.45f);
            float manaPower = MathHelper.Clamp(power, 0f, 1f);
            float reverseManaPower = MathHelper.Lerp(0.7f, 0.1f, manaPower > 0f ? 1f - manaPower : 0.5f);
            Vector2 shake = Main.rand.NextVector2Circular(2f, 2f) * manaPower;
            SpriteEffects flipSprite = Projectile.spriteDirection * Owner.gravDir == -1f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            int layerCount = NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeLayerCount();
            float sizeMultiplier = NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeSizeMultiplier();
            float brightnessMultiplier = NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeBrightnessMultiplier();

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < layerCount; i++)
            {
                float iMult = 1f - 0.1f * i;
                Color layerColor = (Color.Lerp(theme, coreWhite, i * 0.1f) with { A = 0 }) * power * brightnessMultiplier;

                Main.EntitySpriteDraw(
                    bloom,
                    eye + shake,
                    null,
                    layerColor,
                    Main.rand.NextFloat(-5f, 5f),
                    bloom.Size() * 0.5f,
                    new Vector2(1f, 0.35f)
                        * NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeBloomScale()
                        * manaPower
                        * Main.rand.NextFloat(0.7f, 1.3f)
                        * iMult
                        * sizeMultiplier,
                    flipSprite,
                    0);

                for (int b = -1; b <= 1; b += 2)
                {
                    float sine = MathHelper.Lerp(
                        (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f / MathHelper.Pi),
                        reverseManaPower * b,
                        0.75f);
                    Vector2 starScale = new Vector2(0.3f, 1f * sine * b)
                        * (Main.rand.NextFloat(
                            NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeHalfStarMinScale(),
                            NewLegendPristineFuryHoldOut_DragonDrawData.DragonEyeHalfStarMaxScale()) * iMult + manaPower * 1.2f)
                        * sizeMultiplier;
                    float rotation = Projectile.rotation
                        + dragonEyeTimer * manaPower * Math.Max(i - 2, 0) * 0.2f
                        + MathHelper.PiOver4 * b;

                    Main.EntitySpriteDraw(
                        halfStar,
                        eye,
                        null,
                        layerColor,
                        rotation,
                        halfStar.Size() * 0.5f,
                        starScale,
                        flipSprite,
                        0);
                }
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

        // 灵梦FOMO：每 60 帧从未选中的印记方向各发射一颗低威力残响火球。
        private const int FOMOEchoInterval = 60;
        private void HandleFOMOEchoes()
        {
            if (!Owner.GetModPlayer<PFAccessoryPlayer>().LingmuFOMOEquipped)
                return;

            fomoEchoTimer++;
            if (fomoEchoTimer < FOMOEchoInterval)
                return;

            fomoEchoTimer = 0;

            PristineFuryPlayer pfPlayer = Owner.GetModPlayer<PristineFuryPlayer>();
            if (pfPlayer.MarkQueueCount < 2)
                return;

            int idleFlameType = ModContent.ProjectileType<PFIdle_Flame>();
            Vector2 muzzle = GunTipPosition;

            for (int i = 0; i < pfPlayer.MarkQueueCount; i++)
            {
                if (i == pfPlayer.SelectedMarkIndex)
                    continue;

                PristineFuryMark echMark = pfPlayer.MarkQueue[i];
                Color echColor = PristineFuryMarkHelper.GetColor(echMark);

                float spread = Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 vel = AimDirection.RotatedBy(spread) * 14f;

                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    muzzle,
                    vel,
                    idleFlameType,
                    GetScaledDamage(0.35f, echMark),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner,
                    i);
                PFLeftEffectRules.ApplyTheme(idx, echMark);
            }

            SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(CurrentMark), 0.5f);
        }
    }
}
