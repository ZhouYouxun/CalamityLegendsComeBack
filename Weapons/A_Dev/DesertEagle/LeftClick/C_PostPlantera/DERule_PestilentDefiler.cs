using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    /// <summary>
    /// 疫病污染者：瘟疫绿弹，命中时分裂 3 颗子弹 + 1 团瘟疫毒雾，施加 Plague。
    /// </summary>
    public class DERule_PestilentDefiler : DEBulletRule
    {
        private static readonly Color PestGreen = new(70, 220, 40);
        private static readonly Color PestDark = new(20, 130, 10);
        private static readonly Color PestYellow = new(180, 240, 60);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PestilentDefiler>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.45f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // TerraBlade 绿色尾迹
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.TerraBlade,
                -forward * 0.5f + Main.rand.NextVector2Circular(0.35f, 0.35f), 100, PestGreen, 1.0f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 0.9f, false, 6, 0.013f, PestGreen, new Vector2(0.5f, 1.8f)));
            }

            Lighting.AddLight(projectile.Center, PestGreen.R / 255f * 0.3f, PestGreen.G / 255f * 0.4f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 300);
            SpawnSplit(projectile, target.Center);
            SpawnPestImpact(projectile.Center);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnPestImpact(projectile.Center);
            return true;
        }

        private static void SpawnSplit(Projectile source, Vector2 hitPos)
        {
            int splitDmg = (int)(source.damage * 0.66f);
            float baseAngle = source.velocity.ToRotation();
            float[] offsets = { -MathHelper.ToRadians(40f), 0f, MathHelper.ToRadians(40f) };

            foreach (float offset in offsets)
            {
                float angle = baseAngle + offset;
                Projectile.NewProjectile(
                    source.GetSource_FromAI(), hitPos + angle.ToRotationVector2() * 4f,
                    angle.ToRotationVector2() * 11f,
                    ModContent.ProjectileType<DEBullet_PestSplit>(),
                    splitDmg, source.knockBack * 0.5f, source.owner);
            }

            // 驻留瘟疫毒雾（视觉 dust 模拟）
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(hitPos + Main.rand.NextVector2Circular(18f, 18f),
                    DustID.TerraBlade, Main.rand.NextVector2Circular(1.2f, 1.2f), 120, PestGreen, 0.9f);
                d.noGravity = true;
            }
        }

        private static void SpawnPestImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, PestYellow, 0.85f, 20));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, PestGreen * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 18, true, 0.78f));
                for (int i = 0; i < 14; i++)
                {
                    float angle = MathHelper.TwoPi * i / 14f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 7f, angle.ToRotationVector2() * 8f,
                        false, 10, 0.02f, i % 2 == 0 ? PestGreen : PestYellow, new Vector2(0.8f, 0.45f)));
                }
            }
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.TerraBlade,
                    Main.rand.NextVector2Circular(8f, 8f), 100, PestGreen, 1.1f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Plague round; splits into 3 virulent sub-rounds + a lingering toxic cloud; applies Plague";
        public override string TooltipEffectZH => "瘟疫弹，命中时分裂3颗子弹+驻留毒雾，施加瘟疫";
    }
}
