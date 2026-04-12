using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class TitanHeartEffect : DefaultEffect
    {
        private const string GlowBladeTexture = "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade";

        public override int EffectID => 8;

        // 真实物品ID（不再占位）
        public override int AmmoType => ModContent.ItemType<TitanHeart>();

        // 深红主题（从图提取偏暗红）
        public override Color ThemeColor => new Color(180, 40, 40);
        public override Color StartColor => new Color(220, 60, 60);
        public override Color EndColor => new Color(120, 20, 20);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void SetDefaults(Projectile projectile)
        {
            //// ===== 基础强化 =====

            //// 放大体积（命中体感更强）
            //projectile.width = (int)(projectile.width * 1.6f);
            //projectile.height = (int)(projectile.height * 1.6f);

            //// 穿透大幅提升
            //projectile.penetrate = 5;

            //// 击退增强
            //projectile.knockBack *= 1.8f;

            //// 伤害削弱（平衡）
            //projectile.damage = (int)(projectile.damage * 0.55f);
        }



        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // ===== 放大体积 =====
            Vector2 center = projectile.Center;

            projectile.width = 250;
            projectile.height = 250;

            projectile.Center = center;

            // ===== 穿透（关键修复点）=====
            projectile.penetrate = 12;

            // ===== 击退增强 =====
            projectile.knockBack *= 1.8f;

            // ===== 伤害削弱 =====
            projectile.damage = (int)(projectile.damage * 0.55f);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 以弹幕中心为圆心，生成每隔 90 度分布的一圈旋转刀光。
            float spinPhase = Main.GlobalTimeWrappedHourly * 12f + projectile.identity * 0.37f;
            float orbitRadius = 11f;
            Color sparkColor = Color.Lerp(ThemeColor, StartColor, 0.35f) * 0.96f;





            for (int i = 0; i < 4; i++)
            {
                // =========================
                // 基础角度（四等分90°）
                // =========================
                float angle = spinPhase + MathHelper.PiOver2 * i;

                // =========================
                // 径向方向（决定位置 + 视觉朝向）
                // =========================
                Vector2 radialDirection = angle.ToRotationVector2();

                // =========================
                // 切线方向（决定旋转运动）
                // =========================
                Vector2 tangentialVelocity = radialDirection.RotatedBy(MathHelper.PiOver2) * 1.55f;

                // =========================
                // 生成位置（圆周上）
                // =========================
                Vector2 sparkCenter = projectile.Center + radialDirection * orbitRadius;

                // =========================
                // 最终速度（旋转 + 轻微跟随本体）
                // =========================
                Vector2 finalVelocity = tangentialVelocity + projectile.velocity * 0.03f;

                // =========================
                // 计算修正旋转（关键）
                // 👉 让贴图朝“径向”，而不是朝“速度”
                // =========================
                float extraRot = radialDirection.ToRotation() - finalVelocity.ToRotation();

                Particle customLine = new CustomSpark(
                    sparkCenter,
                    finalVelocity,
                    GlowBladeTexture,
                    false,
                    2,
                    0.16f,
                    sparkColor,
                    new Vector2(0.56f, 1.15f), // 👉 已压短
                    glowCenter: true,
                    shrinkSpeed: 1.2f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.72f,
                    extraRotation: extraRot
                );

                GeneralParticleHandler.SpawnParticle(customLine);
            }



        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 center = target.Center;

            // ================= 崩飞效果 =================
            if (target.CanBeMoved(true))
            {
                // 👉 方向：沿弹幕方向击飞
                Vector2 launchVel = projectile.velocity.SafeNormalize(Vector2.UnitX);

                // 👉 力度：原版30的80%
                float launchPower = 30f * 0.8f;

                target.noTileCollide = false;
                target.knockBackResist = 1f;

                target.MoveNPC(launchVel, launchPower * 0.5f, true);

                // ================= 屏幕震动 =================
                float shakePower = 10f;
                float distanceFactor = Utils.GetLerpValue(1000f, 0f, projectile.Distance(Main.LocalPlayer.Center), true);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                    Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);
            }





            // ================= 第一层：核心爆闪 =================
            for (int i = 0; i < 10; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });

                Dust d = Dust.NewDustPerfect(
                    center,
                    dustType,
                    Main.rand.NextVector2Circular(2f, 2f),
                    0,
                    default,
                    1.6f
                );

                d.noGravity = true;
                d.fadeIn = 1.6f;
            }

            // ================= 第二层：椭圆冲击环（主视觉） =================
            int ringCount = 32;

            for (int i = 0; i < ringCount; i++)
            {
                float t = (float)i / ringCount;
                float angle = MathHelper.TwoPi * t;

                // 椭圆（横向拉伸）
                Vector2 dir = new Vector2(
                    MathF.Cos(angle) * 2.2f,
                    MathF.Sin(angle)
                );

                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });

                Dust d = Dust.NewDustPerfect(
                    center,
                    dustType,
                    dir * Main.rand.NextFloat(6f, 12f),
                    0,
                    default,
                    1.3f
                );

                d.noGravity = true;
                d.fadeIn = 1.5f;
            }

            // ================= 第三层：旋转双环（数学美感） =================
            int spiralCount = 24;

            for (int i = 0; i < spiralCount; i++)
            {
                float t = i / (float)spiralCount;

                float angle1 = t * MathHelper.TwoPi + Main.rand.NextFloat(0.2f);
                float angle2 = angle1 + MathHelper.Pi;

                float radius = MathHelper.Lerp(2f, 12f, t);

                Vector2 pos1 = center + angle1.ToRotationVector2() * radius * 6f;
                Vector2 pos2 = center + angle2.ToRotationVector2() * radius * 6f;

                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });

                Dust d1 = Dust.NewDustPerfect(pos1, dustType, (pos1 - center).SafeNormalize(Vector2.Zero) * 4f, 0, default, 1.1f);
                Dust d2 = Dust.NewDustPerfect(pos2, dustType, (pos2 - center).SafeNormalize(Vector2.Zero) * 4f, 0, default, 1.1f);

                d1.noGravity = true;
                d2.noGravity = true;
            }

            // ================= 第四层：前向冲击流（打击方向感） =================
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 18; i++)
            {
                Vector2 vel =
                    forward.RotatedByRandom(MathHelper.ToRadians(12f))
                    * Main.rand.NextFloat(8f, 16f);

                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });

                Dust d = Dust.NewDustPerfect(
                    center,
                    dustType,
                    vel,
                    0,
                    default,
                    1.4f
                );

                d.noGravity = true;
                d.fadeIn = 1.6f;
            }

            // ================= 第五层：反向吸附流（对冲） =================
            for (int i = 0; i < 14; i++)
            {
                Vector2 pos = center + Main.rand.NextVector2Circular(80f, 80f);

                Vector2 vel = (center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(6f, 12f);

                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });

                Dust d = Dust.NewDustPerfect(
                    pos,
                    dustType,
                    vel,
                    0,
                    default,
                    1.2f
                );

                d.noGravity = true;
            }
        }




    }
}
