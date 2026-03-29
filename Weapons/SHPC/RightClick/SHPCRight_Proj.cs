using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal class SHPCRight_Proj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        public int WeaponStage;

        private bool penetratedSet;

        // 是否允许分裂（子弹用）
        private bool canSplit = true;

        public override Color? GetAlpha(Color lightColor)
            => new Color(255, 235, 120, 0);

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBeam(200f, 3f, lightColor);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 500;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // ===== 根据热值调整初速度倍率 =====
            float speedMultiplier = WeaponStage switch
            {
                1 => 0.86f,
                2 => 0.98f,
                3 => 1.12f,
                4 => 1.25f,
                5 => 1.38f,
                6 => 1.48f,
                >= 7 => 1.58f,
                _ => 0.86f
            };

            Projectile.velocity *= speedMultiplier;

            // 子弹标记（防止递归分裂）
            if (Projectile.ai[0] == 1f)
                canSplit = false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 25;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, 0.5f, 0.2f, 0.5f);
            float timerIncr = 3f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += timerIncr;
                if (Projectile.localAI[0] > 100f)
                {
                    Projectile.localAI[0] = 100f;
                }
            }
            else
            {
                Projectile.localAI[0] -= timerIncr;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 120).ToVector3() * 0.6f);

            // 穿透设置
            if (!penetratedSet)
            {
                Projectile.penetrate = WeaponStage switch
                {
                    >= 7 => -1,
                    >= 4 => 7,
                    >= 2 => 3,
                    _ => 1
                };
                penetratedSet = true;
            }

            // 飞行特效：简单随机释放
            if (Projectile.numUpdates == 0)
            {
                //SpawnFlightEffects();
            }
        }

        // ===== 核心：伤害倍率 + 防御处理 =====
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float multiplier = WeaponStage switch
            {
                1 => 1f,
                2 => 1.1f,
                3 => 1.25f,
                4 => 1.5f,
                5 => 1.65f,
                6 => 1.75f,
                >= 7 => 2f,
                _ => 1f
            };

            modifiers.SourceDamage *= multiplier;

            // Stage4+：无视防御与DR
            if (WeaponStage >= 4)
            {
                modifiers.DefenseEffectiveness *= 0f;
                modifiers.FinalDamage /= 1f - target.Calamity().DR;
            }

            // Stage7：附加最大生命【1‰】伤害
            if (WeaponStage >= 7)
            {
                modifiers.SourceDamage += target.lifeMax * 0.001f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnHitEffects(target);
        }

        public override void OnKill(int timeLeft)
        {
            // ===== Stage3：小爆炸 =====
            if (WeaponStage >= 3)
            {
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner
                );

                Projectile explosion = Main.projectile[explosionIndex];
                explosion.width = 25;
                explosion.height = 25;
            }

            // ===== Stage5：概率分裂 =====
            if (WeaponStage >= 5 && canSplit && Main.rand.NextBool(3))
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 velocity =
                        Projectile.velocity.RotatedByRandom(0.6f) *
                        Main.rand.NextFloat(0.7f, 1.1f);

                    int index = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        Type,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        1f // 标记为子弹（禁止再分裂）
                    );

                    if (Main.projectile[index].ModProjectile is SHPCRight_Proj p)
                    {
                        p.WeaponStage = WeaponStage;
                    }
                }
            }

            SpawnDeathEffects();
        }

        #region 特效

        private void SpawnFlightEffects()
        {
            int roll = Main.rand.Next(3);

            if (roll == 0)
            {
                // 电能Dust
                int lightDustCount = Main.rand.Next(1, 3);
                for (int i = 0; i < lightDustCount; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * (1f - Projectile.Opacity) * 18f;
                    Dust light = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.5f, 1f));
                    light.velocity = Main.rand.NextVector2Circular(4f, 4f);
                    light.scale = Main.rand.NextFloat(0.75f, 1.05f);
                    light.noGravity = true;
                }
            }
            else if (roll == 1)
            {
                // 辉光球
                Vector2 dir = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 position = Projectile.Center + dir * Main.rand.NextFloat(4f, 10f);
                Vector2 velocity = dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.5f, 2.2f);

                Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.25f, 0.85f));
                GlowOrbParticle glow = new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 0.9f),
                    glowColor,
                    true,
                    true
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }
            else
            {
                // 光芒粒子
                Vector2 velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
                float scale = Main.rand.NextFloat(0.2f, 0.4f);
                Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.7f));
                int lifetime = Main.rand.Next(10, 16);

                SquishyLightParticle particle = new(
                    Projectile.Center,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnHitEffects(NPC target)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            float baseAngle = back.ToRotation();

            // 扇形Dust
            int dustCount = 10;
            float fanHalfAngle = MathHelper.ToRadians(28f);
            for (int i = 0; i < dustCount; i++)
            {
                float t = dustCount == 1 ? 0.5f : i / (float)(dustCount - 1);
                float angle = baseAngle + MathHelper.Lerp(-fanHalfAngle, fanHalfAngle, t);
                float speed = MathHelper.Lerp(2.5f, 6.8f, 1f - Math.Abs(t - 0.5f) * 2f);

                Dust light = Dust.NewDustPerfect(target.Center, 267);
                light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.4f, 0.95f));
                light.velocity = angle.ToRotationVector2() * speed;
                light.scale = Main.rand.NextFloat(0.9f, 1.25f);
                light.noGravity = true;
            }

            // 扇形辉光球
            int orbCount = 4;
            for (int i = 0; i < orbCount; i++)
            {
                float t = orbCount == 1 ? 0.5f : i / (float)(orbCount - 1);
                float angle = baseAngle + MathHelper.Lerp(-fanHalfAngle, fanHalfAngle, t);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.6f, 3.8f);
                Vector2 position = target.Center + Main.rand.NextVector2Circular(4f, 4f);

                Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.3f, 0.85f));
                GlowOrbParticle glow = new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 0.9f),
                    glowColor,
                    true,
                    true
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }

            // 命中光粒子
            int lightCount = 3;
            for (int i = 0; i < lightCount; i++)
            {
                Vector2 velocity = back.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.8f, 2.2f);
                float scale = Main.rand.NextFloat(0.28f, 0.48f);
                Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f));
                int lifetime = Main.rand.Next(12, 18);

                SquishyLightParticle particle = new(
                    target.Center,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnDeathEffects()
        {
            Vector2 outwardBase = Main.rand.NextVector2Unit();

            // 扩散Dust
            int lightDustCount = 14;
            for (int i = 0; i < lightDustCount; i++)
            {
                Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0f, 10f);
                Dust light = Dust.NewDustPerfect(dustSpawnPosition, 267);
                light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.5f, 1f));
                light.velocity = Main.rand.NextVector2Circular(6f, 6f);
                light.scale = Main.rand.NextFloat(0.9f, 1.35f);
                light.noGravity = true;
            }

            // 扩散光芒粒子
            int lightCount = 6;
            for (int i = 0; i < lightCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 4.2f);
                float scale = Main.rand.NextFloat(0.25f, 0.55f);
                Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.25f, 0.85f));
                int lifetime = Main.rand.Next(14, 24);

                SquishyLightParticle particle = new(
                    Projectile.Center,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 扩散辉光球
            int orbCount = 5;
            for (int i = 0; i < orbCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.4f, 3.5f);
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(3f, 3f);
                Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f));

                GlowOrbParticle glow = new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 0.9f),
                    glowColor,
                    true,
                    true
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }

            //// 小冲击波
            //Particle pulse = new DirectionalPulseRing(
            //    Projectile.Center,
            //    Projectile.velocity * 0.75f,
            //    Color.Gold,
            //    new Vector2(1f, 2.5f),
            //    Projectile.rotation - MathHelper.PiOver4,
            //    0.2f,
            //    0.03f,
            //    20
            //);
            //GeneralParticleHandler.SpawnParticle(pulse);
        }

        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(WeaponStage);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            WeaponStage = reader.ReadInt32();
        }
    }
}