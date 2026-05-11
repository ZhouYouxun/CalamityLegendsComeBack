using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class PristineFire : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Particles/MediumMist";

        public float smokeOpa = 0.25f;
        public ref float time => ref Projectile.ai[2];
        public Vector2 beamPos;
        public Vector2 beamPos2;
        public bool hasIgnited = false;

        public int boomTime = PristineFury.boomTime;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            float mult = Utils.GetLerpValue(140, 75, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Red, Color.Goldenrod, mult).ToVector3() * 0.7f);

            if (Projectile.timeLeft < 116)
            {
                float sine = (float)Math.Sin(time * 0.65f / MathHelper.Pi);

                beamPos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * sine * (30f * mult * -1) * Utils.GetLerpValue(116, 108, Projectile.timeLeft, true);
                beamPos2 = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * sine * (30f * mult) * Utils.GetLerpValue(116, 108, Projectile.timeLeft, true);
                for (int i = 0; i < 2; i++)
                {
                    Particle beam = new CustomSpark(i == 0 ? beamPos : beamPos2, Projectile.velocity * 0.1f, "CalamityMod/Particles/SmallBloom", false, 6, (0.065f * mult) + 0.01f, Color.Lerp(Color.Red, Color.Goldenrod, mult), new Vector2(1f, 2.5f), true, false);
                    GeneralParticleHandler.SpawnParticle(beam);
                }
            }

            if (!hasIgnited)
            {
                for (int x = 0; x < Main.maxProjectiles; x++)
                {
                    Projectile projectile = Main.projectile[x];
                    if (Vector2.Distance(Projectile.Center, projectile.Center) <= 100 && projectile.active && projectile.type == ModContent.ProjectileType<PristineSecondary>() && projectile.Opacity > 0.7f)
                    {
                        if (projectile.ai[2] == 0)
                        {
                            projectile.ai[2] = boomTime;
                            hasIgnited = true;
                            SoundStyle ignite = new("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn");
                            SoundEngine.PlaySound(ignite with { Volume = 1f, Pitch = Main.rand.NextFloat(0.5f, 0.6f), SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);
                        }
                        else
                            hasIgnited = true;
                    }
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            time--;
        }

        
        //Doze - Flamethrowers in vanilla are long debuff infliction tools (20 seconds of their debuff).
        //I am applying this as the base for Cal flamethrowers, with shorter times being the exception instead of the rule
        //On Pristine Fury, the full 20 seconds is limited to the ignition of the secondary.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

        public override void OnKill(int timeLeft)
        {
            Projectile.ExpandHitboxBy(50);
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();

            for (int i = 0; i < 4; i++)
            {
                Vector2 linePos = i < 2 ? beamPos : beamPos2;
                Vector2 lineVel = (Utils.DirectionFrom(linePos, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 40) * 3).RotatedByRandom(0.16f) * Main.rand.NextFloat(0.4f, 2.5f);

                Particle beam = new CustomSpark(linePos, lineVel, "CalamityMod/Particles/SmallBloom", false, 11, 0.09f, Main.rand.NextBool() ? Color.Orange : Color.DarkOrange, new Vector2(2f, 1.5f), true, false, 0, false, false, 1.1f);
                GeneralParticleHandler.SpawnParticle(beam);
            }

            for (int i = 0; i <= 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 169 : 158, new Vector2(5, 5).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.5f), 0, default, Main.rand.NextFloat(1.6f, 2.2f));
                dust.noGravity = true;
                dust.fadeIn = 0.5f;
            }
            // Big poofy column
            for (int i = 0; i < 7; i++)
            {
                float velMulti = Main.rand.NextFloat(0.1f, 1.8f);
                Vector2 smokePos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
                Vector2 smokeVel = Vector2.UnitY * Main.rand.NextFloat(-12f, -8f) * velMulti;
                Particle smoke = new MediumMistParticle(smokePos, smokeVel, Main.rand.NextBool() ? Color.Orange : Color.DarkOrange, Color.Black, Main.rand.NextFloat(0.7f, 1.9f) - velMulti, 225 - Main.rand.Next(60), 0.1f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
