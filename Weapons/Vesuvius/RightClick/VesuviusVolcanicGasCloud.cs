using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick
{
    public class VesuviusVolcanicGasCloud : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0] <= 0f ? 132f : Projectile.ai[0];
        private float Wind => Projectile.ai[1] == 0f ? 1f : Math.Sign(Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 116;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 156;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 26;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] == 1f)
            {
                Projectile.Resize((int)Radius, (int)(Radius * 0.72f));
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.netUpdate = true;
            }

            float lifeRatio = Utils.GetLerpValue(0f, 156f, Projectile.timeLeft, true);
            float fadeIn = Utils.GetLerpValue(0f, 24f, Projectile.localAI[0], true);
            Projectile.Opacity = fadeIn * Utils.GetLerpValue(0f, 34f, Projectile.timeLeft, true);
            Projectile.velocity.X += Wind * 0.014f;
            Projectile.velocity.Y -= 0.006f + 0.002f * lifeRatio;
            Projectile.velocity *= 0.986f;
            Projectile.rotation += Wind * 0.004f;

            Lighting.AddLight(Projectile.Center, 0.22f * Projectile.Opacity, 0.2f * Projectile.Opacity, 0.05f * Projectile.Opacity);

            if (!Main.dedServ)
                SpawnGasVisuals();
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= 14f && Projectile.timeLeft > 18;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
            target.AddBuff(BuffID.Poisoned, 240);
            target.AddBuff(BuffID.Slow, 90);
        }

        private void SpawnGasVisuals()
        {
            float fade = Projectile.Opacity;
            Vector2 extent = new(Projectile.width * 0.46f, Projectile.height * 0.42f);

            if (Main.rand.NextBool(2))
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(extent.X, extent.Y);
                Dust sulfur = Dust.NewDustPerfect(
                    position,
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.YellowTorch,
                    Projectile.velocity * Main.rand.NextFloat(-0.08f, 0.16f) + Main.rand.NextVector2Circular(0.55f, 0.55f) - Vector2.UnitY * Main.rand.NextFloat(0.05f, 0.28f),
                    150,
                    Color.Lerp(new Color(132, 126, 72), VesuviusProjectileVisuals.LavaGold, Main.rand.NextFloat(0.08f, 0.22f)),
                    Main.rand.NextFloat(0.72f, 1.28f) * fade);
                sulfur.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Particle ash = new SmallSmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(extent.X, extent.Y),
                    Projectile.velocity * Main.rand.NextFloat(0.02f, 0.18f) + Main.rand.NextVector2Circular(0.28f, 0.28f),
                    Color.Lerp(VesuviusProjectileVisuals.AshGray, new Color(156, 150, 84), 0.36f),
                    Color.Lerp(Color.Black, VesuviusProjectileVisuals.AshGray, 0.45f),
                    Main.rand.NextFloat(0.55f, 1.15f) * fade,
                    0.48f,
                    Main.rand.NextFloat(-0.04f, 0.04f));
                GeneralParticleHandler.SpawnParticle(ash);
            }

            if (Main.rand.NextBool(18))
            {
                Particle spark = new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(extent.X * 0.7f, extent.Y * 0.7f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) - Vector2.UnitY * Main.rand.NextFloat(0.15f, 0.55f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.18f, 0.38f) * fade,
                    Main.rand.NextBool(3) ? VesuviusProjectileVisuals.HotWhite : new Color(210, 190, 96));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Projectile.Opacity;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.8f + Projectile.identity);
            Vector2 baseScale = new(Projectile.width / (float)smoke.Width, Projectile.height / (float)smoke.Height);
            Color smokeColor = Color.Lerp(VesuviusProjectileVisuals.RavagerSmoke, new Color(128, 118, 74), 0.4f);
            Color sulfurColor = new Color(190, 170, 82, 0);

            for (int i = 0; i < 3; i++)
            {
                float localPulse = pulse + i * 0.18f;
                Vector2 offset = new Vector2((i - 1) * Projectile.width * 0.13f, (i == 1 ? -1f : 1f) * Projectile.height * 0.08f).RotatedBy(Projectile.rotation * 0.4f);
                Main.EntitySpriteDraw(
                    smoke,
                    Projectile.Center + offset - Main.screenPosition,
                    null,
                    smokeColor * (0.22f + localPulse * 0.05f) * fade * VesuviusProjectileVisuals.VisualIntensity,
                    Projectile.rotation + i * 1.7f,
                    smoke.Size() * 0.5f,
                    baseScale * new Vector2(1.35f - i * 0.1f, 0.88f + i * 0.06f) * VesuviusProjectileVisuals.VisualScale,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                sulfurColor * (0.16f + pulse * 0.05f) * fade * VesuviusProjectileVisuals.VisualIntensity,
                -Projectile.rotation * 0.55f,
                bloom.Size() * 0.5f,
                new Vector2(Projectile.width / (float)bloom.Width, Projectile.height / (float)bloom.Height) * 1.12f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            return false;
        }
    }
}
