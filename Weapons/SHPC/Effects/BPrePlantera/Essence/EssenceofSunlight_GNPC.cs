using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_GNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private readonly List<SunlightMark> marks = new();

        private sealed class SunlightMark
        {
            public int Owner;
            public int Damage;
            public int Timer;
            public int NextStrike = 60;
        }

        public void AddMark(int owner, int damage)
        {
            marks.Add(new SunlightMark
            {
                Owner = owner,
                Damage = damage,
                Timer = 0,
                NextStrike = 60
            });
        }

        public override void AI(NPC npc)
        {
            if (marks.Count <= 0)
                return;

            Vector2 headPos = npc.Center + new Vector2(0f, -npc.height * 0.01f);

            for (int i = 0; i < 3; i++)
            {
                Particle trail = new SparkParticle(
                    headPos + new Vector2(Main.rand.NextFloat(-20f, 20f), 0f),
                    new Vector2(0f, Main.rand.NextFloat(-6f, -3f)),
                    false,
                    40,
                    Main.rand.NextFloat(0.8f, 1.2f),
                    new Color(255, 220, 80)
                );
                GeneralParticleHandler.SpawnParticle(trail);
            }

            if (Main.GameUpdateCount % 2 == 0)
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

            for (int i = marks.Count - 1; i >= 0; i--)
            {
                SunlightMark mark = marks[i];
                mark.Timer++;

                if (mark.Timer >= mark.NextStrike && mark.NextStrike <= 180)
                {
                    Vector2 spawnPos = npc.Center + new Vector2(Main.rand.NextFloat(-3f, 3f) * 16f, -16f * 16f);

                    Projectile.NewProjectile(
                        npc.GetSource_FromThis(),
                        spawnPos,
                        new Vector2(0f, 16f),
                        ModContent.ProjectileType<EssenceofSunlight_Lighting>(),
                        mark.Damage,
                        0f,
                        mark.Owner
                    );

                    mark.NextStrike += 60;
                }

                if (mark.Timer >= 180)
                    marks.RemoveAt(i);
            }
        }
    }
}
