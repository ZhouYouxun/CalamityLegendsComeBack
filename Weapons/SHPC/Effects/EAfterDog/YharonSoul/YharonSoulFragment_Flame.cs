using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul
{
    internal class YharonSoulFragment_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj"; // 使用透明贴图

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 300;
            Projectile.friendly = true; // 友方投射物
            Projectile.ignoreWater = true; // 无视水
            Projectile.tileCollide = false; // 不与地形碰撞
            Projectile.DamageType = DamageClass.Magic; // 伤害
            Projectile.penetrate = -1; // 无限穿透
            Projectile.MaxUpdates = 10; // 额外更新次数
            Projectile.timeLeft = 60; // 生命周期
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        private Vector2? fixedMouseDirection; // 存储固定的鼠标方向
        public override void AI()
        {
            Projectile.velocity *= 1f;

            // 第一次运行时记录鼠标方向
            if (fixedMouseDirection == null)
            {
                fixedMouseDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX); // 从弹幕指向鼠标的方向
            }



            {
                Vector2 forward = fixedMouseDirection.Value;

                // ================= 主体 Dust：龙息喷射 =================
                for (int i = 0; i < Main.rand.Next(150, 171); i++)
                {
                    float angle = MathHelper.ToRadians(-45f + Main.rand.NextFloat(90f)); // 主喷射扇形
                    Vector2 velocity = forward.RotatedBy(angle) * Main.rand.NextFloat(10f, 40f);

                    int dustType = Main.rand.Next(3) switch
                    {
                        0 => 6,   // 火焰
                        1 => 244, // 高亮火焰
                        _ => 31   // 烟雾
                    };

                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        dustType,
                        velocity,
                        100,
                        default,
                        Main.rand.NextFloat(3.5f, 6.0f) * Main.rand.NextFloat(0.8f, 1.35f)
                    );
                    dust.noGravity = true;
                }

                // ================= CustomSpark：辅助高亮核心喷流 =================
                for (int i = 0; i < 4; i++)
                {
                    Vector2 sparkVelocity = forward.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.9f, 2.2f);

                    Particle beamCore = new CustomSpark(
                        Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        sparkVelocity,
                        "CalamityMod/Particles/SmallBloom",
                        false,
                        10,
                        Main.rand.NextFloat(0.12f, 0.22f),
                        Main.rand.NextBool(3) ? Color.OrangeRed : Color.Lerp(Color.Orange, Color.White, 0.3f),
                        new Vector2(1f, 1f),
                        true,
                        false,
                        0f,
                        false,
                        false,
                        0.5f
                    );
                    GeneralParticleHandler.SpawnParticle(beamCore);
                }

                // ================= SparkParticle：前方火星 =================
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 sparkPos = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                        Vector2 sparkVel = forward.RotatedByRandom(0.45f) * Main.rand.NextFloat(4f, 12f);

                        SparkParticle spark = new SparkParticle(
                            sparkPos,
                            sparkVel,
                            false,
                            Main.rand.Next(12, 20),
                            Main.rand.NextFloat(0.5f, 0.9f),
                            Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed
                        );
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }

                // ================= GlowSparkParticle：细长亮火丝 =================
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 glowVel = forward.RotatedByRandom(0.28f) * Main.rand.NextFloat(5f, 14f);

                        float glowScale = Main.rand.NextFloat(0.0075f, 0.014f) * 2f;
                        Particle glowSpark = new GlowSparkParticle(
                            Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                            glowVel,
                            false,
                            8,
                            glowScale,
                            Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed,
                            new Vector2(2f, 1f),
                            true,
                            false,
                            1.3f
                        );
                        GeneralParticleHandler.SpawnParticle(glowSpark);
                    }
                }

                //// ================= SmallSmokeParticle：辅助黑灰烟 =================
                //for (int i = 0; i < 3; i++)
                //{
                //    Vector2 smokeVel = forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(3f, 10f);
                //    float smokeScale = Main.rand.NextFloat(0.45f, 1.1f);

                //    SmallSmokeParticle smoke = new SmallSmokeParticle(
                //        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                //        smokeVel,
                //        Color.DimGray,
                //        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                //        smokeScale,
                //        100
                //    );
                //    GeneralParticleHandler.SpawnParticle(smoke);
                //}
            }

            // 实时更新鼠标位置的喷射逻辑虽然很帅，但是不太推荐
            //{
            //    // 喷射烟雾粒子
            //    Vector2 directionToMouse = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX); // 从弹幕指向鼠标的方向
            //    for (int i = 0; i < Main.rand.Next(150, 171); i++)
            //    {
            //        float angle = MathHelper.ToRadians(-45 + Main.rand.NextFloat(90)); // 随机角度范围 -45 至 45
            //        Vector2 velocity = directionToMouse.RotatedBy(angle) * Main.rand.NextFloat(10f, 40f); // 高速粒子
            //        int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.Smoke; // Torch 和 Smoke 随机选择
            //        Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 100, default, Main.rand.NextFloat(3.5f, 6.0f)).noGravity = true;
            //    }
            //}
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 屏幕震动效果
            float shakePower = 1.5f; // 设置震动强度
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true); // 距离衰减
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);



            base.OnSpawn(source);
        }


        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int stack = target.GetGlobalNPC<YharonSoulFragment_GN>().stack;
            float mult = 1f + stack; // 每层+1倍
            modifiers.SourceDamage *= mult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 全部Debuff无条件
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.CursedInferno, 300);
            target.AddBuff(BuffID.Daybreak, 300);
            target.AddBuff(ModContent.BuffType<ElementalMix>(), 300);
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 300);
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);

            // 叠层
            var gn = target.GetGlobalNPC<YharonSoulFragment_GN>();
            gn.stack++;
            if (gn.stack > 4)
                gn.stack = 4;

            target.AddBuff(ModContent.BuffType<YharonSoulFragment_Buff>(), 300);











            // ================= 命中特效：向上方约5度扇形猛烈喷射 =================
            Vector2 upwardForward = (-Vector2.UnitY).RotatedBy(MathHelper.ToRadians(5f));

            // 主轴 Dust：6 / 244 / 31
            for (int i = 0; i < 90; i++)
            {
                float angle = MathHelper.ToRadians(-32f + Main.rand.NextFloat(64f));
                Vector2 velocity = upwardForward.RotatedBy(angle) * Main.rand.NextFloat(8f, 22f);

                int dustType = Main.rand.Next(3) switch
                {
                    0 => 6,
                    1 => 244,
                    _ => 31
                };

                Color dustColor = dustType == 31
                    ? Color.Gray
                    : (Main.rand.NextBool() ? Color.Orange : Color.OrangeRed);

                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(14f, 10f),
                    dustType,
                    velocity,
                    100,
                    dustColor,
                    Main.rand.NextFloat(2.1f, 4.2f)
                );
                dust.noGravity = true;
            }

            // SmallSmokeParticle：上喷黑灰烟
            for (int i = 0; i < 10; i++)
            {
                Vector2 smokeVel = upwardForward.RotatedByRandom(0.42f) * Main.rand.NextFloat(3f, 12f);
                float smokeScale = Main.rand.NextFloat(0.65f, 1.45f);

                SmallSmokeParticle smoke = new SmallSmokeParticle(
                    target.Center + Main.rand.NextVector2Circular(18f, 12f),
                    smokeVel,
                    Color.DimGray,
                    Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                    smokeScale,
                    100
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // SparkParticle：上喷火星
            for (int i = 0; i < 12; i++)
            {
                Vector2 sparkVel = upwardForward.RotatedByRandom(0.3f) * Main.rand.NextFloat(10f, 26f);

                SparkParticle spark = new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    sparkVel,
                    true,
                    Main.rand.Next(20, 34),
                    Main.rand.NextFloat(0.85f, 1.45f),
                    Main.rand.NextBool(4) ? Color.OrangeRed : Color.Orange
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }


        }
    









    }
}
