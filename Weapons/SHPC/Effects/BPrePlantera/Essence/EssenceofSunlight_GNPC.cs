using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_GNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool marked;
        public int timer;
        public int owner;

        public override void ResetEffects(NPC npc)
        {
            // 不在这里清marked，保持状态
        }

        public override void AI(NPC npc)
        {
            if (!marked)
                return;

            timer++;

            Vector2 headPos = npc.Center + new Vector2(0f, -npc.height * 0.01f);

            // ===== 垂直上升 Spark =====
            for (int i = 0; i < 3; i++)
            {
                Particle trail = new SparkParticle(
                    headPos + new Vector2(Main.rand.NextFloat(-20f, 20f), 0f),
                    new Vector2(0f, Main.rand.NextFloat(-6f, -3f)), // 严格向上
                    false,
                    40,
                    Main.rand.NextFloat(0.8f, 1.2f),
                    new Color(255, 220, 80)
                );
                GeneralParticleHandler.SpawnParticle(trail);
            }

            // ===== GlowSpark（更亮的）=====
            if (timer % 2 == 0)
            {
                Particle glow = new GlowSparkParticle(
                    headPos + new Vector2(Main.rand.NextFloat(-16f, 16f), 0f),
                    new Vector2(0f, Main.rand.NextFloat(-5f, -2f)),
                    false,
                    10,
                    0.08f,
                    new Color(255, 240, 120),
                    new Vector2(1.2f, 0.3f),
                    true,
                    false,
                    1
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }

            // ===== 3秒触发 =====
            if (timer >= 180)
            {
                Vector2 spawnPos = npc.Center + new Vector2(0f, -16f * 16f);


                Projectile.NewProjectile(
                    npc.GetSource_FromThis(),
                    spawnPos,
                    new Vector2(0f, 16f),
                    ModContent.ProjectileType<EssenceofSunlight_Lighting>(),
                    50,
                    0f,
                    owner
                );

                marked = false;
            }
        }






    }
}