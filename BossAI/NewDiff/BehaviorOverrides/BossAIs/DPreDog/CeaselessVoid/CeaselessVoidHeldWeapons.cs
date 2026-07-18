using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.CeaselessVoid
{
    internal sealed class VoidHeldMirrorBlade : BossHeldAimedWeapon
    {
        public override string WeaponName => "MirrorBlade";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldVoidConcentration : BossHeldAimedWeapon
    {
        public override string WeaponName => "VoidConcentrationStaff";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldDarkSpark : BossHeldAimedWeapon
    {
        public override string WeaponName => "DarkSpark";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldEventHorizon : BossHeldAimedWeapon
    {
        public override string WeaponName => "EventHorizon";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldMistlestorm : BossHeldAimedWeapon
    {
        public override string WeaponName => "Mistlestorm";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldOntologicalDespoiler : BossHeldAimedWeapon
    {
        public override string WeaponName => "OntologicalDespoiler";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(200, 120, 255) * 0.5f;
    }

    internal sealed class VoidHeldSealedSingularity : BossHeldAimedWeapon
    {
        public override string WeaponName => "SealedSingularity";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(120, 30, 180) * 0.5f;
    }

    internal sealed class VoidHeldTacticiansTrump : BossHeldAimedWeapon
    {
        public override string WeaponName => "TacticiansTrumpCard";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class VoidHeldEternity : BossHeldAimedWeapon
    {
        public override string WeaponName => "Eternity";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(200, 120, 255) * 0.5f;
    }

    internal sealed class VoidHeldPhantasmalFury : BossHeldAimedWeapon
    {
        public override string WeaponName => "PhantasmalFury";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(80, 160, 255) * 0.5f;
    }

    internal sealed class VoidHeldRealityRupture : BossHeldAimedWeapon
    {
        public override string WeaponName => "RealityRupture";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }
}
