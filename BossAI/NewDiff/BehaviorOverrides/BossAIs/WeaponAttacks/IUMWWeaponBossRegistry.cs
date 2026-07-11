using System.Collections.Generic;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks
{
    internal static class IUMWWeaponBossRegistry
    {
        private static readonly Dictionary<int, IUMWWeaponBossProfile> ProfilesByNpcType = new();
        private static readonly Dictionary<string, int> ItemTypeCache = new();

        public static void Load()
        {
            ProfilesByNpcType.Clear();
            ItemTypeCache.Clear();

            foreach (IUMWWeaponBossProfile profile in IUMWWeaponBossProfiles.All)
            {
                foreach (string npcName in profile.NpcNames)
                {
                    if (ModContent.TryFind("CalamityMod/" + npcName, out ModNPC npc))
                        ProfilesByNpcType[npc.Type] = profile;
                }
            }
        }

        public static void Unload()
        {
            ProfilesByNpcType.Clear();
            ItemTypeCache.Clear();
        }

        public static bool TryGetProfile(int npcType, out IUMWWeaponBossProfile profile)
        {
            return ProfilesByNpcType.TryGetValue(npcType, out profile);
        }

        public static bool TryCreateAI(string npcName, out IUMWBossAI ai)
        {
            ai = null;

            if (!ModContent.TryFind("CalamityMod/" + npcName, out ModNPC npc))
                return false;

            if (!TryGetProfile(npc.Type, out IUMWWeaponBossProfile profile))
                return false;

            ai = new IUMWWeaponBossAI(npc.Type, profile);
            return true;
        }

        public static int GetItemType(string internalName)
        {
            if (string.IsNullOrWhiteSpace(internalName))
                return 0;

            if (ItemTypeCache.TryGetValue(internalName, out int cached))
                return cached;

            int type = 0;
            if (ModContent.TryFind("CalamityMod/" + internalName, out ModItem calamityItem))
                type = calamityItem.Type;
            else if (ModContent.TryFind("CalamityLegendsComeBack/" + internalName, out ModItem localItem))
                type = localItem.Type;

            ItemTypeCache[internalName] = type;
            return type;
        }
    }
}
