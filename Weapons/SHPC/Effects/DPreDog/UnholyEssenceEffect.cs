using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class UnholyEssenceEffect : DefaultEffect
    {
        public override int EffectID => 26;

        public override int AmmoType => ModContent.ItemType<UnholyEssence>();

        public override Color ThemeColor => new Color(255, 220, 90);   // 主体金黄
        public override Color StartColor => new Color(255, 245, 180);  // 接近白的高光黄
        public override Color EndColor => new Color(255, 140, 40);     // 橙红收边

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        // 自定义计数器，禁止使用 localAI
        private int lifeTimer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            lifeTimer = 0;

            // 初始速度 1.5 倍
            projectile.velocity *= 1.5f;

            // 出生瞬间做一圈偏神圣/邪异的绿色能量喷散
            for (int i = 0; i < 10; i++)
            {
                Vector2 dustVelocity = projectile.velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.15f, 0.75f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(2) ? DustID.GemEmerald : DustID.GreenTorch,
                    dustVelocity,
                    0,
                    Main.rand.NextBool(2) ? new Color(210, 255, 180) : new Color(120, 255, 120),
                    Main.rand.NextFloat(1.1f, 1.8f)
                );
                dust.noGravity = true;
            }

            // 再叠一层前向冲击脉冲
            Particle pulse = new CustomPulse(
                projectile.Center,
                projectile.velocity * 0.12f,
                new Color(170, 255, 150),
                "CalamityMod/Particles/BloomCircle",
                new Vector2(0.7f, 1.4f),
                projectile.velocity.ToRotation(),
                0.02f,
                0.18f,
                18,
                true
            );
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            lifeTimer++;

            // 每帧加速
            projectile.velocity *= 1.02f;

            // 飞行照明
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.55f);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            // 前向拖尾火花，参考 SeekingScorcher 那种更有冲击力的高亮感觉
            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPos = projectile.Center - forward * Main.rand.NextFloat(6f, 18f) + right * Main.rand.NextFloat(-4f, 4f);
                Vector2 vel = -forward.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 3.6f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    vel,
                    0,
                    Main.rand.NextBool(2) ? new Color(180, 255, 170) : new Color(120, 255, 120),
                    Main.rand.NextFloat(1f, 1.55f)
                );
                dust.noGravity = true;
            }

            // 周边薄雾感，让主体更“邪异”
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GreenFairy,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    new Color(140, 255, 140),
                    Main.rand.NextFloat(0.8f, 1.25f)
                );
                dust.noGravity = true;
                dust.velocity *= 0.4f;
            }

            // 低频释放一个前向细长脉冲，增强“高速穿行感”
            if (lifeTimer % 5 == 0)
            {
                Particle streakPulse = new CustomPulse(
                    projectile.Center - forward * 8f,
                    projectile.velocity * 0.05f,
                    new Color(150, 255, 140),
                    "CalamityMod/Particles/BlastCone",
                    new Vector2(Main.rand.NextFloat(0.45f, 0.8f), Main.rand.NextFloat(1.1f, 1.8f)),
                    projectile.velocity.ToRotation(),
                    0.04f,
                    0.12f,
                    14,
                    true
                );
                GeneralParticleHandler.SpawnParticle(streakPulse);
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中时补一点近距离爆开的绿色火花
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    dir,
                    0,
                    Main.rand.NextBool(2) ? new Color(200, 255, 180) : new Color(110, 255, 110),
                    Main.rand.NextFloat(1f, 1.6f)
                );
                dust.noGravity = true;
            }

            Particle hitPulse = new CustomPulse(
                target.Center,
                Vector2.Zero,
                new Color(180, 255, 160),
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One * 0.8f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0f,
                0.12f,
                18,
                true
            );
            GeneralParticleHandler.SpawnParticle(hitPulse);
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 forward = projectile.oldVelocity.Length() > 0.1f
                ? projectile.oldVelocity.SafeNormalize(Vector2.UnitX)
                : projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 绝对只朝左右之一释放
            Vector2 waveVelocity = forward.X >= 0f ? Vector2.UnitX : -Vector2.UnitX;
            // === 发射左右冲击波（核心逻辑）===
            Vector2 spawnVelocity = waveVelocity * 24f; // 速度必须高，否则没有“冲击波”质感

            int waveIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                spawnVelocity,
                ModContent.ProjectileType<UnholyEssence_Wave>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            // 传递初始方向（给拖尾/特效用）
            Projectile wave = Main.projectile[waveIndex];
            wave.rotation = spawnVelocity.ToRotation();

            // 死亡主脉冲
            Particle deathPulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(160, 255, 150),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 1.05f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.04f,
                0.32f,
                22,
                true
            );
            GeneralParticleHandler.SpawnParticle(deathPulse);

            // 前向弧形冲击的视觉预热，方向绝对跟随左右
            for (int i = 0; i < 14; i++)
            {
                float angleOffset = MathHelper.Lerp(-0.85f, 0.85f, i / 13f);
                Vector2 dir = waveVelocity.RotatedBy(angleOffset);
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(6f, 18f);
                Vector2 vel = dir * Main.rand.NextFloat(2f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    vel,
                    0,
                    Main.rand.NextBool(2) ? new Color(210, 255, 180) : new Color(120, 255, 120),
                    Main.rand.NextFloat(1.1f, 1.8f)
                );
                dust.noGravity = true;
            }

            // 中心补一层爆散
            for (int i = 0; i < 20; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5.5f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(2) ? DustID.GreenFairy : DustID.GreenTorch,
                    dir,
                    0,
                    new Color(160, 255, 150),
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }
        }
    }
}