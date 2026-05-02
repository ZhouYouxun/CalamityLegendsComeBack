namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public static class SHPCAmmoCapacity
    {
        // Indexed by SHPC effect ID. 0, 16, 20 and 27 are currently unused gaps.
        public static readonly int[] ShotsPerAmmo =
        {
            0, // EffectID 0: Unused gap / 未使用空位
            75, // EffectID 1: Energy Core / 钨钢能量核心
            75, // EffectID 2: Stormlion Mandible / 风暴之颚
            50, // EffectID 3: Sulphuric Scale / 硫磺鳞片
            50, // EffectID 4: Purified Gel / 纯净凝胶
            50, // EffectID 5: Essence of Havoc / 混沌精华
            50, // EffectID 6: Essence of Eleum / 冰精华
            50, // EffectID 7: Essence of Sunlight / 日光精华
            200, // EffectID 8: Titan Heart / 泰坦之心
            50, // EffectID 9: Soul of Light / 光明之魂
            50, // EffectID 10: Soul of Night / 暗影之魂
            50, // EffectID 11: Soul of Flight / 飞翔之魂
            75, // EffectID 12: Soul of Fright / 恐惧之魂
            75, // EffectID 13: Soul of Might / 力量之魂
            75, // EffectID 14: Soul of Sight / 视域之魂
            75, // EffectID 15: Living Shard / 生命碎片
            0, // EffectID 16: Unused gap / 未使用空位
            50, // EffectID 17: Depth Cells / 深渊细胞
            50, // EffectID 18: Plague Cell Canister / 瘟疫细胞罐
            75, // EffectID 19: Ashes of Calamity / 灾厄尘
            0, // EffectID 20: Beetle Husk, not implemented / 甲虫外壳，未实现
            75, // EffectID 21: Solar Fragment / 日曜碎片
            75, // EffectID 22: Vortex Fragment / 星旋碎片
            75, // EffectID 23: Nebula Fragment / 星云碎片
            75, // EffectID 24: Stardust Fragment / 星尘碎片
            75, // EffectID 25: Meld Blob / 冥思溶剂
            75, // EffectID 26: Unholy Essence / 浊火精华
            0, // EffectID 27: Unused gap / 未使用空位
            100, // EffectID 28: Divine Geode / 神圣晶石
            75, // EffectID 29: Bloodstone Core / 血神核心
            125, // EffectID 30: Ruinous Soul / 毁灭之灵
            75, // EffectID 31: Necroplasm / 灵质
            225, // EffectID 32: Dark Plasma / 暗离子体
            225, // EffectID 33: Twisting Nether / 扭曲虚空
            100, // EffectID 34: Endothermic Energy / 恒温能量
            100, // EffectID 35: Nightmare Fuel / 梦魇魔能
            150, // EffectID 36: Ascendant Spirit Essence / 化神魂精
            150, // EffectID 37: Yharon Soul Fragment / 龙魂碎片
            200, // EffectID 38: Exo Prism / 星流棱晶
            200, // EffectID 39: Ashes of Annihilation / 湮灭余烬
            225 // EffectID 40: Armored Shell / 装甲外壳
        };

        public static int GetCapacity(int effectID)
        {
            if (effectID < 0 || effectID >= ShotsPerAmmo.Length)
                return 50;

            int capacity = ShotsPerAmmo[effectID];
            return capacity > 0 ? capacity : 50;
        }
    }
}
