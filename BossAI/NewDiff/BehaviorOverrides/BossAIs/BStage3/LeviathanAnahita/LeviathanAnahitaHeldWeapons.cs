using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.LeviathanAnahita
{
    // =====================================================================================================================
    // GREENTIDE — Anahita's held tide-blade, a heavy singing swing as the wave-cutters are called down.
    // =====================================================================================================================
    internal sealed class AnahitaHeldGreentide : BossHeldSwingWeapon
    {
        public override string WeaponName => "Greentide";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 28;
        public override int SlashTime => 12;
        public override int RecoveryTime => 22;
        public override Color GlowColor => new Color(60, 220, 160) * 0.5f;
    }

    // =====================================================================================================================
    // ANAHITA'S ARPEGGIO — held harp, gentle sway as each note in the phrase is plucked.
    // =====================================================================================================================
    internal sealed class AnahitaHeldArpeggio : BossHeldAimedWeapon
    {
        public override string WeaponName => "AnahitasArpeggio";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(120, 220, 255) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.05f) * 0.09f;
    }

    // =====================================================================================================================
    // ATLANTIS — Anahita's held trident, raised aloft while its copies lock the triangle around the player.
    // =====================================================================================================================
    internal sealed class AnahitaHeldAtlantis : BossHeldAimedWeapon
    {
        public override string WeaponName => "Atlantis";
        public override float SpriteScale => 1.35f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.45f);
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(60, 160, 255) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.02f;
    }

    // =====================================================================================================================
    // LEVIATITAN — Leviathan's cannon, protruding from the jaw and recoiling hard as the bubble is fired.
    // =====================================================================================================================
    internal sealed class LeviathanHeldLeviatitan : BossHeldAimedWeapon
    {
        public override string WeaponName => "Leviatitan";
        public override float SpriteScale => 1.5f;
        public override float AimLerp => 0.06f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 20;
        public override Color GlowColor => new Color(60, 160, 255) * 0.5f;
    }

    // =====================================================================================================================
    // GASTRIC BELCHER — Leviathan's held staff, summoning the acid stomach out to one side.
    // =====================================================================================================================
    internal sealed class LeviathanHeldGastricBelcher : BossHeldAimedWeapon
    {
        public override string WeaponName => "GastricBelcherStaff";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(90, 220, 60) * 0.45f;
    }

    // =====================================================================================================================
    // LEVIATHAN TEETH — held dagger fan, sharp forward pulse as the teeth are flung.
    // =====================================================================================================================
    internal sealed class LeviathanHeldTeeth : BossHeldAimedWeapon
    {
        public override string WeaponName => "LeviathanTeeth";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(60, 160, 255) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.06f;
    }
}
