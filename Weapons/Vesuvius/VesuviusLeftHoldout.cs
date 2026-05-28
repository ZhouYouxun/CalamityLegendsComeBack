using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.BStage1;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.EStage4;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.FStage5;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityMod;
using CalamityMod.Graphics.Metaballs;
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
        private const int MaximumReleaseTime = 112;

        private bool released;
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
        private float ChargeCompletion => released
            ? Utils.GetLerpValue(GetReleaseTime(releaseStage), 0f, releaseTimer, true)
            : MathHelper.Clamp(chargeFrames / (float)Math.Max(1, VesuviusProgression.GetStageStartFrame(Math.Max(1, currentStage + 1))), 0f, 1f);

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
            float breathingOffset = released ? 0f : (float)Math.Sin(chargeFrames * 0.08f) * (3f + currentStage);
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) + Direction * recoil + Direction.RotatedBy(MathHelper.PiOver2) * breathingOffset;
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
            Projectile.timeLeft = MaximumReleaseTime;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<VesuviusPassivePlayer>().LeftClickCooldown = VesuviusProgression.ClickLockoutFrames;
            Owner.GetModPlayer<EXSkill.VesuviusEXPlayer>().GainEX(Math.Max(1, releaseStage));

            if (SoundEngine.TryGetActiveSound(chargeLoopSlot, out var sound))
                sound?.Stop();

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.9f, Pitch = -0.18f + releaseStage * 0.04f }, GunTip);
            ApplyScreenShake(4f + releaseStage * 1.6f);

            if (Main.myPlayer == Projectile.owner && releaseStage <= 0)
                FireStageZero();
        }

        private void ReleaseAI()
        {
            releaseTimer++;
            Projectile.timeLeft = Math.Max(2, MaximumReleaseTime - releaseTimer);

            if (Main.myPlayer == Projectile.owner)
            {
                if (releaseStage >= 1)
                    FireBasaltStream();

                if (releaseStage >= 2)
                    FireVolcanicBombs();

                if (releaseStage >= 3 && releaseTimer == 3)
                    FireMagmaPillars();

                if (releaseStage >= 4)
                    FireHomingCinders();

                if (releaseStage >= 5 && releaseTimer == 5)
                    FireObsidianShards();
            }

            ReleaseMuzzleEffects();

            if (releaseTimer >= GetReleaseTime(releaseStage))
                Projectile.Kill();
        }

        private int GetReleaseTime(int stage)
        {
            return stage switch
            {
                <= 0 => 16,
                1 => 58,
                2 => 74,
                3 => 84,
                4 => 96,
                _ => MaximumReleaseTime
            };
        }

        private void FireStageZero()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Direction.RotatedBy(MathHelper.ToRadians((i - 1) * 8f)) * Main.rand.NextFloat(13.5f, 16.5f);
                SpawnMoltenAsteroid(GunTip, velocity, 0, 0.7f, true, 0.58f);
            }
        }

        private void FireBasaltStream()
        {
            if (releaseTimer > 54 || releaseTimer % 2 != 0)
                return;

            Vector2 velocity = Direction.RotatedBy(Main.rand.NextFloat(-0.22f, 0.22f)) * Main.rand.NextFloat(11.5f, 17f);
            SpawnMoltenAsteroid(GunTip + Main.rand.NextVector2Circular(7f, 7f), velocity, Main.rand.Next(6), Main.rand.NextFloat(0.42f, 0.66f), true, 0.4f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 ashVelocity = Direction.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(5f, 9f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Main.rand.NextVector2Circular(8f, 8f),
                    ashVelocity,
                    ModContent.ProjectileType<VesuviusVolcanicAsh>(),
                    Math.Max(1, (int)(Projectile.damage * 0.16f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);
            }
        }

        private void FireVolcanicBombs()
        {
            if (releaseTimer > 72 || releaseTimer % 14 != 4)
                return;

            Vector2 velocity = Direction.RotatedBy(Main.rand.NextFloat(-0.18f, 0.18f)) * Main.rand.NextFloat(9f, 12f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTip + Main.rand.NextVector2Circular(5f, 5f),
                velocity,
                ModContent.ProjectileType<VesuviusVolcanicBomb>(),
                Math.Max(1, (int)(Projectile.damage * 0.78f)),
                Projectile.knockBack * 1.15f,
                Projectile.owner,
                Main.rand.Next(6),
                Main.rand.NextFloat(1.08f, 1.36f));
        }

        private void FireMagmaPillars()
        {
            const int pillarCount = 6;
            for (int i = 0; i < pillarCount; i++)
            {
                float offset = MathHelper.Lerp(-0.44f, 0.44f, i / (float)(pillarCount - 1));
                Vector2 velocity = Direction.RotatedBy(offset) * 28f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Direction * 12f,
                    velocity,
                    ModContent.ProjectileType<VesuviusMagmaPillar>(),
                    Math.Max(1, (int)(Projectile.damage * 0.68f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    i,
                    releaseStage);
            }
        }

        private void FireHomingCinders()
        {
            if (releaseTimer > 92 || releaseTimer % 5 != 1)
                return;

            Vector2 velocity = Direction.RotatedBy(Main.rand.NextFloat(-0.82f, 0.82f)) * Main.rand.NextFloat(8f, 13.5f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTip + Main.rand.NextVector2Circular(16f, 16f),
                velocity,
                ModContent.ProjectileType<VesuviusHomingCinder>(),
                Math.Max(1, (int)(Projectile.damage * 0.36f)),
                Projectile.knockBack * 0.32f,
                Projectile.owner);
        }

        private void FireObsidianShards()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Direction.RotatedBy(MathHelper.ToRadians(-14f + i * 14f)) * 21f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Direction * 18f,
                    velocity,
                    ModContent.ProjectileType<VesuviusObsidianShard>(),
                    Math.Max(1, (int)(Projectile.damage * 1.24f)),
                    Projectile.knockBack * 1.1f,
                    Projectile.owner,
                    i);
            }
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

            if (chargeFrames == 12)
                chargeLoopSlot = SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.48f, Pitch = -0.35f }, Projectile.Center);

            SpawnHeliumStyleChargeFX(stageColor, chargePower);

            if (chargeFrames % Math.Max(1, 4 - currentStage) == 0)
            {
                RancorLavaMetaball.SpawnParticle(
                    GunTip + Main.rand.NextVector2Circular(6f + currentStage * 1.5f, 6f + currentStage * 1.5f),
                    Projectile.scale * (18f + currentStage * 4f + chargePower * 10f));
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

            Lighting.AddLight(GunTip, stageColor.ToVector3() * (0.35f + currentStage * 0.08f));
        }

        private void SpawnHeliumStyleChargeFX(Color stageColor, float chargePower)
        {
            if (chargeFrames < 10)
                return;

            float pullRadius = 18f + chargePower * (56f + currentStage * 8f);
            int streakRate = Math.Max(1, 5 - currentStage);
            if (chargeFrames % streakRate == 0 && !released)
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

            if (chargeFrames % Math.Max(2, 6 - currentStage) == 0)
            {
                Vector2 dustVelocity = Vector2.One.RotatedByRandom(100f) * Main.rand.NextFloat(2.2f, 4.8f + currentStage);
                Dust dust = Dust.NewDustPerfect(GunTip + dustVelocity, DustID.FireworksRGB, dustVelocity * 0.22f, 0, default, Main.rand.NextFloat(0.42f, 0.86f));
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.White, Main.rand.NextBool(4) ? Color.Orange : stageColor, 0.68f);
            }
        }

        private void SpawnStageBurst(int stage)
        {
            if (Main.dedServ)
                return;

            Color burstColor = VesuviusProgression.GetStageColor(stage);
            GeneralParticleHandler.SpawnParticle(new PulseRing(GunTip, Vector2.Zero, burstColor, 0.08f, 2.4f + stage * 0.38f, 22));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(GunTip, Vector2.Zero, burstColor * 0.8f, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-0.4f, 0.4f), 0.1f, 0.45f + stage * 0.08f, 18));
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = -0.35f + stage * 0.08f }, GunTip);
            ApplyScreenShake(2.5f + stage * 0.9f);

            for (int i = 0; i < 16 + stage * 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(GunTip, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f), 80, burstColor, Main.rand.NextFloat(0.8f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void ReleaseMuzzleEffects()
        {
            if (Main.dedServ)
                return;

            if (releaseTimer % 2 == 0)
            {
                Color color = VesuviusProgression.GetStageColor(releaseStage);
                RancorLavaMetaball.SpawnParticle(
                    GunTip + Main.rand.NextVector2Circular(12f, 12f),
                    Projectile.scale * Main.rand.NextFloat(24f, 46f));

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
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1800f, 240f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(chargeLoopSlot, out var sound))
                sound?.Stop();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D volatileCore = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/VolatileStarcore").Value;

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

            Vector2 tipScreen = GunTip - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                stageColor with { A = 0 } * (0.34f + pulse * 0.16f) * (0.35f + chargeIntensity) * fullChargeBonus,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.42f + stageForDraw * 0.08f + pulse * 0.08f) * (0.75f + chargeIntensity) * fullChargeBonus,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                tipScreen,
                null,
                Color.White with { A = 0 } * chargeIntensity * 0.58f,
                -Projectile.rotation,
                bloom.Size() * 0.5f,
                (0.19f + pulse * 0.04f) * (0.8f + chargeIntensity) * fullChargeBonus,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            if (!released && currentStage > 0)
            {
                int afterimageCount = 1 + currentStage;
                for (int i = 0; i < afterimageCount; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / afterimageCount + Main.GlobalTimeWrappedHourly * 2.2f).ToRotationVector2() * (1.5f + currentStage * 0.9f);
                    Main.EntitySpriteDraw(texture, drawPosition + offset, null, stageColor * 0.18f, staffRotation, origin, Projectile.scale, effects);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, staffRotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (0.72f + pulse * 0.28f), staffRotation, origin, Projectile.scale, effects);

            if (chargeIntensity > 0.03f)
            {
                Main.EntitySpriteDraw(
                    volatileCore,
                    tipScreen,
                    coreSource,
                    Color.White * chargeIntensity,
                    0f,
                    coreSource.Size() * 0.5f,
                    Projectile.scale * MathHelper.Lerp(0.2f, 0.58f, chargeIntensity) * fullChargeBonus,
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
