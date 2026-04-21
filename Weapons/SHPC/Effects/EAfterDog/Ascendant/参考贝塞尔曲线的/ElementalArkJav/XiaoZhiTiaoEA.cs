using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.Localization;
using System;
using static Terraria.GameContent.Animations.IL_Actions.NPCs;


namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class XiaoZhiTiaoEA : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            // 困难模式前：Orange，价值15金
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.value = Item.sellPrice(0, 15, 0, 0);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindLastIndex(x => x.Mod == "Terraria" && x.Name.StartsWith("Tooltip"));
            if (index != -1)
            {
                if (Main.keyState.PressingShift())
                    tooltips.Insert(index + 1, new TooltipLine(Mod, "ShiftTooltip", GetHjsonText("Items.XiaoZhiTiaoEA.ShiftTipDetailed"))
                    { OverrideColor = Color.LightBlue });
                else
                    tooltips.Insert(index + 1, new TooltipLine(Mod, "NormalTooltip", GetHjsonText("Items.XiaoZhiTiaoEA.ShiftTip"))
                    { OverrideColor = Color.Gray });
            }
        }

        private static string GetHjsonText(string key) => Language.GetTextValue($"Mods.CalamityThrowingSpear.{key}");
    }
}



/*
 AXiaoZhiTiaoHaHaHa: {
		DisplayName: 鸢素方舟石板
		Tooltip:
			'''
			石板上的语句已经模糊不清，疑似是被人破坏了
			石板的背面写着另一些莫名其妙的话，有些字难以辨认：
			[c/aa5fff:“嘿，我知道你会看到这段话的”]
			[c/aa5fff:“你可能会好奇我是谁，又为什么要“破坏文物”，但都不重要”]
			[c/aa5fff:“这类石板都是记载有关■■■■的信息的，这块已经没用了”]
			[c/aa5fff:“为什么没用了？因为东西我拿了啊”]
			[c/aa5fff:“我知道你可能会很急，但你先别急”]
			[c/aa5fff:“你把][c/bfbfbf:那三个该死的机械造物][c/aa5fff:给拆了对吧”]
			[c/aa5fff:“那些][c/003cff:蕴含着][c/11ff00:不同力量][c/ff7200:的魂魄][c/aa5fff:可以用于制作][c/ff3c00:一种能够提升体质的水果][c/aa5fff:”]
			[c/aa5fff:“以后还会有■■■■■■■能做出][c/ff7e90:这类有奇妙的功效的水果][c/aa5fff:”]
			[c/aa5fff:“我知道你可能挺想吃][c/ff7e90:这些水果][c/aa5fff:的，但是听我一句劝，我建议你别吃”]
			[c/aa5fff:“为什么？因为它们][c/ff0000:■■■■■■][c/aa5fff:！”]
			[c/aa5fff:“说老实话，你有这个心思还不如多跑跑步，玩玩躲避球什么的”]
			[c/aa5fff:“如果你听得进我的劝][c/ffaf54:少吃水果][c/aa5fff:，那我可能会在你处理掉][c/00ffb7:某些蠢蠢欲动的天外来物][c/aa5fff:之后把][c/ffaf54:■■■■][c/aa5fff:直接给你”]
			[c/aa5fff:“或许你会想是不是拿完奖励再吃就没事了，打消这个念头吧，我会一直盯着你的][c/ff0000:>:)][c/aa5fff:”]
			[c/aa5fff:“                                                                                                        ——■■■■”]
			'''
	}
 */
