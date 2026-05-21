using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFEvilT2_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 110;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Timer++;

            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 9f, 16.5f);
            Projectile.scale = Utils.GetLerpValue(0f, 18f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin(Timer * 0.22f + Projectile.ai[1]) * 0.018f) * 0.996f;

            if (Timer > 10f)
            {
                NPC target = FindTarget(520f);
                if (target != null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.055f);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Color purple = PFLeftEffectRules.GetThemeColor(Projectile, new Color(210, 58, 235));
            Color blood = new(255, 44, 82);
            Color smokeColor = Color.Lerp(purple, Color.Black, 0.52f + 0.16f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f));
            Lighting.AddLight(Projectile.Center, Color.Lerp(purple, blood, 0.35f).ToVector3() * Projectile.scale * 0.58f);

            if (Main.dedServ)
                return;

            float smokeRot = MathHelper.ToRadians(3f);
            Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.36f, smokeColor, 20, Projectile.scale * Main.rand.NextFloat(0.55f, 1.05f), 0.82f, smokeRot, required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(4))
            {
                Color inner = Color.Lerp(smokeColor, blood, 0.3f);
                Particle glow = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * 0.34f, inner, 14, Projectile.scale * Main.rand.NextFloat(0.36f, 0.68f), 0.8f, smokeRot, true, 0.005f);
                GeneralParticleHandler.SpawnParticle(glow);
            }

            if (Timer % 9f == 0f)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Particle spark = new SparkParticle(Projectile.Center - forward * 12f, -forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 4.6f), false, 16, Main.rand.NextFloat(0.55f, 1f), Color.Lerp(purple, blood, Main.rand.NextFloat(0.2f, 0.6f)));
                GeneralParticleHandler.SpawnParticle(spark);
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

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 22f * Projectile.scale, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<BrainRot>(), 720);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color purple = PFLeftEffectRules.GetThemeColor(Projectile, new Color(210, 58, 235));
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.PurpleTorch : DustID.CrimsonTorch,
                    Main.rand.NextVector2Circular(5f, 5f),
                    100,
                    Main.rand.NextBool() ? purple : new Color(255, 44, 82),
                    Main.rand.NextFloat(0.7f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D eater = TextureAssets.Projectile[ProjectileID.TinyEater].Value;
            Texture2D knife = TextureAssets.Projectile[ProjectileID.VampireKnife].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(6f, 18f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Color purple = PFLeftEffectRules.GetThemeColor(Projectile, new Color(210, 58, 235)) * opacity;
            Color blood = new Color(255, 44, 82) * opacity;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(purple, blood, completion) * (1f - completion) * 0.42f;
                Main.EntitySpriteDraw(eater, oldDrawPosition, null, trailColor, Projectile.rotation, eater.Size() * 0.5f, Projectile.scale * (0.72f - completion * 0.18f), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(eater, drawPosition, null, Color.Lerp(purple, Color.White, 0.18f) * 0.86f, Projectile.rotation, eater.Size() * 0.5f, Projectile.scale * 0.86f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(knife, drawPosition - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 4f, null, Color.Lerp(blood, Color.White with { A = 0 }, 0.25f) * 0.75f, Projectile.rotation + MathHelper.PiOver4, knife.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
