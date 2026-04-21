using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Melee
{
    public class CrescentMoonProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.extraUpdates = 2;
            Projectile.aiStyle = -1;
            AIType = -1;
            Projectile.timeLeft = 240 * Projectile.MaxUpdates;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0f, 0f, 0.6f);
            if (Projectile.soundDelay == 0 && Projectile.velocity.Length() > 0.1f)
            {
                Projectile.soundDelay = 60;
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f }, Projectile.position);
            }

            Projectile.rotation += Projectile.direction * 0.15f;

            if (Projectile.FinalExtraUpdate())
            {
                GeneralParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, Vector2.Zero, Color.SkyBlue, 0.65f, 0.65f, 2, false),true,Enums.GeneralDrawLayer.AfterProjectiles);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Vector2.UnitX.RotatedBy(Projectile.rotation) * 0.1f, "CalamityMod/Projectiles/Melee/CrescentMoonProj", false, 2, 1f, Color.White, Vector2.One, false), false, Enums.GeneralDrawLayer.AfterProjectiles);
            }
                
            Projectile.velocity *= 0.965f;
            if (Projectile.timeLeft < 225 * Projectile.MaxUpdates)
                CalamityUtils.HomeInOnNPC(Projectile, true, 600f, 12f, 20f, true);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.CanBeChasedBy(Projectile, false) && target.Calamity().IsArmored())
                return false;
            return base.CanHitNPC(target);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                    109,
                    111,
                    132
                });

                int dust = Dust.NewDust(Projectile.Center, 0, 0, dustType);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
