using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge
{
    internal sealed class ScourgeHeldSubmarineShocker : BossHeldAimedWeapon
    {
        public override string WeaponName => "SubmarineShocker";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.08f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(120, 220, 255) * 0.5f;
    }

    internal sealed class ScourgeHeldBarinautical : BossHeldAimedWeapon
    {
        public override string WeaponName => "Barinautical";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(120, 220, 255) * 0.5f;
    }

    internal sealed class ScourgeHeldDownpour : BossHeldAimedWeapon
    {
        public override string WeaponName => "Downpour";
        public override float SpriteScale => 1.3f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(110, 180, 255) * 0.5f;
    }

    internal sealed class ScourgeHeldDeepseaStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "DeepseaStaff";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(100, 200, 180) * 0.5f;
    }

    internal sealed class ScourgeHeldScourgeSeas : BossHeldSwingWeapon
    {
        public override string WeaponName => "ScourgeoftheSeas";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 18;
        public override int SlashTime => 10;
        public override int RecoveryTime => 14;
        public override Color GlowColor => new Color(180, 255, 140) * 0.5f;
    }

    internal sealed class ScourgeHeldFlakToxicannon : BossHeldAimedWeapon
    {
        public override string WeaponName => "FlakToxicannon";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(150, 255, 100) * 0.5f;
    }

    internal sealed class ScourgeHeldSlitheringEels : BossHeldAimedWeapon
    {
        public override string WeaponName => "SlitheringEels";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(150, 255, 120) * 0.5f;
    }

    internal sealed class ScourgeHeldCausticCroaker : BossHeldAimedWeapon
    {
        public override string WeaponName => "CausticCroakerStaff";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(150, 255, 120) * 0.5f;
    }

    internal sealed class ScourgeHeldSkyfinBombers : BossHeldSwingWeapon
    {
        public override string WeaponName => "SkyfinBombers";
        public override float SpriteScale => 1.3f;
        public override int WindupTime => 14;
        public override int SlashTime => 8;
        public override int RecoveryTime => 10;
        public override Color GlowColor => new Color(150, 255, 120) * 0.5f;
    }

    internal sealed class ScourgeHeldSpentFuel : BossHeldSwingWeapon
    {
        public override string WeaponName => "SpentFuelContainer";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 20;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(150, 255, 120) * 0.5f;
    }

    internal sealed class ScourgeHeldSulphurousGrabber : BossHeldSwingWeapon
    {
        public override string WeaponName => "SulphurousGrabber";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 16;
        public override int SlashTime => 8;
        public override int RecoveryTime => 12;
        public override Color GlowColor => new Color(120, 200, 90) * 0.5f;
    }
}
