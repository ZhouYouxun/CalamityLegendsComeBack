using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeSwordWave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private readonly List<Vector2> oldCenters = new();
        private ref float Time => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.945f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 4f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.48f);

            oldCenters.Insert(0, Projectile.Center);
            if (oldCenters.Count > 8)
                oldCenters.RemoveAt(oldCenters.Count - 1);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.42f, Pitch = 0.28f }, Projectile.Center);
                ApplyScreenShake(3.6f);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-26f, 26f),
                    DustID.SnowflakeIce,
                    direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.4f, 1.25f),
                    120,
                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostCoreColor : CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(34f, 18f),
                        -direction * Main.rand.NextFloat(2.5f, 6.5f),
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.34f, 0.62f),
                        CosmicDischargeCommon.FrostCoreColor));
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 18f;
            Vector2 end = Projectile.Center + direction * 118f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 42f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyColdDebuffs(target, 150);
            ApplyScreenShake(4.8f);

            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = 0.42f,
                Pitch = 0.16f,
                MaxInstances = 4
            }, target.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                direction,
                CosmicDischargeCommon.FrostWhiteColor * 0.34f,
                Vector2.One,
                direction.ToRotation(),
                0.035f,
                0.22f,
                14));

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.86f) * Main.rand.NextFloat(2.4f, 8.8f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(14f, 14f),
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.34f, 0.62f),
                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostCoreColor : CosmicDischargeCommon.FrostWhiteColor));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 scale = new Vector2(1.65f, 0.32f) * Projectile.Opacity;
            Color outer = CosmicDischargeCommon.FrostGlowColor * 0.18f * Projectile.Opacity;
            Color inner = CosmicDischargeCommon.FrostCoreColor * 0.34f * Projectile.Opacity;

            for (int i = oldCenters.Count - 1; i >= 0; i--)
            {
                float fade = 1f - i / (float)oldCenters.Count;
                Main.EntitySpriteDraw(
                    bloom,
                    oldCenters[i] - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.FrostDarkColor * 0.12f * fade * Projectile.Opacity,
                    Projectile.rotation,
                    origin,
                    scale * MathHelper.Lerp(0.75f, 1.25f, fade),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, outer, Projectile.rotation, origin, scale * 1.45f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, inner, Projectile.rotation, origin, scale, SpriteEffects.None);
            return false;
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1300f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }

    public class CosmicDischargeIceBomb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DetonateDelay => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];
        private bool detonated;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.coldDamage = true;
        }

        public override bool ShouldUpdatePosition() => !detonated;

        public override bool? CanDamage() => detonated && Time <= DetonateDelay + 3f;

        public override void AI()
        {
            if (DetonateDelay <= 0f)
                DetonateDelay = 22f;

            Time++;
            Projectile.velocity *= 0.94f;
            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.3f);

            if (!detonated && Time >= DetonateDelay)
                Detonate();

            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = Utils.GetLerpValue(DetonateDelay + 15f, DetonateDelay, Time, true);
                if (Time >= DetonateDelay + 16f)
                    Projectile.Kill();
            }
            else if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Frost,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!detonated)
                return false;

            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= 92f * 92f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyColdDebuffs(target, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;

            if (!detonated)
            {
                float pulse = 0.7f + 0.18f * MathF.Sin(Time * 0.35f);
                Main.EntitySpriteDraw(
                    bloom,
                    Projectile.Center - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.FrostCoreColor * 0.26f,
                    Projectile.rotation,
                    origin,
                    0.18f * pulse,
                    SpriteEffects.None);
                return false;
            }

            float progress = Utils.GetLerpValue(DetonateDelay, DetonateDelay + 16f, Time, true);
            float fade = Utils.GetLerpValue(DetonateDelay + 16f, DetonateDelay + 4f, Time, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.FrostGlowColor * 0.26f * fade,
                0f,
                origin,
                MathHelper.Lerp(0.35f, 1.45f, progress),
                SpriteEffects.None);
            return false;
        }

        private void Detonate()
        {
            detonated = true;
            Projectile.Resize(184, 184);
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.48f, Pitch = 0.28f }, Projectile.Center);
            ApplyScreenShake(4.4f);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new PulseRing(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.FrostCoreColor * 0.58f,
                0.05f,
                1.2f,
                18));

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.SnowflakeIce,
                    velocity,
                    120,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        velocity * 1.25f,
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.3f, 0.52f),
                        CosmicDischargeCommon.FrostCoreColor));
                }
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1100f, 100f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
