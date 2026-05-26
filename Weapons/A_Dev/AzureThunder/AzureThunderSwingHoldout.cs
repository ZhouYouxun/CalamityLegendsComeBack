using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderSwingHoldout : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        private const float OldTextureSize = 158f;
        private const float CurrentTextureSize = 80f;
        private const float HoldoutDrawScale = OldTextureSize / CurrentTextureSize;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";
        public override int AssignedItemID => ModContent.ItemType<AzureThunder>();

        public override Vector2 SpriteOrigin => new(0f, 172f / HoldoutDrawScale);
        public override float HitboxOutset => 118f;
        public override Vector2 HitboxSize => new(170f, 170f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private int comboIndex;
        private int currentStage;
        private int stageTimer;
        private int stageDuration;
        private int gapTimer;
        private int swingDirection = 1;

        private bool stageActive;
        private bool releaseRequested;
        private bool releaseFinalStarted;
        private bool stageEventOne;
        private bool swingSoundPlayed;
        private bool postSwing;
        private int harmonyBarrageShotsFired;
        private int finalLightningFired;
        private float fadeIn;
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection;

        private AzureThunderPlayer ThunderPlayer => Owner.GetModPlayer<AzureThunderPlayer>();
        private bool HarmonyActive => ThunderPlayer.HarmonyActive;
        private int ComboLength => HarmonyActive ? 3 : 4;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.extraUpdates = 0;
            Projectile.scale = 1f;
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            Projectile.ai[1] = -1f;
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                WhenSpawned();
                whenSpawned = false;
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                Projectile.netUpdate = true;
            }

            bool itemAnimationActive = Owner.ItemAnimationActive;
            if (Owner.HeldItem.type != AssignedItemID || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;

            if (itemAnimationActive || IgnoreActiveAnimation)
            {
                Animation++;
                UseStyle();
                Owner.heldProj = Projectile.whoAmI;
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            }
            else
            {
                Animation = 0f;
                if (DrawUnconditionally)
                {
                    Owner.heldProj = Projectile.whoAmI;
                    Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
                }

                NumberOfAnimations = 0;
                ResetStyle();
            }

            int itemAnimationMax = Math.Max(1, Owner.itemAnimationMax);
            AnimationProgress = Animation % itemAnimationMax;

            if (AbsolutePosition == Vector2.Zero)
                Projectile.position = Owner.position + Owner.Size / 2f - Projectile.Size / 2f + Offset;
            else
            {
                AbsolutePosition += Projectile.velocity;
                Projectile.position = AbsolutePosition - Projectile.Size / 2f + Offset;
            }

            if (AnimationProgress == itemAnimationMax - 1)
            {
                OnEndUse();
                NumberOfAnimations++;
            }

            if (Owner.itemAnimation == itemAnimationMax - 1)
            {
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                OnBeginUse();
            }

            if (DrawUnconditionally)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
        }

        public override bool? CanDamage() => CanHit;

        public override void UseStyle()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssignedItemID)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 2;
            Projectile.Center = Owner.MountedCenter;

            bool holdingLeft = Owner.channel &&
                (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen &&
                !Main.blockMouse;

            if (!holdingLeft)
                releaseRequested = true;

            if (!stageActive)
            {
                CanHit = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.25f);

                if (gapTimer > 0)
                {
                    gapTimer--;
                    ApplyIdleRotation();
                    ApplyArmRotation();
                    return;
                }

                StartStage();
                ApplyArmRotation();
                return;
            }

            RunStage();
            ApplyArmRotation();
        }

        private void StartStage()
        {
            if (Main.myPlayer == Projectile.owner && !ThunderPlayer.TrySpendMana())
            {
                Projectile.Kill();
                return;
            }

            stageActive = true;
            releaseFinalStarted = releaseRequested;
            currentStage = comboIndex % ComboLength;
            stageDuration = HarmonyActive ? 30 : 45;
            stageTimer = 0;
            stageEventOne = false;
            harmonyBarrageShotsFired = 0;
            finalLightningFired = 0;
            swingSoundPlayed = false;
            postSwing = false;
            CanHit = false;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            swingDirection = comboIndex % 2 == 0 ? -1 : 1;
            Projectile.ai[1] = swingDirection;

            if (HarmonyActive && Main.myPlayer == Projectile.owner)
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, currentStage == 2 ? 7.5f : 5f);

            if (!HarmonyActive && currentStage == 3)
                ThunderPlayer.RestoreManaForOwnedSwords(includeLeftClickGrowth: true);
        }

        private void RunStage()
        {
            stageTimer++;

            if (stageTimer < SwingImpactFrame)
            {
                UpdateLockedAimFromMouse();
                postSwing = false;
                CanHit = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.35f);
                Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.1f);
                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * GetWindupAngleScale()),
                    0.2f);

                if (stageTimer >= stageDuration)
                    EndStage();

                return;
            }

            if (!postSwing)
                FlipAsSword = Owner.direction < 0;

            postSwing = true;
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.1f);

            float swingTime = stageTimer - stageDuration / 3f;
            float swingTimeMax = stageDuration - stageDuration / 3f;
            float swingProgress = MathHelper.Clamp(swingTime / swingTimeMax, 0f, 1f);

            bool hitWindow = swingTime > (int)(swingTimeMax * 0.4f) && swingTime < (int)(swingTimeMax * 0.7f);
            CanHit = hitWindow;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.3f : 0.35f);

            if (swingTime >= (int)(swingTimeMax * 0.4f) && !swingSoundPlayed)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.75f, Pitch = Main.rand.NextFloat(0.08f, 0.18f) }, Owner.Center);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.35f, Pitch = 0.3f }, Owner.Center);
                swingSoundPlayed = true;
            }

            RotationOffset = MathHelper.Lerp(
                RotationOffset,
                MathHelper.ToRadians(MathHelper.Lerp(
                    150f * Projectile.ai[1] * Owner.direction,
                    120f * -Projectile.ai[1] * Owner.direction,
                    CalamityUtils.ExpInOutEasing(swingProgress, 1))),
                0.2f);

            if (HarmonyActive)
                RunHarmonyStage();
            else
                RunNormalStage();

            SpawnSwingParticles(swingProgress);

            if (stageTimer >= stageDuration)
                EndStage();
        }

        private void UpdateLockedAimFromMouse()
        {
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
        }

        private float GetWindupAngleScale()
        {
            float chainedSwingBoost = comboIndex > 0 ? 1f : Utils.GetLerpValue(stageDuration * 0.8f, stageDuration, stageTimer, true);
            return 1f + chainedSwingBoost * 0.35f;
        }

        private int SwingImpactFrame => HarmonyActive ? stageDuration / 3 : (int)(stageDuration / 1.5f);
        private bool ReachedSwingImpact(int delay = 0) => stageTimer >= SwingImpactFrame + delay;

        private void RunNormalStage()
        {
            switch (currentStage)
            {
                case 0:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnLightOrbs();
                        stageEventOne = true;
                    }
                    break;

                case 1:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnFlyingSwords(2, 16, false);
                        SpawnMouseLightning(0.4f, true);
                        stageEventOne = true;
                    }
                    break;

                case 2:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnFlyingSwords(4, 17, true);
                        SpawnMouseLightning(0.5f, true);
                        stageEventOne = true;
                    }
                    break;

                case 3:
                    int strikeCount = GetFinalLightningCount();
                    if (finalLightningFired < strikeCount && ReachedSwingImpact(finalLightningFired * 5))
                    {
                        SpawnForwardLightning(finalLightningFired);
                        finalLightningFired++;
                    }
                    break;
            }
        }

        private void RunHarmonyStage()
        {
            if (currentStage <= 1)
            {
                int strikeCount = AzureThunderProgression.DownedDragonfolly ? 5 : 4;
                if (harmonyBarrageShotsFired < strikeCount && ReachedSwingImpact(harmonyBarrageShotsFired * 5))
                {
                    SpawnParallelBarrageLightning(harmonyBarrageShotsFired);
                    harmonyBarrageShotsFired++;
                }

                return;
            }

            if (!stageEventOne && ReachedSwingImpact())
            {
                SpawnGrandSword();
                stageEventOne = true;
            }
        }

        private int GetFinalLightningCount()
        {
            if (AzureThunderProgression.DownedYharon && AzureThunderPlayer.CountOwnedGroundSwords(Owner) >= AzureThunderGroundSword.MaxGroundSwords)
                return 9;

            return 3 + (AzureThunderProgression.DownedDragonfolly ? 1 : 0);
        }

        private void EndStage()
        {
            stageActive = false;
            CanHit = false;
            stageTimer = 0;
            comboIndex++;
            swingDirection = comboIndex % 2 == 0 ? -1 : 1;
            Projectile.ai[1] = swingDirection;

            if (releaseFinalStarted)
            {
                Projectile.Kill();
                return;
            }

            gapTimer = 5;
        }

        private void ApplyIdleRotation()
        {
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.3f);
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(40f * Projectile.ai[1] * Owner.direction), 0.18f);
        }

        private void ApplyArmRotation()
        {
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void SpawnLightOrbs()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            AzureThunderSounds.PlayOrbRelease(Owner.Center);
            Vector2 right = lockedAimDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, i);
                Vector2 velocity = lockedAimDirection.RotatedBy(i * 0.16f) * 14f;

                int orb = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<AzureThunderLightOrb>(),
                    Math.Max(1, (int)(Projectile.damage * 0.35f)),
                    Projectile.knockBack,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(orb))
                    AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[orb]);
            }
        }

        private void SpawnMouseLightning(float damageFactor, bool gainCharge)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 impactPosition = target?.Center ?? lockedMouseWorld;

            // Stage 2/3 left-click thunder is now a true overhead strike instead of a side shot.
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                impactPosition,
                target,
                Math.Max(1, (int)(Projectile.damage * damageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: gainCharge,
                applyStaticDischarge: false,
                big: false,
                spawnHeightMultiplier: 0.9f);
        }

        private void SpawnForwardLightning(int index)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int totalStrikes = GetFinalLightningCount();
            Vector2 forward = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float centeredIndex = index - (totalStrikes - 1) * 0.5f;
            float forwardDistance = 240f + index * 118f + Main.rand.NextFloat(-32f, 42f);
            float sideOffset = centeredIndex * 86f + Main.rand.NextFloat(-34f, 34f);
            Vector2 strikeTarget = Owner.Center + forward * forwardDistance + right * sideOffset - Vector2.UnitY * Main.rand.NextFloat(20f, 90f);

            // The final chain keeps a 5-frame cadence, but these offsets prevent the three bolts from stacking.
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                strikeTarget,
                null,
                Math.Max(1, (int)(Projectile.damage * 1.75f)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: true,
                applyStaticDischarge: index == totalStrikes - 1,
                big: index == totalStrikes - 1,
                spawnHeightMultiplier: 0.95f);
        }

        private void SpawnParallelBarrageLightning(int index)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 focusPoint = target?.Center ?? lockedMouseWorld;
            int strikeCount = AzureThunderProgression.DownedDragonfolly ? 5 : 4;
            Vector2 sweepAxis = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);
            float centeredIndex = index - (strikeCount - 1) * 0.5f;
            Vector2 lineCenter = focusPoint + sweepAxis * (centeredIndex * 82f + Main.rand.NextFloat(-22f, 22f));
            lineCenter += lockedAimDirection * Main.rand.NextFloat(-38f, 38f);

            // Harmony barrage still sweeps across the aim line, but every bolt falls from above.
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                lineCenter,
                null,
                Math.Max(1, (int)(Projectile.damage * 0.8f)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: false,
                applyStaticDischarge: index == strikeCount - 1,
                big: index == strikeCount - 1,
                spawnHeightMultiplier: 0.82f);
        }

        private void SpawnFlyingSwords(int count, int delay, bool behindOwner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            AzureThunderSounds.PlaySwordMaterialize(Owner.Center);
            Vector2 right = lockedAimDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < count; i++)
            {
                float centeredIndex = i - (count - 1) * 0.5f;
                Vector2 spawnPosition;
                if (behindOwner)
                {
                    int row = i / 2;
                    int side = i % 2 == 0 ? -1 : 1;
                    spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, side) - lockedAimDirection * row * 36f;
                }
                else
                    spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, centeredIndex < 0f ? -1 : 1);

                int sword = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<AzureThunderFlyingSword>(),
                    Math.Max(1, (int)(Projectile.damage * 0.25f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    delay + i * 2,
                    lockedMouseWorld.X,
                    lockedMouseWorld.Y);

                if (Main.projectile.IndexInRange(sword))
                    AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[sword]);
            }
        }

        private void SpawnGrandSword()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 impactPosition = target?.Center ?? lockedMouseWorld;
            Vector2 spawnPosition = Owner.MountedCenter + lockedAimDirection * 48f;
            spawnPosition = impactPosition - Vector2.UnitY * 780f;

            int grandSword = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderGrandSword>(),
                Math.Max(1, (int)(Projectile.damage * AzureThunderProgression.UltimateGrandSwordDamageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1,
                impactPosition.X,
                impactPosition.Y);

            if (Main.projectile.IndexInRange(grandSword))
                AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[grandSword]);
        }

        private Vector2 GetDanceOfLightSpawnPosition(Vector2 shootDirection, float sideBias)
        {
            shootDirection = shootDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float shootAngle = shootDirection.ToRotation();
            float side = sideBias == 0f ? (Main.rand.NextBool() ? -1f : 1f) : Math.Sign(sideBias);
            float offsetAngle = MathHelper.Pi + side * Main.rand.NextFloat(0.18f * MathHelper.Pi, 0.4f * MathHelper.Pi);
            Vector2 offset = offsetAngle.ToRotationVector2().RotatedBy(shootAngle) * Main.rand.NextFloat(40f, 140f);
            return Owner.MountedCenter + offset;
        }

        private void SpawnSwingParticles(float progress)
        {
            if (!CanHit)
                return;

            Vector2 slashDirection = (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2();
            Vector2 right = slashDirection.RotatedBy(MathHelper.PiOver2);
            float distance = HarmonyActive ? 210f : currentStage == 3 ? 205f : 160f;

            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Owner.Center + slashDirection * Main.rand.NextFloat(40f, distance) + right * Main.rand.NextFloat(-20f, 20f);
                Vector2 velocity = -slashDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, 5f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure));
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Owner.Center + slashDirection * distance + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.FireworksRGB,
                    -slashDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.2f, 4f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!DrawUnconditionally && Owner.itemAnimation <= 0)
                return false;

            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunderGhost");
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");

            float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Vector2 origin = FlipAsSword ? new Vector2(texture.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float drawScale = Projectile.scale * HoldoutDrawScale;

            Main.EntitySpriteDraw(
                swoosh.Value,
                drawPosition,
                null,
                AzureThunderColors.Azure with { A = 0 } * fadeIn * 0.38f,
                (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -90f : 90f) * -Owner.direction,
                swoosh.Size() * 0.5f,
                Projectile.scale * (HarmonyActive ? 0.86f : 0.68f),
                SpriteEffects.None);

            for (int i = 0; i < 20; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 4.5f * fadeIn;
                Color auraColor = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.Yellow, HarmonyActive ? 0.65f : 0.22f) with { A = 0 };

                Main.EntitySpriteDraw(
                    ghost.Value,
                    drawPosition + offset,
                    ghost.Value.Frame(1, FrameCount, 0, Frame),
                    auraColor * 0.13f * fadeIn,
                    Projectile.rotation + RotationOffset + r,
                    origin,
                    drawScale,
                    effects);
            }

            Main.EntitySpriteDraw(
                texture.Value,
                drawPosition,
                texture.Value.Frame(1, FrameCount, 0, Frame),
                lightColor,
                Projectile.rotation + RotationOffset + r,
                origin,
                drawScale,
                effects);

            return false;
        }
    }
}
