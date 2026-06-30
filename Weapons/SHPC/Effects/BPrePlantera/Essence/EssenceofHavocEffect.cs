using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

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
            projectile.velocity *= 1.1f;
            //projectile.extraUpdates++;
            projectile.timeLeft = 900;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.velocity.Y = MathHelper.Min(projectile.velocity.Y + 0.34f, 18f);
            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (projectile.velocity.Y > 0f)
                modifiers.SourceDamage *= 1.5f;
            else if (projectile.velocity.Y < 0f)
                modifiers.SourceDamage *= 0.75f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/最后通牒爆炸")
            {
                Volume = 3f
            }, projectile.Center);

            if (projectile.owner == Main.myPlayer)
            {
                int explosionIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner);

                if (Main.projectile.IndexInRange(explosionIndex))
                {
                    Projectile explosion = Main.projectile[explosionIndex];
                    int explosionSize = 224; // 边长为224像素
                    explosion.width = explosionSize;
                    explosion.height = explosionSize;
                    explosion.Center = projectile.Center;
                    explosion.netUpdate = true;
                }
            }

            bool movingDownward = projectile.velocity.Y > 0f;

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

                foreach (Vector2 dir in dirs)
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

            Vector2[] invDirs =
            {
                Vector2.UnitX,
                -Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitY
            };

            float[] speeds = { 6f, 10f, 14f };

            foreach (Vector2 dir in invDirs)
            {
                foreach (float speed in speeds)
                {
                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        dir * speed,
                        ModContent.ProjectileType<EssenceofHavoc_INV>(),
                        (int)(projectile.damage * (movingDownward ? 0.9f : 0.5f)),
                        projectile.knockBack,
                        projectile.owner
                    );
                }
            }

            // ===== 新增自定义火花与Dust特效，配合原有的十字爆炸 =====
            if (!Main.dedServ)
            {
                // 1. 环状散开的火尘 (SolarFlare & Torch & CopperCoin dusts)
                for (int i = 0; i < 35; i++)
                {
                    Vector2 dustVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 11f);
                    Dust d = Dust.NewDustPerfect(
                        projectile.Center,
                        Utils.SelectRandom(Main.rand, DustID.SolarFlare, DustID.Torch, DustID.CopperCoin),
                        dustVel,
                        100,
                        default,
                        Main.rand.NextFloat(1.3f, 2.4f)
                    );
                    d.noGravity = true;
                }

                // 2. 随机四射的火花粒子 (SparkParticles)
                for (int i = 0; i < 18; i++)
                {
                    Vector2 sparkVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 10f);
                    Color sparkColor = Main.rand.NextBool() ? new Color(255, 125, 40) : new Color(255, 60, 20);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        sparkVel,
                        false,
                        Main.rand.Next(24, 36),
                        Main.rand.NextFloat(0.9f, 1.5f),
                        sparkColor
                    ));
                }

                // 3. 逐渐扩大的红色光圈 (DirectionalPulseRing)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    projectile.Center,
                    Vector2.Zero,
                    new Color(255, 90, 30),
                    Vector2.One,
                    0f,
                    0.28f,
                    0f,
                    24
                ));
            }
        }
    }
}
