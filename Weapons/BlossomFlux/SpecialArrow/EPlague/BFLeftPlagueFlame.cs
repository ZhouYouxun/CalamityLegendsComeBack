using CalamityMod.Particles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFLeftPlagueFlame : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 42;
            Projectile.MaxUpdates = 120;
            Projectile.alpha = 255;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Time++;

            Color plagueGreen = new(124, 238, 68);
            Color plagueYellow = new(218, 255, 116);
            Lighting.AddLight(Projectile.Center, plagueGreen.ToVector3() * 0.28f);

            if (Time > 4f && Main.rand.NextBool(38))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    -Projectile.velocity.RotatedByRandom(0.05f) * Main.rand.NextFloat(0.08f, 0.22f),
                    100,
                    Main.rand.NextBool(3) ? plagueYellow : plagueGreen,
                    Main.rand.NextFloat(0.45f, 0.95f));
                ember.noGravity = true;
            }

            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            float fade = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Color sparkColor = Color.Lerp(plagueGreen, plagueYellow, Main.rand.NextFloat(0.12f, 0.55f)) * fade;

            CustomSpark spark = new(
                Projectile.Center,
                Projectile.velocity * Main.rand.NextFloat(0.3f, 2.4f),
                "CalamityMod/Particles/SmallBloom",
                false,
                4,
                Main.rand.NextFloat(0.055f, 0.09f),
                sparkColor,
                new Vector2(1f, 1f + Time * 0.03f),
                true,
                false);
            GeneralParticleHandler.SpawnParticle(spark);

            if (Main.rand.NextBool(3))
            {
                MediumMistParticle smoke = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity.RotatedByRandom(0.15f) * Main.rand.NextFloat(0.2f, 0.65f),
                    new Color(114, 186, 52) * fade,
                    new Color(220, 255, 130) * fade,
                    Main.rand.NextFloat(0.65f, 1.15f),
                    34,
                    Main.rand.NextFloat(-0.04f, 0.04f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 240);
            target.AddBuff(BuffID.Venom, 180);
        }
    }
}
