using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Polterghast
{
    internal sealed class GhastHeldTerrorBlade : BossHeldSwingWeapon
    {
        public override string WeaponName => "TerrorBlade";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 22;
        public override int SlashTime => 10;
        public override int RecoveryTime => 16;
        public override Color GlowColor => new Color(180, 30, 60) * 0.5f;
    }

    internal sealed class GhastHeldBansheeHook : BossHeldAimedWeapon
    {
        public override string WeaponName => "BansheeHook";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.08f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(180, 30, 60) * 0.5f;
    }

    internal sealed class GhastHeldDaemonsFlame : BossHeldAimedWeapon
    {
        public override string WeaponName => "DaemonsFlame";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldFatesReveal : BossHeldAimedWeapon
    {
        public override string WeaponName => "FatesReveal";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldGhastlyVisage : BossHeldAimedWeapon
    {
        public override string WeaponName => "GhastlyVisage";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldEtherealSubjugator : BossHeldAimedWeapon
    {
        public override string WeaponName => "EtherealSubjugator";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 100, 200) * 0.5f;
    }

    internal sealed class GhastHeldGhoulishGouger : BossHeldAimedWeapon
    {
        public override string WeaponName => "GhoulishGouger";
        public override float SpriteScale => 1.4f;
        public override float AimLerp => 0.09f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldGalileoGladius : BossHeldSwingWeapon
    {
        public override string WeaponName => "GalileoGladius";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 14;
        public override int SlashTime => 8;
        public override int RecoveryTime => 10;
        public override Color GlowColor => new Color(220, 160, 255) * 0.5f;
    }

    internal sealed class GhastHeldCrescentMoon : BossHeldAimedWeapon
    {
        public override string WeaponName => "CrescentMoon";
        public override float SpriteScale => 1.4f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldHalleysInferno : BossHeldAimedWeapon
    {
        public override string WeaponName => "HalleysInferno";
        public override float SpriteScale => 1.35f;
        public override float AimLerp => 0.09f;
        public override int PulseDuration => 10;
        public override Color GlowColor => new Color(255, 140, 40) * 0.5f;
    }

    internal sealed class GhastHeldAlphaDraconis : BossHeldAimedWeapon
    {
        public override string WeaponName => "AlphaDraconis";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(90, 160, 255) * 0.5f;
    }

    internal sealed class GhastHeldStratusSphere : BossHeldAimedWeapon
    {
        public override string WeaponName => "StratusSphere";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldSirius : BossHeldAimedWeapon
    {
        public override string WeaponName => "Sirius";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 240, 255) * 0.5f;
    }

    internal sealed class GhastHeldWarloksMoon : BossHeldAimedWeapon
    {
        public override string WeaponName => "WarloksMoonFist";
        public override float SpriteScale => 1.4f;
        public override float RestOutset => 16f;
        public override int PulseDuration => 16;
        public override Color GlowColor => new Color(160, 60, 220) * 0.5f;
    }

    internal sealed class GhastHeldVega : BossHeldAimedWeapon
    {
        public override string WeaponName => "Vega";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(220, 160, 255) * 0.5f;
    }
}
