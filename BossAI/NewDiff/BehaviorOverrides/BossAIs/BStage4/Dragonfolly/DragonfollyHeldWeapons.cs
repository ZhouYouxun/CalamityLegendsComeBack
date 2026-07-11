using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Dragonfolly
{
    internal sealed class FollyHeldProboscis : BossHeldAimedWeapon
    {
        public override string WeaponName => "GildedProboscis";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override float RestOutset => 22f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(255, 210, 60) * 0.5f;
    }

    internal sealed class FollyHeldGoldenEagle : BossHeldAimedWeapon
    {
        public override string WeaponName => "GoldenEagle";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.09f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(255, 210, 60) * 0.5f;
    }

    internal sealed class FollyHeldRougeSlash : BossHeldSwingWeapon
    {
        public override string WeaponName => "RougeSlash";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 20;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(255, 120, 190) * 0.5f;
    }
}
