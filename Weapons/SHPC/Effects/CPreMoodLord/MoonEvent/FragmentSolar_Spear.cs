using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolar_Spear : ModProjectile
    {
        // ===== 自定义计数器（禁止用localAI）=====
        private int hitCount;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = 2; // 穿透两次
            Projectile.timeLeft = 180;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            Projectile.DamageType = DamageClass.Ranged;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.extraUpdates = 1;
        }

        // ================= OnSpawn =================
        public override void OnSpawn(IEntitySource source)
        {
        }

        // ================= AI =================
        public override void AI()
        {
            // 基础旋转（长矛感）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // ===== 日耀飞行光效 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 dir = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.SolarFlare,
                    dir * Main.rand.NextFloat(2f, 6f),
                    0,
                    default,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );
                dust.noGravity = true;
            }

            // 微量橙色火焰补层
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    Color.Orange,
                    Main.rand.NextFloat(1.0f, 1.5f)
                );
                d.noGravity = true;
            }

            // 微加速（有一点“日耀冲刺感”）
            Projectile.velocity *= 1.01f;
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;

            // ===== 日耀爆炸（每次命中触发）=====
            SpawnSolarExplosion();

            // 达到穿透次数后强制结束
            if (hitCount >= 2)
            {
                Projectile.Kill();
            }
        }

        // ================= OnKill =================
        public override void OnKill(int timeLeft)
        {
            // 死亡也补一个爆炸
            SpawnSolarExplosion();
        }

        // ================= TileCollide =================
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        // ================= 爆炸特效 =================
        private void SpawnSolarExplosion()
        {
            Vector2 center = Projectile.Center;

            // ===== 第一层：规则环（16向）=====
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 dir = angle.ToRotationVector2();

                Dust d = Dust.NewDustPerfect(
                    center + dir * 6f,
                    DustID.Torch,
                    dir * 4f,
                    0,
                    Color.Orange,
                    1.6f
                );
                d.noGravity = true;
            }

            // ===== 第二层：SolarFlare 爆散 =====
            for (int i = 0; i < 24; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.SolarFlare,
                    Main.rand.NextVector2Circular(6f, 6f),
                    0,
                    default,
                    Main.rand.NextFloat(1.5f, 2.3f)
                );
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }

            // ===== 第三层：冲击火花 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f);

                Dust spark = Dust.NewDustPerfect(
                    center,
                    DustID.Torch,
                    vel,
                    0,
                    Color.Yellow,
                    1.2f
                );
                spark.noGravity = true;
            }

            // ===== 局部光照 =====
            Lighting.AddLight(center, Color.Orange.ToVector3() * 0.8f);
        }
    }
}