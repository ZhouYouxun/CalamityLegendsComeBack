using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 天理真和 Buff：终极技期间的防御、免疫、伤害和回复加成。
    internal sealed class AzureThunderHarmonyBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            // 终极状态不保存到存档，死亡/重进后不应保留。
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 切走青霆剑立刻移除终极 Buff，避免其他武器继承收益。
            if (player.HeldItem?.type != ModContent.ItemType<AzureThunder>())
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.statDefense += 9;
            player.endurance += 0.09f;
            player.buffImmune[BuffID.ManaSickness] = true;

            // 35% 通用伤害倍率只服务青霆剑终极状态，换武器上面的判定会立刻取消。
            player.GetDamage(DamageClass.Generic) *= 1.35f;

            // 终极状态压制负生命回复，并额外提供稳定回血。
            player.lifeRegenTime += 2;
            if (player.lifeRegen < 0)
                player.lifeRegen = 0;
            player.lifeRegen += 18;

            // 玩家身上常驻淡金光，提示终极状态仍在持续。
            Lighting.AddLight(player.Center, AzureThunderColors.PaleYellow.ToVector3() * 0.45f);
        }
    }
}
