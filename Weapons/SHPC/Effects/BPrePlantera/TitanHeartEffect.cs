using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class TitanHeartEffect : DefaultEffect
    {
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

            projectile.width = (int)(projectile.width * 1.6f);
            projectile.height = (int)(projectile.height * 1.6f);

            projectile.Center = center;

            // ===== 穿透（关键修复点）=====
            projectile.penetrate = 12;

            // ===== 击退增强 =====
            projectile.knockBack *= 1.8f;

            // ===== 伤害削弱 =====
            projectile.damage = (int)(projectile.damage * 0.55f);
        }




        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 center = target.Center;

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