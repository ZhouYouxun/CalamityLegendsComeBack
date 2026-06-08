using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFFakeCalamityEffect
    {
        private const int ChargeFrames = 108;
        private const int CooldownFrames = 34;
        private const float FireSpeed = 19.5f;
        private const float DamageMultiplier = 3.25f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                if (justReleased && holdout.LeftChargeTimer >= ChargeFrames && holdout.LeftAuxTimer <= 0)
                    FireNovaOrb(holdout);

                KillChargeOrb(holdout);
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (justPressed)
            {
                holdout.LeftTimer = 0;
                holdout.LeftChargeTimer = 0;
                holdout.LeftAuxTimer = 0;
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeStart") { Volume = 0.55f }, holdout.GunTipPosition);
            }

            if (holdout.LeftAuxTimer > 0)
            {
                holdout.LeftAuxTimer--;
                return;
            }

            holdout.LeftChargeTimer = Math.Min(ChargeFrames, holdout.LeftChargeTimer + 1);
            float charge = holdout.LeftChargeTimer / (float)ChargeFrames;
            EnsureChargeOrb(holdout, charge);
            SpawnChargeDust(holdout, charge);

            if (holdout.LeftChargeTimer >= ChargeFrames && holdout.LeftTimer == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLV1") { Volume = 0.65f, Pitch = -0.1f }, holdout.GunTipPosition);
                holdout.LeftTimer = 1;
            }
            else if (holdout.LeftChargeTimer > 12 && holdout.LeftChargeTimer < ChargeFrames && holdout.LeftChargeTimer % 38 == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeLoop") { Volume = 0.32f, Pitch = 0.16f }, holdout.GunTipPosition);
        }

        private static void EnsureChargeOrb(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            int chargeType = ModContent.ProjectileType<PFFakeCalamity_ChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == holdout.Projectile.owner && projectile.type == chargeType && (int)projectile.ai[0] == holdout.Projectile.whoAmI)
                {
                    projectile.ai[1] = charge;
                    projectile.netUpdate = true;
                    return;
                }
            }

            int orb = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + holdout.AimDirection * 8f,
                holdout.AimDirection,
                chargeType,
                0,
                0f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI,
                charge);
            PFLeftEffectRules.ApplyTheme(orb, holdout.CurrentMark);
        }

        private static void KillChargeOrb(NewLegendPristineFuryHoldOut holdout)
        {
            int chargeType = ModContent.ProjectileType<PFFakeCalamity_ChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == holdout.Projectile.owner && projectile.type == chargeType && (int)projectile.ai[0] == holdout.Projectile.whoAmI)
                    projectile.Kill();
            }
        }

        private static void FireNovaOrb(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.myPlayer != holdout.Projectile.owner)
                return;

            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 muzzle = holdout.GunTipPosition + direction * 18f;
            int orb = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction * FireSpeed,
                ModContent.ProjectileType<PFFakeCalamity_NovaOrb>(),
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack * 1.6f,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(orb, holdout.CurrentMark);

            SpawnArcNovaDischarge(muzzle, direction, PristineFuryMarkHelper.GetColor(holdout.CurrentMark));
            holdout.LeftAuxTimer = CooldownFrames;
            holdout.LeftChargeTimer = 0;
            holdout.ApplyRecoil(22f);
            holdout.TriggerMuzzleFlash(26);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 1.45f);
            holdout.Owner.SetScreenshake(6f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/CalamitasClone/CalClone_BigFireballBit" + Main.rand.Next(1, 5)) { Volume = 0.85f, PitchVariance = 0.12f }, muzzle);
        }

        private static void SpawnArcNovaDischarge(Vector2 muzzle, Vector2 direction, Color theme)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 45; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle, ModContent.DustType<SquashDust>());
                dust.velocity = direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(7f, 18f);
                dust.scale = Main.rand.NextFloat(1.05f, 1.85f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool(4) ? Color.White : theme;
                dust.fadeIn = 1f;
            }

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    muzzle,
                    direction * Main.rand.NextFloat(20f, 27f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    18,
                    0.82f - i * 0.14f,
                    i == 0 ? theme : Color.White,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    shrinkSpeed: 0.3f,
                    glowOpacity: 0.88f));
            }

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                muzzle,
                direction * 9f,
                "CalamityMod/Particles/BloomRing",
                false,
                20,
                0.92f,
                theme,
                Vector2.One,
                true,
                false,
                shrinkSpeed: 0.15f));
        }

        private static void SpawnChargeDust(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.25f + charge * 0.5f)
                return;

            Vector2 direction = holdout.AimDirection;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = -direction * Main.rand.NextFloat(18f, 78f + charge * 34f) + side * Main.rand.NextFloat(-14f - charge * 16f, 14f + charge * 16f);
            Dust dust = Dust.NewDustPerfect(
                holdout.GunTipPosition + offset,
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                -offset.SafeNormalize(direction) * Main.rand.NextFloat(1.8f, 5.4f + charge * 2.2f),
                40,
                Color.Lerp(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), Color.White, Main.rand.NextFloat(0.06f, 0.32f)),
                Main.rand.NextFloat(0.72f, 1.28f) * (0.75f + charge * 0.65f));
            dust.noGravity = true;
        }
    }

    internal sealed class PFFakeCalamity_ChargeOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Charge => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float FullChargePulseCreated => ref Projectile.localAI[1];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            int holdoutIndex = (int)HoldoutIndex;
            if (!Main.projectile.IndexInRange(holdoutIndex) || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
            {
                Projectile.Kill();
                return;
            }

            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            Vector2 direction = holdout.AimDirection;
            Projectile.Center = holdout.GunTipPosition + direction * (8f + charge * 6f);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            Projectile.timeLeft = 2;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.36f + charge * 1.2f));
            if (Main.dedServ)
                return;

            if (charge >= 1f && FullChargePulseCreated == 0f)
            {
                FullChargePulseCreated = 1f;
                SpawnFullChargePulse();
            }
        }

        private void SpawnFullChargePulse()
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, ThemeColor * 0.86f, Vector2.One, Projectile.rotation, 0.08f, 1.08f, 24));
            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    (MathHelper.TwoPi * i / 30f).ToRotationVector2() * Main.rand.NextFloat(5.8f, 9f),
                    0,
                    Main.rand.NextBool(3) ? Color.White : ThemeColor,
                    Main.rand.NextFloat(1.05f, 1.48f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            if (charge <= 0.02f || Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 不要 A = 0，否则核心可能直接透明
            Color theme = Color.Lerp(ThemeColor, Color.White, charge * 0.28f) * charge;
            Color white = Color.White * charge;

            float pulse = 0.92f + (float)Math.Sin(Timer * 0.18f) * 0.12f;
            float chargeScale = MathHelper.Lerp(0.35f, 1.45f, charge);

            PFLeftEffectRules.BeginAdditive();

            // 向枪口汇聚的拉丝能量
            for (int i = 0; i < 0; i++)
            {
                float angle = MathHelper.TwoPi * i / 6f + Timer * 0.045f;
                Vector2 orbit = angle.ToRotationVector2();
                Vector2 startOffset = new Vector2(orbit.X * 28f, orbit.Y * 12f).RotatedBy(Projectile.rotation) * chargeScale;

                Vector2 smearPosition = drawPosition + startOffset - direction * (18f + charge * 18f);

                Main.EntitySpriteDraw(
                    smear,
                    smearPosition,
                    null,
                    theme * 0.72f,
                    direction.ToRotation() - MathHelper.PiOver2,
                    new Vector2(smear.Width * 0.5f, smear.Height),
                    new Vector2(0.45f + charge * 0.35f, 0.12f + charge * 0.08f),
                    SpriteEffects.None,
                    0f);
            }

            // 外层大辉光
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                theme * 0.95f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                chargeScale * 0.52f * pulse,
                SpriteEffects.None,
                0f);

            // 内层白热核心
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                white * 0.62f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                chargeScale * 0.22f * pulse,
                SpriteEffects.None,
                0f);

            // 横向压缩核心，让它像枪口前的能量团
            Main.EntitySpriteDraw(
                bloom,
                drawPosition + direction * 2f,
                null,
                Color.Lerp(theme, white, 0.35f) * 0.78f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(chargeScale * 0.62f, chargeScale * 0.34f) * pulse,
                SpriteEffects.None,
                0f);

            // 外层旋转环
            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                theme * 0.58f,
                Projectile.rotation + Timer * 0.04f,
                ring.Size() * 0.5f,
                chargeScale * 0.42f * pulse,
                SpriteEffects.None,
                0f);

            // 满蓄力后出现三颗卫星核心
            if (charge >= 1f)
            {
                for (int satellite = 0; satellite < 3; satellite++)
                {
                    float angle = MathHelper.TwoPi * satellite / 3f + Timer * 0.16f;
                    Vector2 orbit = angle.ToRotationVector2();
                    Vector2 offset = new Vector2(orbit.X * 22f, orbit.Y * 12f).RotatedBy(Projectile.rotation);

                    Main.EntitySpriteDraw(
                        bloom,
                        drawPosition + offset,
                        null,
                        theme * 0.72f,
                        0f,
                        bloom.Size() * 0.5f,
                        0.14f * pulse,
                        SpriteEffects.None,
                        0f);

                    Main.EntitySpriteDraw(
                        bloom,
                        drawPosition + offset,
                        null,
                        white * 0.42f,
                        0f,
                        bloom.Size() * 0.5f,
                        0.065f * pulse,
                        SpriteEffects.None,
                        0f);
                }
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }




    }

    internal sealed class PFFakeCalamity_NovaOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 720;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 7;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 1.05f);

            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust squash = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                ModContent.DustType<SquashDust>());
            squash.noGravity = true;
            squash.scale = Main.rand.NextFloat(0.9f, 1.3f);
            squash.color = Main.rand.NextBool(4) ? Color.White : ThemeColor;
            squash.fadeIn = -0.4f;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f) + Main.rand.NextVector2Circular(9f, 9f),
                    -direction * Main.rand.NextFloat(0.6f, 1.8f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.35f, 0.68f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.08f, 0.38f)),
                    true,
                    false,
                    true));
            }

            if (Timer < 128f && Main.rand.NextBool())
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Projectile.velocity * 0.2f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    32,
                    Main.rand.NextFloat(0.48f, 0.72f),
                    Main.rand.NextBool(4) ? Color.White : ThemeColor,
                    new Vector2(0.2f, 1.4f),
                    true,
                    true,
                    shrinkSpeed: 0.1f));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - direction * 14f + Main.rand.NextVector2Circular(6f, 6f),
                    -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.2f, 3.8f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.16f, 0.3f),
                    Main.rand.NextBool(4) ? Color.White : ThemeColor,
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.7f));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
            target.AddBuff(BuffID.OnFire3, 240);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/SCalSounds/BrimstoneFireblastImpact") { Volume = 0.65f, PitchVariance = 0.15f }, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int explosion = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PFFakeCalamity_NovaExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.72f)),
                    Projectile.knockBack,
                    Projectile.owner);
                PFLeftEffectRules.ApplyTheme(explosion, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/CalamitasClone/CalClone_Explosion" + Main.rand.Next(1, 4)) { Volume = 0.82f, PitchVariance = 0.15f }, Projectile.Center);
            PFFakeCalamity_NovaExplosion.SpawnExplosionEffects(Projectile.Center, ThemeColor, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Color theme = ThemeColor with { A = 0 };
            Color white = Color.White with { A = 0 };
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 0; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, oldDrawPosition, null, theme * (0.28f * (1f - completion) * fade), Projectile.rotation, bloom.Size() * 0.5f, 0.24f * (1f - completion), SpriteEffects.None, 0f);
            }

            float pulse = 0.9f + (float)Math.Sin(Timer * 0.22f) * 0.12f;
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * 0.94f * fade, Projectile.rotation, bloom.Size() * 0.5f, 0.5f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawPosition, null, white * 0.38f * fade, Projectile.rotation, bloom.Size() * 0.5f, 0.2f * pulse, SpriteEffects.None, 0f);
            for (int i = 0; i < 0; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.045f;
                Main.EntitySpriteDraw(star, drawPosition, null, Color.Lerp(theme, white, 0.18f) * 0.48f * fade, rotation, star.Size() * 0.5f, new Vector2(0.14f, 1.45f) * pulse, SpriteEffects.None, 0f);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFFakeCalamity_NovaExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            Projectile.Resize(280, 280);
            Projectile.Damage();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityMod.CalamityUtils.CircularHitboxCollision(Projectile.Center, 140f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 420);
            target.AddBuff(BuffID.OnFire3, 300);
        }

        internal static void SpawnExplosionEffects(Vector2 center, Color theme, float scale)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, theme * 0.88f, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.12f, 1.7f * scale, 24));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.White * 0.55f, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.05f, 1.35f * scale, 20, true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, theme * 0.65f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.06f, 0.95f * scale, 18, true));

            for (int i = 0; i < 42; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 11f) * scale;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(14, 28), Main.rand.NextFloat(0.75f, 1.45f) * scale, Main.rand.NextBool(5) ? Color.White : Color.Lerp(theme, Color.Goldenrod, Main.rand.NextFloat())));
            }

            for (int i = 0; i < 26; i++)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(center + Main.rand.NextVector2Circular(52f, 52f) * scale, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.4f) * scale, false, Main.rand.Next(12, 24), Main.rand.NextFloat(0.8f, 1.22f), Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.38f))));
            }

            for (int i = 0; i < 50; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(54f, 54f) * scale,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 9f) * scale,
                    60,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.06f, 0.3f)),
                    Main.rand.NextFloat(0.85f, 1.7f) * scale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
