using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFEvilT2_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 78;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;

            if (Timer < 6f)
                return;

            Projectile.scale = 1.55f * Utils.GetLerpValue(6f, 38f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Timer * 0.12f + Projectile.ai[1]) * 0.012f) * 0.993f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Color smokeColor = Color.Lerp(Color.BlueViolet, Color.Black, 0.72f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f));
            Lighting.AddLight(Projectile.Center, smokeColor.ToVector3() * Projectile.scale * 0.45f);

            if (Main.dedServ)
                return;

            float smokeRot = MathHelper.ToRadians(3f);
            Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.45f, smokeColor, 22, Projectile.scale * Main.rand.NextFloat(0.62f, 1.18f), 0.82f, smokeRot, required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(4))
            {
                Color inner = Color.Lerp(smokeColor, Color.MidnightBlue, 0.35f);
                Particle glow = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * 0.34f, inner, 14, Projectile.scale * Main.rand.NextFloat(0.36f, 0.68f), 0.8f, smokeRot, true, 0.005f);
                GeneralParticleHandler.SpawnParticle(glow);
            }

            if (Timer % 11f == 0f)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Particle spark = new SparkParticle(Projectile.Center - forward * 12f, -forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 4.6f), false, 16, Main.rand.NextFloat(0.55f, 1f), Color.Lerp(Color.BlueViolet, Color.HotPink, 0.35f));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 52f * Projectile.scale * 0.5f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<BrainRot>(), 720);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(6f, 18f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Color color = new Color(84, 22, 128, 0) * opacity;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.36f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.36f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
