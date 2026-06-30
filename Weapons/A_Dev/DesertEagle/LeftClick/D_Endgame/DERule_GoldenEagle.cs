using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    /// <summary>
    /// 黄金之鹰：高速金色子弹，命中时在命中点放射 4 道金色能量光束（90° 间隔），金色光晕爆发。
    /// </summary>
    public class DERule_GoldenEagle : DEBulletRule
    {
        private static readonly Color GoldPure = new(255, 215, 0);
        private static readonly Color GoldWhite = new(255, 250, 180);
        private static readonly Color GoldDeep = new(200, 150, 0);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.GoldenEagle>();

        public override float SpeedMultiplier => 1.3f;  // 更快

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.6f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 金色尾迹
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center - forward * (i * 3f),
                    DustID.GoldCoin, -forward * 0.4f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    110, GoldPure, Main.rand.NextFloat(0.85f, 1.1f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 1.5f, false, 5, 0.016f, GoldWhite, new Vector2(0.5f, 2.5f)));
            }

            Lighting.AddLight(projectile.Center, GoldPure.R / 255f * 0.65f, GoldPure.G / 255f * 0.55f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnGoldenRadiance(projectile.Center, projectile.velocity.ToRotation());
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SpawnGoldenRadiance(projectile.Center, projectile.velocity.ToRotation());
        }

        private static void SpawnGoldenRadiance(Vector2 pos, float baseAngle)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, GoldWhite, 1.2f, 26));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, GoldPure,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.09f, 22, true, 0.88f));

                // 4 道放射光束（90° 间隔）
                for (int i = 0; i < 4; i++)
                {
                    float angle = baseAngle + MathHelper.PiOver2 * i;
                    Vector2 dir = angle.ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(pos, Vector2.Zero,
                        GoldPure * 0.85f, new Vector2(0.7f, 4.5f), angle, 0.18f, 0.04f, 20));
                    for (int j = 0; j < 8; j++)
                    {
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            pos + dir * (j * 14f), dir * 12f, false, 14,
                            0.032f * (1f - j * 0.08f),
                            j % 2 == 0 ? GoldPure : GoldWhite, new Vector2(0.6f, 0.35f)));
                    }
                }

                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi * i / 16f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 8f, angle.ToRotationVector2() * 9f,
                        false, 10, 0.022f, GoldDeep, new Vector2(1.0f, 0.5f)));
                }
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                Dust dust = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                    angle.ToRotationVector2() * 10f, 100, GoldPure, 1.2f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Fast golden bullet; radiates 4 energy beams in a cross pattern on impact";
        public override string TooltipEffectZH => "高速金色弹，命中时放射4道十字形能量光束";
    }
}
