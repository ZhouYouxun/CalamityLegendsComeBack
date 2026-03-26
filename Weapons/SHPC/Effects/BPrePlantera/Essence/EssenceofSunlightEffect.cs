using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlightEffect : DefaultEffect
    {
        public override int EffectID => 7;

        public override int AmmoType => ModContent.ItemType<EssenceofSunlight>();

        // 日光金色
        public override Color ThemeColor => new Color(255, 220, 90);
        public override Color StartColor => new Color(255, 255, 160);
        public override Color EndColor => new Color(255, 180, 60);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 生命周期
            projectile.timeLeft = 160;

            // 初始化状态（保险）
            var gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();
            gp.chargeTimer = 0;
            gp.isCharging = false;
            gp.chargeDirection = Vector2.Zero;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();

            gp.chargeTimer++;

            // ================= 第一阶段：减速 + 锁敌 =================
            if (!gp.isCharging)
            {
                if (gp.chargeTimer >= 20)
                {
                    NPC target = projectile.Center.ClosestNPCAt(1600f);

                    if (target != null)
                    {
                        gp.chargeDirection = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);
                    }
                    else
                    {
                        // 没目标 → 瞄准鼠标
                        gp.chargeDirection = (Main.MouseWorld - projectile.Center).SafeNormalize(Vector2.UnitX);
                    }

                    // 🔥 瞬间爆发速度
                    projectile.velocity = gp.chargeDirection * 18f;
                    // 冲刺瞬间只触发一次反推特效
                    SpawnChargeBackEffect(projectile);
                    gp.isCharging = true;
                    gp.chargeTimer = 0;
                }
            }
            // ================= 第二阶段：冲锋 =================
            else
            {
                // 抵消默认减速
                projectile.velocity *= 1.05f;
                projectile.extraUpdates = 2;

            }
        }



        private void SpawnChargeBackEffect(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            // ================= 1.核心爆发（强闪） =================
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 dir = angle.ToRotationVector2();

                SquishyLightParticle core = new(
                    projectile.Center,
                    dir * Main.rand.NextFloat(2f, 6f),
                    Main.rand.NextFloat(1.2f, 1.8f),
                    Color.Lerp(new Color(255, 255, 180), new Color(255, 200, 80), Main.rand.NextFloat()),
                    18
                );

                GeneralParticleHandler.SpawnParticle(core);
            }

            // ================= 2.轴对称反推喷流（双翼结构） =================
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 sideOffset = forward.RotatedBy((MathHelper.Pi / 2f) * side);

                for (int i = 0; i < 6; i++)
                {
                    float t = i / 6f;

                    Vector2 velocity =
                        back * MathHelper.Lerp(4f, 10f, t) +
                        sideOffset * MathHelper.Lerp(0.5f, 2.5f, t);

                    SquishyLightParticle jet = new(
                        projectile.Center + sideOffset * 4f,
                        velocity,
                        MathHelper.Lerp(0.8f, 1.4f, 1f - t),
                        Color.Lerp(new Color(255, 255, 160), new Color(255, 180, 60), t),
                        16 + i * 2
                    );

                    GeneralParticleHandler.SpawnParticle(jet);
                }
            }

            // ================= 3.旋转光环（数学美感核心） =================
            for (int i = 0; i < 2; i++)
            {
                float rot = projectile.velocity.ToRotation() + (MathHelper.Pi / 2f) * i;

                Particle pulse = new DirectionalPulseRing(
                    projectile.Center,
                    back * 2f,
                    Color.Lerp(new Color(255, 255, 160), new Color(255, 200, 80), 0.5f),
                    new Vector2(1f, 3f),
                    rot,
                    0.25f,
                    0.02f,
                    24
                );

                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // ================= 4.主冲击波（大环） =================
            Particle mainPulse = new DirectionalPulseRing(
                projectile.Center,
                back * 3f,
                new Color(255, 230, 120),
                new Vector2(1f, 4f),
                projectile.rotation - (MathHelper.Pi / 4f),
                0.35f,
                0.015f,
                28
            );

            GeneralParticleHandler.SpawnParticle(mainPulse);
        }



        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            float radius = 50f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || !npc.CanBeChasedBy())
                    continue;

                if (Vector2.Distance(npc.Center, target.Center) <= radius)
                {
                    var gnpc = npc.GetGlobalNPC<EssenceofSunlight_GNPC>();

                    if (!gnpc.marked)
                    {
                        gnpc.marked = true;
                        gnpc.timer = 0;
                        gnpc.owner = projectile.owner;
                    }
                }
            }
        }
    }

    // ================= 每个弹幕独立状态 =================
    public class EssenceofSunlight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true; // ⭐ 必须

        public int chargeTimer;
        public bool isCharging;
        public Vector2 chargeDirection;
    }
}