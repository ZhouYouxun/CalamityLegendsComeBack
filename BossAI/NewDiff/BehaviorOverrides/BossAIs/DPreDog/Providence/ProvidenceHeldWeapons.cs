using System;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Providence
{
    // =====================================================================================================================
    // P1 — Providence's own arsenal
    // =====================================================================================================================
    internal sealed class ProvHeldHolyCollider : BossHeldSwingWeapon
    {
        public override string WeaponName => "HolyCollider";
        public override float SpriteScale => 1.5f;
        public override int WindupTime => 26;
        public override int SlashTime => 12;
        public override int RecoveryTime => 20;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldBurningRevelation : BossHeldAimedWeapon
    {
        public override string WeaponName => "BurningRevelation";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 140, 40) * 0.5f;
    }

    internal sealed class ProvHeldTelluricGlare : BossHeldAimedWeapon
    {
        public override string WeaponName => "TelluricGlare";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.06f;
        public override float RestOutset => 18f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldBlissfulBombardier : BossHeldAimedWeapon
    {
        public override string WeaponName => "BlissfulBombardier";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldPurgeGuzzler : BossHeldAimedWeapon
    {
        public override string WeaponName => "PurgeGuzzler";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 245, 190) * 0.5f;
    }

    internal sealed class ProvHeldDazzlingStabber : BossHeldAimedWeapon
    {
        public override string WeaponName => "DazzlingStabberStaff";
        public override float SpriteScale => 1.4f;
        public override Vector2 GripOffset => new(0f, -Anchor.height * 0.4f);
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldMoltenAmputator : BossHeldAimedWeapon
    {
        public override string WeaponName => "MoltenAmputator";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 120, 40) * 0.5f;
    }

    internal sealed class ProvHeldPristineFury : BossHeldAimedWeapon
    {
        public override string WeaponName => "PristineFury";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 14f;
        public override int PulseDuration => 8;
        public override Color GlowColor => Color.White * 0.5f;
    }

    // =====================================================================================================================
    // P2 — Divine Geode arsenal
    // =====================================================================================================================
    internal sealed class ProvHeldAetherfluxCannon : BossHeldAimedWeapon
    {
        public override string WeaponName => "AetherfluxCannon";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(120, 190, 255) * 0.5f;
    }

    internal sealed class ProvHeldAngelicShotgun : BossHeldAimedWeapon
    {
        public override string WeaponName => "AngelicShotgun";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.1f;
        public override int PulseDuration => 8;
        public override Color GlowColor => new Color(255, 235, 150) * 0.5f;
    }

    internal sealed class ProvHeldDarkSpark : BossHeldAimedWeapon
    {
        public override string WeaponName => "DarkSpark";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class ProvHeldGalactusBlade : BossHeldSwingWeapon
    {
        public override string WeaponName => "GalactusBlade";
        public override float SpriteScale => 1.5f;
        public override int WindupTime => 24;
        public override int SlashTime => 10;
        public override int RecoveryTime => 18;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldMirrorOfKalandra : BossHeldAimedWeapon
    {
        public override string WeaponName => "MirrorofKalandra";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 240, 255) * 0.5f;

        public override void HeldExtra(float time) => RotationOffset = MathF.Sin(time * 0.03f) * 0.06f;
    }

    internal sealed class ProvHeldMourningstar : BossHeldAimedWeapon
    {
        public override string WeaponName => "Mourningstar";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldShatteredDawn : BossHeldAimedWeapon
    {
        public override string WeaponName => "ShatteredDawn";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.08f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(255, 230, 120) * 0.5f;
    }

    internal sealed class ProvHeldSeekingScorcher : BossHeldSwingWeapon
    {
        public override string WeaponName => "SeekingScorcher";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 20;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(255, 140, 40) * 0.5f;
    }

    internal sealed class ProvHeldMaelstrom : BossHeldAimedWeapon
    {
        public override string WeaponName => "TheMaelstrom";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(255, 245, 190) * 0.5f;
    }

    internal sealed class ProvHeldPrince : BossHeldAimedWeapon
    {
        public override string WeaponName => "ThePrince";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }
}
