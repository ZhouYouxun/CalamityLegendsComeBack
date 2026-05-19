using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDragon_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 fixedDirection;
        private int time;
        private bool postHit;
        private int hitBloomReduction;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 78;
            Projectile.extraUpdates = 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            fixedDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.LocalPlayer.active)
            {
                float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = System.Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, 2.6f * distanceFactor);
            }

            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.18f }, Projectile.Center);
        }

        public override void AI()
        {
            if (fixedDirection == Vector2.Zero)
                fixedDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            Lighting.AddLight(Projectile.Center, new Vector3(0.255f, 0.07f, 0.01f) * 3f);
            Projectile.velocity *= 1.006f;
            EmitYharonFan();
            time++;
        }

        private void EmitYharonFan()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = fixedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float fanHalfAngle = MathHelper.ToRadians(52f);
            float edgeCompression = 0.68f;

            for (int i = 0; i < 9; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float angle = fanHalfAngle * fanT * 0.72f;
                Vector2 sparkVelocity = forward.RotatedBy(angle) * Main.rand.NextFloat(14f, 28f) + right * fanT * Main.rand.NextFloat(1.8f, 5.4f);
                Vector2 sparkOffset = right * fanT * Main.rand.NextFloat(4f, 18f) + forward * Main.rand.NextFloat(4f, 14f);
                Particle beamCore = new CustomSpark(Projectile.Center + sparkOffset, sparkVelocity, "CalamityMod/Particles/SmallBloom", false, Main.rand.Next(12, 18), Main.rand.NextFloat(0.26f, 0.42f), Main.rand.NextBool(3) ? Color.OrangeRed : Color.Lerp(Color.Orange, Color.White, 0.45f), new Vector2(Main.rand.NextFloat(1.6f, 2.5f), Main.rand.NextFloat(1.1f, 1.7f)), true, false, glowOpacity: 0.5f);
                GeneralParticleHandler.SpawnParticle(beamCore);
            }

            if (Main.rand.NextBool())
            {
                for (int i = 0; i < 7; i++)
                {
                    float fanT = Main.rand.NextFloat(-1f, 1f);
                    float angle = fanHalfAngle * fanT * 0.94f;
                    Vector2 sparkPos = Projectile.Center + right * fanT * Main.rand.NextFloat(10f, 24f) + forward * Main.rand.NextFloat(6f, 18f);
                    Vector2 sparkVel = forward.RotatedBy(angle) * Main.rand.NextFloat(12f, 24f) + right * fanT * Main.rand.NextFloat(2f, 7f);
                    SparkParticle spark = new SparkParticle(sparkPos, sparkVel, false, Main.rand.Next(14, 24), Main.rand.NextFloat(0.8f, 1.35f), Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 3; i++)
                {
                    float fanT = Main.rand.NextFloat(-1f, 1f);
                    float angle = fanHalfAngle * fanT * 0.8f;
                    Vector2 glowOffset = right * fanT * Main.rand.NextFloat(6f, 20f) + forward * Main.rand.NextFloat(5f, 16f);
                    Vector2 glowVel = forward.RotatedBy(angle) * Main.rand.NextFloat(16f, 30f) + right * fanT * Main.rand.NextFloat(2.5f, 7.5f);
                    Particle glowSpark = new GlowSparkParticle(Projectile.Center + glowOffset, glowVel, false, Main.rand.Next(9, 14), Main.rand.NextFloat(0.018f, 0.032f), Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed, new Vector2(Main.rand.NextFloat(3.2f, 4.6f), Main.rand.NextFloat(1.0f, 1.35f)), true, false, 1.3f);
                    GeneralParticleHandler.SpawnParticle(glowSpark);
                }
            }

            if (time % 5 == 0)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                float curvedT = (float)System.Math.Sin(fanT * MathHelper.PiOver2) * edgeCompression;
                Vector2 smokeVel = forward.RotatedBy(fanHalfAngle * curvedT) * Main.rand.NextFloat(4f, 10f) + right * fanT * Main.rand.NextFloat(1f, 3f);
                Particle smoke = new SmallSmokeParticle(Projectile.Center + right * fanT * Main.rand.NextFloat(8f, 22f), smokeVel, Color.DimGray, Main.rand.NextBool() ? Color.SlateGray : Color.Black, Main.rand.NextFloat(0.45f, 1.1f), 100);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (time <= 1)
            {
                Player owner = Main.player[Projectile.owner];
                float collisionPoint = 0f;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, owner.Center, Projectile.width, ref collisionPoint);
            }

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width + time * 0.14f, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.8f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hitBloomReduction < 3 && !Main.dedServ)
            {
                Particle blast = new CustomSpark(Projectile.Center, Vector2.Zero, "CalamityMod/Particles/SmallBloom", false, 7, Main.rand.NextFloat(0.6f, 0.7f), Color.OrangeRed, Vector2.One, true, false);
                GeneralParticleHandler.SpawnParticle(blast);
                Particle blastRing = new CustomPulse(target.Center, Vector2.Zero, Color.Lerp(Color.Orange, Color.OrangeRed, Main.rand.NextFloat()) * 0.7f, "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(0.2f, 1.2f), 2.5f, 15, true);
                GeneralParticleHandler.SpawnParticle(blastRing);
                hitBloomReduction++;
            }

            if (!postHit)
            {
                EmitUpwardImpact(target.Center);
                postHit = true;
            }

            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.CursedInferno, 300);
            target.AddBuff(BuffID.Daybreak, 300);
            target.AddBuff(ModContent.BuffType<ElementalMix>(), 300);
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 1200);
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
        }

        private void EmitUpwardImpact(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Vector2 upwardForward = (-Vector2.UnitY).RotatedBy(MathHelper.ToRadians(5f));
            for (int i = 0; i < 18; i++)
            {
                Vector2 sparkVel = upwardForward.RotatedByRandom(0.3f) * Main.rand.NextFloat(10f, 26f);
                SparkParticle spark = new SparkParticle(center + Main.rand.NextVector2Circular(10f, 10f), sparkVel, true, Main.rand.Next(20, 34), Main.rand.NextFloat(0.85f, 1.45f), Main.rand.NextBool(4) ? Color.OrangeRed : Color.Orange);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 smokeVel = upwardForward.RotatedByRandom(0.42f) * Main.rand.NextFloat(3f, 12f);
                Particle smoke = new SmallSmokeParticle(center + Main.rand.NextVector2Circular(18f, 12f), smokeVel, Color.DimGray, Main.rand.NextBool() ? Color.SlateGray : Color.Black, Main.rand.NextFloat(0.65f, 1.45f), 100);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
