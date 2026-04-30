using System.Collections.Generic;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public sealed class LeonidMetalEntry
    {
        public LeonidMetalEntry(
            LeonidMetalID metalID,
            int itemType,
            string internalName,
            string chineseName,
            int originalNumericID,
            string effectGroup,
            Color themeColor)
        {
            MetalID = metalID;
            ItemType = itemType;
            InternalName = internalName;
            ChineseName = chineseName;
            OriginalNumericID = originalNumericID;
            EffectGroup = effectGroup;
            ThemeColor = themeColor;
        }

        public LeonidMetalID MetalID { get; }
        public int EffectID => (int)MetalID;
        public int ItemType { get; }
        public string InternalName { get; }
        public string ChineseName { get; }
        public int OriginalNumericID { get; }
        public string EffectGroup { get; }
        public Color ThemeColor { get; }
    }

    public static class LeonidMetalRegistry
    {
        private static List<LeonidMetalEntry> entries;
        private static Dictionary<int, LeonidMetalEntry> byItemType;
        private static Dictionary<int, LeonidMetalEntry> byEffectID;

        public static IReadOnlyList<LeonidMetalEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return entries;
            }
        }

        public static bool TryGetByItemType(int itemType, out LeonidMetalEntry entry)
        {
            EnsureLoaded();
            return byItemType.TryGetValue(itemType, out entry);
        }

        public static LeonidMetalEntry GetByEffectID(int effectID)
        {
            EnsureLoaded();
            byEffectID.TryGetValue(effectID, out LeonidMetalEntry entry);
            return entry;
        }

        public static LeonidMetalEntry GetByMetalID(LeonidMetalID metalID) => GetByEffectID((int)metalID);

        private static void EnsureLoaded()
        {
            if (entries != null)
                return;

            entries = new List<LeonidMetalEntry>
            {
                // A_Pre8
                new(LeonidMetalID.Copper, ItemID.CopperBar, "CopperBar", "铜锭", 20, "A_Pre8", new Color(202, 128, 82)), // 铜锭 20
                new(LeonidMetalID.Tin, ItemID.TinBar, "TinBar", "锡锭", 703, "A_Pre8", new Color(195, 204, 212)), // 锡锭 703
                new(LeonidMetalID.Iron, ItemID.IronBar, "IronBar", "铁锭", 22, "A_Pre8", new Color(138, 146, 162)), // 铁锭 22
                new(LeonidMetalID.Lead, ItemID.LeadBar, "LeadBar", "铅锭", 704, "A_Pre8", new Color(105, 114, 143)), // 铅锭 704
                new(LeonidMetalID.Silver, ItemID.SilverBar, "SilverBar", "银锭", 21, "A_Pre8", new Color(229, 233, 244)), // 银锭 21
                new(LeonidMetalID.Tungsten, ItemID.TungstenBar, "TungstenBar", "钨锭", 705, "A_Pre8", new Color(134, 187, 157)), // 钨锭 705
                new(LeonidMetalID.Gold, ItemID.GoldBar, "GoldBar", "金锭", 19, "A_Pre8", new Color(255, 213, 89)), // 金锭 19
                new(LeonidMetalID.Platinum, ItemID.PlatinumBar, "PlatinumBar", "铂金锭", 706, "A_Pre8", new Color(209, 247, 255)), // 铂金锭 706

                // B_PreOther
                new(LeonidMetalID.Demonite, ItemID.DemoniteBar, "DemoniteBar", "魔矿锭", 57, "B_PreOther", new Color(121, 90, 167)), // 魔矿锭 57
                new(LeonidMetalID.Crimtane, ItemID.CrimtaneBar, "CrimtaneBar", "猩红矿锭", 1257, "B_PreOther", new Color(215, 74, 92)), // 猩红矿锭 1257
                new(LeonidMetalID.Aerialite, ModContent.ItemType<AerialiteBar>(), "AerialiteBar", "天蓝锭", -1, "C_Calamity", new Color(113, 228, 255)), // 天蓝锭 AerialiteBar
                new(LeonidMetalID.Meteorite, ItemID.MeteoriteBar, "MeteoriteBar", "陨石锭", 117, "B_PreOther", new Color(109, 146, 193)), // 陨石锭 117
                new(LeonidMetalID.Hellstone, ItemID.HellstoneBar, "HellstoneBar", "狱石锭", 175, "B_PreOther", new Color(255, 122, 59)), // 狱石锭 175

                // D_New6
                new(LeonidMetalID.Cobalt, ItemID.CobaltBar, "CobaltBar", "钴锭", 381, "D_New6", new Color(66, 131, 235)), // 钴锭 381
                new(LeonidMetalID.Palladium, ItemID.PalladiumBar, "PalladiumBar", "钯金锭", 1184, "D_New6", new Color(255, 127, 164)), // 钯金锭 1184
                new(LeonidMetalID.Mythril, ItemID.MythrilBar, "MythrilBar", "秘银锭", 382, "D_New6", new Color(78, 224, 197)), // 秘银锭 382
                new(LeonidMetalID.Orichalcum, ItemID.OrichalcumBar, "OrichalcumBar", "山铜锭", 1191, "D_New6", new Color(255, 138, 173)), // 山铜锭 1191
                new(LeonidMetalID.Adamantite, ItemID.AdamantiteBar, "AdamantiteBar", "精金锭", 391, "D_New6", new Color(234, 61, 77)), // 精金锭 391
                new(LeonidMetalID.Titanium, ItemID.TitaniumBar, "TitaniumBar", "钛金锭", 1198, "D_New6", new Color(215, 233, 247)), // 钛金锭 1198

                // C_Calamity
                new(LeonidMetalID.Cryonic, ModContent.ItemType<CryonicBar>(), "CryonicBar", "寒元锭", -1, "C_Calamity", new Color(128, 233, 255)), // 寒元锭 CryonicBar

                // E_Final5
                new(LeonidMetalID.Hallowed, ItemID.HallowedBar, "HallowedBar", "神圣锭", 1225, "E_Final5", new Color(255, 234, 147)), // 神圣锭 1225
                new(LeonidMetalID.Chlorophyte, ItemID.ChlorophyteBar, "ChlorophyteBar", "叶绿锭", 1006, "E_Final5", new Color(126, 231, 92)), // 叶绿锭 1006
                new(LeonidMetalID.Perennial, ModContent.ItemType<PerennialBar>(), "PerennialBar", "永恒锭", -1, "C_Calamity", new Color(94, 239, 151)), // 永恒锭 PerennialBar
                new(LeonidMetalID.Shroomite, ItemID.ShroomiteBar, "ShroomiteBar", "蘑菇矿锭", 1552, "E_Final5", new Color(111, 161, 255)), // 蘑菇矿锭 1552
                new(LeonidMetalID.Spectre, ItemID.SpectreBar, "SpectreBar", "幽灵锭", 3261, "E_Final5", new Color(144, 227, 255)), // 幽灵锭 3261
                new(LeonidMetalID.Scoria, ModContent.ItemType<ScoriaBar>(), "ScoriaBar", "熔渣锭", -1, "C_Calamity", new Color(255, 109, 62)), // 熔渣锭 ScoriaBar
                new(LeonidMetalID.LifeAlloy, ModContent.ItemType<LifeAlloy>(), "LifeAlloy", "生命合金", -1, "C_Calamity", new Color(149, 255, 196)), // 生命合金 LifeAlloy
                new(LeonidMetalID.Lunar, ItemID.LunarBar, "LunarBar", "夜明锭", 3467, "E_Final5", new Color(141, 193, 255)), // 夜明锭 3467

                // F_PostLunar
                new(LeonidMetalID.Astral, ModContent.ItemType<AstralBar>(), "AstralBar", "炫星锭", -1, "F_PostLunar", new Color(123, 93, 255)), // 炫星锭 AstralBar
                new(LeonidMetalID.Uelibloom, ModContent.ItemType<UelibloomBar>(), "UelibloomBar", "龙蒿", -1, "F_PostLunar", new Color(128, 255, 112)), // 龙蒿 UelibloomBar
                new(LeonidMetalID.Cosmilite, ModContent.ItemType<CosmiliteBar>(), "CosmiliteBar", "宇宙锭", -1, "F_PostLunar", new Color(84, 220, 255)), // 宇宙锭 CosmiliteBar
                new(LeonidMetalID.Auric, ModContent.ItemType<AuricBar>(), "AuricBar", "圣金锭", -1, "F_PostLunar", new Color(255, 208, 72)), // 圣金锭 AuricBar
                new(LeonidMetalID.Shadowspec, ModContent.ItemType<ShadowspecBar>(), "ShadowspecBar", "暗影", -1, "F_PostLunar", new Color(149, 95, 255)) // 暗影 ShadowspecBar
            };

            byItemType = new Dictionary<int, LeonidMetalEntry>(entries.Count);
            byEffectID = new Dictionary<int, LeonidMetalEntry>(entries.Count);

            foreach (LeonidMetalEntry entry in entries)
            {
                byItemType[entry.ItemType] = entry;
                byEffectID[entry.EffectID] = entry;
            }
        }
    }
}
