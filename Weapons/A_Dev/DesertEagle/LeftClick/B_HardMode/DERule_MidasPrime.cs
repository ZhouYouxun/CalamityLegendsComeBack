using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    /// <summary>
    /// 迈达斯统帅：黄金弹，最多弹射 3 次，每次弹射伤害额外 +20%，最终命中金色星爆。
    /// </summary>
    public class DERule_MidasPrime : DEBulletRule
    {
        private static readonly Color GoldPure = new(255, 215, 0);
        private static readonly Color GoldDeep = new(200, 150, 0);
        private static readonly Color GoldWhite = new(255, 248, 180);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.MidasPrime>();

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 3f;  // 初始3次弹射

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0).ToDirectionInt();

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            int remainBounces = (int)projectile.ai[1];

            // 弹射次数越少颜色越亮（越接近最终一击颜色越纯）
            Color trailColor = Color.Lerp(GoldDeep, GoldPure, 1f - remainBounces / 3f);

            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center - forward * 3f,
                    DustID.GoldCoin, -forward * 0.55f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    120, trailColor, Main.rand.NextFloat(0.85f, 1.15f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 1.2f, false, 6,
                    0.018f * (1f + (3 - remainBounces) * 0.15f),
                    Color.Lerp(GoldPure, GoldWhite, 0.4f), new Vector2(0.6f, 2.2f)));
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            // 每弹射一次增伤 20%（ai[1]越小=弹射次数越多=增伤越高）
            int consumed = 3 - (int)projectile.ai[1];
            if (consumed > 0)
                modifiers.FinalDamage += consumed * 0.2f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBounceGold(projectile.Center);
            int remaining = (int)projectile.ai[1];
            if (remaining <= 0)
            {
                SpawnFinalGoldBurst(projectile.Center);
                return;
            }

            NPC nextTarget = FindNextBounceTarget(projectile.Center, target.whoAmI, 500f);
            if (nextTarget == null)
            {
                SpawnFinalGoldBurst(projectile.Center);
                return;
            }

            Vector2 dir = projectile.DirectionTo(nextTarget.Center);
            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                projectile.Center + dir * 12f,
                dir * (projectile.velocity.Length() * 1.1f),
                ModContent.ProjectileType<DELeftBullet>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                remaining - 1f);
        }

        private static void SpawnBounceGold(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, GoldPure, 0.45f, 10));
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi * i / 6f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 6f, angle.ToRotationVector2() * 6f,
                        false, 7, 0.018f, GoldPure, new Vector2(0.75f, 0.4f)));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(6f, 6f), 100, GoldPure, 1.0f);
                dust.noGravity = true;
            }
        }

        private static void SpawnFinalGoldBurst(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, GoldWhite, 1.2f, 26));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, GoldPure,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.09f, 24, true, 0.88f));
                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 10f, angle.ToRotationVector2() * 11f,
                        false, 16, 0.038f, i % 2 == 0 ? GoldPure : GoldWhite, new Vector2(1.1f, 0.5f)));
                }
            }
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(10f, 10f), 100, GoldWhite, 1.3f);
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
                if (dist < nearestDist) { nearestDist = dist; nearest = npc; }
            }
            return nearest;
        }

        public override string TooltipEffectEN => "Gold bouncing bullet (3 bounces); +20% damage per bounce; golden starburst on final hit";
        public override string TooltipEffectZH => "黄金弹射弹（最多3次），每次弹射+20%伤害，最终命中触发金色星爆";
    }
}
