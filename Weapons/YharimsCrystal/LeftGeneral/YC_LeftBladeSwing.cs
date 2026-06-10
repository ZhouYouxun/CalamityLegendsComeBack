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

        private int comboIndex;
        private int postComboTimer;
        private int currentStage;
        private int stageTimer;
        private int stageDuration;
        private int gapTimer;
        private int lockedFacing = 1;
        private bool stageActive;
        private bool releaseRequested;
        private bool swingSoundPlayed;
        private bool waveFired;
        private bool shardReleased;
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
            Projectile.localNPCHitCooldown = -1;
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

        public override void UseStyle()
        {
            if (!IsLeftHeld())
                releaseRequested = true;

            if (comboIndex >= 3)
            {
                CanHit = false;
                postSwing = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.22f);
                ApplyIdleRotation();

                if (releaseRequested)
                {
                    ReleaseBurningShard();
                    Projectile.Kill();
                    return;
                }

                postComboTimer++;
                if (postComboTimer >= 48)
                {
                    ReleaseBurningShard();
                    comboIndex = 0;
                    postComboTimer = 0;
                    shardReleased = false;
                    waveFired = false;
                }
                return;
            }

            if (!stageActive)
            {
                CanHit = false;
                postSwing = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.24f);
                bladeFade = MathHelper.Lerp(bladeFade, Empowered ? 1f : 0.28f, 0.08f);

                if (releaseRequested)
                {
                    Projectile.Kill();
                    return;
                }

                if (gapTimer > 0)
                {
                    gapTimer--;
                    ApplyIdleRotation();
                    EmitChargeSparks();
                    ApplyArmRotation();
                    return;
                }

                StartStage();
            }

            RunStage();
            ApplyArmRotation();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnHitEffects(target);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, currentStage == 2 ? 5.5f : 3.2f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FinalDawnSlash") { Volume = 0.62f, Pitch = Main.rand.NextFloat(0.08f, 0.24f) }, target.Center);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float falloff = Utils.Remap(Projectile.numHits, 0f, 7f, currentStage == 2 ? 1.5f : 1.18f, 0.64f, true);
            modifiers.SourceDamage *= falloff;
        }

        private void StartStage()
        {
            StageProfile profile = GetStageProfile(comboIndex);
            stageActive = true;
            currentStage = comboIndex;
            stageDuration = profile.Duration;
            stageTimer = 0;
            swingSoundPlayed = false;
            waveFired = false;
            CanHit = false;
            postSwing = false;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            UpdateLockedAimFromMouse();
            Projectile.ai[1] = -lockedFacing;
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;
            SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.32f, Pitch = -0.28f + currentStage * 0.1f }, Owner.Center);
        }

        private void RunStage()
        {
            stageTimer++;
            StageProfile profile = GetStageProfile(currentStage);
            float progress = MathHelper.Clamp(stageTimer / (float)Math.Max(1, stageDuration), 0f, 1f);

            if (progress < profile.Windup)
            {
                UpdateLockedAimFromMouse();
                Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.14f);
                RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(128f * Projectile.ai[1]), 0.24f);
                bladeFade = MathHelper.Lerp(bladeFade, Empowered ? 1f : 0.45f, 0.08f);
                EmitChargeSparks();
                return;
            }

            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.16f);

            float swingProgress = MathHelper.Clamp((progress - profile.Windup) / Math.Max(0.01f, 1f - profile.Windup), 0f, 1f);
            float eased = CalamityUtils.ExpInOutEasing(swingProgress, 1);
            bool hitWindow = swingProgress > 0.22f && swingProgress < 0.88f;
            CanHit = hitWindow;
            postSwing = swingProgress < 0.82f;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.34f : 0.24f);
            bladeFade = MathHelper.Lerp(bladeFade, Empowered ? 1f : 0.62f, 0.12f);

            if (swingProgress >= 0.28f && !swingSoundPlayed)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SwingMid") { Volume = 0.82f, Pitch = currentStage == 1 ? 0.22f : -0.18f }, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellkiteSwing", 2) { Volume = 0.68f, Pitch = currentStage == 2 ? 0.48f : 0.18f }, Projectile.Center);
                swingSoundPlayed = true;
            }

            if (Empowered && !waveFired && swingProgress >= 0.35f)
            {
                SpawnJudgementWave();
                waveFired = true;
            }

            float start = 154f * Projectile.ai[1];
            float end = currentStage == 2 ? -176f * Projectile.ai[1] : -126f * Projectile.ai[1];
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(MathHelper.Lerp(start, end, eased)), 0.24f);

            if (CanHit)
                SpawnSwingParticles(profile);

            if (stageTimer >= stageDuration)
                EndStage(profile);
        }

        private void EndStage(StageProfile profile)
        {
            stageActive = false;
            stageTimer = 0;
            comboIndex++;
            gapTimer = profile.GapFrames;
            CanHit = false;
            postSwing = false;
        }

        private void ReleaseBurningShard()
        {
            if (shardReleased || Main.myPlayer != Projectile.owner)
                return;

            shardReleased = true;
            Vector2 direction = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            int shard = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 118f,
                direction * 0.01f,
                ModContent.ProjectileType<YC_BurningShard>(),
                Math.Max(1, (int)(Projectile.damage * 0.72f)),
                Projectile.knockBack,
                Projectile.owner,
                0f);

            if (Main.projectile.IndexInRange(shard))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[shard], YCWeaponForm.Crystal);
                Main.projectile[shard].CritChance = Projectile.CritChance;
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f }, Owner.Center);
        }

        private void SpawnJudgementWave()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 direction = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            int wave = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 80f,
                direction * (currentStage == 2 ? 14.5f : 11.5f),
                ModContent.ProjectileType<YC_AuricJudgementWave>(),
                Math.Max(1, (int)(Projectile.damage * (currentStage == 2 ? 0.9f : 0.62f))),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                currentStage);

            if (Main.projectile.IndexInRange(wave))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[wave], YCWeaponForm.Blade);
                Main.projectile[wave].CritChance = Projectile.CritChance;
            }
        }

        private void ApplyIdleRotation()
        {
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.1f);
            RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(112f * Projectile.ai[1]), 0.2f);
            Owner.direction = lockedFacing;
            FlipAsSword = lockedFacing < 0;
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

        private static StageProfile GetStageProfile(int stage)
        {
            return stage switch
            {
                0 => new StageProfile(42, 4, 0.32f, 0.96f),
                1 => new StageProfile(24, 58, 0.25f, 0.86f),
                _ => new StageProfile(54, 0, 0.36f, 1.18f),
            };
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
