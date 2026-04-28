using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleHoldout : RightClickHoldoutBase
    {
        private const string SparkTexturePath = "CalamityMod/Particles/BloomLineSoftEdge";

        private static readonly Color SilverMain = new(214, 224, 236);
        private static readonly Color SilverAccent = new(255, 255, 255);
        private static readonly Color SilverDark = new(140, 152, 170);
        public override bool UseBaseDraw => false;
        public override string Texture => DesertEagle.TextureAssetPath;
        public override int AssociatedItemID => ModContent.ItemType<DesertEagle>();
        public override float MaxOffsetLengthFromArm => 24f;
        public override float OffsetXUpwards => -5f;
        public override float BaseOffsetY => -5f;
        public override float OffsetYDownwards => 5f;
        public override float RecoilResolveSpeed => 0.5f;

        public ref float Time => ref Projectile.ai[0];
        public ref float ChargeTimer => ref Projectile.ai[1];

        public float cooldownTimer;
        public bool OnCooldown => cooldownTimer > 0f;
        public SlotId SoundSlot;
        public bool Spinning => Projectile.ai[2] == 0f;

        public bool failedShot;
        public float drawRot;
        public bool hasPlayedSound;
        public bool hasPlayedReloadSound;
        public float fade;
        public int hitTimer;

        private new Player Owner => Main.player[Projectile.owner];
        private bool RightHeld
        {
            get
            {
                if (Main.myPlayer != Projectile.owner)
                    return true;

                DesertEaglePlayer playerState = Owner.GetModPlayer<DesertEaglePlayer>();
                return playerState.TrackingRightPress ||
                    ((Owner.Calamity().mouseRight || Main.mouseRight) && !Main.mapFullscreen && !Main.blockMouse);
            }
        }

        private bool FullyCharged => ChargeTimer >= DesertEaglePlayer.SpinChargeMax;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 46;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.ArmorPenetration = 25;
        }

        public override void HoldoutAI()
        {
            if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            if (hitTimer > 0)
                hitTimer--;

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            DesertEaglePlayer playerState = Owner.GetModPlayer<DesertEaglePlayer>();
            playerState.SetHoldingDesertEagle();
            playerState.ProcessRightClickState();

            Vector2 aimDirection = (Owner.Calamity().mouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.velocity = aimDirection;
            Projectile.rotation = aimDirection.ToRotation();
            Owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            fade = Utils.GetLerpValue(0f, 40f, Time, true);
            if (SoundEngine.TryGetActiveSound(SoundSlot, out ActiveSound spinSound) && spinSound.IsPlaying)
            {
                spinSound.Volume = fade * 0.45f;
                spinSound.Position = Projectile.Center;
            }

            if (OnCooldown)
            {
                PostFiringCooldown(playerState);
                if (!Spinning)
                    return;
            }

            if (Spinning)
            {
                float armRot = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
                float spinArmRot = (((Projectile.Center + new Vector2(0f, -3f).RotatedBy(drawRot * 0.6f)) - Owner.Center).SafeNormalize(Vector2.UnitX) * 5f).ToRotation();
                Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot + 0.6f * Projectile.direction);
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, spinArmRot + MathHelper.ToRadians(-90f));
            }

            if (Time > 3f)
            {
                if (Spinning)
                {
                    if (!RightHeld)
                    {
                        playerState.UpdateChargeBar(false, 0f);
                        Projectile.Kill();
                        spinSound?.Stop();
                    }
                    else
                    {
                        OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, 9f, 0.5f);
                        drawRot += 0.75f * Projectile.direction * MathHelper.Clamp(fade, 0.3f, 1f);

                        ChargeTimer = MathHelper.Clamp(ChargeTimer + 1f, 0f, DesertEaglePlayer.SpinChargeMax);
                        playerState.UpdateChargeBar(true, ChargeTimer / DesertEaglePlayer.SpinChargeMax);

                        Lighting.AddLight(Projectile.Center, SilverMain.ToVector3() * fade * 0.7f);
                        if (!hasPlayedSound && Main.myPlayer == Projectile.owner)
                        {
                            SoundStyle spin = new("CalamityMod/Sounds/Item/SpinningWoosh");
                            SoundSlot = SoundEngine.PlaySound(spin with { Volume = 0.01f, Pitch = -0.1f, IsLooped = true }, Projectile.Center);
                            hasPlayedSound = true;
                        }

                        SpawnSpinDiscVFX();

                        if (Main.myPlayer == Owner.whoAmI && Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Projectile.ai[2] = 1f;
                            Projectile.netUpdate = true;
                        }
                    }
                }
                else if (!OnCooldown)
                {
                    playerState.UpdateChargeBar(false, 0f);
                    if (FullyCharged)
                        FireHeavyShot();
                    else
                        DudShot();
                }
            }

            Time++;
        }

        private void PostFiringCooldown(DesertEaglePlayer playerState)
        {
            Owner.channel = true;
            playerState.UpdateChargeBar(false, 0f);
            cooldownTimer -= Spinning ? 1f : 2f;

            if (cooldownTimer > 1f)
            {
                if (RightHeld && cooldownTimer < Owner.itemAnimationMax * 1.7f && !failedShot && !Main.mouseLeftRelease)
                    Projectile.ai[2] = 0f;

                if (cooldownTimer <= 18f && !hasPlayedReloadSound)
                {
                    if (!failedShot)
                        SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.65f, Pitch = -0.35f }, Projectile.Center);
                    hasPlayedReloadSound = true;
                }

                if (!Spinning && Main.rand.NextBool(2))
                {
                    Vector2 upwardSmoke = new(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-2.8f, -1.2f));
                    SpawnHeavySmoke(GunTipPosition + Main.rand.NextVector2Circular(2f, 2f), upwardSmoke, 0.95f);
                }
            }
            else if (!Spinning)
            {
                if (SoundEngine.TryGetActiveSound(SoundSlot, out ActiveSound spinSound) && spinSound.IsPlaying)
                    spinSound.Stop();
                Projectile.Kill();
            }
        }

        private void FireHeavyShot()
        {
            Vector2 shotDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction);
            ChargeTimer = 0f;
            cooldownTimer = Owner.itemAnimationMax * 2f;
            hasPlayedReloadSound = false;
            failedShot = false;

            OffsetLengthFromArm -= 35f;
            Owner.velocity -= shotDirection * 12f;
            Owner.SetScreenshake(10f);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    shotDirection * 22f,
                    ModContent.ProjectileType<DesertEagleHeavyRound>(),
                    (int)(Projectile.damage * 20.8f),
                    Projectile.knockBack * 3f,
                    Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item38 with { Volume = 1.1f, Pitch = -0.18f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);
            SpawnSilverImpact(GunTipPosition + shotDirection * 10f, shotDirection, 1.25f);
            for (int i = 0; i < 4; i++)
                SpawnHeavySmoke(GunTipPosition, -shotDirection * Main.rand.NextFloat(0.8f, 2.4f) + Main.rand.NextVector2Circular(0.8f, 0.8f), 1.05f);
        }

        private void DudShot()
        {
            ChargeTimer = 0f;
            OffsetLengthFromArm -= 8f;
            failedShot = true;
            hasPlayedReloadSound = false;
            cooldownTimer = Owner.itemAnimationMax;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DudFire") with { PitchVariance = 0.15f, Volume = 0.75f }, Projectile.Center);
        }

        private void SpawnSpinDiscVFX()
        {
            Particle smear = new CircularSmearVFX(
                Projectile.Center,
                SilverMain * Main.rand.NextFloat(0.45f, 0.6f),
                Main.rand.NextFloat(-8f, 8f),
                Main.rand.NextFloat(1.13f, 1.25f) * fade);
            GeneralParticleHandler.SpawnParticle(smear);

            Particle innerSmear = new CircularSmearVFX(
                Projectile.Center,
                SilverAccent * Main.rand.NextFloat(0.28f, 0.42f),
                Main.rand.NextFloat(-8f, 8f),
                Main.rand.NextFloat(0.65f, 0.72f) * fade);
            GeneralParticleHandler.SpawnParticle(innerSmear);

            if (fade <= 0.2f)
                return;

            float signedSpin = drawRot * Projectile.direction * System.Math.Sign(Projectile.velocity.X);
            float flipRotation = MathHelper.ToRadians(180f) * (Projectile.direction == 1 ? 0f : 1f);

            if (Main.rand.NextBool())
            {
                for (int i = 0; i < 2; i++)
                {
                    float edgeAngle = i * MathHelper.Pi + drawRot * 0.25f + MathHelper.PiOver2;
                    float velocityAngle = i * MathHelper.Pi + signedSpin * 0.25f;
                    Vector2 dustPos = Projectile.Center + edgeAngle.ToRotationVector2().RotatedByRandom(0.5f) * 35f * fade;
                    Vector2 dustVelocity = velocityAngle.ToRotationVector2().RotatedBy(flipRotation) * -3f * Main.rand.NextFloat(0.5f, 1f) * fade;

                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.SilverFlame, dustVelocity);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.42f, 0.62f);
                    dust.color = Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat());
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float edgeAngle = i * MathHelper.Pi + drawRot * 0.5f + MathHelper.PiOver2;
                    float velocityAngle = i * MathHelper.Pi + signedSpin * 0.5f;
                    Vector2 dustPos = Projectile.Center + edgeAngle.ToRotationVector2().RotatedByRandom(0.5f) * 87f * fade;
                    Vector2 dustVelocity = velocityAngle.ToRotationVector2().RotatedBy(flipRotation) * -7f * Main.rand.NextFloat(0.5f, 1f) * fade;

                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.TintableDustLighted, dustVelocity);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.62f, 0.82f);
                    dust.color = Color.Lerp(SilverDark, SilverAccent, Main.rand.NextFloat(0.35f, 1f));
                }
            }
        }

        private static void SpawnSpinHitBurst(Vector2 position, Vector2 direction)
        {
            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float baseRotation = forward.ToRotation();
            const int orderedSparkCount = 18;
            const float goldenAngle = 2.3999631f;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, SilverAccent, 0.82f, 16));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.68f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge",
                    Vector2.One,
                    baseRotation,
                    0.01f,
                    0.085f,
                    15,
                    true,
                    0.8f));

                for (int i = 0; i < orderedSparkCount; i++)
                {
                    float angle = baseRotation + i * goldenAngle;
                    float radius = MathHelper.Lerp(4f, 18f, i / (float)(orderedSparkCount - 1));
                    Vector2 sparkDirection = angle.ToRotationVector2();
                    Vector2 sparkPosition = position + sparkDirection * radius + side * (float)System.Math.Sin(i * 1.618f) * 2f;
                    Vector2 sparkVelocity = sparkDirection * MathHelper.Lerp(2.2f, 7.8f, i / (float)orderedSparkCount) + forward * 1.4f;

                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        sparkPosition,
                        sparkVelocity,
                        SparkTexturePath,
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.026f, 0.044f),
                        Color.Lerp(SilverMain, SilverAccent, i % 3 / 2f),
                        new Vector2(Main.rand.NextFloat(0.62f, 0.95f), Main.rand.NextFloat(1.55f, 2.45f)),
                        shrinkSpeed: 0.78f));
                }

                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkDirection = forward.RotatedBy(Main.rand.NextFloat(-1.35f, 1.35f));
                    Vector2 sparkVelocity = sparkDirection * Main.rand.NextFloat(4.5f, 10.5f) + Main.rand.NextVector2Circular(0.7f, 0.7f);

                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position + Main.rand.NextVector2Circular(6f, 6f),
                        sparkVelocity,
                        false,
                        Main.rand.Next(9, 14),
                        Main.rand.NextFloat(0.028f, 0.04f),
                        Main.rand.NextBool() ? SilverAccent : SilverMain,
                        new Vector2(0.85f, 1.9f),
                        true));
                }
            }

            for (int i = 0; i < 24; i++)
            {
                float angle = baseRotation + i * MathHelper.TwoPi / 24f;
                Vector2 dustDirection = angle.ToRotationVector2();
                Vector2 dustVelocity = dustDirection * MathHelper.Lerp(2.2f, 6.4f, i % 6 / 5f) + forward * 1.2f;

                Dust dust = Dust.NewDustPerfect(
                    position + dustDirection * (4f + i % 4),
                    i % 2 == 0 ? DustID.SilverCoin : DustID.SilverFlame,
                    dustVelocity,
                    100,
                    Color.Lerp(SilverDark, SilverAccent, i % 5 / 4f),
                    MathHelper.Lerp(0.72f, 1.18f, i % 5 / 4f));

                dust.noGravity = true;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 dustDirection = forward.RotatedByRandom(1.25f);
                Dust dust = Dust.NewDustPerfect(
                    position + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.TintableDustLighted,
                    dustDirection * Main.rand.NextFloat(2.4f, 7.2f),
                    105,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat(0.2f, 1f)),
                    Main.rand.NextFloat(0.7f, 1.1f));

                dust.noGravity = true;
            }
        }

        private static void SpawnSilverImpact(Vector2 position, Vector2 direction, float scale = 1f)
        {
            Vector2 impactDirection = direction.SafeNormalize(Vector2.UnitY);
            const float pulseScale = 1.15f;
            const int ringLifetime = 24;
            const int sparkCount = 18;
            const int dustCount = 28;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, SilverAccent, 1.15f * scale, 28));

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    position,
                    Vector2.Zero,
                    SilverAccent,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge",
                    Vector2.One,
                    0f,
                    0.01f,
                    0.08f * pulseScale * scale,
                    ringLifetime,
                    true,
                    0.95f));

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.82f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                    Vector2.One,
                    0f,
                    0.01f,
                    0.12f * pulseScale * scale,
                    ringLifetime + 3,
                    true,
                    0.7f));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    position,
                    Vector2.Zero,
                    SilverAccent * 0.75f,
                    new Vector2(1f, 4.8f) * scale,
                    impactDirection.ToRotation(),
                    0.16f,
                    0.034f,
                    ringLifetime));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.55f,
                    new Vector2(1f, 4.2f) * scale,
                    impactDirection.ToRotation() + MathHelper.PiOver2,
                    0.14f,
                    0.03f,
                    ringLifetime - 2));

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / sparkCount;
                    Vector2 sparkDirection = angle.ToRotationVector2();

                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position + sparkDirection * 10f * scale,
                        sparkDirection * 9f * scale,
                        false,
                        16,
                        0.045f * scale,
                        Color.Lerp(SilverMain, SilverAccent, i % 2 == 0 ? 0.75f : 0.35f),
                        new Vector2(1.2f, 0.58f),
                        true));
                }

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * (i + 0.5f) / sparkCount;
                    Vector2 sparkDirection = angle.ToRotationVector2();

                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        position + sparkDirection * 7f * scale,
                        sparkDirection * 7f * scale,
                        SparkTexturePath,
                        false,
                        15,
                        0.04f * scale,
                        i % 2 == 0 ? SilverAccent : SilverMain,
                        new Vector2(0.75f, 1.8f),
                        shrinkSpeed: 0.78f));
                }
            }

            for (int i = 0; i < dustCount; i++)
            {
                float angle = MathHelper.TwoPi * i / dustCount;
                Vector2 dustDirection = angle.ToRotationVector2();

                Dust dust = Dust.NewDustPerfect(
                    position,
                    i % 2 == 0 ? DustID.SilverCoin : DustID.SilverFlame,
                    dustDirection * 7.5f * scale,
                    105,
                    Color.Lerp(SilverDark, SilverAccent, i % 3 / 2f),
                    1.25f * scale);

                dust.noGravity = true;
            }
        }

        private static void SpawnHeavySmoke(Vector2 position, Vector2 baseVelocity, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Particle smoke = new HeavySmokeParticle(
                position,
                baseVelocity * 0.35f,
                Color.Lerp(Color.Gray, SilverDark, 0.5f),
                Main.rand.Next(24, 34),
                Main.rand.NextFloat(0.58f, 0.9f) * scale,
                0.95f,
                0f,
                true);

            GeneralParticleHandler.SpawnParticle(smoke);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Spinning)
            {
                modifiers.SourceDamage *= 0f;
                return;
            }

            if (hitTimer == 0)
            {
                modifiers.SourceDamage *= 0.08f;
                ChargeTimer = MathHelper.Clamp(ChargeTimer + DesertEaglePlayer.SpinChargeMax / 12f, 0f, DesertEaglePlayer.SpinChargeMax);
                hitTimer = Projectile.localNPCHitCooldown;
            }
            else
                modifiers.SourceDamage *= 0.02f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Spinning)
                return;

            SpawnSpinHitBurst(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));
        }

        public override bool? CanDamage() => Spinning && Time > 5f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 100f, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time < 2f)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f) - (Owner.gravDir == -1 ? MathHelper.Pi * Owner.direction : 0f);
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects flipSprite = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float chargeCompletion = MathHelper.Clamp(ChargeTimer / DesertEaglePlayer.SpinChargeMax, 0f, 1f);
            Color outlineColor = Color.Lerp(SilverAccent, Color.White, 0.55f) * (0.12f + 0.5f * chargeCompletion);
            float outlineDistance = 1f + 4f * chargeCompletion;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    Color.Lerp(SilverDark, SilverAccent, 0.3f) * (0.07f * completion),
                    drawRotation + drawRot * completion,
                    origin,
                    Projectile.scale * MathHelper.Lerp(0.92f, 1f, completion),
                    flipSprite,
                    0);
            }

            if (Spinning && chargeCompletion > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * outlineDistance;
                    Main.EntitySpriteDraw(
                        texture,
                        drawPosition + outlineOffset,
                        null,
                        outlineColor,
                        drawRotation + drawRot,
                        origin,
                        Projectile.scale,
                        flipSprite,
                        0);
                }
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition + (Spinning ? new Vector2(0f, -8f).RotatedBy(drawRot * 0.6f) : Vector2.Zero),
                null,
                Projectile.GetAlpha(lightColor),
                drawRotation + drawRot,
                origin,
                Projectile.scale,
                flipSprite,
                0);

            return false;
        }

        public override void KillHoldoutLogic()
        {
        }
    }
}
