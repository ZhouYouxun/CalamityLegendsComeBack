using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    public class SHPCRight_DeBuff : ModBuff
    {
        // 1 = 普通过热，2 = 高烈度过热
        public static int FireMode = 1;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int stage = player.GetModPlayer<SHPCRight_Player>().HeatStage;

            if (stage <= 0)
                return;

            // ===== Stage5 =====
            if (stage == 5)
            {
                if (player.statLife > 100)
                {
                    if (Main.GameUpdateCount % 4 == 0)
                        player.statLife--;
                }
            }

            // ===== Stage6 =====
            else if (stage == 6)
            {
                if (player.statLife > 100)
                {
                    if (Main.GameUpdateCount % 2 == 0)
                        player.statLife--;
                }
            }

            // ===== Stage7 =====
            else if (stage >= 7)
            {
                if (Main.GameUpdateCount % 1 == 0)
                    player.statLife -= Main.rand.Next(1, 3);

                // 搞笑文本
                if (Main.rand.NextBool(12))
                {
                    Vector2 pos = player.Center;

                    CombatText.NewText(
                        new Rectangle((int)pos.X, (int)pos.Y, player.width, player.height),
                        Color.Red,
                        "锟斤拷烫烫烫！！！"
                    );
                }
            }


            ApplyBurningVisual(player, stage);

        }

        private void ApplyBurningVisual(Player player, int stage)
        {
            if (stage < 5)
                return;

            // ===== 基础燃烧（始终存在）=====
            ApplyStage5Burn(player);

            // ===== Stage6 叠加 =====
            if (stage >= 6)
            {
                // TODO: Stage6 特效
            }

            // ===== Stage7 叠加 =====
            if (stage >= 7)
            {
                // TODO: Stage7 特效
            }
        }

        private void ApplyStage5Burn(Player player)
        {
            Vector2 center = player.Center;

            // ===== 从身体范围随机取点 =====
            Vector2 randPos = center + new Vector2(
                Main.rand.NextFloat(-player.width * 0.4f, player.width * 0.4f),
                Main.rand.NextFloat(-player.height * 0.5f, player.height * 0.5f)
            );

            // ===== 向上 + 旋转（火焰感）=====
            Vector2 upward = new Vector2(
                Main.rand.NextFloat(-0.6f, 0.6f),
                Main.rand.NextFloat(-2.5f, -1.2f)
            );

            // ===== ① 火焰主体（Squishy）=====
            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle flame = new(
                    randPos,
                    upward.RotatedByRandom(0.4f),
                    Main.rand.NextFloat(0.35f, 0.6f),
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat(0.3f, 0.8f)),
                    Main.rand.Next(12, 20),
                    1f,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );

                GeneralParticleHandler.SpawnParticle(flame);
            }

            // ===== ② 火星（Dust）=====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    randPos,
                    DustID.Torch,
                    upward.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.8f, 2.2f),
                    0,
                    Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.6f)
                );
                d.noGravity = true;
            }

            // ===== ③ 慌张感：微爆闪 =====
            if (Main.rand.NextBool(4))
            {
                PointParticle spark = new PointParticle(
                    randPos,
                    upward * 0.5f,
                    false,
                    10,
                    1.1f,
                    Color.OrangeRed
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }










    }
}