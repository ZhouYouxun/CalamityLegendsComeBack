using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
        public override bool EnableDefaultSlowdown => false;

        // ===== 状态 =====
        private int splitDepth;
        private bool initialized;
        private float damageScale = 0.5f;

        private const int MaxDepth = 6;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (!initialized)
            {
                projectile.timeLeft = 150;
                projectile.velocity *= 1.9f;
                initialized = true;

                splitDepth = (int)projectile.ai[0];
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            splitDepth = (int)projectile.ai[0];
            projectile.localAI[0] += projectile.velocity.Length();
            if (projectile.localAI[0] >= 150f * 16f)
                projectile.Kill();

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

         
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= damageScale;
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseDir = Main.rand.NextVector2Unit();
            if (FragmentStardust_Cell.ActiveCellCount() >= FragmentStardust_Cell.MaxActiveCells)
                return;

            for (int i = 0; i < 2; i++)
            {
                if (FragmentStardust_Cell.ActiveCellCount() >= FragmentStardust_Cell.MaxActiveCells)
                    break;

                Vector2 dir = baseDir.RotatedBy(MathHelper.Pi * i);
                Vector2 velocity = dir * Main.rand.NextFloat(2f, 4f);

                int projID = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<FragmentStardust_Cell>(),
                    (int)(projectile.damage * 1.33f),
                    projectile.knockBack,
                    projectile.owner,
                    0f
                );
            }
        }


    }
}
