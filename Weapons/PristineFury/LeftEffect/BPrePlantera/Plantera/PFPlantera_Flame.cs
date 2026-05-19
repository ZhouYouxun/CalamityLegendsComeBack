using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPlantera_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 116;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;

            if (Timer == 1f)
                EmitOpeningBloom();

            Projectile.scale = 1.75f * Utils.GetLerpValue(5f, 32f, Timer, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Timer * 0.055f + Projectile.ai[1] * 0.07f) * 0.025f) * 0.988f;

            NPC target = FindBloomTarget(760f);
            if (target != null && Timer > 22f)
            {
                Vector2 desired = (target.Center - Projectile.Center + side * (float)Math.Sin(Timer * 0.1f) * 40f).SafeNormalize(forward) * 11.6f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.042f);
            }

            EmitWildfireBody();
            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Lime, Color.Turquoise, CalamityUtils.Convert01To010(Utils.GetLerpValue(26f, Lifetime, Timer, true))).ToVector3() * Projectile.scale * 0.34f);
        }

        private NPC FindBloomTarget(float range)
        {
            NPC best = null;
            float bestScore = range;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toTarget = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float score = Projectile.Distance(npc.Center) + Math.Abs(Vector2.Dot(forward.RotatedBy(MathHelper.PiOver2), toTarget)) * 90f + Vector2.Distance(Main.MouseWorld, npc.Center) * 0.11f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }

            return best;
        }

        private void EmitOpeningBloom()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Particle smoke = new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Projectile.velocity.RotatedByRandom(0.14f) * Main.rand.NextFloat(0.3f, 2.2f),
                    Color.Lime,
                    Color.Turquoise,
                    Main.rand.NextFloat(i == 0 ? 0.8f : 0.4f, i == 0 ? 1.9f : 1.1f),
                    180,
                    Main.rand.NextFloat(-3f, 3f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(5) ? 135 : 107);
                dust.noGravity = true;
                dust.velocity = Projectile.velocity.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.3f, 2.9f);
                dust.scale = Main.rand.NextFloat(1.3f, 2.1f);
            }
        }

        private void EmitWildfireBody()
        {
            if (Main.dedServ)
                return;

            float smokeRot = MathHelper.ToRadians(3f);
            float colorValue = CalamityUtils.Convert01To010(Utils.GetLerpValue(30f, Lifetime, Timer, true));
            Color smokeColor = Color.Lerp(Color.Lime, Color.Turquoise, colorValue);
            Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, smokeColor, 18, Projectile.scale * Main.rand.NextFloat(0.6f, 1.2f), 0.4f, smokeRot, true, required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Timer > 4f)
            {
                for (int i = 0; i < 2; i++)
                {
                    float dustArea = Main.rand.NextFloat(0.1f, 1.7f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f) + Projectile.velocity * Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextBool(5) ? 135 : 107);
                    dust.noGravity = true;
                    dust.velocity = new Vector2(6f, 6f).RotatedByRandom(100f) * dustArea;
                    dust.scale = (1.8f - dustArea) * 0.65f;
                }
            }

            if (Main.rand.NextBool(5))
            {
                Particle goldGlow = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.45f, Color.Gold, 9, Projectile.scale * Main.rand.NextFloat(0.4f, 0.7f), 0.2f, smokeRot, true, 0.005f);
                GeneralParticleHandler.SpawnParticle(goldGlow);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 52f * Projectile.scale * 0.5f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 360);

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
