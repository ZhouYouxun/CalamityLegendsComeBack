using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentStardustEffect : DefaultEffect
    {
        public override int EffectID => 24;
        public override int AmmoType => ItemID.FragmentStardust;

        public override Color ThemeColor => new Color(120, 180, 255);
        public override Color StartColor => new Color(180, 220, 255);
        public override Color EndColor => new Color(60, 120, 220);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ===== 状态 =====
        private int splitDepth;
        private bool initialized;
        private bool hitEnemy;
        private float damageScale = 0.5f;

        private int splitTimer; // 递归计时器

        private const int MaxDepth = 6;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (!initialized)
            {
                projectile.timeLeft = 150;
                initialized = true;

                splitDepth = (int)projectile.ai[0];

                // ⭐ 初始直接分裂一次（保证递归一定启动）
                if (splitDepth < MaxDepth)
                {
                    Split(projectile);
                }
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            splitDepth = (int)projectile.ai[0];

            // ❌ 删除额外减速（避免停住被爆炸机制干掉）

            // ===== 伤害缓慢恢复 =====
            damageScale = MathHelper.Lerp(damageScale, 1f, 0.02f);

            // ===== 星辰粒子 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 vel = Main.rand.NextVector2Circular(1.2f, 1.2f);

                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.Electric,
                    vel,
                    0,
                    Color.Lerp(Color.LightBlue, Color.White, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.1f, 1.6f)
                );
                d.noGravity = true;
            }

            // ================= ⭐ 核心递归 =================
            splitTimer++;

            // ⭐ 更快触发，避免被爆炸抢先
            if (!hitEnemy && splitDepth < MaxDepth && splitTimer >= 10)
            {
                splitTimer = 0;
                Split(projectile);
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= damageScale;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitEnemy = true; // 命中后停止递归
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 保持为空（递归完全由AI控制）
        }

        // ================= 核心递归 =================
        private void Split(Projectile projectile)
        {
            Vector2 baseDir = Main.rand.NextVector2Unit();

            for (int i = 0; i < 2; i++)
            {
                Vector2 dir = baseDir.RotatedBy(MathHelper.Pi * i);
                Vector2 velocity = dir * Main.rand.NextFloat(2f, 4f);

                int projID = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    projectile.type,
                    (int)(projectile.damage * 0.5f),
                    projectile.knockBack,
                    projectile.owner
                );

                if (Main.projectile.IndexInRange(projID))
                {
                    Projectile child = Main.projectile[projID];

                    // ===== 递归层级 =====
                    int nextDepth = splitDepth + 1;
                    child.ai[0] = nextDepth;

                    // ⭐ 关键：禁止子弹 proximity 爆炸（否则递归被打断）
                    child.ai[1] = 0f;

                    // ===== 生命周期控制 =====
                    child.timeLeft = Math.Max(40, 100 - splitDepth * 12);
                }
            }
        }
    }
}