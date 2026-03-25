using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentEntropyEffect : DefaultEffect
    {
        public override int EffectID => 25;
        public override int AmmoType => ModContent.ItemType<MeldBlob>();

        // ===== 冥思主题：极黑 =====
        public override Color ThemeColor => new Color(6, 6, 6);
        public override Color StartColor => new Color(20, 20, 20);
        public override Color EndColor => new Color(0, 0, 0);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 抵消默认减速
            projectile.velocity *= 1.02f;

            // 前向黑尘拖尾，参考 Antumbra / DestructionBolt 的同源感觉
            if (Main.rand.NextBool(4))
            {
                int dustType = Main.rand.NextBool(6) ? 278 : 263;

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(15f, 15f),
                    dustType,
                    -projectile.velocity
                );
                dust.scale = dust.type == 278
                    ? Main.rand.NextFloat(0.3f, 0.6f)
                    : Main.rand.NextFloat(0.6f, 1.4f);
                dust.velocity = -projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.1f, 0.7f);
                dust.noGravity = true;
                dust.color = new Color(12, 12, 12);
            }

            // 再补一层极黑薄雾感
            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    191,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    100,
                    Color.Black,
                    Main.rand.NextFloat(0.9f, 1.3f)
                );
                dust.noGravity = true;
                dust.velocity *= 0.35f;
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 大范围低伤害爆炸
            int explosionIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(projectile.damage * 0.1f),
                projectile.knockBack,
                projectile.owner
            );

            Projectile explosion = Main.projectile[explosionIndex];
            explosion.width = 1000;
            explosion.height = 1000;

            // 黑色思维爆炸脉冲，参考 DestructionBolt 的黑色退场
            for (int i = 0; i < 3; i++)
            {
                Particle blastRing = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Black,
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    1.8f + i * 0.25f,
                    0.55f + i * 0.08f,
                    16,
                    false
                );
                GeneralParticleHandler.SpawnParticle(blastRing);
            }

            // 外层黑尘爆散，参考 ELPEntropyEXP 的 191 / 240
            for (int i = 0; i < 54; i++)
            {
                int dustType = Main.rand.NextBool() ? 191 : 240;

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    dustType,
                    Main.rand.NextVector2Circular(6f, 6f),
                    100,
                    Color.Black,
                    Main.rand.NextFloat(1.1f, 1.7f)
                );
                dust.noGravity = true;
                dust.velocity *= 0.7f;
            }

            // 同源黑绿色残影尘，参考 Antumbra / DestructionBolt
            for (int i = 0; i < 20; i++)
            {
                int dustType = Main.rand.NextBool(6) ? 278 : 263;

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                    dustType,
                    Main.rand.NextVector2Circular(4.5f, 4.5f),
                    0,
                    new Color(18, 24, 18),
                    dustType == 278 ? Main.rand.NextFloat(0.5f, 0.8f) : Main.rand.NextFloat(0.8f, 1.3f)
                );
                dust.noGravity = true;
            }

            // 中心塌陷感的小型极黑火花
            for (int i = 0; i < 14; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    240,
                    dir,
                    0,
                    Color.Black,
                    Main.rand.NextFloat(1.0f, 1.4f)
                );
                dust.noGravity = true;
            }
        }
    }
}