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
            ApplyScreenShake(4f + releaseStage * 1.6f);
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

            SpawnHeliumStyleChargeFX(stageColor, chargePower);
            ApplyChargingShake(chargePower);

            if (chargeFrames % Math.Max(1, 4 - currentStage) == 0)
            {
                VesuviusProjectileVisuals.SpawnMoltenBloom(
                    GunTip + Main.rand.NextVector2Circular(6f + currentStage * 1.5f, 6f + currentStage * 1.5f),
                    Projectile.scale * (18f + currentStage * 4f + chargePower * 10f),
                    0.52f);
            }

            if (chargeFrames % 3 == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(12f, 12f),
                    -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(1.2f, 3.4f),
                    Color.Lerp(new Color(74, 54, 42), stageColor, 0.42f),
                    Main.rand.Next(22, 38),
                    Main.rand.NextFloat(0.35f, 0.78f),
                    0.72f,
                    Main.rand.NextFloat(-0.03f, 0.03f),
                    currentStage >= 4,
                    required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (chargeFrames % 5 == 0)
            {
                Particle ash = new SquareAshParticle(
                    GunTip + Main.rand.NextVector2Circular(34f + currentStage * 8f, 24f + currentStage * 5f),
                    Main.rand.NextVector2Circular(1.7f, 1.7f) - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.6f),
                    Main.rand.Next(28, 46),
                    Main.rand.NextFloat(0.55f, 1.1f),
                    Color.Lerp(new Color(116, 88, 66), stageColor, 0.48f));
                GeneralParticleHandler.SpawnParticle(ash);
            }

            Lighting.AddLight(GunTip, stageColor.ToVector3() * (0.42f + currentStage * 0.11f + chargePower * 0.55f));
        }

        private void SpawnHeliumStyleChargeFX(Color stageColor, float chargePower)
        {
            if (chargeFrames < 10)
                return;

            float pullRadius = 18f + chargePower * (72f + currentStage * 12f);
            int streakRate = Math.Max(1, 5 - currentStage);
            if (chargeFrames % streakRate == 0 && !released && !FullyCharged)
            {
                Vector2 inwardVelocity = Main.rand.NextVector2CircularEdge(2.5f, 2.5f) * Main.rand.NextFloat(0.3f * chargeFrames, 0.3f * chargeFrames + pullRadius);
                Particle streak = new ManaDrainStreak(
                    Owner,
                    Main.rand.NextFloat(0.055f + chargePower * 0.035f, 0.085f + chargePower * 0.045f),
                    inwardVelocity,
                    0f,
                    Color.Lerp(Color.Red, stageColor, 0.65f),
                    Color.Lerp(Color.Orange, Color.White, chargePower * 0.45f),
                    7 + currentStage,
                    GunTip);
                GeneralParticleHandler.SpawnParticle(streak);
            }

            if (FullyCharged && Main.rand.NextBool(2))
            {
                Vector2 dustVelocity = Vector2.One.RotatedByRandom(100f) * Main.rand.NextFloat(3f, 5.4f + currentStage);
                Dust dust = Dust.NewDustPerfect(GunTip + dustVelocity, DustID.FireworksRGB, dustVelocity * 0.15f, 0, default, Main.rand.NextFloat(0.35f, 0.7f));
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.White, Main.rand.NextBool(4) ? Color.Orange : stageColor, 0.7f);
            }

            if (chargeFrames % Math.Max(2, 6 - currentStage) == 0)
            {
                Vector2 dustVelocity = Vector2.One.RotatedByRandom(100f) * Main.rand.NextFloat(2.2f, 4.8f + currentStage);
                Dust dust = Dust.NewDustPerfect(GunTip + dustVelocity, DustID.FireworksRGB, dustVelocity * 0.22f, 0, default, Main.rand.NextFloat(0.42f, 0.86f));
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.White, Main.rand.NextBool(4) ? Color.Orange : stageColor, 0.68f);
            }

            if (chargeFrames % Math.Max(4, 10 - currentStage) == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    GunTip - Direction * (8f + chargePower * 16f),
                    -Direction * 0.45f,
                    Color.Lerp(stageColor, Color.White, chargePower * 0.22f),
                    new Vector2(0.72f, 1.35f + chargePower * 1.25f),
                    Direction.ToRotation() - MathHelper.PiOver2,
                    0.12f,
                    0.02f,
                    12));
            }

            if (chargeFrames % 8 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GenericBloom(
                    GunTip + Main.rand.NextVector2Circular(2.5f + chargePower * 5f, 2.5f + chargePower * 5f),
                    Vector2.Zero,
                    Color.Lerp(stageColor, Color.White, chargePower * 0.35f),
                    0.38f + chargePower * 0.55f,
                    12 + currentStage * 2,
                    true));
            }
        }

        private void SpawnStageBurst(int stage)
        {
            if (Main.dedServ)
                return;

            Color burstColor = VesuviusProgression.GetStageColor(stage);
            GeneralParticleHandler.SpawnParticle(new PulseRing(GunTip, Vector2.Zero, burstColor, 0.08f, 1.2f + stage * 0.19f, 22));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Vector2.Zero, burstColor * 0.8f, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-0.4f, 0.4f), 0.1f, 0.225f + stage * 0.04f, 18));
            SoundEngine.PlaySound(
                stage >= VesuviusProgression.GetMaxStage()
                    ? HeliumReadySound with { Volume = 0.95f, Pitch = -0.08f + stage * 0.025f }
                    : SoundID.Item74 with { Volume = 0.55f, Pitch = -0.35f + stage * 0.08f },
                GunTip);
            ApplyScreenShake(2.5f + stage * 0.9f);

            for (int i = 0; i < 16 + stage * 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(GunTip, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f), 80, burstColor, Main.rand.NextFloat(0.8f, 1.45f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 6 + stage; i++)
            {
                Vector2 sparkVelocity = Direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(5f, 14f);
                Particle spark = new LineParticle(GunTip, sparkVelocity, false, 24, Main.rand.NextFloat(0.28f, 0.72f), Main.rand.NextBool() ? Color.OrangeRed : burstColor);
                GeneralParticleHandler.SpawnParticle(spark);
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
                1.1f + stagePower * 0.9f,
                22 + stage * 5));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Vector2.Zero, Color.OrangeRed, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.05f + stage * 0.008f, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Vector2.Zero, color, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.08f + stage * 0.012f, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Direction * 0.8f, color, new Vector2(0.8f, 1.75f + stage * 0.25f), Direction.ToRotation() - MathHelper.PiOver2, 0.12f, 0.02f, 18));

            int dustCount = stage <= 0 ? 12 : 17 + stage * 4;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(GunTip, DustID.FireworksRGB);
                dust.velocity = Direction.RotatedByRandom(0.42f + stage * 0.025f) * Main.rand.NextFloat(5f, 24f + stage * 3f);
                dust.scale = Main.rand.NextFloat(0.55f, 1f);
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.White, Main.rand.NextBool(4) ? Color.Orange : color, 0.7f);
            }

            Particle muzzleSpark = new GlowSparkParticle(
                GunTip,
                Direction * (16f + stage * 2f),
                false,
                6,
                0.052f + stage * 0.006f,
                color,
                new Vector2(1.8f, 0.8f),
                true);
            GeneralParticleHandler.SpawnParticle(muzzleSpark);

            int lineCount = stage <= 0 ? 10 : 18 + stage * 3;
            for (int i = 0; i <= lineCount; i++)
            {
                Vector2 sparkVelocity = Direction * (7.5f + stage);

                float hotScale = Main.rand.NextFloat(0.3f, 0.8f);
                Vector2 hotVelocity = sparkVelocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.5f, 0.7f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(GunTip, hotVelocity, false, 34, hotScale, Main.rand.NextBool() ? Color.Red : Color.DarkRed));

                float brightScale = Main.rand.NextFloat(0.4f, 1f);
                Vector2 brightVelocity = sparkVelocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.9f, 1.6f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(GunTip, brightVelocity, false, 36, brightScale, Main.rand.NextBool() ? Color.DarkOrange : color));
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
            int burstCount = Math.Min(12, (finalBurst ? 7 : 4) + stage * 2);
            float speed = finalBurst ? 14f : 10f;

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
            Color smokeColor = Color.Lerp(Color.SlateGray, heatColor, Main.rand.NextFloat(0.12f, finalBurst ? 0.48f : 0.34f));
            Vector2 spawnPosition = GunTip + jetVelocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(18f, finalBurst ? 34f : 24f);
            Vector2 smokeVelocity = jetVelocity * Main.rand.NextFloat(0.18f, finalBurst ? 0.72f : 0.52f);

            Particle smoke = new HeavySmokeParticle(
                spawnPosition,
                smokeVelocity,
                smokeColor,
                Main.rand.Next(15, finalBurst ? 38 : 31),
                Main.rand.NextFloat(0.12f, finalBurst ? 0.54f : 0.42f),
                0.5f,
                Main.rand.NextFloat(-0.2f, 0.2f),
                Main.rand.NextBool(),
                required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            Dust steamDust = Dust.NewDustPerfect(
                spawnPosition,
                DustID.SteampunkSteam,
                jetVelocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.03f, finalBurst ? 0.32f : 0.22f),
                180,
                smokeColor,
                Main.rand.NextFloat(0.3f, 1.1f));
            steamDust.noGravity = false;
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

            if (releaseTimer % 2 == 0)
            {
                Color color = VesuviusProgression.GetStageColor(releaseStage);
                VesuviusProjectileVisuals.SpawnMoltenBloom(
                    GunTip + Main.rand.NextVector2Circular(12f, 12f),
                    Projectile.scale * Main.rand.NextFloat(24f, 46f),
                    0.58f);

                Particle smoke = new TimedSmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(14f, 14f),
                    -Direction * Main.rand.NextFloat(1f, 2.4f) - Vector2.UnitY * Main.rand.NextFloat(1.5f, 3.5f),
                    Color.Lerp(Color.Gray, color, 0.2f),
                    Color.Transparent,
                    Main.rand.NextFloat(0.65f, 1.15f),
                    0.78f,
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (releaseTimer % 3 == 0 && releaseStage > 0)
            {
                Color color = VesuviusProgression.GetStageColor(releaseStage);
                Vector2 velocity = Direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(6f, 13f + releaseStage);
                Particle line = new LineParticle(GunTip + Main.rand.NextVector2Circular(5f, 5f), velocity, false, 18, Main.rand.NextFloat(0.24f, 0.58f), Main.rand.NextBool() ? Color.OrangeRed : color);
                GeneralParticleHandler.SpawnParticle(line);
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
            Texture2D volatileCore = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/VolatileStarcore").Value;
            Texture2D smokeyHalo = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_SmokeyHalo1").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotation = Projectile.rotation + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f);
            float staffRotation = rotation + MathHelper.ToRadians(45f * Projectile.spriteDirection);
            int stageForDraw = released ? releaseStage : currentStage;
            Color stageColor = VesuviusProgression.GetStageColor(stageForDraw);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (7f + stageForDraw));
            float chargeIntensity = MathHelper.Clamp(ChargeCompletion, 0f, 1f);
            float fullChargeBonus = stageForDraw >= VesuviusProgression.GetMaxStage() && !released ? 1.55f + pulse * 0.35f : 1f;
            int coreFrame = ((released ? releaseTimer : chargeFrames) / (stageForDraw >= 4 ? 1 : 2)) % 6;
            Rectangle coreSource = volatileCore.Frame(1, 6, 0, coreFrame);

            if (!released && !FullyCharged)
            {
                float rumble = MathHelper.Clamp(chargeFrames, 0f, FullChargeFrameTarget);
                drawPosition += Main.rand.NextVector2Circular(rumble / 25f, rumble / 25f);
            }

            Vector2 tipScreen = GunTip - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            float coreScale = (0.32f + stageForDraw * 0.075f + chargeIntensity * 0.36f) * fullChargeBonus;
            float rotationTime = Main.GlobalTimeWrappedHourly;
            Color additiveStageColor = new Color(stageColor.R, stageColor.G, stageColor.B, 0);

            // Four layers, in the same spirit as HeliumFlash's charge-up: a slow smoky halo, a
            // wide coloured bloom, a tight white hotspot, and a rotating ring that tightens as
            // the charge fills. The previous eleven-texture stack (noise x3, flame eye, plasma
            // core, smear, light flash, ...) all landed on the same point and averaged out into
            // one muddy blob where no single element could be read.
            Main.EntitySpriteDraw(
                smokeyHalo,
                tipScreen,
                null,
                additiveStageColor * (0.16f + chargeIntensity * 0.2f),
                -Projectile.rotation + rotationTime * 0.85f,
                smokeyHalo.Size() * 0.5f,
                coreScale * (0.52f + pulse * 0.08f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                additiveStageColor * (0.34f + pulse * 0.16f) * (0.35f + chargeIntensity) * fullChargeBonus,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.42f + stageForDraw * 0.08f + pulse * 0.08f) * (0.75f + chargeIntensity) * fullChargeBonus,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                VesuviusProjectileVisuals.AdditiveColor(Color.White) * chargeIntensity * 0.58f,
                -Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.19f + pulse * 0.04f) * (0.8f + chargeIntensity) * fullChargeBonus,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomRing,
                tipScreen,
                null,
                additiveStageColor * (0.18f + chargeIntensity * 0.42f) * fullChargeBonus,
                Projectile.rotation + rotationTime * 2.2f,
                bloomRing.Size() * 0.5f,
                (0.16f + chargeIntensity * 0.32f) * fullChargeBonus,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, staffRotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(glow, drawPosition, null, new Color(255, 255, 255, 0) * (0.72f + pulse * 0.28f), staffRotation, origin, Projectile.scale, effects);

            // The charge core is drawn opaque over the staff, exactly as HeliumFlash draws its
            // starcore. Additive with A=0 dissolved the animated core into the bloom behind it.
            if (chargeIntensity > 0.03f)
            {
                Main.EntitySpriteDraw(
                    volatileCore,
                    tipScreen,
                    coreSource,
                    Color.White,
                    0f,
                    coreSource.Size() * 0.5f,
                    Projectile.scale * MathHelper.Lerp(0.2f, 0.58f, chargeIntensity) * fullChargeBonus,
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
