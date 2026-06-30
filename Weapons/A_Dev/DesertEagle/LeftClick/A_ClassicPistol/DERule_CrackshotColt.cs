using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    /// <summary>
    /// 神枪手柯尔特：金色弹射弹，命中时自动弹射至下一个敌人，最多弹射 2 次。
    /// 弹射时金币粒子散落，最终命中产生金色爆炸。
    /// </summary>
    public class DERule_CrackshotColt : DEBulletRule
    {
        private static readonly Color GoldMain = new(255, 210, 60);
        private static readonly Color GoldAccent = new(255, 245, 140);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.CrackshotColt>();

        // ai[1] 存剩余弹射次数（从 2 开始递减）
        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 2f;

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0).ToDirectionInt();
            projectile.rotation = projectile.velocity.ToRotation()
                + (projectile.spriteDirection == 1 ? 0f : MathHelper.Pi)
                + MathHelper.ToRadians(90f) * projectile.direction;

            // 金色尾迹
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - forward * 3f,
                    DustID.GoldCoin,
                    -forward * 0.5f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120, GoldMain, Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 0.8f,
                    false, 5, 0.012f, GoldAccent, new Vector2(0.5f, 2.0f)));
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBounceEffect(projectile.Center);

            int remainingBounces = (int)projectile.ai[1];
            if (remainingBounces <= 0) return;

            // 找下一个弹射目标（排除当前目标）
            NPC nextTarget = FindNextBounceTarget(projectile.Center, target.whoAmI, 400f);
            if (nextTarget == null) return;

            Vector2 direction = projectile.DirectionTo(nextTarget.Center);
            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                projectile.Center + direction * 10f,
                direction * (projectile.velocity.Length() * 1.08f),
                ModContent.ProjectileType<DELeftBullet>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                remainingBounces - 1f);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 最终命中：金色爆炸
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(projectile.Center, Vector2.Zero, GoldAccent, 0.9f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(projectile.Center, Vector2.Zero, GoldMain,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 20, true, 0.85f));
                for (int i = 0; i < 14; i++)
                {
                    float angle = MathHelper.TwoPi * i / 14f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        projectile.Center + angle.ToRotationVector2() * 8f,
                        angle.ToRotationVector2() * 8f, false, 12, 0.032f,
                        GoldMain, new Vector2(1f, 0.5f)));
                }
            }
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(8f, 8f), 100, GoldAccent, 1.2f);
                dust.noGravity = true;
            }
        }

        private static void SpawnBounceEffect(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, GoldAccent, 0.5f, 12));
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi * i / 6f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos, angle.ToRotationVector2() * 5f, false, 8, 0.02f, GoldMain, new Vector2(0.8f, 0.45f)));
                }
            }
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(5f, 5f), 100, GoldMain, 0.95f);
                dust.noGravity = true;
            }
        }

        private static NPC FindNextBounceTarget(Vector2 pos, int excludeWhoAmI, float maxRange)
        {
            NPC nearest = null;
            float nearestDist = maxRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                if (npc.whoAmI == excludeWhoAmI) continue;
                float dist = Vector2.Distance(pos, npc.Center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = npc;
                }
            }
            return nearest;
        }

        public override string TooltipEffectEN => "Golden ricochet bullet (2 bounces); coin sparks on each hit";
        public override string TooltipEffectZH => "黄金弹射弹，最多弹射2次，每次命中产生金币特效";
    }
}
