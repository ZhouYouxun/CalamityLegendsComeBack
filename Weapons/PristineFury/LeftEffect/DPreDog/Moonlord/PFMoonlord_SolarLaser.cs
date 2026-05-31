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
        private Color SolarColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

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
            Color orange = new(255, 92, 32);
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
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(basePosition, beamVector * 0.55f + normal * wave * 0.2f, false, 7, 0.34f, Color.Lerp(color, white, 0.32f), true, false, true));
                }

                Particle cut = new GlowSparkParticle(
                    basePosition + offset,
                    beamVector.RotatedBy(wave * 0.42f) * 1.9f,
                    false,
                    6,
                    0.03f,
                    color,
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

            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 start = Projectile.Center.Floor() + beamVector * Projectile.scale * 10f - Main.screenPosition;
            Vector2 end = start + beamVector * BeamLength;
            Vector2 scale = new(Projectile.scale);
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);
            Color beamColor = Color.Lerp(SolarColor, Color.White, 0.32f);

            DelegateMethods.f_1 = 1f;
            DelegateMethods.c_1 = beamColor * 0.86f * Projectile.Opacity;
            Utils.DrawLaser(Main.spriteBatch, texture, start, end, scale, framing);

            for (int i = 0; i < 3; i++)
            {
                beamColor = Color.Lerp(beamColor, Color.White, 0.5f);
                scale *= 0.78f;
                DelegateMethods.c_1 = beamColor * 0.52f * Projectile.Opacity;
                Utils.DrawLaser(Main.spriteBatch, texture, start, end, scale, framing);
            }

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

            Color gold = new(255, 194, 60);
            Color orange = new(255, 94, 36);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, gold, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.38f, 16, true));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, orange, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.2f, 2.1f, 18));

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
