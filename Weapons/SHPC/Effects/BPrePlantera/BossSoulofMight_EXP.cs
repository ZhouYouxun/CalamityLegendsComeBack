using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class BossSoulofMight_EXP : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 300;
            Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            float light = Main.rand.Next(90, 111) * 0.01f * Main.essScale;
            Lighting.AddLight(Projectile.Center, 5f * light, light, 4f * light);

            float particleCount = 25f;
            if (Projectile.ai[0] > 180f)
                particleCount -= (Projectile.ai[0] - 180f) / 2f;
            if (particleCount <= 0f)
            {
                Projectile.Kill();
                return;
            }

            particleCount *= 0.7f;
            Projectile.ai[0] += 4f;

            for (int i = 0; i < particleCount; i++)
            {
                float x = Main.rand.Next(-40, 41);
                float y = Main.rand.Next(-40, 41);
                float length = (float)Math.Sqrt(x * x + y * y);
                if (length == 0f)
                    continue;

                float speed = Main.rand.Next(12, 36) / length;
                int dustType = Main.rand.Next(3) switch
                {
                    0 => 246,
                    1 => 73,
                    _ => 187
                };

                int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    dustType, 0f, 0f, 100, default, 2f);
                Dust dust = Main.dust[index];
                dust.noGravity = true;
                dust.position = Projectile.Center + new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                dust.velocity = new Vector2(x * speed, y * speed);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Electrified, 300);
    }
}
