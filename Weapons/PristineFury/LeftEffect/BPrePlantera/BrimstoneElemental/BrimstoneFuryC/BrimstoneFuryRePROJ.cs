using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Particles;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Typeless;
using CalamityRangerExpansion.Content.BOWChange;

namespace CalamityRangerExpansion.Content.BOWChange.BPrePlantera.BrimstoneFuryC
{
    internal class BrimstoneFuryRePROJ : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";
        private int hitCounter = 0; // 命中计数器

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.arrow = true;
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
        }

        public override void AI()
        {
            // 保持弹幕与方向一致
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) + MathHelper.ToRadians(90) * Projectile.direction;

            // 生成飞行轨迹的粒子特效
            if (Main.rand.NextBool(2))
            {
                Vector2 pointOnEdge = Projectile.Center;
                Particle trail = new SparkParticle(pointOnEdge, Projectile.velocity * 0.5f, false, 60, Main.rand.NextFloat(0.8f, 1.2f), Color.IndianRed);
                GeneralParticleHandler.SpawnParticle(trail);
            }

            BowChangeVFX.SpawnTrail(Projectile, BowChangeTheme.Brimstone, 0.7f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 每次命中弹幕被弹开
            Vector2 reflectDirection = Vector2.Reflect(Projectile.velocity, Vector2.Normalize(target.Center - Projectile.Center));
            Projectile.velocity = reflectDirection;

            // 添加硫磺火 Buff
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
            BowChangeVFX.SpawnImpact(Projectile, BowChangeTheme.Brimstone, 0.95f);

            if (Projectile.owner == Main.myPlayer)
            {
                int needleCount = hitCounter >= 8 ? 5 : 3;
                Vector2 baseDir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                for (int i = 0; i < needleCount; i++)
                {
                    Vector2 vel = baseDir.RotatedBy(Main.rand.NextFloat(-0.7f, 0.7f)) * Main.rand.NextFloat(7f, 11f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        vel,
                        ModContent.ProjectileType<BrimstoneFuryReCinderNeedle>(),
                        Math.Max(1, (int)(Projectile.damage * 0.38f)),
                        Projectile.knockBack * 0.35f,
                        Projectile.owner);
                }
            }

            // 在原地生成重型烟雾粒子特效
            for (int i = 0; i < 10; i++) // 粒子数量
            {
                Vector2 spawnPosition = Projectile.Center;
                Vector2 smokeVelocity = Main.rand.NextVector2Circular(1f, 1f); // 随机方向
                Color smokeColor = Main.rand.Next(new[] { Color.Red, Color.DarkRed, Color.Magenta, Color.Gray, Color.Black });
                float smokeLifetime = 30 + Main.rand.Next(30);

                Particle smoke = new HeavySmokeParticle(
                    spawnPosition,
                    smokeVelocity,
                    smokeColor,
                    (int)smokeLifetime,
                    Projectile.scale * Main.rand.NextFloat(0.7f, 1.3f),
                    1.0f,
                    MathHelper.ToRadians(2f),
                    true
                );

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // 计数命中次数
            hitCounter++;
            if (hitCounter >= 10) // 每 10 次生成大爆炸
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BrimlanceHellfireExplosion>(),
                    (int)(Projectile.damage * 2.5f), // 伤害倍率 2.5
                    Projectile.knockBack,
                    Projectile.owner
                );
                hitCounter = 0; // 重置计数
            }
            else // 其他 9 次生成小爆炸
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FuckYou>(),
                    (int)(Projectile.damage * 1.0f), // 伤害倍率 1.0
                    Projectile.knockBack,
                    Projectile.owner
                );
            }
        }


        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BrimlanceHellfireExplosion>(), (int)(Projectile.damage * 1.5f), Projectile.knockBack, Projectile.owner);
        }
    }
}
