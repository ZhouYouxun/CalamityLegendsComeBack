using CalamityLegendsComeBack.Weapons.A_Olds.TheEnforcer;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofMightEffect : DefaultEffect
    {
        public override int EffectID => 13;

        public override int AmmoType => ItemID.SoulofMight;

        public override Color ThemeColor => new Color(70, 110, 255);
        public override Color StartColor => new Color(150, 190, 255);
        public override Color EndColor => new Color(20, 40, 120);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<BossSoulofMight_GP>().firstFrame = true;
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            BossSoulofMight_GP gp = projectile.GetGlobalProjectile<BossSoulofMight_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            // 主爆炸：大型 SHPE (224)
            SpawnSHPE(projectile, projectile.Center, 224);

            // 主爆炸产生 5~7 灵魂弹
            SpawnSoulBalls(projectile, projectile.Center, Main.rand.Next(5, 8));

            // 3 个小爆炸，尽量朝不同敌人散开
            Vector2[] miniPositions = GetSpreadExplosionPositions(projectile.Center, 3, 380f);
            foreach (Vector2 pos in miniPositions)
            {
                SpawnSHPE(projectile, pos, 75);
                SpawnSoulBalls(projectile, pos, Main.rand.Next(5, 8));
            }
        }

        private void SpawnSHPE(Projectile source, Vector2 center, int size)
        {
            int idx = Projectile.NewProjectile(
                source.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                source.damage,
                source.knockBack,
                source.owner);

            if (!Main.projectile.IndexInRange(idx))
                return;

            Projectile p = Main.projectile[idx];
            p.Resize(size, size);
            p.Center = center;
            p.DamageType = DamageClass.Magic;
            p.netUpdate = true;
        }

        private void SpawnSoulBalls(Projectile source, Vector2 from, int count)
        {
            NPC target = from.ClosestNPCAt(900f);
            float baseAngle = target != null
                ? (target.Center - from).ToRotation()
                : Main.rand.NextFloat(MathHelper.TwoPi);

            float spread = MathHelper.ToRadians(75f);
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + Main.rand.NextFloat(-spread, spread);
                float speed = Main.rand.NextFloat(9f, 17f);
                Projectile.NewProjectile(
                    source.GetSource_FromThis(),
                    from,
                    angle.ToRotationVector2() * speed,
                    ModContent.ProjectileType<BossSoulofMight_Ball>(),
                    (int)(source.damage * 0.38f),
                    source.knockBack,
                    source.owner);
            }
        }

        // 为 3 个小爆炸选取尽量朝向不同敌人、彼此分散的位置
        private Vector2[] GetSpreadExplosionPositions(Vector2 center, int count, float range)
        {
            List<NPC> candidates = Main.npc
                .Where(n => n.active && !n.friendly && n.CanBeChasedBy()
                            && Vector2.Distance(center, n.Center) <= range)
                .OrderBy(n => Vector2.Distance(center, n.Center))
                .ToList();

            Vector2[] result = new Vector2[count];
            List<int> used = new();

            for (int i = 0; i < count; i++)
            {
                NPC chosen = null;
                foreach (NPC npc in candidates)
                {
                    if (used.Contains(npc.whoAmI))
                        continue;
                    bool tooClose = result.Take(i).Any(p => p != Vector2.Zero && Vector2.Distance(p, npc.Center) < 130f);
                    if (!tooClose)
                    {
                        chosen = npc;
                        break;
                    }
                }

                if (chosen != null)
                {
                    result[i] = chosen.Center;
                    used.Add(chosen.whoAmI);
                }
                else
                {
                    // 没有合适敌人，均匀分散
                    float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.35f, 0.35f);
                    result[i] = center + angle.ToRotationVector2() * Main.rand.NextFloat(range * 0.45f, range);
                }
            }

            return result;
        }
    }

    internal class BossSoulofMight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool firstFrame;
    }
}
