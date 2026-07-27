using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Buffs
{
    /// <summary>
    /// 蚀痕（Umbral Corrosion）——冥蚀天底在敌人身上累积的可视标记。
    /// 具体层数存储在 <see cref="General.UmbralCorrosionGlobalNPC"/>；本 Debuff 仅提供图标与"被标记"状态显示。
    /// </summary>
    public class UmbralCorrosion : ModBuff
    {
        // 复用物品贴图作图标，避免额外美术依赖
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadir";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
