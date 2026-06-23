using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    /// <summary>
    /// 小型冰碎片弹幕，由 IceSpikeMinion 的 CryoShardVolley 攻击发射。
    /// 直线飞行，自带冰霜粒子尾迹，命中施加冰霜灼烧。
    /// </summary>
    public class IceShardProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 轻微减速
            Projectile.velocity *= 0.99f;

            // 旋转跟随速度方向
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 每帧 1-2 颗冰霜粒子尾迹
            int dustCount = Main.rand.Next(1, 3);
            for (int i = 0; i < dustCount; i++)
            {
                int dType = Main.rand.NextBool() ? DustID.Ice : DustID.Frost;
                Dust d = Dust.NewDustDirect(
                    Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                d.scale = Main.rand.NextFloat(0.6f, 0.9f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 施加冰霜灼烧 II — 3 秒
            target.AddBuff(BuffID.Frostburn2, 180);

            // 命中时产生 4-6 颗冰霜粒子
            int hitDust = Main.rand.Next(4, 7);
            for (int i = 0; i < hitDust; i++)
            {
                Dust d = Dust.NewDustDirect(
                    target.position, target.width, target.height, DustID.Frost);
                d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 死亡时爆散 5-8 颗冰粒子
            int burstCount = Main.rand.Next(5, 9);
            for (int i = 0; i < burstCount; i++)
            {
                int dType = Main.rand.NextBool() ? DustID.Ice : DustID.Frost;
                Dust d = Dust.NewDustDirect(
                    Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                d.scale = Main.rand.NextFloat(0.7f, 1.1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 弹幕本体不可见，完全依靠冰霜粒子尾迹作为视觉表现
            return false;
        }
    }
}
