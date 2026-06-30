using System.Collections.Generic;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules
{
    public static class DEBulletRegistry
    {
        // GunItemType → Rule 实例
        private static readonly Dictionary<int, DEBulletRule> Rules = new();
        private static readonly DEDefaultBulletRule DefaultRule = new();

        public static void Register(DEBulletRule rule)
        {
            if (rule != null)
                Rules[rule.GunItemType] = rule;
        }

        /// <summary>根据装填的枪 ItemType 获取对应规则；未装填或未注册时返回默认规则（LifeRound 行为）。</summary>
        public static DEBulletRule GetRule(int gunItemType)
        {
            if (gunItemType > 0 && Rules.TryGetValue(gunItemType, out DEBulletRule rule))
                return rule;
            return DefaultRule;
        }

        public static bool IsRegisteredGun(int itemType) => itemType > 0 && Rules.ContainsKey(itemType);

        public static IEnumerable<DEBulletRule> AllRules => Rules.Values;

        public static void Clear() => Rules.Clear();
    }
}
