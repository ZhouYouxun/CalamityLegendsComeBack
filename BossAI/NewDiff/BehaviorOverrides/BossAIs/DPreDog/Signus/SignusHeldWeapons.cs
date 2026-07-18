using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Signus
{
    internal sealed class SignusHeldCosmicKunai : BossHeldAimedWeapon
    {
        public override string WeaponName => "CosmicKunai";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.09f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldCosmilamp : BossHeldAimedWeapon
    {
        public override string WeaponName => "Cosmilamp";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldAethersWhisper : BossHeldAimedWeapon
    {
        public override string WeaponName => "AethersWhisper";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldDeathsAscension : BossHeldSwingWeapon
    {
        public override string WeaponName => "DeathsAscension";
        public override float SpriteScale => 1.45f;
        public override int WindupTime => 22;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldEmpyreanKnives : BossHeldAimedWeapon
    {
        public override string WeaponName => "EmpyreanKnives";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 240, 255) * 0.5f;
    }

    internal sealed class SignusHeldKingConstellations : BossHeldAimedWeapon
    {
        public override string WeaponName => "KingofConstellationsTenryu";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldMagneticMeltdown : BossHeldAimedWeapon
    {
        public override string WeaponName => "MagneticMeltdown";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldNadir : BossHeldSwingWeapon
    {
        public override string WeaponName => "Nadir";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 20;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class SignusHeldSevensStriker : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheSevensStriker";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 6;
        public override Color GlowColor => new Color(220, 160, 255) * 0.5f;
    }

    internal sealed class SignusHeldVenusianTrident : BossHeldAimedWeapon
    {
        public override string WeaponName => "VenusianTrident";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 140, 40) * 0.5f;
    }

    internal sealed class SignusHeldRealityRupture : BossHeldAimedWeapon
    {
        public override string WeaponName => "RealityRupture";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }
}
