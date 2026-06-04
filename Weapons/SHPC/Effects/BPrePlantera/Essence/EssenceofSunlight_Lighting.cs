using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_Lighting : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 7;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            timer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            int targetIndex = (int)Projectile.ai[0];
            float side = Projectile.ai[1] == 0f ? 1f : System.Math.Sign(Projectile.ai[1]);
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 40f, 0.055f);

            if (Main.npc.IndexInRange(targetIndex))
            {
                NPC target = Main.npc[targetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                {
                    float returnPower = Utils.GetLerpValue(16f, 82f, timer, true);
                    Vector2 predictedCenter = target.Center + target.velocity * MathHelper.Lerp(0f, 3f, returnPower);
                    Vector2 toTarget = (predictedCenter - Projectile.Center).SafeNormalize(forward);
                    float fakeMissPower = 1f - returnPower;
                    Vector2 perpendicularBias = toTarget.RotatedBy(MathHelper.PiOver2 * side) * 0.12f * fakeMissPower;
                    Vector2 desired = (toTarget + perpendicularBias).SafeNormalize(toTarget);
                    float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(0.05f), MathHelper.ToRadians(0.62f), (float)System.Math.Pow(returnPower, 1.35f));
                    float easedDesiredRotation = forward.ToRotation().AngleTowards(desired.ToRotation(), maxTurn);
                    Vector2 easedDesired = easedDesiredRotation.ToRotationVector2();
                    float turnStrength = MathHelper.Lerp(0.015f, 0.095f, returnPower);

                    forward = Vector2.Lerp(forward, easedDesired, turnStrength).SafeNormalize(desired);
                    speed = MathHelper.Lerp(speed, 42f, returnPower * 0.025f);
                }
            }

            Projectile.velocity = forward * speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 100).ToVector3() * 0.45f);

            Vector2 futurePos = Projectile.Center + Projectile.velocity * 0.5f;
            int sparkCount = timer < 12 ? 2 : 3;
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 vel = forward.RotatedByRandom(MathHelper.ToRadians(timer < 12 ? 3f : 7f)) * Main.rand.NextFloat(3.6f, 7.2f);
                Particle spark = new GlowSparkParticle(
                    futurePos,
                    vel,
                    false,
                    8,
                    0.12f,
                    new Color(255, 230, 120),
                    new Vector2(1.45f, 0.22f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                Particle trail = new GlowSparkParticle(
                    Projectile.Center - forward * 10f,
                    -forward * Main.rand.NextFloat(1.2f, 3f),
                    false,
                    10,
                    0.1f,
                    new Color(255, 200, 80),
                    new Vector2(1.1f, 0.22f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(trail);
            }
        }

        //public override bool? CanDamage() => timer > 1;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = target.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i < 12; i++)
            {
                Particle core = new GlowSparkParticle(
                    pos,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    false,
                    6,
                    0.2f,
                    new Color(255, 240, 150),
                    new Vector2(0.95f, 0.32f),
                    true,
                    false,
                    1);
                GeneralParticleHandler.SpawnParticle(core);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = forward.RotatedByRandom(MathHelper.ToRadians(4.8f)) * Main.rand.NextFloat(4.8f, 9.6f);
                Particle jet = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    new Color(255, 220, 100),
                    new Vector2(1.55f, 0.26f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(jet);
            }

            SoundEngine.PlaySound(SoundID.Item94, pos);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
