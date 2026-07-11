using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumAureus
{
    // =====================================================================================================================
    // NEBULASH — held whip, heavy windup heave into an explosive crack.
    // =====================================================================================================================
    internal sealed class AureusHeldNebulash : BossHeldSwingWeapon
    {
        public override string WeaponName => "Nebulash";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 26;
        public override int SlashTime => 10;
        public override int RecoveryTime => 20;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    // =====================================================================================================================
    // AURORA BLAZER — held twin-barrel rifle, tracks continuously, light recoil per burst.
    // =====================================================================================================================
    internal sealed class AureusHeldAuroraBlazer : BossHeldAimedWeapon
    {
        public override string WeaponName => "AuroraBlazer";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.09f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(255, 120, 200) * 0.5f;
    }

    // =====================================================================================================================
    // ALULA AUSTRALIS — held staff, gentle sway while the wing feathers do the work.
    // =====================================================================================================================
    internal sealed class AureusHeldAlulaAustralis : BossHeldAimedWeapon
    {
        public override string WeaponName => "AlulaAustralis";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(230, 200, 60) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.04f) * 0.1f;
    }

    // =====================================================================================================================
    // BOREALIS BOMBER — held launcher, kicks upward as it lofts each bomb skyward.
    // =====================================================================================================================
    internal sealed class AureusHeldBorealisBomber : BossHeldAimedWeapon
    {
        public override string WeaponName => "BorealisBomber";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    // =====================================================================================================================
    // AURORADICAL THROW — held boomerang grip, sharp forward pulse as it's launched.
    // =====================================================================================================================
    internal sealed class AureusHeldAuroradical : BossHeldAimedWeapon
    {
        public override string WeaponName => "AuroradicalThrow";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.08f;
    }

    // =====================================================================================================================
    // ASTRAL SCYTHE — held reversed, motion-blurred sway echoing the two scythes crossing the field.
    // =====================================================================================================================
    internal sealed class AureusHeldAstralScythe : BossHeldAimedWeapon
    {
        public override string WeaponName => "AstralScythe";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.35f);
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.05f) * 0.14f;
    }

    // =====================================================================================================================
    // TITAN ARM — held gauntlet, forward thrust pulse timed to the ground-fist eruption.
    // =====================================================================================================================
    internal sealed class AureusHeldTitanArm : BossHeldAimedWeapon
    {
        public override string WeaponName => "TitanArm";
        public override float SpriteScale => 1.45f;
        public override float RestOutset => 10f;
        public override int PulseDuration => 18;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    // =====================================================================================================================
    // STELLAR CANNON — held barrel, locked aim while charging, hard recoil pulse on fire.
    // =====================================================================================================================
    internal sealed class AureusHeldStellarCannon : BossHeldAimedWeapon
    {
        public override string WeaponName => "StellarCannon";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.05f;
        public override float RestOutset => 18f;
        public override int PulseDuration => 20;
        public override Color GlowColor => new Color(255, 240, 180) * 0.55f;
    }

    // =====================================================================================================================
    // STELLAR KNIFE — held rogue blade, brief pulse as each knife volley is tossed skyward.
    // =====================================================================================================================
    internal sealed class AureusHeldStellarKnife : BossHeldAimedWeapon
    {
        public override string WeaponName => "StellarKnife";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(230, 200, 60) * 0.5f;
    }

    // =====================================================================================================================
    // ASTRALACHNEA STAFF — held staff, slow idle sway while the web anchors set into the walls.
    // =====================================================================================================================
    internal sealed class AureusHeldAstralachnea : BossHeldAimedWeapon
    {
        public override string WeaponName => "AstralachneaStaff";
        public override float SpriteScale => 1.3f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.035f) * 0.09f;
    }

    // =====================================================================================================================
    // ABANDONED SLIME STAFF — held staff, small dip as the slime core is dropped.
    // =====================================================================================================================
    internal sealed class AureusHeldAbandonedSlime : BossHeldAimedWeapon
    {
        public override string WeaponName => "AbandonedSlimeStaff";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(230, 200, 60) * 0.45f;
    }

    // =====================================================================================================================
    // HIVE POD — held over the shoulder, forward pulse as the pod is hurled out.
    // =====================================================================================================================
    internal sealed class AureusHeldHivePod : BossHeldAimedWeapon
    {
        public override string WeaponName => "HivePod";
        public override float SpriteScale => 1.35f;
        public override float RestOutset => 12f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }
}
