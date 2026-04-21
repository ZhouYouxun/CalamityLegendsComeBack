using System.Collections.Generic;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    public static class LeonidMetalEffectRegistry
    {
        private static readonly Dictionary<int, LeonidMetalEffect> EffectsByID = new();

        public static void Register(LeonidMetalEffect effect)
        {
            if (effect == null)
                return;

            EffectsByID[effect.EffectID] = effect;
        }

        public static LeonidMetalEffect GetEffectByID(int effectID)
        {
            EffectsByID.TryGetValue(effectID, out LeonidMetalEffect effect);
            return effect;
        }

        public static LeonidMetalEffect[] ResolveEffects(int primaryEffectID, int secondaryEffectID)
        {
            List<LeonidMetalEffect> result = new(2);
            LeonidMetalEffect primary = GetEffectByID(primaryEffectID);
            LeonidMetalEffect secondary = GetEffectByID(secondaryEffectID);

            if (primary != null)
                result.Add(primary);

            if (secondary != null && secondary != primary)
                result.Add(secondary);

            return result.ToArray();
        }
    }
}
