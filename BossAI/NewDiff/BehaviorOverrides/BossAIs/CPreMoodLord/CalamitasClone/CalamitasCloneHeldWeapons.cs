using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.CalamitasClone
{
    // =====================================================================================================================
    // OBLIVION — held near the hip while the thrown yoyo does the sweeping; recoils once as it's cast out.
    // =====================================================================================================================
    internal sealed class CalHeldOblivion : BossHeldAimedWeapon
    {
        public override string WeaponName => "Oblivion";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.06f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(220, 60, 60) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.1f;
    }

    // =====================================================================================================================
    // ANIMOSITY — held rifle, tracks continuously, kicks back hard on the sniper shot.
    // =====================================================================================================================
    internal sealed class CalHeldAnimosity : BossHeldAimedWeapon
    {
        public override string WeaponName => "Animosity";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(255, 60, 60) * 0.55f;
    }

    // =====================================================================================================================
    // LASHES OF CHAOS — held staff, light sway, recoils as each hellfireball is cast.
    // =====================================================================================================================
    internal sealed class CalHeldLashes : BossHeldAimedWeapon
    {
        public override string WeaponName => "LashesofChaos";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.5f);
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(220, 40, 40) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.05f) * 0.12f;
    }

    // =====================================================================================================================
    // ENTROPY'S VIGIL — held aloft while the eye guardians orbit separately; light idle sway.
    // =====================================================================================================================
    internal sealed class CalHeldVigil : BossHeldAimedWeapon
    {
        public override string WeaponName => "EntropysVigil";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.45f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 60, 60) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.04f) * 0.08f;
    }

    // =====================================================================================================================
    // CRUSHSAW CRASHER — held rogue weapon, forward-thrust pulse as the sawblade is thrown.
    // =====================================================================================================================
    internal sealed class CalHeldCrushsaw : BossHeldAimedWeapon
    {
        public override string WeaponName => "CrushsawCrasher";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.08f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(220, 60, 60) * 0.5f;
    }

    // =====================================================================================================================
    // HAVOC'S BREATH — held flamethrower, sustained hold sweeping through the burn arc.
    // =====================================================================================================================
    internal sealed class CalHeldHavoc : BossHeldAimedWeapon
    {
        public override string WeaponName => "HavocsBreath";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(255, 140, 60) * 0.5f;
    }
}
