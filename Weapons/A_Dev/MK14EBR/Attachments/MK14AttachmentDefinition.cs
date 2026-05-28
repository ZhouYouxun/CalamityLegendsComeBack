namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14AttachmentDefinition
    {
        public MK14AttachmentDefinition(
            MK14AttachmentSlot slot,
            int value,
            string key,
            int unlockStage,
            string texturePath = null,
            float damageMultiplier = 1f,
            float projectileSpeedMultiplier = 1f,
            float knockbackMultiplier = 1f,
            float armorPenetration = 0f,
            int extraPenetration = 0,
            float projectileScaleMultiplier = 1f,
            int rpmOverride = 0,
            float barrelBaseSpreadDegrees = -1f,
            float barrelMaxSpreadDegrees = -1f,
            int barrelSpreadRampEnd = -1,
            int infinitePenetrationFrames = 0,
            int aggroReduction = 0,
            bool forceSingleHitAndDoubleStrike = false,
            bool sustainedFireDamageRamp = false,
            bool homing = false,
            bool nightDamageBonus = false,
            bool redDotRangeProfile = false,
            bool highPowerRangeProfile = false,
            bool dragonBreathMarker = false,
            bool laserLocksSpread = false,
            bool movementBipod = false,
            bool spiderSlowOnHit = false)
        {
            Slot = slot;
            Value = value;
            Key = key;
            UnlockStage = unlockStage;
            TexturePath = texturePath;
            DamageMultiplier = damageMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            KnockbackMultiplier = knockbackMultiplier;
            ArmorPenetration = armorPenetration;
            ExtraPenetration = extraPenetration;
            ProjectileScaleMultiplier = projectileScaleMultiplier;
            RpmOverride = rpmOverride;
            BarrelBaseSpreadDegrees = barrelBaseSpreadDegrees;
            BarrelMaxSpreadDegrees = barrelMaxSpreadDegrees;
            BarrelSpreadRampEnd = barrelSpreadRampEnd;
            InfinitePenetrationFrames = infinitePenetrationFrames;
            AggroReduction = aggroReduction;
            ForceSingleHitAndDoubleStrike = forceSingleHitAndDoubleStrike;
            SustainedFireDamageRamp = sustainedFireDamageRamp;
            Homing = homing;
            NightDamageBonus = nightDamageBonus;
            RedDotRangeProfile = redDotRangeProfile;
            HighPowerRangeProfile = highPowerRangeProfile;
            DragonBreathMarker = dragonBreathMarker;
            LaserLocksSpread = laserLocksSpread;
            MovementBipod = movementBipod;
            SpiderSlowOnHit = spiderSlowOnHit;
        }

        public MK14AttachmentSlot Slot { get; }
        public int Value { get; }
        public string Key { get; }
        public int UnlockStage { get; }
        public string TexturePath { get; }
        public float DamageMultiplier { get; }
        public float ProjectileSpeedMultiplier { get; }
        public float KnockbackMultiplier { get; }
        public float ArmorPenetration { get; }
        public int ExtraPenetration { get; }
        public float ProjectileScaleMultiplier { get; }
        public int RpmOverride { get; }
        public float BarrelBaseSpreadDegrees { get; }
        public float BarrelMaxSpreadDegrees { get; }
        public int BarrelSpreadRampEnd { get; }
        public int InfinitePenetrationFrames { get; }
        public int AggroReduction { get; }
        public bool ForceSingleHitAndDoubleStrike { get; }
        public bool SustainedFireDamageRamp { get; }
        public bool Homing { get; }
        public bool NightDamageBonus { get; }
        public bool RedDotRangeProfile { get; }
        public bool HighPowerRangeProfile { get; }
        public bool DragonBreathMarker { get; }
        public bool LaserLocksSpread { get; }
        public bool MovementBipod { get; }
        public bool SpiderSlowOnHit { get; }

        public string NameKey => $"Mods.CalamityLegendsComeBack.MK14EBR.Attachments.{Key}.Name";
        public string EffectKey => $"Mods.CalamityLegendsComeBack.MK14EBR.Attachments.{Key}.Effect";
    }
}
