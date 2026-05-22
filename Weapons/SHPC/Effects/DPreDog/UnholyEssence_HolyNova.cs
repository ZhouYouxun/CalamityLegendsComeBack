using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class UnholyEssence_HolyNova : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;

            NPC target = FindTarget(1200f);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (target is not null)
            {
                Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(direction);
                float trackingPower = Utils.GetLerpValue(0f, 50f, Timer, true);
                float speed = MathHelper.Lerp(10f, 18f, trackingPower);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * speed, MathHelper.Lerp(0.08f, 0.36f, trackingPower));
            }
            else
                Projectile.velocity *= 0.992f;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(255, 232, 120).ToVector3() * 0.52f);

            if (Projectile.numUpdates == 0)
                SpawnNovaTrail(direction);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void SpawnNovaTrail(Vector2 direction)
        {
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Particle pulse = new CustomPulse(
                Projectile.Center,
                -direction * 0.8f,
                Main.rand.NextBool() ? new Color(255, 238, 150) : new Color(255, 172, 70),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Projectile.rotation,
                0.08f,
                0.16f,
                10);
            GeneralParticleHandler.SpawnParticle(pulse);

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(3f, 12f) + side * Main.rand.NextFloat(-6f, 6f),
                DustID.GoldFlame,
                -direction * Main.rand.NextFloat(0.6f, 2.0f) + side * Main.rand.NextFloat(-0.3f, 0.3f),
                0,
                new Color(255, 226, 120),
                Main.rand.NextFloat(0.75f, 1.2f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + (float)System.Math.Sin(Timer * 0.24f) * 0.1f;
            Color gold = new Color(255, 214, 88) with { A = 0 };
            Color white = Color.White with { A = 0 };

            Main.EntitySpriteDraw(bloom, drawPosition, null, gold * 0.42f, Projectile.rotation, bloom.Size() * 0.5f, 0.44f * pulse, SpriteEffects.None);
            //Main.EntitySpriteDraw(star, drawPosition, null, white * 0.58f, Projectile.rotation, star.Size() * 0.5f, new Vector2(0.18f, 0.46f) * pulse, SpriteEffects.None);
            //Main.EntitySpriteDraw(star, drawPosition, null, gold * 0.38f, Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.16f, 0.38f) * pulse, SpriteEffects.None);
            return false;
        }
    }
}
