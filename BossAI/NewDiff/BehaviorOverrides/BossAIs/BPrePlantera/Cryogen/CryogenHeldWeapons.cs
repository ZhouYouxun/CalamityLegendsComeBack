using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.Cryogen
{
    // =====================================================================================================================
    // AVALANCHE — held axe for the slam attack. Timeline mirrors the boss dive exactly when spawned at attack frame 1:
    // heave back 68f -> quiver at apex 22f (dive telegraph runs here) -> 14f whip synced with the 26-speed dive -> recover.
    // The whip itself is a real melee arc: standing next to the slam is now punished by the axe, not just the body.
    // =====================================================================================================================
    internal sealed class CryogenHeldAvalanche : BossHeldSwingWeapon
    {
        public override string WeaponName => "Avalanche";
        public override float SpriteScale => 1.6f;
        public override int WindupTime => 68;
        public override int HoldTime => 22;
        public override int SlashTime => 14;
        public override int RecoveryTime => 45;
        public override float WindupAngle => -2.5f;
        public override float FollowThroughAngle => 1.1f;
        public override float HitboxOutset => 78f;
        public override Vector2 HitboxSize => new(92f, 92f);
        public override Color GlowColor => Color.DeepSkyBlue * 0.55f;

        public override void OnSlashBegin()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = -0.35f }, Projectile.Center);
        }

        public override void OnSlashing(float slashProgress)
        {
            // Frost shears off the blade edge through the whip
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = BladePoint(Main.rand.NextFloat(40f, 110f) * Projectile.scale);
                Dust d = Dust.NewDustPerfect(pos, DustID.Ice, (FinalRotation + MathHelper.PiOver2 * SwingDir).ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 100, Color.Cyan, 1.1f);
                d.noGravity = true;
            }
        }
    }

    // =====================================================================================================================
    // DARKLIGHT GREATSWORD — held blade for the lunge slash. Spawned when the telegraph line appears; the windup burns
    // alongside the line, then OnSlashBegin re-aims along the boss's actual dash velocity so blade and body always agree.
    // =====================================================================================================================
    internal sealed class CryogenHeldDarklight : BossHeldSwingWeapon
    {
        public override string WeaponName => "DarklightGreatsword";
        public override float SpriteScale => 1.5f;
        public override int WindupTime => 34;
        public override int SlashTime => 20;
        public override int RecoveryTime => 20;
        public override float WindupAngle => -2.3f;
        public override float FollowThroughAngle => 1.3f;
        public override float HitboxOutset => 82f;
        public override Vector2 HitboxSize => new(84f, 84f);
        public override Color GlowColor => Color.MediumPurple * 0.55f;

        public override void OnSlashBegin()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = 0.15f }, Projectile.Center);
        }

        public override void OnSlashing(float slashProgress)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = BladePoint(Main.rand.NextFloat(40f, 115f) * Projectile.scale);
                Dust d = Dust.NewDustPerfect(pos, DustID.Vortex, (FinalRotation + MathHelper.PiOver2 * SwingDir).ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 120, Color.MediumPurple, 1f);
                d.noGravity = true;
            }
        }
    }

    // =====================================================================================================================
    // HOARFROST BOW — held above the boss, drawn skyward for the curtain volleys. Purely cosmetic (damage 0):
    // each volley kicks the bow back along its aim (negative pulse = recoil), selling "the bow fired the rain".
    // =====================================================================================================================
    internal sealed class CryogenHeldHoarfrostBow : BossHeldAimedWeapon
    {
        public override string WeaponName => "HoarfrostBow";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.55f);
        public override Color GlowColor => Color.DeepSkyBlue * 0.5f;
        public override int PulseDuration => 16;

        public override void HeldExtra(float time)
        {
            // Idle sway while nocked
            RotationOffset = MathHelper.Lerp(RotationOffset, MathF.Sin(time * 0.06f) * 0.1f, 0.2f);
        }
    }

    // =====================================================================================================================
    // STARNIGHT LANCE — held at the flank during the triple-beam channel. The AI snaps SetAim to each locked warning line
    // and fires a forward thrust pulse as its beam launches: the lance physically stabs along the line the beam rides.
    // The thrust is a real melee hit — hugging the boss during the channel is punished.
    // =====================================================================================================================
    internal sealed class CryogenHeldStarnightLance : BossHeldAimedWeapon
    {
        public override string WeaponName => "StarnightLance";
        public override float SpriteScale => 1.5f;
        public override float RestOutset => 26f;
        public override int PulseDuration => 15;
        public override float HitboxOutset => 70f;
        public override Vector2 HitboxSize => new(70f, 46f);
        public override Color GlowColor => Color.Cyan * 0.5f;

        public override void HeldExtra(float time)
        {
            // Held steady on the aim line — a faint shiver builds anticipation
            RotationOffset = MathF.Sin(time * 0.15f) * 0.02f;
        }
    }

    // =====================================================================================================================
    // ICEBREAKER — held spinning hammer while the thrown copies fly. Lunges forward with each dash-and-throw.
    // =====================================================================================================================
    internal sealed class CryogenHeldIcebreaker : BossHeldAimedWeapon
    {
        public override string WeaponName => "Icebreaker";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override float RestOutset => 18f;
        public override int PulseDuration => 16;
        public override Color GlowColor => Color.DeepSkyBlue * 0.5f;

        public override void HeldExtra(float time)
        {
            RotationOffset += 0.12f; // constant hammer spin
        }
    }

    // =====================================================================================================================
    // SNOWSTORM STAFF — raised skyward, bobbing with the cast rhythm; recoils as each orbiting snowflake is conjured.
    // =====================================================================================================================
    internal sealed class CryogenHeldSnowstormStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "SnowstormStaff";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.5f);
        public override int PulseDuration => 15;
        public override Color GlowColor => Color.DeepSkyBlue * 0.5f;

        public override void HeldExtra(float time)
        {
            RotationOffset = MathF.Sin(time * 0.05f) * 0.12f;
        }
    }

    // =====================================================================================================================
    // SHADECRYSTAL — the crystal focus held aloft; recoils as each ring of shade crystals condenses around the player.
    // =====================================================================================================================
    internal sealed class CryogenHeldShadecrystal : BossHeldAimedWeapon
    {
        public override string WeaponName => "ShadecrystalBarrage";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.45f);
        public override int PulseDuration => 14;
        public override Color GlowColor => Color.MediumPurple * 0.55f;

        public override void HeldExtra(float time)
        {
            RotationOffset = MathF.Sin(time * 0.06f) * 0.15f;
        }
    }

    // =====================================================================================================================
    // DAEDALUS GOLEM STAFF — summoning staff; kicks back whenever the boss's telegraphed light beam fires.
    // =====================================================================================================================
    internal sealed class CryogenHeldDaedalusStaff : BossHeldAimedWeapon
    {
        public override string WeaponName => "DaedalusGolemStaff";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 14;
        public override Color GlowColor => Color.MediumPurple * 0.5f;
    }

    // =====================================================================================================================
    // DARKECHO GREATBOW — tracks the player through the yoyo dance; recoils with each arrow-and-dart volley.
    // =====================================================================================================================
    internal sealed class CryogenHeldDarkechoBow : BossHeldAimedWeapon
    {
        public override string WeaponName => "DarkechoGreatbow";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.12f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => Color.MediumPurple * 0.55f;
    }

    // =====================================================================================================================
    // CRYSTAL PIERCER — a held javelin that jabs toward the player as each phalanx group launches from off-screen.
    // =====================================================================================================================
    internal sealed class CryogenHeldCrystalPiercer : BossHeldAimedWeapon
    {
        public override string WeaponName => "CrystalPiercer";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.06f;
        public override float RestOutset => 20f;
        public override int PulseDuration => 14;
        public override Color GlowColor => Color.MediumPurple * 0.5f;

        public override void HeldExtra(float time)
        {
            RotationOffset = MathF.Sin(time * 0.1f) * 0.03f;
        }
    }
}
