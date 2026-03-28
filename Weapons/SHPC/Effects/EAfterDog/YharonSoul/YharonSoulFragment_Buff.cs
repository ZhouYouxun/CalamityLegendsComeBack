using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul
{
    internal class YharonSoulFragment_Buff : ModBuff
    {
        public override string Texture => "Terraria/Images/Projectile_0"; // 透明占位

        public override void SetStaticDefaults()
        {
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 以敌人中心略偏上的位置作为燃烧核心点
            Vector2 emitCenter = npc.Center + new Vector2(0f, -npc.height * 0.2f);

            // ================= SmallSmokeParticle =================
            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 2; i++)
                {
                    // 平均向上，并带少量左右散行
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(18f)) * Main.rand.NextFloat(0.8f, 2.6f);

                    SmallSmokeParticle smoke = new SmallSmokeParticle(
                        emitCenter + Main.rand.NextVector2Circular(npc.width * 0.18f, npc.height * 0.12f),
                        smokeVelocity,
                        Color.DimGray,
                        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                        Main.rand.NextFloat(0.35f, 0.7f), // 整体缩小
                        Main.rand.Next(45, 75)
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

            // ================= Dust：6 / 244 / 31 =================
            if (Main.rand.NextBool())
            {
                int dustCount = Main.rand.Next(3, 6);
                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = Main.rand.Next(3) switch
                    {
                        0 => 6,
                        1 => 244,
                        _ => 31
                    };

                    Vector2 dustVelocity = -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(22f)) * Main.rand.NextFloat(0.6f, 2.8f);

                    Color dustColor = dustType == 31
                        ? Color.Gray
                        : (Main.rand.NextBool() ? Color.Orange : Color.OrangeRed);

                    Dust dust = Dust.NewDustPerfect(
                        emitCenter + Main.rand.NextVector2Circular(npc.width * 0.22f, npc.height * 0.14f),
                        dustType,
                        dustVelocity,
                        100,
                        dustColor,
                        Main.rand.NextFloat(0.75f, 1.35f) // 小一些
                    );
                    dust.noGravity = true;
                }
            }

            // ================= SparkParticle =================
            if (Main.rand.NextBool(3))
            {
                int sparkCount = Main.rand.Next(1, 3);
                for (int i = 0; i < sparkCount; i++)
                {
                    Vector2 sparkVelocity = -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(20f)) * Main.rand.NextFloat(1.8f, 5.2f);

                    SparkParticle spark = new SparkParticle(
                        emitCenter + Main.rand.NextVector2Circular(npc.width * 0.14f, npc.height * 0.1f),
                        sparkVelocity,
                        false,
                        Main.rand.Next(12, 20),
                        Main.rand.NextFloat(0.45f, 0.8f), // 小一些
                        Main.rand.NextBool(4) ? Color.OrangeRed : Color.Orange
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
        }






    }
}