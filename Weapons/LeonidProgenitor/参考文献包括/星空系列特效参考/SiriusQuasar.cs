using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Summon
{
    public class SiriusQuasar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Summon";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        bool hasExploded = false;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4;

            Projectile.penetrate = -1;
            Projectile.extraUpdates = 30;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 1000;

            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
        }

        public ref float Time => ref Projectile.ai[1];

        public override void AI()
        {
            //Sirius Quasar AI

            Projectile.rotation = Projectile.velocity.ToRotation();
            Time++;
            bool isDrawingUpdate = Projectile.numUpdates % 6 == 0;
            if (Time > 6f && isDrawingUpdate)
            {
                Color outerSparkColor = new Color(8, 35, 156);
                float scaleBoost = MathHelper.Clamp(Time * 0.005f, 0f, 2f);
                float outerSparkScale = 3.2f + scaleBoost;
                SparkParticle spark = new SparkParticle(Projectile.Center, Projectile.velocity, false, 7, outerSparkScale, outerSparkColor);
                GeneralParticleHandler.SpawnParticle(spark);

                Color innerSparkColor = new Color(184, 215, 245);
                float innerSparkScale = 1.6f + scaleBoost;
                SparkParticle spark2 = new SparkParticle(Projectile.Center, Projectile.velocity, false, 7, innerSparkScale, innerSparkColor);
                GeneralParticleHandler.SpawnParticle(spark2);
            }
            for (int d = 0; d < 1; d++)
            {
                Vector2 projPos = Projectile.position;
                projPos -= Projectile.velocity * (d * 0.25f);
                Projectile.alpha = 255;
                int trailDust = Dust.NewDust(projPos, 1, 1, DustID.PurificationPowder, 0f, 0f, 0, default, 1f);
                Main.dust[trailDust].position = projPos;
                Main.dust[trailDust].scale = Main.rand.Next(70, 110) * 0.013f;
                Main.dust[trailDust].velocity *= 0.2f;
                Main.dust[trailDust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Due to Sirius's high Starburst consumption, it inflicts long amounts of Voidfrost on hit
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 600);
            float x4 = Main.rgbToHsl(new Color(103, 203, Main.DiscoB)).X;
            if (!hasExploded && !target.Calamity().IsArmored())
            {
                hasExploded = true;
                SoundStyle fire = new("CalamityMod/Sounds/Item/ScorpioNukeHit");
                SoundEngine.PlaySound(fire with { Volume = 0.75f, Pitch = 0.6f, PitchVariance = 0.2f }, Projectile.Center);
                float numberOfDusts = 156f;
                float rotFactor = 360f / numberOfDusts;
                for (int i = 0; i < numberOfDusts; i++)
                {
                    float rot = MathHelper.ToRadians(i * rotFactor);
                    float intensity = Main.rand.NextFloat(0.2f, 0.5f);
                    Vector2 offset = new Vector2(30f, 5.8f).RotatedBy(rot);
                    Vector2 velOffset = new Vector2(40.8f, 10.5f).RotatedBy(rot);
                    if (i % 2 == 0)
                    {
                        Particle orb = new CustomSpark(Projectile.Center + offset, velOffset * intensity * 0.7f, "CalamityMod/Particles/Sparkle", false, (int)(40 * intensity), intensity, Main.rand.NextBool(3) ? Color.DarkSlateBlue : Color.SlateBlue, new Vector2(1f, 2f), true, true);
                        GeneralParticleHandler.SpawnParticle(orb);
                    }
                    else
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, ModContent.DustType<LightDust>(), velOffset);
                        dust.noGravity = true;
                        dust.velocity = velOffset * intensity;
                        dust.scale = Main.rand.NextFloat(2.5f, 2.8f) * intensity;
                        dust.color = Main.rand.NextBool(3) ? Color.DarkSlateBlue : Color.SlateBlue;
                    }
                }

                Particle bolt2 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.DarkSlateBlue, "CalamityMod/Particles/BloomRing", new Vector2(0.6f, 0.8f), Projectile.velocity.ToRotation(), 0f, 3f, 25);
                GeneralParticleHandler.SpawnParticle(bolt2);
                Particle bolt3 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.SlateBlue, "CalamityMod/Particles/BloomRing", new Vector2(0.3f, 0.7f), Projectile.velocity.ToRotation(), 0f, 4f, 25);
                GeneralParticleHandler.SpawnParticle(bolt3);
            }
        }
    }
}
