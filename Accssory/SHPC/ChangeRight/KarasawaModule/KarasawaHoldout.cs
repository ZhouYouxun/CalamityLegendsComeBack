using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
#if KARASAWA_MODULE_ENABLED
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.KarasawaModule
{
    internal sealed class KarasawaHoldout : RightClickHoldoutBase, ILocalizedModType
    {
        private const int WarningChargeFrames = 90;
        private const int MinChargeFrames = 300;
        private const int FullChargeFrames = 450;
        private const int FireRecoveryFrames = 45;
        private const int FailureCooldownFrames = 240;
        private const int ManaPerShot = 80;

        private SlotId chargeSoundSlot;
        private SlotId pulseSoundSlot;
        private int progressState;
        private int currentEffectID;
        private int sparkTimer;
        private bool launched;
        private bool failed;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendSHPC>();

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 62f;

        private Vector2 ChargeCorePosition =>
            GunTipPosition + Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.spriteDirection) *
            MathHelper.Clamp((Charge - WarningChargeFrames) / 36f, 0f, 14f);

        public override float MaxOffsetLengthFromArm => 42f;
        public override float RecoilResolveSpeed => 0.075f;
        public override float OffsetXUpwards => -12f;
        public override float OffsetXDownwards => 4f;
        public override float BaseOffsetY => -4f;
        public override float OffsetYUpwards => 14f;
        public override float OffsetYDownwards => 4f;

        private ref float Charge => ref Projectile.ai[0];
        private ref float RecoilTimer => ref Projectile.ai[2];

        private float ChargeRatio => MathHelper.Clamp(Charge / FullChargeFrames, 0f, 1f);
        private float ReleaseRatio => MathHelper.Clamp((Charge - MinChargeFrames) / (FullChargeFrames - MinChargeFrames), 0f, 1f);

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            progressState = (int)Projectile.ai[0];
            currentEffectID = (int)Projectile.ai[1];
            Charge = 0f;
            RecoilTimer = 0f;
            FrontArmStretch = Player.CompositeArmStretchAmount.Full;
            BackArmStretch = Player.CompositeArmStretchAmount.Quarter;
            ExtraBackArmRotation = MathHelper.ToRadians(-10f);
            chargeSoundSlot = SoundEngine.PlaySound(NewLegendSHPC.EnergyMinigunSpinUp, Projectile.Center);
            SpawnOpeningVents();
        }

        public override void KillHoldoutLogic()
        {
            if (Owner is null || !Owner.active || Owner.dead || Owner.CCed || Owner.noItems)
            {
                FailIfCharged();
                Projectile.Kill();
                return;
            }

            if (Owner.HeldItem.type != AssociatedItemID ||
                !Owner.GetModPlayer<KarasawaModulePlayer>().KarasawaModuleEquipped)
            {
                FailIfCharged();
                Projectile.Kill();
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            if (Charge < 0f)
            {
                if (RecoilTimer <= 0f)
                    Projectile.Kill();

                return;
            }

            bool holding = Main.mouseRight && NewLegendSHPC.CanUseWorldRightClick(Owner);
            if (holding)
                return;

            if (Charge >= MinChargeFrames)
                ReleaseShot();
            else
            {
                FailIfCharged();
                Projectile.Kill();
            }
        }

        public override void HoldoutAI()
        {
            UpdateActiveSounds();

            if (Charge < 0f)
            {
                if (RecoilTimer > 0f)
                {
                    RecoilTimer--;
                    SpawnRecoveryAfterglow();
                }

                return;
            }

            Charge++;
            sparkTimer++;

            if (Charge > WarningChargeFrames)
            {
                Owner.mount.Dismount(Owner);
                LimitOwnerSpeed();
            }

            if (Charge == 36f)
                SpawnContractionRing(90f, 66, 0.75f);
            if (Charge == 56f)
                SpawnContractionRing(60f, 44, 1.15f);
            if (Charge == 76f)
                SpawnContractionRing(30f, 26, 1.45f);

            if (Charge >= MinChargeFrames && (Charge - MinChargeFrames) % 150f == 10f)
                pulseSoundSlot = SoundEngine.PlaySound(NewLegendSHPC.LightningChainRelease, Projectile.Center);

            SpawnChargeEffects();

            if (Charge > WarningChargeFrames)
            {
                float shake = MathHelper.Clamp((Charge - WarningChargeFrames) * 0.01f, 0f, 3.6f);
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, shake);
            }

            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            StopChargeSounds();
            FailIfCharged();
        }

        private void ReleaseShot()
        {
            if (launched)
                return;

            int manaCost = Owner.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickManaCost(ManaPerShot);
            if (manaCost > 0 && !Owner.CheckMana(Owner.HeldItem, manaCost, true, false))
            {
                FailIfCharged();
                Projectile.Kill();
                return;
            }

            launched = true;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 spawnPosition = GunTipPosition - direction * 20f;
            Vector2 velocity = direction * 5f;
            float multiplier = MathHelper.Clamp(MathHelper.Lerp(1f, 25f, ReleaseRatio), 1f, 25f);
            int damage = Math.Max(1, (int)(Projectile.damage * multiplier));

            if (Main.myPlayer == Projectile.owner)
            {
                int shotIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<KarasawaBurst>(),
                    damage,
                    Projectile.knockBack,
                    Owner.whoAmI,
                    ReleaseRatio,
                    Charge >= FullChargeFrames ? 0f : -1f);

                if (Main.projectile.IndexInRange(shotIndex))
                    Main.projectile[shotIndex].CritChance = Projectile.CritChance;
            }

            SoundEngine.PlaySound(NewLegendSHPC.FinalUltimatumExplosion, GunTipPosition);
            SoundEngine.PlaySound(NewLegendSHPC.LightningChainRelease, GunTipPosition);
            SpawnLaunchBurst(direction);

            Vector2 pushback = direction * MathHelper.Clamp(-Charge / 75f, -6f, -4f);
            Owner.velocity += pushback;
            Owner.Calamity().GeneralScreenShakePower = Math.Max(
                Owner.Calamity().GeneralScreenShakePower,
                MathHelper.Clamp(Charge * 0.1f, 30f, 45f));

            StopChargeSounds();
            Owner.GetModPlayer<SHPCRight_Player>().SetAttackLockout(FireRecoveryFrames + 10);
            OffsetLengthFromArm -= 30f;
            Charge = -1f;
            RecoilTimer = FireRecoveryFrames;
            Projectile.netUpdate = true;
        }

        private void FailIfCharged()
        {
            if (failed || launched || Owner is null || Charge < 100f)
                return;

            failed = true;
            StopChargeSounds();
            Owner.GetModPlayer<SHPCRight_Player>().SetAttackLockout(FailureCooldownFrames);
            SoundEngine.PlaySound(NewLegendSHPC.VacuumEnd, Owner.Center);
            SpawnFailureBurst();
        }

        private void StopChargeSounds()
        {
            if (SoundEngine.TryGetActiveSound(chargeSoundSlot, out ActiveSound chargeSound))
                chargeSound?.Stop();

            if (SoundEngine.TryGetActiveSound(pulseSoundSlot, out ActiveSound pulseSound))
                pulseSound?.Stop();
        }

        private void UpdateActiveSounds()
        {
            if (SoundEngine.TryGetActiveSound(chargeSoundSlot, out ActiveSound chargeSound) && chargeSound.IsPlaying)
                chargeSound.Position = Projectile.Center;

            if (SoundEngine.TryGetActiveSound(pulseSoundSlot, out ActiveSound pulseSound) && pulseSound.IsPlaying)
                pulseSound.Position = Projectile.Center;
        }

        private void LimitOwnerSpeed()
        {
            float speedLimit = (Charge >= MinChargeFrames ? 3f : 4f) * Owner.moveSpeed;
            if (Owner.velocity.Length() <= speedLimit || Owner.pulley)
                return;

            Owner.velocity.X = Math.Min(Math.Abs(Owner.velocity.X), speedLimit) * Math.Sign(Owner.velocity.X == 0f ? Owner.direction : Owner.velocity.X);
            Owner.velocity.Y = Math.Min(Math.Abs(Owner.velocity.Y), speedLimit) * Math.Sign(Owner.velocity.Y == 0f ? Owner.gravDir : Owner.velocity.Y);
        }

        private void SpawnOpeningVents()
        {
            if (Main.dedServ || Owner is null)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < 5; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    Owner.MountedCenter + direction * Main.rand.NextFloat(12f, 34f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.8f, 2.1f),
                    120,
                    Color.Lerp(Color.DarkGray, Color.White, Main.rand.NextFloat(0.1f, 0.45f)),
                    Main.rand.NextFloat(0.75f, 1.25f));
                smoke.noGravity = true;
            }
        }

        private void SpawnContractionRing(float radius, int count, float scale)
        {
            if (Main.dedServ)
                return;

            Vector2 center = GunTipPosition - Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * 14f;
            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPosition = center + Main.rand.NextVector2Unit() * radius * Main.rand.NextFloat(0.75f, 1.1f);
                Vector2 velocity = (center - spawnPosition) * 0.085f + Owner.velocity * 0.5f;
                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.RainbowMk2, velocity);
                dust.scale = scale * Main.rand.NextFloat(0.75f, 1.15f);
                dust.color = Color.Lerp(new Color(70, 210, 255), new Color(255, 88, 64), ReleaseRatio * Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        private void SpawnChargeEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 core = ChargeCorePosition;
            Color color = ChargeColor();
            float chargeRatio = ChargeRatio;

            Lighting.AddLight(core, color.ToVector3() * MathHelper.Lerp(0.16f, 0.95f, chargeRatio));

            int dustCount = (int)MathF.Round(MathHelper.SmoothStep(1f, 6f, chargeRatio));
            float outwardness = MathHelper.SmoothStep(24f, 92f, chargeRatio);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 spawn = core + Main.rand.NextVector2Unit() * outwardness * Main.rand.NextFloat(0.72f, 1.15f);
                Vector2 velocity = (core - spawn) * MathHelper.Lerp(0.055f, 0.13f, chargeRatio) + Owner.velocity * 0.45f;
                Dust dust = Dust.NewDustPerfect(spawn, DustID.RainbowMk2, velocity);
                dust.scale = MathHelper.Lerp(0.45f, 1.65f, chargeRatio) * Main.rand.NextFloat(0.78f, 1.18f);
                dust.color = color * Main.rand.NextFloat(0.65f, 1f);
                dust.noGravity = true;
            }

            if (sparkTimer % 4 == 0)
            {
                Particle spark = new CustomSpark(
                    core + right * Main.rand.NextFloat(-10f, 10f),
                    direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(4.2f, 9f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    12,
                    Main.rand.NextFloat(0.026f, 0.05f) * MathHelper.Lerp(0.8f, 1.65f, chargeRatio),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.15f, 0.6f)),
                    new Vector2(0.9f, 0.75f),
                    shrinkSpeed: 0.78f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Charge >= MinChargeFrames)
            {
                Vector2 spawn = core + Main.rand.NextVector2CircularEdge(64f, 64f);
                Particle pull = new SquishyLightParticle(
                    spawn,
                    spawn.DirectionTo(core) * Main.rand.NextFloat(2f, 4f),
                    Main.rand.NextFloat(0.25f, 0.38f) * MathHelper.Lerp(1f, 2.5f, ReleaseRatio),
                    color,
                    Main.rand.Next(12, 18),
                    0.2f,
                    4f);
                GeneralParticleHandler.SpawnParticle(pull);
            }
        }

        private void SpawnLaunchBurst(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 muzzle = GunTipPosition + direction * 10f;
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Color color = ChargeColor();
            Color white = Color.Lerp(color, Color.White, 0.55f);

            for (int i = 0; i < 28; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(8f, 8f), DustID.RainbowMk2);
                dust.velocity = direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(7f, 18f) + right * Main.rand.NextFloat(-2f, 2f);
                dust.color = Color.Lerp(color, white, Main.rand.NextFloat(0.18f, 0.85f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.15f, 2.05f);
            }

            for (int i = 0; i < 4; i++)
            {
                Particle ring = new DirectionalPulseRing(
                    muzzle,
                    direction * Main.rand.NextFloat(1.2f, 3.2f),
                    Color.Lerp(color, white, Main.rand.NextFloat(0.35f, 0.85f)) * 0.9f,
                    new Vector2(1f, 1f),
                    direction.ToRotation(),
                    0.07f,
                    Main.rand.NextFloat(0.24f, 0.42f),
                    20);

                GeneralParticleHandler.SpawnParticle(ring);
            }

            Particle coreLine = new CustomSpark(
                muzzle,
                direction * 18f,
                "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                false,
                16,
                0.08f,
                white,
                new Vector2(1f, 2.6f),
                glowCenter: true,
                shrinkSpeed: 0.82f,
                glowCenterScale: 1.25f,
                glowOpacity: 0.85f);
            GeneralParticleHandler.SpawnParticle(coreLine);
        }

        private void SpawnFailureBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 core = ChargeCorePosition;
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(core + Main.rand.NextVector2Circular(9f, 9f), DustID.Smoke);
                dust.velocity = Main.rand.NextVector2Circular(5f, 5f) - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1f, 3f);
                dust.color = Color.Lerp(Color.DimGray, new Color(80, 190, 210), Main.rand.NextFloat(0.2f, 0.55f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.85f, 1.45f);
            }
        }

        private void SpawnRecoveryAfterglow()
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 muzzle = GunTipPosition + direction * 8f;
            Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.Electric);
            dust.velocity = -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.5f, 2f);
            dust.color = Color.Lerp(new Color(90, 220, 255), Color.White, Main.rand.NextFloat(0.25f, 0.8f));
            dust.noGravity = true;
            dust.scale = Main.rand.NextFloat(0.65f, 1f);
        }

        private Color ChargeColor()
        {
            Color techBlue = new(80, 215, 255);
            Color hotPink = new(255, 78, 180);
            Color redCore = new(255, 70, 48);
            return Charge < MinChargeFrames
                ? Color.Lerp(techBlue, hotPink, MathHelper.Clamp(Charge / MinChargeFrames, 0f, 1f) * 0.55f)
                : Color.Lerp(hotPink, redCore, ReleaseRatio);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null)
                return false;

            Texture2D texture = TextureAssets.Item[ModContent.ItemType<NewLegendSHPC>()].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float rotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            SpriteEffects effects = (float)Projectile.spriteDirection * Owner.gravDir == -1f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            float visibleCharge = Charge < 0f ? 1f : ChargeRatio;
            float flash = MathHelper.Clamp(RecoilTimer / FireRecoveryFrames, 0f, 1f);
            Color glowColor = ChargeColor();

            if (visibleCharge > 0.02f || flash > 0f)
            {
                HoldoutOutlineHelper.DrawSolidOutline(
                    texture,
                    drawPosition,
                    rotation,
                    origin,
                    Vector2.One * Projectile.scale,
                    effects,
                    glowColor,
                    1.8f + visibleCharge * 4.2f + flash * 6f,
                    0.12f + visibleCharge * 0.32f + flash * 0.55f,
                    Main.GlobalTimeWrappedHourly + Projectile.identity * 0.13f,
                    16,
                    manageBlendState: true);
            }

            DrawMuzzleGlow(visibleCharge, flash);
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);
            return false;
        }

        private void DrawMuzzleGlow(float charge, float flash)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 muzzle = ChargeCorePosition - Main.screenPosition;
            Color color = ChargeColor() with { A = 0 };
            Color white = Color.Lerp(color, Color.White, 0.65f) with { A = 0 };
            float time = Main.GlobalTimeWrappedHourly;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                muzzle,
                null,
                Color.Lerp(color, white, charge) * (0.2f + charge * 0.52f + flash * 0.75f),
                0f,
                bloom.Size() * 0.5f,
                new Vector2(0.38f + charge * 0.55f + flash * 0.42f, 0.24f + charge * 0.22f),
                SpriteEffects.None,
                0);

            int starCount = Charge >= MinChargeFrames || Charge < 0f ? 5 : 3;
            for (int i = 0; i < starCount; i++)
            {
                float rotation = direction.ToRotation() + MathHelper.TwoPi * i / starCount + time * (1.15f + i * 0.14f);
                Main.EntitySpriteDraw(
                    star,
                    muzzle,
                    null,
                    Color.Lerp(color, white, 0.52f + 0.16f * MathF.Sin(time + i)) * (0.18f + charge * 0.34f + flash * 0.7f),
                    rotation,
                    star.Size() * 0.5f,
                    new Vector2(0.28f + flash * 0.25f, 1.25f + charge * 1.1f + flash * 1.8f),
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
#endif
