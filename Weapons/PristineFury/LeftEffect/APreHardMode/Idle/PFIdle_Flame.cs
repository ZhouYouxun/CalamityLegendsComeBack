using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFIdle_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/MediumMist";

        private const int Lifetime = 22;
        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 beamPosition;
        private Vector2 beamPosition2;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            float intensity = Utils.GetLerpValue(0f, 9f, Timer, true);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.7f);

            if (Main.dedServ || Timer <= 2f)
                return;

            float sine = (float)Math.Sin(Timer * 0.65f / MathHelper.Pi);
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float spread = 30f * intensity * Utils.GetLerpValue(2f, 8f, Timer, true);
            beamPosition = Projectile.Center - normal * sine * spread;
            beamPosition2 = Projectile.Center + normal * sine * spread;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(beamPosition, Projectile.velocity * 0.1f, "CalamityMod/Particles/SmallBloom", false, 6, 0.065f * intensity + 0.01f, color, new Vector2(1f, 2.5f)));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(beamPosition2, Projectile.velocity * 0.1f, "CalamityMod/Particles/SmallBloom", false, 6, 0.065f * intensity + 0.01f, Color.Lerp(color, Color.White, 0.28f), new Vector2(1f, 2.5f)));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            for (int i = 0; i < 4; i++)
            {
                Vector2 linePosition = i < 2 ? beamPosition : beamPosition2;
                Vector2 lineVelocity = Utils.DirectionFrom(linePosition, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 40f)
                    .RotatedByRandom(0.16f) * Main.rand.NextFloat(1.2f, 7.5f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(linePosition, lineVelocity, "CalamityMod/Particles/SmallBloom", false, 11, 0.09f, Main.rand.NextBool() ? color : Color.Lerp(color, Color.White, 0.35f), new Vector2(2f, 1.5f), true, false, glowOpacity: 1.1f));
            }

            for (int i = 0; i < 7; i++)
            {
                float speedMultiplier = Main.rand.NextFloat(0.1f, 1.8f);
                Vector2 smokePosition = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
                Vector2 smokeVelocity = Vector2.UnitY * Main.rand.NextFloat(-12f, -8f) * speedMultiplier;
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(smokePosition, smokeVelocity, Main.rand.NextBool() ? color : Color.Lerp(color, Color.White, 0.28f), Color.Black, Main.rand.NextFloat(0.7f, 1.9f) - speedMultiplier, 225 - Main.rand.Next(60), 0.1f));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
