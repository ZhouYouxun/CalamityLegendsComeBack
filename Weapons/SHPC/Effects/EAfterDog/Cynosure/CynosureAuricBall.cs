using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    public class CynosureAuricBall : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/注能金源珠九帧图";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private const int Lifetime = 300;
        private const int ScatterSlowdownFrames = 120;
        private const float HomingRange = 2600f;
        private const float StartingHomingSpeed = 24f;
        private const float MaxHomingSpeed = 96f;
        private const float ScatterLongSpeed = 30f;
        private const float ScatterShortSpeed = 12f;
        private const float ScatterDamping = 0.975f;
        private static readonly Color AuricBlue = new(55, 185, 255);
        private static readonly Color AuricGold = new(255, 218, 84);
        private static readonly Color AuricWhite = new(225, 248, 255);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 9;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            if (Projectile.numUpdates == 0 && ++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Projectile.localAI[0] == 0f)
                InitializeScatter();

            float age = Lifetime - Projectile.timeLeft;
            NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], Projectile.Center);
            if (age < ScatterSlowdownFrames)
                Projectile.velocity *= ScatterDamping;
            else
                HomeWithAcceleration(target, age);

            if (Projectile.velocity.Length() > 0.1f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.5f, 0.92f) * 0.55f);
            SpawnAuricTrail(age);

            if (Projectile.timeLeft % 2 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.05f, 0, AuricBlue, 0.62f);
                dust.noGravity = true;
            }
        }

        private void InitializeScatter()
        {
            Projectile.localAI[0] = 1f;
            bool secondEllipse = Projectile.ai[1] != 0f;
            float ellipseRotation = secondEllipse ? MathHelper.Pi * 0.75f : MathHelper.PiOver4;
            float angle = Projectile.ai[2];
            float deterministicJitter = 1f + MathF.Sin(angle * 7f + (secondEllipse ? 1.7f : 0f)) * 0.08f;
            Projectile.velocity = new Vector2(
                MathF.Cos(angle) * ScatterLongSpeed,
                MathF.Sin(angle) * ScatterShortSpeed
            ).RotatedBy(ellipseRotation) * deterministicJitter;
            Projectile.Center += Projectile.velocity;
        }

        private void HomeWithAcceleration(NPC target, float age)
        {
            float homingAge = age - ScatterSlowdownFrames;

            if (target == null || !target.active || target.friendly || target.dontTakeDamage || Projectile.Distance(target.Center) > HomingRange)
            {
                FreeDrift(0.991f);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            if (currentVelocity.Length() < 0.1f)
                currentVelocity = Projectile.SafeDirectionTo(target.Center) * StartingHomingSpeed;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(Vector2.UnitX));
            float ramp = Utils.GetLerpValue(0f, 62f, homingAge, true);
            ramp *= ramp;
            float closePressure = Utils.GetLerpValue(520f, 90f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Clamp(MathHelper.Lerp(0.2f, 0.94f, Math.Max(ramp, closePressure * 0.95f)), 0.2f, 0.94f);
            float targetSpeed = MathHelper.Lerp(StartingHomingSpeed, MaxHomingSpeed, ramp) + closePressure * 8f;
            Vector2 desiredVelocity = desiredDirection * targetSpeed;
            float inertia = MathHelper.Lerp(18f, 1.45f, pullStrength);

            Projectile.velocity = (currentVelocity * inertia + desiredVelocity) / (inertia + 1f);

            float sway = MathF.Sin((homingAge + Projectile.identity * 9f) * 0.085f) * MathHelper.Lerp(0.016f, 0.002f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sway);

            float speedLimit = MaxHomingSpeed + 14f;
            if (Projectile.velocity.Length() > speedLimit)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * speedLimit;
        }

        private void FreeDrift(float damping = 0.996f)
        {
            float wander = MathF.Sin((Lifetime - Projectile.timeLeft + Projectile.identity * 5f) * 0.08f) * 0.006f;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private void SpawnAuricTrail(float age)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float ramp = Utils.GetLerpValue(ScatterSlowdownFrames, ScatterSlowdownFrames + 90f, age, true);

            if (Projectile.timeLeft % 3 == 0)
            {
                Color trailColor = Main.rand.NextBool(4) ? AuricGold : Color.Lerp(AuricBlue, AuricWhite, Main.rand.NextFloat(0.1f, 0.7f));
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(3f, 12f) + side * Main.rand.NextFloat(-5f, 5f),
                    -forward * Main.rand.NextFloat(0.9f, 2.8f + ramp * 4f) + side * Main.rand.NextFloat(-0.45f, 0.45f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.13f, 0.28f + ramp * 0.22f),
                    trailColor));
            }

            if (Projectile.timeLeft % 5 == 0 && Main.rand.NextBool(2))
            {
                float phase = age * 0.21f + Projectile.ai[2];
                Vector2 curl = side * MathF.Sin(phase * 2f) * Main.rand.NextFloat(3f, 10f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 18f) + curl,
                    DustID.Electric,
                    -forward * Main.rand.NextFloat(0.6f, 2.4f) + side * MathF.Cos(phase * 3f) * 0.45f,
                    0,
                    Main.rand.NextBool(3) ? AuricGold : AuricWhite,
                    Main.rand.NextFloat(0.78f, 1.22f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { Volume = 0.14f, Pitch = 0.26f, PitchVariance = 0.12f, MaxInstances = 4 }, Projectile.Center);
            CynosureVisuals.SpawnElectricBurst(Projectile.Center, 7, 2.8f, 12f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(AuricBlue, Color.White, 0.45f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.035f,
                0.24f,
                13));

            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f + Projectile.ai[2];
                Vector2 direction = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(2f, 8f),
                    direction * Main.rand.NextFloat(2f, 9f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.18f, 0.42f),
                    i % 2 == 0 ? AuricGold : AuricBlue));
            }
        }

        public override bool? CanDamage() => Lifetime - Projectile.timeLeft >= ScatterSlowdownFrames ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i -= 2)
            {
                Color afterimageColor = Color.Lerp(AuricBlue, AuricGold, i / (float)Projectile.oldPos.Length) with { A = 0 };
                afterimageColor *= ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length) * 0.62f;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    afterimageColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            float outlinePulse = 1f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * (2.2f * outlinePulse);
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.Center + offset - Main.screenPosition,
                    frame,
                    (AuricBlue with { A = 0 }) * 0.48f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 1.08f,
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
