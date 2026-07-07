using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class BossSoulofMight_EXP : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 300;
            Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            // ===== 1. 光照（保持不变逻辑，但写法更干净）=====
            float lightFactor = Main.rand.NextFloat(0.9f, 1.1f) * Main.essScale;
            Lighting.AddLight(Projectile.Center, 0.45f * lightFactor, 0.7f * lightFactor, 1.9f * lightFactor);

            // ===== 2. 生命周期控制 =====
            float spawnCount = 14f;

            if (Projectile.ai[0] > 180f)
                spawnCount -= (Projectile.ai[0] - 180f) / 2f;

            if (spawnCount <= 0f)
            {
                Projectile.Kill();
                return;
            }

            spawnCount *= 1.15f;

            // Strong sustained lightning burst.
            spawnCount *= 1.2f;

            Projectile.ai[0] += 4f;

            // ===== 3. 噪声种子（让每一帧有结构变化，而不是纯随机）=====
            float noiseSeed = Projectile.identity * 0.137f + Projectile.ai[0] * 0.021f;

            // ===== 4. 粒子生成 =====
            int count = (int)spawnCount;
            for (int i = 0; i < count; i++)
            {
                // ===== 噪声角度（核心）=====
                float angleNoise = (float)Math.Sin(noiseSeed + i * 0.55f);
                float angle = MathHelper.TwoPi * (i / (float)count) + angleNoise * 0.6f;

                Vector2 dir = angle.ToRotationVector2();

                // ===== 半径（保持原本范围，但带一点波动）=====
                float radius = Main.rand.NextFloat(9f, 28f);

                // ===== 速度（保持原有强度）=====
                Vector2 velocity = dir * radius;

                // ===== 粒子类型（不变）=====
                int dustType = Main.rand.NextBool(3)
                    ? DustID.UltraBrightTorch
                    : DustID.Electric;

                int dustIndex = Dust.NewDust(
                    Projectile.Center,
                    0,
                    0,
                    dustType,
                    velocity.X,
                    velocity.Y,
                    100,
                    default,
                    2f
                );

                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true;
                dust.color = Main.rand.NextBool(3)
                    ? new Color(70, 130, 255)
                    : new Color(170, 220, 255);
                dust.scale *= Main.rand.NextFloat(0.82f, 1.22f);

                // ===== 中心轻微扩散（保持原感觉，但更干净）=====
                dust.position += Main.rand.NextVector2Circular(18f, 18f);
            }

            if (Projectile.ai[0] % 12f == 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 6f + Main.rand.NextFloat(-0.18f, 0.18f)).ToRotationVector2() * Main.rand.NextFloat(5f, 19f);
                    Dust arcDust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                        DustID.Electric,
                        velocity,
                        70,
                        Main.rand.NextBool() ? Color.White : new Color(90, 170, 255),
                        Main.rand.NextFloat(1.05f, 1.7f));
                    arcDust.noGravity = true;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300); // 原版的带电效果
            //target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 300); // 电偶腐蚀
        }
    }
}
