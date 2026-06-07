using CalamityMod.Buffs.DamageOverTime;
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
    internal sealed class PFMoonlord_SolarLaser : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 7;
        private const float MaxBeamScale = 6.2f;
        private const float MaxBeamLength = 1800f;
        private const float HitboxWidth = 68f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        private Vector2 beamVector = Vector2.UnitX;
        private ref float BeamLength => ref Projectile.ai[0];
        private Color SolarColor => new(126, 255, 156);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                beamVector = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.rotation = beamVector.ToRotation();
                Projectile.velocity = Vector2.Zero;
                BeamLength = MaxBeamLength;
            }

            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = Utils.GetLerpValue(0f, 0.2f, completion, true) * Utils.GetLerpValue(1f, 0.68f, completion, true);
            Projectile.scale = MaxBeamScale * fade;
            Lighting.AddLight(Projectile.Center, SolarColor.ToVector3() * (0.65f + fade * 1.05f));
            ProduceBeamDust();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + beamVector * BeamLength,
                HitboxWidth * Projectile.scale,
                ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) =>
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

            if (Main.myPlayer != Projectile.owner)
                return;

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PFMoonlord_SolarExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.48f)),
                Projectile.knockBack * 0.35f,
                Projectile.owner);

            PFLeftEffectRules.ApplyTheme(projectileIndex, (PristineFuryMark)(int)Projectile.ai[2]);
        }

        private void ProduceBeamDust()
        {
            if (Main.dedServ || beamVector == Vector2.Zero || BeamLength <= 2f || Projectile.scale <= 0.05f)
                return;

            Vector2 normal = beamVector.RotatedBy(MathHelper.PiOver2);
            Color gold = SolarColor;
            Color orange = new(34, 12, 46);
            Color white = Color.White;
            int points = Math.Clamp((int)(BeamLength / 34f), 18, 56);
            float time = Main.GlobalTimeWrappedHourly;

            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(
                Projectile.Center,
                beamVector * BeamLength,
                0.95f,
                Color.Lerp(gold, white, 0.42f) * 0.38f * Projectile.scale,
                10));

            for (int i = 0; i < points; i++)
            {
                float completion = points == 1 ? 0f : i / (float)(points - 1);
                Vector2 basePosition = Projectile.Center + beamVector * BeamLength * completion;
                float profile = (float)Math.Sin(completion * MathHelper.Pi);
                float wave = (float)Math.Sin(time * 8.6f + completion * MathHelper.TwoPi * 3.8f + Projectile.identity * 0.2f);
                Vector2 offset = normal * wave * MathHelper.Lerp(2f, 8f, profile);
                Color color = Color.Lerp(gold, orange, completion * 0.35f + 0.24f);

                if ((i & 1) == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(basePosition, beamVector * 0.55f + normal * wave * 0.2f, false, 7, 0.26f, Color.Lerp(color, white, 0.26f), true, false, true));
                }

                Particle cut = new GlowSparkParticle(
                    basePosition + offset,
                    beamVector.RotatedBy(wave * 0.42f) * 1.9f,
                    false,
                    6,
                    0.03f,
                    Main.rand.NextBool(3) ? Color.Black : color,
                    new Vector2(2.6f, 1f),
                    true,
                    false,
                    1.15f);

                GeneralParticleHandler.SpawnParticle(cut);
            }

        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (beamVector == Vector2.Zero || BeamLength <= 0f || Projectile.scale <= 0.03f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, SolarColor);
            float opacity = Projectile.Opacity;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + beamVector * BeamLength - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();

            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            float drawScale = 1.3f * (Projectile.scale / MaxBeamScale);
            float rotation = beamVector.ToRotation() - MathHelper.PiOver2;
            Vector2 scaleVec = new Vector2(drawScale, drawScale);

            // Draw start piece
            Main.spriteBatch.Draw(startTex, start, null, theme * opacity, rotation, startTex.Size() / 2f, scaleVec, SpriteEffects.None, 0f);

            float currentLength = BeamLength;
            currentLength -= (startTex.Height / 2 + endTex.Height) * drawScale;
            Vector2 center = Projectile.Center + beamVector * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * (Projectile.timeLeft / 3 % 4);
                Rectangle sourceRect = new Rectangle(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                    {
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    }
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, theme * opacity, rotation, new Vector2(sourceRect.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += beamVector * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                    {
                        sourceRect.Y = 0;
                    }
                }
            }

            Vector2 endPos = center - Main.screenPosition;
            Main.spriteBatch.Draw(endTex, endPos, null, theme * opacity, rotation, new Vector2(endTex.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);

            // Origin glow
            Main.EntitySpriteDraw(bloom, start, null, theme * (0.86f * opacity), 0f, bloom.Size() * 0.5f, 0.6f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * (0.65f * opacity), 0f, bloomRing.Size() * 0.5f, 0.9f * drawScale, SpriteEffects.None, 0);

            // End glow
            Main.EntitySpriteDraw(bloom, end, null, theme * (0.9f * opacity), 0f, bloom.Size() * 0.5f, 0.7f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.52f * opacity), 0f, bloom.Size() * 0.5f, 0.3f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFMoonlord_SolarExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 118;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            if (Timer == 1f)
            {
                Projectile.Damage();
                SpawnExplosionEffects();
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.35f, MaxInstances = 8 }, Projectile.Center);
            }
        }

        public override bool? CanDamage() => Timer <= 1f;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);

        private void SpawnExplosionEffects()
        {
            if (Main.dedServ)
                return;

            Color gold = new(126, 255, 156);
            Color orange = new(32, 10, 48);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.38f, 16, true));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, gold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.2f, 2.1f, 18));

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * Main.rand.NextFloat(3.6f, 8.5f);
                Particle spark = new CustomSpark(
                    Projectile.Center,
                    velocity,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.12f, 0.22f),
                    Main.rand.NextBool() ? gold : orange,
                    Vector2.One,
                    true,
                    true,
                    glowOpacity: 0.35f);

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
