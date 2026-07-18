using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.Ravager
{
    // =====================================================================================================================
    // P1 — Body's own arsenal
    // =====================================================================================================================
    internal sealed class RavagerHeldUltimusCleaver : BossHeldSwingWeapon
    {
        public override string WeaponName => "UltimusCleaver";
        public override float SpriteScale => 1.6f;
        public override int WindupTime => 30;
        public override int SlashTime => 14;
        public override int RecoveryTime => 24;
        public override Color GlowColor => new Color(200, 40, 40) * 0.5f;
    }

    internal sealed class RavagerHeldRealmRavager : BossHeldAimedWeapon
    {
        public override string WeaponName => "RealmRavager";
        public override float SpriteScale => 1.5f;
        public override float AimLerp => 0.06f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(200, 40, 40) * 0.5f;
    }

    internal sealed class RavagerHeldHematemesis : BossHeldAimedWeapon
    {
        public override string WeaponName => "Hematemesis";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.35f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;
    }

    internal sealed class RavagerHeldCraniumSmasher : BossHeldAimedWeapon
    {
        public override string WeaponName => "CraniumSmasher";
        public override float SpriteScale => 1.5f;
        public override float AimLerp => 0.05f;
        public override int PulseDuration => 18;
        public override Color GlowColor => new Color(200, 40, 40) * 0.5f;
    }

    internal sealed class RavagerHeldVesuvius : BossHeldAimedWeapon
    {
        public override string WeaponName => "Vesuvius";
        public override float SpriteScale => 1.45f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(255, 140, 40) * 0.5f;
    }

    internal sealed class RavagerHeldCorpusAvertor : BossHeldAimedWeapon
    {
        public override string WeaponName => "CorpusAvertor";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 40, 40) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.08f;
    }

    // =====================================================================================================================
    // P2 — Bloodstone Core arsenal
    // =====================================================================================================================
    internal sealed class RavagerHeldMutilator : BossHeldSwingWeapon
    {
        public override string WeaponName => "TheMutilator";
        public override float SpriteScale => 1.5f;
        public override int WindupTime => 22;
        public override int SlashTime => 10;
        public override int RecoveryTime => 18;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;
    }

    internal sealed class RavagerHeldLacerator : BossHeldAimedWeapon
    {
        public override string WeaponName => "Lacerator";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;
    }

    internal sealed class RavagerHeldClaretCannon : BossHeldAimedWeapon
    {
        public override string WeaponName => "ClaretCannon";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.09f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(255, 60, 60) * 0.55f;
    }

    internal sealed class RavagerHeldArterialAssault : BossHeldAimedWeapon
    {
        public override string WeaponName => "ArterialAssault";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;
    }

    internal sealed class RavagerHeldBloodBoiler : BossHeldAimedWeapon
    {
        public override string WeaponName => "BloodBoiler";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(220, 40, 40) * 0.5f;
    }

    internal sealed class RavagerHeldSanguineFlare : BossHeldAimedWeapon
    {
        public override string WeaponName => "SanguineFlare";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 18;
        public override Color GlowColor => new Color(220, 30, 30) * 0.55f;
    }

    internal sealed class RavagerHeldViscera : BossHeldAimedWeapon
    {
        public override string WeaponName => "Viscera";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.04f) * 0.09f;
    }

    internal sealed class RavagerHeldDragonbloodDisgorger : BossHeldAimedWeapon
    {
        public override string WeaponName => "DragonbloodDisgorger";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 12f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(220, 60, 40) * 0.5f;
    }

    internal sealed class RavagerHeldBloodsoakedCrasher : BossHeldSwingWeapon
    {
        public override string WeaponName => "BloodsoakedCrasher";
        public override float SpriteScale => 1.6f;
        public override int WindupTime => 34;
        public override int SlashTime => 12;
        public override int RecoveryTime => 26;
        public override Color GlowColor => new Color(200, 30, 30) * 0.5f;
    }
}
