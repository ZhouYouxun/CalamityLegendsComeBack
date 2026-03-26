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






            // ===== 视觉特效（玩家中心）=====
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(player.Center, 267);
                dust.velocity = Main.rand.NextVector2Circular(3f, 3f);
                dust.scale = 1f + stage * 0.1f;
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                SquishyLightParticle particle = new(
                    player.Center,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0.3f + stage * 0.05f,
                    Color.Lerp(Color.OrangeRed, Color.White, 0.5f),
                    12
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }



        }














    }
}