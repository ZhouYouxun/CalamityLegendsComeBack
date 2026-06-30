using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    /// <summary>
    /// 幻星冲击炮：橙蓝星质归巢弹，命中施加星质感染，双色尾迹特效。
    /// </summary>
    public class DERule_AstralBlaster : DEBulletRule
    {
        private static readonly Color AstralOrange = new(255, 160, 50);
        private static readonly Color AstralBlue = new(100, 160, 255);
        private static readonly Color AstralWhite = new(230, 220, 255);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.AstralBlaster>();

        public override int ExtraUpdates => 3;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.light = 0.5f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 归巢
            NPC target = FindNearest(projectile.Center, 280f);
            if (target != null && projectile.Distance(target.Center) > 30f)
            {
                float speed = projectile.velocity.Length();
                Vector2 desired = projectile.DirectionTo(target.Center) * speed;
                projectile.velocity = Vector2.Lerp(projectile.velocity, desired, 0.09f);
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            float t = Main.GlobalTimeWrappedHourly * 8f + projectile.whoAmI * 0.73f;

            // 橙蓝交替双色尾迹
            for (int i = 0; i < 2; i++)
            {
                Color trailColor = i % 2 == 0 ? AstralOrange : AstralBlue;
                Vector2 side = projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                float offset = (float)System.Math.Sin(t + i * MathHelper.Pi) * 4.5f;
                Dust dust = Dust.NewDustPerfect(projectile.Center + side * offset,
                    DustID.TintableDustLighted,
                    -projectile.velocity * 0.35f, 120, trailColor, 0.95f);
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f), false, 6,
                    0.015f, Main.rand.NextBool() ? AstralOrange : AstralBlue, new Vector2(0.6f, 0.6f)));
            }

            Lighting.AddLight(projectile.Center, AstralOrange.R / 255f * 0.4f, AstralBlue.G / 255f * 0.35f, AstralBlue.B / 255f * 0.4f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
            SpawnAstralImpact(projectile.Center);
        }

        private static void SpawnAstralImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, AstralWhite, 0.9f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, AstralOrange * 0.7f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.065f, 18, true, 0.82f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, AstralBlue * 0.6f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.09f, 22, true, 0.72f));
                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi * i / 16f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 8f, angle.ToRotationVector2() * 9f,
                        false, 12, 0.022f, i % 2 == 0 ? AstralOrange : AstralBlue, new Vector2(0.9f, 0.5f)));
                }
            }
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(9f, 9f), 120,
                    i % 2 == 0 ? AstralOrange : AstralBlue, 1.1f);
                dust.noGravity = true;
            }
        }

        private static NPC FindNearest(Vector2 pos, float range)
        {
            NPC nearest = null;
            float nearestDist = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = Vector2.Distance(pos, npc.Center);
                if (dist < nearestDist) { nearestDist = dist; nearest = npc; }
            }
            return nearest;
        }

        public override string TooltipEffectEN => "Homing astral round with dual orange-blue trail; applies Astral Infection on hit";
        public override string TooltipEffectZH => "追踪星质弹，橙蓝双色尾迹，命中施加星质感染";
    }
}
