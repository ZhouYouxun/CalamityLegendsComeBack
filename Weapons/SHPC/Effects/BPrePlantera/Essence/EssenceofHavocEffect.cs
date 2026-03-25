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

        // ===== 自定义状态 =====
        private int fallDelayTimer; // 延迟计时
        private bool detectedTarget; // 是否检测到目标
        private bool isFalling; // 是否处于坠落状态
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // ===== 重置状态（关键修复）=====【不然从第2发开始就零帧起手，开始直接下落】
            fallDelayTimer = 0;
            detectedTarget = false;
            isFalling = false;
        }
        public override void AI(Projectile projectile, Player owner)
        {
            float gravity = 0.18f;
            float maxFallSpeed = 16f;

            projectile.velocity.Y += gravity;

            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            // ===== 向下射线检测（20×16宽度，不穿墙）=====

            float step = 16f; // 每次往下一个tile
            float maxSteps = 60; // 最远检测（60格）

            NPC target = null;

            for (int s = 0; s < maxSteps; s++)
            {
                Vector2 checkPos = projectile.Center + new Vector2(0f, s * step);

                // ===== 遇到方块直接停止 =====
                if (Collision.SolidCollision(checkPos - new Vector2(10f, 10f), 20, 20))
                {
                    break;
                }

                // ===== 检测NPC（固定20×16范围）=====
                Rectangle hitbox = new Rectangle(
                    (int)checkPos.X - 10,
                    (int)checkPos.Y - 8,
                    20,
                    16
                );

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];

                    if (npc.active && !npc.friendly && npc.CanBeChasedBy())
                    {
                        if (npc.Hitbox.Intersects(hitbox))
                        {
                            target = npc;
                            break;
                        }
                    }
                }

                if (target != null)
                    break;
            }

            // ===== 检测到目标 → 开始延迟计时 =====
            if (target != null && !isFalling)
            {
                detectedTarget = true;
            }

            if (detectedTarget && !isFalling)
            {
                fallDelayTimer++;

                // ⚠️ 这里会在检测到敌人之后等待X帧在下落【确保二者对齐】
                if (fallDelayTimer >= 1)
                {
                    isFalling = true;
                }
            }

            // ===== 真正开始坠落 =====
            if (isFalling)
            {
                projectile.velocity.X = 0f;
                projectile.velocity.Y += 0.6f;
            }

            projectile.rotation = projectile.velocity.ToRotation();

            projectile.velocity *= 1.020408f;
        }

        // ===== 坠落状态伤害强化 =====
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (isFalling)
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
            // ===== 原爆炸 =====
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];

            proj.width = 175;
            proj.height = 175;



            {
                // ===== 十字光束（核心结构）=====

                Vector2 dirX = Vector2.UnitX;
                Vector2 dirY = Vector2.UnitY;

                int layers = 10; // 层数（控制长度）
                float baseSpeed = 6f;

                for (int i = 0; i < layers; i++)
                {
                    float speed = baseSpeed + i * 1.8f; // 速度递进（关键）

                    float scale = (0.8f + i * 0.08f) * SquishyLightParticleFactor;

                    Color color = Color.Lerp(ThemeColor, Color.White, i / (float)layers);

                    int life = 28 + i * 2;

                    // ===== 四个方向 =====
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
            }










        }
    }
}