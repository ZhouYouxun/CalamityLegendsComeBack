using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // 侦查链补爆用的小型爆炸，名字保留项目内指定称呼。
    internal class FuckYou : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Vector2 center = Projectile.Center;
                int blastSize = Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 88;
                Projectile.width = blastSize;
                Projectile.height = blastSize;
                Projectile.Center = center;

                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);

                for (int i = 0; i < 18; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(2.2f, 5.6f);
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.Torch,
                        velocity,
                        100,
                        new Color(255, 145, 92),
                        Main.rand.NextFloat(1f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }
    }
}
