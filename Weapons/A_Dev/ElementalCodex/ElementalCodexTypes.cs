using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal enum ElementalCodexElement
    {
        None,
        Fire,
        Water,
        Ice,
        Lightning,
        Nature,
        Disease
    }

    internal enum ElementalCodexReaction
    {
        None,
        SteamBurst,
        MeltingImpact,
        Overload,
        Scorch,
        Paralysis,
        Freeze,
        Electrified,
        Growth,
        Wither,
        Condensation,
        ColdStorage,
        CorruptFreeze,
        Flourish,
        Control,
        Neutralization
    }

    internal sealed class ElementalCodexWeaponDefinition
    {
        public ElementalCodexWeaponDefinition(string chineseName, string internalName, bool vanilla, ElementalCodexElement[] elements)
        {
            ChineseName = chineseName;
            InternalName = internalName;
            Vanilla = vanilla;
            Elements = elements.Where(e => e != ElementalCodexElement.None).Distinct().ToArray();
            if (Elements.Length == 0)
                Elements = new[] { ElementalCodexElement.Fire };
        }

        public string ChineseName { get; }
        public string InternalName { get; }
        public bool Vanilla { get; }
        public ElementalCodexElement[] Elements { get; }
        public bool Mixed => Elements.Length > 1;

        public ElementalCodexElement PickElementForHit(int itemType)
        {
            if (Elements.Length == 1)
                return Elements[0];

            int index = (int)((Main.GameUpdateCount / 20 + itemType) % Elements.Length);
            return Elements[index];
        }

        public string GetLocalizedElementList()
        {
            string[] names = Elements.Select(ElementalCodexContent.GetElementName).ToArray();
            return string.Join("/", names);
        }
    }

    internal static class ElementalCodexContent
    {
        public static readonly ElementalCodexElement[] AllElements =
        {
            ElementalCodexElement.Fire,
            ElementalCodexElement.Water,
            ElementalCodexElement.Ice,
            ElementalCodexElement.Lightning,
            ElementalCodexElement.Nature,
            ElementalCodexElement.Disease
        };

        public static int GetBuffType(ElementalCodexElement element) => element switch
        {
            ElementalCodexElement.Fire => ModContent.BuffType<ElementalFireDebuff>(),
            ElementalCodexElement.Water => ModContent.BuffType<ElementalWaterDebuff>(),
            ElementalCodexElement.Ice => ModContent.BuffType<ElementalIceDebuff>(),
            ElementalCodexElement.Lightning => ModContent.BuffType<ElementalLightningDebuff>(),
            ElementalCodexElement.Nature => ModContent.BuffType<ElementalNatureDebuff>(),
            ElementalCodexElement.Disease => ModContent.BuffType<ElementalDiseaseDebuff>(),
            _ => 0
        };

        public static string GetElementName(ElementalCodexElement element)
        {
            string key = $"Mods.CalamityLegendsComeBack.ElementalCodex.Elements.{element}";
            string value = Language.GetTextValue(key);
            return value == key ? element.ToString() : value;
        }

        public static string GetReactionName(ElementalCodexReaction reaction)
        {
            string key = $"Mods.CalamityLegendsComeBack.ElementalCodex.Reactions.{reaction}";
            string value = Language.GetTextValue(key);
            return value == key ? reaction.ToString() : value;
        }

        public static Color GetElementColor(ElementalCodexElement element) => element switch
        {
            ElementalCodexElement.Fire => new Color(255, 86, 42),
            ElementalCodexElement.Water => new Color(72, 158, 255),
            ElementalCodexElement.Ice => new Color(150, 230, 255),
            ElementalCodexElement.Lightning => new Color(190, 92, 255),
            ElementalCodexElement.Nature => new Color(104, 224, 108),
            ElementalCodexElement.Disease => new Color(64, 58, 68),
            _ => Color.White
        };

        public static Color GetReactionColor(ElementalCodexReaction reaction) => reaction switch
        {
            ElementalCodexReaction.SteamBurst => new Color(150, 214, 255),
            ElementalCodexReaction.MeltingImpact => new Color(255, 134, 74),
            ElementalCodexReaction.Overload => new Color(255, 92, 220),
            ElementalCodexReaction.Scorch => new Color(255, 150, 42),
            ElementalCodexReaction.Paralysis => new Color(196, 96, 64),
            ElementalCodexReaction.Freeze => new Color(160, 240, 255),
            ElementalCodexReaction.Electrified => new Color(164, 118, 255),
            ElementalCodexReaction.Growth => new Color(126, 240, 118),
            ElementalCodexReaction.Wither => new Color(96, 84, 110),
            ElementalCodexReaction.Condensation => new Color(120, 210, 255),
            ElementalCodexReaction.ColdStorage => new Color(178, 250, 226),
            ElementalCodexReaction.CorruptFreeze => new Color(80, 52, 104),
            ElementalCodexReaction.Flourish => new Color(116, 255, 162),
            ElementalCodexReaction.Control => new Color(210, 92, 255),
            ElementalCodexReaction.Neutralization => new Color(220, 230, 188),
            _ => Color.White
        };

        public static ElementalCodexReaction GetReaction(ElementalCodexElement first, ElementalCodexElement second)
        {
            if (first == ElementalCodexElement.None || second == ElementalCodexElement.None || first == second)
                return ElementalCodexReaction.None;

            ElementalCodexElement a = first < second ? first : second;
            ElementalCodexElement b = first < second ? second : first;

            return (a, b) switch
            {
                (ElementalCodexElement.Fire, ElementalCodexElement.Water) => ElementalCodexReaction.SteamBurst,
                (ElementalCodexElement.Fire, ElementalCodexElement.Ice) => ElementalCodexReaction.MeltingImpact,
                (ElementalCodexElement.Fire, ElementalCodexElement.Lightning) => ElementalCodexReaction.Overload,
                (ElementalCodexElement.Fire, ElementalCodexElement.Nature) => ElementalCodexReaction.Scorch,
                (ElementalCodexElement.Fire, ElementalCodexElement.Disease) => ElementalCodexReaction.Paralysis,
                (ElementalCodexElement.Water, ElementalCodexElement.Ice) => ElementalCodexReaction.Freeze,
                (ElementalCodexElement.Water, ElementalCodexElement.Lightning) => ElementalCodexReaction.Electrified,
                (ElementalCodexElement.Water, ElementalCodexElement.Nature) => ElementalCodexReaction.Growth,
                (ElementalCodexElement.Water, ElementalCodexElement.Disease) => ElementalCodexReaction.Wither,
                (ElementalCodexElement.Ice, ElementalCodexElement.Lightning) => ElementalCodexReaction.Condensation,
                (ElementalCodexElement.Ice, ElementalCodexElement.Nature) => ElementalCodexReaction.ColdStorage,
                (ElementalCodexElement.Ice, ElementalCodexElement.Disease) => ElementalCodexReaction.CorruptFreeze,
                (ElementalCodexElement.Lightning, ElementalCodexElement.Nature) => ElementalCodexReaction.Flourish,
                (ElementalCodexElement.Lightning, ElementalCodexElement.Disease) => ElementalCodexReaction.Control,
                (ElementalCodexElement.Nature, ElementalCodexElement.Disease) => ElementalCodexReaction.Neutralization,
                _ => ElementalCodexReaction.None
            };
        }
    }

    internal static class ElementalCodexBalance
    {
        public const int OverloadRadius = 10 * 16;

        public static int GetElementDurationFrames(Player player, Item weapon, int panelDamage)
        {
            int useTime = Math.Max(weapon.useTime, 1);
            int damageTerm = Math.Clamp(panelDamage / 2, 0, 240);
            int speedPenalty = Math.Clamp((18 - useTime) * 4, -40, 48);
            return Math.Clamp(240 + damageTerm - speedPenalty, 180, 540);
        }

        public static int GetReactionCooldownFrames(int sourceDuration, ElementalCodexReaction reaction)
        {
            float multiplier = GetProgressCooldownMultiplier();
            if (reaction == ElementalCodexReaction.Neutralization)
                multiplier *= 2.8f;

            return Math.Max(90, (int)(sourceDuration * multiplier));
        }

        private static float GetProgressCooldownMultiplier()
        {
            int progress = 0;
            if (Main.hardMode)
                progress++;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                progress++;
            if (NPC.downedPlantBoss)
                progress++;
            if (NPC.downedGolemBoss)
                progress++;
            if (NPC.downedAncientCultist)
                progress++;
            if (NPC.downedMoonlord)
                progress++;
            if (CalamityMod.DownedBossSystem.downedProvidence)
                progress++;
            if (CalamityMod.DownedBossSystem.downedPolterghast)
                progress++;
            if (CalamityMod.DownedBossSystem.downedDoG)
                progress++;
            if (CalamityMod.DownedBossSystem.downedYharon)
                progress++;
            if (CalamityMod.DownedBossSystem.downedExoMechs || CalamityMod.DownedBossSystem.downedCalamitas)
                progress++;

            return MathHelper.Clamp(1f - progress * 0.055f, 0.42f, 1f);
        }
    }
}
