using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    /// <summary>
    /// 凤凰爆破枪（原版）：橙红色燃烧弹，命中施加地狱烈焰，消亡时火焰爆炸。
    /// </summary>
    public class DERule_PhoenixBlaster : DEBulletRule
    {
        private static readonly Color FireOrange = new(255, 130, 20);
        private static readonly Color FireRed = new(255, 50, 10);
        private static readonly Color FireYellow = new(255, 230, 50);

        public override int GunItemType => ItemID.PhoenixBlaster;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.55f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 双色火焰尾迹
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center - forward * 4f,
                    i == 0 ? DustID.Torch : DustID.OrangeTorch,
                    -forward * 0.6f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120, i == 0 ? FireOrange : FireRed, Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    projectile.Center, -forward * 0.08f,
                    Color.Lerp(FireOrange, FireRed, 0.5f) * 0.4f, 14,
                    Main.rand.NextFloat(0.35f, 0.5f), 0.55f, Main.rand.NextFloat(-0.03f, 0.03f)));
            }

            Lighting.AddLight(projectile.Center, FireOrange.R / 255f * 0.65f, FireOrange.G / 255f * 0.4f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
            SpawnFireImpact(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitX));
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SpawnFireImpact(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitX));
        }

        private static void SpawnFireImpact(Vector2 pos, Vector2 dir)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, FireYellow, 1.0f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, FireOrange * 0.8f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.08f, 20, true, 0.75f));
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(2.5f, 2.5f),
                        Color.Lerp(FireOrange, FireRed, Main.rand.NextFloat()) * 0.55f, 18,
                        Main.rand.NextFloat(0.45f, 0.7f), 0.5f, Main.rand.NextFloat(-0.025f, 0.025f)));
                }
                for (int i = 0; i < 12; i++)
                {
                    float angle = MathHelper.TwoPi * i / 12f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 6f, angle.ToRotationVector2() * 8f,
                        false, 10, 0.025f, i % 2 == 0 ? FireOrange : FireRed, new Vector2(0.9f, 0.5f)));
                }
            }
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Torch,
                    Main.rand.NextVector2Circular(9f, 9f), 110, FireOrange, 1.2f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Incendiary round; applies Hellfire and erupts in a fireball on impact";
        public override string TooltipEffectZH => "燃烧弹，命中施加地狱烈焰，消亡时产生火球爆炸";
    }
}
