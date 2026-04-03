using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
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
                Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
                float fanHalfAngle = MathHelper.ToRadians(52f);
                float edgeCompression = 0.68f;

                // ================= 主体 Dust：巨大扇形龙息喷射 =================
                for (int i = 0; i < Main.rand.Next(150, 171); i++)
                {
                    float fanT = Main.rand.NextFloat(-1f, 1f);
                    float curvedT = (float)Math.Sin(fanT * MathHelper.PiOver2) * edgeCompression;
                    float angle = fanHalfAngle * curvedT;
                    float axialSpeed = Main.rand.NextFloat(18f, 46f);
                    float lateralSpeed = fanT * Main.rand.NextFloat(2.5f, 10.5f);
                    Vector2 spawnOffset = right * fanT * Main.rand.NextFloat(8f, 26f) + forward * Main.rand.NextFloat(0f, 10f);
                    Vector2 velocity = forward.RotatedBy(angle) * axialSpeed + right * lateralSpeed;

                    int dustType = Main.rand.Next(3) switch
                    {
                        0 => 6,
                        1 => 244,
                        _ => 31
                    };

                    //Dust dust = Dust.NewDustPerfect(
                    //    Projectile.Center + spawnOffset,
                    //    dustType,
                    //    velocity,
                    //    100,
                    //    default,
                    //    Main.rand.NextFloat(4.2f, 7.6f) * Main.rand.NextFloat(0.9f, 1.35f)
                    //);
                    //dust.noGravity = true;

                }

                // ================= CustomSpark：与主喷流同构的大型核心火束 =================
                for (int i = 0; i < 9; i++)
                {
                    float fanT = Main.rand.NextFloat(-1f, 1f);
                    float angle = fanHalfAngle * fanT * 0.72f;
                    Vector2 sparkVelocity = forward.RotatedBy(angle) * Main.rand.NextFloat(14f, 28f) + right * fanT * Main.rand.NextFloat(1.8f, 5.4f);
                    Vector2 sparkOffset = right * fanT * Main.rand.NextFloat(4f, 18f) + forward * Main.rand.NextFloat(4f, 14f);

                    Particle beamCore = new CustomSpark(
                        Projectile.Center + sparkOffset,
                        sparkVelocity,
                        "CalamityMod/Particles/SmallBloom",
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.26f, 0.42f),
                        Main.rand.NextBool(3) ? Color.OrangeRed : Color.Lerp(Color.Orange, Color.White, 0.45f),
                        new Vector2(Main.rand.NextFloat(1.6f, 2.5f), Main.rand.NextFloat(1.1f, 1.7f)),
                        true,
                        false,
                        0f,
                        false,
                        false,
                        0.5f
                    );
                    GeneralParticleHandler.SpawnParticle(beamCore);
                }

                // ================= SparkParticle：巨大扇面外缘火星 =================
                if (Main.rand.NextBool())
                {
                    for (int i = 0; i < 7; i++)
                    {
                        float fanT = Main.rand.NextFloat(-1f, 1f);
                        float angle = fanHalfAngle * fanT * 0.94f;
                        Vector2 sparkPos = Projectile.Center + right * fanT * Main.rand.NextFloat(10f, 24f) + forward * Main.rand.NextFloat(6f, 18f);
                        Vector2 sparkVel = forward.RotatedBy(angle) * Main.rand.NextFloat(12f, 24f) + right * fanT * Main.rand.NextFloat(2f, 7f);

                        SparkParticle spark = new SparkParticle(
                            sparkPos,
                            sparkVel,
                            false,
                            Main.rand.Next(14, 24),
                            Main.rand.NextFloat(0.8f, 1.35f),
                            Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed
                        );
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }

                // ================= GlowSparkParticle：巨大扇形细长炽焰丝 =================
                if (Main.rand.NextBool())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float fanT = Main.rand.NextFloat(-1f, 1f);
                        float angle = fanHalfAngle * fanT * 0.8f;
                        Vector2 glowOffset = right * fanT * Main.rand.NextFloat(6f, 20f) + forward * Main.rand.NextFloat(5f, 16f);
                        Vector2 glowVel = forward.RotatedBy(angle) * Main.rand.NextFloat(16f, 30f) + right * fanT * Main.rand.NextFloat(2.5f, 7.5f);

                        float glowScale = Main.rand.NextFloat(0.02f, 0.038f);
                        Particle glowSpark = new GlowSparkParticle(
                            Projectile.Center + glowOffset,
                            glowVel,
                            false,
                            Main.rand.Next(9, 14),
                            glowScale,
                            Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed,
                            new Vector2(Main.rand.NextFloat(3.2f, 4.6f), Main.rand.NextFloat(1.0f, 1.35f)),
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
            float shakePower = 5.5f; // 设置震动强度
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true); // 距离衰减
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);



            // 播放音效
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Weapons/SHPC/轨道炮攻击-仅开火")
                {
                    Volume = 1.1f,
                    Pitch = 0.1f
                },
                Projectile.Center
            );


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

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<FuckYou>(),
                (int)(Projectile.damage * 1.15f),
                Projectile.knockBack,
                Projectile.owner
            );


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
