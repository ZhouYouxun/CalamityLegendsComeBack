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
        public new string LocalizationCategory => "Projectiles.NewWeapons.BPrePlantera";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 500;
            Projectile.height = 500;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            // ===== 1. 光照（保持不变逻辑，但写法更干净）=====
            float lightFactor = Main.rand.NextFloat(0.9f, 1.1f) * Main.essScale;
            Lighting.AddLight(Projectile.Center, 5f * lightFactor, 1f * lightFactor, 4f * lightFactor);

            // ===== 2. 生命周期控制 =====
            float spawnCount = 25f;

            if (Projectile.ai[0] > 180f)
                spawnCount -= (Projectile.ai[0] - 180f) / 2f;

            if (spawnCount <= 0f)
            {
                Projectile.Kill();
                return;
            }

            spawnCount *= 0.7f;

            // ❗ 粒子量减少X0%
            spawnCount *= 0.5f;

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
                float radius = Main.rand.NextFloat(12f, 36f);

                // ===== 速度（保持原有强度）=====
                Vector2 velocity = dir * radius;

                // ===== 粒子类型（不变）=====
                int dustType = Main.rand.NextBool()
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

                // ===== 中心轻微扩散（保持原感觉，但更干净）=====
                dust.position += Main.rand.NextVector2Circular(10f, 10f);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300); // 原版的带电效果
            //target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 300); // 电偶腐蚀
        }
    }
}
