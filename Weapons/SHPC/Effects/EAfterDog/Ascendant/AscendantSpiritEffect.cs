using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpiritEffect : DefaultEffect
    {
        public override int EffectID => 36;

        public override int AmmoType => ModContent.ItemType<AscendantSpiritEssence>();

        public override Color ThemeColor => new Color(120, 160, 255);   // 主体蓝紫发光
        public override Color StartColor => new Color(200, 220, 255);   // 核心偏白高光
        public override Color EndColor => new Color(40, 60, 120);       // 尾部深蓝收束
        public override float SquishyLightParticleFactor => 2f;
        public override float ExplosionPulseFactor => 2f;

        // ===== 发射计时 =====
        private int shootTimer = 0;
        private int intervalReduceTimer = 0;

        // ===== 可调参数 =====
        private int shootInterval = 8;       // 初始射击间隔
        private int minShootInterval = 4;    // 最低射击间隔
        private int reduceStepTime = 24;     // 每隔多少帧降低一次间隔
        private float turnSpeed = 0.035f;    // 每帧最大转向速度（弧度）
        private float detectRange = 800f;    // 索敌范围

        // ===== 自定义状态 =====
        private Vector2 initialVelocity = Vector2.Zero; // 记录弹幕初始速度
        private float facingRotation = 0f;              // 当前朝向角

        private float spreadAngle = MathHelper.Pi / 4f; // 初始±45°
        private float minSpreadAngle = MathHelper.ToRadians(1f); // 最终±1°

        private float squareHalfSize = 20f * 16f; // 方形半边长（你可以改）
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 先拷贝一份初始速度
            initialVelocity = projectile.velocity * 0.5f;

            // 朝向也基于这份初始速度
            if (initialVelocity != Vector2.Zero)
                facingRotation = initialVelocity.ToRotation();
            else
                facingRotation = 0f;

            projectile.velocity *= 0.75f;

            projectile.penetrate = -1;
            projectile.timeLeft *= 5;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 射击间隔逐渐降低 =====
            intervalReduceTimer++;
            if (intervalReduceTimer >= reduceStepTime)
            {
                intervalReduceTimer = 0;
                if (shootInterval > minShootInterval)
                    shootInterval--;
            }

            // ===== 索敌 =====
            NPC target = null;
            float nearestDistance = detectRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.CanBeChasedBy())
                {
                    float distance = Vector2.Distance(projectile.Center, npc.Center);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        target = npc;
                    }
                }
            }

            // ===== 慢慢转向目标 =====
            if (target != null)
            {
                Vector2 toTarget = target.Center - projectile.Center;
                if (toTarget != Vector2.Zero)
                {
                    float targetRotation = toTarget.ToRotation();

                    // 关键：取最短角差，这样目标从左瞬间跳右时，会立刻切换右拐
                    float angleDifference = MathHelper.WrapAngle(targetRotation - facingRotation);

                    // 每帧只转一点点，但方向实时切换
                    angleDifference = MathHelper.Clamp(angleDifference, -turnSpeed, turnSpeed);
                    facingRotation += angleDifference;
                }
            }

            if (intervalReduceTimer == 0 && spreadAngle > minSpreadAngle)
            {
                spreadAngle *= 0.9f; // 收缩倍率（你可以改）
                if (spreadAngle < minSpreadAngle)
                    spreadAngle = minSpreadAngle;
            }

            // ===== 方形四边 → 向中心射击 =====
            shootTimer++;
            if (shootTimer >= shootInterval)
            {
                shootTimer = 0;

                float speed = initialVelocity.Length();
                Vector2 center = projectile.Center;

                // ===== 上边（往下）=====
                float xTop = Main.rand.NextFloat(-squareHalfSize, squareHalfSize);
                Vector2 spawnTop = center + new Vector2(xTop, -squareHalfSize);
                Vector2 velTop = Vector2.UnitY * speed;

                // ===== 下边（往上）=====
                float xBottom = Main.rand.NextFloat(-squareHalfSize, squareHalfSize);
                Vector2 spawnBottom = center + new Vector2(xBottom, squareHalfSize);
                Vector2 velBottom = -Vector2.UnitY * speed;

                // ===== 左边（往右）=====
                float yLeft = Main.rand.NextFloat(-squareHalfSize, squareHalfSize);
                Vector2 spawnLeft = center + new Vector2(-squareHalfSize, yLeft);
                Vector2 velLeft = Vector2.UnitX * speed;

                // ===== 右边（往左）=====
                float yRight = Main.rand.NextFloat(-squareHalfSize, squareHalfSize);
                Vector2 spawnRight = center + new Vector2(squareHalfSize, yRight);
                Vector2 velRight = -Vector2.UnitX * speed;

                // 发射四个
                Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnTop, velTop, ModContent.ProjectileType<AscendantSpirit_PROJ>(), (int)(projectile.damage * 1.5f), projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnBottom, velBottom, ModContent.ProjectileType<AscendantSpirit_PROJ>(), (int)(projectile.damage * 1.5f), projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnLeft, velLeft, ModContent.ProjectileType<AscendantSpirit_PROJ>(), (int)(projectile.damage * 1.5f), projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnRight, velRight, ModContent.ProjectileType<AscendantSpirit_PROJ>(), (int)(projectile.damage * 1.5f), projectile.knockBack, projectile.owner);
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 0.5f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }
    }
}