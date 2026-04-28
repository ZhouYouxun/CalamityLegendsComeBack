using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFLeftPlagueFlame : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color sparkColor;
        private int time;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 60;
            Projectile.timeLeft = 240;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float ownerDistance = Vector2.Distance(owner.Center, Projectile.Center);
            time++;

            Color plagueGreen = new(124, 238, 68);
            Color plagueYellow = new(218, 255, 116);
            Color deepGreen = new(36, 152, 48);
            float colorPulse = Main.GlobalTimeWrappedHourly * 8f;
            sparkColor = Color.Lerp(plagueGreen, plagueYellow, colorPulse % 2f > 1f ? 1f : colorPulse % 1f);

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.2f);
            Lighting.AddLight(Projectile.Center, plagueGreen.ToVector3() * 0.35f);

            if (Main.dedServ)
                return;

            if (ownerDistance < 1400f)
            {
                float sparkScale = (37f - time * 0.165f) * 0.005f;
                sparkScale = MathHelper.Clamp(sparkScale, 0.035f, 0.19f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * Main.rand.NextFloat(0.5f, 4f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    4,
                    sparkScale,
                    sparkColor,
                    new Vector2(1f, 1f + time * 0.1f)));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * Main.rand.NextFloat(0.5f, 4f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    4,
                    sparkScale * 0.62f,
                    Color.Lerp(deepGreen, sparkColor, 0.55f),
                    new Vector2(1f, 1f + time * 0.1f)));
            }

            if (Main.rand.NextBool(35) && ownerDistance < 1400f && time > 5)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    263,
                    new Vector2(0f, -5f).RotatedByRandom(0.05f) * Main.rand.NextFloat(0.3f, 1.6f));

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.3f, 1f);
                dust.color = sparkColor;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 1200);
        }
    }
}
