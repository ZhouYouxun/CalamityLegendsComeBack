using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFEvilT2_Flame : ModProjectile, ILocalizedModType
    {
        private const int DecelerationFrames = 22;
        private const int Lifetime = 100;
        private ref float Timer => ref Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Timer++;
            float speed = Projectile.velocity.Length();
            if (Timer <= DecelerationFrames)
                speed = Math.Max(3.2f, speed * 0.925f);
            else
                speed = Math.Min(16.8f, speed * 1.055f + 0.04f);

            NPC target = Timer > 14f ? FindTarget(520f) : null;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (target != null)
                direction = Vector2.Lerp(direction, Projectile.SafeDirectionTo(target.Center), 0.035f).SafeNormalize(direction);

            Projectile.velocity = direction * speed;
            Projectile.rotation = direction.ToRotation();
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.7f);

            if (Main.dedServ || Timer % 2f != 0f)
                return;

            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                Projectile.Center - direction * 8f,
                -direction * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                Color.Lerp(theme, Color.DarkGoldenrod, 0.38f),
                18,
                Main.rand.NextFloat(0.42f, 0.72f),
                0.66f,
                Main.rand.NextFloat(-0.05f, 0.05f),
                glowing: true));

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - direction * 5f + Main.rand.NextVector2Circular(4f, 4f),
                    -direction * Main.rand.NextFloat(0.55f, 1.6f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.16f, 0.46f)),
                    true,
                    false,
                    true));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -direction * Main.rand.NextFloat(0.35f, 1.1f) + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    Main.rand.NextFloat(0.28f, 0.48f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.36f)),
                    Main.rand.Next(12, 20)));
            }
        }

        private NPC FindTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrainRot>(), 360);
            SpawnImpactSmoke();
        }

        public override void OnKill(int timeLeft) => SpawnImpactSmoke();

        private void SpawnImpactSmoke()
        {
            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
            for (int i = 0; i < 12; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    Main.rand.NextVector2Circular(3.8f, 3.8f),
                    Color.Lerp(theme, Color.DarkGoldenrod, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(20, 32),
                    Main.rand.NextFloat(0.65f, 1.2f),
                    0.72f,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    glowing: true));
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 5.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.34f, 0.72f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.42f)),
                    true,
                    false,
                    true));
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(2.2f, 2.2f),
                    Color.Lerp(theme, Color.Goldenrod, Main.rand.NextFloat(0.14f, 0.38f)),
                    Color.Black,
                    Main.rand.NextFloat(0.38f, 0.78f),
                    Main.rand.Next(22, 36),
                    Main.rand.NextFloat(-0.04f, 0.04f)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Color theme = (PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92)) with { A = 0 }) * Projectile.Opacity;
            Vector2 center = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(line, center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 13f, null, theme * 0.68f, Projectile.rotation + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.22f, 0.9f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.45f), Projectile.rotation, bloom.Size() * 0.5f, 0.17f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
