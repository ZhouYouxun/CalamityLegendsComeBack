using System.Collections.Generic;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules
{
    public static class EffectRegistry
    {
        // EffectID → Effect实例
        private static readonly Dictionary<int, RulesOfEffect> EffectsByID = new();

        // AmmoType → EffectID
        private static readonly Dictionary<int, int> AmmoToEffectID = new();

        // 默认效果（兜底）
        private static readonly DefaultEffect Default = new();

        // 注册一个效果
        public static void RegisterEffect(RulesOfEffect effect)
        {
            if (effect == null)
                return;

            EffectsByID[effect.EffectID] = effect;

            // 同时注册弹药映射
            AmmoToEffectID[effect.AmmoType] = effect.EffectID;
        }

        // 根据 EffectID 获取效果
        public static RulesOfEffect GetEffectByID(int effectID)
        {
            if (EffectsByID.TryGetValue(effectID, out var effect))
                return effect;

            return Default;
        }

        // 根据 Ammo 获取 EffectID
        public static int GetEffectIDByAmmo(int ammoType)
        {
            if (AmmoToEffectID.TryGetValue(ammoType, out int effectID))
                return effectID;

            return Default.EffectID;
        }

        // 判断某个物品是不是合法弹药
        public static bool IsRegisteredAmmo(int ammoType)
        {
            return AmmoToEffectID.ContainsKey(ammoType);
        }
    }
}