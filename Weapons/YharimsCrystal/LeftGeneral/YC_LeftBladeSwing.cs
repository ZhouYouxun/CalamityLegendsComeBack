using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_LeftBladeSwing : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        private readonly BalanceYharimsCrystal balance = new();

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";
        public override int AssignedItemID => ModContent.ItemType<NewLegendYharimsCrystal>();
        public override Vector2 SpriteOrigin => new(0f, 186f);
        public override float HitboxOutset => 132f * Projectile.scale;
        public override Vector2 HitboxSize => new Vector2(288f, 288f) * Projectile.scale;
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private static readonly Color BladeGold = new(255, 214, 88);
        private static readonly Color BladeOrange = new(255, 111, 34);
        private static readonly Color BladeWhite = new(255, 246, 196);

        private const int LoopChargeFrames = 40;
        private const int LoopSpinFrames = 60;
        private const int LoopHoldFrames = 40;
        private const int JudgementRaiseFrames = 60;
        private const int MaxBladeHitFireballs = 5;
        private const int StateLoopCharge = 0;
        private const int StateLoopSpin = 1;
        private const int StateLoopHold = 2;
        private const int StateJudgementRaise = 3;

        private int leftClickState = 0;
        private int loopTimer;
        private int chargeTimer;
        private bool chargeCompleteSoundPlayed = false;
        private bool spinStartSoundPlayed;
        private bool judgementRequested;
        private bool releasedDuringLoopHold;
        private int judgementTargetIndex = -1;
        private int bladeHitFireballsThisSpin;
        private float spinAngle;
        private float chargeBorderIntensity;

        private int currentStage;
        private int lockedFacing = 1;
        private bool postSwing;
        private float fadeIn;
        private float bladeFade;
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection = Vector2.UnitX;

        private bool Empowered => Owner.GetModPlayer<YharimsCrystalStatePlayer>().BladeEmpowered;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 0;
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;
            Projectile.scale = balance.GetLeftBladeScale();
            Owner.GetModPlayer<YharimsCrystalStatePlayer>().SetLastWeapon(YCWeaponForm.Blade);
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);
            UpdateLockedAimFromMouse();
            Projectile.ai[1] = -lockedFacing;
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.72f, Pitch = -0.18f }, Owner.Center);
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                WhenSpawned();
                whenSpawned = false;
                Projectile.netUpdate = true;
            }

            if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssignedItemID)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Animation++;
            UseStyle();
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => CanHit ? null : false;

        private bool IsRightHeld()
        {
            return (Main.mouseRight || Owner.Calamity().mouseRight || Owner.controlUseTile) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        public override void UseStyle()
        {
            bool leftHeld = IsLeftHeld();
            if (!leftHeld)
            {
                if (leftClickState == StateLoopHold)
                    releasedDuringLoopHold = true;
                else if (leftClickState != StateJudgementRaise)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (leftHeld && IsRightHeld())
                judgementRequested = true;

            switch (leftClickState)
            {
                case StateLoopCharge:
                    DoLoopCharge();
                    break;
                case StateLoopSpin:
                    DoLoopSpin();
                    break;
                case StateLoopHold:
                    DoLoopHold();
                    break;
                case StateJudgementRaise:
                    DoJudgementRaise();
                    break;
            }

            if (leftClickState != StateJudgementRaise)
                ApplyArmRotation();
        }

        private void DoLoopCharge()
        {
            loopTimer++;
            UpdateLockedAimFromMouse();
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;
            Projectile.ai[1] = -lockedFacing;

            float progress = MathHelper.Clamp(loopTimer / (float)LoopChargeFrames, 0f, 1f);
            CanHit = false;
            postSwing = false;
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.25f);
            bladeFade = MathHelper.Lerp(bladeFade, MathHelper.Lerp(0.35f, 1f, progress), 0.16f);
            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, progress, 0.22f);
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.12f);
            RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(112f * Projectile.ai[1]), 0.2f);

            if (loopTimer == 1)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.44f, Pitch = -0.18f }, Owner.Center);

            EmitChargeSparks();

            if (loopTimer >= LoopChargeFrames)
                StartLoopSpin();
        }

        private void StartLoopSpin()
        {
            leftClickState = StateLoopSpin;
            loopTimer = 0;
            spinAngle = 0f;
            spinStartSoundPlayed = false;
            chargeCompleteSoundPlayed = false;
            releasedDuringLoopHold = false;
            bladeHitFireballsThisSpin = 0;
            currentStage = 2;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.62f, Pitch = -0.18f }, Owner.Center);

            if (!Main.dedServ)
            {
                Vector2 edge = GetBladeTipPosition();
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(edge, Vector2.Zero, BladeGold, Vector2.One, FinalRotation, 0.08f, 1.65f, 18));
            }
        }

        private void DoLoopSpin()
        {
            loopTimer++;
            UpdateLockedAimFromMouse();
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;

            float progress = MathHelper.Clamp(loopTimer / (float)LoopSpinFrames, 0f, 1f);
            float spinSpeed = GetSpinSpeed(progress);
            spinAngle -= MathHelper.TwoPi * 3.45f / LoopSpinFrames * spinSpeed;

            CanHit = loopTimer > 4 && loopTimer < LoopSpinFrames - 2;
            postSwing = true;
            fadeIn = MathHelper.Lerp(fadeIn, 1f, 0.24f);
            bladeFade = MathHelper.Lerp(bladeFade, 1f, 0.28f);
            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, MathHelper.Lerp(1f, 0.35f, progress), 0.2f);
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.06f);
            RotationOffset = MathHelper.ToRadians(112f * Projectile.ai[1]) + spinAngle * Projectile.ai[1];

            if (!spinStartSoundPlayed && loopTimer >= 6)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellkiteSwing", 2) { Volume = 0.82f, Pitch = -0.08f }, Projectile.Center);
                spinStartSoundPlayed = true;
            }

            if (loopTimer % 12 == 0)
                Projectile.ResetLocalNPCHitImmunity();

            if (loopTimer % 5 == 0 && Projectile.owner == Main.myPlayer && YC_EssenceFlame.CanSpawnMoreFor(Owner))
                SpawnSpinFlame();

            SpawnSwingParticles(new StageProfile(LoopSpinFrames, 0, 0f, 1.08f));

            if (loopTimer >= LoopSpinFrames)
            {
                if (judgementRequested)
                    StartJudgementRaise();
                else
                    StartLoopHold();
            }
        }

        private void StartLoopHold()
        {
            leftClickState = StateLoopHold;
            loopTimer = 0;
            CanHit = false;
            postSwing = true;
            chargeBorderIntensity = Math.Max(chargeBorderIntensity, 0.45f);
            // 归一化累积旋转量，防止Lerp期间刀视觉上疯狂旋转
            RotationOffset = MathHelper.WrapAngle(RotationOffset);
        }

        private void DoLoopHold()
        {
            loopTimer++;
            UpdateLockedAimFromMouse();
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;

            float progress = MathHelper.Clamp(loopTimer / (float)LoopHoldFrames, 0f, 1f);
            CanHit = false;
            postSwing = true;
            fadeIn = MathHelper.Lerp(fadeIn, 0.85f, 0.12f);
            bladeFade = MathHelper.Lerp(bladeFade, MathHelper.Lerp(0.85f, 1f, progress), 0.16f);
            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, MathHelper.Lerp(0.6f, 0.9f, progress), 0.18f);
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.12f);
            float idleSwing = MathF.Sin(loopTimer * 0.18f) * MathHelper.ToRadians(7f);
            // 归一化后用AngleLerp，避免Lerp大值引起的视觉旋转
            RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(112f * Projectile.ai[1]) + idleSwing, 0.15f);

            EmitChargeSparks();
            if (loopTimer == LoopHoldFrames - 8 && !chargeCompleteSoundPlayed)
            {
                chargeCompleteSoundPlayed = true;
                SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.55f, Pitch = 0.08f }, Projectile.Center);
            }

            // 停顿期间右键可以直接丢出
            if (judgementRequested)
            {
                StartJudgementRaise();
                return;
            }

            if (loopTimer >= LoopHoldFrames)
            {
                if (releasedDuringLoopHold)
                {
                    Projectile.Kill();
                    return;
                }

                // 停顿结束后回到蓄力阶段，而不是直接进入旋转
                StartLoopCharge();
            }
        }

        private void StartLoopCharge()
        {
            leftClickState = StateLoopCharge;
            loopTimer = 0;
            chargeCompleteSoundPlayed = false;
            releasedDuringLoopHold = false;
        }

        private void StartJudgementRaise()
        {
            leftClickState = StateJudgementRaise;
            loopTimer = 0;
            chargeTimer = 0;
            CanHit = false;
            postSwing = true;
            fadeIn = 1f;
            bladeFade = 1f;
            chargeBorderIntensity = 1f;
            chargeCompleteSoundPlayed = false;
            RotationOffset = MathHelper.WrapAngle(RotationOffset);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.28f }, Owner.Center);
        }

        private void DoJudgementRaise()
        {
            loopTimer++;
            chargeTimer++;
            CanHit = false;
            postSwing = true;
            float raiseProgress = MathHelper.Clamp(loopTimer / (float)JudgementRaiseFrames, 0f, 1f);
            // 举刀过程刀身完全可见并增亮，体现"拿在手上蓄力"
            fadeIn = MathHelper.Lerp(fadeIn, 1.0f, 0.15f);
            bladeFade = MathHelper.Lerp(bladeFade, 1.25f + raiseProgress * 0.15f, 0.08f);
            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, 1.4f, 0.08f);
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;
            Projectile.rotation = Projectile.rotation.AngleLerp(-MathHelper.PiOver2, 0.18f);
            RotationOffset = RotationOffset.AngleLerp(0f, 0.18f);
            ArmRotationOffset = MathHelper.ToRadians(-92f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-92f);

            // 蓄力阶段屏幕持续轻微震动，越到后期越强
            if (loopTimer % 8 == 0)
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.5f + raiseProgress * 3f);

            EmitJudgementChargeEffects();

            if (loopTimer >= JudgementRaiseFrames)
            {
                LaunchJudgementBlade();
                Projectile.Kill();
            }
        }

        private float GetSpinSpeed(float progress)
        {
            if (progress < 0.16f)
            {
                float accel = progress / 0.16f;
                return MathHelper.SmoothStep(0.35f, 1.65f, accel);
            }

            float decay = (progress - 0.16f) / 0.84f;
            return MathHelper.Lerp(1.65f, 0.24f, 1f - MathF.Pow(1f - decay, 2.15f));
        }

        private bool HasValidJudgementTarget()
        {
            if (judgementTargetIndex < 0 || judgementTargetIndex >= Main.maxNPCs)
                return false;

            NPC target = Main.npc[judgementTargetIndex];
            return target.active && target.CanBeChasedBy(Projectile);
        }

        private void SpawnSpinFlame()
        {
            float orbitPhase = YC_EssenceFlame.NextOrbitPhaseFor(Owner);
            float orbitAngle = YC_EssenceFlame.GetOrbitAngle(orbitPhase, 0f, Owner.direction);
            Vector2 orbitDirection = orbitAngle.ToRotationVector2();
            Vector2 tangent = orbitDirection.RotatedBy(MathHelper.PiOver2 * (Owner.direction >= 0 ? 1f : -1f));
            Vector2 spawnPosition = Owner.Center + orbitDirection * (138f * Projectile.scale);
            Vector2 fireDirection = (tangent * 0.7f + orbitDirection * 0.3f).SafeNormalize(Vector2.UnitX * Owner.direction);

            int flame = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                fireDirection * Main.rand.NextFloat(15f, 21f),
                ModContent.ProjectileType<YC_EssenceFlame>(),
                (int)(Projectile.damage * 0.7f),
                Projectile.knockBack * 0.3f,
                Projectile.owner,
                -1f,
                orbitPhase);

            if (Main.projectile.IndexInRange(flame))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[flame], YCWeaponForm.Blade);
                Main.projectile[flame].CritChance = Projectile.CritChance;
            }
        }

        private Vector2 GetBladeTipPosition()
        {
            Vector2 tipDirection = (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            return Owner.Center + tipDirection * (240f * Projectile.scale);
        }

        private void EmitJudgementChargeEffects()
        {
            if (Main.dedServ)
                return;

            float charge = MathHelper.Clamp(loopTimer / (float)JudgementRaiseFrames, 0f, 1f);
            Vector2 tip = Owner.Center - Vector2.UnitY * (150f * Projectile.scale);

            if (loopTimer % Math.Max(2, (int)MathHelper.Lerp(7f, 2f, charge)) == 0)
            {
                Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(80f, 190f) * (1f - charge * 0.45f);
                Vector2 position = tip + orbit + Main.rand.NextVector2Circular(16f, 16f);
                Vector2 velocity = (tip - position).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3f, 8f + charge * 6f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position,
                    velocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.55f, 1.05f) * (0.75f + charge),
                    Main.rand.NextBool(3) ? BladeWhite : BladeGold,
                    new Vector2(0.26f, 1.15f + charge * 0.45f),
                    true,
                    true,
                    shrinkSpeed: 0.18f));
            }

            if (loopTimer % 15 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    tip,
                    Vector2.Zero,
                    Color.Lerp(BladeOrange, BladeGold, charge),
                    Vector2.One,
                    -MathHelper.PiOver2,
                    0.06f,
                    1.2f + charge * 0.9f,
                    18));
            }

            if (loopTimer % 20 == 0)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.18f + charge * 0.2f, Pitch = -0.4f + charge * 0.16f, MaxInstances = 4 }, tip);
        }

        private void LaunchJudgementBlade()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int targetIndex = HasValidJudgementTarget() ? judgementTargetIndex : -1;
            int thrown = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center - Vector2.UnitY * 72f,
                -Vector2.UnitY * 28f,
                ModContent.ProjectileType<YC_ThrownBlade>(),
                (int)(Projectile.damage * 1.85f),
                Projectile.knockBack * 1.5f,
                Projectile.owner,
                2f,
                targetIndex);

            if (Main.projectile.IndexInRange(thrown))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[thrown], YCWeaponForm.Blade);
                Main.projectile[thrown].CritChance = Projectile.CritChance;
            }

            // 三板斧：强烈屏幕震动
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 7.5f);

            // 三板斧：抛出时大量粒子爆发
            if (!Main.dedServ)
            {
                Vector2 launchPos = Owner.Center - Vector2.UnitY * 72f;
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(launchPos, Vector2.Zero, BladeGold, Vector2.One, -MathHelper.PiOver2, 0.12f, 2.4f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(launchPos, Vector2.Zero, BladeOrange * 0.9f, "CalamityMod/Particles/BloomCircle", Vector2.One, 0f, 0.18f, 1.1f, 14, true));
                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 18f);
                    Dust d = Dust.NewDustPerfect(launchPos + Main.rand.NextVector2Circular(18f, 18f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(1.0f, 1.6f));
                    d.noGravity = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1f, Pitch = -0.35f }, Owner.Center);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.12f }, Owner.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnHitEffects(target);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.2f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FinalDawnSlash") { Volume = 0.62f, Pitch = Main.rand.NextFloat(0.08f, 0.24f) }, target.Center);

            judgementTargetIndex = target.whoAmI;
            if (IsRightHeld())
                judgementRequested = true;

            if (Projectile.owner == Main.myPlayer &&
                leftClickState == StateLoopSpin &&
                bladeHitFireballsThisSpin < MaxBladeHitFireballs &&
                YC_BurningShard.CanSpawnFollowFireballFor(Owner))
            {
                int orbitSlot = YC_BurningShard.NextFollowOrbitSlotFor(Owner);
                int shard = Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<YC_BurningShard>(),
                    (int)(Projectile.damage * 0.85f),
                    Projectile.knockBack * 0.2f,
                    Projectile.owner,
                    2f, // Follow player
                    orbitSlot);
                if (Main.projectile.IndexInRange(shard))
                {
                    bladeHitFireballsThisSpin++;
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[shard], YCWeaponForm.Crystal);
                    Main.projectile[shard].CritChance = Projectile.CritChance;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float falloff = Utils.Remap(Projectile.numHits, 0f, 7f, 1.18f, 0.64f, true);
            modifiers.SourceDamage *= falloff;
        }

        private void UpdateLockedAimFromMouse()
        {
            lockedMouseWorld = NewLegendYharimsCrystal.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            lockedFacing = lockedAimDirection.X >= 0f ? 1 : -1;
        }

        private float GetAimRotation() =>
            Owner.AngleTo(lockedMouseWorld) + MathHelper.ToRadians(lockedFacing < 0 ? 0f : 120f);

        private bool IsLeftHeld()
        {
            return Owner.channel &&
                (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        private void ApplyArmRotation()
        {
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void EmitChargeSparks()
        {
            if (Main.dedServ || !Main.rand.NextBool(Empowered ? 2 : 5))
                return;

            Vector2 position = Owner.Center + lockedAimDirection.RotatedByRandom(0.72f) * Main.rand.NextFloat(32f, 120f);
            Vector2 velocity = lockedAimDirection.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.6f, 2.8f);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                position,
                velocity,
                "CalamityMod/Particles/Sparkle",
                false,
                Main.rand.Next(14, 22),
                Main.rand.NextFloat(0.62f, 1.1f),
                Main.rand.NextBool(3) ? BladeWhite : BladeGold,
                new Vector2(0.28f, 1f),
                true,
                true,
                shrinkSpeed: 0.18f));
        }

        private void SpawnSwingParticles(StageProfile profile)
        {
            if (Main.dedServ)
                return;

            Vector2 slashDirection = (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 tangent = slashDirection.RotatedBy(MathHelper.PiOver2 * Math.Sign(Projectile.ai[1] == 0f ? 1f : Projectile.ai[1]));
            float reach = 260f * Projectile.scale * profile.ParticleReach;

            for (int i = 0; i < (currentStage == 2 ? 7 : 4); i++)
            {
                Vector2 position = Owner.Center + slashDirection * Main.rand.NextFloat(48f, reach) + tangent * Main.rand.NextFloat(-16f, 24f);
                Vector2 velocity = tangent * Main.rand.NextFloat(3.6f, 9f) + slashDirection * Main.rand.NextFloat(0.4f, 2.2f);
                Color color = Main.rand.NextBool(4) ? BladeWhite : Color.Lerp(BladeOrange, BladeGold, Main.rand.NextFloat(0.2f, 0.9f));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.05f, 0.09f) * Projectile.scale,
                    color,
                    new Vector2(1.4f, 0.22f),
                    true,
                    false,
                    0.8f));
            }
        }

        private void SpawnHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = lockedAimDirection.RotatedByRandom(0.62f) * Main.rand.NextFloat(5f, 18f);
                Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, BladeGold * 0.8f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.12f, 0.72f, 16, true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> ghost = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/EarthGhost");
            Asset<Texture2D> glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/EarthGlow");
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");

            float swordRotation = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Vector2 origin = FlipAsSword ? new Vector2(texture.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Color aura = Color.Lerp(BladeOrange, BladeGold, 0.55f) with { A = 0 };

            if (CanHit || postSwing)
            {
                Main.EntitySpriteDraw(
                    swoosh.Value,
                    drawPosition,
                    null,
                    aura * fadeIn * (currentStage == 2 ? 0.9f : 0.64f),
                    FinalRotation + MathHelper.ToRadians(45f) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -82f : 82f) * -Owner.direction,
                    swoosh.Size() * 0.5f,
                    Projectile.scale * (currentStage == 2 ? 1.08f : 0.82f),
                    SpriteEffects.None);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 4.8f * fadeIn;
                Main.EntitySpriteDraw(
                    ghost.Value,
                    drawPosition + offset,
                    null,
                    aura * 0.13f * fadeIn,
                    Projectile.rotation + RotationOffset + swordRotation,
                    origin,
                    Projectile.scale,
                    effects);
            }

            float chargeOutline = MathHelper.Clamp(chargeBorderIntensity, 0f, 1.4f);
            if (chargeOutline > 0.02f)
            {
                Color outlineColor = Color.Lerp(BladeOrange, BladeWhite, MathHelper.Clamp(chargeOutline / 1.4f, 0f, 1f)) with { A = 0 };
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * (4f + chargeOutline * 5f);
                    Main.EntitySpriteDraw(
                        glow.Value,
                        drawPosition + offset,
                        null,
                        outlineColor * 0.18f * chargeOutline,
                        Projectile.rotation + RotationOffset + swordRotation,
                        origin,
                        Projectile.scale * (1f + chargeOutline * 0.035f),
                        effects);
                }

                Vector2 tipDrawPosition = GetBladeTipPosition() - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
                float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 16f);
                Main.EntitySpriteDraw(
                    bloom.Value,
                    tipDrawPosition,
                    null,
                    outlineColor * 0.42f * chargeOutline,
                    0f,
                    bloom.Size() * 0.5f,
                    Projectile.scale * (0.32f + chargeOutline * 0.24f) * pulse,
                    SpriteEffects.None);

                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offsetDir = Vector2.One.RotatedBy(Projectile.rotation + RotationOffset + MathHelper.ToRadians(90f));
                bool tip = i > 13;
                float tipScale = tip ? Utils.Remap(i, 13f, 18f, 0.85f, 0.34f) : 1f;
                Vector2 drawOffset = -offsetDir * 8f * i * bladeFade;
                Main.EntitySpriteDraw(
                    bloom.Value,
                    Projectile.Center - offsetDir * 68f - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
                    null,
                    Color.Lerp(BladeGold, BladeWhite, 0.28f) with { A = 0 } * 0.28f * bladeFade,
                    RotationOffset + Projectile.rotation + MathHelper.ToRadians(45f),
                    bloom.Size() * 0.5f,
                    new Vector2(0.58f * tipScale, 1f) * 0.42f * tipScale * Projectile.scale * bladeFade,
                    effects);
            }

            Main.EntitySpriteDraw(texture.Value, drawPosition, null, lightColor, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(glow.Value, drawPosition, null, BladeGold * 0.8f, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            return false;
        }

        private readonly struct StageProfile
        {
            public readonly int Duration;
            public readonly int GapFrames;
            public readonly float Windup;
            public readonly float ParticleReach;

            public StageProfile(int duration, int gapFrames, float windup, float particleReach)
            {
                Duration = duration;
                GapFrames = gapFrames;
                Windup = windup;
                ParticleReach = particleReach;
            }
        }
    }
}
