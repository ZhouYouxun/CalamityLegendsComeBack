using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14RuntimeStats
    {
        public int Rpm = 660;
        public float DamageMultiplier = 1f;
        public float ProjectileSpeedMultiplier = 1f;
        public float KnockbackMultiplier = 1f;
        public float ArmorPenetration;
        public int ExtraPenetration;
        public float ProjectileScaleMultiplier = 1f;
        public float BarrelBaseSpreadDegrees = 1f;
        public float BarrelMaxSpreadDegrees = 5f;
        public int BarrelSpreadRampEnd = 100;
        public int InfinitePenetrationFrames;
        public int AggroReduction;
        public bool ForceSingleHitAndDoubleStrike;
        public bool SustainedFireDamageRamp;
        public bool Homing;
        public bool NightDamageBonus;
        public bool RedDotRangeProfile;
        public bool HighPowerRangeProfile;
        public bool DragonBreathMarker;
        public bool LaserLocksSpread;
        public bool MovementBipod;
        public bool SpiderSlowOnHit;
    }

    internal static class MK14AttachmentDatabase
    {
        private const float BipodStationaryMph = 3f;
        private const float BipodFullSpeedMph = 30f;
        private const float TerrariaVelocityToMph = 216000f / 42240f;

        public static readonly MK14AttachmentDefinition[] Barrels = MK14BarrelData.Entries;
        public static readonly MK14AttachmentDefinition[] Muzzles = MK14MuzzleData.Entries;
        public static readonly MK14AttachmentDefinition[] Underbarrels = MK14UnderbarrelData.Entries;
        public static readonly MK14AttachmentDefinition[] Stocks = MK14StockData.Entries;
        public static readonly MK14AttachmentDefinition[] Sights = MK14SightData.Entries;

        public static MK14AttachmentDefinition[] GetEntries(MK14AttachmentSlot slot) => slot switch
        {
            MK14AttachmentSlot.Barrel => Barrels,
            MK14AttachmentSlot.Muzzle => Muzzles,
            MK14AttachmentSlot.Underbarrel => Underbarrels,
            MK14AttachmentSlot.Stock => Stocks,
            MK14AttachmentSlot.Sight => Sights,
            _ => Array.Empty<MK14AttachmentDefinition>()
        };

        public static MK14AttachmentDefinition Get(MK14AttachmentSlot slot, int value)
        {
            MK14AttachmentDefinition[] entries = GetEntries(slot);
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Value == value)
                    return entries[i];
            }

            return entries.Length > 0 ? entries[0] : null;
        }

        public static bool IsUnlocked(MK14AttachmentDefinition definition)
        {
            if (definition == null)
                return false;

            return new BalanceMK14EBR().GetCompletedStageIndex() >= definition.UnlockStage;
        }

        public static MK14RuntimeStats BuildStats(NewLegendMK14EBR weapon, Player owner = null)
        {
            MK14RuntimeStats stats = new();

            Apply(stats, Get(MK14AttachmentSlot.Barrel, (int)weapon.Barrel));
            Apply(stats, Get(MK14AttachmentSlot.Muzzle, (int)weapon.Muzzle));
            Apply(stats, Get(MK14AttachmentSlot.Underbarrel, (int)weapon.Underbarrel));
            Apply(stats, Get(MK14AttachmentSlot.Stock, (int)weapon.Stock));
            Apply(stats, Get(MK14AttachmentSlot.Sight, (int)weapon.Sight));

            if (owner != null)
                ApplyPlayerConditionalStats(stats, owner);

            return stats;
        }

        public static float ComputeSpreadDegrees(MK14RuntimeStats stats, int consecutiveShots)
        {
            if (stats.LaserLocksSpread)
                return 1f;

            int rampEnd = Math.Max(1, stats.BarrelSpreadRampEnd);
            float ramp = Utils.GetLerpValue(0f, rampEnd, consecutiveShots, true);
            return Math.Max(0f, MathHelper.Lerp(stats.BarrelBaseSpreadDegrees, stats.BarrelMaxSpreadDegrees, ramp));
        }

        public static float ComputeSustainedFireDamageMultiplier(MK14RuntimeStats stats, int consecutiveShots)
        {
            if (!stats.SustainedFireDamageRamp)
                return 1f;

            float ramp = Utils.GetLerpValue(0f, 100f, consecutiveShots, true);
            return MathHelper.Lerp(1f, 1.15f, ramp);
        }

        private static void Apply(MK14RuntimeStats stats, MK14AttachmentDefinition definition)
        {
            if (definition == null)
                return;

            stats.DamageMultiplier *= definition.DamageMultiplier;
            stats.ProjectileSpeedMultiplier *= definition.ProjectileSpeedMultiplier;
            stats.KnockbackMultiplier *= definition.KnockbackMultiplier;
            stats.ArmorPenetration += definition.ArmorPenetration;
            stats.ExtraPenetration += definition.ExtraPenetration;
            stats.ProjectileScaleMultiplier *= definition.ProjectileScaleMultiplier;
            stats.InfinitePenetrationFrames = Math.Max(stats.InfinitePenetrationFrames, definition.InfinitePenetrationFrames);
            stats.AggroReduction += definition.AggroReduction;

            if (definition.RpmOverride > 0)
                stats.Rpm = definition.RpmOverride;

            if (definition.BarrelBaseSpreadDegrees >= 0f)
                stats.BarrelBaseSpreadDegrees = definition.BarrelBaseSpreadDegrees;

            if (definition.BarrelMaxSpreadDegrees >= 0f)
                stats.BarrelMaxSpreadDegrees = definition.BarrelMaxSpreadDegrees;

            if (definition.BarrelSpreadRampEnd >= 0)
                stats.BarrelSpreadRampEnd = definition.BarrelSpreadRampEnd;

            stats.ForceSingleHitAndDoubleStrike |= definition.ForceSingleHitAndDoubleStrike;
            stats.SustainedFireDamageRamp |= definition.SustainedFireDamageRamp;
            stats.Homing |= definition.Homing;
            stats.NightDamageBonus |= definition.NightDamageBonus;
            stats.RedDotRangeProfile |= definition.RedDotRangeProfile;
            stats.HighPowerRangeProfile |= definition.HighPowerRangeProfile;
            stats.DragonBreathMarker |= definition.DragonBreathMarker;
            stats.LaserLocksSpread |= definition.LaserLocksSpread;
            stats.MovementBipod |= definition.MovementBipod;
            stats.SpiderSlowOnHit |= definition.SpiderSlowOnHit;
        }

        private static void ApplyPlayerConditionalStats(MK14RuntimeStats stats, Player owner)
        {
            if (!stats.MovementBipod)
                return;

            float speedMph = Math.Abs(owner.velocity.X) * TerrariaVelocityToMph;
            float fullSpeedMph = Math.Max(BipodFullSpeedMph, owner.maxRunSpeed * TerrariaVelocityToMph);
            float movingRatio = Utils.GetLerpValue(BipodStationaryMph, fullSpeedMph, speedMph, true);
            float multiplier = MathHelper.Lerp(1.15f, 0.85f, movingRatio);
            stats.DamageMultiplier *= multiplier;
            stats.ProjectileSpeedMultiplier *= multiplier;
        }
    }
}
