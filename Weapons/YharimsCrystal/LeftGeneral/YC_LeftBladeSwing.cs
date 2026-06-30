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

        // Earth swing fields — copied exactly
        public Vector2 mousePos;
        public Vector2 aimVel;
        public bool doSwing = true;
        public bool postSwing = false;
        public float fadeIn = 0;
        public int useAnim;
        public int swingCount;
        public bool playSwingSound = true;
        public bool allowSecondHit = true;
        public float bladeFade = 0;
        public int armoredHits = 0;
        public bool finalFlip = false;
        public int pause = 0;

        // Throw fields
        private const int ThrowWindupFrames = 30;
        private bool inThrowWindup = false;
        private int throwTimer = 0;
        private bool throwRequested;
        private int throwTargetIndex = -1;
        private float throwStartRotationOffset;
        private float chargeBorderIntensity;
        private int bladeHitFireballsThisSpin;
        private const int MaxBladeHitFireballs = 5;

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

            // Earth's WhenSpawned — copied exactly
            Projectile.knockBack = 0;
            Projectile.ai[1] = 1;
            mousePos = Owner.Calamity().mouseWorld;
            aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;
            useAnim = Owner.itemAnimationMax * 2;

            if (mousePos.X < Owner.Center.X) Owner.direction = -1;
            else Owner.direction = 1;

            FlipAsSword = Owner.direction == -1 ? true : false;

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

        private bool IsLeftHeld()
        {
            return Owner.channel &&
                (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        private bool IsRightHeld()
        {
            return (Main.mouseRight || Owner.Calamity().mouseRight || Owner.controlUseTile) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        public override void UseStyle()
        {
            if (inThrowWindup)
            {
                DoThrowWindup();
                return;
            }

            bool leftHeld = IsLeftHeld();
            if (!leftHeld)
            {
                Projectile.Kill();
                return;
            }

            if (leftHeld && IsRightHeld())
                throwRequested = true;

            // ── Earth's UseStyle — copied exactly ──────────────────────────────
            if (pause > 0)
            {
                pause--;
                Animation--;
                return;
            }

            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = false;

            if (CanHit || postSwing)
                mousePos = Owner.Center - aimVel;
            else
                mousePos = Owner.Calamity().mouseWorld;

            if (CanHit)
                fadeIn = MathHelper.Lerp(fadeIn, 1, 0.2f);
            else
                fadeIn = MathHelper.Lerp(fadeIn, 0, 0.28f);

            if (Projectile.ai[1] == -1)
                bladeFade = MathHelper.Lerp(bladeFade, 1, 0.15f);
            else
                bladeFade = MathHelper.Lerp(bladeFade, 0, 0.045f);

            if (!doSwing)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                    Projectile.localNPCImmunity[i] = 0;

                allowSecondHit = true;
                playSwingSound = true;
                Projectile.numHits = 0;
                bladeHitFireballsThisSpin = 0;
                mousePos = Owner.Calamity().mouseWorld;
                aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;
                CanHit = false;
                if (mousePos.X < Owner.Center.X) Owner.direction = -1;
                else Owner.direction = 1;
                FlipAsSword = Owner.direction == -1 ? true : false;
                if (swingCount % 2 == 0)
                    useAnim = Owner.itemAnimationMax * 2;
                else
                    useAnim = Owner.itemAnimationMax;

                doSwing = true;
                finalFlip = false;
                armoredHits = 0;
            }
            else
            {
                if (!CanHit && !postSwing)
                {
                    if (mousePos.X < Owner.Center.X) Owner.direction = -1;
                    else Owner.direction = 1;
                }
                else
                {
                    if ((Owner.Center - aimVel).X < Owner.Center.X) Owner.direction = -1;
                    else Owner.direction = 1;
                }

                Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f), 0.1f);
                if (AnimationProgress < (useAnim / (swingCount % 2 == 0 ? 1.3f : 7)))
                {
                    aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;
                    CanHit = false;
                    postSwing = false;
                    if (AnimationProgress == 0)
                    {
                        Animation = 0;
                        doSwing = false;
                        Projectile.ai[1] = -Projectile.ai[1];
                    }
                    RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * (1 + (Utils.GetLerpValue(useAnim * 0.35f, useAnim * 0.6f, Animation, true)) * 0.25f)), 0.2f);
                }
                else
                {
                    if (!finalFlip)
                        FlipAsSword = Owner.direction < 0 ? true : false;

                    float time = (AnimationProgress) - (useAnim / 2.5f);
                    float timeMax = useAnim - (useAnim / 2.5f);

                    if (time >= (int)(timeMax * 0.4f) && playSwingSound)
                    {
                        SoundStyle swing = new("CalamityMod/Sounds/Item/SwingMid");
                        SoundEngine.PlaySound(swing with { Volume = 0.8f, Pitch = (Projectile.ai[1] == 1 ? -0.4f : -0.1f) }, Projectile.Center);
                        SoundStyle swing2 = new("CalamityMod/Sounds/Item/HellkiteSwing", 2);
                        SoundEngine.PlaySound(swing2 with { Volume = 0.8f, Pitch = (Projectile.ai[1] == 1 ? 0.4f : 0.7f) }, Projectile.Center);
                        swingCount++;
                        playSwingSound = false;
                    }
                    if ((int)(time) % 2 == 0 && Projectile.ai[1] == 1 && !Main.dedServ)
                    {
                        SoundStyle swoosh = new("CalamityMod/Sounds/Item/SwooshMid");
                        SoundEngine.PlaySound(swoosh with { Volume = 1f, Pitch = -0.4f, MaxInstances = -1 }, Projectile.Center);
                    }
                    if (time > (int)(timeMax * 0.45f) && time < (int)(timeMax * 0.9f))
                    {
                        CanHit = true;

                        // YC projectile spawns — untouched
                        if ((int)time % 7 == 0 && Projectile.owner == Main.myPlayer && YC_EssenceFlame.CanSpawnMoreFor(Owner))
                            SpawnSpinFlame();

                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 particleVel = new Vector2(0, 10 * -Projectile.ai[1] * Owner.direction).RotatedBy(FinalRotation + MathHelper.ToRadians(-45));
                            Vector2 particlePos = Owner.Center + (new Vector2(Main.rand.Next(30, (int)(170 * (1 + bladeFade * 0.6f))), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45))) * Projectile.scale;
                            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(particlePos, -particleVel.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.75f, 0.9f), false, 14, Main.rand.NextFloat(0.06f, 0.03f) * Projectile.scale, Color.Lerp(BladeOrange, BladeGold, 0.5f), new Vector2(1.3f, 0.2f), true, false, 0.55f));
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 particleVel = new Vector2(0, 10 * -Projectile.ai[1] * Owner.direction).RotatedBy(FinalRotation + MathHelper.ToRadians(-45));
                            Vector2 particlePos = Owner.Center + (new Vector2(Main.rand.Next(30, (int)(270 * (1 + bladeFade * 0.6f))), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45))) * Projectile.scale;
                            if (Main.rand.NextBool(3))
                            {
                                Particle sparker = new CustomSpark(particlePos + Main.rand.NextVector2Circular(15, 15), -particleVel * Main.rand.NextFloat(0.4f, 0.8f), "CalamityMod/Particles/Sparkle", false, 30, Main.rand.NextFloat(1.2f, 2.2f) * Projectile.scale, BladeGold, new Vector2(0.4f, Main.rand.NextFloat(0.9f, 1.4f)), true, true);
                                GeneralParticleHandler.SpawnParticle(sparker);
                            }
                            else
                            {
                                GeneralParticleHandler.SpawnParticle(new CustomSpark(particlePos, -particleVel.RotatedByRandom(0.2f) * 2, "CalamityMod/Particles/LargeBloom", false, Main.rand.Next(7, 9 + 1), Main.rand.NextFloat(0.3f, 0.35f) * Projectile.scale, BladeOrange * 0.65f, new Vector2(1f, 1.2f), true, false, 0, false, false, 0.45f));
                            }
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            float randRot = Main.rand.NextFloat(-30, -60);
                            Vector2 dustVel = (new Vector2(0, 10 * -Projectile.ai[1] * Owner.direction)).RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));
                            GenericSparkle sparker = new GenericSparkle(Owner.Center + (new Vector2(270 * (1 + bladeFade * 0.6f), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45)).RotatedByRandom(0.3f)) * Projectile.scale, Vector2.Zero, Color.White, BladeGold, Main.rand.NextFloat(0.5f, 0.7f) * Projectile.scale, 11, Main.rand.NextFloat(-0.1f, 0.1f), 2.68f);
                            GeneralParticleHandler.SpawnParticle(sparker);
                            Dust dust2 = Dust.NewDustPerfect(Owner.Center + (new Vector2(270 * (1 + bladeFade * 0.6f), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45)).RotatedByRandom(0.3f)) * Projectile.scale, DustID.GoldFlame, dustVel);
                            dust2.scale = Main.rand.NextFloat(0.65f, 1.05f);
                            dust2.noGravity = true;
                            dust2.color = Color.Lerp(Color.White, BladeGold, 0.5f);
                        }
                    }
                    else
                        CanHit = false;

                    float start = swingCount % 2 != 0 ? (150 * Projectile.ai[1] * Owner.direction) : (150f * Projectile.ai[1] * Owner.direction);
                    float end = swingCount % 2 != 0 ? ((270) * -Projectile.ai[1] * Owner.direction) : (120f * -Projectile.ai[1] * Owner.direction);
                    RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(MathHelper.Lerp(start, end, CalamityUtils.ExpInOutEasing(time / timeMax, 1))), 0.2f);
                    if (time > timeMax * 0.8f)
                        RotationOffset = Utils.AngleLerp(RotationOffset, MathHelper.ToRadians(MathHelper.Lerp(start, end, CalamityUtils.ExpInOutEasing(time / timeMax, 1))), 0.2f);
                    if (time >= timeMax)
                        doSwing = false;
                    if (time < (int)(timeMax * 0.7f))
                        postSwing = true;

                    if (throwRequested)
                    {
                        StartThrowWindup();
                        return;
                    }
                }
            }
            // ── End of Earth's UseStyle ────────────────────────────────────────

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void StartThrowWindup()
        {
            inThrowWindup = true;
            throwTimer = 0;
            CanHit = false;
            postSwing = true;
            fadeIn = 1f;
            bladeFade = 1f;
            chargeBorderIntensity = 1f;
            float baseOffset = MathHelper.ToRadians(112f * Projectile.ai[1]);
            throwStartRotationOffset = baseOffset + MathHelper.WrapAngle(RotationOffset - baseOffset);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.74f, Pitch = -0.18f }, Owner.Center);
        }

        private void DoThrowWindup()
        {
            throwTimer++;
            CanHit = false;
            postSwing = true;
            mousePos = Owner.Calamity().mouseWorld;
            aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65;
            if (mousePos.X < Owner.Center.X) Owner.direction = -1;
            else Owner.direction = 1;
            FlipAsSword = Owner.direction == -1;

            float raiseProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(throwTimer / (float)ThrowWindupFrames, 0f, 1f));
            fadeIn = MathHelper.Lerp(fadeIn, 1.0f, 0.15f);
            bladeFade = MathHelper.Lerp(bladeFade, 1.25f + raiseProgress * 0.15f, 0.08f);
            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, 1.4f, 0.08f);

            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f), 0.16f);
            float raisedOffset = MathHelper.ToRadians(112f * Projectile.ai[1]) - MathHelper.PiOver2 * Projectile.ai[1];
            RotationOffset = MathHelper.Lerp(throwStartRotationOffset, raisedOffset, raiseProgress);
            ArmRotationOffset = MathHelper.ToRadians(-140f + 28f * raiseProgress);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f + 28f * raiseProgress);

            if (throwTimer % 5 == 0)
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.2f + raiseProgress * 4.2f);

            EmitThrowWindupEffects();

            if (throwTimer >= ThrowWindupFrames)
            {
                LaunchThrownBlade();
                Projectile.Kill();
            }
        }

        private bool HasValidThrowTarget()
        {
            if (throwTargetIndex < 0 || throwTargetIndex >= Main.maxNPCs)
                return false;
            NPC target = Main.npc[throwTargetIndex];
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

        private void EmitThrowWindupEffects()
        {
            if (Main.dedServ)
                return;

            float charge = MathHelper.Clamp(throwTimer / (float)ThrowWindupFrames, 0f, 1f);
            Vector2 tip = GetBladeTipPosition();
            Vector2 lockedAimDirection = (mousePos - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);

            if (throwTimer % Math.Max(2, (int)MathHelper.Lerp(7f, 2f, charge)) == 0)
            {
                Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(80f, 190f) * (1f - charge * 0.45f);
                Vector2 position = tip + orbit + Main.rand.NextVector2Circular(16f, 16f);
                Vector2 velocity = (tip - position).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3f, 8f + charge * 6f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position, velocity,
                    "CalamityMod/Particles/Sparkle", false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.55f, 1.05f) * (0.75f + charge),
                    Main.rand.NextBool(3) ? BladeWhite : BladeGold,
                    new Vector2(0.26f, 1.15f + charge * 0.45f),
                    true, true, shrinkSpeed: 0.18f));
            }

            if (throwTimer % 10 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    tip, Vector2.Zero,
                    Color.Lerp(BladeOrange, BladeGold, charge),
                    Vector2.One, lockedAimDirection.ToRotation(),
                    0.06f, 1.2f + charge * 0.9f, 18));
            }

            if (throwTimer % 20 == 0)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.18f + charge * 0.2f, Pitch = -0.4f + charge * 0.16f, MaxInstances = 4 }, tip);
        }

        private void LaunchThrownBlade()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 launchDirection = (mousePos - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 launchPosition = Owner.MountedCenter + launchDirection * 44f;
            int targetIndex = HasValidThrowTarget() ? throwTargetIndex : -1;
            int thrown = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                launchPosition,
                launchDirection * 8.5f,
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

            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 8f);
            Owner.velocity -= launchDirection * 1.25f;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(launchPosition, Vector2.Zero, BladeGold, Vector2.One, launchDirection.ToRotation(), 0.12f, 2.4f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(launchPosition, Vector2.Zero, BladeOrange * 0.9f, "CalamityMod/Particles/BloomCircle", Vector2.One, launchDirection.ToRotation(), 0.18f, 1.1f, 14, true));
                for (int i = 0; i < 42; i++)
                {
                    Vector2 vel = launchDirection.RotatedByRandom(0.72f) * Main.rand.NextFloat(4f, 19f);
                    Dust d = Dust.NewDustPerfect(launchPosition + Main.rand.NextVector2Circular(18f, 18f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(1.0f, 1.6f));
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

            throwTargetIndex = target.whoAmI;
            if (IsRightHeld())
                throwRequested = true;

            if (Projectile.owner == Main.myPlayer &&
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
                    2f,
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

        private void SpawnHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            Vector2 lockedAimDirection = (mousePos - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);

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
                    swoosh.Value, drawPosition, null,
                    aura * fadeIn * (CanHit ? 0.9f : 0.64f),
                    FinalRotation + MathHelper.ToRadians(45f) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -82f : 82f) * -Owner.direction,
                    swoosh.Size() * 0.5f,
                    Projectile.scale * (CanHit ? 1.08f : 0.82f),
                    SpriteEffects.None);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 4.8f * fadeIn;
                Main.EntitySpriteDraw(ghost.Value, drawPosition + offset, null, aura * 0.13f * fadeIn, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            }

            float chargeOutline = MathHelper.Clamp(chargeBorderIntensity, 0f, 1.4f);
            if (chargeOutline > 0.02f)
            {
                Color outlineColor = Color.Lerp(BladeOrange, BladeWhite, MathHelper.Clamp(chargeOutline / 1.4f, 0f, 1f)) with { A = 0 };
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * (4f + chargeOutline * 5f);
                    Main.EntitySpriteDraw(glow.Value, drawPosition + offset, null, outlineColor * 0.18f * chargeOutline, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale * (1f + chargeOutline * 0.035f), effects);
                }

                Vector2 tipDrawPosition = GetBladeTipPosition() - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
                float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 16f);
                Main.EntitySpriteDraw(bloom.Value, tipDrawPosition, null, outlineColor * 0.42f * chargeOutline, 0f, bloom.Size() * 0.5f, Projectile.scale * (0.32f + chargeOutline * 0.24f) * pulse, SpriteEffects.None);

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
    }
}
