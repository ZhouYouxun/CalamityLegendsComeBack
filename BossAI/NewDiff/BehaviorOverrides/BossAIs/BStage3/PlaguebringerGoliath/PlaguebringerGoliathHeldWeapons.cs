using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.PlaguebringerGoliath
{
    // =====================================================================================================================
    // VIRULENCE — held greatsword swing, matching Cryogen's Avalanche/Darklight windup->quiver->whip timeline.
    // =====================================================================================================================
    internal sealed class PlagueHeldVirulence : BossHeldSwingWeapon
    {
        public override string WeaponName => "Virulence";
        public override float SpriteScale => 1.5f;
        public override int WindupTime => 40;
        public override int SlashTime => 16;
        public override int RecoveryTime => 24;
        public override float WindupAngle => -2.2f;
        public override float FollowThroughAngle => 1.2f;
        public override float HitboxOutset => 78f;
        public override Vector2 HitboxSize => new(84f, 84f);
        public override Color GlowColor => new Color(90, 220, 60) * 0.55f;

        public override void OnSlashBegin() => SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);

        public override void OnSlashing(float slashProgress)
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = BladePoint(Main.rand.NextFloat(40f, 110f) * Projectile.scale);
                Dust d = Dust.NewDustPerfect(pos, DustID.Venom, (FinalRotation + MathHelper.PiOver2 * SwingDir).ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }
    }

    // =====================================================================================================================
    // MALEVOLENCE — bow held skyward, recoils on each executioner volley (matches HoarfrostBow's pattern exactly).
    // =====================================================================================================================
    internal sealed class PlagueHeldMalevolence : BossHeldAimedWeapon
    {
        public override string WeaponName => "Malevolence";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.55f);
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;
        public override int PulseDuration => 16;

        public override void HeldExtra(float time) => RotationOffset = MathHelper.Lerp(RotationOffset, MathF.Sin(time * 0.06f) * 0.1f, 0.2f);
    }

    // =====================================================================================================================
    // PLAGUE STAFF — raised aloft, recoils as each fang sigil is cast.
    // =====================================================================================================================
    internal sealed class PlagueHeldStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "PlagueStaff";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.5f);
        public override Color GlowColor => new Color(140, 90, 220) * 0.5f;
        public override int PulseDuration => 15;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.05f) * 0.12f;
    }

    // =====================================================================================================================
    // FUEL CELL BUNDLE — held at the hip, thrust-tossed forward with each flask throw.
    // =====================================================================================================================
    internal sealed class PlagueHeldFuelCell : BossHeldAimedWeapon
    {
        public override string WeaponName => "FuelCellBundle";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.08f;
        public override float RestOutset => 12f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;
    }

    // =====================================================================================================================
    // INFECTED REMOTE — held terminal, light idle sway, brief pulse as Virili is summoned.
    // =====================================================================================================================
    internal sealed class PlagueHeldRemote : BossHeldAimedWeapon
    {
        public override string WeaponName => "InfectedRemote";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.05f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(90, 220, 60) * 0.45f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.04f) * 0.08f;
    }

    // =====================================================================================================================
    // THE SYRINGE — held javelin that jabs forward as it's thrown (the thrust is a real melee hit, matching Cryogen's
    // Crystal Piercer pattern: the held weapon sells the throw, a separate ranged projectile carries the flight/embed).
    // =====================================================================================================================
    internal sealed class PlagueHeldSyringe : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheSyringe";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.06f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.1f) * 0.03f;
    }

    // =====================================================================================================================
    // THE HIVE — held cannon, recoils hard with each nuke launch.
    // =====================================================================================================================
    internal sealed class PlagueHeldHive : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheHive";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.07f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;
    }

    // =====================================================================================================================
    // PESTILENT DEFILER — held rifle, tracks player continuously, light recoil per burst.
    // =====================================================================================================================
    internal sealed class PlagueHeldDefiler : BossHeldAimedWeapon
    {
        public override string WeaponName => "PestilentDefiler";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.12f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;
    }

    // =====================================================================================================================
    // MALACHITE — held dagger, quick flick with each lock-fire in the sequence.
    // =====================================================================================================================
    internal sealed class PlagueHeldMalachite : BossHeldAimedWeapon
    {
        public override string WeaponName => "Malachite";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.1f;
        public override float RestOutset => 12f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(90, 220, 100) * 0.5f;
    }

    // =====================================================================================================================
    // BLIGHT SPEWER — held flamethrower, sustained hold that sweeps its aim through the burn arc.
    // =====================================================================================================================
    internal sealed class PlagueHeldBlightSpewer : BossHeldAimedWeapon
    {
        public override string WeaponName => "BlightSpewer";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(140, 255, 100) * 0.5f;
    }

    // =====================================================================================================================
    // PANDEMIC — one yoyo idles near the boss's hand while its co-orbiting twin does the actual work.
    // =====================================================================================================================
    internal sealed class PlagueHeldPandemic : BossHeldAimedWeapon
    {
        public override string WeaponName => "Pandemic";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.04f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset += 0.08f;
    }

    // =====================================================================================================================
    // PLAGUE TAINTED SMG — held gun, continuous tracking with steady recoil chatter during the spray.
    // =====================================================================================================================
    internal sealed class PlagueHeldSMG : BossHeldAimedWeapon
    {
        public override string WeaponName => "PlagueTaintedSMG";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.14f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(90, 220, 60) * 0.5f;
    }
}
