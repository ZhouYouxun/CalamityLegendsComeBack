using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFMoonlord_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BounceHits => ref Projectile.localAI[1];
        private readonly Color innerColor = Color.LightGreen;

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 45 * 13;
            Projectile.extraUpdates = 13;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Timer++;
            Lighting.AddLight(Projectile.Center, innerColor.ToVector3() * 0.2f);
            Player owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);

            if (!Main.dedServ && Main.rand.NextBool(5) && Timer > 12f && targetDist < 1400f)
            {
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center + Main.rand.NextVector2CircularEdge(5f, 5f), Projectile.velocity * Main.rand.NextFloat(0.05f, 0.5f), Color.Black, Main.rand.NextFloat(0.2f, 0.4f), Main.rand.Next(9, 12), true, false));

                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), ModContent.DustType<VoidDustInverted>());
                dust.scale = Main.rand.NextFloat(0.6f, 1.2f);
                dust.velocity = new Vector2(0f, Main.rand.NextFloat(0.1f, 5f));
                dust.noGravity = false;
                dust.color = innerColor;
            }

            if (!Main.dedServ && Projectile.timeLeft % 2 == 0 && Timer > 12f && targetDist < 1400f)
            {
                Particle black = new CustomSpark(Projectile.Center, -Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark2", false, 17, 0.052f, Color.Black, new Vector2(0.6f, 1.3f), false);
                GeneralParticleHandler.SpawnParticle(black);
                Particle green = new CustomSpark(Projectile.Center, -Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark", false, 17, 0.027f, innerColor, new Vector2(0.6f, 1.3f), true, false);
                GeneralParticleHandler.SpawnParticle(green);
                green.DrawLayer = CalamityMod.Enums.GeneralDrawLayer.AfterEverything;
            }

            if (Timer == 9f)
                ReleaseVoidDust(10, Projectile.velocity * 2f, 0.7f);

            if (BounceHits > 0f)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(6) ? 278 : 263, -Projectile.velocity);
                dust.scale = dust.type == 278 ? Main.rand.NextFloat(0.3f, 0.6f) : Main.rand.NextFloat(0.6f, 1.4f);
                dust.velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f, 1.7f);
                dust.noGravity = true;
                dust.color = innerColor;
                HomeAfterBounce();
            }
        }

        private void HomeAfterBounce()
        {
            NPC best = null;
            float bestDistance = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }

            if (best != null)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (best.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 7f, 0.08f);
        }

        private void ReleaseVoidDust(int count, Vector2 baseVelocity, float spread)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f);
                dust.velocity = baseVelocity.RotatedByRandom(spread) * Main.rand.NextFloat(0.35f, 1f);
                dust.noGravity = true;
                dust.color = innerColor;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black, "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.35f, 0.4f, 38, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, innerColor, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.48f, 0.52f, 38));
            ReleaseVoidDust(12, Main.rand.NextVector2CircularEdge(5f, 5f), 100f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 45 * 13;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black, "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.35f, 0.4f, 38, false));
                for (int i = 0; i < 3; i++)
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, innerColor, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.48f, 0.52f, 38));
                ReleaseVoidDust(10, Main.rand.NextVector2CircularEdge(5f, 5f), 100f);
            }

            BounceHits++;
            if (BounceHits >= 4f)
                Projectile.Kill();

            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;

            return false;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
