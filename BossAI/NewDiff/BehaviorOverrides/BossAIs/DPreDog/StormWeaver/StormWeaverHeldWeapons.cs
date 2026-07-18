using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.StormWeaver
{
    internal sealed class WeaverHeldSkytideDragoon : BossHeldAimedWeapon
    {
        public override string WeaponName => "SkytideDragoon";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.09f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class WeaverHeldStorm : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheStorm";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class WeaverHeldVolterion : BossHeldAimedWeapon
    {
        public override string WeaponName => "Volterion";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class WeaverHeldAquasScepter : BossHeldAimedWeapon
    {
        public override string WeaponName => "AquasScepter";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(140, 210, 255) * 0.5f;
    }

    internal sealed class WeaverHeldCorinthPrime : BossHeldAimedWeapon
    {
        public override string WeaponName => "CorinthPrime";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class WeaverHeldStellarTorus : BossHeldAimedWeapon
    {
        public override string WeaponName => "StellarTorusStaff";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 200, 255) * 0.5f;
    }

    internal sealed class WeaverHeldTeslaStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "Teslastaff";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(220, 240, 255) * 0.5f;
    }

    internal sealed class WeaverHeldTwistingThunder : BossHeldAimedWeapon
    {
        public override string WeaponName => "TwistingThunder";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(255, 120, 200) * 0.5f;
    }

    internal sealed class WeaverHeldPack : BossHeldAimedWeapon
    {
        public override string WeaponName => "ThePack";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class WeaverHeldShadowboltStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "ShadowboltStaff";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class WeaverHeldSeadragon : BossHeldAimedWeapon
    {
        public override string WeaponName => "Seadragon";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(140, 210, 255) * 0.5f;
    }

    internal sealed class WeaverHeldFourSeasons : BossHeldSwingWeapon
    {
        public override string WeaponName => "FourSeasonsGalaxia";
        public override float SpriteScale => 1.45f;
        public override int WindupTime => 24;
        public override int SlashTime => 10;
        public override int RecoveryTime => 18;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class WeaverHeldRealityRupture : BossHeldAimedWeapon
    {
        public override string WeaponName => "RealityRupture";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }
}
