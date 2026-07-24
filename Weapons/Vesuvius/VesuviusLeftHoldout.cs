using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    public class VesuviusLeftHoldout : ModProjectile, ILocalizedModType
    {
        private const int MaximumReleaseTime = 46;
        private const int HeliumChargeStartFrames = 10;
        private const int HeliumChargeLoopFrames = 120;
        private const int HeliumAftershotBaseFrames = 17;
        private static readonly SoundStyle HeliumChargeSound = new("CalamityMod/Sounds/Item/HeliumFlashCharge");
        private static readonly SoundStyle HeliumChargeLoopSound = new("CalamityMod/Sounds/Item/HeliumFlashFullChargeLoop");
        private static readonly SoundStyle HeliumReadySound = new("CalamityMod/Sounds/Item/HeliumFlashReady");
        private static readonly SoundStyle HeliumFireSound = new("CalamityMod/Sounds/Item/HeliumFlashFire") { Volume = 1f };
        private static readonly SoundStyle HeliumDudFireSound = new("CalamityMod/Sounds/Item/HeliumFlashDudFire");
        private static readonly SoundStyle HeliumSteamReleaseSound = new("CalamityMod/Sounds/Item/HeliumFlashSteamRelease");

        private bool released;
        private bool steamVentTriggered;
        private bool cooldownSteamTriggered;
        private bool aftershotCooldownApplied;
        private int releaseTimer;
        private int chargeFrames;
        private int currentStage;
        private int releaseStage;
        private SlotId chargeLoopSlot;

        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Projectile.Center + Direction * 48f;
        private int FullChargeFrameTarget => Math.Max(1, VesuviusProgression.GetStageStartFrame(VesuviusProgression.GetMaxStage()));
        private bool FullyCharged => currentStage >= VesuviusProgression.GetMaxStage() && chargeFrames >= FullChargeFrameTarget;
        private float ChargeCompletion => released
            ? Utils.GetLerpValue(GetReleaseTime(releaseStage), 0f, releaseTimer, true)
            : Utils.GetLerpValue(0f, FullChargeFrameTarget, chargeFrames, true);

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.ModItem is not NewVesuvius)
            {
                Projectile.Kill();
                return;
            }

            UpdateHeldPosition();
            ManipulateOwner();

            if (released)
            {
                ReleaseAI();
                return;
            }

            Projectile.timeLeft = 2;
            chargeFrames++;

            int nextStage = Math.Max(currentStage, VesuviusProgression.GetChargeStage(chargeFrames));
            if (nextStage > currentStage)
            {
                currentStage = nextStage;
                SpawnStageBurst(currentStage);
            }

            ChargingEffects();

            if (!IsStillCharging())
                StartRelease();
        }

        private void UpdateHeldPosition()
        {
            if (Main.myPlayer == Projectile.owner && !released)
            {
                Vector2 targetDirection = (Owner.Calamity().mouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetDirection, 0.42f).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.netUpdate = true;
            }

            Projectile.direction = Direction.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.rotation = Direction.ToRotation();

            float recoil = released ? MathHelper.Clamp(32f - releaseTimer * 1.1f, 0f, 32f) : 34f;
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) + Direction * recoil;
        }

        private void ManipulateOwner()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Direction * Projectile.direction).ToRotation();
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Direction.ToRotation() - MathHelper.PiOver2);
        }

        private bool IsStillCharging()
        {
            if (Owner.CantUseHoldout())
                return false;

            if (Main.myPlayer == Projectile.owner)
                return Main.mouseLeft && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;

            return Owner.channel;
        }

        private void StartRelease()
        {
            released = true;
            releaseStage = currentStage;
            releaseTimer = 0;
            steamVentTriggered = false;
            cooldownSteamTriggered = false;
            aftershotCooldownApplied = false;
            Projectile.timeLeft = MaximumReleaseTime;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<EXSkill.VesuviusEXPlayer>().GainEX(Math.Max(1, releaseStage));

            if (SoundEngine.TryGetActiveSound(chargeLoopSlot, out var sound))
                sound?.Stop();

            SoundEngine.PlaySound(
                releaseStage <= 0
                    ? HeliumDudFireSound with { Volume = 0.45f, Pitch = 0.18f }
                    : HeliumFireSound with { Volume = 0.82f + releaseStage * 0.05f, Pitch = -0.18f + releaseStage * 0.035f },
                GunTip);
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.65f, Pitch = -0.24f + releaseStage * 0.04f }, GunTip);
            ApplyScreenShake(2.5f + releaseStage * 0.75f);
            SpawnReleaseCoreFlash(releaseStage);

            if (Main.myPlayer == Projectile.owner)
                FireReleasedPayload();
        }

        private void ReleaseAI()
        {
            releaseTimer++;
            Projectile.timeLeft = Math.Max(2, MaximumReleaseTime - releaseTimer);

            if (releaseTimer <= 8)
                ReleaseMuzzleEffects();
            if (!steamVentTriggered && releaseTimer >= GetSteamVentFrame(releaseStage))
            {
                steamVentTriggered = true;
                SoundEngine.PlaySound(HeliumSteamReleaseSound with { Volume = 0.44f + releaseStage * 0.04f, Pitch = -0.08f + releaseStage * 0.025f }, GunTip);
                SpawnHeliumSteamReleaseBurst(releaseStage, false);
                ApplyScreenShake(1.4f + releaseStage * 0.45f);
            }

            if (releaseTimer >= GetReleaseTime(releaseStage))
            {
                ApplyAftershotCooldown();
                SpawnAftershotCooldownSteam();
                Projectile.Kill();
            }
        }

        private int GetReleaseTime(int stage)
        {
            return stage switch
            {
                <= 1 => 22,
                2 => 34,
                _ => 46
            };
        }

        private int GetSteamVentFrame(int stage)
        {
            return Math.Max(8, Math.Min(GetReleaseTime(stage) - 4, 12 + stage * 5));
        }

        private int GetAftershotCooldownFrames(int stage)
        {
            int heliumCooldown = stage >= VesuviusProgression.GetMaxStage()
                ? HeliumAftershotBaseFrames * 3
                : (int)(HeliumAftershotBaseFrames * 1.5f);

            return Math.Max(VesuviusProgression.ClickLockoutFrames, heliumCooldown + Math.Max(0, stage - 1) * 4);
        }

        private void FireReleasedPayload()
        {
            float speed = 26f + releaseStage * 4f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTip + Direction * 10f,
                Direction * speed,
                ModContent.ProjectileType<VesuviusArcOrb>(),
                Math.Max(1, VesuviusProgression.GetLeftDamage(releaseStage, Projectile.damage)),
                Projectile.knockBack * (1f + releaseStage * 0.2f),
                Projectile.owner,
                releaseStage);

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = releaseStage >= 3 ? 0.8f : 0.62f, Pitch = releaseStage >= 3 ? -0.42f : -0.3f }, GunTip);
        }

        private void SpawnMoltenAsteroid(Vector2 position, Vector2 velocity, int variant, float scale, bool noLargeExplosion, float damageMultiplier)
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                position,
                velocity,
                ModContent.ProjectileType<VesuviusMoltenAsteroid>(),
                Math.Max(1, (int)(Projectile.damage * damageMultiplier)),
                Projectile.knockBack * 0.55f,
                Projectile.owner,
                variant,
                scale,
                noLargeExplosion ? 1f : 0f);
        }

        private void ChargingEffects()
        {
            if (Main.dedServ)
                return;

            Color stageColor = VesuviusProgression.GetStageColor(currentStage);
            float chargePower = ChargeCompletion;

            UpdateChargeSoundPosition();
            if (chargeFrames == HeliumChargeStartFrames)
                chargeLoopSlot = SoundEngine.PlaySound(HeliumChargeSound with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);

            if (FullyCharged && (chargeFrames - FullChargeFrameTarget) % HeliumChargeLoopFrames == 0)
                chargeLoopSlot = SoundEngine.PlaySound(HeliumChargeLoopSound with { Volume = 0.48f, Pitch = -0.08f }, Projectile.Center);

            SpawnVolcanicPressureChargeFX(stageColor, chargePower);
            ApplyChargingShake(chargePower);

            int ashInterval = currentStage >= 3 ? 4 : 6;
            if (chargeFrames % ashInterval == 0)
            {
                Vector2 radial = Main.rand.NextVector2CircularEdge(1f, 1f);
                float radius = 30f + chargePower * (26f + currentStage * 3f);
                Vector2 spawnPosition = GunTip + radial * radius;
                Vector2 inwardVelocity = -radial * Main.rand.NextFloat(1.8f, 3.1f + chargePower * 1.8f);
                GeneralParticleHandler.SpawnParticle(new SquareAshParticle(
                    spawnPosition,
                    inwardVelocity + Owner.velocity * 0.15f,
                    Main.rand.Next(20, 29),
                    Main.rand.NextFloat(0.42f, 0.72f),
                    Color.Lerp(VesuviusProjectileVisuals.AshGray, stageColor, 0.18f + chargePower * 0.16f)));
            }

            if (chargeFrames % (currentStage >= 3 ? 5 : 7) == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(7f, 5f),
                    -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.32f, 0.32f)) * Main.rand.NextFloat(0.65f, 1.65f),
                    Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, stageColor, 0.18f),
                    Main.rand.Next(24, 36),
                    Main.rand.NextFloat(0.26f, 0.52f),
                    0.62f,
                    Main.rand.NextFloat(-0.03f, 0.03f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            Lighting.AddLight(GunTip, stageColor.ToVector3() * (0.3f + currentStage * 0.08f + chargePower * 0.38f));
        }

        private void SpawnVolcanicPressureChargeFX(Color stageColor, float chargePower)
        {
            if (chargeFrames < HeliumChargeStartFrames)
                return;

            int fissureInterval = currentStage >= 3 ? 3 : 5;
            if (chargeFrames % fissureInterval == 0 && !released)
            {
                Vector2 radial = Main.rand.NextVector2CircularEdge(1f, 1f);
                float radius = 22f + chargePower * (38f + currentStage * 4f);
                Vector2 spawnPosition = GunTip + radial * radius;
                Vector2 inwardVelocity = -radial * Main.rand.NextFloat(2.4f, 4.2f + chargePower * 2f);
                Color fissureColor = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.HotWhite, 0.18f + chargePower * 0.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    spawnPosition,
                    inwardVelocity,
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.36f, 0.62f),
                    fissureColor,
                    true));
            }

            if (FullyCharged && chargeFrames % 12 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    GunTip,
                    Vector2.Zero,
                    Color.Lerp(stageColor, VesuviusProjectileVisuals.HotWhite, 0.35f),
                    new Vector2(1f, 0.42f),
                    Direction.ToRotation(),
                    0.5f,
                    0.12f,
                    15));
            }
        }

        private void SpawnStageBurst(int stage)
        {
            if (Main.dedServ)
                return;

            Color burstColor = VesuviusProgression.GetStageColor(stage);
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(GunTip, 0.08f, 12, 0.3f + stage * 0.04f, Color.Lerp(burstColor, Color.White, 0.36f)));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Vector2.Zero, burstColor, new Vector2(1f, 0.44f), Direction.ToRotation(), 0.14f, 0.82f + stage * 0.11f, 18));
            SoundEngine.PlaySound(
                stage >= VesuviusProgression.GetMaxStage()
                    ? HeliumReadySound with { Volume = 0.95f, Pitch = -0.08f + stage * 0.025f }
                    : SoundID.Item74 with { Volume = 0.55f, Pitch = -0.35f + stage * 0.08f },
                GunTip);
            ApplyScreenShake(1.6f + stage * 0.48f);

            for (int i = 0; i < 8 + stage * 2; i++)
            {
                Vector2 debrisVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6.5f);
                Dust dust = Dust.NewDustPerfect(GunTip, Main.rand.NextBool(3) ? DustID.Obsidian : DustID.Stone, debrisVelocity, 100, Color.Lerp(Color.DarkGray, burstColor, 0.22f), Main.rand.NextFloat(0.72f, 1.22f));
                dust.noGravity = i % 3 == 0;
            }

            for (int i = 0; i < 3 + stage; i++)
            {
                Vector2 fissureVelocity = Direction.RotatedByRandom(0.38f) * Main.rand.NextFloat(4f, 8f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(GunTip, fissureVelocity, true, Main.rand.Next(14, 21), Main.rand.NextFloat(0.4f, 0.68f), Main.rand.NextBool(4) ? Color.White : burstColor, true));
            }
        }

        private void SpawnReleaseCoreFlash(int stage)
        {
            if (Main.dedServ)
                return;

            Color color = VesuviusProgression.GetStageColor(stage);
            float stagePower = MathHelper.Clamp(stage / (float)Math.Max(1, VesuviusProgression.GetMaxStage()), 0f, 1f);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                GunTip,
                Vector2.Zero,
                Color.Lerp(color, Color.White, 0.2f),
                0.72f + stagePower * 0.38f,
                15 + stage * 2));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Direction * 0.2f, VesuviusProjectileVisuals.HotWhite, "CalamityMod/Particles/SoftRoundExplosion", new Vector2(1.35f, 0.72f), Direction.ToRotation(), 0.03f, 0.12f + stage * 0.012f, 12));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Direction * 0.45f, color, "CalamityMod/Particles/FlameExplosion", new Vector2(1.6f, 0.7f), Direction.ToRotation(), 0.04f, 0.17f + stage * 0.018f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Direction * 0.65f, color, new Vector2(1f, 0.45f), Direction.ToRotation(), 0.12f, 1.05f + stage * 0.1f, 16));
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(GunTip, 0.11f, 11, 0.32f + stagePower * 0.16f, VesuviusProjectileVisuals.HotWhite));

            int dustCount = stage <= 0 ? 6 : 7 + stage * 2;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 debrisVelocity = Direction.RotatedByRandom(0.38f) * Main.rand.NextFloat(4f, 12f + stage);
                Dust dust = Dust.NewDustPerfect(GunTip, Main.rand.NextBool(3) ? DustID.Obsidian : DustID.InfernoFork, debrisVelocity, 60, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.35f)), Main.rand.NextFloat(0.7f, 1.18f));
                dust.noGravity = i % 2 == 0;
            }

            Particle muzzleCore = new PointParticle(
                GunTip,
                Direction * (10f + stage * 1.6f),
                false,
                12,
                0.72f + stage * 0.06f,
                color,
                true);
            GeneralParticleHandler.SpawnParticle(muzzleCore);

            int fissureCount = stage <= 0 ? 3 : 4 + stage * 2;
            for (int i = 0; i < fissureCount; i++)
            {
                Vector2 fissureVelocity = Direction.RotatedByRandom(0.24f) * Main.rand.NextFloat(5f, 12f + stage * 1.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(GunTip + Main.rand.NextVector2Circular(3f, 3f), fissureVelocity, true, Main.rand.Next(14, 22), Main.rand.NextFloat(0.42f, 0.74f), Main.rand.NextBool(4) ? VesuviusProjectileVisuals.HotWhite : color, true));
            }
        }

        private void SpawnAftershotCooldownSteam()
        {
            if (cooldownSteamTriggered)
                return;

            cooldownSteamTriggered = true;
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(HeliumSteamReleaseSound with { Volume = 0.5f + releaseStage * 0.035f, Pitch = -0.05f + releaseStage * 0.02f }, GunTip);
            SpawnHeliumSteamReleaseBurst(releaseStage, true);
        }

        private void SpawnHeliumSteamReleaseBurst(int stage, bool finalBurst)
        {
            if (Main.dedServ)
                return;

            Color heatColor = VesuviusProgression.GetStageColor(stage);
            int burstCount = 1 + (finalBurst ? 1 : 0) + (stage >= 3 ? 1 : 0);
            float speed = finalBurst ? 10f : 7.5f;

            for (int b = 0; b < burstCount; b++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 smokeVel1 = Direction.RotatedBy(MathHelper.ToRadians(84f * side)) * speed;
                    Vector2 smokeVel2 = Direction.RotatedBy(MathHelper.ToRadians(48f * side)) * speed;
                    Vector2 smokeVel3 = Direction.RotatedBy(MathHelper.ToRadians(125f * side)) * speed;

                    SpawnSteamJet(smokeVel1, heatColor, finalBurst);
                    SpawnSteamJet(smokeVel2, heatColor, finalBurst);
                    SpawnSteamJet(smokeVel3, heatColor, finalBurst);
                }
            }
        }

        private void SpawnSteamJet(Vector2 jetVelocity, Color heatColor, bool finalBurst)
        {
            Color smokeColor = Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, heatColor, Main.rand.NextFloat(0.08f, finalBurst ? 0.3f : 0.2f));
            Vector2 spawnPosition = GunTip + jetVelocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(12f, finalBurst ? 24f : 18f);
            Vector2 smokeVelocity = jetVelocity * Main.rand.NextFloat(0.14f, finalBurst ? 0.46f : 0.34f);

            Particle smoke = new HeavySmokeParticle(
                spawnPosition,
                smokeVelocity,
                smokeColor,
                Main.rand.Next(20, finalBurst ? 35 : 30),
                Main.rand.NextFloat(0.18f, finalBurst ? 0.42f : 0.34f),
                0.58f,
                Main.rand.NextFloat(-0.11f, 0.11f),
                false);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(2))
            {
                Vector2 emberVelocity = jetVelocity.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.12f, finalBurst ? 0.38f : 0.28f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(spawnPosition, emberVelocity, true, Main.rand.Next(13, 20), Main.rand.NextFloat(0.3f, 0.52f), Color.Lerp(heatColor, Color.White, 0.16f), true));
            }
        }

        private void ApplyAftershotCooldown()
        {
            if (aftershotCooldownApplied)
                return;

            aftershotCooldownApplied = true;
            Owner.GetModPlayer<VesuviusPassivePlayer>().LeftClickCooldown = Math.Max(
                Owner.GetModPlayer<VesuviusPassivePlayer>().LeftClickCooldown,
                GetAftershotCooldownFrames(releaseStage));
        }

        private void ReleaseMuzzleEffects()
        {
            if (Main.dedServ)
                return;

            if (releaseTimer % 3 == 0)
            {
                Color color = VesuviusProgression.GetStageColor(releaseStage);
                Particle smoke = new TimedSmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(8f, 7f),
                    -Direction * Main.rand.NextFloat(0.5f, 1.4f) - Vector2.UnitY * Main.rand.NextFloat(0.8f, 2f),
                    Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, color, 0.12f),
                    Color.Transparent,
                    Main.rand.NextFloat(0.42f, 0.78f),
                    0.68f,
                    Main.rand.Next(24, 37),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (releaseTimer % 4 == 0 && releaseStage > 0)
            {
                Color color = VesuviusProgression.GetStageColor(releaseStage);
                Vector2 velocity = Direction.RotatedByRandom(0.2f) * Main.rand.NextFloat(4f, 8f + releaseStage);
                GeneralParticleHandler.SpawnParticle(new PointParticle(GunTip + Main.rand.NextVector2Circular(4f, 4f), velocity, true, 16, Main.rand.NextFloat(0.3f, 0.5f), Main.rand.NextBool(4) ? VesuviusProjectileVisuals.HotWhite : color, true));
            }
        }

        private void UpdateChargeSoundPosition()
        {
            if (SoundEngine.TryGetActiveSound(chargeLoopSlot, out var sound) && sound.IsPlaying)
                sound.Position = Projectile.Center;
        }

        private void ApplyChargingShake(float chargePower)
        {
            if (chargeFrames < HeliumChargeStartFrames)
                return;

            float distanceFactor = Utils.GetLerpValue(1400f, 220f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            float preFullProgress = Utils.GetLerpValue(HeliumChargeStartFrames, FullChargeFrameTarget - 36f, chargeFrames, true);
            float rise = (float)Math.Pow(preFullProgress, 1.35f);
            float calm = Utils.GetLerpValue(FullChargeFrameTarget - 42f, FullChargeFrameTarget + 36f, chargeFrames, true);
            calm = calm * calm * (3f - 2f * calm);
            float calmMultiplier = (1f - calm) * (1f - calm);
            float maxStage = Math.Max(1f, VesuviusProgression.GetMaxStage());
            float stageRatio = MathHelper.Clamp(currentStage / maxStage, 0f, 1f);
            float pulse = 0.45f + 0.55f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Lerp(10f, 16f, stageRatio));
            float basePower = 0.12f + chargePower * 0.54f + MathHelper.Lerp(0.08f, 0.82f, stageRatio) * rise;
            float shakePower = (basePower + pulse * 0.42f * rise) * distanceFactor * 0.67f * calmMultiplier;

            if (shakePower <= 0.01f)
                return;

            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower);
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1800f, 240f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor * 0.67f);
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(chargeLoopSlot, out var sound))
                sound?.Stop();

            if (released)
            {
                ApplyAftershotCooldown();
                SpawnAftershotCooldownSteam();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D moltenCore = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten3").Value;
            Texture2D moltenCoreGlow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow3").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotation = Projectile.rotation + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f);
            float staffRotation = rotation + MathHelper.ToRadians(45f * Projectile.spriteDirection);
            int stageForDraw = released ? releaseStage : currentStage;
            Color stageColor = VesuviusProgression.GetStageColor(stageForDraw);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (7f + stageForDraw));
            float chargeIntensity = MathHelper.Clamp(ChargeCompletion, 0f, 1f);
            float fullChargeBonus = stageForDraw >= VesuviusProgression.GetMaxStage() && !released ? 1.06f + pulse * 0.05f : 1f;

            if (!released && !FullyCharged)
                drawPosition += Main.rand.NextVector2Circular(0.15f + chargeIntensity * 2.65f, 0.15f + chargeIntensity * 2.65f);

            Vector2 tipScreen = GunTip - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            float coreScale = (0.17f + stageForDraw * 0.025f + chargeIntensity * 0.2f) * fullChargeBonus;

            // Calamity's Helium Flash cadence is kept, but the visual material is volcanic:
            // one restrained pressure halo, one white-hot center, and a flattened gasket ring.
            // The opaque molten rock below prevents the charge from reading as a miniature sun.
            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                stageColor * (0.16f + chargeIntensity * 0.28f),
                Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.2f + stageForDraw * 0.025f + chargeIntensity * 0.24f) * fullChargeBonus,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                Color.White * chargeIntensity * 0.48f,
                -Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.08f + chargeIntensity * 0.11f) * fullChargeBonus,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomRing,
                tipScreen,
                null,
                Color.Lerp(stageColor, VesuviusProjectileVisuals.HotWhite, 0.18f) * (0.12f + chargeIntensity * 0.3f),
                Projectile.rotation,
                bloomRing.Size() * 0.5f,
                new Vector2(0.24f + chargeIntensity * 0.24f, 0.1f + chargeIntensity * 0.08f) * fullChargeBonus,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, staffRotation, origin, Projectile.scale, effects);

            if (chargeIntensity > 0.03f)
            {
                Main.EntitySpriteDraw(
                    moltenCore,
                    tipScreen,
                    null,
                    Color.White,
                    Main.GlobalTimeWrappedHourly * (0.8f + stageForDraw * 0.12f),
                    moltenCore.Size() * 0.5f,
                    coreScale,
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (0.5f + pulse * 0.18f), staffRotation, origin, Projectile.scale, effects);
            if (chargeIntensity > 0.03f)
            {
                Main.EntitySpriteDraw(
                    moltenCoreGlow,
                    tipScreen,
                    null,
                    Color.White * (0.5f + chargeIntensity * 0.42f),
                    Main.GlobalTimeWrappedHourly * (0.8f + stageForDraw * 0.12f),
                    moltenCoreGlow.Size() * 0.5f,
                    coreScale,
                    SpriteEffects.None);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
