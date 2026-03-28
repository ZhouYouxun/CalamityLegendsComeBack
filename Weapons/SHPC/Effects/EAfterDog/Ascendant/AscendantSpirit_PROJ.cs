using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpirit_PROJ : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color currentColor = Color.Black;

        // ===== 数学时间参数 =====
        private float t;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 500;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10 * Projectile.extraUpdates;
            Projectile.alpha = 255;
            Projectile.ignoreWater = true;
            AIType = ProjectileID.Bullet;
        }

        // ================= OnSpawn =================
        public override void OnSpawn(IEntitySource source)
        {
            // 四种颜色池
            Color[] palette =
            {
                new Color(255, 140, 40),   // 赤黄
                //new Color(10, 10, 10),     // 极黑
                new Color(80, 200, 255),   // 亮蓝
                new Color(255, 120, 200)   // 粉色
            };

            currentColor = palette[Main.rand.Next(palette.Length)];

            Projectile.scale = 0.025f;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            t += 0.15f; // 时间推进（控制数学节奏）

            // ===== 数学优雅粒子释放 =====
            // 使用 sin 控制“呼吸式频率”
            float emissionFactor = 0.5f + 0.5f * (float)Math.Sin(t * 0.8f);

            // 控制是否释放（非纯随机）
            if (Main.rand.NextFloat() < emissionFactor)
            {
                Vector2 spawnPos = Projectile.Center;

                // 速度基础：沿当前方向 + 微扰
                Vector2 baseVel = Projectile.velocity.SafeNormalize(Vector2.UnitY);

                // 加一点正交波动（数学感关键）
                Vector2 ortho = baseVel.RotatedBy(MathHelper.PiOver2);
                float wave = (float)Math.Sin(t * 2f);

                Vector2 velocity =
                    baseVel * Main.rand.NextFloat(2f, 5f) +
                    ortho * wave * 0.8f;

                Particle spark = new CustomSpark(
                    spawnPos,
                    velocity,
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.85f, 1.2f),
                    Color.Lerp(currentColor, Color.White, 0.5f + 0.5f * (float)Math.Sin(t)),
                    new Vector2(Main.rand.NextFloat(1.1f, 1.45f), Main.rand.NextFloat(0.28f, 0.5f)),
                    true,
                    false,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false,
                    false,
                    0.08f
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int b = 0; b < 2; b++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowTorch,
                    new Vector2(2, 2).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.5f)
                );

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.5f, 1.1f);
                dust.color = currentColor;

                GlowOrbParticle orb = new GlowOrbParticle(
                    Projectile.Center,
                    new Vector2(2, 2).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.5f),
                    false,
                    5,
                    Main.rand.NextFloat(0.35f, 0.45f),
                    currentColor,
                    true,
                    true
                );

                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 12, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;
            Texture2D texture2 = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            // ===== 使用当前颜色 =====
            CalamityUtils.DrawAfterimagesCentered(
                Projectile,
                ProjectileID.Sets.TrailingMode[Projectile.type],
                Color.Lerp(currentColor, Color.White, 0.15f),
                1,
                texture
            );

            CalamityUtils.DrawAfterimagesCentered(
                Projectile,
                ProjectileID.Sets.TrailingMode[Projectile.type],
                currentColor with { A = 0 },
                1,
                texture2
            );

            return false;
        }

        // ===== 保持默认 =====
        public override bool? CanDamage() => Projectile.localAI[0] < 20 ? false : null;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) =>
            modifiers.SourceDamage.Flat += 0f;
    }
}