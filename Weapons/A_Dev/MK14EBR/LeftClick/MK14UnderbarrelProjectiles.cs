using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14GrenadeProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/Pic/下挂/m14榴弹";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 130;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity.Y += 0.12f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.34f, 0.22f, 0.08f));

            DrawFuseTrail();
        }

        private void DrawFuseTrail()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float timer = 130f - Projectile.timeLeft;

            if (Main.rand.NextBool(2))
            {
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(8f, 18f) + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.Smoke,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.11f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    145,
                    Color.Lerp(Color.DimGray, Color.SlateGray, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.78f, 1.12f));
                smoke.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 fuseOffset = -forward * Main.rand.NextFloat(7f, 15f) + normal * Main.rand.NextFloat(-2.2f, 2.2f);
                SparkParticle spark = new(
                    Projectile.Center + fuseOffset,
                    -forward.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.2f, 3.4f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.34f, 0.58f),
                    Main.rand.NextBool() ? new Color(255, 184, 72) : new Color(255, 112, 54));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 2; i++)
            {
                float phase = timer * 0.38f + i * MathHelper.Pi;
                Vector2 wavePosition = Projectile.Center - forward * (i * 7f + 4f) + normal * MathF.Sin(phase) * 4.5f;
                Dust tracer = Dust.NewDustPerfect(
                    wavePosition,
                    DustID.GemTopaz,
                    -Projectile.velocity * 0.04f,
                    110,
                    Color.Lerp(new Color(255, 198, 92), new Color(120, 214, 255), i * 0.35f),
                    Main.rand.NextFloat(0.5f, 0.78f));
                tracer.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                GlowSparkParticle glow = new(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 12f),
                    -forward.RotatedByRandom(0.2f) * Main.rand.NextFloat(2f, 4f),
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.012f, 0.02f),
                    new Color(255, 164, 72),
                    new Vector2(Main.rand.NextFloat(1.9f, 2.7f), Main.rand.NextFloat(0.55f, 0.85f)),
                    true,
                    false,
                    0.7f);
                GeneralParticleHandler.SpawnParticle(glow);
            }

            if (Projectile.numUpdates == 0 && Projectile.timeLeft % 7 == 0)
            {
                Particle sideWave = new DirectionalPulseRing(
                    Projectile.Center - forward * 4f,
                    normal * Main.rand.NextFloat(-0.7f, 0.7f),
                    Color.Lerp(new Color(255, 184, 72), new Color(120, 214, 255), 0.38f) * 0.48f,
                    new Vector2(0.5f, 1.45f),
                    normal.ToRotation(),
                    0.035f,
                    Main.rand.NextFloat(0.08f, 0.12f),
                    13);
                GeneralParticleHandler.SpawnParticle(sideWave);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(texture.Width, texture.Height) * 0.5f;
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = Color.Lerp(new Color(255, 176, 70), new Color(92, 180, 255), 0.25f) * (completion * 0.42f);
                Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * (0.8f + completion * 0.18f), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.62f, Pitch = -0.1f }, Projectile.Center);
            SpawnTotalityFire();
            SpawnGrenadeImpactVisuals();
        }

        private void SpawnTotalityFire()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!ModContent.TryFind("CalamityMod/TotalityFire", out ModProjectile totalityFire))
                return;

            int damage = Math.Max(1, (int)(Projectile.damage * 0.32f));
            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2().RotatedByRandom(0.16f) * Main.rand.NextFloat(4.5f, 7.8f);
                int fireIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + velocity.SafeNormalize(Vector2.UnitX) * 8f,
                    velocity,
                    totalityFire.Type,
                    damage,
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);

                if (!Main.projectile.IndexInRange(fireIndex))
                    continue;

                Projectile fire = Main.projectile[fireIndex];
                fire.DamageType = DamageClass.Ranged;
                fire.usesLocalNPCImmunity = true;
                fire.localNPCHitCooldown = 15;
                fire.CritChance = Projectile.CritChance;
            }
        }

        private void SpawnGrenadeImpactVisuals()
        {
            Particle explosion = new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 146, 64) * 0.78f,
                Vector2.One,
                Main.rand.NextFloat(-0.18f, 0.18f),
                0f,
                0.22f,
                12,
                true);
            GeneralParticleHandler.SpawnParticle(explosion);

            Particle flame = new FlameExplosion(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 170, 76),
                Vector2.One,
                Main.rand.NextFloat(-0.24f, 0.24f),
                0.08f,
                0.58f,
                16,
                0.62f);
            GeneralParticleHandler.SpawnParticle(flame);

            Particle brightPulse = new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 198, 82),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.06f,
                0.72f,
                20);
            GeneralParticleHandler.SpawnParticle(brightPulse);

            Particle smokeRing = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 150, 72) * 0.72f,
                new Vector2(1.7f, 1.7f),
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.14f,
                1.35f,
                22);
            GeneralParticleHandler.SpawnParticle(smokeRing);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 15f);
                Particle trapSpark = new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    false,
                    Main.rand.Next(16, 30),
                    Main.rand.NextFloat(0.58f, 1.12f),
                    Main.rand.NextBool() ? new Color(255, 218, 86) : new Color(255, 96, 42));
                GeneralParticleHandler.SpawnParticle(trapSpark);
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 12f);
                Particle glowSpark = new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.014f, 0.026f),
                    Main.rand.NextBool() ? new Color(255, 164, 54) : new Color(255, 72, 38),
                    new Vector2(Main.rand.NextFloat(2.4f, 4.2f), Main.rand.NextFloat(0.65f, 1.05f)),
                    true,
                    false,
                    0.7f);
                GeneralParticleHandler.SpawnParticle(glowSpark);
            }

            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.Smoke : DustID.Torch,
                    Main.rand.NextVector2Circular(9f, 9f),
                    120,
                    Main.rand.NextBool() ? Color.Gray : Color.Orange,
                    Main.rand.NextFloat(0.9f, 1.65f));
                dust.noGravity = true;
            }
        }
    }

    internal sealed class MK14DragonBreathPellet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 23;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.018f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.54f, 0.13f, 0.035f));
            SpawnDragonBreathStream();
        }

        private void SpawnDragonBreathStream()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float fanHalfAngle = MathHelper.ToRadians(34f);

            for (int i = 0; i < 7; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float angle = fanHalfAngle * fanT;
                Vector2 velocity = forward.RotatedBy(angle) * Main.rand.NextFloat(5.5f, 12f) + right * fanT * Main.rand.NextFloat(0.8f, 2.4f);
                Particle flame = new CustomSpark(
                    Projectile.Center + right * fanT * Main.rand.NextFloat(2f, 9f),
                    velocity,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(9, 16),
                    Main.rand.NextFloat(0.14f, 0.24f),
                    Color.Lerp(new Color(255, 92, 44), new Color(255, 174, 70), Main.rand.NextFloat(0.2f, 0.75f)),
                    new Vector2(Main.rand.NextFloat(1.4f, 2.3f), Main.rand.NextFloat(0.75f, 1.15f)),
                    true,
                    false,
                    0f,
                    false,
                    false,
                    0.45f);
                GeneralParticleHandler.SpawnParticle(flame);
            }

            for (int i = 0; i < 2; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                Vector2 sparkVelocity = forward.RotatedBy(fanHalfAngle * fanT * 0.7f) * Main.rand.NextFloat(7f, 15f) + right * fanT * Main.rand.NextFloat(0.8f, 2.8f);
                Particle beamCore = new CustomSpark(
                    Projectile.Center + right * fanT * Main.rand.NextFloat(2f, 8f),
                    sparkVelocity,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.16f, 0.25f),
                    Main.rand.NextBool() ? new Color(255, 104, 56) : new Color(255, 154, 66),
                    new Vector2(Main.rand.NextFloat(1.1f, 1.7f), Main.rand.NextFloat(0.65f, 1.05f)),
                    true,
                    false,
                    0f,
                    false,
                    false,
                    0.35f);
                GeneralParticleHandler.SpawnParticle(beamCore);
            }

            if (Main.rand.NextBool(2))
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                SparkParticle spark = new(
                    Projectile.Center + right * fanT * Main.rand.NextFloat(4f, 12f),
                    forward.RotatedBy(fanHalfAngle * fanT) * Main.rand.NextFloat(6f, 13f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.42f, 0.75f),
                    Main.rand.NextBool() ? new Color(220, 76, 42) : new Color(255, 134, 54));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(3))
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                GlowSparkParticle glowSpark = new(
                    Projectile.Center + right * fanT * Main.rand.NextFloat(3f, 10f),
                    forward.RotatedBy(fanHalfAngle * fanT * 0.75f) * Main.rand.NextFloat(7f, 14f),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.012f, 0.02f),
                    Main.rand.NextBool() ? new Color(255, 96, 52) : new Color(255, 142, 66),
                    new Vector2(Main.rand.NextFloat(1.9f, 2.8f), Main.rand.NextFloat(0.62f, 0.95f)),
                    true,
                    false,
                    0.72f);
                GeneralParticleHandler.SpawnParticle(glowSpark);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
            target.GetGlobalNPC<MK14DragonBreathGlobalNPC>().ApplyMark(Projectile.owner, 300);

            Vector2 upwardForward = (-Vector2.UnitY).RotatedBy(MathHelper.ToRadians(5f));
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = upwardForward.RotatedByRandom(0.42f) * Main.rand.NextFloat(4f, 13f);
                Particle flame = new CustomSpark(
                    target.Center + Main.rand.NextVector2Circular(10f, 8f),
                    velocity,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.16f, 0.28f),
                    Main.rand.NextBool(3) ? new Color(120, 82, 70) : new Color(255, 104, 48),
                    new Vector2(Main.rand.NextFloat(1.2f, 2.1f), Main.rand.NextFloat(0.7f, 1.1f)),
                    true,
                    false,
                    0f,
                    false,
                    false,
                    0.4f);
                GeneralParticleHandler.SpawnParticle(flame);
            }

            for (int i = 0; i < 4; i++)
            {
                SparkParticle spark = new(
                    target.Center + Main.rand.NextVector2Circular(8f, 8f),
                    upwardForward.RotatedByRandom(0.32f) * Main.rand.NextFloat(6f, 15f),
                    true,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.52f, 0.85f),
                    Main.rand.NextBool() ? new Color(255, 82, 42) : new Color(255, 145, 64));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
