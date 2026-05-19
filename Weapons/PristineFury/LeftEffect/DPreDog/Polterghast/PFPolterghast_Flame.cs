using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPolterghast_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 20 * 15;
            Projectile.extraUpdates = 15;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Timer++;

            bool drawingUpdate = Projectile.numUpdates % 3 == 0;
            if (Timer > 6f && drawingUpdate)
            {
                float scaleBoost = MathHelper.Clamp(Timer * 0.005f, 0f, 2f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, Projectile.velocity, false, 7, 3.2f + scaleBoost, new Color(8, 35, 156)));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, Projectile.velocity, false, 7, 1.6f + scaleBoost, new Color(184, 215, 245)));
            }
            else if (Timer == 5f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Projectile.velocity * 0.75f, Color.Aqua, new Vector2(1f, 2.5f), Projectile.rotation, 0.2f, 0.03f, 20));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Projectile.velocity * 0.4f, Color.DodgerBlue, new Vector2(1f, 2.5f), Projectile.rotation, 0.1f, 0.025f, 35));
                ReleaseCometDust(24, 0.4f);
            }

            if (Projectile.numUpdates == 0)
                Lighting.AddLight(Projectile.Center, Color.MediumBlue.ToVector3() * 0.4f);
        }

        private void ReleaseCometDust(int count, float spread)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 172 : 206, Projectile.velocity);
                dust.scale = Main.rand.NextFloat(1.2f, 2.4f);
                dust.velocity = Projectile.velocity.RotatedByRandom(spread) * Main.rand.NextFloat(0.3f, 2.4f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 300);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.72f, Pitch = 0.35f }, Projectile.Center);
            ReleaseCometDust(14, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, new Color(55, 140, 255, 0) * 0.45f, Projectile.rotation, bloom.Size() * 0.5f, 0.16f + Timer * 0.0008f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
