using System;
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

        private ref float Time => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 26;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.965f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.36f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(32f, 16f),
                    DustID.SnowflakeIce,
                    Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.1f, 0.45f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
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
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 scale = new Vector2(1.35f, 0.26f) * Projectile.Opacity;
            Color outer = CosmicDischargeCommon.FrostGlowColor * 0.18f * Projectile.Opacity;
            Color inner = CosmicDischargeCommon.FrostCoreColor * 0.34f * Projectile.Opacity;

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, outer, Projectile.rotation, origin, scale * 1.45f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, inner, Projectile.rotation, origin, scale, SpriteEffects.None);
            return false;
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
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.SnowflakeIce,
                    (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f),
                    120,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }
        }
    }
}
