using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    /// <summary>
    /// 九头蛇：剧毒弹，命中时 8 射线星形爆炸，施加 Venom + GalvanicCorrosion。
    /// </summary>
    public class DERule_Hydra : DEBulletRule
    {
        private static readonly Color VenomGreen = new(60, 230, 50);
        private static readonly Color RainbowPurple = new(180, 50, 230);
        private static readonly Color VenomDark = new(20, 130, 10);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Hydra>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.45f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 毒液尾迹
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Venom,
                -forward * 0.5f + Main.rand.NextVector2Circular(0.4f, 0.4f), 100, VenomGreen, 0.95f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 0.9f, false, 6, 0.014f, VenomGreen, new Vector2(0.5f, 2.0f)));
            }

            Lighting.AddLight(projectile.Center, 0f, VenomGreen.G / 255f * 0.4f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 60);
            SpawnHydraStarburst(projectile.Center);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnHydraStarburst(projectile.Center);
            return true;
        }

        private static void SpawnHydraStarburst(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, VenomGreen, 1.0f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, VenomGreen * 0.75f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.08f, 20, true, 0.85f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, RainbowPurple * 0.45f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.1f, 24, true, 0.7f));

                // 8 射线
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    Vector2 dir = angle.ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(pos, Vector2.Zero,
                        VenomGreen * 0.7f, new Vector2(0.8f, 3.5f), angle, 0.12f, 0.028f, 16));
                    for (int j = 0; j < 5; j++)
                    {
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            pos + dir * (j * 12f), dir * 9f + Main.rand.NextVector2Circular(1.5f, 1.5f),
                            false, 10, 0.018f, j % 2 == 0 ? VenomGreen : RainbowPurple, new Vector2(0.6f, 0.4f)));
                    }
                }
            }
            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                Dust dust = Dust.NewDustPerfect(pos, DustID.Venom,
                    angle.ToRotationVector2() * 9f, 100, VenomGreen, 1.15f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Venom round; explodes in an 8-ray starburst on impact + Venom + Galvanic Corrosion";
        public override string TooltipEffectZH => "毒液弹，命中时8射线星爆，施加毒液+伽尔腐蚀";
    }
}
