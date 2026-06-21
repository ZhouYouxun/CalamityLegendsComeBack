namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public static class SHPCAmmoCapacity
    {
        // Indexed by SHPC effect ID. 0, 16, 20 and 27 are currently unused gaps.
        public static readonly int[] ShotsPerAmmo =
        {
            0, // EffectID 0: Unused gap
            75, // EffectID 1: Energy Core
            75, // EffectID 2: Stormlion Mandible
            50, // EffectID 3: Sulphuric Scale
            75, // EffectID 4: Purified Gel
            50, // EffectID 5: Essence of Havoc
            50, // EffectID 6: Essence of Eleum
            50, // EffectID 7: Essence of Sunlight
            50, // EffectID 8: Starblight Soot
            50, // EffectID 9: Soul of Light
            50, // EffectID 10: Soul of Night
            50, // EffectID 11: Soul of Flight
            75, // EffectID 12: Soul of Fright
            75, // EffectID 13: Soul of Might
            75, // EffectID 14: Soul of Sight
            75, // EffectID 15: Living Shard
            0, // EffectID 16: Unused gap
            50, // EffectID 17: Depth Cells
            50, // EffectID 18: Plague Cell Canister
            75, // EffectID 19: Ashes of Calamity
            0, // EffectID 20: Beetle Husk, not implemented
            75, // EffectID 21: Solar Fragment
            75, // EffectID 22: Vortex Fragment
            75, // EffectID 23: Nebula Fragment
            75, // EffectID 24: Stardust Fragment
            75, // EffectID 25: Meld Blob
            75, // EffectID 26: Unholy Essence
            0, // EffectID 27: Unused gap
            100, // EffectID 28: Divine Geode
            75, // EffectID 29: Bloodstone Core
            125, // EffectID 30: Ruinous Soul
            75, // EffectID 31: Necroplasm
            225, // EffectID 32: Dark Plasma
            225, // EffectID 33: Twisting Nether
            100, // EffectID 34: Endothermic Energy
            100, // EffectID 35: Nightmare Fuel
            500, // EffectID 36: Ascendant Spirit Essence
            150, // EffectID 37: Yharon Soul Fragment
            200, // EffectID 38: Exo Prism
            200, // EffectID 39: Ashes of Annihilation
            225, // EffectID 40: Armored Shell
            75, // EffectID 41: Pearl Shard
            100, // EffectID 42: Darksun Fragment
            1, // EffectID 43: Cynosure
            225, // EffectID 44: Core of Calamity
            50 // EffectID 45: Anodized Wulfrum Metal
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
