using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityLegendsComeBack.Weapons.Visuals;
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

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal sealed class MilitaryCaller_HoldOut : RightClickHoldoutBase, ILocalizedModType
    {
        private const int StartupFrames = 10;
        private const int FireInterval = 44;
        private const float BeaconSpeed = 15.5f;

        private int startupTimer = StartupFrames;
        private int fireTimer;
        private int muzzleFlashTimer;
        private int chargeSparkTimer;
        private int failureCooldown;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendSHPC>();
        public override bool UseBaseDraw => true;

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 58f;

        public override float MaxOffsetLengthFromArm => 38f;
        public override float RecoilResolveSpeed => 0.11f;
        public override float OffsetXUpwards => -8f;
        public override float OffsetXDownwards => 5f;
        public override float BaseOffsetY => -5f;
        public override float OffsetYUpwards => 10f;
        public override float OffsetYDownwards => 4f;

        private float ChargeCompletion => MathHelper.Clamp(fireTimer / (float)FireInterval, 0f, 1f);

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            FrontArmStretch = Player.CompositeArmStretchAmount.Full;
            BackArmStretch = Player.CompositeArmStretchAmount.Quarter;
            ExtraBackArmRotation = MathHelper.ToRadians(-9f);
            startupTimer = Owner.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickStartupFrames(StartupFrames);
            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.45f, Pitch = 0.18f }, Projectile.Center);
        }

        public override void HoldoutAI()
        {
            if (startupTimer > 0)
            {
                startupTimer--;
                SpawnChargeEffects(true);
                return;
            }

            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;

            if (failureCooldown > 0)
                failureCooldown--;

            fireTimer++;
            SpawnChargeEffects(false);

            if (fireTimer < FireInterval)
                return;

            Vector2 aimWorld = GetAimWorld();
            if (!MilitaryTurretUtility.CanIssueCall(Owner, aimWorld, out string failureReason))
            {
                RejectCall(failureReason, aimWorld);
                fireTimer = FireInterval - 12;
                return;
            }

            int manaCost = Owner.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickManaCost(MilitaryTurretUtility.ManaPerCall);
            if (manaCost > 0 && !Owner.CheckMana(Owner.HeldItem, manaCost, true, false))
            {
                RejectCall("魔力不足", GunTipPosition);
                fireTimer = FireInterval - 10;
                return;
            }

            FireBeacon();
            fireTimer = 0;
        }

        private void FireBeacon()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 spawnPosition = GunTipPosition + direction * 8f;
            MilitaryTurretKind kind = MilitaryTurretUtility.SelectBiomeTurret(Owner);

            if (Main.myPlayer == Projectile.owner)
            {
                int beaconIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction.RotatedByRandom(MathHelper.ToRadians(1.5f)) * BeaconSpeed,
                    ModContent.ProjectileType<MilitaryCallerBeacon>(),
                    0,
                    0f,
                    Projectile.owner,
                    (float)kind,
                    Projectile.damage);

                if (Main.projectile.IndexInRange(beaconIndex))
                    Main.projectile[beaconIndex].CritChance = Projectile.CritChance;
            }

            MilitaryTurretStats stats = MilitaryTurretUtility.GetStats(kind);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.56f, Pitch = 0.24f }, GunTipPosition);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 2.8f);
            OffsetLengthFromArm -= 18f;
            muzzleFlashTimer = 13;
            SpawnMuzzleBurst(direction, stats.ThemeColor);
        }

        private void RejectCall(string reason, Vector2 worldPosition)
        {
            if (failureCooldown <= 0)
            {
                MilitaryTurretUtility.NotifyFailure(Owner, reason, worldPosition);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f, Pitch = -0.25f }, Owner.Center);
                failureCooldown = 30;
            }
        }

        private void SpawnChargeEffects(bool startup)
        {
            if (Main.dedServ)
                return;

            chargeSparkTimer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 core = GunTipPosition + direction * MathHelper.Lerp(-5f, 9f, ChargeCompletion);
            Color blue = new(65, 175, 255);
            Color cyan = new(140, 245, 255);
            Color white = new(235, 255, 255);

            Lighting.AddLight(core, cyan.ToVector3() * (0.12f + ChargeCompletion * 0.28f));

            if (startup || chargeSparkTimer % 4 == 0)
            {
                Dust dust = Dust.NewDustPerfect(core + Main.rand.NextVector2Circular(5f, 5f), DustID.Electric);
                dust.velocity = direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(1.2f, 3.5f) + right * Main.rand.NextFloat(-0.6f, 0.6f);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.78f, 1.18f);
                dust.color = Color.Lerp(blue, white, Main.rand.NextFloat(0.25f, 0.9f));
            }

            if (chargeSparkTimer % 10 == 0)
            {
                Particle spark = new CustomSpark(
                    core + right * Main.rand.NextFloat(-7f, 7f),
                    direction.RotatedByRandom(0.18f) * Main.rand.NextFloat(3.5f, 7f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    10,
                    Main.rand.NextFloat(0.026f, 0.044f),
                    Color.Lerp(blue, cyan, Main.rand.NextFloat()),
                    new Vector2(0.9f, 0.75f),
                    shrinkSpeed: 0.78f);

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnMuzzleBurst(Vector2 direction, Color themeColor)
        {
            if (Main.dedServ)
                return;

            Vector2 muzzle = GunTipPosition + direction * 8f;
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Color white = new(235, 255, 255);

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.RainbowMk2);
                dust.velocity = direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(5f, 11f) + right * Main.rand.NextFloat(-1.1f, 1.1f);
                dust.color = Color.Lerp(themeColor, white, Main.rand.NextFloat(0.25f, 0.85f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.9f, 1.35f);
            }

            for (int i = 0; i < 3; i++)
            {
                Particle ring = new DirectionalPulseRing(
                    muzzle,
                    direction * Main.rand.NextFloat(0.8f, 2.4f),
                    Color.Lerp(themeColor, white, Main.rand.NextFloat(0.35f, 0.8f)) * 0.8f,
                    new Vector2(1f, 1f),
                    direction.ToRotation(),
                    0.07f,
                    Main.rand.NextFloat(0.18f, 0.31f),
                    17);

                GeneralParticleHandler.SpawnParticle(ring);
            }
        }

        private Vector2 GetAimWorld()
        {
            Vector2 mouseWorld = Owner.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return CtrlChipPlayer.GetAimWorld(Owner, mouseWorld);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = SpriteEffects.None;

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = texture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            float chargePulse = 0.42f + ChargeCompletion * 0.56f;
            float flashPulse = muzzleFlashTimer / 13f;
            Color outlineColor = (Color.Lerp(new Color(85, 205, 255), Color.White, 0.55f) with { A = 0 }) * (0.48f + chargePulse * 0.28f + flashPulse * 0.65f);
            float outlineDistance = 1.7f + ChargeCompletion * 2.1f + flashPulse * 3.6f;

            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, outlineColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            }

            if (flashPulse > 0f)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    texture,
                    drawPosition,
                    Projectile.rotation,
                    origin,
                    Vector2.One * Projectile.scale,
                    effects,
                    3.5f + flashPulse * 7f,
                    flashPulse * 0.9f,
                    Main.GlobalTimeWrappedHourly + Projectile.identity * 0.17f,
                    22,
                    manageBlendState: true);
            }

            DrawMuzzleGlow(flashPulse);
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }

        private void DrawMuzzleGlow(float flashPulse)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 muzzle = GunTipPosition + direction * 8f - Main.screenPosition;
            Color cyan = new(85, 220, 255, 0);
            Color white = new(235, 255, 255, 0);
            float charge = ChargeCompletion;
            float time = Main.GlobalTimeWrappedHourly;

            Main.EntitySpriteDraw(
                bloom,
                muzzle,
                null,
                Color.Lerp(cyan, white, charge) * (0.22f + charge * 0.24f + flashPulse * 0.5f),
                0f,
                bloom.Size() * 0.5f,
                new Vector2(0.32f + charge * 0.24f + flashPulse * 0.35f, 0.18f + charge * 0.13f),
                SpriteEffects.None,
                0);

            for (int i = 0; i < 3; i++)
            {
                float rotation = direction.ToRotation() + MathHelper.TwoPi * i / 3f + time * (1.2f + i * 0.12f);
                Main.EntitySpriteDraw(
                    star,
                    muzzle,
                    null,
                    Color.Lerp(cyan, white, 0.58f) * (0.18f + charge * 0.22f + flashPulse * 0.56f),
                    rotation,
                    star.Size() * 0.5f,
                    new Vector2(0.24f + flashPulse * 0.25f, 1.05f + charge * 0.62f + flashPulse * 1.2f),
                    SpriteEffects.None,
                    0);
            }
        }
    }
}

