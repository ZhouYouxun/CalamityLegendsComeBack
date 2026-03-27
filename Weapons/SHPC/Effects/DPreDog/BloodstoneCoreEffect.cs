using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class BloodstoneCoreEffect : DefaultEffect
    {
        public override int EffectID => 29;

        public override int AmmoType => ModContent.ItemType<BloodstoneCore>();

        public override Color ThemeColor => new Color(220, 40, 40);
        public override Color StartColor => new Color(255, 120, 120);
        public override Color EndColor => new Color(90, 0, 0);

        public override float SquishyLightParticleFactor => 0.85f;
        public override float ExplosionPulseFactor => 1.05f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();
            data.empowered = false;
            data.linkedPlayerIndex = -1;

            Player targetPlayer = FindClosestValidPlayer(projectile.Center, 45f * 16f);
            if (targetPlayer != null)
            {
                int lifeThreshold = (int)(targetPlayer.statLifeMax2 * 0.5f);

                if (targetPlayer.statLife > lifeThreshold && targetPlayer.statLife > 66)
                {
                    targetPlayer.statLife -= 66;
                    CombatText.NewText(targetPlayer.Hitbox, Color.Red, 66, true, false);

                    data.empowered = true;
                    data.linkedPlayerIndex = targetPlayer.whoAmI;
                }
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            Color lightColor = data.empowered ? new Color(255, 70, 70) : new Color(170, 35, 35);
            Lighting.AddLight(projectile.Center, lightColor.ToVector3() * (data.empowered ? 0.75f : 0.45f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    -projectile.velocity * Main.rand.NextFloat(0.18f, 0.5f),
                    0,
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, data.empowered ? 1.8f : 1.35f)
                );
                dust.noGravity = true;
            }

            if (data.empowered && data.linkedPlayerIndex >= 0 && data.linkedPlayerIndex < Main.maxPlayers)
            {
                Player linkedPlayer = Main.player[data.linkedPlayerIndex];
                if (linkedPlayer.active && !linkedPlayer.dead)
                {
                    CreateBloodLink(projectile.Center, linkedPlayer.Center);
                }
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            if (data.empowered)
                modifiers.SourceDamage *= 1.2f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            bool empowered = data.empowered;
            Vector2 center = projectile.Center;

            // ================= 命中特效（保留原有） =================
            int count = empowered ? 18 : 10;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, empowered ? 6f : 3.5f);

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    velocity,
                    0,
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, empowered ? 1.8f : 1.25f)
                );
                dust.noGravity = true;
            }

            // ================= 爆炸生成 =================
            int explosionDamage = empowered ? (int)(projectile.damage * 1.6f) : (int)(projectile.damage * 0.95f);

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                explosionDamage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            int radius = empowered ? 260 : 90;
            proj.width = radius;
            proj.height = radius;

            // ================= 统计范围内敌人数量 =================
            int enemyCount = 0;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.lifeMax <= 5)
                    continue;

                if (Vector2.Distance(center, npc.Center) <= radius * 0.5f)
                {
                    enemyCount++;
                }
            }

            // ================= 吸血计算（核心设计） =================
            // 👉 单体 = 负收益，多目标 = 正收益
            int healAmount = 0;

            if (empowered)
            {
                if (enemyCount <= 1)
                {
                    healAmount = 18; // ❌ 明显亏（原本扣66）
                }
                else if (enemyCount == 2)
                {
                    healAmount = 36;
                }
                else if (enemyCount == 3)
                {
                    healAmount = 66; // ✔ 打平
                }
                else
                {
                    // 👉 超过3个开始爆发收益
                    healAmount = 66 + (enemyCount - 3) * 28;
                }

                // ================= 应用回血 =================
                owner.statLife += healAmount;
                if (owner.statLife > owner.statLifeMax2)
                    owner.statLife = owner.statLifeMax2;

                owner.HealEffect(healAmount);

                // ================= 吸血特效（完全保留） =================
                Vector2 dir = (owner.Center - center).SafeNormalize(Vector2.UnitY);

                for (int j = 0; j < 18; j++)
                {
                    float t = j / 18f;
                    Vector2 pos = Vector2.Lerp(center, owner.Center, t);

                    Dust d = Dust.NewDustPerfect(
                        pos,
                        Main.rand.NextBool(2) ? DustID.Blood : DustID.LifeDrain,
                        dir * Main.rand.NextFloat(1f, 4f),
                        0,
                        Color.Lerp(new Color(255, 120, 120), new Color(200, 20, 20), Main.rand.NextFloat()),
                        Main.rand.NextFloat(1f, 1.8f)
                    );
                    d.noGravity = true;
                }
            }
        }














        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            // ================= 基础参数 =================
            bool empowered = data.empowered;

            // ================= 强度倍率 =================
            float scaleFactor = empowered ? 1.8f : 1f;
            int density = empowered ? 90 : 40;

            Vector2 center = projectile.Center;

            //// ================= ① 核心血爆脉冲 =================
            //Particle corePulse = new CustomPulse(
            //    center,
            //    Vector2.Zero,
            //    empowered ? new Color(255, 60, 60) : new Color(200, 40, 40),
            //    "CalamityMod/Particles/DetailedExplosion",
            //    Vector2.One * (1.2f * scaleFactor),
            //    Main.rand.NextFloat(-0.3f, 0.3f),
            //    0.05f,
            //    0.4f * scaleFactor,
            //    (int)(28 * scaleFactor),
            //    false
            //);
            //GeneralParticleHandler.SpawnParticle(corePulse);

            // ================= ② 阿基米德螺旋血爆 =================
            float golden = MathHelper.ToRadians(137.5f);
            for (int i = 0; i < density; i++)
            {
                float t = i * 0.25f;
                float r = 3f + 0.35f * t;

                Vector2 vel = (t + i * 0.1f).ToRotationVector2() * r * Main.rand.NextFloat(1.5f, 3.5f) * scaleFactor;

                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.Blood,
                    vel,
                    0,
                    Color.Lerp(new Color(255, 80, 80), new Color(120, 0, 0), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.2f, 2.2f * scaleFactor)
                );
                d.noGravity = true;
            }

            // ================= ③ 玫瑰曲线血阵（核心宏伟结构） =================
            int petals = empowered ? 90 : 45;
            for (int i = 0; i < petals; i++)
            {
                float theta = MathHelper.TwoPi * i / petals;
                float rose = 10f * (1 + 0.5f * (float)Math.Sin(6 * theta));

                Vector2 vel = theta.ToRotationVector2() * rose * Main.rand.NextFloat(1.5f, 3.5f) * scaleFactor;

                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.Blood,
                    vel,
                    0,
                    Color.Red,
                    Main.rand.NextFloat(1.4f, 2.4f * scaleFactor)
                );
                d.noGravity = true;
            }

            // ================= ④ 黄金角血刺（Bloodstone风格核心） =================
            int spikes = empowered ? 70 : 30;
            for (int i = 0; i < spikes; i++)
            {
                float angle = i * golden;

                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(4f, 9f) * scaleFactor;

                SparkParticle spark = new SparkParticle(
                    center,
                    vel,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(1.6f, 3.2f * scaleFactor),
                    Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ================= ⑤ 血雾膨胀层 =================
            int smokeCount = empowered ? 18 : 6;
            for (int i = 0; i < smokeCount; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center,
                    Main.rand.NextVector2Circular(3f, 3f),
                    Color.Lerp(new Color(80, 0, 0), new Color(200, 30, 30), Main.rand.NextFloat()),
                    Main.rand.Next(30, 50),
                    Main.rand.NextFloat(1f, 2.2f * scaleFactor),
                    0.4f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

          
        }

        private Player FindClosestValidPlayer(Vector2 center, float maxDistance)
        {
            Player closest = null;
            float closestDistance = maxDistance;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;

                float distance = Vector2.Distance(center, player.Center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player;
                }
            }

            return closest;
        }

        private void CreateBloodLink(Vector2 start, Vector2 end)
        {
            Vector2 toEnd = end - start;
            float distance = toEnd.Length();
            if (distance <= 8f)
                return;

            Vector2 direction = toEnd / distance;
            int steps = (int)(distance / 12f);

            for (int i = 0; i <= steps; i++)
            {
                if (Main.rand.NextBool(4))
                    continue;

                float factor = i / (float)Math.Max(steps, 1);
                Vector2 point = Vector2.Lerp(start, end, factor) + Main.rand.NextVector2Circular(3f, 3f);

                Dust dust = Dust.NewDustPerfect(
                    point,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.1f),
                    0,
                    Color.Lerp(new Color(255, 90, 90), new Color(120, 0, 0), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.85f, 1.35f)
                );
                dust.noGravity = true;
            }
        }

    }

    public class BloodstoneCoreEffectData : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool empowered;
        public int linkedPlayerIndex = -1;
    }
}