using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumDeus
{
    // The worm has no hands — weapons float anchored just ahead of the Head, same treatment Leviathan got.

    internal sealed class DeusHeldMicrowave : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheMicrowave";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(255, 150, 40) * 0.5f;
    }

    internal sealed class DeusHeldStarSputter : BossHeldAimedWeapon
    {
        public override string WeaponName => "StarSputter";
        public override float SpriteScale => 1.35f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    internal sealed class DeusHeldStarShower : BossHeldAimedWeapon
    {
        public override string WeaponName => "StarShower";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.35f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.45f;
    }

    internal sealed class DeusHeldStarspawnHelix : BossHeldAimedWeapon
    {
        public override string WeaponName => "StarspawnHelixStaff";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.045f) * 0.1f;
    }

    internal sealed class DeusHeldRegulusRiot : BossHeldAimedWeapon
    {
        public override string WeaponName => "RegulusRiot";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    internal sealed class DeusHeldAstralPike : BossHeldAimedWeapon
    {
        public override string WeaponName => "AstralPike";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    internal sealed class DeusHeldAstralBlaster : BossHeldAimedWeapon
    {
        public override string WeaponName => "AstralBlaster";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class DeusHeldAstralStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "AstralStaff";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.45f;
    }

    internal sealed class DeusHeldRadiantStar : BossHeldAimedWeapon
    {
        public override string WeaponName => "RadiantStar";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    internal sealed class DeusHeldTrueBiomeBlade : BossHeldSwingWeapon
    {
        public override string WeaponName => "TrueBiomeBlade";
        public override float SpriteScale => 1.45f;
        public override int WindupTime => 26;
        public override int SlashTime => 12;
        public override int RecoveryTime => 20;
        public override Color GlowColor => new Color(200, 120, 255) * 0.5f;
    }
}
