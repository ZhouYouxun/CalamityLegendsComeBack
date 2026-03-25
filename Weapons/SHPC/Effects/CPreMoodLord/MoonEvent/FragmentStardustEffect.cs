using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentStardustEffect : DefaultEffect
    {
        public override int EffectID => 24;

        public override int AmmoType => ItemID.FragmentStardust;

        // ===== 星辰主题 =====
        public override Color ThemeColor => new Color(120, 180, 255);
        public override Color StartColor => new Color(180, 220, 255);
        public override Color EndColor => new Color(60, 120, 220);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ===== 自定义变量（禁止localAI）=====
        private int splitDepth;
        private bool initialized;
        private bool hitEnemy;
        private float damageScale = 0.5f;

        private const int MaxDepth = 6;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (!initialized)
            {
                projectile.timeLeft = 150;
                splitDepth = 0;
                initialized = true;
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 持续减速
            projectile.velocity *= 0.99f;

            // 伤害逐渐恢复
            damageScale = MathHelper.Lerp(damageScale, 1f, 0.02f);

            // ===== 星辰粒子（数学感轻结构）=====
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

            // ===== 接近消失 → 递归分裂 =====
            if (projectile.timeLeft == 1 && !hitEnemy)
            {
                if (splitDepth < MaxDepth)
                {
                    Split(projectile);
                }
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
            hitEnemy = true;
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
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

                    // ===== 继承递归层级 =====
                    // ===== 用 ai[0] 传递递归层级 =====
                    child.ai[0] = projectile.ai[0] + 1;

                    // 初始伤害衰减（用 ai[1] 存）
                    child.ai[1] = 0.5f;

                    // ===== 子弹更短命（递归终止关键）=====
                    child.timeLeft = Math.Max(30, projectile.timeLeft + 40 - splitDepth * 20);
                }
            }
        }










    }
}