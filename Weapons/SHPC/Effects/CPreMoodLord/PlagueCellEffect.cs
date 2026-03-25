using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class PlagueCellEffect : DefaultEffect
    {
        public override int EffectID => 18;
        public override int AmmoType => ModContent.ItemType<PlagueCellCanister>();

        // ===== 瘟疫主题色 =====
        public override Color ThemeColor => new Color(40, 120, 40);
        public override Color StartColor => new Color(110, 220, 90);
        public override Color EndColor => new Color(20, 55, 20);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 给命中的敌人顺手挂上瘟疫相关debuff
            target.AddBuff(ModContent.BuffType<Plague>(), 180);
            target.AddBuff(BuffID.Venom, 180);
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ================= 大范围瘟疫爆炸伤害 =================
            int explosionIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile explosion = Main.projectile[explosionIndex];
            explosion.width = 260;
            explosion.height = 260;

            // ================= 核心贴图爆闪 =================
            Color explosionColor = Color.DarkGreen;
            Particle blastRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                explosionColor,
                "CalamityLegendsComeBack/Weapons/SHPC/Effects/CPreMoodLord/BiologicalHazards",
                Vector2.One * 0.33f,
                Main.rand.NextFloat(-10f, 10f),
                0.075f,
                0.40f,
                30
            );
            GeneralParticleHandler.SpawnParticle(blastRing);

            // "CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord/BiologicalHazards",



            // ================= 五边形脉冲环 =================
            float radius = 2 * 16f;
            int pulseCount = 5;
            float baseAngle = Main.rand.NextFloat(0f, MathHelper.TwoPi);

            for (int i = 0; i < pulseCount; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi / pulseCount * i;
                Vector2 position = projectile.Center + angle.ToRotationVector2() * radius;

                float pulseScale = Main.rand.NextFloat(0.32f, 0.42f);
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    position,
                    new Vector2(2f, 2f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.25f, 1.2f),
                    (Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green) * 0.85f,
                    new Vector2(1f, 1f),
                    pulseScale - 0.25f,
                    pulseScale,
                    0f,
                    15
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // ================= 十字与斜向炸散 Dust =================
            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi / 18f * i;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 10f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                    velocity,
                    0,
                    Color.DarkGreen,
                    Main.rand.NextFloat(1.15f, 1.75f)
                );
                dust.noGravity = true;
            }

            for (int i = 0; i < 26; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextBool() ? DustID.TerraBlade : DustID.GreenTorch,
                    velocity,
                    0,
                    Main.rand.NextBool() ? Color.DarkGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }

            // ================= 后方射出 3~9 个瘟疫蜜蜂 =================
            int beeCount = Main.rand.Next(3, 10);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 backward = -forward;

            // 从玩家后方一点的位置放出，更有“武器后喷出蜂群”的感觉
            Vector2 spawnCenter = owner.Center + backward * (2.2f * 16f);

            for (int i = 0; i < beeCount; i++)
            {
                Vector2 spawnPos = spawnCenter + Main.rand.NextVector2Circular(14f, 14f);

                // 基础方向还是朝前，但带较大散布
                Vector2 beeVelocity =
                    forward.RotatedByRandom(MathHelper.ToRadians(30f)) *
                    Main.rand.NextFloat(7f, 13f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    beeVelocity,
                    ModContent.ProjectileType<PlaguenadeBee>(),
                    (int)(projectile.damage * 0.42f),
                    0f,
                    projectile.owner
                );
            }

            // ================= 音效 =================
            SoundEngine.PlaySound(SoundID.Item14, projectile.Center);
        }








    }
}