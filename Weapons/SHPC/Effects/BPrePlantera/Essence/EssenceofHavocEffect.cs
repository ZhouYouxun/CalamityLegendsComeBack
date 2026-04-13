using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofHavocEffect : DefaultEffect
    {
        public override int EffectID => 5;

        public override int AmmoType => ModContent.ItemType<EssenceofHavoc>();

        public override Color ThemeColor => new Color(255, 110, 40);
        public override Color StartColor => new Color(255, 180, 80);
        public override Color EndColor => new Color(200, 60, 20);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.5f;
            projectile.timeLeft = 140;

            var gp = projectile.GetGlobalProjectile<EssenceofHavoc_GP>();
            gp.fallDelayTimer = 0;
            gp.detectedTarget = false;
            gp.isFalling = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<EssenceofHavoc_GP>();

            float gravity = 0.18f;
            float maxFallSpeed = 16f;

            projectile.velocity.Y += gravity;

            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            // ================= 正下方窄区域检测（修复点） =================
            NPC target = null;

            float maxDistance = 60f * 16f; // 60格
            float maxHorizontal = 1f * 16f; // ⭐ 只允许 ±X格

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || !npc.CanBeChasedBy())
                    continue;

                float dx = npc.Center.X - projectile.Center.X;
                float dy = npc.Center.Y - projectile.Center.Y;

                if (dy > 0 && dy < maxDistance && Math.Abs(dx) <= maxHorizontal)
                {
                    target = npc;
                    break;
                }
            }

            // ================= 触发冲刺 =================
            if (target != null && !gp.isFalling)
            {
                gp.detectedTarget = true;
            }

            if (gp.detectedTarget && !gp.isFalling)
            {
                gp.fallDelayTimer++;

                if (gp.fallDelayTimer >= 1)
                {
                    gp.isFalling = true;

                    // ⭐ 冲刺瞬间（比日光弱一点）
                    projectile.velocity = new Vector2(0f, 12f);

                    // ⭐ 只触发一次特效
                    SpawnChargeBackEffect(projectile);
                }
            }

            // ================= 下坠 =================
            if (gp.isFalling)
            {
                projectile.velocity.X = 0f;
                projectile.velocity.Y += 3.6f;
            }

            projectile.rotation = projectile.velocity.ToRotation();

            projectile.velocity *= 1.030408f;
        }

        // ===== 坠落伤害强化 =====
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            var gp = projectile.GetGlobalProjectile<EssenceofHavoc_GP>();

            if (gp.isFalling)
            {
                modifiers.SourceDamage *= 1.5f;
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            var gp = projectile.GetGlobalProjectile<EssenceofHavoc_GP>();
            bool isFalling = gp.isFalling;

            Vector2 dirX = Vector2.UnitX;
            Vector2 dirY = Vector2.UnitY;

            int layers = 10;
            float baseSpeed = 6f;

            for (int i = 0; i < layers; i++)
            {
                float speed = baseSpeed + i * 1.8f;
                float scale = (0.8f + i * 0.08f) * SquishyLightParticleFactor;
                Color color = Color.Lerp(ThemeColor, Color.White, i / (float)layers);
                int life = 28 + i * 2;

                Vector2[] dirs =
                {
                    dirX,
                    -dirX,
                    dirY,
                    -dirY
                };

                foreach (var dir in dirs)
                {
                    SquishyLightParticle particle = new(
                        projectile.Center,
                        dir * speed,
                        scale,
                        color,
                        life
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }
            }

            Vector2[] di1rs =
            {
                Vector2.UnitX,
                -Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitY
            };

            float[] speeds = { 6f, 10f, 14f };

            foreach (var dir in di1rs)
            {
                foreach (float spd in speeds)
                {
                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        dir * spd,
                        ModContent.ProjectileType<EssenceofHavoc_INV>(),
                        (int)(projectile.damage * (isFalling ? 1.2f : 0.5f)),
                        projectile.knockBack,
                        projectile.owner
                    );
                }
            }
        }

        // ================= 冲刺瞬间特效 =================
        private void SpawnChargeBackEffect(Projectile projectile)
        {
            Vector2 back = -projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f;

                Vector2 dir = back.RotatedBy(angle) * Main.rand.NextFloat(2f, 5f);

                SquishyLightParticle particle = new(
                    projectile.Center,
                    dir,
                    Main.rand.NextFloat(1.2f, 1.6f),
                    Color.Lerp(StartColor, EndColor, Main.rand.NextFloat()),
                    16
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            Particle pulse = new DirectionalPulseRing(
                projectile.Center,
                back * 2f,
                ThemeColor,
                new Vector2(1f, 3f),
                projectile.rotation - (MathHelper.Pi / 4f),
                0.25f,
                0.02f,
                22
            );

            GeneralParticleHandler.SpawnParticle(pulse);
        }
    }

    public class EssenceofHavoc_GP : GlobalProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override bool InstancePerEntity => true;

        public int fallDelayTimer;
        public bool detectedTarget;
        public bool isFalling;
    }
}